using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.OneTimePassword
{
    public class Main : IPlugin, IPluginI18n, ISettingProvider
    {
        public static ImageSource LoadImage(string path) => new BitmapImage(new Uri(path));

        private PluginInitContext Context = new();
        private Setting Settings = new();

        public void Init(PluginInitContext context)
        {
            Context = context;
            Settings = context.API.LoadSettingJsonStorage<Setting>();
        }

        public List<Result> Query(Query query)
        {
            var API = Context.API;
            var GetTranslation = Context.API.GetTranslation;

            var otps = Settings.Otps;
            if (otps == null || otps.Count == 0)
                return new List<Result> {
                    new() {
                        Title = GetTranslation("flowlauncher_plugin_otp_main_empty_title"),
                        SubTitle = GetTranslation("flowlauncher_plugin_otp_main_empty_sub_title"),
                        Action = (_) =>
                        {
                            API.OpenSettingDialog();
                            return true;
                        },
                    }
                };

            Result compute(Otp otp)
            {
                var (password, remaining) = otp.Compute();
                return new Result
                {
                    Title = $"{password} - {otp.Label}",
                    SubTitle = $"{GetTranslation("flowlauncher_plugin_otp_main_expired")}: {remaining}s ({GetTranslation("flowlauncher_plugin_otp_main_copy")})",
                    Icon = string.IsNullOrEmpty(otp.Icon) ? null : () => LoadImage(otp.Icon),
                    Action = (_) =>
                        {
                            API.CopyToClipboard(password, showDefaultNotification: false);
                            return true;
                        }
                };
            }

            if (string.IsNullOrEmpty(query.Search))
                return new List<Result>(otps.Select(compute));
            else
                return new List<Result>(otps.Where(otp => API.FuzzySearch(query.Search, otp.Label).Success).Select(compute));
        }

        public string? GetTranslatedPluginTitle() => Context.API.GetTranslation("flowlauncher_plugin_otp_plugin_title");
        public string? GetTranslatedPluginDescription() => Context.API.GetTranslation("flowlauncher_plugin_otp_plugin_description");

        public Control CreateSettingPanel()
        {
            var control = new SettingControl(Context, Settings);
            control.OnUpdate += (sender, settings) =>
            {
                Settings = settings;
                Context.API.SaveSettingJsonStorage<Setting>();
            };

            return control;
        }
    }
}
