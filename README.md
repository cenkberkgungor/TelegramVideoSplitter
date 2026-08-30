<div align="center">

# VideoSplitter

**Split videos without re-encoding and optionally upload the parts to Telegram.**

[GitHub Profile](https://github.com/cenkberkgungor) ·
[Repository](https://github.com/cenkberkgungor/TelegramVideoSplitter) ·
[Issues](https://github.com/cenkberkgungor/TelegramVideoSplitter/issues)

**Current version: v1.0.0**

</div>

---

## 🇹🇷 Türkçe

### VideoSplitter nedir?

VideoSplitter, videoları **yeniden encode etmeden** parçalara ayırmak için geliştirilmiş bir Windows masaüstü uygulamasıdır.

FFmpeg'in stream-copy özelliğini kullanarak video ve ses akışlarını yeniden sıkıştırmadan böler. Böylece mümkün olduğu durumlarda görüntü ve ses kalitesi korunur.

İsteğe bağlı Telegram entegrasyonu sayesinde oluşturulan video parçaları doğrudan:

- Kayıtlı Mesajlar
- Gruplar
- Süper gruplar
- Yetkili olduğunuz kanallar

gibi hedeflere yüklenebilir.

### Özellikler

- 🎬 Yeniden encode etmeden video bölme
- ✨ Kalite kaybını önleyen `-c copy` yaklaşımı
- 📦 Kullanıcı tarafından belirlenen maksimum part boyutu
- 🏷️ Özel part etiketi desteği
- ⚡ MP4/M4V/MOV için Fast Start desteği
- 📤 İsteğe bağlı Telegram yükleme
- 🚀 Paralel Telegram transferi
- 🖼️ Telegram videoları için otomatik thumbnail oluşturma
- ▶️ Telegram üzerinde stream edilebilir video metadata desteği
- 📊 İlerleme yüzdesi, geçen süre ve kalan süre
- 📈 Telegram yükleme hızının görüntülenmesi
- ❌ İşlem iptal desteği
- 📂 Çıktı klasörünü doğrudan açma
- 🇹🇷 Türkçe arayüz
- 🇬🇧 İngilizce arayüz
- 🔐 Telegram API bilgilerini Windows kullanıcı hesabına bağlı şekilde korumalı saklama

### Nasıl çalışır?

VideoSplitter videoyu yeniden encode etmek yerine FFmpeg ile mevcut video ve ses akışlarını yeni dosyalara kopyalar.

Temel yaklaşım:

```text
Girdi videosu
     ↓
FFmpeg stream copy
     ↓
Boyut kontrollü segmentler
     ↓
MP4 Fast Start
     ↓
Part 1 / Part 2 / Part 3 ...
     ↓
İsteğe bağlı Telegram yükleme
```

> Not: Kesme noktaları videonun keyframe yapısına bağlıdır. Bu nedenle part süreleri her videoda tam olarak aynı olmayabilir.

### Dosya adlandırma

Örnek:

```text
Orijinal:
video.mp4

Part etiketi:
PRT

Çıktı:
video-PRT1.mp4
video-PRT2.mp4
video-PRT3.mp4
```

### Telegram entegrasyonu

Telegram yükleme özelliği tamamen isteğe bağlıdır.

Telegram etkinleştirildiğinde uygulama:

1. Video metadata bilgilerini FFprobe ile okur.
2. Video için otomatik thumbnail oluşturur.
3. Dosyayı Telegram'a yükler.
4. Süre, çözünürlük ve streaming bilgilerini Telegram'a gönderir.
5. Geçici thumbnail dosyasını işlem sonunda siler.

### Gereksinimler

Kaynak koddan derlemek için:

- Windows
- Visual Studio 2026 veya uyumlu yeni sürüm
- .NET 10 SDK
- WPF
- FFmpeg / FFprobe
- WTelegramClient

### Kaynak koddan derleme

1. Repository'yi klonlayın:

```bash
git clone https://github.com/cenkberkgungor/TelegramVideoSplitter.git
```

2. Solution'ı Visual Studio ile açın.
3. NuGet paketlerini restore edin.
4. `FFmpeg` klasöründe `ffmpeg.exe` ve `ffprobe.exe` bulunduğundan emin olun.
5. `Release` yapılandırmasını seçin.
6. Projeyi derleyin.

### Telegram kurulumu

Telegram yükleme özelliğini kullanmak için kendi Telegram API bilgileriniz gerekir.

API bilgilerinizi:

- Başka kişilerle paylaşmayın.
- GitHub repository'sine commit etmeyin.
- Ekran görüntülerinde açık şekilde göstermeyin.

VideoSplitter Telegram ayarlarını yerel Windows kullanıcı hesabı altında saklar.

### Gizlilik

VideoSplitter'ın video bölme işlemi yerel bilgisayarınızda yapılır.

Telegram yükleme özelliği kapalıysa videolar Telegram'a gönderilmez.

Telegram özelliği açıkken yalnızca seçtiğiniz çıktı parçaları seçtiğiniz Telegram hedefine yüklenir.

---

## 🇬🇧 English

### What is VideoSplitter?

VideoSplitter is a Windows desktop application designed to split videos into smaller parts **without re-encoding**.

It uses FFmpeg stream copy whenever possible, preserving the original encoded video and audio streams instead of compressing them again.

Optional Telegram integration can upload generated video parts directly to:

- Saved Messages
- Groups
- Supergroups
- Channels where the account has permission to post

### Features

- 🎬 Split videos without re-encoding
- ✨ Stream-copy based workflow using `-c copy`
- 📦 User-defined maximum part size
- 🏷️ Custom part labels
- ⚡ Fast Start support for MP4/M4V/MOV
- 📤 Optional Telegram upload
- 🚀 Parallel Telegram transfers
- 🖼️ Automatic Telegram video thumbnails
- ▶️ Streaming-friendly Telegram video metadata
- 📊 Progress, elapsed time and estimated remaining time
- 📈 Telegram upload speed display
- ❌ Cancellation support
- 📂 Open output folder directly
- 🇹🇷 Turkish interface
- 🇬🇧 English interface
- 🔐 Telegram API settings protected for the current Windows user

### Build from source

Requirements:

- Windows
- Visual Studio 2026 or a compatible newer version
- .NET 10 SDK
- WPF
- FFmpeg / FFprobe
- WTelegramClient

Clone:

```bash
git clone https://github.com/cenkberkgungor/TelegramVideoSplitter.git
```

Open the solution in Visual Studio, restore NuGet packages, ensure FFmpeg binaries are available, and build using the `Release` configuration.

---

## Version

Current release:

```text
v1.0.0
```

Semantic versioning will be used:

```text
1.0.0 → Initial stable release
1.1.0 → New features
1.1.1 → Bug fixes
2.0.0 → Major changes
```

## Third-party components

VideoSplitter uses:

- [FFmpeg](https://ffmpeg.org/)
- [WTelegramClient](https://github.com/wiz0u/WTelegramClient)

Please refer to the respective projects for their own licenses and terms.

## License

This project is licensed under the **MIT License**.

See [LICENSE](LICENSE) for details.

## Developer

**Cenk Berk Güngör**

GitHub: https://github.com/cenkberkgungor

## Contributing

Issues and suggestions are welcome:

https://github.com/cenkberkgungor/TelegramVideoSplitter/issues

## Disclaimer

VideoSplitter is an independent project and is not affiliated with or endorsed by Telegram or FFmpeg.
