using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace baranggaysystem1.Views.Panels;

/// <summary>
/// Read-only detail panel for viewing resident information in a fullscreen view.
/// Displays all resident data in a structured, non-editable layout.
/// Used with ShowSideToolbar = true for quick actions (Edit, Certificate, Payment, etc.).
///
/// Requirements: 1.1, 5.7
/// </summary>
public partial class ResidentDetailPanel : UserControl
{
    private readonly ResidentDto _resident;

    public ResidentDetailPanel(ResidentDto resident)
    {
        InitializeComponent();

        _resident = resident ?? throw new ArgumentNullException(nameof(resident));
        PopulateDetails();
    }

    /// <summary>
    /// Gets the resident ID for use by toolbar action handlers.
    /// </summary>
    public int ResidentId => _resident.Id ?? 0;

    /// <summary>
    /// Gets the full resident DTO for use by toolbar action handlers.
    /// </summary>
    public ResidentDto Resident => _resident;

    private void PopulateDetails()
    {
        // Header
        string fullName = $"{_resident.FirstName} {_resident.MiddleName} {_resident.LastName}".Replace("  ", " ").Trim();
        if (!string.IsNullOrWhiteSpace(_resident.Suffix))
        {
            fullName += $" {_resident.Suffix}";
        }
        lblFullName.Text = fullName;
        lblResidentMeta.Text = $"ID #{_resident.Id} • {FormatValue(_resident.Gender)} • {FormatValue(_resident.CivilStatus)}";

        // Photo
        if (_resident.PhotoBytes != null && _resident.PhotoBytes.Length > 0)
        {
            LoadPhotoFromBytes(_resident.PhotoBytes);
        }

        // Personal Info
        lblGender.Text = FormatValue(_resident.Gender);
        lblCivilStatus.Text = FormatValue(_resident.CivilStatus);
        lblBirthDate.Text = _resident.DateOfBirth != DateTime.MinValue
            ? _resident.DateOfBirth.ToString("MMMM dd, yyyy")
            : "—";
        lblNationality.Text = FormatValue(_resident.Nationality);
        lblReligion.Text = FormatValue(_resident.Religion);
        lblBloodType.Text = FormatValue(_resident.BloodType);
        lblOccupation.Text = FormatValue(_resident.Occupation);
        lblEducation.Text = FormatValue(_resident.EducationalAttainment);
        lblPlaceOfBirth.Text = FormatValue(_resident.PlaceOfBirth);

        // Contact & Address
        lblContact.Text = FormatValue(_resident.ContactNo);
        lblEmail.Text = FormatValue(_resident.EmailAddress);
        lblPurok.Text = _resident.PurokId.HasValue ? $"Purok #{_resident.PurokId}" : "—";
        lblHouseNo.Text = FormatValue(_resident.HouseNo);
        lblStreet.Text = FormatValue(_resident.Street);
        lblHouseholdRelationship.Text = FormatValue(_resident.HouseholdRelationship);

        // Government IDs
        lblPhilHealth.Text = FormatValue(_resident.PhilHealthNo);
        lblSss.Text = FormatValue(_resident.SssNo);
        lblTin.Text = FormatValue(_resident.TinNo);
        lblVotersId.Text = FormatValue(_resident.VotersIdNo);

        // Registries (badges)
        badgePwd.Visibility = _resident.IsPwd ? Visibility.Visible : Visibility.Collapsed;
        badgeSenior.Visibility = _resident.IsSenior ? Visibility.Visible : Visibility.Collapsed;
        badge4Ps.Visibility = _resident.Is4PsBeneficiary ? Visibility.Visible : Visibility.Collapsed;
        badgeVoter.Visibility = _resident.IsRegisteredVoter ? Visibility.Visible : Visibility.Collapsed;
        badgeSoloParent.Visibility = _resident.IsSoloParent ? Visibility.Visible : Visibility.Collapsed;
        badgeYouth.Visibility = _resident.IsYouth ? Visibility.Visible : Visibility.Collapsed;
        badgeIndigent.Visibility = _resident.IsIndigent ? Visibility.Visible : Visibility.Collapsed;

        // Residency
        lblDateOfResidency.Text = _resident.DateOfResidency.HasValue
            ? _resident.DateOfResidency.Value.ToString("MMMM dd, yyyy")
            : "—";
        lblStatus.Text = FormatValue(_resident.Status);
    }

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

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    }
}
