using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal static class UiVisualTestRunner
{
    private static readonly Size[] StandardSizes =
    {
        new(1366, 768),
        new(1100, 700)
    };

    public static bool IsUiTestRequested(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return false;
        }

        return args.Any(a =>
            string.Equals(a, "--ui-test", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "/ui-test", StringComparison.OrdinalIgnoreCase));
    }

    public static int Run(string[]? args)
    {
        DateTime startedAt = DateTime.Now;
        string timestamp = startedAt.ToString("yyyyMMdd-HHmmss");
        string artifactsRoot = Path.Combine(AppContext.BaseDirectory, "Artifacts", "ui-visual", timestamp);
        Directory.CreateDirectory(artifactsRoot);

        var issues = new List<UiIssue>();
        AppLogger.LogInfo($"UI visual test started. Output: {artifactsRoot}");

        Size[] standardSizes = ResolveRequestedSizes(args);
        TrySeedSession(issues);
        int sampleUserId = ResolveSampleUserId();
        var plans = BuildPlans(sampleUserId, standardSizes);

        foreach (FormPlan plan in plans)
        {
            foreach (Size requestedSize in plan.Sizes)
            {
                RunPlan(plan, requestedSize, artifactsRoot, issues);
            }
        }

        string reportPath = WriteReport(artifactsRoot, startedAt, DateTime.Now, plans, issues);
        AppLogger.LogInfo($"UI visual test completed. Report: {reportPath}");

        bool hasCritical = issues.Any(i => i.Severity == UiIssueSeverity.Critical);
        bool hasWarnings = issues.Any(i => i.Severity == UiIssueSeverity.Warning);
        Console.WriteLine(hasCritical
            ? $"UI_TEST_RESULT=FAILED report={reportPath}"
            : hasWarnings
                ? $"UI_TEST_RESULT=PASSED_WITH_WARNINGS report={reportPath}"
                : $"UI_TEST_RESULT=PASSED report={reportPath}");

        return hasCritical ? 1 : 0;
    }

    private static Size[] ResolveRequestedSizes(string[]? args)
    {
        var sizes = new List<Size>(StandardSizes);
        if (!IsFullscreenRequested(args))
        {
            return sizes.ToArray();
        }

        Rectangle bounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var fullscreenSize = new Size(
            Math.Max(1024, bounds.Width),
            Math.Max(720, bounds.Height));

        if (!sizes.Contains(fullscreenSize))
        {
            sizes.Insert(0, fullscreenSize);
        }

        return sizes.ToArray();
    }

    private static bool IsFullscreenRequested(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            return false;
        }

        return args.Any(a =>
            string.Equals(a, "--ui-test-fullscreen", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "/ui-test-fullscreen", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "--fullscreen", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "/fullscreen", StringComparison.OrdinalIgnoreCase));
    }

    private static List<FormPlan> BuildPlans(int sampleUserId, IReadOnlyList<Size> standardSizes)
    {
        var plans = new List<FormPlan>
        {
            new("Login", () => new Form1(), standardSizes),
            new("AdminDashboard", () => new AdminDashboard(), standardSizes),
            new("AdminDashboardResidents", () =>
            {
                var form = new AdminDashboard();
                form.OpenResidentsFromDashboard(ResidentsView.Profile);
                return form;
            }, standardSizes),
            new("Residents", () => new Residents(), standardSizes),
            new("ResidentsEmbedded", () =>
            {
                var form = new Residents();
                form.ConfigureForEmbeddedNavigation();
                return form;
            }, standardSizes),
            new("Reports", () => new Reports(), standardSizes),
            new("SidebarSettings", () => new SidebarSettingsForm(), new[] { new Size(700, 520) })
        };

        if (sampleUserId > 0)
        {
            plans.Add(new FormPlan("UpdateUser", () => new UpdateUserForm(sampleUserId), new[] { new Size(760, 620) }));
        }

        return plans;
    }

    private static void TrySeedSession(List<UiIssue> issues)
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"SELECT ua.user_id,
                         ua.barangay_id,
                         ua.username,
                         COALESCE(r.name, 'Staff') AS role_name
                  FROM user_account ua
                  LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                  LEFT JOIN role r ON r.role_id = ur.role_id
                  WHERE ua.is_active = 1
                  ORDER BY (COALESCE(r.name, 'Staff') = 'Admin') DESC, ua.user_id ASC
                  LIMIT 1",
                conn);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                issues.Add(UiIssue.Warning("Session", "No active users found; UI checks run without seeded user session."));
                return;
            }

            UserSession.UserId = Convert.ToInt32(reader["user_id"]);
            UserSession.BarangayId = reader["barangay_id"] == DBNull.Value
                ? SchemaDefaults.DefaultBarangayId
                : Convert.ToInt32(reader["barangay_id"]);
            UserSession.Username = Convert.ToString(reader["username"]) ?? string.Empty;
            UserSession.Role = Convert.ToString(reader["role_name"]) ?? "Staff";
            Permissions.Refresh();
        }
        catch (Exception ex)
        {
            issues.Add(UiIssue.Warning("Session", $"Unable to seed test user session. {ex.Message}"));
        }
    }

    private static int ResolveSampleUserId()
    {
        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(
                "SELECT user_id FROM user_account WHERE is_active = 1 ORDER BY user_id ASC LIMIT 1",
                conn);
            object? value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(value);
        }
        catch
        {
            return 0;
        }
    }

    private static void RunPlan(FormPlan plan, Size requestedSize, string artifactsRoot, List<UiIssue> issues)
    {
        string runLabel = $"{plan.Name}@{requestedSize.Width}x{requestedSize.Height}";
        Stopwatch stopwatch = Stopwatch.StartNew();
        Form? form = null;

        try
        {
            form = plan.Factory();
            Size size = NormalizeSize(form, requestedSize);
            PrepareForm(form, size);

            form.Show();
            for (int i = 0; i < 4; i++)
            {
                Application.DoEvents();
                Thread.Sleep(120);
            }

            ValidateControlTree(form, form, issues, plan.Name);
            CaptureSnapshot(form, artifactsRoot, plan.Name, size, issues);
        }
        catch (Exception ex)
        {
            issues.Add(UiIssue.Critical(runLabel, $"Failed: {ex.Message}"));
            AppLogger.LogError($"UI visual test failed for {runLabel}.", ex);
        }
        finally
        {
            try
            {
                if (form != null && !form.IsDisposed)
                {
                    form.Hide();
                    form.Close();
                    form.Dispose();
                }
            }
            catch
            {
                // best effort cleanup
            }

            stopwatch.Stop();
            AppLogger.LogInfo($"UI visual run completed: {runLabel} ({stopwatch.ElapsedMilliseconds} ms)");
        }
    }

    private static Size NormalizeSize(Form form, Size requested)
    {
        int width = Math.Max(requested.Width, Math.Max(640, form.MinimumSize.Width));
        int height = Math.Max(requested.Height, Math.Max(420, form.MinimumSize.Height));
        return new Size(width, height);
    }

    private static void PrepareForm(Form form, Size size)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(30, 30);
        form.WindowState = FormWindowState.Normal;
        form.Size = size;
        form.BringToFront();
    }

    private static void CaptureSnapshot(Form form, string artifactsRoot, string formName, Size size, List<UiIssue> issues)
    {
        try
        {
            using var bitmap = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height));
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            string fileName = $"{SanitizeName(formName)}_{size.Width}x{size.Height}.png";
            string path = Path.Combine(artifactsRoot, fileName);
            bitmap.Save(path, ImageFormat.Png);
        }
        catch (Exception ex)
        {
            issues.Add(UiIssue.Warning(formName, $"Screenshot capture failed ({size.Width}x{size.Height}): {ex.Message}"));
        }
    }

    private static void ValidateControlTree(Control root, Control parent, List<UiIssue> issues, string formName)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            bool parentCollapsed = parent.ClientSize.Width <= 1 || parent.ClientSize.Height <= 1;
            if (!parentCollapsed && (child.Width <= 0 || child.Height <= 0))
            {
                issues.Add(UiIssue.Warning(formName, $"Zero-sized control: {Describe(child)}"));
            }

            if (ShouldCheckBounds(parent, child))
            {
                Rectangle b = child.Bounds;
                int maxW = parent.ClientSize.Width;
                int maxH = parent.ClientSize.Height;
                const int tolerance = 2;
                if (b.Left < -tolerance ||
                    b.Top < -tolerance ||
                    b.Right > maxW + tolerance ||
                    b.Bottom > maxH + tolerance)
                {
                    issues.Add(UiIssue.Warning(
                        formName,
                        $"Possible clipped/offscreen control: {Describe(child)} in {Describe(parent)} bounds={b} parentClient={parent.ClientSize}"));
                }
            }

            ValidateTextFit(formName, child, issues);
            ValidateControlTree(root, child, issues, formName);
        }
    }

    private static void ValidateTextFit(string formName, Control control, List<UiIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(control.Text))
        {
            return;
        }

        if (control is not Label && control is not ButtonBase)
        {
            return;
        }

        if (control.AutoSize)
        {
            return;
        }

        Size measured = TextRenderer.MeasureText(control.Text, control.Font);
        int available = control.ClientSize.Width - 8;
        if (available > 0 && measured.Width > available + 14)
        {
            issues.Add(UiIssue.Warning(
                formName,
                $"Text may be clipped: {Describe(control)} text='{control.Text}' textWidth={measured.Width} available={available}"));
        }
    }

    private static bool ShouldCheckBounds(Control parent, Control child)
    {
        if (child.Dock != DockStyle.None)
        {
            return false;
        }

        if (parent is FlowLayoutPanel || parent is TableLayoutPanel || parent is SplitContainer || parent is TabControl)
        {
            return false;
        }

        if (parent is Panel panel && panel.AutoScroll)
        {
            return false;
        }

        return true;
    }

    private static string WriteReport(
        string artifactsRoot,
        DateTime startedAt,
        DateTime finishedAt,
        IReadOnlyCollection<FormPlan> plans,
        IReadOnlyCollection<UiIssue> issues)
    {
        string reportPath = Path.Combine(artifactsRoot, "report.md");
        var sb = new StringBuilder();
        sb.AppendLine("# UI Visual Check Report");
        sb.AppendLine();
        sb.AppendLine($"- Started: `{startedAt:yyyy-MM-dd HH:mm:ss}`");
        sb.AppendLine($"- Finished: `{finishedAt:yyyy-MM-dd HH:mm:ss}`");
        sb.AppendLine($"- Duration: `{(finishedAt - startedAt).TotalSeconds:F1}s`");
        sb.AppendLine($"- Forms tested: `{plans.Count}`");
        sb.AppendLine($"- Screenshots: `{artifactsRoot}`");
        sb.AppendLine();

        int criticalCount = issues.Count(i => i.Severity == UiIssueSeverity.Critical);
        int warningCount = issues.Count(i => i.Severity == UiIssueSeverity.Warning);
        sb.AppendLine($"- Critical issues: `{criticalCount}`");
        sb.AppendLine($"- Warnings: `{warningCount}`");
        sb.AppendLine();

        if (issues.Count == 0)
        {
            sb.AppendLine("No issues detected.");
        }
        else
        {
            sb.AppendLine("## Findings");
            foreach (UiIssue issue in issues)
            {
                sb.AppendLine($"- [{issue.Severity}] `{issue.Scope}` - {issue.Message}");
            }
        }

        File.WriteAllText(reportPath, sb.ToString());
        return reportPath;
    }

    private static string Describe(Control control)
    {
        string name = string.IsNullOrWhiteSpace(control.Name) ? "(unnamed)" : control.Name;
        return $"{control.GetType().Name}:{name}";
    }

    private static string SanitizeName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return value;
    }

    private sealed class FormPlan
    {
        public FormPlan(string name, Func<Form> factory, IReadOnlyList<Size> sizes)
        {
            Name = name;
            Factory = factory;
            Sizes = sizes;
        }

        public string Name { get; }
        public Func<Form> Factory { get; }
        public IReadOnlyList<Size> Sizes { get; }
    }

    private enum UiIssueSeverity
    {
        Warning,
        Critical
    }

    private sealed class UiIssue
    {
        private UiIssue(UiIssueSeverity severity, string scope, string message)
        {
            Severity = severity;
            Scope = scope;
            Message = message;
        }

        public UiIssueSeverity Severity { get; }
        public string Scope { get; }
        public string Message { get; }

        public static UiIssue Warning(string scope, string message)
        {
            return new UiIssue(UiIssueSeverity.Warning, scope, message);
        }

        public static UiIssue Critical(string scope, string message)
        {
            return new UiIssue(UiIssueSeverity.Critical, scope, message);
        }
    }
}
