using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WTelegram;

namespace VideoSplitter
{
    public partial class TelegramSettingsWindow : Window
    {
        private enum AppLanguage
        {
            Turkish,
            English
        }

        private AppLanguage _language = AppLanguage.Turkish;

        private Client? _client;

        private string? _currentLoginStep;

        private int _loadedApiId;
        private string _loadedApiHash = "";
        private string _loadedPhone = "";

        public bool ConnectionEstablished { get; private set; }

        public string ConnectedAccountText { get; private set; } = "";

        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "VideoSplitter");

        private static readonly string SettingsPath =
            Path.Combine(
                SettingsDirectory,
                "telegram-settings.dat");

        private static readonly string SessionPath =
            Path.Combine(
                SettingsDirectory,
                "Telegram.session");

        public TelegramSettingsWindow()
        {
            InitializeComponent();

            // WTelegram'ın ayrıntılı konsol loglarını kapat.
            // null kullanmıyoruz.
            WTelegram.Helpers.Log = (_, _) => { };

            RefreshLanguage();
        }

        // =========================================================
        // WINDOW
        // =========================================================

        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadSavedSettings();

            RefreshLanguage();
        }

        private void Window_Closed(
            object? sender,
            EventArgs e)
        {
            try
            {
                _client?.Dispose();
            }
            catch
            {
            }

            _client = null;
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
            Title =
                T(
                    "Telegram Ayarları",
                    "Telegram Settings");

            TitleTextBlock.Text =
                T(
                    "Telegram Ayarları",
                    "Telegram Settings");

            SubtitleTextBlock.Text =
                T(
                    "Telegram hesabınızı Video Splitter'a bağlayın.",
                    "Connect your Telegram account to Video Splitter.");

            ApiIdLabel.Text =
                "API ID";

            ApiHashLabel.Text =
                "API Hash";

            PhoneLabel.Text =
                T(
                    "Telefon Numarası",
                    "Phone Number");

            PhoneHintTextBlock.Text =
                T(
                    "Uluslararası format kullanın. Örn: +905xxxxxxxxx",
                    "Use international format. Example: +12025550123");

            ConnectButton.Content =
                T(
                    "TELEGRAM'A BAĞLAN",
                    "CONNECT TO TELEGRAM");

            StatusTitleTextBlock.Text =
                T(
                    "Durum:",
                    "Status:");

            ContinueLoginButton.Content =
                T(
                    "DEVAM ET",
                    "CONTINUE");

            CloseButton.Content =
                T(
                    "KAPAT",
                    "CLOSE");

            SecurityInfoTextBlock.Text =
                T(
                    "API bilgileriniz bu Windows kullanıcısına bağlı olarak şifrelenir ve yalnızca bu bilgisayarda saklanır.",
                    "Your API credentials are encrypted for the current Windows user and stored only on this computer.");

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

            if (ConnectionEstablished)
            {
                StatusTextBlock.Text =
                    T(
                        $"Bağlandı: {ConnectedAccountText}",
                        $"Connected: {ConnectedAccountText}");
            }
            else if (File.Exists(SessionPath))
            {
                StatusTextBlock.Text =
                    T(
                        "Kayıtlı Telegram oturumu bulundu.",
                        "A saved Telegram session was found.");
            }
            else
            {
                StatusTextBlock.Text =
                    T(
                        "Bağlı değil",
                        "Not connected");
            }

            RefreshLoginStepLanguage();
        }

        // =========================================================
        // BAĞLAN
        // =========================================================

        private async void ConnectButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!int.TryParse(
                    ApiIdTextBox.Text.Trim(),
                    out int apiId) ||
                apiId <= 0)
            {
                MessageBox.Show(
                    T(
                        "Geçerli bir API ID girin.",
                        "Enter a valid API ID."),

                    T(
                        "Geçersiz API ID",
                        "Invalid API ID"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string apiHash =
                ApiHashPasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(
                    apiHash))
            {
                MessageBox.Show(
                    T(
                        "API Hash boş bırakılamaz.",
                        "API Hash cannot be empty."),

                    T(
                        "API Hash gerekli",
                        "API Hash required"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string phone =
                PhoneTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(
                    phone))
            {
                MessageBox.Show(
                    T(
                        "Telefon numaranızı uluslararası formatta girin.",
                        "Enter your phone number in international format."),

                    T(
                        "Telefon numarası gerekli",
                        "Phone number required"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                SetBusy(true);

                StatusTextBlock.Text =
                    T(
                        "Telegram'a bağlanılıyor...",
                        "Connecting to Telegram...");

                Directory.CreateDirectory(
                    SettingsDirectory);

                // API bilgileri değiştirilmişse eski session
                // yeni API Hash ile açılamaz.
                if (File.Exists(SessionPath) &&
                    !string.IsNullOrWhiteSpace(
                        _loadedApiHash) &&
                    (_loadedApiId != apiId ||
                     !string.Equals(
                         _loadedApiHash,
                         apiHash,
                         StringComparison.Ordinal)))
                {
                    try
                    {
                        File.Delete(
                            SessionPath);
                    }
                    catch
                    {
                    }
                }

                SaveSettings(
                    apiId,
                    apiHash,
                    phone);

                try
                {
                    _client?.Dispose();
                }
                catch
                {
                }

                _client =
                    new Client(
                        apiId,
                        apiHash,
                        SessionPath);

                _client.ParallelTransfers = 4;

                await AdvanceLoginAsync(
                    phone);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text =
                    T(
                        "Bağlantı başarısız.",
                        "Connection failed.");

                MessageBox.Show(
                    ex.Message,

                    T(
                        "Telegram Hatası",
                        "Telegram Error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // =========================================================
        // DOĞRULAMA / 2FA DEVAM
        // =========================================================

        private async void ContinueLoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_client == null ||
                string.IsNullOrWhiteSpace(
                    _currentLoginStep))
            {
                return;
            }

            string value;

            if (_currentLoginStep ==
                "password")
            {
                value =
                    LoginValuePasswordBox
                    .Password;
            }
            else
            {
                value =
                    LoginValueTextBox
                    .Text
                    .Trim();
            }

            if (string.IsNullOrWhiteSpace(value) &&
                _currentLoginStep !=
                "verification_code" &&
                _currentLoginStep !=
                "email")
            {
                MessageBox.Show(
                    T(
                        "Lütfen gerekli bilgiyi girin.",
                        "Please enter the requested information."),

                    T(
                        "Bilgi gerekli",
                        "Information required"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                SetBusy(true);

                await AdvanceLoginAsync(
                    value);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text =
                    T(
                        "Giriş işlemi başarısız.",
                        "Login failed.");

                MessageBox.Show(
                    ex.Message,

                    T(
                        "Telegram Hatası",
                        "Telegram Error"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // =========================================================
        // LOGIN AKIŞI
        // =========================================================

        private async Task AdvanceLoginAsync(
            string? loginInfo)
        {
            if (_client == null)
            {
                return;
            }

            string? nextStep =
                await _client.Login(
                    loginInfo);

            if (_client.User != null)
            {
                LoginCompleted();

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    nextStep))
            {
                nextStep =
                    await _client.Login(
                        null);

                if (_client.User != null)
                {
                    LoginCompleted();

                    return;
                }
            }

            ShowLoginStep(
                nextStep);
        }

        private void ShowLoginStep(
            string? step)
        {
            _currentLoginStep =
                step;

            LoginValueTextBox.Text = "";
            LoginValuePasswordBox.Password = "";

            LoginValueTextBox.Visibility =
                Visibility.Visible;

            LoginValuePasswordBox.Visibility =
                Visibility.Collapsed;

            switch (step)
            {
                case "verification_code":

                    StatusTextBlock.Text =
                        T(
                            "Telegram doğrulama kodu bekleniyor.",
                            "Waiting for Telegram verification code.");

                    break;

                case "password":

                    LoginValueTextBox.Visibility =
                        Visibility.Collapsed;

                    LoginValuePasswordBox.Visibility =
                        Visibility.Visible;

                    StatusTextBlock.Text =
                        T(
                            "İki aşamalı doğrulama şifresi gerekli.",
                            "Two-step verification password required.");

                    break;

                case "email":

                    StatusTextBlock.Text =
                        T(
                            "Telegram e-posta adresi istiyor.",
                            "Telegram is requesting an email address.");

                    break;

                case "email_verification_code":

                    StatusTextBlock.Text =
                        T(
                            "E-posta doğrulama kodu gerekli.",
                            "Email verification code required.");

                    break;

                case "name":
                case "first_name":
                case "last_name":

                    StatusTextBlock.Text =
                        T(
                            "Telegram hesap adı bilgisi istiyor.",
                            "Telegram is requesting account name information.");

                    break;

                case "phone_number":

                    StatusTextBlock.Text =
                        T(
                            "Telefon numarası tekrar gerekli.",
                            "Phone number is required again.");

                    break;

                default:

                    StatusTextBlock.Text =
                        T(
                            $"Telegram ek bilgi istiyor: {step ?? "bilinmiyor"}",
                            $"Telegram requires additional information: {step ?? "unknown"}");

                    break;
            }

            LoginStepPanel.Visibility =
                Visibility.Visible;

            RefreshLoginStepLanguage();
        }

        private void RefreshLoginStepLanguage()
        {
            if (string.IsNullOrWhiteSpace(
                    _currentLoginStep))
            {
                return;
            }

            switch (_currentLoginStep)
            {
                case "verification_code":

                    LoginStepLabel.Text =
                        T(
                            "Telegram Doğrulama Kodu",
                            "Telegram Verification Code");

                    LoginStepHintTextBlock.Text =
                        T(
                            "Telegram uygulamanıza gönderilen kodu girin.",
                            "Enter the code sent to your Telegram app.");

                    break;

                case "password":

                    LoginStepLabel.Text =
                        T(
                            "2FA Şifresi",
                            "2FA Password");

                    LoginStepHintTextBlock.Text =
                        T(
                            "Telegram iki aşamalı doğrulama şifrenizi girin.",
                            "Enter your Telegram two-step verification password.");

                    break;

                case "email":

                    LoginStepLabel.Text =
                        T(
                            "E-posta Adresi",
                            "Email Address");

                    LoginStepHintTextBlock.Text =
                        T(
                            "Telegram giriş doğrulaması için e-posta isteyebilir.",
                            "Telegram may request an email address for login verification.");

                    break;

                case "email_verification_code":

                    LoginStepLabel.Text =
                        T(
                            "E-posta Doğrulama Kodu",
                            "Email Verification Code");

                    LoginStepHintTextBlock.Text =
                        T(
                            "E-posta adresinize gelen doğrulama kodunu girin.",
                            "Enter the verification code sent to your email.");

                    break;

                case "name":
                case "first_name":
                case "last_name":

                    LoginStepLabel.Text =
                        T(
                            "Hesap Adı",
                            "Account Name");

                    LoginStepHintTextBlock.Text =
                        T(
                            "Telegram'ın istediği hesap adı bilgisini girin.",
                            "Enter the account name information requested by Telegram.");

                    break;

                case "phone_number":

                    LoginStepLabel.Text =
                        T(
                            "Telefon Numarası",
                            "Phone Number");

                    LoginStepHintTextBlock.Text =
                        T(
                            "Telefon numaranızı uluslararası formatta girin.",
                            "Enter your phone number in international format.");

                    break;

                default:

                    LoginStepLabel.Text =
                        T(
                            "Telegram Bilgisi",
                            "Telegram Information");

                    LoginStepHintTextBlock.Text =
                        _currentLoginStep ?? "";

                    break;
            }
        }

        // =========================================================
        // LOGIN TAMAMLANDI
        // =========================================================

        private void LoginCompleted()
        {
            if (_client?.User == null)
            {
                return;
            }

            ConnectionEstablished = true;

            if (!string.IsNullOrWhiteSpace(
                    _client.User.username))
            {
                ConnectedAccountText =
                    "@" +
                    _client.User.username;
            }
            else
            {
                ConnectedAccountText =
                    _client.User.first_name;

                if (!string.IsNullOrWhiteSpace(
                        _client.User.last_name))
                {
                    ConnectedAccountText +=
                        " " +
                        _client.User.last_name;
                }
            }

            LoginStepPanel.Visibility =
                Visibility.Collapsed;

            _currentLoginStep =
                null;

            StatusTextBlock.Text =
                T(
                    $"Bağlandı: {ConnectedAccountText}",
                    $"Connected: {ConnectedAccountText}");

            MessageBox.Show(
                T(
                    $"Telegram hesabı başarıyla bağlandı.\n\n{ConnectedAccountText}",
                    $"Telegram account connected successfully.\n\n{ConnectedAccountText}"),

                T(
                    "Bağlantı Başarılı",
                    "Connection Successful"),

                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // AYARLARI KAYDET
        // =========================================================

        private void SaveSettings(
            int apiId,
            string apiHash,
            string phone)
        {
            Directory.CreateDirectory(
                SettingsDirectory);

            TelegramStoredSettings settings =
                new TelegramStoredSettings
                {
                    ApiId = apiId,
                    ApiHash = apiHash,
                    Phone = phone
                };

            string json =
                JsonSerializer.Serialize(
                    settings);

            byte[] rawData =
                Encoding.UTF8.GetBytes(
                    json);

            byte[] protectedData =
                ProtectedData.Protect(
                    rawData,
                    null,
                    DataProtectionScope.CurrentUser);

            File.WriteAllBytes(
                SettingsPath,
                protectedData);

            _loadedApiId =
                apiId;

            _loadedApiHash =
                apiHash;

            _loadedPhone =
                phone;
        }

        // =========================================================
        // AYARLARI YÜKLE
        // =========================================================

        private void LoadSavedSettings()
        {
            if (!File.Exists(
                    SettingsPath))
            {
                return;
            }

            try
            {
                byte[] protectedData =
                    File.ReadAllBytes(
                        SettingsPath);

                byte[] rawData =
                    ProtectedData.Unprotect(
                        protectedData,
                        null,
                        DataProtectionScope.CurrentUser);

                string json =
                    Encoding.UTF8.GetString(
                        rawData);

                TelegramStoredSettings? settings =
                    JsonSerializer.Deserialize
                        <TelegramStoredSettings>(
                            json);

                if (settings == null)
                {
                    return;
                }

                _loadedApiId =
                    settings.ApiId;

                _loadedApiHash =
                    settings.ApiHash ?? "";

                _loadedPhone =
                    settings.Phone ?? "";

                ApiIdTextBox.Text =
                    settings.ApiId > 0
                        ? settings.ApiId.ToString()
                        : "";

                ApiHashPasswordBox.Password =
                    settings.ApiHash ?? "";

                PhoneTextBox.Text =
                    settings.Phone ?? "";

                if (File.Exists(
                        SessionPath))
                {
                    StatusTextBlock.Text =
                        T(
                            "Kayıtlı Telegram oturumu bulundu. Bağlan'a basarak devam edebilirsiniz.",
                            "A saved Telegram session was found. Press Connect to continue.");
                }
            }
            catch
            {
                StatusTextBlock.Text =
                    T(
                        "Kayıtlı Telegram ayarları okunamadı.",
                        "Saved Telegram settings could not be read.");
            }
        }

        // =========================================================
        // BUSY
        // =========================================================

        private void SetBusy(
            bool busy)
        {
            ConnectButton.IsEnabled =
                !busy;

            ContinueLoginButton.IsEnabled =
                !busy;

            ApiIdTextBox.IsEnabled =
                !busy;

            ApiHashPasswordBox.IsEnabled =
                !busy;

            PhoneTextBox.IsEnabled =
                !busy;

            CloseButton.IsEnabled =
                !busy;

            Cursor =
                busy
                    ? System.Windows.Input.Cursors.Wait
                    : System.Windows.Input.Cursors.Arrow;
        }

        // =========================================================
        // KAPAT
        // =========================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        // =========================================================
        // MODEL
        // =========================================================

        private sealed class TelegramStoredSettings
        {
            public int ApiId { get; set; }

            public string? ApiHash { get; set; }

            public string? Phone { get; set; }
        }
    }
}