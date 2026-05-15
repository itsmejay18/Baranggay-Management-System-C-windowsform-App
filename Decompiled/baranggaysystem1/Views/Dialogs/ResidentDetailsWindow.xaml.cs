using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using baranggaysystem1.ViewModels;
using Microsoft.Win32;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResidentDetailsWindow : Window
{
	private readonly ResidentDetailsViewModel _vm;

	public ResidentDetailsWindow(ResidentDto? existingResident = null)
	{
		InitializeComponent();
		_vm = new ResidentDetailsViewModel(existingResident)
		{
			CloseAction = delegate(bool result)
			{
				DialogResult = result;
				Close();
			}
		};
		DataContext = _vm;

		// Load existing photo if available
		if (existingResident?.PhotoBytes != null && existingResident.PhotoBytes.Length > 0)
		{
			LoadPhotoFromBytes(existingResident.PhotoBytes);
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
				byte[] photoBytes = File.ReadAllBytes(dialog.FileName);
				_vm.SetPhotoBytes(photoBytes);
				LoadPhotoFromBytes(photoBytes);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to load photo: {ex.Message}", "Photo Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
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
}
