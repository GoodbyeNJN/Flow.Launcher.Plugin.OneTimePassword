using Microsoft.Win32;
using System.Windows;

namespace Flow.Launcher.Plugin.OneTimePassword
{
    public class EditorWindowViewModel : BaseModel
    {
        public readonly PluginInitContext Context;

        private string uri = "";
        public string Uri
        {
            get => uri;
            set
            {
                var trimmed = value.Trim();
                if (uri == trimmed || string.IsNullOrEmpty(trimmed)) return;
                if (!Otp.ValidateUri(trimmed))
                {
                    var warning = Context.API.GetTranslation("flowlauncher_plugin_otp_editor_form_uri_invalid_warning");
                    MessageBox.Show(warning);
                    return;
                }

                if (string.IsNullOrEmpty(Label))
                    Label = Otp.GetOtpLabel(trimmed);

                uri = trimmed;
                OnPropertyChanged(nameof(uri));
            }
        }

        private string label = "";
        public string Label
        {
            get => label;
            set
            {
                var trimmed = value.Trim();
                if (label == trimmed || string.IsNullOrEmpty(trimmed)) return;

                label = trimmed;
                OnPropertyChanged(nameof(label));
            }
        }

        private string icon = "";
        public string? Icon
        {
            get => icon;
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                icon = value;
                OnPropertyChanged(nameof(icon));
            }
        }

        public EditorWindowViewModel(PluginInitContext context) => Context = context;

        public EditorWindowViewModel(PluginInitContext context, Otp otp) : this(context)
        {
            Uri = otp.Uri;
            Label = otp.Label;
            Icon = otp.Icon;
        }

        public Otp ToOtp() => new(Uri) { Label = Label, Icon = Icon };
    }

    public partial class EditorWindow : Window
    {
        private readonly PluginInitContext Context;
        private readonly EditorWindowViewModel ViewModel;

        public event EventHandler<Otp>? OnConfirmed;

        public EditorWindow(PluginInitContext context)
        {
            InitializeComponent();

            Context = context;
            DataContext = ViewModel = new EditorWindowViewModel(context);
        }

        public EditorWindow(PluginInitContext context, Otp otp) : this(context)
        {
            DataContext = ViewModel = new EditorWindowViewModel(context, otp);

            LoadImage(otp.Icon);
        }

        private void LoadImage(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                OtpIcon.Source = Main.LoadImage(path);
            }
            catch { }
        }

        private void OnSelectIcon(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.gif, *.png, *.ico)|*.jpg; *.jpeg; *.gif; *.png; *.ico|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() != true) return;

            ViewModel.Icon = dialog.FileName;
            LoadImage(dialog.FileName);
        }

        private void OnCancel(object sender, RoutedEventArgs e) => Close();

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            var GetTranslation = Context.API.GetTranslation;

            if (string.IsNullOrEmpty(ViewModel.Uri))
            {
                var warning = GetTranslation("flowlauncher_plugin_otp_editor_form_uri_empty_warning");
                MessageBox.Show(warning);
                return;
            }
            if (string.IsNullOrEmpty(ViewModel.Label))
            {
                var warning = GetTranslation("flowlauncher_plugin_otp_editor_form_label_empty_warning");
                MessageBox.Show(warning);
                return;
            }

            OnConfirmed?.Invoke(this, ViewModel.ToOtp());
            Close();
        }
    }
}
