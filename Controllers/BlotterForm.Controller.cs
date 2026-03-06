using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    internal partial class BlotterForm
    {
        private sealed class BlotterFormController
        {
            private readonly BlotterForm _form;

            public BlotterFormController(BlotterForm form)
            {
                _form = form;
            }

            public void HandleRespondentModeChanged()
            {
                _form.UpdateRespondentMode();
            }

            public void HandleSave()
            {
                if (!_form.AnalysisBlotterId.HasValue && !Permissions.CanCreateBlotter)
                {
                    ControllerDialogs.Warning("You do not have permission to file blotter cases.");
                    return;
                }

                if (!_form.ValidateReviewInputs(showMessages: true, out string inlineMessage, out string inlineTitle))
                {
                    ControllerDialogs.Warning(inlineMessage, inlineTitle);
                    return;
                }

                var blotter = _form.Blotter;
                var validation = ValidationService.ValidateBlotterFormSave(
                    _form.rbResident.Checked,
                    _form.GetRespondentName(),
                    blotter.IncidentType,
                    blotter.IncidentLocation,
                    blotter.IncidentDetails,
                    blotter.IncidentDate,
                    blotter.Status,
                    blotter.ResolutionDetails);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                _form.DialogResult = DialogResult.OK;
                _form.Close();
            }

            public async Task HandleRunAiAnalysisAsync()
            {
                if (!_form.AnalysisBlotterId.HasValue)
                {
                    ControllerDialogs.Warning("No blotter_id is attached to this form. Open an existing blotter record first.", "AI Analysis");
                    return;
                }

                int blotterId = _form.AnalysisBlotterId.Value;
                _form.SetAiBusy(true);

                try
                {
                    AiBlotterAnalysis analysis = await _form.AiService.AnalyzeBlotterAsync(blotterId).ConfigureAwait(true);
                    await _form.AiService.SaveAnalysisAsync(blotterId, analysis).ConfigureAwait(true);
                    _form.PopulateAiAnalysis(analysis);

                    bool failed = analysis.Summary.StartsWith("AI analysis failed:", StringComparison.OrdinalIgnoreCase);
                    string details = $"Category: {analysis.SuggestedCategory} ({analysis.CategoryConfidence:P0})\n" +
                                     $"Risk: {analysis.RiskLevel} ({analysis.RiskScore})\n" +
                                     $"Model: {analysis.Model}\n" +
                                     $"Processed: {analysis.ProcessedAt:yyyy-MM-dd HH:mm}";
                    CaseTimelineService.Log(
                        blotterId,
                        failed ? "AI_ANALYSIS_FAIL" : "AI_ANALYSIS",
                        failed ? "AI analysis failed" : "AI analysis completed",
                        details,
                        null,
                        null,
                        UserSession.UserId);
                    _form.ReloadTimeline();

                    if (!analysis.Summary.StartsWith("AI analysis failed:", StringComparison.OrdinalIgnoreCase))
                    {
                        ControllerDialogs.Info("AI analysis completed and saved.", "AI Analysis");
                    }
                    else
                    {
                        ControllerDialogs.Warning("AI returned invalid JSON. Fallback result was saved.", "AI Analysis");
                    }
                }
                catch (Exception ex)
                {
                    AiBlotterAnalysis failed = AiBlotterAnalysis.CreateFailed(ex.Message, _form.AiService.ModelName);

                    try
                    {
                        await _form.AiService.SaveAnalysisAsync(blotterId, failed).ConfigureAwait(true);
                        _form.PopulateAiAnalysis(failed);
                        CaseTimelineService.Log(
                            blotterId,
                            "AI_ANALYSIS_FAIL",
                            "AI analysis failed",
                            ex.Message,
                            null,
                            null,
                            UserSession.UserId);
                        _form.ReloadTimeline();
                    }
                    catch
                    {
                        // If save fails, we still show the failure summary in UI.
                        _form.PopulateAiAnalysis(failed);
                    }

                    ControllerDialogs.Error(ex, "AI analysis failed.", "AI Analysis");
                }
                finally
                {
                    _form.SetAiBusy(false);
                }
            }

            public async Task HandleUpdateStatusAsync()
            {
                if (!_form.AnalysisBlotterId.HasValue)
                {
                    ControllerDialogs.Warning("Open an existing blotter record first.", "Update Status");
                    return;
                }

                if (!Permissions.CanUpdateBlotterStatus)
                {
                    ControllerDialogs.Warning("Only Admin users can update blotter status.", "Update Status");
                    return;
                }

                string currentStatus = _form.GetCurrentStatus();
                if (!_form.IsStatusChanged())
                {
                    ControllerDialogs.Info("Status is already up to date.", "Update Status");
                    return;
                }

                string? referralDestination = null;
                if (currentStatus.Equals("Referred", StringComparison.OrdinalIgnoreCase))
                {
                    referralDestination = ControllerDialogs.Prompt(
                        "Enter referral destination (required):",
                        "Referral Destination");
                    if (referralDestination == null)
                    {
                        return;
                    }
                    referralDestination = referralDestination.Trim();
                    if (string.IsNullOrWhiteSpace(referralDestination))
                    {
                        ControllerDialogs.Warning("Referral destination is required.", "Update Status");
                        return;
                    }
                }

                var validation = ValidationService.ValidateBlotterStatusTransition(
                    _form.GetOriginalStatus(),
                    currentStatus,
                    _form.txtResolution.Text,
                    referralDestination);
                if (!validation.IsValid)
                {
                    ControllerDialogs.Warning(validation.Message, validation.Title);
                    return;
                }

                var confirm = ControllerDialogs.Confirm($"Update blotter status to '{currentStatus}'?", "Confirm Update");
                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                try
                {
                    await _form.UpdateStatusAsync(_form.AnalysisBlotterId.Value, currentStatus, referralDestination).ConfigureAwait(true);
                    _form.MarkStatusUpdated(currentStatus);
                    _form.ReloadTimeline();
                    ControllerDialogs.Info("Blotter status updated.", "Update Status");
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to update status.", "Update Status");
                }
            }

            public async Task HandleScheduleMediationAsync()
            {
                if (!_form.AnalysisBlotterId.HasValue)
                {
                    ControllerDialogs.Warning("Open an existing blotter record first.", "Schedule Mediation");
                    return;
                }

                if (!Permissions.CanUpdateBlotterStatus)
                {
                    ControllerDialogs.Warning("Only Admin users can schedule mediation.", "Schedule Mediation");
                    return;
                }

                var schedule = _form.PromptMediationSchedule();
                if (!schedule.HasValue)
                {
                    return;
                }

                try
                {
                    await _form.ScheduleMediationAsync(_form.AnalysisBlotterId.Value, schedule.Value.ScheduleAt, schedule.Value.Venue).ConfigureAwait(true);
                    _form.ReloadTimeline();
                    ControllerDialogs.Info("Mediation scheduled.", "Schedule Mediation");
                }
                catch (Exception ex)
                {
                    ControllerDialogs.Error(ex, "Unable to schedule mediation.", "Schedule Mediation");
                }
            }

            public void HandlePrint()
            {
                if (_form.AnalysisBlotterId.HasValue)
                {
                    CaseTimelineService.Log(
                        _form.AnalysisBlotterId.Value,
                        "PRINT_PREVIEW",
                        "Print preview opened",
                        null,
                        null,
                        null,
                        UserSession.UserId);
                    _form.ReloadTimeline();
                }
                _form.ShowPrintPreview();
            }
        }
    }
}



