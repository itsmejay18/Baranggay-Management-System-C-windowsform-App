using System;
using System.Linq;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1
{
    public partial class UpdateUserForm
    {
        private sealed class UpdateUserFormController
        {
            private readonly UpdateUserForm _form;
            private readonly int _userId;

            public UpdateUserFormController(UpdateUserForm form, int userId)
            {
                _form = form;
                _userId = userId;
            }

            public void LoadUser()
            {
                if (!Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    _form.Close();
                    return;
                }

                try
                {
                    var table = DbHelper.LoadTable(
                        "SELECT ua.username, ua.first_name, ua.middle_name, ua.last_name, ua.email, ua.contact_no, ua.position, ua.department, ua.last_project, " +
                        "COALESCE(r.name, 'Staff') AS role, ua.is_active, ua.photo_url AS photo " +
                        "FROM user_account ua " +
                        "LEFT JOIN user_role ur ON ur.user_id = ua.user_id " +
                        "LEFT JOIN role r ON r.role_id = ur.role_id " +
                        "WHERE ua.user_id = @id",
                        cmd => cmd.Parameters.AddWithValue("@id", _userId));

                    if (table.Rows.Count == 0)
                    {
                        ControllerDialogs.Warning("User not found.");
                        _form.Close();
                        return;
                    }

                    var row = table.Rows[0];
                    var username = row["username"]?.ToString() ?? string.Empty;
                    var firstName = row["first_name"]?.ToString() ?? string.Empty;
                    var middleName = row["middle_name"]?.ToString() ?? string.Empty;
                    var lastName = row["last_name"]?.ToString() ?? string.Empty;
                    var email = row["email"]?.ToString() ?? string.Empty;
                    var contact = row["contact_no"]?.ToString() ?? string.Empty;
                    var position = row["position"]?.ToString() ?? string.Empty;
                    var department = row["department"]?.ToString() ?? string.Empty;
                    var lastProject = row["last_project"]?.ToString() ?? string.Empty;
                    var role = row["role"]?.ToString() ?? "Staff";
                    var photo = row["photo"]?.ToString();
                    bool isActive = row["is_active"] != DBNull.Value && Convert.ToInt32(row["is_active"]) == 1;
                    _form.SetUserFields(username, firstName, middleName, lastName, email, contact, position, department, lastProject, role, isActive, photo);
                }
                catch
                {
                    ControllerDialogs.Warning("Unable to load user details.");
                    _form.Close();
                }
            }

            public void SaveUser()
            {
                if (!Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    _form.Close();
                    return;
                }

                string username = _form.GetUsername();
                string firstName = _form.GetFirstName();
                string middleName = _form.GetMiddleName();
                string lastName = _form.GetLastName();
                string email = _form.GetEmail();
                string contact = _form.GetContact();
                string position = _form.GetPosition();
                string department = _form.GetDepartment();
                string lastProject = _form.GetLastProject();
                string role = _form.GetRole();
                bool isActive = _form.GetIsActive();
                string? photoPath = _form.GetPhotoPath();

                var validation = ValidationService.ValidateUserUpdate(username);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Save changes to this staff account?",
                    "Confirm Update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                try
                {
                    using var conn = DBConnection.GetConnection();
                    conn.Open();
                    using var tx = conn.BeginTransaction();
                    object? beforeSnapshot = ReadUserAuditSnapshot(conn, _userId, tx);
                    string oldRoleName = ReadUserRoleName(conn, _userId, tx);

                    string fullName = string.Join(" ", new[] { firstName, middleName, lastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

                    string sql = @"UPDATE user_account 
                                   SET username = @username,
                                       first_name = @firstName,
                                       middle_name = @middleName,
                                       last_name = @lastName,
                                       full_name = @fullName,
                                       email = @email,
                                       contact_no = @contact,
                                       position = @position,
                                       department = @department,
                                       last_project = @lastProject,
                                       is_active = @active,
                                       photo_url = @photo,
                                       updated_at = NOW()
                                   WHERE user_id = @id";
                    using var cmd = new MySqlCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@middleName", middleName);
                    cmd.Parameters.AddWithValue("@lastName", lastName);
                    cmd.Parameters.AddWithValue("@fullName", string.IsNullOrWhiteSpace(fullName) ? (object)DBNull.Value : fullName);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@contact", contact);
                    cmd.Parameters.AddWithValue("@position", position);
                    cmd.Parameters.AddWithValue("@department", department);
                    cmd.Parameters.AddWithValue("@lastProject", lastProject);
                    cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@photo", (object?)photoPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", _userId);
                    cmd.ExecuteNonQuery();

                    int roleId = EnsureRole(conn, role, tx);
                    using var clearRole = new MySqlCommand("DELETE FROM user_role WHERE user_id = @id", conn, tx);
                    clearRole.Parameters.AddWithValue("@id", _userId);
                    clearRole.ExecuteNonQuery();

                    using var insertRole = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@id, @rid)", conn, tx);
                    insertRole.Parameters.AddWithValue("@id", _userId);
                    insertRole.Parameters.AddWithValue("@rid", roleId);
                    insertRole.ExecuteNonQuery();

                    object? afterSnapshot = ReadUserAuditSnapshot(conn, _userId, tx);
                    string note = string.Equals(oldRoleName, role, StringComparison.OrdinalIgnoreCase)
                        ? "User account updated."
                        : $"User account updated. Role changed: {oldRoleName} -> {role}.";
                    AuditTrailService.LogTransactional(
                        conn,
                        tx,
                        "Users",
                        "user_account",
                        _userId,
                        "UPDATE",
                        beforeSnapshot,
                        afterSnapshot,
                        note);

                    tx.Commit();

                    if (_userId == UserSession.UserId)
                    {
                        UserSession.Role = role;
                        Permissions.Refresh();
                    }

                    ControllerDialogs.Info("User updated.");
                    _form.Close();
                }
                catch
                {
                    ControllerDialogs.Warning("Unable to update user.");
                }
            }

            public void UploadPhoto()
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Select staff photo",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _form.SetPhotoPath(dialog.FileName);
                }
            }

            public void RemovePhoto()
            {
                _form.SetPhotoPath(null);
            }

            private static int EnsureRole(MySqlConnection conn, string roleName, MySqlTransaction? tx = null)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    roleName = "Staff";
                }

                using var select = new MySqlCommand("SELECT role_id FROM role WHERE name = @name LIMIT 1", conn);
                select.Transaction = tx;
                select.Parameters.AddWithValue("@name", roleName);
                object? existing = select.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    return Convert.ToInt32(existing);
                }

                using var insert = new MySqlCommand("INSERT INTO role (name) VALUES (@name)", conn);
                insert.Transaction = tx;
                insert.Parameters.AddWithValue("@name", roleName);
                insert.ExecuteNonQuery();
                return (int)insert.LastInsertedId;
            }

            private static string ReadUserRoleName(MySqlConnection conn, int userId, MySqlTransaction? tx = null)
            {
                using var cmd = new MySqlCommand(
                    @"SELECT COALESCE(r.name, 'Staff')
                      FROM user_account ua
                      LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                      LEFT JOIN role r ON r.role_id = ur.role_id
                      WHERE ua.user_id = @id
                      LIMIT 1",
                    conn);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", userId);
                object? roleName = cmd.ExecuteScalar();
                return Convert.ToString(roleName) ?? "Staff";
            }

            private static object? ReadUserAuditSnapshot(MySqlConnection conn, int userId, MySqlTransaction? tx = null)
            {
                using var cmd = new MySqlCommand(
                    @"SELECT ua.user_id, ua.username, ua.first_name, ua.middle_name, ua.last_name, ua.full_name,
                             ua.email, ua.contact_no, ua.position, ua.department, ua.last_project, ua.is_active,
                             ua.photo_url, COALESCE(r.name, 'Staff') AS role_name
                      FROM user_account ua
                      LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                      LEFT JOIN role r ON r.role_id = ur.role_id
                      WHERE ua.user_id = @id
                      LIMIT 1",
                    conn);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", userId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                return new
                {
                    UserId = Convert.ToInt32(reader["user_id"]),
                    Username = Convert.ToString(reader["username"]) ?? string.Empty,
                    FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty,
                    MiddleName = Convert.ToString(reader["middle_name"]) ?? string.Empty,
                    LastName = Convert.ToString(reader["last_name"]) ?? string.Empty,
                    FullName = Convert.ToString(reader["full_name"]) ?? string.Empty,
                    Email = Convert.ToString(reader["email"]) ?? string.Empty,
                    ContactNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
                    Position = Convert.ToString(reader["position"]) ?? string.Empty,
                    Department = Convert.ToString(reader["department"]) ?? string.Empty,
                    LastProject = Convert.ToString(reader["last_project"]) ?? string.Empty,
                    IsActive = reader["is_active"] != DBNull.Value && Convert.ToInt32(reader["is_active"]) == 1,
                    PhotoUrl = Convert.ToString(reader["photo_url"]) ?? string.Empty,
                    Role = Convert.ToString(reader["role_name"]) ?? "Staff"
                };
            }
        }
    }
}
