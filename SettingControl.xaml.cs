using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.OneTimePassword
{
    public class SettingControlViewModel : BaseModel
    {
        private readonly Setting Settings;

        private ObservableCollection<Otp> otps = new();
        public ObservableCollection<Otp> Otps
        {
            get => otps;
            set
            {
                otps = value;
                OnPropertyChanged(nameof(otps));
            }
        }

        public SettingControlViewModel(Setting settings)
        {
            Settings = settings;
            Otps = new(settings.Otps);
        }

        public Setting ToSettings()
        {
            Settings.Otps = Otps.ToList();
            return Settings;
        }
    }

    public partial class SettingControl : UserControl
    {
        private readonly PluginInitContext Context;
        private readonly SettingControlViewModel ViewModel;

        public event EventHandler<Setting>? OnUpdate;

        public SettingControl(PluginInitContext context, Setting settings)
        {
            InitializeComponent();

            Context = context;
            DataContext = ViewModel = new(settings);
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            var window = new EditorWindow(Context);
            window.OnConfirmed += (object? sender, Otp otp) =>
            {
                ViewModel.Otps.Add(otp);
                OnUpdate?.Invoke(this, ViewModel.ToSettings()); 
            };
            window.ShowDialog();
        }

        private void OnDelete(object sender, RoutedEventArgs e)
        {
            var index = ListView.SelectedIndex;
            var selected = ViewModel.Otps.ElementAtOrDefault(index);
            if (selected == null) return;

            var warning = Context.API.GetTranslation("flowlauncher_plugin_otp_setting_delete_warning");
            var result = MessageBox.Show(string.Format(warning, selected.Label), "", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            ViewModel.Otps.RemoveAt(index);
            OnUpdate?.Invoke(this, ViewModel.ToSettings());
        }

        private void OnEdit(object sender, RoutedEventArgs e)
        {
            var index = ListView.SelectedIndex;
            var selected = ViewModel.Otps.ElementAtOrDefault(index);
            if (selected == null) return;

            var window = new EditorWindow(Context, selected);
            window.OnConfirmed += (object? sender, Otp otp) =>
            {
                ViewModel.Otps[index] = otp;
                OnUpdate?.Invoke(this, ViewModel.ToSettings());
            };
            window.ShowDialog();
        }
    }
}
