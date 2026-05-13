using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using baranggaysystem1.Database;
using baranggaysystem1.Views;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.helper;

namespace baranggaysystem1.Documentation;

internal static class UserManualCaptureRunner
{
	private sealed record RouteCapturePlan(string Route, string FileName, int DelayMs);

	private sealed record WindowCapturePlan(string FileName, Func<Window> Factory, double Width, double Height, int DelayMs);

	private const double MainWindowWidth = 1440.0;

	private const double MainWindowHeight = 900.0;

	private static readonly RouteCapturePlan[] MainRoutes = new RouteCapturePlan[16]
	{
		new RouteCapturePlan("Home", "home-landing", 1200),
		new RouteCapturePlan("DashboardNotifications", "dashboard", 1400),
		new RouteCapturePlan("ResidentWorkspace", "resident-records", 1200),
		new RouteCapturePlan("Households", "households", 1200),
		new RouteCapturePlan("ResidentCategories", "tags-categories", 1000),
		new RouteCapturePlan("DeceasedRegistry", "deceased-registry", 1000),
		new RouteCapturePlan("Clearances", "clearances", 1200),
		new RouteCapturePlan("Permits", "permits", 1000),
		new RouteCapturePlan("ResidentCases", "blotter-cases", 1200),
		new RouteCapturePlan("ResidentPayments", "payments", 1000),
		new RouteCapturePlan("Collections", "collections", 1000),
		new RouteCapturePlan("Reports", "reports", 1000),
		new RouteCapturePlan("Officials", "officials", 1000),
		new RouteCapturePlan("StaffUsers", "staff-users", 1200),
		new RouteCapturePlan("RolePermissions", "roles-permissions", 1000),
		new RouteCapturePlan("Settings", "system-settings", 900)
	};

	public static bool IsRequested(string[]? args)
	{
		if (args == null || args.Length == 0)
		{
			return false;
		}
		return args.Any((string arg) => string.Equals(arg, "--capture-user-manual", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "/capture-user-manual", StringComparison.OrdinalIgnoreCase));
	}

	public static async Task<int> RunAsync(string[]? args)
	{
		Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		string outputDirectory = ResolveOutputDirectory(args);
		Directory.CreateDirectory(outputDirectory);
		InitializeOfflineData();
		SeedSessionFromOfflineData();
		List<string> warnings = new List<string>();
		await CaptureWindowAsync(() => new LoginWindow(), "login-window", outputDirectory, 1440.0, 900.0, 1200, warnings);
		await CaptureWindowAsync(() => new RegisterWindow(), "register-window", outputDirectory, 1440.0, 900.0, 1400, warnings);
		MainWindow mainWindow = null;
		try
		{
			mainWindow = new MainWindow();
			PrepareWindow(mainWindow, 1440.0, 900.0);
			Application.Current.MainWindow = mainWindow;
			mainWindow.Show();
			await WaitForWindowAsync(mainWindow, 1400);
			RouteCapturePlan[] mainRoutes = MainRoutes;
			foreach (RouteCapturePlan route in mainRoutes)
			{
				mainWindow.NavigatePage(route.Route);
				await WaitForWindowAsync(mainWindow, route.DelayMs);
				SaveWindowContent(mainWindow, Path.Combine(outputDirectory, route.FileName + ".png"));
			}
			foreach (WindowCapturePlan item in BuildDialogPlans())
			{
				await CaptureWindowAsync(item.Factory, item.FileName, outputDirectory, item.Width, item.Height, item.DelayMs, warnings, mainWindow);
			}
		}
		catch (Exception ex)
		{
			warnings.Add("Main capture flow failed: " + ex.Message);
			AppLogger.LogError("User manual capture failed.", ex);
		}
		finally
		{
			try
			{
				if (mainWindow != null && !mainWindow.IsClosed())
				{
					mainWindow.Close();
				}
			}
			catch
			{
			}
		}
		WriteManifest(outputDirectory, warnings);
		return (warnings.Count == 0) ? 0 : 0;
	}

