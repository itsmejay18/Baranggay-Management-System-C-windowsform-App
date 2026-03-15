using baranggaysystem1.Database;
using baranggaysystem1.helper;
using System;
using System.Data;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class Form1
    {
        private sealed class Form1Controller
        {
            private readonly Form1 _form;

            public Form1Controller(Form1 form)
            {
                _form = form;
            }

            public void HandleLoginAsync()
            {
                _form.ShowLoginProgress();
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        HandleLogin();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError("Login failed with exception.", ex);
                        _form.HideLoginProgress();
                        _form.BeginInvoke(new Action(() =>
                        {
                            ControllerDialogs.Error($"Login failed: {ex.Message}");
                        }));
                    }
                });
            }

            public void HandleLogin()
            {
                string username = _form.txtUsername.Text;
                string password = _form.txtPassword.Text;

                // temporary backdoor so you can enter the dashboard without database access
                if (username == "admin" && password == "admin")
                {
                    UserSession.UserId = -1;
                    UserSession.BarangayId = SchemaDefaults.DefaultBarangayId;
                    UserSession.Role = "Admin";
                    UserSession.Username = username;
                    Permissions.Refresh();
                    _form.CompleteLogin(new AdminDashboard());
                    return;
                }

                string hashedPassword = PasswordHelper.HashPassword(password);

                string query = @"SELECT ua.user_id,
                                        ua.barangay_id,
                                        COALESCE(r.name, 'Staff') AS role
                     FROM user_account ua
                     LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                     LEFT JOIN role r ON r.role_id = ur.role_id
                     WHERE ua.username=@username
                     AND ua.password_hash=@password
                     AND ua.is_active=1
                     ORDER BY
                        (r.name = 'Super Admin') DESC,
                        (r.name = 'Admin') DESC
                     LIMIT 1";

                DataTable table = DbHelper.LoadTable(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);
                });

                if (table.Rows.Count > 0)
                {
                    DataRow row = table.Rows[0];
                    UserSession.UserId = Convert.ToInt32(row["user_id"]);
                    UserSession.BarangayId = row["barangay_id"] == DBNull.Value
                        ? SchemaDefaults.DefaultBarangayId
                        : Convert.ToInt32(row["barangay_id"]);
                    UserSession.Role = Convert.ToString(row["role"]) ?? string.Empty;
                    UserSession.Username = username;
                    Permissions.Refresh();

                    // start warming the cache in the background so UI stays responsive
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            // preload a few key tables used across the app
                            DatabaseManager.PreloadCache(
                                // dashboard / announcement data
                                @"SELECT announcement_id, title, body, priority, status, DATE_FORMAT(created_at, '%b %d, %Y') AS published FROM announcements ORDER BY created_at DESC LIMIT 8",
                                @"SELECT project_id, name, status, budget, `lead`, remarks, DATE_FORMAT(start_date, '%b %d, %Y') AS start_date, DATE_FORMAT(end_date, '%b %d, %Y') AS end_date FROM projects ORDER BY created_at DESC LIMIT 8",
                                // a small resident sample (barangay scope)
                                $"SELECT resident_id, firstname, lastname FROM resident WHERE barangay_id = {UserSession.BarangayId} LIMIT 100"
                            );

                            // user‑specific queries need parameter binding so load them separately
                            DbHelper.LoadTable(@"SELECT a.announcement_id, a.title, a.body, a.priority, a.status, DATE_FORMAT(a.created_at, '%b %d, %Y') AS published, COALESCE(au.state, 'NEW') AS user_state
                                              FROM announcements a
                                              LEFT JOIN announcement_user_state au
                                                ON au.announcement_id = a.announcement_id
                                               AND au.user_id = @uid
                                              WHERE COALESCE(au.state, 'NEW') <> 'ARCHIVED'
                                              ORDER BY a.created_at DESC
                                              LIMIT 8",
                                cmd => cmd.Parameters.AddWithValue("@uid", UserSession.UserId));

                            DbHelper.LoadTable(@"SELECT a.announcement_id, a.title, a.body, a.priority, a.status, DATE_FORMAT(a.created_at, '%b %d, %Y') AS published, COALESCE(au.state, 'NEW') AS user_state
                                              FROM announcements a
                                              LEFT JOIN announcement_user_state au
                                                ON au.announcement_id = a.announcement_id
                                               AND au.user_id = @uid
                                              WHERE a.status = 'Published'
                                                AND COALESCE(au.state, 'NEW') = 'NEW'
                                              ORDER BY a.created_at DESC
                                              LIMIT 10",
                                cmd => cmd.Parameters.AddWithValue("@uid", UserSession.UserId));
                        }
                        catch
                        {
                            // ignore preload hiccups
                        }
                    });

                    if (UserSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                        || UserSession.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        _form.CompleteLogin(new AdminDashboard());
                    }
                    else
                    {
                        _form.CompleteLogin(new StaffDashboard());
                    }
                }
                else
                {
                    ControllerDialogs.Warning("Invalid username or password");
                }
            }

            public void HandleRegister()
            {
                _form.OpenRegister();
            }

            public void HandleLoad()
            {
                _form.StartFadeIn();
            }
        }
    }
}
