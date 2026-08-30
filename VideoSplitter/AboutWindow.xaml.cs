using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace VideoSplitter
{
    public partial class AboutWindow : Window
    {
        private const string GitHubProfileUrl =
            "https://github.com/cenkberkgungor";

        private const string RepositoryUrl =
            "https://github.com/cenkberkgungor/TelegramVideoSplitter";

        private const string IssuesUrl =
            "https://github.com/cenkberkgungor/TelegramVideoSplitter/issues";

        private readonly bool _isTurkish;

        public AboutWindow(
            bool isTurkish)
        {
            InitializeComponent();

            _isTurkish =
                isTurkish;

            RefreshLanguage();
        }

        private string T(
            string turkish,
            string english)
        {
            return _isTurkish
                ? turkish
                : english;
        }

        private void RefreshLanguage()
        {
            string version =
                GetAppVersion();

            Title =
                T(
                    "Hakkında - VideoSplitter",
                    "About - VideoSplitter");

            VersionTextBlock.Text =
                T(
                    $"Sürüm {version}",
                    $"Version {version}");

            DescriptionTextBlock.Text =
                T(
                    "Videoları kalite kaybı olmadan parçalara böler ve isteğe bağlı olarak Telegram'a yükler.",
                    "Splits videos into parts without quality loss and can optionally upload them to Telegram.");

            DeveloperLabelTextBlock.Text =
                T(
                    "Geliştirici",
                    "Developer");

            GitHubProfileButton.Content =
                T(
                    "GitHub Profili",
                    "GitHub Profile");

            ProjectRepositoryButton.Content =
                T(
                    "Proje Deposu",
                    "Project Repository");

            ReportIssueButton.Content =
                T(
                    "Sorun Bildir / GitHub Issues",
                    "Report an Issue / GitHub Issues");

            TechnologyTextBlock.Text =
                T(
                    "FFmpeg ile çalışır • Telegram entegrasyonu: WTelegramClient",
                    "Powered by FFmpeg • Telegram integration: WTelegramClient");

            CloseButton.Content =
                T(
                    "KAPAT",
                    "CLOSE");
        }

        private void GitHubProfileButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenUrl(
                GitHubProfileUrl);
        }

        private void ProjectRepositoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenUrl(
                RepositoryUrl);
        }

        private void ReportIssueButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenUrl(
                IssuesUrl);
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void OpenUrl(
            string url)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            url,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    T(
                        "Bağlantı tarayıcıda açılamadı.\n\n",
                        "The link could not be opened in your browser.\n\n") +
                    ex.Message,

                    T(
                        "Bağlantı Hatası",
                        "Link Error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static string GetAppVersion()
        {
            Version? version =
                Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version;

            if (version == null)
            {
                return "1.1.0";
            }

            return
                $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
        }
    }
}
