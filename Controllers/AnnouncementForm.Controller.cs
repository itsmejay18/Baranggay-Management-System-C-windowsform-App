using System;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class AnnouncementForm
    {
        private sealed class AnnouncementFormController
        {
            private readonly AnnouncementForm _form;

            public AnnouncementFormController(AnnouncementForm form)
            {
                _form = form;
            }

            public void Initialize()
            {
                if (!Permissions.CanManageAnnouncements)
                {
                    ControllerDialogs.Warning("Only Admin users can manage announcements.");
                    _form.Close();
                    return;
                }

                _form.cmbPriority.Items.AddRange(new object[] { "Low", "Normal", "High" });
                _form.cmbPriority.SelectedIndex = 1;

                _form.cmbStatus.Items.AddRange(new object[] { "Draft", "Published", "Archived" });
                _form.cmbStatus.SelectedIndex = 1;

                UiTheme.StyleTextBox(_form.txtTitle);
                UiTheme.StyleTextBox(_form.txtMessage);
                UiTheme.StyleComboBox(_form.cmbPriority);
                UiTheme.StyleComboBox(_form.cmbStatus);
                UiTheme.StylePrimaryButton(_form.btnSave);
                UiTheme.StyleSecondaryButton(_form.btnCancel);

                _form.btnSave.Click += (_, __) => SaveAnnouncement();
                _form.btnCancel.Click += (_, __) => _form.Close();
            }

            private void SaveAnnouncement()
            {
                if (!Permissions.CanManageAnnouncements)
                {
                    ControllerDialogs.Warning("You do not have permission to manage announcements.");
                    return;
                }

                string title = _form.GetTitleText();
                var validation = ValidationService.ValidateAnnouncementSave(title);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                string body = _form.GetMessageText();
                string priority = _form.GetPriority();
                string status = _form.GetStatus();
                int pinned = _form.GetPinned() ? 1 : 0;

                try
                {
                    DatabaseManager.Insert(
                        @"INSERT INTO announcements (title, body, priority, status, is_pinned)
                          VALUES (@title, @body, @priority, @status, @pinned)",
                        new[]
                        {
                            new DbParameterValue("@title", title),
                            new DbParameterValue("@body", body),
                            new DbParameterValue("@priority", priority),
                            new DbParameterValue("@status", status),
                            new DbParameterValue("@pinned", pinned)
                        });

                    _form.CloseWithSuccess();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to save announcement.");
                }
            }
        }
    }
}
