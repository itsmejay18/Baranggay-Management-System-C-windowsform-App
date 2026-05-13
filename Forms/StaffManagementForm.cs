using baranggaysystem1.Controllers;
using baranggaysystem1.Models;
using System;
using System.Windows.Forms;

namespace baranggaysystem1.Forms
{
    public partial class StaffManagementForm : Form
    {
        private readonly StaffController _staffController;
        public StaffManagementForm()
        {
            InitializeComponent();
            _staffController = new StaffController();
            LoadStaffs();
        }

        private void LoadStaffs()
        {
            dataGridView1.DataSource = _staffController.GetStaffs();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var staff = new Staff
            {
                FirstName = firstNameTextBox.Text,
                LastName = lastNameTextBox.Text,
                Position = positionTextBox.Text,
                ContactNumber = contactNumberTextBox.Text,
                Address = addressTextBox.Text
            };
            _staffController.AddStaff(staff);
            LoadStaffs();
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var staff = (Staff)dataGridView1.SelectedRows[0].DataBoundItem;
                staff.FirstName = firstNameTextBox.Text;
                staff.LastName = lastNameTextBox.Text;
                staff.Position = positionTextBox.Text;
                staff.ContactNumber = contactNumberTextBox.Text;
                staff.Address = addressTextBox.Text;
                _staffController.UpdateStaff(staff);
                LoadStaffs();
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var staff = (Staff)dataGridView1.SelectedRows[0].DataBoundItem;
                _staffController.DeleteStaff(staff.StaffId);
                LoadStaffs();
            }
        }
    }
}
