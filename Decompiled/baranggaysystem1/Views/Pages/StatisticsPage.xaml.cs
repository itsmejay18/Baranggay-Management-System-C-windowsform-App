using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views.Pages;

public partial class StatisticsPage : UserControl
{
    private const double MaxBarWidth = 260.0;
    private readonly StatisticsService _service = new StatisticsService();

    public StatisticsPage()
    {
        InitializeComponent();
        base.Loaded += async (_, __) => await LoadAsync();
    }

    public sealed class BarRow
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public double BarWidth { get; set; }
    }

    private async Task LoadAsync()
    {
        lblUpdatedAt.Text = "Loading statistics...";
        try
        {
            var totalsTask = _service.LoadTotalsAsync();
            var purokTask = _service.LoadResidentsByPurokAsync();
            var ageTask = _service.LoadResidentsByAgeBracketAsync();
            var blotterTask = _service.LoadBlotterByTypeAsync();
            var certsTask = _service.LoadCertificatesByTypeAsync();
            var bookingsTask = _service.LoadBookingsByFacilityAsync();
            var severityTask = _service.LoadPatrolLogsBySeverityAsync();
            var trendTask = _service.LoadMonthlyCertificateTrendAsync();

            await Task.WhenAll(totalsTask, purokTask, ageTask, blotterTask,
                certsTask, bookingsTask, severityTask, trendTask);

            ApplyTotals(totalsTask.Result);
            BindChart(chartPurok, emptyPurok, purokTask.Result);
            BindChart(chartAge, emptyAge, ageTask.Result);
            BindChart(chartBlotter, emptyBlotter, blotterTask.Result);
            BindChart(chartCerts, emptyCerts, certsTask.Result);
            BindChart(chartBookings, emptyBookings, bookingsTask.Result);
            BindChart(chartSeverity, emptySeverity, severityTask.Result);
            BindChart(chartTrend, emptyTrend, trendTask.Result);

            lblUpdatedAt.Text = $"Last updated {DateTime.Now:MMM d, yyyy h:mm tt}";
        }
        catch (Exception ex)
        {
            AppLogger.LogError("StatisticsPage load failed.", ex);
            lblUpdatedAt.Text = "Failed to load statistics. Please refresh.";
        }
    }

    private void ApplyTotals(StatisticsService.Totals t)
    {
        valResidents.Text = t.Residents.ToString("N0");
        valActive.Text = t.ActiveResidents.ToString("N0");
        valHouseholds.Text = t.Households.ToString("N0");
        valPuroks.Text = t.Puroks.ToString("N0");
        valMale.Text = t.Male.ToString("N0");
        valFemale.Text = t.Female.ToString("N0");
        valDeceased.Text = t.Deceased.ToString("N0");

        valSeniors.Text = t.Seniors.ToString("N0");
        valYouth.Text = t.Youth.ToString("N0");
        valPwd.Text = t.Pwd.ToString("N0");
        valSoloParents.Text = t.SoloParents.ToString("N0");
        valIndigent.Text = t.Indigent.ToString("N0");
        val4Ps.Text = t.FourPs.ToString("N0");
        valVoters.Text = t.Voters.ToString("N0");

        valPendingCerts.Text = t.PendingCerts.ToString("N0");
        valReleasedCerts.Text = t.ReleasedCertsThisMonth.ToString("N0");
        valOpenBlotter.Text = t.OpenBlotter.ToString("N0");
        valResolvedBlotter.Text = t.ResolvedBlotter.ToString("N0");
        valMeetings.Text = t.UpcomingMeetings.ToString("N0");
        valBookings.Text = t.PendingBookings.ToString("N0");
        valShiftsToday.Text = t.ShiftsToday.ToString("N0");
        valPatrolLogs.Text = t.PatrolLogsThisWeek.ToString("N0");
        valRevenue.Text = "₱" + t.RevenueThisMonth.ToString("N2");
        valAyuda.Text = t.AyudaReleasedThisMonth.ToString("N0");
    }

    private static void BindChart(ItemsControl control, TextBlock emptyLabel,
        IReadOnlyList<StatisticsService.CategoryCount> data)
    {
        if (data == null || data.Count == 0)
        {
            control.ItemsSource = null;
            emptyLabel.Visibility = Visibility.Visible;
            return;
        }
        int max = data.Max(c => c.Count);
        if (max <= 0) max = 1;
        var rows = data.Select(c => new BarRow
        {
            Label = c.Label,
            Count = c.Count,
            BarWidth = Math.Max(4, (c.Count / (double)max) * MaxBarWidth)
        }).ToList();
        control.ItemsSource = rows;
        emptyLabel.Visibility = Visibility.Collapsed;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"statistics_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            Title = "Export statistics snapshot"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var totals = await _service.LoadTotalsAsync();
            await _service.ExportSummaryCsvAsync(totals, dialog.FileName);
            MessageBox.Show($"Exported to:\n{dialog.FileName}", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Statistics export failed.", ex);
            MessageBox.Show("Failed to export: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
