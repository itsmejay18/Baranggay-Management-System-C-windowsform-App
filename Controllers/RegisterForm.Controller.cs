using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class RegisterForm
    {
        private sealed class RegisterFormController
        {
            private readonly RegisterForm _form;

            public RegisterFormController(RegisterForm form)
            {
                _form = form;
            }

            public void HandleRegister()
            {
                bool hasExistingUsers = HasExistingUsers();
                if (hasExistingUsers && !Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only authorized users can create new accounts.");
                    return;
                }

                string username = _form.txtUsername.Text;
                string password = _form.txtPassword.Text;
                string role = _form.cmbRole.SelectedItem?.ToString() ?? string.Empty;
                string? photoPath = _form.GetPhotoPath();

                var validation = ValidationService.ValidateRegistration(username, password, role);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                string hashedPassword = PasswordHelper.HashPassword(password);

                using (MySqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    SchemaBootstrap.EnsureCoreDefaults(conn);
                    using var tx = conn.BeginTransaction();

                    int roleId = EnsureRole(conn, role, tx);

                    const string query = @"INSERT INTO user_account
                        (barangay_id, username, password_hash, full_name, is_active, photo_url, created_at, updated_at)
                        VALUES (@barangayId, @username, @password, @fullName, 1, @photo, NOW(), NOW())";

                    using var cmd = new MySqlCommand(query, conn, tx);
                    cmd.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);
                    cmd.Parameters.AddWithValue("@fullName", username);
                    cmd.Parameters.AddWithValue("@photo", string.IsNullOrWhiteSpace(photoPath) ? DBNull.Value : photoPath);
                    cmd.ExecuteNonQuery();

                    int userId = (int)cmd.LastInsertedId;

                    using var roleCmd = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@uid, @rid)", conn, tx);
                    roleCmd.Parameters.AddWithValue("@uid", userId);
                    roleCmd.Parameters.AddWithValue("@rid", roleId);
                    roleCmd.ExecuteNonQuery();

                    object? afterSnapshot = ReadUserAuditSnapshot(conn, userId, tx);
                    AuditTrailService.LogTransactional(
                        conn,
                        tx,
                        "Users",
                        "user_account",
                        userId,
                        "CREATE",
                        null,
                        afterSnapshot,
                        $"User account created with role '{role}'.");

                    tx.Commit();
                }

                ControllerDialogs.Info("User registered successfully!");
            }

            public void HandleBackToLogin()
            {
                Form1 loginForm = new Form1();
                loginForm.ShowDialog();
            }

            public void HandleLoad()
            {
                _form.StartFadeIn();
            }

            public void HandleUploadPhoto()
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

            public void HandleRemovePhoto()
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

            private static bool HasExistingUsers()
            {
                using var conn = DBConnection.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_account", conn);
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
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
