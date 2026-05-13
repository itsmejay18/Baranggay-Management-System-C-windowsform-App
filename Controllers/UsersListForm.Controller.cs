using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class UsersListForm
    {
        private sealed class UsersListFormController
        {
            private readonly UsersListForm _form;
            private readonly System.Windows.Forms.Timer _searchDebounceTimer = new System.Windows.Forms.Timer();
            private CancellationTokenSource? _loadCancellation;
            private int _loadVersion;
            private bool _permissionsDenied;

            public UsersListFormController(UsersListForm form)
            {
                _form = form;
                _searchDebounceTimer.Interval = 250;
                _searchDebounceTimer.Tick += (_, __) =>
                {
                    _searchDebounceTimer.Stop();
                    TriggerLoad(immediate: true);
                };
                _form.Disposed += (_, __) =>
                {
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Dispose();
                    _loadCancellation?.Cancel();
                    _loadCancellation?.Dispose();
                    _loadCancellation = null;
                };
            }

            public void TriggerLoad(bool immediate = false)
            {
                if (!Permissions.CanManageUsers)
                {
                    if (_permissionsDenied)
                    {
                        return;
                    }

                    _permissionsDenied = true;
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    _form.Close();
                    return;
                }

                _permissionsDenied = false;
                if (immediate)
                {
                    _searchDebounceTimer.Stop();
                    _ = LoadUsersAsync();
                    return;
                }

                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }

            public async Task LoadUsersAsync()
            {
                if (!Permissions.CanManageUsers || _form.IsDisposed)
                {
                    return;
                }

                int loadVersion = Interlocked.Increment(ref _loadVersion);
                _loadCancellation?.Cancel();
                _loadCancellation?.Dispose();
                _loadCancellation = new CancellationTokenSource();
                CancellationToken token = _loadCancellation.Token;

                var sql = new StringBuilder();
                sql.Append("SELECT ua.user_id, ua.username, ua.first_name, ua.middle_name, ua.last_name, ");
                sql.Append("COALESCE(r.name, 'Staff') AS role, ua.is_active, ua.email, ua.contact_no, ua.position, ua.department, ua.last_project, ua.last_login_at AS last_login, ua.created_at ");
                sql.Append("FROM user_account ua ");
                sql.Append("LEFT JOIN user_role ur ON ur.user_id = ua.user_id ");
                sql.Append("LEFT JOIN role r ON r.role_id = ur.role_id ");
                sql.Append("WHERE 1=1 ");

                string search = _form.SearchText.Trim();
                string role = _form.RoleFilter;
                string status = _form.StatusFilter;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql.Append(" AND (ua.username LIKE @q OR ua.first_name LIKE @q OR ua.middle_name LIKE @q OR ua.last_name LIKE @q OR ua.email LIKE @q OR ua.full_name LIKE @q) ");
                }

                if (!string.IsNullOrWhiteSpace(role) && role != "All")
                {
                    sql.Append(" AND r.name = @role ");
                }

                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    sql.Append(" AND ua.is_active = @active ");
                }

                try
                {
                    DataTable table = await Task.Run(() => DbHelper.LoadTable(sql.ToString(), cmd =>
                    {
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            cmd.Parameters.AddWithValue("@q", "%" + search + "%");
                        }

                        if (!string.IsNullOrWhiteSpace(role) && role != "All")
                        {
                            cmd.Parameters.AddWithValue("@role", role);
                        }

                        if (!string.IsNullOrWhiteSpace(status) && status != "All")
                        {
                            int active = status == "Active" ? 1 : 0;
                            cmd.Parameters.AddWithValue("@active", active);
                        }
                    }), token).ConfigureAwait(true);

                    if (token.IsCancellationRequested || _form.IsDisposed || loadVersion != _loadVersion)
                    {
                        return;
                    }

                    _form.UsersGrid.DataSource = table;
                    _form.UsersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    ConfigureUsersGridColumns();
                }
                catch (OperationCanceledException)
                {
                    // Ignore canceled loads (newer query already queued).
                }
                catch (Exception ex)
                {
                    if (!_form.IsDisposed)
                    {
                        AppLogger.LogError("Failed to load users.", ex);
                    }
                }
            }

            public void EditSelected()
            {
                if (!Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    _form.Close();
                    return;
                }

                if (_form.UsersGrid.CurrentRow == null)
                {
                    ControllerDialogs.Warning("Select a user first.");
                    return;
                }

                var cell = _form.UsersGrid.CurrentRow.Cells["user_id"];
                if (cell?.Value == null || !int.TryParse(cell.Value.ToString(), out var userId))
                {
                    ControllerDialogs.Warning("Invalid user selection.");
                    return;
                }

                using var form = new UpdateUserForm(userId);
                form.ShowDialog(_form);
                TriggerLoad(immediate: true);
            }

            private void ConfigureUsersGridColumns()
            {
                var grid = _form.UsersGrid;
                if (grid.Columns.Count == 0)
                {
                    return;
                }

                if (grid.Columns.Contains("user_id"))
                {
                    grid.Columns["user_id"].Visible = false;
                }

                if (grid.Columns.Contains("username"))
                {
                    grid.Columns["username"].HeaderText = "Username";
                    grid.Columns["username"].FillWeight = 95;
                }

                if (grid.Columns.Contains("first_name"))
                {
                    grid.Columns["first_name"].HeaderText = "First Name";
                    grid.Columns["first_name"].FillWeight = 90;
                }

                if (grid.Columns.Contains("middle_name"))
                {
                    grid.Columns["middle_name"].HeaderText = "Middle Name";
                    grid.Columns["middle_name"].Visible = false;
                }

                if (grid.Columns.Contains("last_name"))
                {
                    grid.Columns["last_name"].HeaderText = "Last Name";
                    grid.Columns["last_name"].FillWeight = 90;
                }

                if (grid.Columns.Contains("role"))
                {
                    grid.Columns["role"].HeaderText = "Role";
                    grid.Columns["role"].FillWeight = 70;
                }

                if (grid.Columns.Contains("is_active"))
                {
                    grid.Columns["is_active"].HeaderText = "Active";
                    grid.Columns["is_active"].FillWeight = 55;
                }

                if (grid.Columns.Contains("email"))
                {
                    grid.Columns["email"].HeaderText = "Email";
                    grid.Columns["email"].FillWeight = 125;
                }

                if (grid.Columns.Contains("contact_no"))
                {
                    grid.Columns["contact_no"].HeaderText = "Contact";
                    grid.Columns["contact_no"].FillWeight = 85;
                }

                if (grid.Columns.Contains("position"))
                {
                    grid.Columns["position"].HeaderText = "Position";
                    grid.Columns["position"].FillWeight = 90;
                }

                if (grid.Columns.Contains("department"))
                {
                    grid.Columns["department"].HeaderText = "Department";
                    grid.Columns["department"].Visible = false;
                }

                if (grid.Columns.Contains("last_project"))
                {
                    grid.Columns["last_project"].HeaderText = "Last Project";
                    grid.Columns["last_project"].Visible = false;
                }

                if (grid.Columns.Contains("last_login"))
                {
                    grid.Columns["last_login"].HeaderText = "Last Login";
                    grid.Columns["last_login"].FillWeight = 95;
                    grid.Columns["last_login"].DefaultCellStyle.Format = "MMM dd, yyyy h:mm tt";
                }

                if (grid.Columns.Contains("created_at"))
                {
                    grid.Columns["created_at"].HeaderText = "Created";
                    grid.Columns["created_at"].FillWeight = 85;
                    grid.Columns["created_at"].DefaultCellStyle.Format = "MMM dd, yyyy";
                }
            }
        }
    }
}
