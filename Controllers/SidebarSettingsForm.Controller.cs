using baranggaysystem1.helper;
using baranggaysystem1.Database;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class SidebarSettingsForm
{
    private sealed class SidebarSettingsFormController
    {
        private readonly SidebarSettingsForm _form;

        public SidebarSettingsFormController(SidebarSettingsForm form)
        {
            _form = form;
        }

        public void Load()
        {
            var settings = SidebarSettingsStore.Load();
            _form.ApplySettingsToInputs(settings);

            var dbSettings = DbConnectionSettingsStore.LoadOrDefault();
            _form.ApplyDatabaseSettings(dbSettings);
            _form.SetDatabaseModeLabel(OfflineDatabaseSupport.IsOffline);
            _form.SetDatabaseStatus(
                OfflineDatabaseSupport.IsOffline
                    ? "Offline mode is active. Data shown comes from local SQLite cache."
                    : "Online mode is active. Data is loaded from MySQL.",
                isSuccess: !OfflineDatabaseSupport.IsOffline,
                isError: false);
        }

        public void Save()
        {
            var settings = _form.ReadSettingsFromInputs();
            SidebarSettingsStore.Save(settings);

            var dbSettings = _form.ReadDatabaseSettings();
            DbConnectionSettingsStore.Save(dbSettings);

            if (!OfflineDatabaseSupport.IsOffline)
            {
                string profileConnection = DbConnectionSettingsStore.BuildConnectionString(dbSettings, includeDatabase: true);
                if (DBConnection.TryGetWorkingConnectionString(profileConnection, out string workingConnectionString, out string errorMessage))
                {
                    DBConnection.SetRuntimeConnectionString(workingConnectionString);
                    _form.SetDatabaseStatus("Settings saved and applied to the active online connection.", isSuccess: true, isError: false);
                }
                else
                {
                    _form.SetDatabaseStatus(
                        $"Settings saved, but active connection could not be refreshed: {errorMessage}",
                        isSuccess: false,
                        isError: true);
                }
            }
            else
            {
                _form.SetDatabaseStatus("Settings saved. Click \"Switch Online\" to reconnect and load MySQL data.", isSuccess: false, isError: false);
            }

            _form.DialogResult = DialogResult.OK;
            _form.Close();
        }

        public void ResetToDefaults()
        {
            _form.ApplySettingsToInputs(SidebarBehaviorSettings.CreateDefault());
            _form.ApplyDatabaseSettings(DatabaseConnectionProfile.CreateDefault());
            _form.SetDatabaseStatus("Sidebar and database fields reset to defaults.", isSuccess: false, isError: false);
        }

        public async void TestDatabaseConnection()
        {
            DatabaseConnectionProfile profile = _form.ReadDatabaseSettings();
            _form.SetDatabaseActionBusy(true);
            _form.SetDatabaseStatus("Testing MySQL connection...", isSuccess: false, isError: false);

            (bool ok, string message) result = await Task.Run(() =>
            {
                string requestedConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: true);
                if (DBConnection.TryGetWorkingConnectionString(requestedConnection, out _, out string error))
                {
                    return (true, "Connection successful. MySQL is reachable with these settings.");
                }

                return (false, $"Connection failed: {error}");
            });

            _form.SetDatabaseStatus(result.message, isSuccess: result.ok, isError: !result.ok);
            _form.SetDatabaseActionBusy(false);
        }

        public async void SwitchToOnlineMode()
        {
            DatabaseConnectionProfile profile = _form.ReadDatabaseSettings();
            _form.SetDatabaseActionBusy(true);
            _form.SetDatabaseStatus("Switching to online mode...", isSuccess: false, isError: false);

            (bool ok, string message) result = await Task.Run(() => TrySwitchToOnline(profile));

            _form.SetDatabaseModeLabel(OfflineDatabaseSupport.IsOffline);
            _form.SetDatabaseStatus(result.message, isSuccess: result.ok, isError: !result.ok);
            _form.SetDatabaseActionBusy(false);
        }

        public async void SwitchToOfflineMode()
        {
            _form.SetDatabaseActionBusy(true);
            _form.SetDatabaseStatus("Switching to offline mode...", isSuccess: false, isError: false);

            (bool ok, string message) result = await Task.Run(() =>
            {
                bool offlineReady = OfflineDatabaseSupport.IsAvailable || OfflineDatabaseSupport.EnsureInitialised();
                if (!offlineReady)
                {
                    return (false, "Offline database is not ready. Could not switch to offline mode.");
                }

                OfflineDatabaseSupport.ActivateOfflineMode();
                return (true, "Offline mode enabled. You can continue working using local cached data.");
            });

            _form.SetDatabaseModeLabel(OfflineDatabaseSupport.IsOffline);
            _form.SetDatabaseStatus(result.message, isSuccess: result.ok, isError: !result.ok);
            _form.SetDatabaseActionBusy(false);
        }

        private static (bool ok, string message) TrySwitchToOnline(DatabaseConnectionProfile profile)
        {
            try
            {
                string requestedConnection = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: true);
                if (!DBConnection.TryGetWorkingConnectionString(requestedConnection, out string workingConnectionString, out string error))
                {
                    bool offlineReady = OfflineDatabaseSupport.IsAvailable || OfflineDatabaseSupport.EnsureInitialised();
                    if (offlineReady)
                    {
                        OfflineDatabaseSupport.ActivateOfflineMode();
                    }

                    return (false, $"Unable to switch online: {error}");
                }

                DbConnectionSettingsStore.Save(profile);
                DBConnection.SetRuntimeConnectionString(workingConnectionString);

                SchemaGuard.EnsureDatabaseReady();
                OfflineDatabaseSupport.ActivateOnlineMode();
                int syncedChanges = OfflineSyncService.TrySyncPendingChanges();
                if (syncedChanges > 0)
                {
                    AppLogger.LogInfo($"[OfflineSync] Replayed {syncedChanges} queued change(s) after switching online from settings.");
                }

                return (true, "Online mode enabled. Live MySQL data is now active.");
            }
            catch (Exception ex)
            {
                bool offlineReady = OfflineDatabaseSupport.IsAvailable || OfflineDatabaseSupport.EnsureInitialised();
                if (offlineReady)
                {
                    OfflineDatabaseSupport.ActivateOfflineMode();
                }

                return (false, $"Switch to online failed: {ex.Message}");
            }
        }
    }
}
