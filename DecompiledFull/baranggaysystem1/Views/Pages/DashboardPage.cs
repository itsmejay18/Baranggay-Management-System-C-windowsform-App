using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using baranggaysystem1.Database;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class DashboardPage : UserControl, IComponentConnector
{
	private readonly AnnouncementService _announcementService = new AnnouncementService();

	private readonly DashboardReminderService _dashboardReminderService = new DashboardReminderService();

	private readonly ProjectService _projectService = new ProjectService();

	private readonly bool _showReminderEntry;

	internal ScrollViewer pageScrollViewer;

	internal StackPanel reminderCenterSection;

	internal TextBlock reminderStatusNote;

	internal TextBlock urgentReminderCountText;

	internal TextBlock importantReminderCountText;

	internal TextBlock planReminderCountText;

	internal StackPanel importantReminderCards;

	internal StackPanel planReminderCards;

	internal StackPanel dashboardOverviewSection;

	internal TextBlock welcomeGreeting;

	internal TextBlock welcomeDate;

	internal Grid kpiRow;

	internal TextBlock statResidentsValue;

	internal TextBlock statActiveValue;

	internal TextBlock statHouseholdsValue;

	internal TextBlock statCertsValue;

	internal Border kpiBlotter;

	internal TextBlock statBlotterValue;

	internal UniformGrid quickLaunchGrid;

	internal Button quickTileResident;

	internal Button quickTileCert;

	internal Button quickTileBlotter;

	internal Button quickTileReports;

	internal Button btnAnnouncementRegistry;

	internal Button btnAnnouncementNew;

	internal StackPanel announcementCards;

	internal Border projectsPanel;

	internal Button btnProjectRegistry;

	internal Button btnProjectNew;

	internal StackPanel projectCards;

	internal Border governanceToolsPanel;

	internal Calendar actionCalendar;

	internal TextBlock calendarInfoText;

	private bool _contentLoaded;

	public DashboardPage(bool showReminderEntry = false)
	{
		_showReminderEntry = showReminderEntry;
		InitializeComponent();
		base.Loaded += async delegate
		{
			await LoadDashboardAsync();
		};
		actionCalendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
		string text = UserSession.Username ?? "User";
		int hour = DateTime.Now.Hour;
		welcomeGreeting.Text = (((hour < 12) ? "Good morning" : ((hour < 17) ? "Good afternoon" : "Good evening")) + ", " + text).ToUpperInvariant();
		welcomeDate.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
		if (_showReminderEntry)
		{
			ShowReminderCenter();
		}
		else
		{
			ShowDashboardOverview();
		}
		ApplyRoleDashboard();
	}

	private void ApplyRoleDashboard()
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool num = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		bool flag2 = num || Permissions.CanManageAnnouncements;
		bool flag3 = num || Permissions.CanManageProjects;
		bool flag4 = flag2 || flag3;
		if (!num && !Permissions.CanCreateBlotter)
		{
			kpiBlotter.Visibility = Visibility.Collapsed;
		}
		if (!num)
		{
			quickTileBlotter.Visibility = ((!Permissions.CanCreateBlotter) ? Visibility.Collapsed : Visibility.Visible);
			quickTileReports.Visibility = ((!Permissions.CanViewHotspotReports) ? Visibility.Collapsed : Visibility.Visible);
			int num2 = 2;
			if (quickTileBlotter.Visibility == Visibility.Visible)
			{
				num2++;
			}
			if (quickTileReports.Visibility == Visibility.Visible)
			{
				num2++;
			}
			quickLaunchGrid.Columns = num2;
			governanceToolsPanel.Visibility = Visibility.Collapsed;
		}
		if (!num && !flag3)
		{
			projectsPanel.Visibility = Visibility.Collapsed;
		}
		btnAnnouncementNew.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		btnProjectNew.Visibility = ((!flag3) ? Visibility.Collapsed : Visibility.Visible);
		btnAnnouncementRegistry.Visibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
		btnProjectRegistry.Visibility = ((!flag4) ? Visibility.Collapsed : Visibility.Visible);
	}

	private async Task LoadDashboardAsync()
	{
		if (_showReminderEntry)
		{
			await LoadReminderCenterAsync();
		}
		else
		{
			await LoadDashboardOverviewAsync();
		}
	}

	private async Task LoadDashboardOverviewAsync()
	{
		_ = 1;
		try
		{
			UpdateStatCards(await FetchStats());
			await LoadFeaturePanelsAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: failed to load stats.", ex);
		}
	}

	private async Task LoadReminderCenterAsync()
	{
		reminderStatusNote.Text = "Loading reminders...";
		importantReminderCards.Children.Clear();
		planReminderCards.Children.Clear();
		try
		{
			ApplyReminderSnapshot(await _dashboardReminderService.LoadSnapshotAsync());
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: reminder center load failed.", ex);
			urgentReminderCountText.Text = "0";
			importantReminderCountText.Text = "0";
			planReminderCountText.Text = "0";
			reminderStatusNote.Text = "Reminders unavailable.";
			importantReminderCards.Children.Clear();
			planReminderCards.Children.Clear();
			importantReminderCards.Children.Add(BuildEmptyState("Unable to load important notifications right now."));
			planReminderCards.Children.Add(BuildEmptyState("Unable to load planned work right now."));
		}
	}

	private async Task<(int residents, int active, int households, int certs, int blotter)> FetchStats()
	{
		return (residents: await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resident WHERE is_deleted=0 OR is_deleted IS NULL"), active: await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resident WHERE status='ACTIVE' AND (is_deleted=0 OR is_deleted IS NULL)"), households: await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM household"), certs: await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM document_request WHERE status='SUBMITTED'"), blotter: await DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM case_record WHERE UPPER(status) IN ('OPEN','ONGOING')"));
	}

	private void UpdateStatCards((int residents, int active, int households, int certs, int blotter) stats)
	{
		statResidentsValue.Text = stats.residents.ToString("N0");
		statActiveValue.Text = stats.active.ToString("N0");
		statHouseholdsValue.Text = stats.households.ToString("N0");
		statCertsValue.Text = stats.certs.ToString("N0");
		statBlotterValue.Text = stats.blotter.ToString("N0");
	}

	private async Task LoadFeaturePanelsAsync()
	{
		try
		{
			Task<IReadOnlyList<AnnouncementRecord>> announcementTask = _announcementService.GetRecentAnnouncementsAsync();
			Task<IReadOnlyList<ProjectRecord>> projectTask = _projectService.GetRecentProjectsAsync();
			await Task.WhenAll(announcementTask, projectTask);
			RenderAnnouncementCards(announcementTask.Result);
			RenderProjectCards(projectTask.Result);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: feature panel load failed.", ex);
			announcementCards.Children.Clear();
			projectCards.Children.Clear();
			announcementCards.Children.Add(BuildEmptyState("Unable to load announcements right now."));
			projectCards.Children.Add(BuildEmptyState("Unable to load projects and programs right now."));
		}
	}

	private void RenderAnnouncementCards(IReadOnlyList<AnnouncementRecord> announcements)
	{
		announcementCards.Children.Clear();
		if (announcements == null || announcements.Count == 0)
		{
			announcementCards.Children.Add(BuildEmptyState("No announcements yet."));
			return;
		}
		foreach (AnnouncementRecord announcement in announcements)
		{
			announcementCards.Children.Add(BuildAnnouncementCard(announcement));
		}
	}

	private void RenderProjectCards(IReadOnlyList<ProjectRecord> projects)
	{
		projectCards.Children.Clear();
		if (projects == null || projects.Count == 0)
		{
			projectCards.Children.Add(BuildEmptyState("No projects or programs yet."));
			return;
		}
		foreach (ProjectRecord project in projects)
		{
			projectCards.Children.Add(BuildProjectCard(project));
		}
	}

	private Border BuildAnnouncementCard(AnnouncementRecord announcement)
	{
		Border border = CreateFeatureCardShell();
		StackPanel stackPanel = new StackPanel();
		Grid grid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition(),
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			},
			Children = { (UIElement)new TextBlock
			{
				Text = announcement.Title,
				FontWeight = FontWeights.Bold,
				FontSize = 13.0,
				Foreground = (Brush)Application.Current.Resources["Slate900Brush"],
				TextWrapping = TextWrapping.Wrap
			} }
		};
		if (announcement.IsPinned)
		{
			Border border2 = CreateChip("Pinned", "#DBEAFE", "#1D4ED8");
			Grid.SetColumn(border2, 1);
			border2.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
			grid.Children.Add(border2);
		}
		stackPanel.Children.Add(grid);
		WrapPanel wrapPanel = new WrapPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		wrapPanel.Children.Add(CreateAnnouncementPriorityChip(announcement.Priority));
		wrapPanel.Children.Add(CreateAnnouncementStatusChip(announcement.Status));
		stackPanel.Children.Add(wrapPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = announcement.CreatedAtDisplay,
			FontSize = 11.0,
			Foreground = (Brush)Application.Current.Resources["Slate500Brush"]
		});
		if (CanManageAnnouncements())
		{
			stackPanel.Children.Add(CreateActionRow(async delegate
			{
				await OpenAnnouncementEditorAsync(announcement);
			}, async delegate
			{
				await DeleteAnnouncementAsync(announcement);
			}));
		}
		border.Child = stackPanel;
		return border;
	}

	private Border BuildProjectCard(ProjectRecord project)
	{
		Border border = CreateFeatureCardShell();
		StackPanel stackPanel = new StackPanel
		{
			Children = { (UIElement)new TextBlock
			{
				Text = project.Name,
				FontWeight = FontWeights.Bold,
				FontSize = 13.0,
				Foreground = (Brush)Application.Current.Resources["Slate900Brush"],
				TextWrapping = TextWrapping.Wrap
			} }
		};
		WrapPanel wrapPanel = new WrapPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 8.0)
		};
		wrapPanel.Children.Add(CreateProjectTypeChip(project.RecordType));
		wrapPanel.Children.Add(CreateProjectStatusChip(project.Status));
		wrapPanel.Children.Add(CreateProjectOutcomeChip(project.OutcomeStatus));
		stackPanel.Children.Add(wrapPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = BuildProjectScheduleLabel(project),
			FontSize = 11.0,
			Foreground = (Brush)Application.Current.Resources["Slate500Brush"]
		});
		if (project.LastActivityDate.HasValue)
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = "Last active " + project.LastActivityDisplay,
				FontSize = 11.0,
				Foreground = (Brush)Application.Current.Resources["Slate500Brush"],
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
		}
		if (CanManageProjects())
		{
			stackPanel.Children.Add(CreateActionRow(async delegate
			{
				await OpenProjectEditorAsync(project);
			}, async delegate
			{
				await DeleteProjectAsync(project);
			}));
		}
		border.Child = stackPanel;
		return border;
	}

	private Border CreateFeatureCardShell()
	{
		return new Border
		{
			Background = (Brush)Application.Current.Resources["Slate50Brush"],
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
			BorderBrush = (Brush)Application.Current.Resources["Slate100Brush"],
			BorderThickness = new Thickness(1.0)
		};
	}

	private Border CreateChip(string text, string backgroundHex, string foregroundHex)
	{
		return new Border
		{
			Background = (Brush)new BrushConverter().ConvertFromString(backgroundHex),
			CornerRadius = new CornerRadius(999.0),
			Padding = new Thickness(9.0, 3.0, 9.0, 3.0),
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
			Child = new TextBlock
			{
				Text = text,
				FontSize = 10.5,
				FontWeight = FontWeights.Bold,
				Foreground = (Brush)new BrushConverter().ConvertFromString(foregroundHex)
			}
		};
	}

	private Border CreateAnnouncementPriorityChip(string priority)
	{
		if (!(priority == "High"))
		{
			if (priority == "Low")
			{
				return CreateChip("Low Priority", "#ECFDF5", "#166534");
			}
			return CreateChip("Normal Priority", "#FEF3C7", "#B45309");
		}
		return CreateChip("High Priority", "#FEE2E2", "#B91C1C");
	}

	private Border CreateAnnouncementStatusChip(string status)
	{
		if (!(status == "Draft"))
		{
			if (status == "Archived")
			{
				return CreateChip("Archived", "#F3F4F6", "#4B5563");
			}
			return CreateChip("Published", "#DBEAFE", "#1D4ED8");
		}
		return CreateChip("Draft", "#E5E7EB", "#374151");
	}

	private Border CreateProjectStatusChip(string status)
	{
		return status switch
		{
			"Completed" => CreateChip("Completed", "#DCFCE7", "#166534"), 
			"Ongoing" => CreateChip("Ongoing", "#DBEAFE", "#1D4ED8"), 
			"On hold" => CreateChip("On Hold", "#FEE2E2", "#B91C1C"), 
			_ => CreateChip("Planned", "#FEF3C7", "#B45309"), 
		};
	}

	private Border CreateProjectTypeChip(string recordType)
	{
		if (!string.Equals(recordType, "Program", StringComparison.OrdinalIgnoreCase))
		{
			return CreateChip("Project", "#E0F2FE", "#0369A1");
		}
		return CreateChip("Program", "#CCFBF1", "#0F766E");
	}

	private Border CreateProjectOutcomeChip(string outcomeStatus)
	{
		return outcomeStatus switch
		{
			"Achieved" => CreateChip("Outcome Achieved", "#DCFCE7", "#166534"), 
			"Needs follow-up" => CreateChip("Needs Follow-up", "#FEE2E2", "#B91C1C"), 
			"In progress" => CreateChip("Outcome In Progress", "#DBEAFE", "#1D4ED8"), 
			_ => CreateChip("Outcome Pending", "#E5E7EB", "#374151"), 
		};
	}

	private UIElement CreateActionRow(Func<Task> editAction, Func<Task> deleteAction)
	{
		StackPanel obj = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = "Edit",
			Style = (Style)Application.Current.Resources["GhostButtonStyle"],
			Height = 30.0,
			MinWidth = 68.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			Margin = new Thickness(0.0, 0.0, 4.0, 0.0)
		};
		button.Click += async delegate
		{
			await editAction();
		};
		Button button2 = new Button
		{
			Content = "Delete",
			Style = (Style)Application.Current.Resources["GhostButtonStyle"],
			Height = 30.0,
			MinWidth = 72.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			Foreground = (Brush)new BrushConverter().ConvertFromString("#B91C1C")
		};
		button2.Click += async delegate
		{
			await deleteAction();
		};
		obj.Children.Add(button);
		obj.Children.Add(button2);
		return obj;
	}

	private static UIElement BuildEmptyState(string message)
	{
		return new TextBlock
		{
			Text = message,
			FontSize = 12.0,
			Foreground = (Brush)Application.Current.Resources["Slate500Brush"],
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
	}

	private static string BuildProjectScheduleLabel(ProjectRecord project)
	{
		if (project.StartDate.HasValue && project.EndDate.HasValue)
		{
			return $"{project.StartDate.Value:MMM dd, yyyy} - {project.EndDate.Value:MMM dd, yyyy}";
		}
		if (project.StartDate.HasValue)
		{
			return $"Starts {project.StartDate.Value:MMM dd, yyyy}";
		}
		if (project.EndDate.HasValue)
		{
			return $"Target end {project.EndDate.Value:MMM dd, yyyy}";
		}
		return "Created " + project.CreatedAtDisplay;
	}

	private void ApplyReminderSnapshot(DashboardReminderSnapshot snapshot)
	{
		urgentReminderCountText.Text = snapshot.UrgentCount.ToString("N0");
		importantReminderCountText.Text = snapshot.NotificationCount.ToString("N0");
		planReminderCountText.Text = snapshot.PlanCount.ToString("N0");
		bool flag = snapshot.NotificationCount > 0 || snapshot.PlanCount > 0;
		reminderStatusNote.Text = (flag ? $"{snapshot.NotificationCount:N0} important item(s) | {snapshot.PlanCount:N0} plan item(s)" : "No urgent reminders right now.");
		RenderReminderCards(importantReminderCards, snapshot.Notifications, "No urgent notifications were found for the current dashboard check.");
		RenderReminderCards(planReminderCards, snapshot.Plans, "No upcoming plans or schedules were found for the current dashboard check.");
	}

	private void RenderReminderCards(Panel host, IReadOnlyList<DashboardReminderItem> items, string emptyMessage)
	{
		host.Children.Clear();
		if (items == null || items.Count == 0)
		{
			host.Children.Add(BuildEmptyState(emptyMessage));
			return;
		}
		foreach (DashboardReminderItem item in items)
		{
			host.Children.Add(BuildReminderCard(item));
		}
	}

	private Border BuildReminderCard(DashboardReminderItem item)
	{
		(string accentHex, string chipBackgroundHex, string chipForegroundHex) tuple = ResolveReminderPalette(item.Severity);
		string item2 = tuple.accentHex;
		string item3 = tuple.chipBackgroundHex;
		string item4 = tuple.chipForegroundHex;
		Border obj = new Border
		{
			Background = (Brush)Application.Current.Resources["Slate50Brush"],
			BorderBrush = (Brush)Application.Current.Resources["Slate100Brush"],
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(14.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
			Padding = new Thickness(0.0)
		};
		Grid grid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = new GridLength(6.0)
				},
				new ColumnDefinition()
			}
		};
		Border element = new Border
		{
			Background = (Brush)new BrushConverter().ConvertFromString(item2),
			CornerRadius = new CornerRadius(14.0, 0.0, 0.0, 14.0)
		};
		grid.Children.Add(element);
		Border border = new Border
		{
			Padding = new Thickness(16.0, 14.0, 16.0, 14.0)
		};
		Grid.SetColumn(border, 1);
		StackPanel stackPanel = new StackPanel();
		Grid grid2 = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		StackPanel element2 = new StackPanel
		{
			Children = 
			{
				(UIElement)new TextBlock
				{
					Text = item.Title,
					FontWeight = FontWeights.Bold,
					FontSize = 14.0,
					Foreground = (Brush)Application.Current.Resources["Slate900Brush"],
					TextWrapping = TextWrapping.Wrap
				},
				(UIElement)new TextBlock
				{
					Text = item.Category,
					FontSize = 10.5,
					FontWeight = FontWeights.Bold,
					Foreground = (Brush)Application.Current.Resources["Slate500Brush"],
					Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
				}
			}
		};
		grid2.Children.Add(element2);
		Border element3 = new Border
		{
			Background = (Brush)new BrushConverter().ConvertFromString(item3),
			CornerRadius = new CornerRadius(999.0),
			Padding = new Thickness(10.0, 4.0, 10.0, 4.0),
			VerticalAlignment = VerticalAlignment.Top,
			Child = new TextBlock
			{
				Text = ResolveSeverityLabel(item.Severity),
				FontSize = 10.5,
				FontWeight = FontWeights.Bold,
				Foreground = (Brush)new BrushConverter().ConvertFromString(item4)
			}
		};
		Grid.SetColumn(element3, 1);
		grid2.Children.Add(element3);
		stackPanel.Children.Add(grid2);
		stackPanel.Children.Add(new TextBlock
		{
			Text = item.Description,
			FontSize = 12.5,
			Foreground = (Brush)Application.Current.Resources["Slate700Brush"],
			TextWrapping = TextWrapping.Wrap
		});
		Grid grid3 = new Grid
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		grid3.ColumnDefinitions.Add(new ColumnDefinition());
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.Children.Add(new TextBlock
		{
			Text = item.Footnote,
			Style = (Style)Application.Current.Resources["SmallTextStyle"],
			Foreground = (Brush)Application.Current.Resources["Slate500Brush"],
			VerticalAlignment = VerticalAlignment.Center,
			TextWrapping = TextWrapping.Wrap
		});
		if (!string.IsNullOrWhiteSpace(item.Route) && CanOpenReminderRoute(item.Route))
		{
			Button button = new Button
			{
				Content = item.ActionLabel,
				Style = (Style)Application.Current.Resources["GhostButtonStyle"],
				Height = 30.0,
				MinWidth = 96.0,
				Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
				Margin = new Thickness(12.0, 0.0, 0.0, 0.0)
			};
			button.Click += delegate
			{
				NavigateReminderRoute(item.Route);
			};
			Grid.SetColumn(button, 1);
			grid3.Children.Add(button);
		}
		stackPanel.Children.Add(grid3);
		border.Child = stackPanel;
		grid.Children.Add(border);
		obj.Child = grid;
		return obj;
	}

	private static (string accentHex, string chipBackgroundHex, string chipForegroundHex) ResolveReminderPalette(DashboardReminderSeverity severity)
	{
		return severity switch
		{
			DashboardReminderSeverity.Urgent => (accentHex: "#DC2626", chipBackgroundHex: "#FEE2E2", chipForegroundHex: "#B91C1C"), 
			DashboardReminderSeverity.Attention => (accentHex: "#D97706", chipBackgroundHex: "#FEF3C7", chipForegroundHex: "#B45309"), 
			_ => (accentHex: "#2563EB", chipBackgroundHex: "#DBEAFE", chipForegroundHex: "#1D4ED8"), 
		};
	}

	private static string ResolveSeverityLabel(DashboardReminderSeverity severity)
	{
		return severity switch
		{
			DashboardReminderSeverity.Urgent => "Urgent", 
			DashboardReminderSeverity.Attention => "Needs Attention", 
			_ => "Planned", 
		};
	}

	private bool CanOpenReminderRoute(string route)
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		return route switch
		{
			"Clearances" => flag2 || Permissions.CanRequestCertificates || Permissions.CanEditCertificateRequests || Permissions.CanApproveCertificates || Permissions.CanIssueCertificates || Permissions.CanCancelCertificates || Permissions.CanExportCertificates, 
			"ResidentCases" => flag2 || Permissions.CanCreateBlotter || Permissions.CanUpdateBlotterStatus, 
			"GovernanceRegistry" => flag2 || Permissions.CanManageAnnouncements || Permissions.CanManageProjects, 
			"NotificationOutbox" => flag2, 
			_ => true, 
		};
	}

	private void NavigateReminderRoute(string route)
	{
		if (Application.Current.MainWindow is MainWindow mainWindow)
		{
			mainWindow.NavigatePage(route);
		}
	}

	private void ShowReminderCenter()
	{
		reminderCenterSection.Visibility = Visibility.Visible;
		dashboardOverviewSection.Visibility = Visibility.Collapsed;
		pageScrollViewer.ScrollToHome();
	}

	private void ShowDashboardOverview()
	{
		reminderCenterSection.Visibility = Visibility.Collapsed;
		dashboardOverviewSection.Visibility = Visibility.Visible;
		pageScrollViewer.ScrollToHome();
	}

	private void Calendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
	{
		DateTime? selectedDate = actionCalendar.SelectedDate;
		if (selectedDate.HasValue)
		{
			DateTime valueOrDefault = selectedDate.GetValueOrDefault();
			calendarInfoText.Text = $"Selected: {valueOrDefault:ddd, MMM dd, yyyy}\nNo urgent items scheduled.";
			calendarInfoText.Visibility = Visibility.Visible;
		}
	}

	private async void BtnRefreshReminderCenter_Click(object sender, RoutedEventArgs e)
	{
		await LoadReminderCenterAsync();
	}

	private void BtnOpenDashboardOverview_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Dashboard");
	}

	private void BtnOpenReminderCenter_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("DashboardNotifications");
	}

	private void QuickAddResident_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("ResidentWorkspace");
	}

	private void QuickAddCertificate_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Clearances");
	}

	private void QuickAddBlotter_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("ResidentCases");
	}

	private void QuickOpenReports_Click(object sender, RoutedEventArgs e)
	{
		(Application.Current.MainWindow as MainWindow)?.NavigatePage("Reports");
	}

	private async void BtnAnnouncementNew_Click(object sender, RoutedEventArgs e)
	{
		await OpenAnnouncementEditorAsync(null);
	}

	private async void BtnAnnouncementRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadFeaturePanelsAsync();
	}

	private async void BtnProjectNew_Click(object sender, RoutedEventArgs e)
	{
		await OpenProjectEditorAsync(null);
	}

	private async void BtnProjectRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadFeaturePanelsAsync();
	}

	private void BtnOpenGovernanceRegistry_Click(object sender, RoutedEventArgs e)
	{
		if (!CanOpenGovernanceRegistry())
		{
			DialogService.Instance.ShowWarning("You do not have permission to open the announcements and projects registry.");
		}
		else
		{
			(Application.Current.MainWindow as MainWindow)?.NavigatePage("GovernanceRegistry");
		}
	}

	private bool CanManageAnnouncements()
	{
		if (!Permissions.IsAdmin)
		{
			return Permissions.CanManageAnnouncements;
		}
		return true;
	}

	private bool CanManageProjects()
	{
		if (!Permissions.IsAdmin)
		{
			return Permissions.CanManageProjects;
		}
		return true;
	}

	private bool CanOpenGovernanceRegistry()
	{
		if (!CanManageAnnouncements())
		{
			return CanManageProjects();
		}
		return true;
	}

	private async Task OpenAnnouncementEditorAsync(AnnouncementRecord? announcement)
	{
		if (!CanManageAnnouncements())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage announcements.");
			return;
		}
		AnnouncementWindow window = ((announcement == null) ? new AnnouncementWindow() : new AnnouncementWindow(announcement));
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			await LoadFeaturePanelsAsync();
		}
	}

	private async Task OpenProjectEditorAsync(ProjectRecord? project)
	{
		if (!CanManageProjects())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage projects.");
			return;
		}
		ProjectWindow window = ((project == null) ? new ProjectWindow() : new ProjectWindow(project));
		if (DialogService.Instance.ShowDialog(window) == true)
		{
			await LoadFeaturePanelsAsync();
		}
	}

	private async Task DeleteAnnouncementAsync(AnnouncementRecord announcement)
	{
		if (!CanManageAnnouncements())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage announcements.");
		}
		else if (DialogService.Instance.Confirm("Delete announcement \"" + announcement.Title + "\"?", "Delete Announcement"))
		{
			try
			{
				await _announcementService.DeleteAnnouncementAsync(announcement.AnnouncementId);
				await LoadFeaturePanelsAsync();
			}
			catch (Exception ex)
			{
				AppLogger.LogError("DashboardPage: failed to delete announcement.", ex);
				DialogService.Instance.ShowError(ex.Message, "Delete Announcement");
			}
		}
	}

	private async Task DeleteProjectAsync(ProjectRecord project)
	{
		if (!CanManageProjects())
		{
			DialogService.Instance.ShowWarning("You do not have permission to manage projects.");
			return;
		}
		string value = (string.Equals(project.RecordType, "Program", StringComparison.OrdinalIgnoreCase) ? "program" : "project");
		if (!DialogService.Instance.Confirm($"Delete {value} \"{project.Name}\"?", "Delete Project"))
		{
			return;
		}
		try
		{
			await _projectService.DeleteProjectAsync(project.ProjectId);
			await LoadFeaturePanelsAsync();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("DashboardPage: failed to delete project.", ex);
			DialogService.Instance.ShowError(ex.Message, "Delete Project");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/dashboardpage.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			pageScrollViewer = (ScrollViewer)target;
			break;
		case 2:
			reminderCenterSection = (StackPanel)target;
			break;
		case 3:
			reminderStatusNote = (TextBlock)target;
			break;
		case 4:
			((Button)target).Click += BtnRefreshReminderCenter_Click;
			break;
		case 5:
			((Button)target).Click += BtnOpenDashboardOverview_Click;
			break;
		case 6:
			urgentReminderCountText = (TextBlock)target;
			break;
		case 7:
			importantReminderCountText = (TextBlock)target;
			break;
		case 8:
			planReminderCountText = (TextBlock)target;
			break;
		case 9:
			importantReminderCards = (StackPanel)target;
			break;
		case 10:
			planReminderCards = (StackPanel)target;
			break;
		case 11:
			((Button)target).Click += BtnOpenDashboardOverview_Click;
			break;
		case 12:
			dashboardOverviewSection = (StackPanel)target;
			break;
		case 13:
			welcomeGreeting = (TextBlock)target;
			break;
		case 14:
			welcomeDate = (TextBlock)target;
			break;
		case 15:
			((Button)target).Click += BtnOpenReminderCenter_Click;
			break;
		case 16:
			kpiRow = (Grid)target;
			break;
		case 17:
			statResidentsValue = (TextBlock)target;
			break;
		case 18:
			statActiveValue = (TextBlock)target;
			break;
		case 19:
			statHouseholdsValue = (TextBlock)target;
			break;
		case 20:
			statCertsValue = (TextBlock)target;
			break;
		case 21:
			kpiBlotter = (Border)target;
			break;
		case 22:
			statBlotterValue = (TextBlock)target;
			break;
		case 23:
			quickLaunchGrid = (UniformGrid)target;
			break;
		case 24:
			quickTileResident = (Button)target;
			quickTileResident.Click += QuickAddResident_Click;
			break;
		case 25:
			quickTileCert = (Button)target;
			quickTileCert.Click += QuickAddCertificate_Click;
			break;
		case 26:
			quickTileBlotter = (Button)target;
			quickTileBlotter.Click += QuickAddBlotter_Click;
			break;
		case 27:
			quickTileReports = (Button)target;
			quickTileReports.Click += QuickOpenReports_Click;
			break;
		case 28:
			btnAnnouncementRegistry = (Button)target;
			btnAnnouncementRegistry.Click += BtnOpenGovernanceRegistry_Click;
			break;
		case 29:
			btnAnnouncementNew = (Button)target;
			btnAnnouncementNew.Click += BtnAnnouncementNew_Click;
			break;
		case 30:
			announcementCards = (StackPanel)target;
			break;
		case 31:
			projectsPanel = (Border)target;
			break;
		case 32:
			btnProjectRegistry = (Button)target;
			btnProjectRegistry.Click += BtnOpenGovernanceRegistry_Click;
			break;
		case 33:
			btnProjectNew = (Button)target;
			btnProjectNew.Click += BtnProjectNew_Click;
			break;
		case 34:
			projectCards = (StackPanel)target;
			break;
		case 35:
			governanceToolsPanel = (Border)target;
			break;
		case 36:
			actionCalendar = (Calendar)target;
			break;
		case 37:
			calendarInfoText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
