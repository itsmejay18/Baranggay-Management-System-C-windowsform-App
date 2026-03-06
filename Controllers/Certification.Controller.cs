using System;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class Certification
    {
        private sealed class CertificationController
        {
            private readonly Certification _form;

            public CertificationController(Certification form)
            {
                _form = form;
            }

            public void HandleTypeChanged()
            {
                _form.UpdateBusinessFields();
                _form.UpdateIssueChecklist();
            }

            public void HandleIssueFieldChanged()
            {
                _form.UpdateIssueChecklist();
            }

            public void HandleSave()
            {
                var type = _form._type.SelectedItem?.ToString() ?? string.Empty;
                var validation = ValidationService.ValidateCertificateDialogSave(
                    type,
                    _form._purpose.Text,
                    _form._businessName.Text,
                    _form._businessNature.Text,
                    _form._fee.Value,
                    _form._orNumber.Text,
                    _form._paymentMethod.SelectedItem?.ToString(),
                    _form._issuedDate.Value.Date,
                    _form._mode);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                _form._entry.Type = type;
                _form._entry.Purpose = _form._purpose.Text.Trim();
                _form._entry.Fee = _form._fee.Value;
                _form._entry.OrNumber = _form._orNumber.Text.Trim();
                _form._entry.PaymentMethod = _form._paymentMethod.SelectedItem?.ToString() ?? string.Empty;
                _form._entry.IssuedDate = _form._mode == CertificateDialogMode.Issue ? _form._issuedDate.Value.Date : null;
                _form._entry.BusinessName = _form._businessName.Text.Trim();
                _form._entry.BusinessNature = _form._businessNature.Text.Trim();
                _form._entry.Remarks = _form._remarks.Text.Trim();

                _form.DialogResult = DialogResult.OK;
            }
        }
    }
}