	private static IReadOnlyList<WindowCapturePlan> BuildDialogPlans()
	{
		int targetUserId = ((UserSession.UserId <= 0) ? 1 : UserSession.UserId);
		string targetUsername = (string.IsNullOrWhiteSpace(UserSession.Username) ? "admin" : UserSession.Username);
		return new WindowCapturePlan[13]
		{
			new WindowCapturePlan("resident-details", () => new ResidentDetailsWindow(), 980.0, 760.0, 800),
			new WindowCapturePlan("household-details", () => new HouseholdDetailsWindow(null), 1000.0, 760.0, 800),
			new WindowCapturePlan("certification-window", () => new CertificationWindow(0, "Sample Resident"), 860.0, 640.0, 700),
			new WindowCapturePlan("blotter-details", () => new BlotterDetailsWindow(), 1120.0, 820.0, 900),
			new WindowCapturePlan("announcement-window", () => new AnnouncementWindow(), 760.0, 560.0, 650),
			new WindowCapturePlan("project-window", () => new ProjectWindow(), 760.0, 560.0, 650),
			new WindowCapturePlan("official-details", () => new OfficialDetailsWindow(), 860.0, 640.0, 650),
			new WindowCapturePlan("staff-details", () => new StaffDetailsWindow(), 980.0, 760.0, 800),
			new WindowCapturePlan("update-user", () => new UpdateUserWindow(targetUserId, targetUsername), 720.0, 520.0, 600),
			new WindowCapturePlan("global-search", () => new GlobalSearchWindow(), 640.0, 500.0, 550),
			new WindowCapturePlan("ellie-assistant", () => new EllieAssistantWindow(), 520.0, 600.0, 550),
			new WindowCapturePlan("password-dialog", () => new PasswordDialog(), 460.0, 320.0, 500),
			new WindowCapturePlan("package-installer", () => new PackageInstallerWindow(), 640.0, 480.0, 550)
		};
	}

	private static void InitializeOfflineData()
	{
		try
		{
			if (OfflineDatabaseSupport.EnsureInitialised())
			{
				OfflineDatabaseSupport.ActivateOfflineMode();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("User manual capture could not initialize offline data.", ex);
		}
	}

	private static void SeedSessionFromOfflineData()
	{
		UserSession.UserId = 1;
		UserSession.BarangayId = 1;
		UserSession.Username = "admin";
		UserSession.Role = "Super Admin";
		try
		{
			SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
			try
			{
				SqliteCommand val = connection.CreateCommand();
				try
				{
					((DbCommand)(object)val).CommandText = "\n                SELECT user_id, IFNULL(barangay_id, 1) AS barangay_id, username\n                FROM user_account\n                WHERE IFNULL(is_active, 1) = 1\n                ORDER BY user_id\n                LIMIT 1;";
					SqliteDataReader val2 = val.ExecuteReader();
					try
					{
						if (((DbDataReader)(object)val2).Read())
						{
							UserSession.UserId = ((((DbDataReader)(object)val2)["user_id"] == DBNull.Value) ? 1 : Convert.ToInt32(((DbDataReader)(object)val2)["user_id"]));
							UserSession.BarangayId = ((((DbDataReader)(object)val2)["barangay_id"] == DBNull.Value) ? 1 : Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]));
							UserSession.Username = Convert.ToString(((DbDataReader)(object)val2)["username"]) ?? "admin";
						}
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)connection)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("User manual capture is using default session values.", ex);
		}
		Permissions.Refresh();
	}

	private static async Task CaptureWindowAsync(Func<Window> factory, string fileName, string outputDirectory, double width, double height, int settleDelayMs, List<string> warnings, Window? owner = null)
	{
		Window window = null;
		try
		{
			window = factory();
			if (owner != null)
			{
				window.Owner = owner;
			}
			PrepareWindow(window, width, height);
			window.Show();
			await WaitForWindowAsync(window, settleDelayMs);
			SaveWindowContent(window, Path.Combine(outputDirectory, fileName + ".png"));
		}
		catch (Exception ex)
		{
			warnings.Add("Failed to capture '" + fileName + "': " + ex.Message);
			AppLogger.LogError("User manual capture failed for '" + fileName + "'.", ex);
		}
		finally
		{
			try
			{
				if (window != null && !window.IsClosed())
				{
					window.Close();
				}
			}
			catch
			{
			}
		}
	}

