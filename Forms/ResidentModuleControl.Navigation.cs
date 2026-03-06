using System;
using System.Collections;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class ResidentModuleControl
{
    public int? SelectedResidentId => _selectedResidentId;

    public string ActiveProfileRouteSegment => string.IsNullOrWhiteSpace(_currentProfileRouteSegment)
        ? "overview"
        : _currentProfileRouteSegment;

    public bool NavigateToResidentProfile(int residentId, string profileSegment)
    {
        if (residentId <= 0)
        {
            return false;
        }

        bool previousSuppress = _suppressAutoOverviewOnSelection;
        _suppressAutoOverviewOnSelection = true;
        try
        {
            EnsureResidentsLoaded();
            bool selected = SelectResidentById(residentId);
            if (!selected)
            {
                return false;
            }

            SetResidentProfileTab(profileSegment, userInitiated: false, force: true);
            return true;
        }
        finally
        {
            _suppressAutoOverviewOnSelection = previousSuppress;
            RaiseResidentRouteChanged();
        }
    }

    public void NavigateToProfileRoute(string profileSegment)
    {
        SetResidentProfileTab(profileSegment, userInitiated: false, force: true);
        RaiseResidentRouteChanged();
    }

    private void RaiseResidentRouteChanged()
    {
        RouteChanged?.Invoke(
            this,
            new ResidentRouteChangedEventArgs(
                _selectedResidentId,
                string.IsNullOrWhiteSpace(_currentProfileRouteSegment) ? "overview" : _currentProfileRouteSegment));
    }

    public bool SelectResidentById(int residentId)
    {
        if (residentId <= 0)
        {
            return false;
        }

        // Ensure we're not stuck in "show deleted" mode for jump navigation.
        if (_showDeletedResidents)
        {
            _residentShowDeletedToggle.Checked = false;
            _showDeletedResidents = false;
        }

        if (!IsResidentView() || dgvResidents.DataSource == null)
        {
            LoadResidents();
        }

        bool selected = TrySelectResidentRow(residentId);
        if (!selected && !string.IsNullOrWhiteSpace(_searchBox.Text))
        {
            _searchBox.Text = string.Empty;
            ApplyResidentSearch();
            selected = TrySelectResidentRow(residentId);
        }

        if (!selected)
        {
            selected = TrySelectResidentAcrossPages(residentId);
        }

        return selected;
    }

    public bool SelectCertificateById(int certificateId)
    {
        if (certificateId <= 0)
        {
            return false;
        }

        // Clear filters so selection isn't hidden by filters.
        try
        {
            _certSearchBox.Text = string.Empty;
            _certFilterType.SelectedIndex = 0;
            _certFilterStatus.SelectedIndex = 0;
            _certFilterFrom.Checked = false;
            _certFilterTo.Checked = false;
            ApplyCertificateFilters();
        }
        catch
        {
            // ignore filter reset failures, best effort
        }

        if (_selectedResidentId.HasValue)
        {
            try
            {
                LoadCertificatesForResident(_selectedResidentId.Value);
            }
            catch
            {
                // ignore, selection might still work with existing table
            }
        }

        int? before = _selectedCertificateId;
        SelectCertificateRow(certificateId);
        return _selectedCertificateId == certificateId || before != _selectedCertificateId;
    }

    public bool SelectBlotterById(int blotterId)
    {
        if (blotterId <= 0)
        {
            return false;
        }

        if (_selectedResidentId.HasValue)
        {
            try
            {
                LoadBlottersForResident(_selectedResidentId.Value);
            }
            catch
            {
                // ignore
            }
        }

        int? before = _selectedBlotterId;
        SelectBlotterCard(blotterId);
        return _selectedBlotterId == blotterId || before != _selectedBlotterId;
    }

    private bool TrySelectResidentRow(int residentId)
    {
        foreach (DataGridViewRow row in (IEnumerable)dgvResidents.Rows)
        {
            if (row.Cells["resident_id"]?.Value == null)
            {
                continue;
            }

            if (!int.TryParse(row.Cells["resident_id"].Value.ToString(), out int id) || id != residentId)
            {
                continue;
            }

            row.Selected = true;
            dgvResidents.CurrentCell = row.Cells["lastname"] ?? row.Cells["firstname"] ?? row.Cells[0];

            try
            {
                dgvResidents.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index);
            }
            catch
            {
                // ignore scroll failures
            }

            PopulateResidentDetails(row);
            return true;
        }

        return false;
    }

    private bool TrySelectResidentAcrossPages(int residentId)
    {
        if (_residentTable == null)
        {
            return false;
        }

        var view = _residentTable.DefaultView;
        int rowIndex = -1;
        for (int i = 0; i < view.Count; i++)
        {
            object? value = view[i]["resident_id"];
            if (value == null || value == DBNull.Value)
            {
                continue;
            }

            if (Convert.ToInt32(value) == residentId)
            {
                rowIndex = i;
                break;
            }
        }

        if (rowIndex < 0)
        {
            return false;
        }

        _residentPageIndex = rowIndex / ResidentPageSize;
        ApplyResidentPaging(resetPage: false);
        return TrySelectResidentRow(residentId);
    }
}
