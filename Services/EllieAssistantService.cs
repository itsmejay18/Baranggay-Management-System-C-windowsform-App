using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1;

internal sealed class EllieAssistantService
{
    private readonly OllamaClient _ollamaClient;

    public EllieAssistantService(OllamaClient? ollamaClient = null)
    {
        _ollamaClient = ollamaClient ?? new OllamaClient(model: "gemma3:1b");
    }

    public async Task<string> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "Please type your question first.";
        }

        string systemContext = await BuildSystemContextAsync(cancellationToken).ConfigureAwait(false);
        string prompt = BuildPrompt(question, systemContext);

        try
        {
            string output = await _ollamaClient.GenerateAsync(prompt, cancellationToken).ConfigureAwait(false);
            string cleaned = JsonUtils.TrimCodeFences(output).Trim();
            return string.IsNullOrWhiteSpace(cleaned)
                ? "I could not generate an answer right now."
                : cleaned;
        }
        catch (Exception ex)
        {
            return BuildFallbackAnswer(question, ex.Message);
        }
    }

    private async Task<string> BuildSystemContextAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Available modules:");
        sb.AppendLine("- Dashboard: KPIs, officials cards, trends, announcements, projects, action center.");
        sb.AppendLine("- Residents: profile details, photo, resident list, edit/add/delete.");
        sb.AppendLine("- Blotter: file cases, case status (Ongoing/Settled/Referred), respondent and incident tracking.");
        sb.AppendLine("- Certificates: requests, approval/issuance workflow, certificate types and records.");
        sb.AppendLine("- History: activity timeline and filtering by module/date.");
        sb.AppendLine("- Reports: summary and printable reports.");
        sb.AppendLine("- Settings: sidebar behavior options.");

        DashboardSnapshot snapshot = await LoadDashboardSnapshotAsync(cancellationToken).ConfigureAwait(false);
        sb.AppendLine();
        sb.AppendLine("Current live snapshot:");
        sb.AppendLine($"- Total residents: {snapshot.TotalResidents}");
        sb.AppendLine($"- Active residents: {snapshot.ActiveResidents}");
        sb.AppendLine($"- Households: {snapshot.Households}");
        sb.AppendLine($"- Pending certificates: {snapshot.PendingCertificates}");
        sb.AppendLine($"- Ongoing blotter: {snapshot.OngoingBlotter}");
        sb.AppendLine($"- Active staff/admin accounts: {snapshot.ActiveUsers}");

        string? projectRoot = TryGetProjectRoot();
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            AppendFileSnippet(sb, Path.Combine(projectRoot, "Database", "migrations", "20260211_new_schema.sql"), "Database schema snapshot");
            AppendFileSnippet(sb, Path.Combine(projectRoot, "Database", "rule", "ruletext.txt"), "System rule notes");
        }

        return sb.ToString();
    }

    private static void AppendFileSnippet(StringBuilder sb, string filePath, string title)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        string content = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        const int maxChars = 4500;
        string snippet = content.Length > maxChars ? content[..maxChars] : content;
        sb.AppendLine();
        sb.AppendLine(title + ":");
        sb.AppendLine(snippet);
    }

    private async Task<DashboardSnapshot> LoadDashboardSnapshotAsync(CancellationToken cancellationToken)
    {
        using var connection = DBConnection.GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return new DashboardSnapshot
        {
            TotalResidents = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0", cancellationToken).ConfigureAwait(false),
            ActiveResidents = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'ACTIVE'", cancellationToken).ConfigureAwait(false),
            Households = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM household", cancellationToken).ConfigureAwait(false),
            PendingCertificates = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED'", cancellationToken).ConfigureAwait(false),
            OngoingBlotter = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING'", cancellationToken).ConfigureAwait(false),
            ActiveUsers = await SafeScalarAsync(connection, "SELECT COUNT(*) FROM user_account WHERE is_active = 1", cancellationToken).ConfigureAwait(false),
        };
    }

    private static async Task<int> SafeScalarAsync(MySqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        try
        {
            using var command = new MySqlCommand(sql, connection);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result == null || result == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(result);
        }
        catch
        {
            return 0;
        }
    }

    private static string BuildPrompt(string question, string systemContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are Ellie, a local assistant inside a Barangay Management System (WinForms desktop app).");
        sb.AppendLine("Answer clearly and practically, based on the provided system context.");
        sb.AppendLine("Keep responses concise, actionable, and use numbered steps when user asks how-to.");
        sb.AppendLine("Do not invent database fields or app screens outside the context.");
        sb.AppendLine("If information is missing, say what is missing and suggest where to check in the app.");
        sb.AppendLine();
        sb.AppendLine("System context:");
        sb.AppendLine(systemContext);
        sb.AppendLine();
        sb.AppendLine("User question:");
        sb.AppendLine(question.Trim());
        sb.AppendLine();
        sb.AppendLine("Response:");
        return sb.ToString();
    }

    private static string BuildFallbackAnswer(string question, string reason)
    {
        return
            "I cannot reach the local AI right now (" + reason + "). " +
            "You can still use these modules directly: Dashboard, Residents, Blotter, Certificates, History, Reports, and Settings. " +
            "Question received: \"" + question.Trim() + "\".";
    }

    private static string? TryGetProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && current != null; i++)
        {
            if (File.Exists(Path.Combine(current.FullName, "baranggaysystem1.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private sealed class DashboardSnapshot
    {
        public int TotalResidents { get; init; }
        public int ActiveResidents { get; init; }
        public int Households { get; init; }
        public int PendingCertificates { get; init; }
        public int OngoingBlotter { get; init; }
        public int ActiveUsers { get; init; }
    }
}