	private static void PrepareWindow(Window window, double width, double height)
	{
		window.WindowStartupLocation = WindowStartupLocation.Manual;
		window.WindowState = WindowState.Normal;
		window.ShowInTaskbar = false;
		window.Width = width;
		window.Height = height;
		window.Left = -32000.0;
		window.Top = 0.0;
		window.UpdateLayout();
	}

	private static async Task WaitForWindowAsync(Window window, int settleDelayMs)
	{
		if (!window.IsLoaded)
		{
			TaskCompletionSource<object?> tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
			RoutedEventHandler loadedHandler = null;
			loadedHandler = delegate
			{
				window.Loaded -= loadedHandler;
				tcs.TrySetResult(null);
			};
			window.Loaded += loadedHandler;
			await tcs.Task;
		}
		for (int i = 0; i < 3; i++)
		{
			await WaitForDispatcherAsync();
			await Task.Delay(120);
		}
		if (settleDelayMs > 0)
		{
			await Task.Delay(settleDelayMs);
		}
		await WaitForDispatcherAsync();
		window.UpdateLayout();
		(window.Content as FrameworkElement)?.UpdateLayout();
	}

	private static Task WaitForDispatcherAsync()
	{
		return ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
		{
		}, (DispatcherPriority)2).Task;
	}

	private static void SaveWindowContent(Window window, string outputPath)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		if (!(window.Content is FrameworkElement frameworkElement))
		{
			throw new InvalidOperationException("Window content is not a FrameworkElement.");
		}
		frameworkElement.UpdateLayout();
		double num = ((frameworkElement.ActualWidth > 0.0) ? frameworkElement.ActualWidth : window.ActualWidth);
		double num2 = ((frameworkElement.ActualHeight > 0.0) ? frameworkElement.ActualHeight : window.ActualHeight);
		if (num <= 0.0 || num2 <= 0.0)
		{
			num = Math.Max(window.Width, 640.0);
			num2 = Math.Max(window.Height, 480.0);
			frameworkElement.Measure(new Size(num, num2));
			frameworkElement.Arrange(new Rect(0.0, 0.0, num, num2));
			frameworkElement.UpdateLayout();
		}
		DpiScale dpi = VisualTreeHelper.GetDpi(frameworkElement);
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(num * dpi.DpiScaleX), (int)Math.Ceiling(num2 * dpi.DpiScaleY), 96.0 * dpi.DpiScaleX, 96.0 * dpi.DpiScaleY, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(frameworkElement);
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
		using FileStream stream = File.Create(outputPath);
		pngBitmapEncoder.Save(stream);
	}

	private static string ResolveOutputDirectory(string[]? args)
	{
		string text = TryGetOptionValue(args, "--manual-output");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return Path.GetFullPath(text);
		}
		string text2 = FindRepositoryRoot();
		string text3 = DateTime.Now.ToString("yyyyMMdd-HHmmss");
		return Path.Combine(text2, "docs", "user-manual", "screenshots", text3);
	}

	private static string? TryGetOptionValue(string[]? args, string optionName)
	{
		if (args == null || args.Length == 0)
		{
			return null;
		}
		for (int i = 0; i < args.Length; i++)
		{
			if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				return args[i + 1];
			}
		}
		return null;
	}

	private static string FindRepositoryRoot()
	{
		for (DirectoryInfo directoryInfo = new DirectoryInfo(AppContext.BaseDirectory); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (File.Exists(Path.Combine(directoryInfo.FullName, "baranggaysystem1.sln")))
			{
				return directoryInfo.FullName;
			}
		}
		return Directory.GetCurrentDirectory();
	}

	private static void WriteManifest(string outputDirectory, IReadOnlyList<string> warnings)
	{
		List<string> list = new List<string>
		{
			$"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
			"Output: " + outputDirectory,
			$"Screenshots: {Directory.GetFiles(outputDirectory, "*.png").Length}",
			string.Empty,
			"Files:"
		};
		list.AddRange(Directory.GetFiles(outputDirectory, "*.png").Select(Path.GetFileName).OrderBy<string, string>((string name) => name, StringComparer.OrdinalIgnoreCase));
		if (warnings.Count > 0)
		{
			list.Add(string.Empty);
			list.Add("Warnings:");
			list.AddRange(warnings);
		}
		File.WriteAllLines(Path.Combine(outputDirectory, "manifest.txt"), list);
	}
}
