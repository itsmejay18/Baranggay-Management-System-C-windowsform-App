using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.ViewModels.Navigation;

/// <summary>
/// Abstract base class for fullscreen form content panels.
/// Provides common form infrastructure including dirty state tracking,
/// validation, async save with timeout, and unsaved changes confirmation.
///
/// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 10.5
/// </summary>
public abstract class FullscreenFormBase : UserControl
{
    private bool _isDirty;
    private bool _isValid;
    private bool _isSaving;
    private bool _hasValidationErrors;

    /// <summary>
    /// Indicates whether the form has unsaved modifications.
    /// Setting this property fires the DirtyStateChanged event when the value changes.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                DirtyStateChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Indicates whether the form is currently in a valid state.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        protected set
        {
            if (_isValid != value)
            {
                _isValid = value;
            }
        }
    }

    /// <summary>
    /// Indicates whether a save operation is currently in progress.
    /// Used to prevent duplicate submissions (Requirement 6.6).
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            UpdateSaveButtonState();
        }
    }

    /// <summary>
    /// Indicates whether the form currently has validation errors.
    /// When true, form submission is prevented (Requirement 10.5).
    /// Updated by <see cref="ValidateAndDisplayErrors"/> and <see cref="TrySaveAsync"/>.
    /// </summary>
    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        private set
        {
            if (_hasValidationErrors != value)
            {
                _hasValidationErrors = value;
                UpdateSaveButtonState();
                ValidationStateChanged?.Invoke(this, !value);
            }
        }
    }

    /// <summary>
    /// Optional reference to the save button control.
    /// When set, the save button is automatically disabled during save operations,
    /// when validation errors are present, or re-enabled when the operation completes.
    /// Subclasses should set this in their constructor or Loaded event.
    /// </summary>
    protected Button? SaveButton { get; set; }

    /// <summary>
    /// Optional reference to the FormValidationPanel control.
    /// When set, validation errors are automatically displayed/cleared by TrySaveAsync
    /// and ValidateAndDisplayErrors.
    /// Subclasses should set this in their constructor or Loaded event.
    /// </summary>
    protected FormValidationPanel? ValidationPanel { get; set; }

    /// <summary>
    /// Raised when the dirty state changes. The bool argument is the new IsDirty value.
    /// </summary>
    public event EventHandler<bool>? DirtyStateChanged;

    /// <summary>
    /// Raised when a save operation completes successfully.
    /// </summary>
    public event EventHandler? SaveCompleted;

    /// <summary>
    /// Raised when the validation state changes. The bool argument is the new IsValid value
    /// (true = no errors, false = has errors).
    /// </summary>
    public event EventHandler<bool>? ValidationStateChanged;

    /// <summary>
    /// Validates the form fields and returns whether the form is valid.
    /// Implementations should populate validation error messages that can be
    /// retrieved via <see cref="GetValidationErrors"/>.
    /// </summary>
    /// <returns>True if the form is valid; false otherwise.</returns>
    protected abstract bool ValidateForm();

    /// <summary>
    /// Persists the form data asynchronously.
    /// Called only after ValidateForm() returns true.
    /// </summary>
    /// <returns>True if the save succeeded; false otherwise.</returns>
    protected abstract Task<bool> SaveAsync();

    /// <summary>
    /// Resets the form to its initial state, clearing all fields and validation errors.
    /// </summary>
    protected abstract void ResetForm();

    /// <summary>
    /// Discards all in-memory form data by resetting the form and clearing dirty state.
    /// This is a public entry point for external callers (e.g., SessionSecurityIntegration)
    /// that need to discard form data on logout without submitting pending changes.
    /// 
    /// Requirement 10.2: Discard the in-memory form data and navigate to login
    /// without submitting any pending changes.
    /// </summary>
    public void DiscardFormData()
    {
        IsDirty = false;
        IsValid = false;
        HasValidationErrors = false;
        ValidationPanel?.Clear();
        ResetForm();
    }

    /// <summary>
    /// Returns the current list of validation error messages.
    /// Subclasses should override this to provide specific error messages
    /// for display in the FormValidationPanel.
    /// Default implementation returns an empty list.
    /// </summary>
    /// <returns>A list of validation error message strings.</returns>
    protected virtual IReadOnlyList<string> GetValidationErrors()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates all form fields and displays any errors inline using the FormValidationPanel.
    /// Call this method from subclasses when input fields change (e.g., on TextChanged or LostFocus)
    /// to provide real-time inline validation feedback.
    /// 
    /// Requirement 10.5: Validate all user input fields using FormValidationPanel and prevent
    /// form submission when validation errors are present.
    /// </summary>
    /// <returns>True if the form is valid (no errors); false if validation errors exist.</returns>
    public bool ValidateAndDisplayErrors()
    {
        bool valid = ValidateForm();
        IsValid = valid;

        if (!valid)
        {
            var errors = GetValidationErrors();
            if (ValidationPanel != null && errors.Any())
            {
                ValidationPanel.ShowErrors(errors);
            }
            else if (ValidationPanel != null)
            {
                ValidationPanel.ShowError("Please correct the validation errors before saving.");
            }

            HasValidationErrors = true;
        }
        else
        {
            // Clear validation panel when all errors are resolved
            ValidationPanel?.Clear();
            HasValidationErrors = false;
        }

        UpdateSaveButtonState();
        return valid;
    }

    /// <summary>
    /// Marks the form as dirty and optionally triggers inline validation.
    /// Call this from subclasses when any input field value changes.
    /// This provides the recommended pattern for wiring up field change events.
    /// 
    /// Requirement 10.5: Validate all user input fields using FormValidationPanel.
    /// </summary>
    /// <param name="validateImmediately">
    /// If true, triggers inline validation immediately (recommended for LostFocus events).
    /// If false, only marks the form as dirty without validating (recommended for TextChanged events
    /// to avoid excessive validation during typing).
    /// </param>
    protected void MarkFieldDirty(bool validateImmediately = false)
    {
        IsDirty = true;

        if (validateImmediately)
        {
            ValidateAndDisplayErrors();
        }
    }

    /// <summary>
    /// Attempts to save the form with the full save workflow:
    /// 1. Disables save button on activation (Req 6.1, 6.6)
    /// 2. Calls ValidateForm() before SaveAsync() (Req 6.1)
    /// 3. On validation failure: shows FormValidationPanel errors, re-enables save button (Req 6.2)
    /// 4. On save success: clears validation messages, sets IsDirty to false, fires SaveCompleted,
    ///    shows success toast, invokes OnSaved callback (Req 6.3, 6.5)
    /// 5. On save failure/timeout (30s): shows error toast, re-enables save button,
    ///    preserves form input (Req 6.4)
    /// 6. Prevents duplicate submissions while save is in progress (Req 6.6)
    /// </summary>
    /// <returns>True if the save completed successfully; false otherwise.</returns>
    public async Task<bool> TrySaveAsync()
    {
        // Requirement 6.6: Prevent duplicate submissions while save is in progress
        if (IsSaving)
        {
            return false;
        }

        // Requirement 10.5: Prevent form submission when validation errors are present.
        // This guards against programmatic calls to TrySaveAsync() when the form is invalid.
        if (HasValidationErrors)
        {
            return false;
        }

        try
        {
            // Requirement 6.1/6.6: Disable save control on activation
            IsSaving = true;

            // Step 1: Validate the form (Requirement 6.1: invoke ValidateForm() before persisting)
            bool valid = ValidateForm();
            IsValid = valid;

            if (!valid)
            {
                // Requirement 6.2: Display validation error messages using FormValidationPanel,
                // re-enable save control, remain on current form
                var errors = GetValidationErrors();
                if (ValidationPanel != null && errors.Any())
                {
                    ValidationPanel.ShowErrors(errors);
                }
                else if (ValidationPanel != null)
                {
                    ValidationPanel.ShowError("Please correct the validation errors before saving.");
                }

                // Requirement 10.5: Mark that validation errors are present to prevent
                // further submission attempts until errors are resolved
                HasValidationErrors = true;

                return false;
            }

            // Step 2: Attempt save with 30-second timeout (Requirement 6.4)
            // Validation passed — clear any previous validation error state
            HasValidationErrors = false;
            ValidationPanel?.Clear();

            bool saveResult;
            try
            {
                var saveTask = SaveAsync();
                var completedTask = await Task.WhenAny(saveTask, Task.Delay(TimeSpan.FromSeconds(30)));

                if (completedTask != saveTask)
                {
                    // Requirement 6.4: Timeout — show error toast, re-enable save, preserve input
                    ToastService.Error("Save Failed", "The save operation timed out after 30 seconds.");
                    return false;
                }

                saveResult = await saveTask;
            }
            catch (OperationCanceledException)
            {
                // Requirement 6.4: Timeout/cancellation — show error toast, preserve input
                ToastService.Error("Save Failed", "The save operation timed out after 30 seconds.");
                return false;
            }
            catch (Exception ex)
            {
                // Requirement 6.4: Exception — show error toast, re-enable save, preserve input
                ToastService.Error("Save Failed", ex.Message);
                return false;
            }

            if (!saveResult)
            {
                // Requirement 6.4: Save returned false — show error toast, preserve input
                ToastService.Error("Save Failed", "The save operation did not complete successfully.");
                return false;
            }

            // Step 3: Save succeeded (Requirements 6.3, 6.5)
            // Requirement 6.3: Clear any previously displayed FormValidationPanel messages
            ValidationPanel?.Clear();

            // Requirement 6.3: Set IsDirty to false
            IsDirty = false;

            // Requirement 6.3: Fire the SaveCompleted event
            SaveCompleted?.Invoke(this, EventArgs.Empty);

            // Requirement 6.3: Display a success toast notification
            ToastService.Success("Saved", "Record saved successfully.");

            // Requirement 6.5: Invoke the OnSaved callback to allow origin page to refresh
            FullscreenNavigationExtensions.InvokeOnSavedCallback();

            return true;
        }
        finally
        {
            // Re-enable save button (whether success or failure)
            // On success: IsSaving = false re-enables the button
            // On failure (Req 6.2, 6.4): re-enable save control
            IsSaving = false;
        }
    }

    /// <summary>
    /// Updates the save button's enabled state based on the current IsSaving state
    /// and validation errors.
    /// Requirement 6.1: Disable save control on activation.
    /// Requirement 6.2/6.4: Re-enable save control on validation failure or save failure.
    /// Requirement 10.5: Prevent form submission when validation errors are present.
    /// </summary>
    private void UpdateSaveButtonState()
    {
        if (SaveButton != null)
        {
            // Disable save button when saving is in progress OR when validation errors exist
            SaveButton.IsEnabled = !_isSaving && !_hasValidationErrors;
        }
    }

    /// <summary>
    /// Shows a confirmation dialog if the form has unsaved changes.
    /// Returns true if it is safe to discard (no changes or user confirmed discard).
    /// Returns false if the user chose to keep editing.
    /// </summary>
    /// <returns>True if navigation should proceed; false if it should be cancelled.</returns>
    public bool ConfirmDiscard()
    {
        if (!IsDirty)
        {
            return true;
        }

        var owner = Window.GetWindow(this);
        return ConfirmationDialog.Show(
            owner,
            "Unsaved Changes",
            "You have unsaved changes. Are you sure you want to go back? Your changes will be lost.",
            "Discard Changes",
            "Keep Editing",
            ConfirmationType.Warning);
    }
}
