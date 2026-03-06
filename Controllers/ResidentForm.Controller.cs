using System;
using System.IO;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    internal partial class ResidentForm
    {
        private sealed class ResidentFormController
        {
            private readonly ResidentForm _form;

            public ResidentFormController(ResidentForm form)
            {
                _form = form;
            }

            public void HandlePhotoUpload()
            {
                using var dialog = new OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                    Title = "Select a resident photo"
                };

                if (dialog.ShowDialog(_form) != DialogResult.OK) return;

                try
                {
                    _form._photoBytes = File.ReadAllBytes(dialog.FileName);
                    _form.UpdatePhotoPreview();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to read photo.", "Photo Error");
                }
            }

            public void HandlePhotoRemove()
            {
                _form._photoBytes = null;
                _form.UpdatePhotoPreview();
            }

            public void HandleSave()
            {
                var validation = ValidationService.ValidateResidentFormSave(
                    _form.txtFirstName.Text,
                    _form.txtLastName.Text,
                    _form.dtpBirthDate.Value.Date);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                ResidentDto resident = _form.Resident;
                var householdValidation = ValidationService.ValidateHouseholdConsistency(resident, resident.Id);
                if (!householdValidation.IsValid)
                {
                    ControllerDialogs.Warning(householdValidation.Message, householdValidation.Title);
                    return;
                }

                _form.DialogResult = DialogResult.OK;
                _form.Close();
            }
        }
    }
}
