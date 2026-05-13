using System;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class AdminDashboard
    {
        private sealed class AdminDashboardController
        {
            private readonly AdminDashboard _form;
            private bool _featuresInitialized;
            private bool _kpiDrillDownWired;
            private bool _backupInProgress;
            private bool _featureSchemasEnsured;
            private readonly SemaphoreSlim _featureSchemaGate = new SemaphoreSlim(1, 1);

            public AdminDashboardController(AdminDashboard form)
            {
                _form = form;
            }

            public async Task LoadDashboardStatsAsync()
            {
                InitializeFeatures();
                await EnsureFeatureSchemasInitializedAsync().ConfigureAwait(true);

                // Show loading state
                _form.SetDashboardLoading(true);

                try
                {
                    // Parallelize metric queries to reduce total load time
                    var totalResidentsTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0");
                    var activeResidentsTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'ACTIVE'");
                    var householdsTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM household");
                    var pendingCertificatesTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED'");
                    var ongoingBlotterTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING'");

                    // Wait for all metrics to complete in parallel, but keep UI responsive on DB stalls.
                    Task metricsTask = Task.WhenAll(totalResidentsTask, activeResidentsTask, householdsTask, pendingCertificatesTask, ongoingBlotterTask);
                    Task completed = await Task.WhenAny(metricsTask, Task.Delay(TimeSpan.FromSeconds(8))).ConfigureAwait(true);
                    if (!ReferenceEquals(completed, metricsTask))
                    {
                        AppLogger.LogWarning("Dashboard stats timed out. Rendering fallback values to keep UI responsive.");
                        _form.BeginInvoke(new Action(() => _form.SetDashboardStats(0, 0, 0, 0, 0)));
                        return;
                    }

                    int totalResidents = await totalResidentsTask;
                    int activeResidents = await activeResidentsTask;
                    int households = await householdsTask;
                    int pendingCertificates = await pendingCertificatesTask;
                    int ongoingBlotter = await ongoingBlotterTask;

                    // Update UI on main thread
                    _form.BeginInvoke(new Action(() =>
                    {
                        _form.SetDashboardStats(
                            totalResidents,
                            activeResidents,
                            households,
                            pendingCertificates,
                            ongoingBlotter);
                    }));
                }
                catch (Exception ex)
                {
                    // Log error and show fallback values
                    AppLogger.LogError($"Failed to load dashboard stats: {ex.Message}");

                    // Update UI with zeros on error
                    _form.BeginInvoke(new Action(() =>
                    {
                        _form.SetDashboardStats(0, 0, 0, 0, 0);
                    }));
                }
                finally
                {
                    // Hide loading state
                    _form.BeginInvoke(new Action(() => _form.SetDashboardLoading(false)));
                }

                // Load other dashboard components asynchronously (fire-and-forget with error handling)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.WhenAll(
                            LoadDashboardTrendsAsync(),
                            LoadOfficialsAsync(),
                            LoadAnnouncementsAsync(),
                            LoadProjectsAsync(),
                            LoadActionCenterAsync(),
                            LoadNotificationsAsync(),
                            LoadBackupStatusAsync(),
                            LoadSchemaVersionAsync()
                        );
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Failed to load dashboard components: {ex.Message}");
                    }
                });
            }

            public void RefreshNotifications()
            {
                _ = LoadNotificationsAsync();
            }

            public void RefreshActionCenter()
            {
                _ = LoadActionCenterAsync();
            }

            public void RefreshBackupStatus()
            {
                _ = LoadBackupStatusAsync();
            }

            public void RunNotificationAutomation(bool includeReminderQueue)
            {
                if (!helper.Permissions.CanDispatchNotifications)
                {
                    return;
                }

                System.Threading.Tasks.Task.Run(() =>
                {
                    OutboundNotificationService.TryRunScheduledAutomation(includeReminderQueue, maxDispatch: 20);
                });
            }

            public void RunBackupNow(BackupMode mode)
            {
                StartBackup(showDialogs: true, onlyIfDue: false, mode);
            }

            public void TryRunScheduledBackup()
            {
                StartBackup(showDialogs: false, onlyIfDue: true, BackupMode.Incremental);
            }

            private void InitializeFeatures()
            {
                if (_featuresInitialized)
                {
                    return;
                }

                _featuresInitialized = true;
                _form.announcementsNew.Click += (_, __) => HandleNewAnnouncement();
                _form.announcementsRefresh.Click += (_, __) => LoadAnnouncements();
                _form.projectsNew.Click += (_, __) => HandleNewProject();
                _form.projectsRefresh.Click += (_, __) => LoadProjects();
                _form.notificationViewAll.Click += (_, __) => HandleMarkAllNotificationsRead();
                WireKpiDrillDown();
            }

            private void LoadBackupStatus()
            {
                var info = BackupService.TryGetLatestRun();
                if (info?.State == BackupRunState.Running && !_backupInProgress)
                {
                    info = BackupService.MarkInterruptedRunAsFailed(
                        info,
                        "Backup was left in a running state after the app closed.");
                }

                _form.SetBackupStatus(info);
            }
 
            private void LoadSchemaVersion()
            {
                string? version = Database.MigrationRunner.TryGetCurrentSchemaVersion();
                _form.SetSchemaVersion(version);
            }

            private void StartBackup(bool showDialogs, bool onlyIfDue, BackupMode mode)
            {
                if (_backupInProgress)
                {
                    return;
                }

                DateTime now = DateTime.Now;
                if (onlyIfDue)
                {
                    DateTime? lastOk = BackupService.TryGetLastSuccessfulBackupAt();
                    if (lastOk.HasValue && lastOk.Value.Date == now.Date)
                    {
                        return;
                    }
                }

                _backupInProgress = true;
                _form.SetBackupStatus(new BackupRunInfo(null, now, null, BackupRunState.Running, null, null, null, mode));

                int? triggeredByUserId = null;
                try
                {
                    triggeredByUserId = helper.UserSession.UserId;
                }
                catch
                {
                    triggeredByUserId = null;
                }

                System.Threading.Tasks.Task.Run(() =>
                {
                    BackupRunInfo result;
                    try
                    {
                        result = BackupService.RunBackupNow(triggeredByUserId, compressToZip: true, mode: mode);
                    }
                    catch (Exception ex)
                    {
                        result = new BackupRunInfo(null, now, DateTime.Now, BackupRunState.Failed, null, null, ex.Message, mode);
                    }

                    try
                    {
                        _form.BeginInvoke(new Action(() =>
                        {
                            _form.SetBackupStatus(result);

                            if (!showDialogs)
                            {
                                return;
                            }

                            if (result.State == BackupRunState.Success && !string.IsNullOrWhiteSpace(result.FilePath))
                            {
                                string modeText = result.Mode switch
                                {
                                    BackupMode.Incremental => "Incremental",
                                    BackupMode.Differential => "Differential",
                                    _ => "Full"
                                };
                                var open = ControllerDialogs.Confirm(
                                    $"{modeText} backup created successfully.\n\nOpen backups folder?",
                                    "Backup");
                                if (open == DialogResult.Yes)
                                {
                                    BackupService.OpenBackupFolder();
                                }
                            }
                            else if (result.State == BackupRunState.Failed)
                            {
                                ControllerDialogs.Error(result.ErrorMessage ?? "Backup failed.", "Backup");
                            }
                        }));
                    }
                    catch
                    {
                        // Ignore UI update failures.
                    }
                    finally
                    {
                        _backupInProgress = false;
                    }
                });
            }

            private void EnsureAnnouncementsSchema()
            {
                DbHelper.ExecuteNonQuery(
                    @"CREATE TABLE IF NOT EXISTS announcements (
                        announcement_id INT AUTO_INCREMENT PRIMARY KEY,
                        title VARCHAR(150) NOT NULL,
                        body TEXT,
                        priority ENUM('Low','Normal','High') DEFAULT 'Normal',
                        status ENUM('Draft','Published','Archived') DEFAULT 'Published',
                        is_pinned TINYINT(1) DEFAULT 0,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                      )");
            }

            private void EnsureAnnouncementUserStateSchema()
            {
                DbHelper.ExecuteNonQuery(
                    @"CREATE TABLE IF NOT EXISTS announcement_user_state (
                        user_id INT NOT NULL,
                        announcement_id INT NOT NULL,
                        state ENUM('NEW','READ','ARCHIVED') NOT NULL DEFAULT 'NEW',
                        read_at DATETIME NULL,
                        archived_at DATETIME NULL,
                        updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        PRIMARY KEY (user_id, announcement_id),
                        INDEX idx_announcement_state (announcement_id, state),
                        INDEX idx_user_state (user_id, state)
                      )");
            }

            private void EnsureProjectsSchema()
            {
                DbHelper.ExecuteNonQuery(
                    @"CREATE TABLE IF NOT EXISTS projects (
                        project_id INT AUTO_INCREMENT PRIMARY KEY,
                        name VARCHAR(150) NOT NULL,
                        status ENUM('Planned','Ongoing','On hold','Completed') DEFAULT 'Planned',
                        budget DECIMAL(12,2) DEFAULT 0,
                        start_date DATE NULL,
                        end_date DATE NULL,
                        `lead` VARCHAR(100),
                        remarks TEXT,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                      )");
            }

            private async Task EnsureFeatureSchemasInitializedAsync()
            {
                if (_featureSchemasEnsured)
                {
                    return;
                }

                await _featureSchemaGate.WaitAsync().ConfigureAwait(true);
                try
                {
                    if (_featureSchemasEnsured)
                    {
                        return;
                    }

                    await Task.Run(() =>
                    {
                        EnsureAnnouncementsSchema();
                        EnsureAnnouncementUserStateSchema();
                        EnsureProjectsSchema();
                    }).ConfigureAwait(true);

                    _featureSchemasEnsured = true;
                }
                finally
                {
                    _featureSchemaGate.Release();
                }
            }

            private void LoadAnnouncements()
            {
                _form.ShowAnnouncementsLoading();

                var table = DbHelper.LoadTable(
                    @"SELECT a.announcement_id,
                             a.title,
                             a.body,
                             a.priority,
                             a.status,
                             DATE_FORMAT(a.created_at, '%b %d, %Y') AS published,
                             COALESCE(au.state, 'NEW') AS user_state
                      FROM announcements a
                      LEFT JOIN announcement_user_state au
                        ON au.announcement_id = a.announcement_id
                       AND au.user_id = @uid
                      WHERE COALESCE(au.state, 'NEW') <> 'ARCHIVED'
                      ORDER BY a.created_at DESC
                      LIMIT 8",
                    cmd => cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId));
                _form.SetAnnouncements(table);
            }

            private void LoadNotifications()
            {
                var table = DbHelper.LoadTable(
                    @"SELECT a.announcement_id,
                             a.title,
                             a.body,
                             a.priority,
                             a.status,
                             DATE_FORMAT(a.created_at, '%b %d, %Y') AS published,
                             COALESCE(au.state, 'NEW') AS user_state
                      FROM announcements a
                      LEFT JOIN announcement_user_state au
                        ON au.announcement_id = a.announcement_id
                       AND au.user_id = @uid
                      WHERE a.status = 'Published'
                        AND COALESCE(au.state, 'NEW') = 'NEW'
                      ORDER BY a.created_at DESC
                      LIMIT 10",
                    cmd => cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId));
                _form.SetNotifications(table);
            }

            private void LoadProjects()
            {
                _form.ShowProjectsLoading();

                var table = DbHelper.LoadTable(
                    @"SELECT project_id,
                             name,
                             status,
                             budget,
                             `lead`,
                             remarks,
                             DATE_FORMAT(start_date, '%b %d, %Y') AS start_date,
                             DATE_FORMAT(end_date, '%b %d, %Y') AS end_date
                      FROM projects
                      ORDER BY created_at DESC
                      LIMIT 8");
                _form.SetProjects(table);
            }

            private void HandleNewAnnouncement()
            {
                if (!helper.Permissions.CanManageAnnouncements)
                {
                    ControllerDialogs.Warning("Only Admin users can manage announcements.");
                    return;
                }

                using var form = new AnnouncementForm();
                if (form.ShowDialog(_form) == System.Windows.Forms.DialogResult.OK)
                {
                    LoadAnnouncements();
                    Try(LoadNotifications);
                }
            }

            public void HandleAnnouncementViewed(int announcementId)
            {
                if (announcementId <= 0)
                {
                    return;
                }

                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO announcement_user_state (user_id, announcement_id, state, read_at, archived_at)
                      VALUES (@uid, @aid, 'READ', NOW(), NULL)
                      ON DUPLICATE KEY UPDATE state='READ', read_at=COALESCE(read_at, NOW()), archived_at=NULL",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId);
                        cmd.Parameters.AddWithValue("@aid", announcementId);
                    });

                Try(LoadAnnouncements);
                Try(LoadNotifications);
            }

            public void HandleAnnouncementArchive(int announcementId)
            {
                if (announcementId <= 0)
                {
                    return;
                }

                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO announcement_user_state (user_id, announcement_id, state, archived_at)
                      VALUES (@uid, @aid, 'ARCHIVED', NOW())
                      ON DUPLICATE KEY UPDATE state='ARCHIVED', archived_at=COALESCE(archived_at, NOW())",
                    cmd =>
                    {
                        cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId);
                        cmd.Parameters.AddWithValue("@aid", announcementId);
                    });

                Try(LoadAnnouncements);
                Try(LoadNotifications);
            }

            public void HandleMarkAllNotificationsRead()
            {
                var confirm = ControllerDialogs.Confirm("Mark all notifications as read?", "Confirm");
                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                DbHelper.ExecuteNonQuery(
                    @"INSERT INTO announcement_user_state (user_id, announcement_id, state, read_at)
                      SELECT @uid, a.announcement_id, 'READ', NOW()
                      FROM announcements a
                      LEFT JOIN announcement_user_state au
                        ON au.announcement_id = a.announcement_id
                       AND au.user_id = @uid
                      WHERE a.status = 'Published'
                        AND COALESCE(au.state, 'NEW') <> 'ARCHIVED'
                      ON DUPLICATE KEY UPDATE state='READ', read_at=COALESCE(read_at, NOW()), archived_at=NULL",
                    cmd => cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId));

                Try(LoadNotifications);
                Try(LoadAnnouncements);
            }

            private void HandleNewProject()
            {
                if (!helper.Permissions.CanManageProjects)
                {
                    ControllerDialogs.Warning("Only Admin users can manage projects.");
                    return;
                }

                using var form = new ProjectForm();
                if (form.ShowDialog(_form) == System.Windows.Forms.DialogResult.OK)
                {
                    LoadProjects();
                }
            }

            private void LoadActionCenter()
            {
                var table = new DataTable();
                table.Columns.Add("module", typeof(string));
                table.Columns.Add("alert", typeof(string));
                table.Columns.Add("count", typeof(int));
                table.Columns.Add("target_view", typeof(string));
                table.Columns.Add("priority_level", typeof(int));

                // Reminders: SLA-driven due soon + overdue items.
                AddActionRow(
                    table,
                    "Certificates",
                    "Overdue approvals",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM document_request\r\n                                WHERE status = 'SUBMITTED'\r\n                                  AND requested_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)"),
                    "Certificates",
                    0);

                AddActionRow(
                    table,
                    "Certificates",
                    "Overdue pickups",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM document_request\r\n                                WHERE status = 'APPROVED'\r\n                                  AND approved_at IS NOT NULL\r\n                                  AND approved_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY)"),
                    "Certificates",
                    0);

                AddActionRow(
                    table,
                    "Blotter",
                    "Overdue cases",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM case_record\r\n                                WHERE status IN ('OPEN','ONGOING')\r\n                                  AND created_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)"),
                    "Blotter",
                    0);

                AddActionRow(
                    table,
                    "Certificates",
                    "Approvals due soon",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM document_request\r\n                                WHERE status = 'SUBMITTED'\r\n                                  AND requested_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)\r\n                                  AND requested_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)"),
                    "Certificates",
                    1);

                AddActionRow(
                    table,
                    "Certificates",
                    "Pickups due soon",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM document_request\r\n                                WHERE status = 'APPROVED'\r\n                                  AND approved_at IS NOT NULL\r\n                                  AND approved_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY)\r\n                                  AND approved_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)"),
                    "Certificates",
                    1);

                AddActionRow(
                    table,
                    "Certificates",
                    "Clearances expiring soon",
                    SafeScalar(@"SELECT COUNT(*)
                                 FROM document_request dr
                                 INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                                 WHERE dr.status = 'RELEASED'
                                   AND dr.expires_at IS NOT NULL
                                   AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                                   AND DATE(dr.expires_at) >= CURDATE()
                                   AND DATE(dr.expires_at) <= DATE_ADD(CURDATE(), INTERVAL IFNULL(dt.renewal_reminder_days, 30) DAY)"),
                    "Certificates",
                    1);

                AddActionRow(
                    table,
                    "Certificates",
                    "Expired clearances",
                    SafeScalar(@"SELECT COUNT(*)
                                 FROM document_request dr
                                 INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                                 WHERE dr.status = 'RELEASED'
                                   AND dr.expires_at IS NOT NULL
                                   AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                                   AND DATE(dr.expires_at) < CURDATE()"),
                    "Certificates",
                    0);

                AddActionRow(
                    table,
                    "Blotter",
                    "Cases due soon",
                    SafeScalar($"SELECT COUNT(*)\r\n                                FROM case_record\r\n                                WHERE status IN ('OPEN','ONGOING')\r\n                                  AND created_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)\r\n                                  AND created_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY), INTERVAL {SlaRules.BlotterDueSoonDays + 1} DAY)"),
                    "Blotter",
                    1);

                // Hearing reminders (optional; depends on case_hearing usage).
                AddActionRow(
                    table,
                    "Blotter",
                    "Missed hearings",
                    SafeScalar("SELECT COUNT(*) FROM case_hearing WHERE status = 'SCHEDULED' AND schedule_at < CURDATE()"),
                    "Blotter",
                    0);

                AddActionRow(
                    table,
                    "Blotter",
                    "Hearings today",
                    SafeScalar("SELECT COUNT(*) FROM case_hearing WHERE status = 'SCHEDULED' AND schedule_at >= CURDATE() AND schedule_at < DATE_ADD(CURDATE(), INTERVAL 1 DAY)"),
                    "Blotter",
                    0);

                AddActionRow(
                    table,
                    "Residents",
                    "Inactive residents",
                    SafeScalar("SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'MOVED_OUT'"),
                    "Profile",
                    2);

                AddActionRow(
                    table,
                    "Staff",
                    "Inactive accounts",
                    SafeScalar("SELECT COUNT(*) FROM user_account WHERE is_active = 0"),
                    "Users",
                    2);

                if (table.Rows.Count == 0)
                {
                    table.Rows.Add("System", "No urgent action items", 0, string.Empty, 9);
                }

                var view = table.DefaultView;
                view.Sort = "priority_level ASC, count DESC";
                _form.SetActionCenter(view.ToTable());
            }

            private static void AddActionRow(
                DataTable table,
                string module,
                string alert,
                int count,
                string targetView,
                int priorityLevel)
            {
                if (count <= 0)
                {
                    return;
                }

                table.Rows.Add(module, alert, count, targetView, priorityLevel);
            }

            public void HandleActionCenterOpen()
            {
                if (!_form.TryGetSelectedActionTarget(out var target))
                {
                    return;
                }

                switch (target)
                {
                    case "Certificates":
                        _form.OpenResidentsFromDashboard(ResidentsView.Certificates);
                        break;
                    case "Blotter":
                        _form.OpenResidentsFromDashboard(ResidentsView.Blotter);
                        break;
                    case "Profile":
                        _form.OpenResidentsFromDashboard(ResidentsView.Profile);
                        break;
                    case "Users":
                        HandleViewAllStaff();
                        break;
                }
            }

            public void HandleOpenEllieAssistant()
            {
                using var form = new EllieAssistantForm();
                form.ShowDialog(_form);
            }

            private void WireKpiDrillDown()
            {
                if (_kpiDrillDownWired)
                {
                    return;
                }

                _kpiDrillDownWired = true;
                WireControlClick(_form.statResidentsCard, (_, __) => _form.OpenResidentsFromDashboard(ResidentsView.Profile));
                WireControlClick(_form.statActiveCard, (_, __) => _form.OpenResidentsFromDashboard(ResidentsView.Profile));
                WireControlClick(_form.statHouseholdsCard, (_, __) => _form.OpenResidentsFromDashboard(ResidentsView.Profile));
                WireControlClick(_form.statCertsCard, (_, __) => _form.OpenResidentsFromDashboard(ResidentsView.Certificates));
                WireControlClick(_form.statBlotterCard, (_, __) => _form.OpenResidentsFromDashboard(ResidentsView.Blotter));
            }

            private static void WireControlClick(Control root, EventHandler handler)
            {
                root.Cursor = Cursors.Hand;
                root.Click += handler;
                foreach (Control child in root.Controls)
                {
                    WireControlClick(child, handler);
                }
            }

            private void LoadDashboardTrends()
            {
                int certRequested = SafeScalar("SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED'");
                int certApproved = SafeScalar("SELECT COUNT(*) FROM document_request WHERE status = 'APPROVED'");
                int certIssued = SafeScalar("SELECT COUNT(*) FROM document_request WHERE status = 'RELEASED'");
                int certCancelled = SafeScalar("SELECT COUNT(*) FROM document_request WHERE status = 'CANCELLED'");

                int blotterOngoing = SafeScalar("SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING'");
                int blotterSettled = SafeScalar("SELECT COUNT(*) FROM case_record WHERE status = 'SETTLED'");
                int blotterReferred = SafeScalar("SELECT COUNT(*) FROM case_record WHERE status = 'REFERRED'");

                var monthLabels = new string[6];
                var monthCounts = new int[6];

                DateTime now = DateTime.Now;
                for (int i = 0; i < 6; i++)
                {
                    var month = new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i);
                    monthLabels[i] = month.ToString("MMM");
                    monthCounts[i] = 0;
                }

                var table = SafeLoadTable(
                    "SELECT DATE_FORMAT(created_at, '%Y-%m') AS ym, COUNT(*) AS total " +
                    "FROM resident WHERE IFNULL(is_deleted,0)=0 AND created_at >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH) " +
                    "GROUP BY ym");

                foreach (System.Data.DataRow row in table.Rows)
                {
                    var ym = row["ym"]?.ToString();
                    if (string.IsNullOrWhiteSpace(ym)) continue;

                    if (DateTime.TryParse(ym + "-01", out var month))
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            var expected = new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i);
                            if (month.Year == expected.Year && month.Month == expected.Month)
                            {
                                if (int.TryParse(row["total"]?.ToString(), out var count))
                                {
                                    monthCounts[i] = count;
                                }
                                break;
                            }
                        }
                    }
                }

                _form.SetDashboardTrendStats(
                    certRequested, certApproved, certIssued, certCancelled,
                    blotterOngoing, blotterSettled, blotterReferred,
                    monthLabels, monthCounts);
            }

            private async Task LoadDashboardTrendsAsync()
            {
                try
                {
                    // Parallelize trend queries
                    var certRequestedTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED'");
                    var certApprovedTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM document_request WHERE status = 'APPROVED'");
                    var certIssuedTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM document_request WHERE status = 'RELEASED'");
                    var certCancelledTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM document_request WHERE status = 'CANCELLED'");

                    var blotterOngoingTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING'");
                    var blotterSettledTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM case_record WHERE status = 'SETTLED'");
                    var blotterReferredTask = DatabaseManagerAsync.SafeScalarAsync("SELECT COUNT(*) FROM case_record WHERE status = 'REFERRED'");

                    await Task.WhenAll(certRequestedTask, certApprovedTask, certIssuedTask, certCancelledTask,
                                     blotterOngoingTask, blotterSettledTask, blotterReferredTask);

                    int certRequested = await certRequestedTask;
                    int certApproved = await certApprovedTask;
                    int certIssued = await certIssuedTask;
                    int certCancelled = await certCancelledTask;
                    int blotterOngoing = await blotterOngoingTask;
                    int blotterSettled = await blotterSettledTask;
                    int blotterReferred = await blotterReferredTask;

                    var monthLabels = new string[6];
                    var monthCounts = new int[6];

                    DateTime now = DateTime.Now;
                    for (int i = 0; i < 6; i++)
                    {
                        var month = new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i);
                        monthLabels[i] = month.ToString("MMM");
                        monthCounts[i] = 0;
                    }

                    var table = await DatabaseManagerAsync.SafeLoadTableAsync(
                        "SELECT DATE_FORMAT(created_at, '%Y-%m') AS ym, COUNT(*) AS total " +
                        "FROM resident WHERE IFNULL(is_deleted,0)=0 AND created_at >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH) " +
                        "GROUP BY ym");

                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        var ym = row["ym"]?.ToString();
                        if (string.IsNullOrWhiteSpace(ym)) continue;

                        if (DateTime.TryParse(ym + "-01", out var month))
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                var expected = new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i);
                                if (month.Year == expected.Year && month.Month == expected.Month)
                                {
                                    if (int.TryParse(row["total"]?.ToString(), out var count))
                                    {
                                        monthCounts[i] = count;
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    // Update UI on main thread
                    _form.BeginInvoke(new Action(() =>
                    {
                        _form.SetDashboardTrendStats(
                            certRequested, certApproved, certIssued, certCancelled,
                            blotterOngoing, blotterSettled, blotterReferred,
                            monthLabels, monthCounts);
                    }));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load dashboard trends: {ex.Message}");
                }
            }

            private async Task LoadOfficialsAsync()
            {
                try
                {
                    var table = await DatabaseManagerAsync.LoadTableAsync(
                        "SELECT ua.user_id, ua.username, COALESCE(r.name, 'Staff') AS role, ua.photo_url AS photo, ua.is_active, ua.last_login_at " +
                        "FROM user_account ua " +
                        "LEFT JOIN user_role ur ON ur.user_id = ua.user_id " +
                        "LEFT JOIN role r ON r.role_id = ur.role_id " +
                        "ORDER BY (r.name = 'Super Admin') DESC, (r.name = 'Admin') DESC, ua.created_at DESC " +
                        "LIMIT 4");

                    var officials = new AdminDashboard.OfficialInfo[table.Rows.Count];
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var row = table.Rows[i];
                        int userId = row["user_id"] != DBNull.Value ? Convert.ToInt32(row["user_id"]) : 0;
                        var name = row["username"]?.ToString() ?? "Official";
                        var role = row["role"]?.ToString() ?? "Staff";
                        var photo = row["photo"]?.ToString();
                        bool isActive = row["is_active"] != DBNull.Value && Convert.ToInt32(row["is_active"]) == 1;
                        string? lastLoginText = FormatLastLogin(row["last_login_at"]);
                        officials[i] = new AdminDashboard.OfficialInfo(userId, name, role, photo, isActive, lastLoginText);
                    }

                    _form.BeginInvoke(new Action(() => _form.SetOfficials(officials)));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load officials: {ex.Message}");

                    try
                    {
                        var fallbackTable = await DatabaseManagerAsync.LoadTableAsync(
                            "SELECT ua.user_id, ua.username, COALESCE(r.name, 'Staff') AS role, ua.is_active " +
                            "FROM user_account ua " +
                            "LEFT JOIN user_role ur ON ur.user_id = ua.user_id " +
                            "LEFT JOIN role r ON r.role_id = ur.role_id " +
                            "ORDER BY (r.name = 'Super Admin') DESC, (r.name = 'Admin') DESC, ua.created_at DESC " +
                            "LIMIT 4");

                        var officials = new AdminDashboard.OfficialInfo[fallbackTable.Rows.Count];
                        for (int i = 0; i < fallbackTable.Rows.Count; i++)
                        {
                            var row = fallbackTable.Rows[i];
                            int userId = row["user_id"] != DBNull.Value ? Convert.ToInt32(row["user_id"]) : 0;
                            var name = row["username"]?.ToString() ?? "Official";
                            var role = row["role"]?.ToString() ?? "Staff";
                            bool isActive = row["is_active"] != DBNull.Value && Convert.ToInt32(row["is_active"]) == 1;
                            officials[i] = new AdminDashboard.OfficialInfo(userId, name, role, null, isActive);
                        }

                        _form.BeginInvoke(new Action(() => _form.SetOfficials(officials)));
                    }
                    catch
                    {
                        // Keep dashboard usable even when fallback data cannot be loaded.
                    }
                }
            }

            private async Task LoadAnnouncementsAsync()
            {
                try
                {
                    _form.BeginInvoke(new Action(() => _form.ShowAnnouncementsLoading()));

                    var table = await DatabaseManagerAsync.LoadTableAsync(
                        @"SELECT a.announcement_id,
                                 a.title,
                                 a.body,
                                 a.priority,
                                 a.status,
                                 DATE_FORMAT(a.created_at, '%b %d, %Y') AS published,
                                 COALESCE(au.state, 'NEW') AS user_state
                          FROM announcements a
                          LEFT JOIN announcement_user_state au
                            ON au.announcement_id = a.announcement_id
                           AND au.user_id = @uid
                          WHERE COALESCE(au.state, 'NEW') <> 'ARCHIVED'
                          ORDER BY a.created_at DESC
                          LIMIT 8",
                        cmd => cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId));

                    _form.BeginInvoke(new Action(() => _form.SetAnnouncements(table)));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load announcements: {ex.Message}");
                }
            }

            private async Task LoadProjectsAsync()
            {
                try
                {
                    _form.BeginInvoke(new Action(() => _form.ShowProjectsLoading()));

                    var table = await DatabaseManagerAsync.LoadTableAsync(
                        @"SELECT project_id,
                                 name,
                                 status,
                                 budget,
                                 `lead`,
                                 remarks,
                                 DATE_FORMAT(start_date, '%b %d, %Y') AS start_date,
                                 DATE_FORMAT(end_date, '%b %d, %Y') AS end_date
                          FROM projects
                          ORDER BY created_at DESC
                          LIMIT 8");

                    _form.BeginInvoke(new Action(() => _form.SetProjects(table)));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load projects: {ex.Message}");
                }
            }

            private async Task LoadActionCenterAsync()
            {
                try
                {
                    var table = new DataTable();
                    table.Columns.Add("module", typeof(string));
                    table.Columns.Add("alert", typeof(string));
                    table.Columns.Add("count", typeof(int));
                    table.Columns.Add("target_view", typeof(string));
                    table.Columns.Add("priority_level", typeof(int));

                    var overdueApprovalsTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED' AND requested_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)");
                    var overduePickupsTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM document_request WHERE status = 'APPROVED' AND approved_at IS NOT NULL AND approved_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY)");
                    var overdueCasesTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM case_record WHERE status IN ('OPEN','ONGOING') AND created_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)");
                    var approvalsDueSoonTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED' AND requested_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY) AND requested_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)");
                    var pickupsDueSoonTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM document_request WHERE status = 'APPROVED' AND approved_at IS NOT NULL AND approved_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY) AND approved_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)");
                    var clearancesExpiringSoonTask = DatabaseManagerAsync.SafeScalarAsync(
                        @"SELECT COUNT(*)
                          FROM document_request dr
                          INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                          WHERE dr.status = 'RELEASED'
                            AND dr.expires_at IS NOT NULL
                            AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                            AND DATE(dr.expires_at) >= CURDATE()
                            AND DATE(dr.expires_at) <= DATE_ADD(CURDATE(), INTERVAL IFNULL(dt.renewal_reminder_days, 30) DAY)");
                    var expiredClearancesTask = DatabaseManagerAsync.SafeScalarAsync(
                        @"SELECT COUNT(*)
                          FROM document_request dr
                          INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                          WHERE dr.status = 'RELEASED'
                            AND dr.expires_at IS NOT NULL
                            AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                            AND DATE(dr.expires_at) < CURDATE()");
                    var casesDueSoonTask = DatabaseManagerAsync.SafeScalarAsync(
                        $"SELECT COUNT(*) FROM case_record WHERE status IN ('OPEN','ONGOING') AND created_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY) AND created_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY), INTERVAL {SlaRules.BlotterDueSoonDays + 1} DAY)");
                    var missedHearingsTask = DatabaseManagerAsync.SafeScalarAsync(
                        "SELECT COUNT(*) FROM case_hearing WHERE status = 'SCHEDULED' AND schedule_at < CURDATE()");
                    var hearingsTodayTask = DatabaseManagerAsync.SafeScalarAsync(
                        "SELECT COUNT(*) FROM case_hearing WHERE status = 'SCHEDULED' AND schedule_at >= CURDATE() AND schedule_at < DATE_ADD(CURDATE(), INTERVAL 1 DAY)");
                    var inactiveResidentsTask = DatabaseManagerAsync.SafeScalarAsync(
                        "SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'MOVED_OUT'");
                    var inactiveAccountsTask = DatabaseManagerAsync.SafeScalarAsync(
                        "SELECT COUNT(*) FROM user_account WHERE is_active = 0");

                    await Task.WhenAll(
                        overdueApprovalsTask,
                        overduePickupsTask,
                        overdueCasesTask,
                        approvalsDueSoonTask,
                        pickupsDueSoonTask,
                        clearancesExpiringSoonTask,
                        expiredClearancesTask,
                        casesDueSoonTask,
                        missedHearingsTask,
                        hearingsTodayTask,
                        inactiveResidentsTask,
                        inactiveAccountsTask);

                    AddActionRow(table, "Certificates", "Overdue approvals", await overdueApprovalsTask, "Certificates", 0);
                    AddActionRow(table, "Certificates", "Overdue pickups", await overduePickupsTask, "Certificates", 0);
                    AddActionRow(table, "Blotter", "Overdue cases", await overdueCasesTask, "Blotter", 0);
                    AddActionRow(table, "Certificates", "Approvals due soon", await approvalsDueSoonTask, "Certificates", 1);
                    AddActionRow(table, "Certificates", "Pickups due soon", await pickupsDueSoonTask, "Certificates", 1);
                    AddActionRow(table, "Certificates", "Clearances expiring soon", await clearancesExpiringSoonTask, "Certificates", 1);
                    AddActionRow(table, "Certificates", "Expired clearances", await expiredClearancesTask, "Certificates", 0);
                    AddActionRow(table, "Blotter", "Cases due soon", await casesDueSoonTask, "Blotter", 1);
                    AddActionRow(table, "Blotter", "Missed hearings", await missedHearingsTask, "Blotter", 0);
                    AddActionRow(table, "Blotter", "Hearings today", await hearingsTodayTask, "Blotter", 0);
                    AddActionRow(table, "Residents", "Inactive residents", await inactiveResidentsTask, "Profile", 2);
                    AddActionRow(table, "Staff", "Inactive accounts", await inactiveAccountsTask, "Users", 2);

                    if (table.Rows.Count == 0)
                    {
                        table.Rows.Add("System", "No urgent action items", 0, string.Empty, 9);
                    }

                    var view = table.DefaultView;
                    view.Sort = "priority_level ASC, count DESC";
                    _form.BeginInvoke(new Action(() => _form.SetActionCenter(view.ToTable())));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load action center: {ex.Message}");
                }
            }

            private async Task LoadNotificationsAsync()
            {
                try
                {
                                        var table = await DatabaseManagerAsync.LoadTableAsync(
                                                @"SELECT a.announcement_id,
                                                                 a.title,
                                                                 a.body,
                                                                 a.priority,
                                                                 a.status,
                                                                 DATE_FORMAT(a.created_at, '%b %d, %Y') AS published,
                                                                 COALESCE(au.state, 'NEW') AS user_state
                                                    FROM announcements a
                                                    LEFT JOIN announcement_user_state au
                                                        ON au.announcement_id = a.announcement_id
                                                     AND au.user_id = @uid
                                                    WHERE a.status = 'Published'
                                                        AND COALESCE(au.state, 'NEW') = 'NEW'
                                                    ORDER BY a.created_at DESC
                                                    LIMIT 10",
                                                cmd => cmd.Parameters.AddWithValue("@uid", helper.UserSession.UserId));

                    _form.BeginInvoke(new Action(() => _form.SetNotifications(table)));
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load notifications: {ex.Message}");
                }
            }

            private Task LoadBackupStatusAsync()
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var info = BackupService.TryGetLatestRun();
                        if (info?.State == BackupRunState.Running && !_backupInProgress)
                        {
                            info = BackupService.MarkInterruptedRunAsFailed(
                                info,
                                "Backup was left in a running state after the app closed.");
                        }

                        _form.BeginInvoke(new Action(() => _form.SetBackupStatus(info)));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Failed to load backup status: {ex.Message}");
                    }
                });
            }

            private Task LoadSchemaVersionAsync()
            {
                return Task.Run(() =>
                {
                    try
                    {
                        string? version = Database.MigrationRunner.TryGetCurrentSchemaVersion();
                        _form.BeginInvoke(new Action(() => _form.SetSchemaVersion(version)));
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Failed to load schema version: {ex.Message}");
                    }
                });
            }

            private static int SafeScalar(string sql)
            {
                try
                {
                    return DbHelper.ExecuteScalar<int>(sql);
                }
                catch
                {
                    return 0;
                }
            }

            private static System.Data.DataTable SafeLoadTable(string sql)
            {
                try
                {
                    return DbHelper.LoadTable(sql);
                }
                catch
                {
                    return new System.Data.DataTable();
                }
            }

            private static void Try(Action action)
            {
                try
                {
                    action();
                }
                catch
                {
                    // Ignore feature load failures to keep dashboard usable.
                }
            }

            private void LoadOfficials()
            {
                try
                {
                    var table = DbHelper.LoadTable(
                        "SELECT ua.user_id, ua.username, COALESCE(r.name, 'Staff') AS role, ua.photo_url AS photo, ua.is_active, ua.last_login_at " +
                        "FROM user_account ua " +
                        "LEFT JOIN user_role ur ON ur.user_id = ua.user_id " +
                        "LEFT JOIN role r ON r.role_id = ur.role_id " +
                        "ORDER BY (r.name = 'Super Admin') DESC, (r.name = 'Admin') DESC, ua.created_at DESC " +
                        "LIMIT 4");

                    var officials = new AdminDashboard.OfficialInfo[table.Rows.Count];
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var row = table.Rows[i];
                        int userId = row["user_id"] != DBNull.Value ? Convert.ToInt32(row["user_id"]) : 0;
                        var name = row["username"]?.ToString() ?? "Official";
                        var role = row["role"]?.ToString() ?? "Staff";
                        var photo = row["photo"]?.ToString();
                        bool isActive = row["is_active"] != DBNull.Value && Convert.ToInt32(row["is_active"]) == 1;
                        string? lastLoginText = FormatLastLogin(row["last_login_at"]);
                        officials[i] = new AdminDashboard.OfficialInfo(userId, name, role, photo, isActive, lastLoginText);
                    }

                    _form.SetOfficials(officials);
                }
                catch
                {
                    var table = DbHelper.LoadTable(
                        "SELECT ua.user_id, ua.username, COALESCE(r.name, 'Staff') AS role, ua.is_active " +
                        "FROM user_account ua " +
                        "LEFT JOIN user_role ur ON ur.user_id = ua.user_id " +
                        "LEFT JOIN role r ON r.role_id = ur.role_id " +
                        "ORDER BY (r.name = 'Super Admin') DESC, (r.name = 'Admin') DESC, ua.created_at DESC " +
                        "LIMIT 4");

                    var officials = new AdminDashboard.OfficialInfo[table.Rows.Count];
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        var row = table.Rows[i];
                        int userId = row["user_id"] != DBNull.Value ? Convert.ToInt32(row["user_id"]) : 0;
                        var name = row["username"]?.ToString() ?? "Official";
                        var role = row["role"]?.ToString() ?? "Staff";
                        bool isActive = row["is_active"] != DBNull.Value && Convert.ToInt32(row["is_active"]) == 1;
                        officials[i] = new AdminDashboard.OfficialInfo(userId, name, role, null, isActive);
                    }

                    _form.SetOfficials(officials);
                }
            }

            private static string? FormatLastLogin(object? value)
            {
                if (value == null || value == DBNull.Value)
                {
                    return null;
                }

                if (!DateTime.TryParse(Convert.ToString(value), out var timestamp))
                {
                    return null;
                }

                return timestamp.ToString("MMM dd, h:mm tt");
            }

            public void HandleUpdateOfficial(int userId)
            {
                if (!helper.Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    return;
                }

                using var form = new UpdateUserForm(userId);
                form.ShowDialog(_form);
                _ = LoadDashboardStatsAsync();
            }

            public void HandleViewAllStaff()
            {
                if (!helper.Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    return;
                }

                _form.OpenUsersListModule();
            }
        }
    }
}
