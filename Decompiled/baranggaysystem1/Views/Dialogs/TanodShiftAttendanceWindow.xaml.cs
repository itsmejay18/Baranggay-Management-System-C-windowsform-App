using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Dialogs;

public partial class TanodShiftAttendanceWindow : Window
{
    private readonly TanodService _service = new TanodService();
    private readonly int _shiftId;
    private int? _selectedAssignmentId;

    public TanodShiftAttendanceWindow(int shiftId, string shiftLabel)
    {
        InitializeComponent();
        _shiftId = shiftId;
        headerSubtitle.Text = shiftLabel;
        base.Loaded += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var table = await _service.GetShiftAssignmentsAsync(_shiftId);
            assignmentsGrid.Columns.Clear();

            if (table.Rows.Count == 0)
            {
                assignmentsGrid.ItemsSource = null;
                emptyLabel.Visibility = Visibility.Visible;
                return;
            }
            emptyLabel.Visibility = Visibility.Collapsed;
            AddCol("Member", "full_name", 1.6);
            AddCol("Rank", "rank_title", 0.9);
            AddCol("Status", "attendance_status", 0.8);
            AddDateTimeCol("Checked In", "check_in_at", 1.1);
            AddDateTimeCol("Checked Out", "check_out_at", 1.1);
            assignmentsGrid.ItemsSource = table.DefaultView;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Load shift assignments failed.", ex);
            MessageBox.Show("Failed to load assignments: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddCol(string header, string binding, double star)
    {
        assignmentsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]"),
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AddDateTimeCol(string header, string binding, double star)
    {
        assignmentsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding($"[{binding}]") { StringFormat = "yyyy-MM-dd HH:mm" },
            Width = new DataGridLength(star, DataGridLengthUnitType.Star)
        });
    }

    private void AssignmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAssignmentId = (assignmentsGrid.SelectedItem is DataRowView row)
            ? Convert.ToInt32(row["assignment_id"])
            : (int?)null;
    }

    private async void BtnMarkPresent_Click(object sender, RoutedEventArgs e) => await SetStatus("PRESENT");
    private async void BtnMarkLate_Click(object sender, RoutedEventArgs e) => await SetStatus("LATE");
    private async void BtnMarkAbsent_Click(object sender, RoutedEventArgs e) => await SetStatus("ABSENT");

    private async Task SetStatus(string status)
    {
        if (!_selectedAssignmentId.HasValue)
        {
            MessageBox.Show("Select a member first.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            await _service.UpdateAttendanceAsync(_selectedAssignmentId.Value, status);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Update attendance failed.", ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
