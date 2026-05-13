using CommunityToolkit.Mvvm.ComponentModel;

namespace baranggaysystem1.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
	private bool _isBusy;

	private string _busyMessage = string.Empty;

	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			SetProperty<bool>(ref _isBusy, value, "IsBusy");
		}
	}

	public string BusyMessage
	{
		get
		{
			return _busyMessage;
		}
		set
		{
			SetProperty<string>(ref _busyMessage, value, "BusyMessage");
		}
	}

	protected void SetBusy(bool busy, string message = "Loading…")
	{
		IsBusy = busy;
		BusyMessage = (busy ? message : string.Empty);
	}
}
