using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VideoSplitter
{
    public partial class MainWindow : Window
    {
        private enum AppLanguage
        {
            Turkish,
            English
        }

        private AppLanguage _language =
            AppLanguage.Turkish;

        private CancellationTokenSource?
            _cancellationTokenSource;

        private Process?
            _currentProcess;

        private string?
            _lastOutputFolder;

        private string _currentStatusKey =
            "Ready";

        private object[] _currentStatusArguments =
            Array.Empty<object>();

        private double _displayPercent =
            0;

        private TimeSpan _displayElapsed =
            TimeSpan.Zero;

        private TimeSpan? _displayRemaining =
            null;

        // =========================================================
        // TELEGRAM
        // =========================================================

        private bool _telegramConnected =
            false;

        private bool _telegramLoading =
            false;

        private string _telegramAccountText =
            "";

        private List<TelegramDestination>
            _telegramDestinations =
                new List<TelegramDestination>();

        private TelegramDestination?
            _selectedTelegramDestination;

        public MainWindow()
        {
            InitializeComponent();

            RefreshLanguage();

            RefreshTelegramPanel();

            RefreshTelegramConnectionUi();
        }

        // =========================================================
        // DİL
        // =========================================================

        private string T(
            string turkish,
            string english)
        {
            return _language ==
                   AppLanguage.Turkish
                ? turkish
                : english;
        }

        private void TurkishButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _language =
                AppLanguage.Turkish;

            RefreshLanguage();
        }

        private void EnglishButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _language =
                AppLanguage.English;

            RefreshLanguage();
        }

        private void RefreshLanguage()
        {
            string appVersion =
                GetAppVersion();

            Title =
                $"VideoSplitter v{appVersion}";

            AppTitleTextBlock.Text =
                "VideoSplitter";

            AboutButton.Content =
                T(
                    "Hakkında",
                    "About");

            FooterVersionTextBlock.Text =
                $"v{appVersion}";

            FooterGitHubButton.Content =
                "GitHub";

            SubtitleTextBlock.Text =
                T(
                    "Videolarınızı kalite kaybı olmadan parçalara bölün.",
                    "Split your videos into parts without quality loss.");

            VideoFileLabel.Text =
                T(
                    "Video Dosyası",
                    "Video File");

            SelectVideoButton.Content =
                T(
                    "Video Seç",
                    "Select Video");

            PartSizeLabel.Text =
                T(
                    "Maksimum Part Boyutu",
                    "Maximum Part Size");

            PartSizeHintTextBlock.Text =
                T(
                    "Bu değer maksimum sınırdır.",
                    "This value is the maximum limit.");

            PartSuffixLabel.Text =
                T(
                    "Part Etiketi",
                    "Part Label");

            PartSuffixHintTextBlock.Text =
                T(
                    "Örn: video-PRT1.mp4",
                    "Example: video-PRT1.mp4");

            OutputFolderLabel.Text =
                T(
                    "Çıktı Klasörü",
                    "Output Folder");

            SelectOutputButton.Content =
                T(
                    "Klasör Seç",
                    "Select Folder");

            SplitButton.Content =
                T(
                    "VİDEOYU BÖL",
                    "SPLIT VIDEO");

            CancelButton.Content =
                T(
                    "İPTAL ET",
                    "CANCEL");

            OpenOutputButton.Content =
                T(
                    "ÇIKTI KLASÖRÜNÜ AÇ",
                    "OPEN OUTPUT FOLDER");

            // Telegram
            TelegramTitleTextBlock.Text =
                T(
                    "Telegram (İsteğe Bağlı)",
                    "Telegram (Optional)");

            TelegramUploadCheckBox.Content =
                T(
                    "Bölme bittikten sonra Telegram'a yükle",
                    "Upload to Telegram after splitting");

            TelegramAccountLabel.Text =
                T(
                    "Telegram Hesabı",
                    "Telegram Account");

            TelegramSettingsButton.Content =
                T(
                    "Telegram Ayarları",
                    "Telegram Settings");

            TelegramTargetLabel.Text =
                T(
                    "Hedef",
                    "Destination");

            TelegramParallelLabel.Text =
                T(
                    "Paralel Transfer",
                    "Parallel Transfers");

            TelegramParallelAutoItem.Content =
                T(
                    "Otomatik (4)",
                    "Automatic (4)");

            TelegramParallelHintTextBlock.Text =
                T(
                    "Testimizde 4 en iyi sonucu verdi.",
                    "4 gave the best result in our test.");

            TelegramAlbumLabel.Text =
                T(
                    "Albüm Açıklaması",
                    "Album Caption");

            TelegramAlbumHintTextBlock.Text =
                T(
                    "Boş bırakılırsa video dosya adı kullanılır.",
                    "If empty, video file name is used.");

            TelegramDeletePartsCheckBox.Content =
                T(
                    "Yüklemeden sonra yerel partları sil",
                    "Delete local parts after upload");

            TurkishButton.FontWeight =
                _language ==
                AppLanguage.Turkish
                    ? FontWeights.Bold
                    : FontWeights.Normal;

            EnglishButton.FontWeight =
                _language ==
                AppLanguage.English
                    ? FontWeights.Bold
                    : FontWeights.Normal;

            TurkishButton.Opacity =
                _language ==
                AppLanguage.Turkish
                    ? 1.0
                    : 0.60;

            EnglishButton.Opacity =
                _language ==
                AppLanguage.English
                    ? 1.0
                    : 0.60;

            if (VideoPathTextBox.Text.Length == 0 &&
                (OutputPathTextBox.Text ==
                 "Video ile aynı klasör" ||
                 OutputPathTextBox.Text ==
                 "Same folder as video"))
            {
                OutputPathTextBox.Text =
                    T(
                        "Video ile aynı klasör",
                        "Same folder as video");
            }

            RefreshProgressTexts();

            RefreshStatusText();

            RefreshTelegramConnectionUi();
        }

        // =========================================================
        // HAKKINDA + GITHUB
        // =========================================================

        private void AboutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow window =
                new AboutWindow(
                    _language ==
                    AppLanguage.Turkish)
                {
                    Owner =
                        this
                };

            window.ShowDialog();
        }

        private void FooterGitHubButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenExternalUrl(
                "https://github.com/cenkberkgungor/TelegramVideoSplitter");
        }

        private void OpenExternalUrl(
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
                return "1.2.0";
            }

            return
                $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
        }

        // =========================================================
        // TELEGRAM PANEL
        // =========================================================

        private async void TelegramUploadCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            RefreshTelegramPanel();

            if (TelegramUploadCheckBox.IsChecked ==
                    true &&
                !_telegramConnected &&
                !_telegramLoading &&
                TelegramService.HasSavedSession)
            {
                await LoadTelegramConnectionAsync(
                    false);
            }
        }

        private void RefreshTelegramPanel()
        {
            if (TelegramOptionsPanel == null ||
                TelegramUploadCheckBox == null)
            {
                return;
            }

            TelegramOptionsPanel.IsEnabled =
                TelegramUploadCheckBox.IsChecked ==
                true;
        }

        // =========================================================
        // TELEGRAM AYARLARI
        // =========================================================

        private async void TelegramSettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            TelegramSettingsWindow window =
                new TelegramSettingsWindow
                {
                    Owner =
                        this
                };

            window.ShowDialog();

            await LoadTelegramConnectionAsync(
                true);
        }

        // =========================================================
        // TELEGRAM BAĞLANTISI
        // =========================================================

        private async Task LoadTelegramConnectionAsync(
            bool showErrors)
        {
            if (_telegramLoading)
            {
                return;
            }

            _telegramLoading =
                true;

            try
            {
                TelegramSettingsButton.IsEnabled =
                    false;

                TelegramConnectionStatusTextBlock.Text =
                    T(
                        "Bağlantı kontrol ediliyor...",
                        "Checking connection...");

                TelegramTargetComboBox.IsEnabled =
                    false;

                TelegramConnectionSnapshot snapshot =
                    await TelegramService
                    .LoadConnectionAsync();

                if (!snapshot.IsConnected)
                {
                    _telegramConnected =
                        false;

                    _telegramAccountText =
                        "";

                    _telegramDestinations.Clear();

                    _selectedTelegramDestination =
                        null;

                    RefreshTelegramConnectionUi();

                    if (showErrors &&
                        !string.IsNullOrWhiteSpace(
                            snapshot.ErrorMessage))
                    {
                        MessageBox.Show(
                            snapshot.ErrorMessage,

                            T(
                                "Telegram Bağlantı Hatası",
                                "Telegram Connection Error"),

                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    return;
                }

                _telegramConnected =
                    true;

                _telegramAccountText =
                    snapshot.AccountText;

                _telegramDestinations =
                    snapshot.Destinations;

                if (_telegramDestinations.Count >
                    0)
                {
                    if (_selectedTelegramDestination ==
                        null)
                    {
                        _selectedTelegramDestination =
                            _telegramDestinations[0];
                    }
                }

                RefreshTelegramConnectionUi();
            }
            finally
            {
                _telegramLoading =
                    false;

                TelegramSettingsButton.IsEnabled =
                    true;
            }
        }

        private void RefreshTelegramConnectionUi()
        {
            if (TelegramConnectionStatusTextBlock ==
                    null ||
                TelegramTargetComboBox ==
                    null)
            {
                return;
            }

            TelegramTargetComboBox.SelectionChanged -=
                TelegramTargetComboBox_SelectionChanged;

            TelegramDestinationKind?
                previousKind =
                    _selectedTelegramDestination?.Kind;

            long? previousId =
                _selectedTelegramDestination?.Id;

            TelegramTargetComboBox.Items.Clear();

            if (!_telegramConnected)
            {
                TelegramConnectionStatusTextBlock.Text =
                    T(
                        "Bağlı değil",
                        "Not connected");

                ComboBoxItem item =
                    new ComboBoxItem
                    {
                        Content =
                            T(
                                "Önce Telegram hesabını bağlayın",
                                "Connect your Telegram account first")
                    };

                TelegramTargetComboBox.Items.Add(
                    item);

                TelegramTargetComboBox.SelectedIndex =
                    0;

                TelegramTargetComboBox.IsEnabled =
                    false;

                TelegramTargetComboBox.SelectionChanged +=
                    TelegramTargetComboBox_SelectionChanged;

                return;
            }

            TelegramConnectionStatusTextBlock.Text =
                T(
                    $"Bağlandı: {_telegramAccountText}",
                    $"Connected: {_telegramAccountText}");

            ComboBoxItem?
                itemToSelect =
                    null;

            foreach (TelegramDestination destination
                     in _telegramDestinations)
            {
                ComboBoxItem item =
                    new ComboBoxItem
                    {
                        Content =
                            GetTelegramDestinationText(
                                destination),

                        Tag =
                            destination
                    };

                TelegramTargetComboBox.Items.Add(
                    item);

                if (previousKind ==
                        destination.Kind &&
                    previousId ==
                        destination.Id)
                {
                    itemToSelect =
                        item;
                }
            }

            if (itemToSelect != null)
            {
                TelegramTargetComboBox.SelectedItem =
                    itemToSelect;
            }
            else if (TelegramTargetComboBox.Items.Count >
                     0)
            {
                TelegramTargetComboBox.SelectedIndex =
                    0;
            }

            TelegramTargetComboBox.IsEnabled =
                TelegramTargetComboBox.Items.Count >
                0;

            TelegramTargetComboBox.SelectionChanged +=
                TelegramTargetComboBox_SelectionChanged;

            UpdateSelectedTelegramDestination();
        }

        private string GetTelegramDestinationText(
            TelegramDestination destination)
        {
            if (destination.Kind ==
                TelegramDestinationKind.SavedMessages)
            {
                return T(
                    "Kayıtlı Mesajlar",
                    "Saved Messages");
            }

            string typeText =
                destination.Kind ==
                TelegramDestinationKind.Channel
                    ? T(
                        "Kanal",
                        "Channel")
                    : T(
                        "Grup",
                        "Group");

            string usernameText =
                string.IsNullOrWhiteSpace(
                    destination.Username)
                    ? ""
                    : $" (@{destination.Username})";

            return
                $"{typeText}: " +
                $"{destination.Title}" +
                $"{usernameText}";
        }

        private void TelegramTargetComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            UpdateSelectedTelegramDestination();
        }

        private void UpdateSelectedTelegramDestination()
        {
            if (TelegramTargetComboBox.SelectedItem
                is ComboBoxItem item &&
                item.Tag
                is TelegramDestination destination)
            {
                _selectedTelegramDestination =
                    destination;
            }
        }

        private int GetSelectedParallelTransfers()
        {
            return TelegramParallelComboBox.SelectedIndex switch
            {
                1 => 1,
                2 => 2,
                3 => 4,
                4 => 8,

                // Otomatik
                _ => 4
            };
        }

        // =========================================================
        // STATUS
        // =========================================================

        private void SetStatus(
            string key,
            params object[] arguments)
        {
            _currentStatusKey =
                key;

            _currentStatusArguments =
                arguments;

            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            StatusTextBlock.Text =
                GetStatusText(
                    _currentStatusKey,
                    _currentStatusArguments);
        }

        private string GetStatusText(
            string key,
            object[] arguments)
        {
            switch (key)
            {
                case "Ready":
                    return T(
                        "Hazır",
                        "Ready");

                case "VideoSelected":
                    return T(
                        "Video seçildi",
                        "Video selected");

                case "OutputSelected":
                    return T(
                        "Çıktı klasörü seçildi",
                        "Output folder selected");

                case "Analyzing":
                    return T(
                        "Video analiz ediliyor...",
                        "Analyzing video...");

                case "Splitting":
                    return string.Format(
                        T(
                            "Video bölünüyor... Boyut kontrolü {0}/{1}",
                            "Splitting video... Size check {0}/{1}"),
                        arguments);

                case "Checking":
                    return string.Format(
                        T(
                            "Kontrol ediliyor... En büyük part: {0:0.0} MB",
                            "Checking... Largest part: {0:0.0} MB"),
                        arguments);

                case "Preparing":
                    return T(
                        "Sonuçlar hazırlanıyor...",
                        "Preparing results...");

                case "SplitSuccess":
                    return T(
                        "Video başarıyla bölündü!",
                        "Video split successfully!");

                case "TelegramPreparing":
                    return T(
                        "Telegram yüklemesi hazırlanıyor...",
                        "Preparing Telegram upload...");

                case "TelegramUploading":
                    return string.Format(
                        T(
                            "Telegram'a yükleniyor... {0}/{1} • {2} • Ort: {3:0.00} MB/s",
                            "Uploading to Telegram... {0}/{1} • {2} • Avg: {3:0.00} MB/s"),
                        arguments);

                case "TelegramSuccess":
                    return T(
                        "Video bölündü ve Telegram'a başarıyla yüklendi!",
                        "Video split and uploaded to Telegram successfully!");

                case "TelegramFailed":
                    return T(
                        "Video bölündü fakat Telegram yüklemesi başarısız oldu.",
                        "Video was split, but Telegram upload failed.");

                case "Canceling":
                    return T(
                        "İşlem iptal ediliyor...",
                        "Canceling operation...");

                case "Cancelled":
                    return T(
                        "İşlem iptal edildi",
                        "Operation cancelled");

                case "Error":
                    return T(
                        "Hata oluştu",
                        "An error occurred");

                default:
                    return key;
            }
        }

        // =========================================================
        // PROGRESS TEXT
        // =========================================================

        private void RefreshProgressTexts()
        {
            PercentTextBlock.Text =
                $"{_displayPercent:0}%";

            ElapsedTextBlock.Text =
                $"{T("Geçen", "Elapsed")}: " +
                $"{FormatTime(_displayElapsed)}";

            RemainingTextBlock.Text =
                $"{T("Kalan", "Remaining")}: " +
                (_displayRemaining.HasValue
                    ? FormatTime(
                        _displayRemaining.Value)
                    : "--:--");
        }

        private void UpdateTelegramProgress(
            TelegramUploadProgress progress)
        {
            ProgressBar.IsIndeterminate =
                false;

            ProgressBar.Value =
                progress.OverallPercent;

            _displayPercent =
                progress.OverallPercent;

            _displayElapsed =
                progress.Elapsed;

            _displayRemaining =
                progress.Remaining;

            RefreshProgressTexts();

            SetStatus(
                "TelegramUploading",
                progress.FileIndex,
                progress.FileCount,
                progress.FileName,
                progress.AverageMBps);
        }

        // =========================================================
        // VIDEO SEÇ
        // =========================================================

        private void SelectVideoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new OpenFileDialog();

            openFileDialog.Title =
                T(
                    "Video Dosyası Seç",
                    "Select Video File");

            openFileDialog.Filter =
                T(
                    "Video Dosyaları|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.m4v;*.ts|" +
                    "MP4 Dosyaları|*.mp4|" +
                    "Tüm Dosyalar|*.*",

                    "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.m4v;*.ts|" +
                    "MP4 Files|*.mp4|" +
                    "All Files|*.*");

            openFileDialog.Multiselect =
                false;

            if (openFileDialog.ShowDialog() ==
                true)
            {
                VideoPathTextBox.Text =
                    openFileDialog.FileName;

                string? videoFolder =
                    Path.GetDirectoryName(
                        openFileDialog.FileName);

                if (!string.IsNullOrEmpty(
                        videoFolder))
                {
                    OutputPathTextBox.Text =
                        videoFolder;
                }

                SetStatus(
                    "VideoSelected");
            }
        }

        // =========================================================
        // OUTPUT FOLDER
        // =========================================================

        private void SelectOutputButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFolderDialog folderDialog =
                new OpenFolderDialog();

            folderDialog.Title =
                T(
                    "Çıktı Klasörünü Seç",
                    "Select Output Folder");

            if (!string.IsNullOrWhiteSpace(
                    OutputPathTextBox.Text) &&
                Directory.Exists(
                    OutputPathTextBox.Text))
            {
                folderDialog.InitialDirectory =
                    OutputPathTextBox.Text;
            }

            if (folderDialog.ShowDialog() ==
                true)
            {
                OutputPathTextBox.Text =
                    folderDialog.FolderName;

                SetStatus(
                    "OutputSelected");
            }
        }

        // =========================================================
        // SPLIT
        // =========================================================

        private async void SplitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string inputFile =
                VideoPathTextBox.Text;

            string outputFolder =
                OutputPathTextBox.Text;

            bool telegramRequested =
                TelegramUploadCheckBox.IsChecked ==
                true;

            // =====================================================
            // TELEGRAM ÖN KONTROL
            // =====================================================

            if (telegramRequested)
            {
                if (!_telegramConnected &&
                    TelegramService.HasSavedSession)
                {
                    await LoadTelegramConnectionAsync(
                        true);
                }

                if (!_telegramConnected)
                {
                    MessageBox.Show(
                        T(
                            "Telegram'a yüklemek için önce Telegram hesabınızı bağlayın.",
                            "Connect your Telegram account before uploading to Telegram."),

                        T(
                            "Telegram Bağlantısı Gerekli",
                            "Telegram Connection Required"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                UpdateSelectedTelegramDestination();

                if (_selectedTelegramDestination ==
                    null)
                {
                    MessageBox.Show(
                        T(
                            "Lütfen bir Telegram hedefi seçin.",
                            "Please select a Telegram destination."),

                        T(
                            "Telegram Hedefi Gerekli",
                            "Telegram Destination Required"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }

            // =====================================================
            // NORMAL KONTROLLER
            // =====================================================

            if (string.IsNullOrWhiteSpace(
                    inputFile) ||
                !File.Exists(
                    inputFile))
            {
                MessageBox.Show(
                    T(
                        "Lütfen önce geçerli bir video dosyası seçin.",
                        "Please select a valid video file first."),

                    T(
                        "Video seçilmedi",
                        "No video selected"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    outputFolder) ||
                !Directory.Exists(
                    outputFolder))
            {
                MessageBox.Show(
                    T(
                        "Lütfen geçerli bir çıktı klasörü seçin.",
                        "Please select a valid output folder."),

                    T(
                        "Çıktı klasörü geçersiz",
                        "Invalid output folder"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!double.TryParse(
                    PartSizeTextBox.Text.Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double partSize))
            {
                MessageBox.Show(
                    T(
                        "Lütfen geçerli bir part boyutu girin.",
                        "Please enter a valid part size."),

                    T(
                        "Geçersiz değer",
                        "Invalid value"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (partSize <= 0)
            {
                MessageBox.Show(
                    T(
                        "Part boyutu 0'dan büyük olmalıdır.",
                        "Part size must be greater than 0."),

                    T(
                        "Geçersiz değer",
                        "Invalid value"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string partSuffix =
                new string(
                    PartSuffixTextBox.Text
                    .Where(
                        c => !char.IsWhiteSpace(c))
                    .ToArray());

            partSuffix =
                partSuffix.TrimStart('-');

            if (string.IsNullOrWhiteSpace(
                    partSuffix))
            {
                MessageBox.Show(
                    T(
                        "Lütfen bir part etiketi girin.\n\nÖrneğin: PRT",
                        "Please enter a part label.\n\nExample: PRT"),

                    T(
                        "Part etiketi gerekli",
                        "Part label required"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (partSuffix.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >=
                0)
            {
                MessageBox.Show(
                    T(
                        "Part etiketinde Windows dosya adlarında kullanılamayan karakterler var.",
                        "The part label contains characters that cannot be used in Windows file names."),

                    T(
                        "Geçersiz part etiketi",
                        "Invalid part label"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            PartSuffixTextBox.Text =
                partSuffix;

            string selectedUnit =
                ((ComboBoxItem)
                    SizeUnitComboBox.SelectedItem)
                .Content
                .ToString()!;

            long targetBytes;

            if (selectedUnit ==
                "GB")
            {
                targetBytes =
                    (long)Math.Floor(
                        partSize *
                        1024.0 *
                        1024.0 *
                        1024.0);
            }
            else
            {
                targetBytes =
                    (long)Math.Floor(
                        partSize *
                        1024.0 *
                        1024.0);
            }

            string ffmpegPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "FFmpeg",
                    "ffmpeg.exe");

            string ffprobePath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "FFmpeg",
                    "ffprobe.exe");

            if (!File.Exists(
                    ffmpegPath) ||
                !File.Exists(
                    ffprobePath))
            {
                MessageBox.Show(
                    T(
                        "FFmpeg veya FFprobe bulunamadı.",
                        "FFmpeg or FFprobe could not be found."),

                    T(
                        "FFmpeg hatası",
                        "FFmpeg error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            string baseName =
                Path.GetFileNameWithoutExtension(
                    inputFile);

            string extension =
                Path.GetExtension(
                    inputFile);

            string partSearchPattern =
                $"{baseName}-{partSuffix}*{extension}";

            string[] existingParts =
                Directory.GetFiles(
                    outputFolder,
                    partSearchPattern);

            if (existingParts.Length >
                0)
            {
                MessageBoxResult result =
                    MessageBox.Show(
                        T(
                            "Bu video ve part etiketi için daha önce oluşturulmuş dosyalar bulundu.\n\n" +
                            "İşlem başarılı olursa eski partların üzerine yazılsın mı?",

                            "Existing part files were found for this video and part label.\n\n" +
                            "Should they be replaced if the operation completes successfully?"),

                        T(
                            "Eski partlar bulundu",
                            "Existing parts found"),

                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                if (result !=
                    MessageBoxResult.Yes)
                {
                    return;
                }
            }

            string? tempFolder =
                null;

            bool splittingCompleted =
                false;

            bool telegramUploadStarted =
                false;

            List<string> finalOutputParts =
                new List<string>();

            _cancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken cancellationToken =
                _cancellationTokenSource.Token;

            Stopwatch operationStopwatch =
                Stopwatch.StartNew();

            try
            {
                SetControlsForOperation(
                    true);

                _lastOutputFolder =
                    null;

                ProgressBar.Value =
                    0;

                ProgressBar.IsIndeterminate =
                    true;

                _displayPercent =
                    0;

                _displayElapsed =
                    TimeSpan.Zero;

                _displayRemaining =
                    null;

                RefreshProgressTexts();

                SetStatus(
                    "Analyzing");

                double bitrate =
                    await GetVideoBitrateAsync(
                        ffprobePath,
                        inputFile,
                        cancellationToken);

                cancellationToken
                    .ThrowIfCancellationRequested();

                double videoDuration =
                    await GetVideoDurationAsync(
                        ffprobePath,
                        inputFile,
                        cancellationToken);

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (videoDuration <=
                    0)
                {
                    throw new Exception(
                        T(
                            "Videonun süresi okunamadı.",
                            "The video duration could not be read."));
                }

                ProgressBar.IsIndeterminate =
                    false;

                double segmentSeconds =
                    (targetBytes * 8.0) /
                    bitrate;

                segmentSeconds *=
                    0.95;

                if (segmentSeconds <
                    1)
                {
                    segmentSeconds =
                        1;
                }

                tempFolder =
                    Path.Combine(
                        outputFolder,
                        ".VideoSplitter_" +
                        Guid.NewGuid()
                        .ToString("N"));

                Directory.CreateDirectory(
                    tempFolder);

                bool success =
                    false;

                const int maxAttempts =
                    10;

                for (int attempt = 1;
                     attempt <= maxAttempts;
                     attempt++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    foreach (string oldTempFile
                             in Directory.GetFiles(
                                 tempFolder))
                    {
                        File.Delete(
                            oldTempFile);
                    }

                    ProgressBar.Value =
                        0;

                    _displayPercent =
                        0;

                    _displayElapsed =
                        operationStopwatch.Elapsed;

                    _displayRemaining =
                        null;

                    RefreshProgressTexts();

                    SetStatus(
                        "Splitting",
                        attempt,
                        maxAttempts);

                    string outputPattern =
                        Path.Combine(
                            tempFolder,
                            $"{baseName}-{partSuffix}%d{extension}");

                    await RunSegmentPassAsync(
                        ffmpegPath,
                        inputFile,
                        outputPattern,
                        segmentSeconds,
                        videoDuration,
                        operationStopwatch,
                        cancellationToken);

                    cancellationToken
                        .ThrowIfCancellationRequested();

                    string[] generatedParts =
                        Directory.GetFiles(
                            tempFolder,
                            partSearchPattern);

                    if (generatedParts.Length ==
                        0)
                    {
                        throw new Exception(
                            T(
                                "FFmpeg hiçbir part dosyası oluşturmadı.",
                                "FFmpeg did not create any part files."));
                    }

                    long largestPart =
                        generatedParts
                        .Max(
                            file =>
                                new FileInfo(file)
                                .Length);

                    double largestMB =
                        largestPart /
                        1024.0 /
                        1024.0;

                    SetStatus(
                        "Checking",
                        largestMB);

                    if (largestPart <=
                        targetBytes)
                    {
                        success =
                            true;

                        break;
                    }

                    double ratio =
                        (double)targetBytes /
                        largestPart;

                    double newSegmentSeconds =
                        segmentSeconds *
                        ratio *
                        0.97;

                    if (newSegmentSeconds >=
                        segmentSeconds)
                    {
                        newSegmentSeconds =
                            segmentSeconds *
                            0.90;
                    }

                    segmentSeconds =
                        Math.Max(
                            1.0,
                            newSegmentSeconds);
                }

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (!success)
                {
                    throw new Exception(
                        T(
                            "Bu videoda kalite kaybı olmadan belirlenen maksimum dosya boyutu garanti edilemedi.\n\n" +
                            "Videonun keyframe aralıkları çok uzun olabilir. Daha büyük bir maksimum boyut seçmeyi deneyin.",

                            "The requested maximum file size could not be guaranteed without re-encoding this video.\n\n" +
                            "The video's keyframe intervals may be too long. Try selecting a larger maximum size."));
                }

                SetStatus(
                    "Preparing");

                cancellationToken
                    .ThrowIfCancellationRequested();

                foreach (string oldPart
                         in existingParts)
                {
                    if (File.Exists(
                            oldPart))
                    {
                        File.Delete(
                            oldPart);
                    }
                }

                string[] finalTempParts =
                    Directory.GetFiles(
                        tempFolder,
                        partSearchPattern);

                foreach (string tempPart
                         in finalTempParts)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    string destinationPath =
                        Path.Combine(
                            outputFolder,
                            Path.GetFileName(
                                tempPart));

                    File.Move(
                        tempPart,
                        destinationPath,
                        true);

                    finalOutputParts.Add(
                        destinationPath);
                }

                finalOutputParts =
                    finalOutputParts
                    .OrderBy(
                        file =>
                            GetPartNumber(
                                file,
                                baseName,
                                partSuffix,
                                extension))
                    .ToList();

                splittingCompleted =
                    true;

                operationStopwatch.Stop();

                _lastOutputFolder =
                    outputFolder;

                OpenOutputButton.IsEnabled =
                    true;

                // =================================================
                // TELEGRAM YÜKLEME
                // =================================================

                if (telegramRequested)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    telegramUploadStarted =
                        true;

                    ProgressBar.IsIndeterminate =
                        false;

                    ProgressBar.Value =
                        0;

                    _displayPercent =
                        0;

                    _displayElapsed =
                        TimeSpan.Zero;

                    _displayRemaining =
                        null;

                    RefreshProgressTexts();

                    SetStatus(
                        "TelegramPreparing");

                    TelegramDestination destination =
                        _selectedTelegramDestination!;

                    int parallelTransfers =
                        GetSelectedParallelTransfers();

                    string albumCaption =
                        TelegramAlbumNameTextBox.Text?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(
                            albumCaption))
                    {
                        albumCaption =
                            Path.GetFileNameWithoutExtension(
                                inputFile);
                    }

                    if (albumCaption.Length > 1024)
                    {
                        albumCaption =
                            albumCaption.Substring(0, 1024);
                    }

                    Progress<TelegramUploadProgress>
                        uploadProgress =
                            new Progress<TelegramUploadProgress>(
                                UpdateTelegramProgress);

                    try
                    {
                        await TelegramService.UploadFilesAsync(
                            destination,
                            finalOutputParts,
                            parallelTransfers,
                            uploadProgress,
                            cancellationToken,
                            albumCaption);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ProgressBar.IsIndeterminate =
                            false;

                        SetStatus(
                            "TelegramFailed");

                        MessageBox.Show(
                            T(
                                "Video başarıyla bölündü ancak Telegram yüklemesi başarısız oldu.\n\n" +
                                "Oluşturulan video parçaları bilgisayarınızda duruyor.\n\n",

                                "The video was split successfully, but the Telegram upload failed.\n\n" +
                                "The generated parts are still saved on your computer.\n\n") +
                            ex.Message,

                            T(
                                "Telegram Yükleme Hatası",
                                "Telegram Upload Error"),

                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    ProgressBar.Value =
                        100;

                    _displayPercent =
                        100;

                    _displayRemaining =
                        TimeSpan.Zero;

                    RefreshProgressTexts();

                    SetStatus(
                        "TelegramSuccess");

                    int uploadedCount =
                        finalOutputParts.Count;

                    string deleteInfo = "";

                    bool deleteRequested =
                        TelegramDeletePartsCheckBox.IsChecked ==
                        true;

                    if (deleteRequested &&
                        uploadedCount > 0)
                    {
                        int deleted = 0;

                        foreach (string partPath
                                 in finalOutputParts)
                        {
                            try
                            {
                                if (File.Exists(
                                        partPath))
                                {
                                    File.Delete(
                                        partPath);
                                }

                                deleted++;
                            }
                            catch
                            {
                            }
                        }

                        if (deleted >= uploadedCount)
                        {
                            deleteInfo =
                                T(
                                    "\nYerel partlar silindi.",
                                    "\nLocal parts deleted.");
                        }
                        else
                        {
                            deleteInfo =
                                T(
                                    $"\nYerel partlar silindi: {deleted}/{uploadedCount}.",
                                    $"\nLocal parts deleted: {deleted}/{uploadedCount}.");
                        }
                    }

                    MessageBox.Show(
                        T(
                            $"Video başarıyla bölündü ve Telegram'a yüklendi.\n\n" +
                            $"Yüklenen part sayısı: {uploadedCount}\n" +
                            $"Hedef: {GetTelegramDestinationText(destination)}" +
                            $"{deleteInfo}",

                            $"Video split and uploaded to Telegram successfully.\n\n" +
                            $"Parts uploaded: {uploadedCount}\n" +
                            $"Destination: {GetTelegramDestinationText(destination)}" +
                            $"{deleteInfo}"),

                        T(
                            "İşlem Tamamlandı",
                            "Operation Completed"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    ProgressBar.IsIndeterminate =
                        false;

                    ProgressBar.Value =
                        100;

                    _displayPercent =
                        100;

                    _displayElapsed =
                        operationStopwatch.Elapsed;

                    _displayRemaining =
                        TimeSpan.Zero;

                    RefreshProgressTexts();

                    SetStatus(
                        "SplitSuccess");

                    double maxAllowedMB =
                        targetBytes /
                        1024.0 /
                        1024.0;

                    MessageBox.Show(
                        T(
                            $"Video başarıyla bölündü.\n\n" +
                            $"Dosya adı biçimi:\n" +
                            $"{baseName}-{partSuffix}1{extension}\n\n" +
                            $"Oluşturulan hiçbir part {maxAllowedMB:0.##} MB sınırını geçmiyor.",

                            $"Video split successfully.\n\n" +
                            $"File name format:\n" +
                            $"{baseName}-{partSuffix}1{extension}\n\n" +
                            $"No generated part exceeds the {maxAllowedMB:0.##} MB limit."),

                        T(
                            "İşlem tamamlandı",
                            "Operation completed"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            // =====================================================
            // CANCEL
            // =====================================================

            catch (OperationCanceledException)
            {
                operationStopwatch.Stop();

                ProgressBar.IsIndeterminate =
                    false;

                _displayRemaining =
                    null;

                RefreshProgressTexts();

                SetStatus(
                    "Cancelled");

                if (splittingCompleted &&
                    telegramUploadStarted)
                {
                    MessageBox.Show(
                        T(
                            "Telegram yüklemesi iptal edildi.\n\nVideo parçaları bilgisayarınızda duruyor.",
                            "Telegram upload was cancelled.\n\nThe video parts remain saved on your computer."),

                        T(
                            "Yükleme İptal Edildi",
                            "Upload Cancelled"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        T(
                            "Video bölme işlemi iptal edildi.",
                            "The video splitting operation was cancelled."),

                        T(
                            "İşlem iptal edildi",
                            "Operation cancelled"),

                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            // =====================================================
            // ERROR
            // =====================================================

            catch (Exception ex)
            {
                operationStopwatch.Stop();

                ProgressBar.IsIndeterminate =
                    false;

                SetStatus(
                    "Error");

                MessageBox.Show(
                    ex.Message,

                    T(
                        "Hata",
                        "Error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            // =====================================================
            // CLEANUP
            // =====================================================

            finally
            {
                operationStopwatch.Stop();

                _currentProcess =
                    null;

                if (!string.IsNullOrWhiteSpace(
                        tempFolder) &&
                    Directory.Exists(
                        tempFolder))
                {
                    try
                    {
                        Directory.Delete(
                            tempFolder,
                            true);
                    }
                    catch
                    {
                    }
                }

                SetControlsForOperation(
                    false);

                _cancellationTokenSource?
                    .Dispose();

                _cancellationTokenSource =
                    null;
            }
        }

        // =========================================================
        // CONTROLS
        // =========================================================

        private void SetControlsForOperation(
            bool working)
        {
            SplitButton.IsEnabled =
                !working;

            SelectVideoButton.IsEnabled =
                !working;

            SelectOutputButton.IsEnabled =
                !working;

            PartSizeTextBox.IsEnabled =
                !working;

            SizeUnitComboBox.IsEnabled =
                !working;

            PartSuffixTextBox.IsEnabled =
                !working;

            TelegramAlbumNameTextBox.IsEnabled =
                !working;

            TelegramUploadCheckBox.IsEnabled =
                !working;

            TelegramOptionsPanel.IsEnabled =
                !working &&
                TelegramUploadCheckBox.IsChecked ==
                true;

            CancelButton.IsEnabled =
                working;

            if (working)
            {
                OpenOutputButton.IsEnabled =
                    false;
            }
        }

        // =========================================================
        // CANCEL
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_cancellationTokenSource ==
                    null ||
                _cancellationTokenSource
                    .IsCancellationRequested)
            {
                return;
            }

            SetStatus(
                "Canceling");

            CancelButton.IsEnabled =
                false;

            _cancellationTokenSource
                .Cancel();

            if (_currentProcess !=
                null)
            {
                TryKillProcess(
                    _currentProcess);
            }
        }

        // =========================================================
        // OUTPUT
        // =========================================================

        private void OpenOutputButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string folder =
                _lastOutputFolder ??
                OutputPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(
                    folder) ||
                !Directory.Exists(
                    folder))
            {
                MessageBox.Show(
                    T(
                        "Çıktı klasörü bulunamadı.",
                        "The output folder could not be found."),

                    T(
                        "Klasör bulunamadı",
                        "Folder not found"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            folder,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    T(
                        "Çıktı klasörü açılamadı.\n\n",
                        "The output folder could not be opened.\n\n") +
                    ex.Message,

                    T(
                        "Hata",
                        "Error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // BITRATE
        // =========================================================

        private async Task<double>
            GetVideoBitrateAsync(
                string ffprobePath,
                string inputFile,
                CancellationToken cancellationToken)
        {
            string output =
                await RunFFprobeAsync(
                    ffprobePath,
                    inputFile,
                    "format=bit_rate",
                    cancellationToken);

            if (double.TryParse(
                    output,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double bitrate) &&
                bitrate > 0)
            {
                return bitrate;
            }

            double duration =
                await GetVideoDurationAsync(
                    ffprobePath,
                    inputFile,
                    cancellationToken);

            if (duration <= 0)
            {
                throw new Exception(
                    T(
                        "Videonun bitrate değeri okunamadı.",
                        "The video's bitrate could not be read."));
            }

            long fileSize =
                new FileInfo(
                    inputFile)
                .Length;

            return
                (fileSize * 8.0) /
                duration;
        }

        // =========================================================
        // DURATION
        // =========================================================

        private async Task<double>
            GetVideoDurationAsync(
                string ffprobePath,
                string inputFile,
                CancellationToken cancellationToken)
        {
            string output =
                await RunFFprobeAsync(
                    ffprobePath,
                    inputFile,
                    "format=duration",
                    cancellationToken);

            if (double.TryParse(
                    output,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double duration))
            {
                return duration;
            }

            return 0;
        }

        // =========================================================
        // FFPROBE
        // =========================================================

        private async Task<string>
            RunFFprobeAsync(
                string ffprobePath,
                string inputFile,
                string showEntry,
                CancellationToken cancellationToken)
        {
            ProcessStartInfo info =
                new ProcessStartInfo
                {
                    FileName =
                        ffprobePath,

                    UseShellExecute =
                        false,

                    RedirectStandardOutput =
                        true,

                    RedirectStandardError =
                        true,

                    CreateNoWindow =
                        true
                };

            info.ArgumentList.Add("-v");
            info.ArgumentList.Add("error");

            info.ArgumentList.Add(
                "-show_entries");

            info.ArgumentList.Add(
                showEntry);

            info.ArgumentList.Add("-of");

            info.ArgumentList.Add(
                "default=nw=1:nk=1");

            info.ArgumentList.Add(
                inputFile);

            using Process process =
                new Process
                {
                    StartInfo =
                        info
                };

            _currentProcess =
                process;

            try
            {
                process.Start();

                using CancellationTokenRegistration registration =
                    cancellationToken.Register(
                        () =>
                            TryKillProcess(
                                process));

                Task<string> outputTask =
                    process
                    .StandardOutput
                    .ReadToEndAsync();

                Task<string> errorTask =
                    process
                    .StandardError
                    .ReadToEndAsync();

                await process
                    .WaitForExitAsync();

                string output =
                    await outputTask;

                string error =
                    await errorTask;

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (process.ExitCode !=
                    0)
                {
                    throw new Exception(
                        "FFprobe:\n\n" +
                        error);
                }

                return
                    output.Trim();
            }
            finally
            {
                if (ReferenceEquals(
                        _currentProcess,
                        process))
                {
                    _currentProcess =
                        null;
                }
            }
        }

        // =========================================================
        // FFMPEG
        // =========================================================

        private async Task
            RunSegmentPassAsync(
                string ffmpegPath,
                string inputFile,
                string outputPattern,
                double segmentSeconds,
                double totalVideoDuration,
                Stopwatch operationStopwatch,
                CancellationToken cancellationToken)
        {
            ProcessStartInfo ffmpegInfo =
                new ProcessStartInfo
                {
                    FileName =
                        ffmpegPath,

                    UseShellExecute =
                        false,

                    RedirectStandardOutput =
                        true,

                    RedirectStandardError =
                        true,

                    CreateNoWindow =
                        true
                };

            ffmpegInfo.ArgumentList.Add("-y");

            ffmpegInfo.ArgumentList.Add("-i");
            ffmpegInfo.ArgumentList.Add(inputFile);

            ffmpegInfo.ArgumentList.Add("-map");
            ffmpegInfo.ArgumentList.Add("0");

            ffmpegInfo.ArgumentList.Add("-c");
            ffmpegInfo.ArgumentList.Add("copy");

            ffmpegInfo.ArgumentList.Add("-f");
            ffmpegInfo.ArgumentList.Add("segment");

            ffmpegInfo.ArgumentList.Add(
                "-segment_time");

            ffmpegInfo.ArgumentList.Add(
                segmentSeconds.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture));

            ffmpegInfo.ArgumentList.Add(
                "-segment_start_number");

            ffmpegInfo.ArgumentList.Add("1");

            ffmpegInfo.ArgumentList.Add(
                "-reset_timestamps");

            ffmpegInfo.ArgumentList.Add("1");

            ffmpegInfo.ArgumentList.Add(
                "-avoid_negative_ts");

            ffmpegInfo.ArgumentList.Add(
                "make_zero");

            // =====================================================
            // MP4 / M4V / MOV FASTSTART
            //
            // Telegram ve web oynatıcılarının videoyu dosyanın
            // tamamını indirmeden oynatabilmesi için MP4 indeksini
            // (moov atom) dosyanın başına taşıyoruz.
            //
            // Yeniden encode YOKTUR.
            // Kalite kaybı YOKTUR.
            // =====================================================

            string outputExtension =
                Path.GetExtension(
                    outputPattern)
                .ToLowerInvariant();

            if (outputExtension == ".mp4" ||
                outputExtension == ".m4v" ||
                outputExtension == ".mov")
            {
                ffmpegInfo.ArgumentList.Add(
                    "-segment_format_options");

                ffmpegInfo.ArgumentList.Add(
                    "movflags=+faststart");
            }

            ffmpegInfo.ArgumentList.Add(
                "-progress");

            ffmpegInfo.ArgumentList.Add(
                "pipe:1");

            ffmpegInfo.ArgumentList.Add(
                "-nostats");

            ffmpegInfo.ArgumentList.Add(
                outputPattern);

            using Process ffmpegProcess =
                new Process
                {
                    StartInfo =
                        ffmpegInfo
                };

            _currentProcess =
                ffmpegProcess;

            Stopwatch passStopwatch =
                Stopwatch.StartNew();

            try
            {
                ffmpegProcess.Start();

                using CancellationTokenRegistration registration =
                    cancellationToken.Register(
                        () =>
                            TryKillProcess(
                                ffmpegProcess));

                Task<string> errorTask =
                    ffmpegProcess
                    .StandardError
                    .ReadToEndAsync();

                while (true)
                {
                    string? line =
                        await ffmpegProcess
                        .StandardOutput
                        .ReadLineAsync();

                    if (line == null)
                    {
                        break;
                    }

                    cancellationToken
                        .ThrowIfCancellationRequested();

                    if (line.StartsWith(
                            "out_time=",
                            StringComparison
                            .OrdinalIgnoreCase))
                    {
                        string timeText =
                            line.Substring(
                                "out_time=".Length);

                        if (TimeSpan.TryParse(
                                timeText,
                                CultureInfo.InvariantCulture,
                                out TimeSpan processedTime))
                        {
                            UpdateProgress(
                                processedTime
                                .TotalSeconds,

                                totalVideoDuration,

                                passStopwatch,

                                operationStopwatch);
                        }
                    }

                    if (line.Equals(
                            "progress=end",
                            StringComparison
                            .OrdinalIgnoreCase))
                    {
                        UpdateProgress(
                            totalVideoDuration,
                            totalVideoDuration,
                            passStopwatch,
                            operationStopwatch);
                    }
                }

                await ffmpegProcess
                    .WaitForExitAsync();

                string error =
                    await errorTask;

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (ffmpegProcess.ExitCode !=
                    0)
                {
                    throw new Exception(
                        "FFmpeg:\n\n" +
                        error);
                }
            }
            finally
            {
                passStopwatch.Stop();

                if (ReferenceEquals(
                        _currentProcess,
                        ffmpegProcess))
                {
                    _currentProcess =
                        null;
                }
            }
        }

        // =========================================================
        // SPLIT PROGRESS
        // =========================================================

        private void UpdateProgress(
            double processedSeconds,
            double totalSeconds,
            Stopwatch passStopwatch,
            Stopwatch operationStopwatch)
        {
            if (totalSeconds <=
                0)
            {
                return;
            }

            double percent =
                (processedSeconds /
                 totalSeconds) *
                100.0;

            percent =
                Math.Clamp(
                    percent,
                    0,
                    100);

            ProgressBar.IsIndeterminate =
                false;

            ProgressBar.Value =
                percent;

            _displayPercent =
                percent;

            _displayElapsed =
                operationStopwatch.Elapsed;

            if (percent >= 1.0 &&
                percent < 100.0)
            {
                double fraction =
                    percent /
                    100.0;

                double estimatedTotalSeconds =
                    passStopwatch
                    .Elapsed
                    .TotalSeconds /
                    fraction;

                double remainingSeconds =
                    estimatedTotalSeconds -
                    passStopwatch
                    .Elapsed
                    .TotalSeconds;

                if (remainingSeconds <
                    0)
                {
                    remainingSeconds =
                        0;
                }

                _displayRemaining =
                    TimeSpan.FromSeconds(
                        remainingSeconds);
            }
            else if (percent >=
                     100)
            {
                _displayRemaining =
                    TimeSpan.Zero;
            }
            else
            {
                _displayRemaining =
                    null;
            }

            RefreshProgressTexts();
        }

        // =========================================================
        // PART SIRASI
        // =========================================================

        private static int GetPartNumber(
            string filePath,
            string baseName,
            string partSuffix,
            string extension)
        {
            string fileName =
                Path.GetFileName(
                    filePath);

            string prefix =
                $"{baseName}-{partSuffix}";

            if (!fileName.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return int.MaxValue;
            }

            int numberLength =
                fileName.Length -
                prefix.Length -
                extension.Length;

            if (numberLength <=
                0)
            {
                return int.MaxValue;
            }

            string numberText =
                fileName.Substring(
                    prefix.Length,
                    numberLength);

            return int.TryParse(
                    numberText,
                    out int number)
                ? number
                : int.MaxValue;
        }

        // =========================================================
        // PROCESS
        // =========================================================

        private static void TryKillProcess(
            Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(
                        entireProcessTree:
                        true);
                }
            }
            catch
            {
            }
        }

        // =========================================================
        // TIME FORMAT
        // =========================================================

        private static string FormatTime(
            TimeSpan time)
        {
            if (time.TotalHours >=
                1)
            {
                return
                    $"{(int)time.TotalHours:00}:" +
                    $"{time.Minutes:00}:" +
                    $"{time.Seconds:00}";
            }

            return
                $"{(int)time.TotalMinutes:00}:" +
                $"{time.Seconds:00}";
        }
    }
}