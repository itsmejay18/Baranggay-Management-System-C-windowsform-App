using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;
using System;
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

            public void HandleLogin()
            {
                string username = _form.txtUsername.Text;
                string password = _form.txtPassword.Text;
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
                     ORDER BY (r.name = 'Admin') DESC
                     LIMIT 1";

                using (MySqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        UserSession.UserId = Convert.ToInt32(reader["user_id"]);
                        UserSession.BarangayId = reader["barangay_id"] == DBNull.Value
                            ? SchemaDefaults.DefaultBarangayId
                            : Convert.ToInt32(reader["barangay_id"]);
                        UserSession.Role = Convert.ToString(reader["role"]) ?? string.Empty;
                        UserSession.Username = username;
                        Permissions.Refresh();

                        if (UserSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            AdminDashboard admin = new AdminDashboard();
                            admin.Show();
                        }
                        else
                        {
                            StaffDashboard staff = new StaffDashboard();
                            staff.Show();
                        }

                        _form.Hide();
                    }
                    else
                    {
                        ControllerDialogs.Warning("Invalid username or password");
                    }
                }
            }

            public void HandleRegister()
            {
                RegisterForm registerForm = new RegisterForm();
                registerForm.Show();
                _form.Hide();
            }

            public void HandleLoad()
            {
                _form.StartFadeIn();
            }
        }
    }
}
