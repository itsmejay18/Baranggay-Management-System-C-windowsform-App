using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Panels;

/// <summary>
/// Fullscreen form panel for Add/Edit resident operations.
/// Extends FullscreenFormBase to provide dirty tracking, validation,
/// and async save workflow.
///
/// Requirements: 1.1, 2.6, 6.3, 6.5
/// </summary>
public partial class ResidentFormPanel : FullscreenFormBase
{
    private readonly ResidentsModuleDataService _dataService;
    private readonly ResidentDto _originalRecord;
    private readonly FormMode _mode;
    private byte[]? _photoBytes;
    private bool _isLoading;

    public ObservableCollection<LookupItem> PurokOptions { get; } = new();

    public string[] GenderOptions { get; } = new[] { "MALE", "FEMALE" };
    public string[] CivilStatusOptions { get; } = new[] { "SINGLE", "MARRIED", "WIDOWED", "SEPARATED" };
    public string[] BloodTypeOptions { get; } = new[] { "", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
    public string[] EducationOptions { get; } = new[] { "", "Elementary", "High School", "Vocational", "College", "Post-Graduate" };
    public string[] HouseholdRelationshipOptions { get; } = new[] { "", "Head", "Spouse", "Son", "Daughter", "Parent", "Sibling", "Grandchild", "Relative", "Boarder" };

    public ResidentFormPanel(FormMode mode, ResidentDto? existingResident = null)
    {
        InitializeComponent();

        _dataService = new ResidentsModuleDataService();
        _mode = mode;
        _originalRecord = existingResident != null ? CloneResident(existingResident) : new ResidentDto();
        _photoBytes = existingResident?.PhotoBytes;

        // Set up the validation panel reference from FullscreenFormBase
        ValidationPanel = validationPanel;

        InitializeComboBoxes();
        PopulateForm(existingResident);
        LoadPurokOptionsAsync(_originalRecord.PurokId);

        // Load existing photo if available
        if (_photoBytes != null && _photoBytes.Length > 0)
        {
            LoadPhotoFromBytes(_photoBytes);
        }
    }

    private void InitializeComboBoxes()
    {
        cmbGender.ItemsSource = GenderOptions;
        cmbCivilStatus.ItemsSource = CivilStatusOptions;
        cmbBloodType.ItemsSource = BloodTypeOptions;
        cmbEducation.ItemsSource = EducationOptions;
        cmbHouseholdRelationship.ItemsSource = HouseholdRelationshipOptions;
        cmbPurok.ItemsSource = PurokOptions;
    }

    private void PopulateForm(ResidentDto? resident)
    {
        _isLoading = true;
        try
        {
            if (resident != null)
            {
                txtFirstName.Text = resident.FirstName;
                txtLastName.Text = resident.LastName;
                txtMiddleName.Text = resident.MiddleName;
                txtSuffix.Text = resident.Suffix;
                cmbGender.SelectedItem = NormalizeGenderSelection(resident.Gender);
                cmbCivilStatus.SelectedItem = NormalizeCivilStatusSelection(resident.CivilStatus);
                dpBirthDate.SelectedDate = resident.DateOfBirth;
                txtNationality.Text = resident.Nationality;
                txtReligion.Text = resident.Religion;
                cmbBloodType.SelectedItem = resident.BloodType ?? "";
                txtOccupation.Text = resident.Occupation;
                cmbEducation.SelectedItem = resident.EducationalAttainment ?? "";
                txtPlaceOfBirth.Text = resident.PlaceOfBirth;
                txtEmail.Text = resident.EmailAddress;
                txtContact.Text = resident.ContactNo;
                txtHouseNo.Text = resident.HouseNo;
                txtStreet.Text = resident.Street;
                dpResidency.SelectedDate = resident.DateOfResidency;
                cmbHouseholdRelationship.SelectedItem = resident.HouseholdRelationship ?? "";
                txtPhilHealth.Text = resident.PhilHealthNo;
                txtSss.Text = resident.SssNo;
                txtTin.Text = resident.TinNo;
                txtVotersId.Text = resident.VotersIdNo;
                chkPwd.IsChecked = resident.IsPwd;
                chkSenior.IsChecked = resident.IsSenior;
                chk4Ps.IsChecked = resident.Is4PsBeneficiary;
                chkVoter.IsChecked = resident.IsRegisteredVoter;
                chkSoloParent.IsChecked = resident.IsSoloParent;
                chkYouth.IsChecked = resident.IsYouth;
                chkIndigent.IsChecked = resident.IsIndigent;
            }
            else
            {
                // Defaults for new resident
                cmbGender.SelectedItem = "MALE";
                cmbCivilStatus.SelectedItem = "SINGLE";
                dpBirthDate.SelectedDate = DateTime.Today;
                txtNationality.Text = "Filipino";
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void LoadPurokOptionsAsync(int? selectedPurokId)
    {
        try
        {
            var options = await _dataService.GetPurokOptionsAsync();
            PurokOptions.Clear();
            foreach (var item in options.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase))
            {
                PurokOptions.Add(item);
            }

            var selected = PurokOptions.FirstOrDefault(o => o.Id == selectedPurokId);
            if (selected == null && selectedPurokId.HasValue && selectedPurokId.Value > 0)
            {
                var fallback = new LookupItem(selectedPurokId.Value, $"Purok #{selectedPurokId.Value}");
                PurokOptions.Add(fallback);
                selected = fallback;
            }
            if (selected == null && PurokOptions.Count == 1)
            {
                selected = PurokOptions[0];
            }

            cmbPurok.SelectedItem = selected;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to load purok options for resident form panel.", ex);
        }
    }

    #region FullscreenFormBase Overrides

    protected override bool ValidateForm()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            errors.Add("First Name is required.");

        if (string.IsNullOrWhiteSpace(txtLastName.Text))
            errors.Add("Last Name is required.");

        if (dpBirthDate.SelectedDate == null)
            errors.Add("Birth Date is required.");
        else if (dpBirthDate.SelectedDate > DateTime.Today)
            errors.Add("Birth Date cannot be in the future.");

        if (cmbPurok.SelectedItem == null)
            errors.Add("Purok / Zone is required.");

        _validationErrors = errors;
        IsValid = errors.Count == 0;
        return IsValid;
    }

    private List<string> _validationErrors = new();

    protected override IReadOnlyList<string> GetValidationErrors()
    {
        return _validationErrors;
    }

    protected override async Task<bool> SaveAsync()
    {
        var selectedPurok = cmbPurok.SelectedItem as LookupItem;

        var dto = new ResidentDto
        {
            Id = _originalRecord.Id,
            FirstName = txtFirstName.Text.Trim(),
            MiddleName = txtMiddleName.Text.Trim(),
            LastName = txtLastName.Text.Trim(),
            Suffix = txtSuffix.Text.Trim(),
            Gender = cmbGender.SelectedItem?.ToString() ?? "MALE",
            DateOfBirth = dpBirthDate.SelectedDate ?? DateTime.Today,
            CivilStatus = cmbCivilStatus.SelectedItem?.ToString() ?? "SINGLE",
            ContactNo = txtContact.Text.Trim(),
            IsPwd = chkPwd.IsChecked == true,
            IsSenior = chkSenior.IsChecked == true,
            Is4PsBeneficiary = chk4Ps.IsChecked == true,
            IsRegisteredVoter = chkVoter.IsChecked == true,
            IsSoloParent = chkSoloParent.IsChecked == true,
            IsYouth = chkYouth.IsChecked == true,
            IsIndigent = chkIndigent.IsChecked == true,
            BarangayId = _dataService.BarangayId,
            PurokId = selectedPurok?.Id,
            HouseholdId = _originalRecord.HouseholdId,
            PhotoBytes = _photoBytes ?? _originalRecord.PhotoBytes,
            Status = string.IsNullOrWhiteSpace(_originalRecord.Status) ? "ACTIVE" : _originalRecord.Status,
            Occupation = txtOccupation.Text.Trim(),
            EducationalAttainment = cmbEducation.SelectedItem?.ToString() ?? "",
            Nationality = txtNationality.Text.Trim(),
            Religion = txtReligion.Text.Trim(),
            BloodType = cmbBloodType.SelectedItem?.ToString() ?? "",
            EmailAddress = txtEmail.Text.Trim(),
            PlaceOfBirth = txtPlaceOfBirth.Text.Trim(),
            HouseNo = txtHouseNo.Text.Trim(),
            Street = txtStreet.Text.Trim(),
            PhilHealthNo = txtPhilHealth.Text.Trim(),
            SssNo = txtSss.Text.Trim(),
            TinNo = txtTin.Text.Trim(),
            VotersIdNo = txtVotersId.Text.Trim(),
            DateOfResidency = dpResidency.SelectedDate,
            HouseholdRelationship = cmbHouseholdRelationship.SelectedItem?.ToString() ?? ""
        };

        await _dataService.SaveResidentAsync(dto);
        return true;
    }

    protected override void ResetForm()
    {
        _isLoading = true;
        try
        {
            txtFirstName.Text = string.Empty;
            txtLastName.Text = string.Empty;
            txtMiddleName.Text = string.Empty;
            txtSuffix.Text = string.Empty;
            cmbGender.SelectedItem = "MALE";
            cmbCivilStatus.SelectedItem = "SINGLE";
            dpBirthDate.SelectedDate = DateTime.Today;
            txtNationality.Text = "Filipino";
            txtReligion.Text = string.Empty;
            cmbBloodType.SelectedItem = "";
            txtOccupation.Text = string.Empty;
            cmbEducation.SelectedItem = "";
            txtPlaceOfBirth.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtContact.Text = string.Empty;
            txtHouseNo.Text = string.Empty;
            txtStreet.Text = string.Empty;
            dpResidency.SelectedDate = null;
            cmbHouseholdRelationship.SelectedItem = "";
            txtPhilHealth.Text = string.Empty;
            txtSss.Text = string.Empty;
            txtTin.Text = string.Empty;
            txtVotersId.Text = string.Empty;
            chkPwd.IsChecked = false;
            chkSenior.IsChecked = false;
            chk4Ps.IsChecked = false;
            chkVoter.IsChecked = false;
            chkSoloParent.IsChecked = false;
            chkYouth.IsChecked = false;
            chkIndigent.IsChecked = false;
            _photoBytes = null;
            residentPhoto.Visibility = Visibility.Collapsed;
            photoPlaceholder.Visibility = Visibility.Visible;
        }
        finally
        {
            _isLoading = false;
        }
    }

    #endregion

    #region Event Handlers

    private void Field_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void Field_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: true);
        }
    }

    private void ComboField_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void DateField_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void CheckField_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void PhotoArea_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Resident Photo",
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _photoBytes = File.ReadAllBytes(dialog.FileName);
                LoadPhotoFromBytes(_photoBytes);
                MarkFieldDirty(validateImmediately: false);
            }
            catch (Exception ex)
            {
                ToastService.Error("Photo Error", $"Failed to load photo: {ex.Message}");
            }
        }
    }

    #endregion

    #region Helpers

    private void LoadPhotoFromBytes(byte[] bytes)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 128;
            bitmap.EndInit();
            bitmap.Freeze();

            residentPhoto.Source = bitmap;
            residentPhoto.Visibility = Visibility.Visible;
            photoPlaceholder.Visibility = Visibility.Collapsed;
        }
        catch
        {
            residentPhoto.Visibility = Visibility.Collapsed;
            photoPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private static ResidentDto CloneResident(ResidentDto resident)
    {
        return new ResidentDto
        {
            Id = resident.Id,
            FirstName = resident.FirstName,
            MiddleName = resident.MiddleName,
            LastName = resident.LastName,
            Suffix = resident.Suffix,
            Gender = resident.Gender,
            DateOfBirth = resident.DateOfBirth,
            CivilStatus = resident.CivilStatus,
            ContactNo = resident.ContactNo,
            IsPwd = resident.IsPwd,
            IsSenior = resident.IsSenior,
            Is4PsBeneficiary = resident.Is4PsBeneficiary,
            IsRegisteredVoter = resident.IsRegisteredVoter,
            IsSoloParent = resident.IsSoloParent,
            IsYouth = resident.IsYouth,
            IsIndigent = resident.IsIndigent,
            Status = resident.Status,
            PhotoBytes = resident.PhotoBytes,
            BarangayId = resident.BarangayId,
            PurokId = resident.PurokId,
            HouseholdId = resident.HouseholdId,
            Occupation = resident.Occupation,
            EducationalAttainment = resident.EducationalAttainment,
            Nationality = resident.Nationality,
            Religion = resident.Religion,
            BloodType = resident.BloodType,
            EmailAddress = resident.EmailAddress,
            PlaceOfBirth = resident.PlaceOfBirth,
            HouseNo = resident.HouseNo,
            Street = resident.Street,
            PhilHealthNo = resident.PhilHealthNo,
            SssNo = resident.SssNo,
            TinNo = resident.TinNo,
            VotersIdNo = resident.VotersIdNo,
            DateOfResidency = resident.DateOfResidency,
            HouseholdRelationship = resident.HouseholdRelationship
        };
    }

    private static string NormalizeGenderSelection(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" => "MALE",
            "F" or "FEMALE" => "FEMALE",
            _ => "MALE",
        };
    }

    private static string NormalizeCivilStatusSelection(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "MARRIED" => "MARRIED",
            "WIDOWED" => "WIDOWED",
            "SEPARATED" => "SEPARATED",
            _ => "SINGLE",
        };
    }

    #endregion
}
