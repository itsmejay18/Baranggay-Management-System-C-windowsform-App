using System;
using System.Data;
using System.Windows.Forms;
using baranggaysystem1.Database;

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
            private bool _dashboardSchemaEnsured;

            public AdminDashboardController(AdminDashboard form)
            {
                _form = form;
            }

            public void LoadDashboardStats()
            {
                InitializeFeatures();
                EnsureDashboardSchema();

                // Load critical counts on UI thread
                (int totalResidents, int activeResidents, int households, int pendingCertificates, int ongoingBlotter) = LoadDashboardCounts();

                _form.SetDashboardStats(
                    totalResidents,
                    activeResidents,
                    households,
                    pendingCertificates,
                    ongoingBlotter);

                // Load non-critical data on background thread to avoid UI blocking
                System.Threading.Tasks.Task.Run(() =>
                {
                    Try(LoadDashboardTrends);
                    Try(LoadOfficials);
                    Try(LoadAnnouncements);
                    Try(LoadProjects);
                    Try(LoadActionCenter);
                    Try(LoadNotifications);
                    Try(LoadBackupStatus);
                    Try(LoadSchemaVersion);
                });
            }

            private void EnsureDashboardSchema()
            {
                if (_dashboardSchemaEnsured)
                {
                    return;
                }

                EnsureAnnouncementsSchema();
                EnsureAnnouncementUserStateSchema();
                EnsureProjectsSchema();
                _dashboardSchemaEnsured = true;
            }

            public void RefreshNotifications()
            {
                Try(LoadNotifications);
            }

            public void RefreshActionCenter()
            {
                Try(LoadActionCenter);
            }

            public void RefreshBackupStatus()
            {
                Try(LoadBackupStatus);
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

                var metrics = SafeLoadTable($@"
                    SELECT
                        (SELECT COUNT(*) FROM document_request
                          WHERE status = 'SUBMITTED'
                            AND requested_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)) AS overdue_approvals,
                        (SELECT COUNT(*) FROM document_request
                          WHERE status = 'APPROVED'
                            AND approved_at IS NOT NULL
                            AND approved_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY)) AS overdue_pickups,
                        (SELECT COUNT(*) FROM case_record
                          WHERE status IN ('OPEN','ONGOING')
                            AND created_at < DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)) AS overdue_cases,
                        (SELECT COUNT(*) FROM document_request
                          WHERE status = 'SUBMITTED'
                            AND requested_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY)
                            AND requested_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateApprovalSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)) AS approvals_due_soon,
                        (SELECT COUNT(*) FROM document_request
                          WHERE status = 'APPROVED'
                            AND approved_at IS NOT NULL
                            AND approved_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY)
                            AND approved_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.CertificateReleaseSlaDays} DAY), INTERVAL {SlaRules.CertificateDueSoonDays + 1} DAY)) AS pickups_due_soon,
                        (SELECT COUNT(*)
                           FROM document_request dr
                           INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                          WHERE dr.status = 'RELEASED'
                            AND dr.expires_at IS NOT NULL
                            AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                            AND DATE(dr.expires_at) >= CURDATE()
                            AND DATE(dr.expires_at) <= DATE_ADD(CURDATE(), INTERVAL IFNULL(dt.renewal_reminder_days, 30) DAY)) AS clearances_expiring_soon,
                        (SELECT COUNT(*)
                           FROM document_request dr
                           INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                          WHERE dr.status = 'RELEASED'
                            AND dr.expires_at IS NOT NULL
                            AND (UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
                            AND DATE(dr.expires_at) < CURDATE()) AS expired_clearances,
                        (SELECT COUNT(*) FROM case_record
                          WHERE status IN ('OPEN','ONGOING')
                            AND created_at >= DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY)
                            AND created_at < DATE_ADD(DATE_SUB(CURDATE(), INTERVAL {SlaRules.BlotterResolutionSlaDays} DAY), INTERVAL {SlaRules.BlotterDueSoonDays + 1} DAY)) AS cases_due_soon,
                        (SELECT COUNT(*) FROM case_hearing
                          WHERE status = 'SCHEDULED'
                            AND schedule_at < CURDATE()) AS missed_hearings,
                        (SELECT COUNT(*) FROM case_hearing
                          WHERE status = 'SCHEDULED'
                            AND schedule_at >= CURDATE()
                            AND schedule_at < DATE_ADD(CURDATE(), INTERVAL 1 DAY)) AS hearings_today,
                        (SELECT COUNT(*) FROM resident
                          WHERE IFNULL(is_deleted,0)=0
                            AND status = 'MOVED_OUT') AS inactive_residents,
                        (SELECT COUNT(*) FROM user_account
                          WHERE is_active = 0) AS inactive_accounts");

                DataRow? row = metrics.Rows.Count > 0 ? metrics.Rows[0] : null;

                AddActionRow(table, "Certificates", "Overdue approvals", ReadInt(row, "overdue_approvals"), "Certificates", 0);
                AddActionRow(table, "Certificates", "Overdue pickups", ReadInt(row, "overdue_pickups"), "Certificates", 0);
                AddActionRow(table, "Blotter", "Overdue cases", ReadInt(row, "overdue_cases"), "Blotter", 0);
                AddActionRow(table, "Certificates", "Approvals due soon", ReadInt(row, "approvals_due_soon"), "Certificates", 1);
                AddActionRow(table, "Certificates", "Pickups due soon", ReadInt(row, "pickups_due_soon"), "Certificates", 1);
                AddActionRow(table, "Certificates", "Clearances expiring soon", ReadInt(row, "clearances_expiring_soon"), "Certificates", 1);
                AddActionRow(table, "Certificates", "Expired clearances", ReadInt(row, "expired_clearances"), "Certificates", 0);
                AddActionRow(table, "Blotter", "Cases due soon", ReadInt(row, "cases_due_soon"), "Blotter", 1);
                AddActionRow(table, "Blotter", "Missed hearings", ReadInt(row, "missed_hearings"), "Blotter", 0);
                AddActionRow(table, "Blotter", "Hearings today", ReadInt(row, "hearings_today"), "Blotter", 0);
                AddActionRow(table, "Residents", "Inactive residents", ReadInt(row, "inactive_residents"), "Profile", 2);
                AddActionRow(table, "Staff", "Inactive accounts", ReadInt(row, "inactive_accounts"), "Users", 2);

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
                var summary = SafeLoadTable(@"
                    SELECT
                        (SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED') AS cert_requested,
                        (SELECT COUNT(*) FROM document_request WHERE status = 'APPROVED') AS cert_approved,
                        (SELECT COUNT(*) FROM document_request WHERE status = 'RELEASED') AS cert_issued,
                        (SELECT COUNT(*) FROM document_request WHERE status = 'CANCELLED') AS cert_cancelled,
                        (SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING') AS blotter_ongoing,
                        (SELECT COUNT(*) FROM case_record WHERE status = 'SETTLED') AS blotter_settled,
                        (SELECT COUNT(*) FROM case_record WHERE status = 'REFERRED') AS blotter_referred");
                DataRow? summaryRow = summary.Rows.Count > 0 ? summary.Rows[0] : null;

                int certRequested = ReadInt(summaryRow, "cert_requested");
                int certApproved = ReadInt(summaryRow, "cert_approved");
                int certIssued = ReadInt(summaryRow, "cert_issued");
                int certCancelled = ReadInt(summaryRow, "cert_cancelled");
                int blotterOngoing = ReadInt(summaryRow, "blotter_ongoing");
                int blotterSettled = ReadInt(summaryRow, "blotter_settled");
                int blotterReferred = ReadInt(summaryRow, "blotter_referred");

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

            private static int ReadInt(DataRow? row, string columnName)
            {
                if (row == null || !row.Table.Columns.Contains(columnName))
                {
                    return 0;
                }

                object? value = row[columnName];
                if (value == null || value == DBNull.Value)
                {
                    return 0;
                }

                if (value is int directInt)
                {
                    return directInt;
                }

                if (int.TryParse(Convert.ToString(value), out int parsed))
                {
                    return parsed;
                }

                try
                {
                    return Convert.ToInt32(value);
                }
                catch
                {
                    return 0;
                }
            }

            private static (int TotalResidents, int ActiveResidents, int Households, int PendingCertificates, int OngoingBlotter) LoadDashboardCounts()
            {
                var table = SafeLoadTable(@"
                    SELECT
                        (SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0) AS total_residents,
                        (SELECT COUNT(*) FROM resident WHERE IFNULL(is_deleted,0)=0 AND status = 'ACTIVE') AS active_residents,
                        (SELECT COUNT(*) FROM household) AS households,
                        (SELECT COUNT(*) FROM document_request WHERE status = 'SUBMITTED') AS pending_certificates,
                        (SELECT COUNT(*) FROM case_record WHERE status = 'ONGOING') AS ongoing_blotter");

                DataRow? row = table.Rows.Count > 0 ? table.Rows[0] : null;
                return (
                    ReadInt(row, "total_residents"),
                    ReadInt(row, "active_residents"),
                    ReadInt(row, "households"),
                    ReadInt(row, "pending_certificates"),
                    ReadInt(row, "ongoing_blotter"));
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
                LoadDashboardStats();
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
