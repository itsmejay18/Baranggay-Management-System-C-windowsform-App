using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace baranggaysystem1.helper;

/// <summary>
/// Manages session timeout and auto-lock functionality.
/// Monitors user activity (mouse/keyboard) and locks the session after inactivity.
/// </summary>
internal sealed class SessionTimeoutManager : IDisposable
{
    private static SessionTimeoutManager? _instance;
    private static readonly object InstanceLock = new object();

    private readonly DispatcherTimer _inactivityTimer;
    private readonly DispatcherTimer _warningTimer;
    private DateTime _lastActivity;
    private bool _isLocked;
    private bool _warningShown;
    private bool _disposed;

    /// <summary>
    /// Inactivity timeout in minutes before auto-lock. Default: 15 minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Warning shown X minutes before lock. Default: 2 minutes.
    /// </summary>
    public int WarningMinutesBeforeLock { get; set; } = 2;

    /// <summary>
    /// Whether session timeout is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the session is currently locked.
    /// </summary>
    public bool IsLocked => _isLocked;

    /// <summary>
    /// Fired when the session is locked due to inactivity.
    /// </summary>
    public event Action? SessionLocked;

    /// <summary>
    /// Fired when a warning is shown before lock.
    /// </summary>
    public event Action<int>? InactivityWarning;

    /// <summary>
    /// Fired when the session is unlocked after re-authentication.
    /// </summary>
    public event Action? SessionUnlocked;

    public static SessionTimeoutManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (InstanceLock)
                {
                    _instance ??= new SessionTimeoutManager();
                }
            }
            return _instance;
        }
    }

    private SessionTimeoutManager()
    {
        _lastActivity = DateTime.UtcNow;

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _inactivityTimer.Tick += CheckInactivity;

        _warningTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _warningTimer.Tick += CheckWarning;
    }

    /// <summary>
    /// Start monitoring user activity. Call after successful login.
    /// </summary>
    public void Start()
    {
        if (!IsEnabled) return;

        _lastActivity = DateTime.UtcNow;
        _isLocked = false;
        _warningShown = false;

        // Hook into application-level input events
        if (Application.Current?.MainWindow != null)
        {
            InputManager.Current.PreProcessInput += OnPreProcessInput;
        }

        _inactivityTimer.Start();
        _warningTimer.Start();

        AppLogger.LogInfo($"Session timeout started. Lock after {TimeoutMinutes} min of inactivity.");
    }

    /// <summary>
    /// Stop monitoring. Call on logout.
    /// </summary>
    public void Stop()
    {
        _inactivityTimer.Stop();
        _warningTimer.Stop();
        InputManager.Current.PreProcessInput -= OnPreProcessInput;
        _isLocked = false;
        _warningShown = false;
    }

    /// <summary>
    /// Record user activity (resets the inactivity timer).
    /// </summary>
    public void RecordActivity()
    {
        _lastActivity = DateTime.UtcNow;
        _warningShown = false;
    }

    /// <summary>
    /// Attempt to unlock the session with password verification.
    /// </summary>
    public bool TryUnlock(string password)
    {
        if (!_isLocked) return true;

        try
        {
            string storedHash = GetCurrentUserPasswordHash();
            if (string.IsNullOrEmpty(storedHash)) return false;

            var result = Database.PasswordHelper.VerifyPassword(password, storedHash, out _);
            if (result == Database.PasswordHelper.VerificationResult.Failed)
            {
                AppLogger.LogWarning($"Failed unlock attempt for user '{UserSession.Username}'.");
                return false;
            }

            _isLocked = false;
            _lastActivity = DateTime.UtcNow;
            _warningShown = false;
            SessionUnlocked?.Invoke();
            AppLogger.LogInfo($"Session unlocked for user '{UserSession.Username}'.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error during session unlock.", ex);
            return false;
        }
    }

    /// <summary>
    /// Manually lock the session (e.g., user clicks "Lock" button).
    /// </summary>
    public void LockNow()
    {
        if (_isLocked) return;
        PerformLock("Manual lock requested.");
    }

    /// <summary>
    /// Load timeout settings from system config.
    /// </summary>
    public void LoadSettings()
    {
        try
        {
            string timeoutStr = Services.SystemConfigService.Get("session_timeout_minutes", "15");
            if (int.TryParse(timeoutStr, out int timeout) && timeout >= 1 && timeout <= 480)
            {
                TimeoutMinutes = timeout;
            }

            string enabledStr = Services.SystemConfigService.Get("session_timeout_enabled", "true");
            IsEnabled = !string.Equals(enabledStr, "false", StringComparison.OrdinalIgnoreCase);

            string warningStr = Services.SystemConfigService.Get("session_warning_minutes", "2");
            if (int.TryParse(warningStr, out int warning) && warning >= 1 && warning <= 10)
            {
                WarningMinutesBeforeLock = warning;
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to load session timeout settings.", ex);
        }
    }

    /// <summary>
    /// Save timeout settings to system config.
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            Services.SystemConfigService.Set("session_timeout_minutes", TimeoutMinutes.ToString());
            Services.SystemConfigService.Set("session_timeout_enabled", IsEnabled.ToString().ToLower());
            Services.SystemConfigService.Set("session_warning_minutes", WarningMinutesBeforeLock.ToString());
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Failed to save session timeout settings.", ex);
        }
    }

    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        if (e.StagingItem.Input is MouseEventArgs || e.StagingItem.Input is KeyboardEventArgs)
        {
            if (!_isLocked)
            {
                _lastActivity = DateTime.UtcNow;
                _warningShown = false;
            }
        }
    }

    private void CheckInactivity(object? sender, EventArgs e)
    {
        if (!IsEnabled || _isLocked) return;

        double inactiveMinutes = (DateTime.UtcNow - _lastActivity).TotalMinutes;
        if (inactiveMinutes >= TimeoutMinutes)
        {
            PerformLock($"Session locked after {TimeoutMinutes} minutes of inactivity.");
        }
    }

    private void CheckWarning(object? sender, EventArgs e)
    {
        if (!IsEnabled || _isLocked || _warningShown) return;

        double inactiveMinutes = (DateTime.UtcNow - _lastActivity).TotalMinutes;
        double warningThreshold = TimeoutMinutes - WarningMinutesBeforeLock;

        if (inactiveMinutes >= warningThreshold)
        {
            _warningShown = true;
            int remainingSeconds = (int)((TimeoutMinutes - inactiveMinutes) * 60);
            InactivityWarning?.Invoke(Math.Max(remainingSeconds, 0));
        }
    }

    private void PerformLock(string reason)
    {
        _isLocked = true;
        _inactivityTimer.Stop();
        _warningTimer.Stop();
        AppLogger.LogInfo(reason);
        SessionLocked?.Invoke();
    }

    private static string GetCurrentUserPasswordHash()
    {
        if (UserSession.UserId <= 0) return string.Empty;

        try
        {
            return Database.DbHelper.ExecuteScalar<string>(
                "SELECT password_hash FROM user_account WHERE user_id = @id AND is_active = 1 LIMIT 1",
                cmd => cmd.Parameters.AddWithValue("@id", (object)UserSession.UserId)
            ) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _inactivityTimer.Stop();
        _warningTimer.Stop();
    }
}
