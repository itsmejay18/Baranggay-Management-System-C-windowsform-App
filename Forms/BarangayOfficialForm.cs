using baranggaysystem1.Controllers;
using baranggaysystem1.Models;
using System;
using System.Windows.Forms;

namespace baranggaysystem1.Forms
{
    public partial class BarangayOfficialForm : Form
    {
        private readonly BarangayOfficialController _barangayOfficialController;
        public BarangayOfficialForm()
        {
            InitializeComponent();
            _barangayOfficialController = new BarangayOfficialController();
            LoadBarangayOfficials();
        }

        private void LoadBarangayOfficials()
        {
            dataGridView1.DataSource = _barangayOfficialController.GetBarangayOfficials();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var official = new BarangayOfficial
            {
                FirstName = firstNameTextBox.Text,
                LastName = lastNameTextBox.Text,
                Position = positionTextBox.Text,
                TermStart = termStartDateTimePicker.Value.ToShortDateString(),
                TermEnd = termEndDateTimePicker.Value.ToShortDateString()
            };
            _barangayOfficialController.AddBarangayOfficial(official);
            LoadBarangayOfficials();
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var official = (BarangayOfficial)dataGridView1.SelectedRows[0].DataBoundItem;
                official.FirstName = firstNameTextBox.Text;
                official.LastName = lastNameTextBox.Text;
                official.Position = positionTextBox.Text;
                official.TermStart = termStartDateTimePicker.Value.ToShortDateString();
                official.TermEnd = termEndDateTimePicker.Value.ToShortDateString();
                _barangayOfficialController.UpdateBarangayOfficial(official);
                LoadBarangayOfficials();
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var official = (BarangayOfficial)dataGridView1.SelectedRows[0].DataBoundItem;
                _barangayOfficialController.DeleteBarangayOfficial(official.OfficialId);
                LoadBarangayOfficials();
            }
        }
    }
}
