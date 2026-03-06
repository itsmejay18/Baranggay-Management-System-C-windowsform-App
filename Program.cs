namespace baranggaysystem1
{
    internal static class Program
    {
        private static int _uiExceptionDialogShown;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[]? args)
        {
            bool isUiTest = UiVisualTestRunner.IsUiTestRequested(args);
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
                        "\n\nTip: verify your MySQL credentials or set BARANGAY_DB_CONNECTION (use SslMode=Disabled).",
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

            Application.Run(new Form1());
        }
    }
}
