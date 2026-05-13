using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace baranggaysystem1.Views;

public partial class RegisterWindow : Window
{
	private readonly RegisterViewModel _vm;















	internal event Action? RegistrationCompleted;

	internal event Action? BackToLoginRequested;

	public RegisterWindow()
	{
		InitializeComponent();
		_vm = (RegisterViewModel)base.DataContext;
		_vm.RegistrationCompleted += delegate
		{
			this.RegistrationCompleted?.Invoke();
		};
		_vm.BackToLoginRequested += delegate
		{
			this.BackToLoginRequested?.Invoke();
		};
		((ObservableObject)_vm).PropertyChanged += delegate(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "PhotoPath")
			{
				UpdatePhotoPreview(_vm.PhotoPath);
			}
		};
		base.Loaded += async delegate
		{
			BeginFadeIn();
			LoadDynamicBranding();
			UpdatePhotoPreview(_vm.PhotoPath);
			txtUsername.Focus();
			await _vm.LoadAsync();
		};
	}

	private void BeginFadeIn()
	{
		base.Opacity = 0.0;
		BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(350.0)));
	}

	private async void BtnRegister_Click(object sender, RoutedEventArgs e)
	{
		btnRegister.IsEnabled = false;
		try
		{
			await _vm.RegisterCommand.ExecuteAsync(txtPassword.Password);
		}
		finally
		{
			btnRegister.IsEnabled = true;
		}
	}

	private void LoadDynamicBranding()
	{
		try
		{
			SystemBrandingSettings systemBrandingSettings = SystemConfigService.LoadBrandingSettings();
			SystemOfficeSettings office = SystemConfigService.LoadOfficeSettings();
			string systemName = systemBrandingSettings.SystemName;
			base.Title = systemName + " - Register";
			brandPrimaryText.Text = BuildGovernmentLabel(systemBrandingSettings);
			brandSecondaryText.Text = BuildProfileLine(systemBrandingSettings);
			brandAddressText.Text = BuildOfficeLine(systemBrandingSettings, office);
			softwareNameText.Text = systemName;
			softwareSubtitleText.Text = BuildRegisterSubtitle(systemBrandingSettings);
			string text = BuildSystemInitials(systemName);
			logoFallbackText.Text = text;
			rightLogoFallbackText.Text = text;
			ApplyLogo(SystemConfigService.GetLogo());
		}
		catch
		{
		}
	}

	private void ApplyLogo(BitmapImage? logo)
	{
		bool flag = logo != null;
		logoImage.Source = logo;
		logoImage.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		logoFallback.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		rightLogoImage.Source = logo;
		rightLogoImage.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		rightLogoFallback.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdatePhotoPreview(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			photoPreview.Source = CreateDefaultAvatar();
			return;
		}
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(path);
			bitmapImage.EndInit();
			((Freezable)bitmapImage).Freeze();
			photoPreview.Source = bitmapImage;
		}
		catch
		{
			photoPreview.Source = CreateDefaultAvatar();
		}
	}

	private static string BuildGovernmentLabel(SystemBrandingSettings branding)
	{
		string text = FirstNonPlaceholder(branding.Municipality, "Municipality", branding.BarangayName, string.Empty, "Your Barangay");
		return "Local Government of " + text;
	}

	private static string BuildProfileLine(SystemBrandingSettings branding)
	{
		List<string> list = new List<string>();
		AddIfMeaningful(list, branding.BarangayName);
		AddIfMeaningful(list, branding.Province, "Province");
		if (list.Count <= 0)
		{
			return "Barangay profile";
		}
		return string.Join(" | ", list);
	}

	private static string BuildOfficeLine(SystemBrandingSettings branding, SystemOfficeSettings office)
	{
		if (!string.IsNullOrWhiteSpace(office.OfficeAddress))
		{
			return office.OfficeAddress.Trim();
		}
		List<string> list = new List<string>();
		AddIfMeaningful(list, branding.Municipality, "Municipality");
		AddIfMeaningful(list, branding.Province, "Province");
		AddIfMeaningful(list, branding.Region, "Region");
		if (list.Count <= 0)
		{
			return "Update office details in System Settings.";
		}
		return string.Join(", ", list);
	}

	private static string BuildRegisterSubtitle(SystemBrandingSettings branding)
	{
		string text = (string.IsNullOrWhiteSpace(branding.BarangayName) ? "your barangay" : branding.BarangayName.Trim());
		return "Create staff access for " + text;
	}

	private static void AddIfMeaningful(ICollection<string> values, string? value, string placeholder = "")
	{
		string text = value?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text) && (string.IsNullOrWhiteSpace(placeholder) || !string.Equals(text, placeholder, StringComparison.OrdinalIgnoreCase)))
		{
			values.Add(text);
		}
	}

	private static string FirstNonPlaceholder(string? primaryValue, string primaryPlaceholder, string? fallbackValue, string fallbackPlaceholder, string finalFallback)
	{
		if (IsMeaningful(primaryValue, primaryPlaceholder))
		{
			return primaryValue.Trim();
		}
		if (IsMeaningful(fallbackValue, fallbackPlaceholder))
		{
			return fallbackValue.Trim();
		}
		return finalFallback;
	}

	private static bool IsMeaningful(string? value, string placeholder)
	{
		string text = value?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return !string.Equals(text, placeholder, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string BuildSystemInitials(string systemName)
	{
		string text = string.Concat(from part in (systemName ?? string.Empty).Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			where !string.Equals(part, "and", StringComparison.OrdinalIgnoreCase)
			select char.ToUpperInvariant(part[0]));
		if (string.IsNullOrWhiteSpace(text))
		{
			return "BMS";
		}
		if (text.Length > 4)
		{
			return text.Substring(0, 4);
		}
		return text;
	}

	private static DrawingImage CreateDefaultAvatar()
	{
		return new DrawingImage(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(200, 200, 200)), null, Geometry.Parse("M 36,20 A 16,16 0 1 1 36,19.9 Z M 12,60 C 12,44 22,36 36,36 C 50,36 60,44 60,60 Z")));
	}}
