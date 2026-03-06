using baranggaysystem1.helper;

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
        }

        public void Save()
        {
            var settings = _form.ReadSettingsFromInputs();
            SidebarSettingsStore.Save(settings);
            _form.DialogResult = System.Windows.Forms.DialogResult.OK;
            _form.Close();
        }

        public void ResetToDefaults()
        {
            _form.ApplySettingsToInputs(SidebarBehaviorSettings.CreateDefault());
        }
    }
}

