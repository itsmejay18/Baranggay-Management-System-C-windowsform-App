using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using baranggaysystem1.Database;
using baranggaysystem1.Documentation;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.Views;

namespace baranggaysystem1;

public partial class App : Application
{
	private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static DispatcherUnhandledExceptionEventHandler _003C_003E9__12_1;

		public static UnhandledExceptionEventHandler _003C_003E9__12_2;

		public static Action _003C_003E9__14_0;

		internal void _003COnStartup_003Eb__12_1(object _, DispatcherUnhandledExceptionEventArgs args)
		{
			AppLogger.LogError("Unhandled UI thread exception.", args.Exception);
			MessageBox.Show("An unexpected error occurred. Please check the logs for details.", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			args.Handled = true;
		}

		internal void _003COnStartup_003Eb__12_2(object _, UnhandledExceptionEventArgs args)
		{
			AppLogger.LogError("Unhandled non-UI exception.", args.ExceptionObject as Exception);
		}

		internal void _003CQueueDatabaseStartupInitialization_003Eb__14_0()
		{
			bool flag = false;
			try
			{
				flag = OfflineDatabaseSupport.EnsureInitialised();
				if (DbConnectionSettingsStore.IsSqliteSelected())
				{
					if (!flag)
					{
						throw new InvalidOperationException("The SQLite database file could not be initialised during startup.");
					}
					DBConnection.SetRuntimeSqliteSelection(isSelected: true);
					OfflineDatabaseSupport.ActivateOfflineMode();
					SystemConfigService.EnsureTable();
					AppLogger.LogInfo("SQLite profile selected. Using local database at " + OfflineDatabaseSupport.GetDatabasePath() + ".");
					return;
				}
				SchemaGuard.EnsureDatabaseReady();
				DBConnection.SetRuntimeSqliteSelection(isSelected: false);
				OfflineDatabaseSupport.ActivateOnlineMode();
				SystemConfigService.EnsureTable();
				int num = OfflineSyncService.TrySyncPendingChanges();
				if (num > 0)
				{
					AppLogger.LogInfo($"[OfflineSync] Replayed {num} queued change(s) to online database.");
				}
				StartupHealthReport startupHealthReport = SchemaGuard.RunStartupHealthChecks();
				string text = startupHealthReport.ToMultilineText(includeOk: false);
				if (startupHealthReport.HasCriticalIssues)
				{
					AppLogger.LogError("Startup health checks failed.\n" + text);
				}
				else if (startupHealthReport.HasWarnings)
				{
					AppLogger.LogWarning("Startup health checks completed with warnings.\n" + text);
				}
				else
				{
					AppLogger.LogInfo("Startup health checks passed.");
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError("Database setup failed during startup.", ex);
				if (flag || OfflineDatabaseSupport.IsAvailable)
				{
					OfflineDatabaseSupport.ActivateOfflineMode();
					AppLogger.LogWarning("Falling back to offline mode.");
				}
			}
		}
	}

	private const string SingleInstanceMutexName = "Local\\BarangaySystem.SingleInstance";

	private const int SwRestore = 9;

	private const int SwShow = 5;

	private Mutex? _singleInstanceMutex;

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindowAsync(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool FlashWindow(nint hWnd, bool bInvert);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hWnd);

	protected override void OnStartup(StartupEventArgs e)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		StartupEventArgs e2 = e;
		if (UserManualCaptureRunner.IsRequested(e2.Args))
		{
			base.OnStartup(e2);
			AppLogger.Initialize();
			((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)async delegate
			{
				int exitCode = await UserManualCaptureRunner.RunAsync(e2.Args);
				Shutdown(exitCode);
			}, (DispatcherPriority)2, Array.Empty<object>());
			return;
		}
		_singleInstanceMutex = new Mutex(initiallyOwned: true, "Local\\BarangaySystem.SingleInstance", out var createdNew);
		if (!createdNew)
		{
			bool flag = false;
			try
			{
				flag = _singleInstanceMutex.WaitOne(500, exitContext: false);
			}
			catch
			{
				flag = true;
			}
			if (!flag)
			{
				TryActivateRunningInstance();
				_singleInstanceMutex.Dispose();
				Shutdown();
				return;
			}
		}
		base.OnStartup(e2);
		AppLogger.Initialize();
		object obj2 = _003C_003Ec._003C_003E9__12_1;
		if (obj2 == null)
		{
			DispatcherUnhandledExceptionEventHandler val = delegate(object _, DispatcherUnhandledExceptionEventArgs args)
			{
				AppLogger.LogError("Unhandled UI thread exception.", args.Exception);
				MessageBox.Show("An unexpected error occurred. Please check the logs for details.", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				args.Handled = true;
			};
			_003C_003Ec._003C_003E9__12_1 = val;
			obj2 = (object)val;
		}
		base.DispatcherUnhandledException += (DispatcherUnhandledExceptionEventHandler)obj2;
		AppDomain.CurrentDomain.UnhandledException += delegate(object _, UnhandledExceptionEventArgs args)
		{
			AppLogger.LogError("Unhandled non-UI exception.", args.ExceptionObject as Exception);
		};
		QueueDatabaseStartupInitialization();
		LoginWindow loginWindow = (LoginWindow)(base.MainWindow = new LoginWindow());
		loginWindow.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		if (_singleInstanceMutex != null)
		{
			try
			{
				_singleInstanceMutex.ReleaseMutex();
			}
			catch
			{
			}
			try
			{
				_singleInstanceMutex.Dispose();
			}
			catch
			{
			}
		}
		base.OnExit(e);
	}

	private static void QueueDatabaseStartupInitialization()
	{
		Task.Run(delegate
		{
			bool flag = false;
			try
			{
				flag = OfflineDatabaseSupport.EnsureInitialised();
				if (DbConnectionSettingsStore.IsSqliteSelected())
				{
					if (!flag)
					{
						throw new InvalidOperationException("The SQLite database file could not be initialised during startup.");
					}
					DBConnection.SetRuntimeSqliteSelection(isSelected: true);
					OfflineDatabaseSupport.ActivateOfflineMode();
					SystemConfigService.EnsureTable();
					AppLogger.LogInfo("SQLite profile selected. Using local database at " + OfflineDatabaseSupport.GetDatabasePath() + ".");
				}
				else
				{
					// Auto-create database if it doesn't exist
					if (DbConnectionSettingsStore.TryLoad(out DatabaseConnectionProfile profile))
					{
						string connStr = DbConnectionSettingsStore.BuildConnectionString(profile);
						DatabaseAutoCreator.TryEnsureReady(connStr, profile);
					}

					SchemaGuard.EnsureDatabaseReady();
					DBConnection.SetRuntimeSqliteSelection(isSelected: false);
					OfflineDatabaseSupport.ActivateOnlineMode();
					SystemConfigService.EnsureTable();
					int num = OfflineSyncService.TrySyncPendingChanges();
					if (num > 0)
					{
						AppLogger.LogInfo($"[OfflineSync] Replayed {num} queued change(s) to online database.");
					}
					StartupHealthReport startupHealthReport = SchemaGuard.RunStartupHealthChecks();
					string text = startupHealthReport.ToMultilineText(includeOk: false);
					if (startupHealthReport.HasCriticalIssues)
					{
						AppLogger.LogError("Startup health checks failed.\n" + text);
					}
					else if (startupHealthReport.HasWarnings)
					{
						AppLogger.LogWarning("Startup health checks completed with warnings.\n" + text);
					}
					else
					{
						AppLogger.LogInfo("Startup health checks passed.");
					}
				}
			}
			catch (Exception ex)
			{
				AppLogger.LogError("Database setup failed during startup.", ex);
				if (flag || OfflineDatabaseSupport.IsAvailable)
				{
					OfflineDatabaseSupport.ActivateOfflineMode();
					AppLogger.LogWarning("Falling back to offline mode.");
				}
			}
		});
	}

	private static void TryActivateRunningInstance()
	{
		try
		{
			using Process process2 = Process.GetCurrentProcess();
			Process process = null;
			Process[] processesByName = Process.GetProcessesByName(process2.ProcessName);
			foreach (Process process3 in processesByName)
			{
				if (process3.Id != process2.Id)
				{
					process = process3;
					break;
				}
			}
			if (process == null)
			{
				return;
			}
			nint num = FindWindowHandle(process);
			if (num != IntPtr.Zero)
			{
				ShowWindowAsync(num, IsIconic(num) ? 9 : 5);
				if (!SetForegroundWindow(num))
				{
					FlashWindow(num, bInvert: true);
				}
			}
		}
		catch
		{
		}
	}

	private static nint FindWindowHandle(Process process)
	{
		Process process2 = process;
		if (process2.MainWindowHandle != IntPtr.Zero)
		{
			return process2.MainWindowHandle;
		}
		nint found = IntPtr.Zero;
		EnumWindows(delegate(nint hWnd, nint _)
		{
			GetWindowThreadProcessId(hWnd, out var processId);
			if (processId == (uint)process2.Id && IsWindowVisible(hWnd))
			{
				found = hWnd;
				return false;
			}
			return true;
		}, IntPtr.Zero);
		return found;
	}}
