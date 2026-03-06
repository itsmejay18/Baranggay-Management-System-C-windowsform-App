using System;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class ProjectForm
    {
        private sealed class ProjectFormController
        {
            private readonly ProjectForm _form;

            public ProjectFormController(ProjectForm form)
            {
                _form = form;
            }

            public void Initialize()
            {
                if (!Permissions.CanManageProjects)
                {
                    ControllerDialogs.Warning("Only Admin users can manage projects.");
                    _form.Close();
                    return;
                }

                _form.cmbStatus.Items.AddRange(new object[] { "Planned", "Ongoing", "On hold", "Completed" });
                _form.cmbStatus.SelectedIndex = 0;

                UiTheme.StyleTextBox(_form.txtName);
                UiTheme.StyleTextBox(_form.txtLead);
                UiTheme.StyleTextBox(_form.txtRemarks);
                UiTheme.StyleComboBox(_form.cmbStatus);
                UiTheme.StylePrimaryButton(_form.btnSave);
                UiTheme.StyleSecondaryButton(_form.btnCancel);

                _form.btnSave.Click += (_, __) => SaveProject();
                _form.btnCancel.Click += (_, __) => _form.Close();
            }

            private void SaveProject()
            {
                if (!Permissions.CanManageProjects)
                {
                    ControllerDialogs.Warning("You do not have permission to manage projects.");
                    return;
                }

                string name = _form.GetNameText();
                var validation = ValidationService.ValidateProjectSave(name);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                string status = _form.GetStatus();
                decimal budget = _form.GetBudget();
                DateTime? startDate = _form.GetStartDate();
                DateTime? endDate = _form.GetEndDate();
                string lead = _form.GetLead();
                string remarks = _form.GetRemarks();

                try
                {
                    DbHelper.ExecuteNonQuery(
                        @"INSERT INTO projects (name, status, budget, start_date, end_date, lead, remarks)
                          VALUES (@name, @status, @budget, @start, @end, @lead, @remarks)",
                        cmd =>
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@status", status);
                            cmd.Parameters.AddWithValue("@budget", budget);
                            cmd.Parameters.AddWithValue("@start", (object?)startDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@end", (object?)endDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@lead", string.IsNullOrWhiteSpace(lead) ? DBNull.Value : lead);
                            cmd.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : remarks);
                        });

                    _form.CloseWithSuccess();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to save project.");
                }
            }
        }
    }
}
