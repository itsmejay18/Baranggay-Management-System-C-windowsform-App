using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class ResidentModuleControl
    {
        private sealed class ResidentModuleController
        {
            private readonly ResidentModuleControl _form;

            public ResidentModuleController(ResidentModuleControl form)
            {
                _form = form;
            }

            public void HandleAddResident(object? sender, EventArgs e)
            {
                if (!Permissions.CanCreateResidents)
                {
                    ControllerDialogs.Warning("You do not have permission to add residents.");
                    return;
                }

                using var form = new ResidentForm("Add Resident");
                if (form.ShowDialog(_form.FindForm()) != DialogResult.OK) return;
                ResidentDto resident = form.Resident;
                var householdValidation = ValidationService.ValidateHouseholdConsistency(resident);
                if (!householdValidation.IsValid)
                {
                    ControllerDialogs.Warning(householdValidation.Message, householdValidation.Title);
                    return;
                }

                var duplicateValidation = ValidationService.ValidateResidentDuplicate(resident);
                if (!duplicateValidation.IsValid)
                {
                    ControllerDialogs.Warning(duplicateValidation.Message, duplicateValidation.Title);
                    return;
                }

                try
                {
                    _form.InsertResident(resident);
                    _form.LoadResidents();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to add resident.");
                }
            }

            public void HandleUpdateResident(object? sender, EventArgs e)
            {
                if (!Permissions.CanUpdateResidents)
                {
                    ControllerDialogs.Warning("You do not have permission to update residents.");
                    return;
                }

                var selectionValidation = ValidationService.ValidateResidentSelection(
                    _form._selectedResidentId,
                    "Please select a resident row first.");
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }

                _form.OpenResidentDetailsModal(readOnly: false, initialTabIndex: 0);
            }

            public void HandleRefreshResidents(object? sender, EventArgs e)
            {
                _form.LoadResidents();
            }

            public void HandleDeleteResident(object? sender, EventArgs e)
            {
                if (!Permissions.CanDeleteResidents)
                {
                    ControllerDialogs.Warning("Only Admin users can delete residents.");
                    return;
                }

                var selected = _form.GetSelectedResident();
                if (selected?.Id == null) return;

                var confirm = ControllerDialogs.Confirm(
                    $"Delete resident {selected.FirstName} {selected.LastName}?\n\nThis is a soft-delete and can be restored later.",
                    "Confirm Delete");

                if (confirm != DialogResult.Yes) return;

                string? reason = ControllerDialogs.Prompt(
                    "Reason for deleting this resident (required):",
                    "Delete Resident");
                if (reason == null) return;
                reason = reason.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    ControllerDialogs.Warning("Deletion reason is required.");
                    return;
                }

                try
                {
                    _form.DeleteResident(selected.Id.Value, reason);
                    _form.LogActivity(
                        selected.Id.Value,
                        "Residents",
                        "Deleted",
                        $"{selected.FirstName} {selected.LastName}".Trim() + " | Reason: " + reason);
                    _form.LoadResidents();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to delete resident.");
                }
            }

            public void HandleRestoreResident(object? sender, EventArgs e)
            {
                if (!Permissions.CanDeleteResidents)
                {
                    ControllerDialogs.Warning("Only Admin users can restore residents.");
                    return;
                }

                if (!_form._showDeletedResidents)
                {
                    ControllerDialogs.Warning("Enable 'Show deleted' to restore a resident.");
                    return;
                }

                var selected = _form.GetSelectedResident();
                if (selected?.Id == null) return;

                var confirm = ControllerDialogs.Confirm(
                    $"Restore resident {selected.FirstName} {selected.LastName}?",
                    "Confirm Restore");

                if (confirm != DialogResult.Yes) return;

                try
                {
                    _form.RestoreResident(selected.Id.Value);
                    _form.LogActivity(selected.Id.Value, "Residents", "Restored",
                        $"{selected.FirstName} {selected.LastName}".Trim());
                    _form.LoadResidents();
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to restore resident.");
                }
            }

            public void HandleQuickEdit(object? sender, EventArgs e)
            {
                HandleUpdateResident(sender, e);
            }

            public void HandleResidentAttachments(object? sender, EventArgs e)
            {
                if (_form._selectedResidentId == null)
                {
                    ControllerDialogs.Warning("Select a resident first.");
                    return;
                }

                _form.OpenAttachmentManager(
                    AttachmentEntityType.Resident,
                    _form._selectedResidentId.Value,
                    _form.GetResidentFullName());
            }

            public void HandleBlotterAttachments(object? sender, EventArgs e)
            {
                if (_form._selectedBlotterId == null)
                {
                    ControllerDialogs.Warning("Select a blotter record first.");
                    return;
                }

                _form.OpenAttachmentManager(
                    AttachmentEntityType.Case,
                    _form._selectedBlotterId.Value,
                    $"Case #{_form._selectedBlotterId.Value}");
            }

            public void HandleCertAttachments(object? sender, EventArgs e)
            {
                if (_form._selectedCertificateId == null)
                {
                    ControllerDialogs.Warning("Select a certificate first.");
                    return;
                }

                string certNo = _form._certNumber.Text;
                if (string.IsNullOrWhiteSpace(certNo) || certNo == "-")
                {
                    certNo = $"#{_form._selectedCertificateId.Value}";
                }

                _form.OpenAttachmentManager(
                    AttachmentEntityType.Certificate,
                    _form._selectedCertificateId.Value,
                    certNo);
            }

            public void HandlePhotoUpload(object? sender, EventArgs e)
            {
                _form.UploadResidentPhoto();
            }

            public void HandlePhotoRemove(object? sender, EventArgs e)
            {
                _form.RemoveResidentPhoto();
            }

            public void HandleSearchClear(object? sender, EventArgs e)
            {
                _form._searchBox.Text = string.Empty;
                _form.ApplyResidentSearch();
            }

            public void HandleFileBlotter(object? sender, EventArgs e)
            {
                if (!Permissions.CanCreateBlotter)
                {
                    ControllerDialogs.Warning("You do not have permission to file blotter cases.");
                    return;
                }

                var validation = ValidationService.ValidateResidentSelection(
                    _form._selectedResidentId,
                    "Select a resident before filing a blotter.");
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }
                int selectedResidentId = _form._selectedResidentId ?? throw new InvalidOperationException("Resident selection is required.");

                var suggestions = _form.LoadResidentNameSuggestions();
                using var form = new BlotterForm(selectedResidentId, _form.GetResidentFullName(), suggestions);
                if (form.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                if (!string.IsNullOrWhiteSpace(form.Blotter.RespondentName))
                {
                    RepeatRespondentCounts repeat = RepeatRespondentService.GetCountsForRespondent(
                        form.Blotter.RespondentResidentId,
                        form.Blotter.RespondentName);

                    if (repeat.ActiveCases >= 1)
                    {
                        string displayName = form.Blotter.RespondentName.Trim();
                        var confirm = ControllerDialogs.Confirm(
                            $"'{displayName}' already has {repeat.TotalCases} blotter case(s) ({repeat.ActiveCases} active). Continue filing?",
                            "Potential Repeat Respondent");
                        if (confirm != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                }

                try
                {
                    _form.InsertBlotter(form.Blotter);
                    _form.LoadBlottersForResident(selectedResidentId);
                    _form.LoadResidentHistory(selectedResidentId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to file blotter.");
                }
            }

            public void HandleRefreshBlotter(object? sender, EventArgs e)
            {
                if (_form._selectedResidentId == null)
                {
                    _form.ClearBlotters();
                    return;
                }

                _form.LoadBlottersForResident(_form._selectedResidentId.Value);
            }

            public void HandleOpenBlotter(object? sender, EventArgs e)
            {
                if (_form._selectedResidentId == null)
                {
                    ControllerDialogs.Warning("Select a resident first.");
                    return;
                }

                var selectedBlotter = _form.GetSelectedBlotterRecord();
                if (selectedBlotter == null)
                {
                    ControllerDialogs.Warning("Select a blotter record first.");
                    return;
                }

                var suggestions = _form.LoadResidentNameSuggestions();
                using var form = new BlotterForm(
                    _form._selectedResidentId.Value,
                    _form.GetResidentFullName(),
                    suggestions,
                    selectedBlotter.BlotterId);

                form.LoadExistingBlotterForReview(
                    selectedBlotter.RespondentName,
                    selectedBlotter.IncidentType,
                    selectedBlotter.IncidentDate,
                    selectedBlotter.IncidentTime,
                    selectedBlotter.IncidentLocation,
                    selectedBlotter.Witnesses,
                    selectedBlotter.ActionTaken,
                    selectedBlotter.IncidentDetails,
                    selectedBlotter.ResolutionDetails,
                    selectedBlotter.Status);

                form.ShowDialog(_form.FindForm());
                if (form.WasUpdated)
                {
                    _form.LoadBlottersForResident(_form._selectedResidentId.Value);
                    _form.LoadResidentHistory(_form._selectedResidentId.Value);
                }
            }

            public void HandleBlotterSave(object? sender, EventArgs e)
            {
                if (!Permissions.CanCreateBlotter)
                {
                    ControllerDialogs.Warning("You do not have permission to file blotter cases.");
                    return;
                }

                var residentValidation = ValidationService.ValidateResidentSelection(
                    _form._selectedResidentId,
                    "Select a resident before filing a blotter.");
                if (!residentValidation.IsValid)
                {
                    ControllerDialogs.Warning(residentValidation.Message, residentValidation.Title);
                    return;
                }
                int selectedResidentId = _form._selectedResidentId ?? throw new InvalidOperationException("Resident selection is required.");

                var blotter = new BlotterDto
                {
                    ComplainantId = selectedResidentId,
                    RespondentName = _form._blotterRespondent.Text.Trim(),
                    IncidentType = _form._blotterIncidentType.Text.Trim(),
                    IncidentDate = _form._blotterIncidentDate.Value.Date,
                    IncidentDetails = _form._blotterDetails.Text.Trim(),
                    Status = (_form._blotterStatus.SelectedItem?.ToString() ?? "Ongoing"),
                    RecordedBy = UserSession.UserId
                };

                var blotterValidation = ValidationService.ValidateBlotterQuickEntry(blotter);
                if (!blotterValidation.IsValid)
                {
                    ControllerDialogs.Warning(blotterValidation.Message, blotterValidation.Title);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(blotter.RespondentName))
                {
                    RepeatRespondentCounts repeat = RepeatRespondentService.GetCountsForRespondent(
                        blotter.RespondentResidentId,
                        blotter.RespondentName);

                    if (repeat.ActiveCases >= 1)
                    {
                        string displayName = blotter.RespondentName.Trim();
                        var confirm = ControllerDialogs.Confirm(
                            $"'{displayName}' already has {repeat.TotalCases} blotter case(s) ({repeat.ActiveCases} active). Continue filing?",
                            "Potential Repeat Respondent");
                        if (confirm != DialogResult.Yes)
                        {
                            return;
                        }
                    }
                }

                try
                {
                    _form.InsertBlotter(blotter);
                    _form.ShowBlotterForm(false);
                    _form.LoadBlottersForResident(selectedResidentId);
                    _form.LoadResidentHistory(selectedResidentId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to save blotter.");
                }
            }

            public void HandleBlotterCancel(object? sender, EventArgs e)
            {
                _form.ShowBlotterForm(false);
            }

            public void HandleCertNew(object? sender, EventArgs e)
            {
                if (!Permissions.CanRequestCertificates)
                {
                    ControllerDialogs.Warning("You do not have permission to create certificate requests.");
                    return;
                }

                var validation = ValidationService.ValidateResidentSelection(
                    _form._selectedResidentId,
                    "Select a resident before adding a certificate.");
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                using var form = new Certification(CertificateDialogMode.Request, _form.GetResidentFullName());
                if (form.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                try
                {
                    var newId = _form.CreateCertificateRequest(form.Entry);
                    _form.RefreshCertificates(newId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to save certificate request.");
                }
            }

            public void HandleCertEdit(object? sender, EventArgs e)
            {
                if (!Permissions.CanEditCertificateRequests)
                {
                    ControllerDialogs.Warning("You do not have permission to edit certificate requests.");
                    return;
                }

                var selectionValidation = ValidationService.ValidateCertificateSelection(_form._selectedCertificateId);
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }
                int selectedCertificateId = _form._selectedCertificateId ?? throw new InvalidOperationException("Certificate selection is required.");

                if (!string.Equals(_form._certStatus.Text, "Requested", StringComparison.OrdinalIgnoreCase))
                {
                    ControllerDialogs.Warning("Only requested certificates can be edited.");
                    return;
                }

                var current = _form.BuildCertificateEntryFromDetails();
                using var form = new Certification(CertificateDialogMode.EditRequest, _form.GetResidentFullName(), current);
                if (form.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                try
                {
                    _form.UpdateCertificateRequest(selectedCertificateId, form.Entry);
                    _form.RefreshCertificates(selectedCertificateId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to update certificate request.");
                }
            }

            public void HandleCertApprove(object? sender, EventArgs e)
            {
                if (!Permissions.CanApproveCertificates)
                {
                    ControllerDialogs.Warning("Only Admin users can approve certificates.");
                    return;
                }

                var selectionValidation = ValidationService.ValidateCertificateSelection(_form._selectedCertificateId);
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }
                int selectedCertificateId = _form._selectedCertificateId ?? throw new InvalidOperationException("Certificate selection is required.");

                if (!WorkflowRules.TryValidateCertificateTransition(_form._certStatus.Text, "Approved", out var certApproveMessage))
                {
                    ControllerDialogs.Warning(certApproveMessage);
                    return;
                }

                try
                {
                    using var conn = DBConnection.GetConnection();
                    conn.Open();
                    using var tx = conn.BeginTransaction();
                    int certificateId = _form._selectedCertificateId.Value;
                    object? beforeSnapshot = ReadCertificateAuditSnapshot(conn, certificateId, tx);

                    const string query = @"UPDATE document_request
                                       SET status='APPROVED',
                                           approved_by_user_id=@uid,
                                           approved_at=NOW()
                                       WHERE doc_request_id=@id
                                         AND status='SUBMITTED'";

                    using var cmd = new MySqlCommand(query, conn, tx);
                    cmd.Parameters.AddWithValue("@uid", UserSession.UserId);
                    cmd.Parameters.AddWithValue("@id", certificateId);
                    var rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        ControllerDialogs.Warning("Unable to approve. The certificate status may have changed.");
                        return;
                    }

                    _form.EnsureCertificateNumber(conn, tx, certificateId, DateTime.Today.Year);

                    object? afterSnapshot = ReadCertificateAuditSnapshot(conn, certificateId, tx);
                    AuditTrailService.LogTransactional(
                        conn,
                        tx,
                        "Certificates",
                        "document_request",
                        certificateId,
                        "APPROVE",
                        beforeSnapshot,
                        afterSnapshot,
                        "Certificate approved.");
                    tx.Commit();
                    _form.LogCertificateAudit(certificateId, "Approved", null);
                    _form.RefreshCertificates(certificateId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to approve certificate.");
                }
            }

            public void HandleCertIssue(object? sender, EventArgs e)
            {
                if (!Permissions.CanIssueCertificates)
                {
                    ControllerDialogs.Warning("Only Admin users can issue certificates.");
                    return;
                }

                var selectionValidation = ValidationService.ValidateCertificateSelection(_form._selectedCertificateId);
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }
                int selectedCertificateId = _form._selectedCertificateId ?? throw new InvalidOperationException("Certificate selection is required.");

                if (!WorkflowRules.TryValidateCertificateTransition(_form._certStatus.Text, "Issued", out var certIssueMessage))
                {
                    ControllerDialogs.Warning(certIssueMessage);
                    return;
                }

                var current = _form.BuildCertificateEntryFromDetails();
                using var form = new Certification(CertificateDialogMode.Issue, _form.GetResidentFullName(), current);
                if (form.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                try
                {
                    _form.IssueCertificate(selectedCertificateId, form.Entry);
                    _form.RefreshCertificates(selectedCertificateId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to issue certificate.");
                }
            }

            public void HandleCertCancel(object? sender, EventArgs e)
            {
                if (!Permissions.CanCancelCertificates)
                {
                    ControllerDialogs.Warning("Only Admin users can cancel certificates.");
                    return;
                }

                var selectionValidation = ValidationService.ValidateCertificateSelection(_form._selectedCertificateId);
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }
                int selectedCertificateId = _form._selectedCertificateId ?? throw new InvalidOperationException("Certificate selection is required.");

                if (!WorkflowRules.TryValidateCertificateTransition(_form._certStatus.Text, "Cancelled", out var certCancelMessage))
                {
                    ControllerDialogs.Warning(certCancelMessage);
                    return;
                }

                var confirm = ControllerDialogs.Confirm("Cancel this certificate request?", "Confirm Cancel");
                if (confirm != DialogResult.Yes) return;

                var reason = ControllerDialogs.Prompt(
                    "Enter cancellation reason (required):",
                    "Cancellation Reason");
                if (reason == null) return;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    ControllerDialogs.Warning("Cancellation reason is required.");
                    return;
                }

                try
                {
                    _form.CancelCertificate(selectedCertificateId, reason.Trim());
                    _form.RefreshCertificates(selectedCertificateId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to cancel certificate.");
                }
            }

            public void HandleCertRefresh(object? sender, EventArgs e)
            {
                if (_form._selectedResidentId == null)
                {
                    _form.ClearCertificates();
                    return;
                }

                _form.LoadCertificatesForResident(_form._selectedResidentId.Value);
            }

            public void HandleCertPrint(object? sender, EventArgs e)
            {
                var selectionValidation = ValidationService.ValidateCertificateSelection(_form._selectedCertificateId);
                if (!selectionValidation.IsValid)
                {
                    ControllerDialogs.Warning(selectionValidation.Message, selectionValidation.Title);
                    return;
                }
                int selectedCertificateId = _form._selectedCertificateId ?? throw new InvalidOperationException("Certificate selection is required.");

                if (!string.Equals(_form._certStatus.Text, "Issued", StringComparison.OrdinalIgnoreCase))
                {
                    ControllerDialogs.Warning("Only issued certificates can be printed.");
                    return;
                }

                using var printDoc = new System.Drawing.Printing.PrintDocument();
                printDoc.DocumentName = $"Certificate {_form._certNumber.Text}";
                printDoc.PrintPage += (_, args) =>
                {
                    _form.DrawCertificatePrint(args.Graphics!, args.MarginBounds);
                };

                using var dialog = new PrintDialog
                {
                    Document = printDoc,
                    UseEXDialog = true
                };

                if (dialog.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                try
                {
                    _form.EnsureCertificateVerificationToken(selectedCertificateId);

                    printDoc.Print();
                    _form.RegisterCertificatePrint(selectedCertificateId);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to print.");
                }
            }

            public void HandleCertExport(object? sender, EventArgs e)
            {
                if (!Permissions.CanExportCertificates)
                {
                    ControllerDialogs.Warning("Only Admin users can export certificates.");
                    return;
                }

                if (_form._certGrid.Rows.Count == 0)
                {
                    ControllerDialogs.Warning("No certificates to export.");
                    return;
                }

                using var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = "certificates.csv"
                };

                if (dialog.ShowDialog(_form.FindForm()) != DialogResult.OK) return;

                var columns = new System.Collections.Generic.List<DataGridViewColumn>();
                foreach (DataGridViewColumn column in _form._certGrid.Columns)
                {
                    if (column.Visible)
                    {
                        columns.Add(column);
                    }
                }

                var lines = new System.Collections.Generic.List<string>
                {
                    string.Join(",", columns.ConvertAll(c => ResidentModuleControl.EscapeCsv(c.HeaderText)))
                };

                foreach (DataGridViewRow row in _form._certGrid.Rows)
                {
                    if (row.IsNewRow) continue;

                    var values = new System.Collections.Generic.List<string>();
                    foreach (var column in columns)
                    {
                        values.Add(ResidentModuleControl.EscapeCsv(row.Cells[column.Name].Value));
                    }
                    lines.Add(string.Join(",", values));
                }

                try
                {
                    File.WriteAllLines(dialog.FileName, lines);
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to export.");
                }
            }

            public void HandleCertFilterClear(object? sender, EventArgs e)
            {
                _form._certSearchBox.Text = string.Empty;
                _form._certFilterType.SelectedIndex = 0;
                _form._certFilterStatus.SelectedIndex = 0;
                _form._certFilterFrom.Checked = false;
                _form._certFilterTo.Checked = false;
                _form.ApplyCertificateFilters();
            }

            public void HandleHistoryFilterClear(object? sender, EventArgs e)
            {
                _form._historySearchBox.Text = string.Empty;
                _form._historyFilterModule.SelectedIndex = 0;
                _form._historyFilterFrom.Checked = false;
                _form._historyFilterTo.Checked = false;
                _form.ApplyHistoryFilters();
            }

            private static object? ReadCertificateAuditSnapshot(MySqlConnection conn, int certificateId, MySqlTransaction? tx = null)
            {
                using var cmd = new MySqlCommand(
                    @"SELECT doc_request_id, resident_id, status, purpose, fee, or_number, document_no,
                             requested_at, approved_at, approved_by_user_id, released_at, released_by_user_id,
                             business_name, business_nature, remarks
                      FROM document_request
                      WHERE doc_request_id=@id
                      LIMIT 1",
                    conn);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@id", certificateId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return null;
                }

                return new
                {
                    CertificateId = Convert.ToInt32(reader["doc_request_id"]),
                    ResidentId = reader["resident_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["resident_id"]),
                    Status = Convert.ToString(reader["status"]) ?? string.Empty,
                    Purpose = Convert.ToString(reader["purpose"]) ?? string.Empty,
                    Fee = reader["fee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["fee"]),
                    OrNumber = Convert.ToString(reader["or_number"]) ?? string.Empty,
                    DocumentNo = Convert.ToString(reader["document_no"]) ?? string.Empty,
                    RequestedAt = reader["requested_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["requested_at"]),
                    ApprovedAt = reader["approved_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["approved_at"]),
                    ApprovedByUserId = reader["approved_by_user_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["approved_by_user_id"]),
                    ReleasedAt = reader["released_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["released_at"]),
                    ReleasedByUserId = reader["released_by_user_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["released_by_user_id"]),
                    BusinessName = Convert.ToString(reader["business_name"]) ?? string.Empty,
                    BusinessNature = Convert.ToString(reader["business_nature"]) ?? string.Empty,
                    Remarks = Convert.ToString(reader["remarks"]) ?? string.Empty
                };
            }
        }
    }
}
