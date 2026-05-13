using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdTransferHistoryWindow : Window
{
	private readonly HouseholdRepository _repository = new HouseholdRepository();

	private readonly int _householdId;

	private readonly int _barangayId;






	public HouseholdTransferHistoryWindow(int householdId, string? householdLabel = null)
	{
		InitializeComponent();
		_householdId = householdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		householdTitleText.Text = (string.IsNullOrWhiteSpace(householdLabel) ? $"Household #{householdId}" : householdLabel.Trim());
		base.Loaded += HouseholdTransferHistoryWindow_Loaded;
	}

	private void HouseholdTransferHistoryWindow_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			IReadOnlyList<HouseholdTransferHistoryItem> transferHistory = _repository.GetTransferHistory(_householdId, _barangayId);
			historyGrid.ItemsSource = transferHistory;
			bool flag = transferHistory.Count > 0;
			emptyState.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			historySummaryText.Text = (flag ? $"{transferHistory.Count} transfer event(s) linked to this household." : "No transfer events have been recorded for this household yet.");
			timelineMetaText.Text = (flag ? "Showing the latest household movements first." : "Transfers from household reassignments, removals, and family moves appear here.");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load household transfer history.", ex);
			historyGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load transfer history.";
			emptyState.Visibility = Visibility.Visible;
			historySummaryText.Text = "Transfer history is temporarily unavailable.";
			timelineMetaText.Text = "Try reopening this window after the registry connection stabilizes.";
		}
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}}
