using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace baranggaysystem1
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = @"Local\BarangaySystem.SingleInstance";
        private const int SwRestore = 9;
        private const int SwShow = 5;
        private static int _uiExceptionDialogShown;

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[]? args)
        {
            using Mutex singleInstanceMutex = new(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                TryActivateRunningInstance();
                return;
            }

            bool isUiTest = UiVisualTestRunner.IsUiTestRequested(args);
            try
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                helper.AppLogger.Initialize();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (_, args) =>
                {
                    helper.AppLogger.LogError("Unhandled UI thread exception.", args.Exception);
                    if (isUiTest)
                    {
                        return;
                    }

                    if (System.Threading.Interlocked.Exchange(ref _uiExceptionDialogShown, 1) == 0)
                    {
                        MessageBox.Show("An unexpected error occurred. Please check the logs for details.",
                            "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    helper.AppLogger.LogWarning("Suppressed repeated UI exception dialog to prevent error loop.");
                };

                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                {
                    var ex = args.ExceptionObject as Exception;
                    helper.AppLogger.LogError("Unhandled non-UI exception.", ex);
                };
                if (!isUiTest)
                {
                    using var installer = new PackageInstallerForm();
                    DialogResult setupResult = installer.ShowDialog();
                    if (setupResult != DialogResult.OK)
                    {
                        return;
                    }

                    try
                    {
                        Database.SchemaGuard.EnsureDatabaseReady();

                        var health = Database.SchemaGuard.RunStartupHealthChecks();
                        string healthIssues = health.ToMultilineText(includeOk: false);
                        if (health.HasCriticalIssues)
                        {
                            helper.AppLogger.LogError("Startup health checks failed.\n" + healthIssues);
                            MessageBox.Show(
                                "Startup health checks found critical issues.\n\n" +
                                healthIssues +
                                "\n\nPlease resolve these issues before using the app.",
                                "Startup Health Check",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }

                        if (health.HasWarnings)
                        {
                            helper.AppLogger.LogWarning("Startup health checks completed with warnings.\n" + healthIssues);
                        }
                        else
                        {
                            helper.AppLogger.LogInfo("Startup health checks passed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        helper.AppLogger.LogError("Database setup failed during startup.", ex);
                        string details = ex.Message;
                        if (ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message))
                        {
                            details += "\n" + ex.InnerException.Message;
                        }

                        MessageBox.Show(
                            "Database setup failed.\n\n" +
                            details +
                            "\n\nTip: verify your DB connection in Package Installer or set BARANGAY_DB_CONNECTION (use SslMode=Disabled).",
                            "Database Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }

                if (isUiTest)
                {
                    int exitCode = UiVisualTestRunner.Run(args);
                    Environment.ExitCode = exitCode;
                    return;
                }

                Application.Run(new StartupApplicationContext());
            }
            finally
            {
                singleInstanceMutex.ReleaseMutex();
            }
        }

        private static void TryActivateRunningInstance()
        {
            try
            {
                using Process currentProcess = Process.GetCurrentProcess();
                Process? existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
                    .Where(process => process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    .OrderByDescending(SafeGetStartTime)
                    .FirstOrDefault();

                if (existingProcess == null)
                {
                    return;
                }

                IntPtr windowHandle = FindWindowHandle(existingProcess);
                if (windowHandle == IntPtr.Zero)
                {
                    return;
                }

                ShowWindowAsync(windowHandle, IsIconic(windowHandle) ? SwRestore : SwShow);
                if (!SetForegroundWindow(windowHandle))
                {
                    FlashWindow(windowHandle, true);
                }
            }
            catch
            {
                // Ignore duplicate-launch activation errors.
            }
        }

        private static DateTime SafeGetStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static IntPtr FindWindowHandle(Process process)
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            IntPtr foundHandle = IntPtr.Zero;
            EnumWindows((windowHandle, _) =>
            {
                GetWindowThreadProcessId(windowHandle, out uint windowProcessId);
                if (windowProcessId != process.Id || !IsWindowVisible(windowHandle))
                {
                    return true;
                }

                foundHandle = windowHandle;
                return false;
            }, IntPtr.Zero);

            return foundHandle;
        }

        private sealed class StartupApplicationContext : ApplicationContext
        {
            public StartupApplicationContext()
            {
                Form1 loginForm = CreateLoginForm();
                MainForm = loginForm;
                loginForm.Show();
            }

            private Form1 CreateLoginForm()
            {
                var loginForm = new Form1();
                loginForm.LoginSucceeded += destinationForm => SwitchMainForm(loginForm, destinationForm);
                loginForm.RegisterRequested += () => SwitchMainForm(loginForm, CreateRegisterForm());
                loginForm.FormClosed += ActiveFormClosed;
                return loginForm;
            }

            private RegisterForm CreateRegisterForm()
            {
                var registerForm = new RegisterForm();
                registerForm.BackToLoginRequested += () => SwitchMainForm(registerForm, CreateLoginForm());
                registerForm.RegistrationCompleted += () => SwitchMainForm(registerForm, CreateLoginForm());
                registerForm.FormClosed += RegisterFormClosed;
                return registerForm;
            }

            private void SwitchMainForm(Form currentForm, Form nextForm)
            {
                currentForm.FormClosed -= ActiveFormClosed;
                currentForm.FormClosed -= RegisterFormClosed;

                if (nextForm is not Form1 && nextForm is not RegisterForm)
                {
                    nextForm.FormClosed += ActiveFormClosed;
                }

                MainForm = nextForm;
                nextForm.Show();

                if (!currentForm.IsDisposed)
                {
                    currentForm.Close();
                }
            }

            private void ActiveFormClosed(object? sender, FormClosedEventArgs e)
            {
                if (ReferenceEquals(MainForm, sender))
                {
                    ExitThread();
                }
            }

            private void RegisterFormClosed(object? sender, FormClosedEventArgs e)
            {
                if (!ReferenceEquals(MainForm, sender))
                {
                    return;
                }

                Form1 loginForm = CreateLoginForm();
                MainForm = loginForm;
                loginForm.Show();
            }
        }
    }
}
