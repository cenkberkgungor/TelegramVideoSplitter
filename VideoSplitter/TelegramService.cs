using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TL;
using WTelegram;

namespace VideoSplitter
{
    public enum TelegramDestinationKind
    {
        SavedMessages,
        Channel,
        Group
    }

    public sealed class TelegramDestination
    {
        public TelegramDestinationKind Kind { get; init; }

        public string Title { get; init; } = "";

        public string Username { get; init; } = "";

        public long Id { get; init; }

        public long AccessHash { get; init; }

        public InputPeer CreateInputPeer()
        {
            return Kind switch
            {
                TelegramDestinationKind.SavedMessages =>
                    InputPeer.Self,

                TelegramDestinationKind.Channel =>
                    new InputPeerChannel(
                        Id,
                        AccessHash),

                TelegramDestinationKind.Group
                    when AccessHash != 0 =>
                    new InputPeerChannel(
                        Id,
                        AccessHash),

                TelegramDestinationKind.Group =>
                    new InputPeerChat(
                        Id),

                _ =>
                    InputPeer.Self
            };
        }
    }

    public sealed class TelegramConnectionSnapshot
    {
        public bool IsConnected { get; init; }

        public string AccountText { get; init; } = "";

        public string ErrorMessage { get; init; } = "";

        public List<TelegramDestination> Destinations { get; init; } =
            new List<TelegramDestination>();
    }

    public sealed class TelegramUploadProgress
    {
        public int FileIndex { get; init; }

        public int FileCount { get; init; }

        public string FileName { get; init; } = "";

        public long CurrentFileUploadedBytes { get; init; }

        public long CurrentFileTotalBytes { get; init; }

        public long OverallUploadedBytes { get; init; }

        public long OverallTotalBytes { get; init; }

        public double OverallPercent { get; init; }

        public double InstantMBps { get; init; }

        public double AverageMBps { get; init; }

        public TimeSpan Elapsed { get; init; }

        public TimeSpan? Remaining { get; init; }
    }

    public static class TelegramService
    {
        public static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "VideoSplitter");

        public static readonly string SettingsPath =
            Path.Combine(
                SettingsDirectory,
                "telegram-settings.dat");

        public static readonly string SessionPath =
            Path.Combine(
                SettingsDirectory,
                "Telegram.session");

        public static bool HasSavedSession =>
            File.Exists(SettingsPath) &&
            File.Exists(SessionPath);

        // =========================================================
        // BAĞLANTI + HEDEFLER
        // =========================================================

        public static async Task<TelegramConnectionSnapshot>
            LoadConnectionAsync()
        {
            if (!HasSavedSession)
            {
                return new TelegramConnectionSnapshot
                {
                    IsConnected = false
                };
            }

            try
            {
                TelegramStoredSettings? settings =
                    LoadSettings();

                if (settings == null ||
                    settings.ApiId <= 0 ||
                    string.IsNullOrWhiteSpace(
                        settings.ApiHash))
                {
                    return new TelegramConnectionSnapshot
                    {
                        IsConnected = false,
                        ErrorMessage =
                            "Telegram ayarları okunamadı."
                    };
                }

                WTelegram.Helpers.Log =
                    (_, _) => { };

                using Client client =
                    CreateClient(
                        settings,
                        4);

                User user =
                    await client.LoginUserIfNeeded();

                if (user == null)
                {
                    return new TelegramConnectionSnapshot
                    {
                        IsConnected = false
                    };
                }

                string accountText =
                    GetAccountText(
                        user);

                List<TelegramDestination> destinations =
                    new List<TelegramDestination>
                    {
                        new TelegramDestination
                        {
                            Kind =
                                TelegramDestinationKind
                                .SavedMessages,

                            Title =
                                "Saved Messages"
                        }
                    };

                Messages_Chats chats =
                    await client.Messages_GetAllChats();

                foreach (ChatBase chatBase
                         in chats.chats.Values)
                {
                    if (!chatBase.IsActive)
                    {
                        continue;
                    }

                    if (chatBase is Channel channel)
                    {
                        // Broadcast channel
                        if (channel.IsChannel)
                        {
                            bool isCreator =
                                channel.flags.HasFlag(
                                    Channel.Flags.creator);

                            bool canPost =
                                channel.admin_rights != null &&
                                channel.admin_rights.flags.HasFlag(
                                    ChatAdminRights.Flags
                                    .post_messages);

                            if (!isCreator &&
                                !canPost)
                            {
                                continue;
                            }

                            destinations.Add(
                                new TelegramDestination
                                {
                                    Kind =
                                        TelegramDestinationKind
                                        .Channel,

                                    Title =
                                        channel.title,

                                    Username =
                                        channel.MainUsername ?? "",

                                    Id =
                                        channel.id,

                                    AccessHash =
                                        channel.access_hash
                                });

                            continue;
                        }

                        // Supergroup
                        if (channel.IsBanned(
                                ChatBannedRights.Flags
                                .send_messages))
                        {
                            continue;
                        }

                        destinations.Add(
                            new TelegramDestination
                            {
                                Kind =
                                    TelegramDestinationKind
                                    .Group,

                                Title =
                                    channel.title,

                                Username =
                                    channel.MainUsername ?? "",

                                Id =
                                    channel.id,

                                AccessHash =
                                    channel.access_hash
                            });

                        continue;
                    }

                    // Basic group
                    if (chatBase is Chat chat)
                    {
                        if (chat.IsBanned(
                                ChatBannedRights.Flags
                                .send_messages))
                        {
                            continue;
                        }

                        destinations.Add(
                            new TelegramDestination
                            {
                                Kind =
                                    TelegramDestinationKind
                                    .Group,

                                Title =
                                    chat.title,

                                Id =
                                    chat.id,

                                AccessHash =
                                    0
                            });
                    }
                }

                List<TelegramDestination> sorted =
                    destinations
                    .Take(1)
                    .Concat(
                        destinations
                        .Skip(1)
                        .OrderBy(
                            x => x.Title,
                            StringComparer
                            .CurrentCultureIgnoreCase))
                    .ToList();

                return new TelegramConnectionSnapshot
                {
                    IsConnected = true,

                    AccountText =
                        accountText,

                    Destinations =
                        sorted
                };
            }
            catch (Exception ex)
            {
                return new TelegramConnectionSnapshot
                {
                    IsConnected = false,

                    ErrorMessage =
                        ex.Message
                };
            }
        }

        // =========================================================
        // DOSYALARI TELEGRAM'A YÜKLE
        // =========================================================

        public static async Task UploadFilesAsync(
            TelegramDestination destination,
            IReadOnlyList<string> filePaths,
            int parallelTransfers,
            IProgress<TelegramUploadProgress>? progress,
            CancellationToken cancellationToken,
            string? albumCaption = null)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(
                    nameof(destination));
            }

            if (filePaths == null ||
                filePaths.Count == 0)
            {
                throw new ArgumentException(
                    "Yüklenecek dosya bulunamadı.",
                    nameof(filePaths));
            }

            foreach (string filePath
                     in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(
                        "Telegram'a yüklenecek dosya bulunamadı.",
                        filePath);
                }
            }

            TelegramStoredSettings? settings =
                LoadSettings();

            if (settings == null ||
                settings.ApiId <= 0 ||
                string.IsNullOrWhiteSpace(
                    settings.ApiHash))
            {
                throw new InvalidOperationException(
                    "Telegram ayarları okunamadı.");
            }

            string ffprobePath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "FFmpeg",
                    "ffprobe.exe");

            string ffmpegPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "FFmpeg",
                    "ffmpeg.exe");

            if (!File.Exists(ffprobePath))
            {
                throw new FileNotFoundException(
                    "FFprobe bulunamadı.",
                    ffprobePath);
            }

            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException(
                    "FFmpeg bulunamadı.",
                    ffmpegPath);
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            string normalizedCaption =
                (albumCaption ?? "").Trim();

            if (normalizedCaption.Length > 1024)
            {
                normalizedCaption =
                    normalizedCaption.Substring(0, 1024);
            }

            WTelegram.Helpers.Log =
                (_, _) => { };

            parallelTransfers =
                Math.Clamp(
                    parallelTransfers,
                    1,
                    16);

            using Client client =
                CreateClient(
                    settings,
                    parallelTransfers);

            User user =
                await client.LoginUserIfNeeded();

            if (user == null)
            {
                throw new InvalidOperationException(
                    "Telegram oturumu açılamadı.");
            }

            InputPeer peer =
                destination.CreateInputPeer();

            long overallTotalBytes =
                filePaths.Sum(
                    file =>
                        new FileInfo(file).Length);

            long completedBytes =
                0;

            long lastOverallBytes =
                0;

            double lastSpeedCheckSeconds =
                0;

            object progressLock =
                new object();

            Stopwatch uploadStopwatch =
                Stopwatch.StartNew();

            // Telegram grouped media (album) en fazla 10 öğe destekler.
            // MP4/M4V/MOV video partlarını burada biriktirip tek bir
            // medya grubu halinde gönderiyoruz.
            const int telegramAlbumMaxItems =
                10;

            List<InputMedia> pendingAlbumMedia =
                new List<InputMedia>();

            for (int fileIndex = 0;
                 fileIndex < filePaths.Count;
                 fileIndex++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                string filePath =
                    filePaths[fileIndex];

                FileInfo fileInfo =
                    new FileInfo(filePath);

                long currentFileSize =
                    fileInfo.Length;

                int currentFileNumber =
                    fileIndex + 1;

                // =================================================
                // VIDEO METADATA
                // =================================================

                VideoMetadata? videoMetadata =
                    await GetVideoMetadataAsync(
                        ffprobePath,
                        filePath,
                        cancellationToken);

                string? thumbnailPath =
                    null;

                try
                {
                    if (videoMetadata != null &&
                        videoMetadata.Width > 0 &&
                        videoMetadata.Height > 0 &&
                        videoMetadata.DurationSeconds > 0)
                    {
                        thumbnailPath =
                            Path.Combine(
                                Path.GetTempPath(),
                                "VideoSplitter_Thumbnail_" +
                                Guid.NewGuid().ToString("N") +
                                ".jpg");

                        bool thumbnailCreated =
                            await CreateVideoThumbnailAsync(
                                ffmpegPath,
                                filePath,
                                thumbnailPath,
                                videoMetadata.DurationSeconds,
                                cancellationToken);

                        if (!thumbnailCreated)
                        {
                            thumbnailPath =
                                null;
                        }
                    }

                    // =================================================
                    // UPLOAD
                    // =================================================

                InputFileBase uploadedFile =
                    await client.UploadFileAsync(
                        filePath,
                        (uploadedBytes, totalBytes) =>
                        {
                            cancellationToken
                                .ThrowIfCancellationRequested();

                            lock (progressLock)
                            {
                                long overallUploadedBytes =
                                    completedBytes +
                                    uploadedBytes;

                                double nowSeconds =
                                    uploadStopwatch
                                    .Elapsed
                                    .TotalSeconds;

                                bool isFinalCallback =
                                    uploadedBytes >=
                                    totalBytes;

                                if (!isFinalCallback &&
                                    nowSeconds -
                                    lastSpeedCheckSeconds <
                                    0.25)
                                {
                                    return;
                                }

                                double instantMBps =
                                    0;

                                double deltaSeconds =
                                    nowSeconds -
                                    lastSpeedCheckSeconds;

                                long deltaBytes =
                                    overallUploadedBytes -
                                    lastOverallBytes;

                                if (deltaSeconds > 0 &&
                                    deltaBytes >= 0)
                                {
                                    instantMBps =
                                        deltaBytes /
                                        deltaSeconds /
                                        1024.0 /
                                        1024.0;
                                }

                                double averageMBps =
                                    0;

                                if (nowSeconds > 0)
                                {
                                    averageMBps =
                                        overallUploadedBytes /
                                        nowSeconds /
                                        1024.0 /
                                        1024.0;
                                }

                                double overallPercent =
                                    overallTotalBytes > 0
                                        ? overallUploadedBytes *
                                          100.0 /
                                          overallTotalBytes
                                        : 0;

                                TimeSpan? remaining =
                                    null;

                                if (averageMBps > 0)
                                {
                                    double remainingMB =
                                        (overallTotalBytes -
                                         overallUploadedBytes) /
                                        1024.0 /
                                        1024.0;

                                    remaining =
                                        TimeSpan.FromSeconds(
                                            Math.Max(
                                                0,
                                                remainingMB /
                                                averageMBps));
                                }

                                progress?.Report(
                                    new TelegramUploadProgress
                                    {
                                        FileIndex =
                                            currentFileNumber,

                                        FileCount =
                                            filePaths.Count,

                                        FileName =
                                            fileInfo.Name,

                                        CurrentFileUploadedBytes =
                                            uploadedBytes,

                                        CurrentFileTotalBytes =
                                            totalBytes,

                                        OverallUploadedBytes =
                                            overallUploadedBytes,

                                        OverallTotalBytes =
                                            overallTotalBytes,

                                        OverallPercent =
                                            Math.Clamp(
                                                overallPercent,
                                                0,
                                                100),

                                        InstantMBps =
                                            instantMBps,

                                        AverageMBps =
                                            averageMBps,

                                        Elapsed =
                                            uploadStopwatch.Elapsed,

                                        Remaining =
                                            remaining
                                    });

                                lastOverallBytes =
                                    overallUploadedBytes;

                                lastSpeedCheckSeconds =
                                    nowSeconds;
                            }
                        });

                cancellationToken
                    .ThrowIfCancellationRequested();

                // =================================================
                // TELEGRAM'A GERÇEK VIDEO OLARAK GÖNDER
                // =================================================

                if (videoMetadata != null &&
                    videoMetadata.Width > 0 &&
                    videoMetadata.Height > 0 &&
                    videoMetadata.DurationSeconds > 0)
                {
                    DocumentAttributeVideo videoAttribute =
                        new DocumentAttributeVideo
                        {
                            w =
                                videoMetadata.Width,

                            h =
                                videoMetadata.Height,

                            duration =
                                videoMetadata.DurationSeconds,

                            flags =
                                videoMetadata.SupportsStreaming
                                    ? DocumentAttributeVideo
                                      .Flags
                                      .supports_streaming
                                    : 0
                        };

                    DocumentAttributeFilename fileNameAttribute =
                        new DocumentAttributeFilename
                        {
                            file_name =
                                fileInfo.Name
                        };

                    InputMediaUploadedDocument media =
                        new InputMediaUploadedDocument(
                            uploadedFile,
                            videoMetadata.MimeType,
                            videoAttribute,
                            fileNameAttribute);

                    if (!string.IsNullOrWhiteSpace(
                            thumbnailPath) &&
                        File.Exists(
                            thumbnailPath))
                    {
                        InputFileBase uploadedThumbnail =
                            await client.UploadFileAsync(
                                thumbnailPath,
                                (uploadedBytes, totalBytes) =>
                                {
                                    cancellationToken
                                        .ThrowIfCancellationRequested();
                                });

                        media.thumb =
                            uploadedThumbnail;

                        media.flags |=
                            InputMediaUploadedDocument
                            .Flags
                            .has_thumb;
                    }

                    pendingAlbumMedia.Add(
                        media);

                    // Telegram bir medya grubunda en fazla 10 öğeye izin verir.
                    // 10'a ulaştığımızda mevcut grubu gönderip yeni grup başlat.
                    if (pendingAlbumMedia.Count >=
                        telegramAlbumMaxItems)
                    {
                        await SendPendingAlbumAsync(
                            client,
                            peer,
                            pendingAlbumMedia,
                            cancellationToken,
                            normalizedCaption);
                    }
                }
                else
                {
                    // Video metadata alınamazsa önce bekleyen video albümünü
                    // gönder, ardından bu dosyayı eski güvenli yöntemle tek başına gönder.
                    await SendPendingAlbumAsync(
                        client,
                        peer,
                        pendingAlbumMedia,
                        cancellationToken,
                        normalizedCaption);

                    string singleCaption =
                        !string.IsNullOrWhiteSpace(
                            normalizedCaption)
                            ? normalizedCaption
                            : fileInfo.Name;

                    await client.SendMediaAsync(
                        peer,
                        singleCaption,
                        uploadedFile);
                }
                }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(
                            thumbnailPath) &&
                        File.Exists(
                            thumbnailPath))
                    {
                        try
                        {
                            File.Delete(
                                thumbnailPath);
                        }
                        catch
                        {
                        }
                    }
                }

                completedBytes +=
                    currentFileSize;

                double elapsedSeconds =
                    Math.Max(
                        uploadStopwatch
                        .Elapsed
                        .TotalSeconds,
                        0.001);

                double averageAfterFile =
                    completedBytes /
                    elapsedSeconds /
                    1024.0 /
                    1024.0;

                double percentAfterFile =
                    overallTotalBytes > 0
                        ? completedBytes *
                          100.0 /
                          overallTotalBytes
                        : 100;

                TimeSpan? remainingAfterFile =
                    null;

                if (averageAfterFile > 0)
                {
                    double remainingMB =
                        (overallTotalBytes -
                         completedBytes) /
                        1024.0 /
                        1024.0;

                    remainingAfterFile =
                        TimeSpan.FromSeconds(
                            Math.Max(
                                0,
                                remainingMB /
                                averageAfterFile));
                }

                progress?.Report(
                    new TelegramUploadProgress
                    {
                        FileIndex =
                            currentFileNumber,

                        FileCount =
                            filePaths.Count,

                        FileName =
                            fileInfo.Name,

                        CurrentFileUploadedBytes =
                            currentFileSize,

                        CurrentFileTotalBytes =
                            currentFileSize,

                        OverallUploadedBytes =
                            completedBytes,

                        OverallTotalBytes =
                            overallTotalBytes,

                        OverallPercent =
                            Math.Clamp(
                                percentAfterFile,
                                0,
                                100),

                        InstantMBps =
                            0,

                        AverageMBps =
                            averageAfterFile,

                        Elapsed =
                            uploadStopwatch.Elapsed,

                        Remaining =
                            remainingAfterFile
                    });
            }

            // Son grupta 10'dan az video kaldıysa şimdi gönder.
            await SendPendingAlbumAsync(
                client,
                peer,
                pendingAlbumMedia,
                cancellationToken,
                normalizedCaption);

            uploadStopwatch.Stop();

            progress?.Report(
                new TelegramUploadProgress
                {
                    FileIndex =
                        filePaths.Count,

                    FileCount =
                        filePaths.Count,

                    FileName =
                        Path.GetFileName(
                            filePaths[^1]),

                    CurrentFileUploadedBytes =
                        new FileInfo(
                            filePaths[^1])
                        .Length,

                    CurrentFileTotalBytes =
                        new FileInfo(
                            filePaths[^1])
                        .Length,

                    OverallUploadedBytes =
                        overallTotalBytes,

                    OverallTotalBytes =
                        overallTotalBytes,

                    OverallPercent =
                        100,

                    InstantMBps =
                        0,

                    AverageMBps =
                        overallTotalBytes /
                        Math.Max(
                            uploadStopwatch
                            .Elapsed
                            .TotalSeconds,
                            0.001) /
                        1024.0 /
                        1024.0,

                    Elapsed =
                        uploadStopwatch.Elapsed,

                    Remaining =
                        TimeSpan.Zero
                });
        }

        // =========================================================
        // TELEGRAM GROUPED MEDIA / ALBUM
        // =========================================================

        private static async Task SendPendingAlbumAsync(
            Client client,
            InputPeer peer,
            List<InputMedia> pendingAlbumMedia,
            CancellationToken cancellationToken,
            string albumCaption = "")
        {
            if (pendingAlbumMedia.Count == 0)
            {
                return;
            }

            cancellationToken
                .ThrowIfCancellationRequested();

            string caption =
                albumCaption ?? "";

            if (pendingAlbumMedia.Count == 1)
            {
                // Telegram albümü en az iki öğe ile anlamlıdır.
                // Tek part kaldıysa normal video mesajı olarak gönder.
                await client.SendMessageAsync(
                    peer,
                    caption,
                    pendingAlbumMedia[0]);
            }
            else
            {
                // WTelegramClient helper'ı uploaded media nesnelerini
                // Telegram'ın grouped media / album formatına çevirir.
                await client.SendAlbumAsync(
                    peer,
                    pendingAlbumMedia,
                    caption);
            }

            pendingAlbumMedia.Clear();

            cancellationToken
                .ThrowIfCancellationRequested();
        }

        // =========================================================
        // VIDEO THUMBNAIL
        // =========================================================

        private static async Task<bool>
            CreateVideoThumbnailAsync(
                string ffmpegPath,
                string videoPath,
                string thumbnailPath,
                int durationSeconds,
                CancellationToken cancellationToken)
        {
            double seekSeconds =
                durationSeconds > 2
                    ? 1.0
                    : 0.0;

            ProcessStartInfo info =
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

            info.ArgumentList.Add("-y");

            if (seekSeconds > 0)
            {
                info.ArgumentList.Add("-ss");

                info.ArgumentList.Add(
                    seekSeconds.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture));
            }

            info.ArgumentList.Add("-i");
            info.ArgumentList.Add(videoPath);

            info.ArgumentList.Add("-frames:v");
            info.ArgumentList.Add("1");

            // Telegram thumbnail'ı için küçük ve güvenli bir JPEG.
            // En/boy oranını korur ve hiçbir kenarı 320 px'i geçmez.
            info.ArgumentList.Add("-vf");
            info.ArgumentList.Add(
                "scale=320:320:force_original_aspect_ratio=decrease");

            info.ArgumentList.Add("-q:v");
            info.ArgumentList.Add("4");

            info.ArgumentList.Add("-an");

            info.ArgumentList.Add(
                thumbnailPath);

            using Process process =
                new Process
                {
                    StartInfo =
                        info
                };

            try
            {
                process.Start();

                using CancellationTokenRegistration registration =
                    cancellationToken.Register(
                        () =>
                        {
                            try
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill(
                                        entireProcessTree: true);
                                }
                            }
                            catch
                            {
                            }
                        });

                Task<string> outputTask =
                    process.StandardOutput
                    .ReadToEndAsync();

                Task<string> errorTask =
                    process.StandardError
                    .ReadToEndAsync();

                await process.WaitForExitAsync();

                _ =
                    await outputTask;

                _ =
                    await errorTask;

                cancellationToken
                    .ThrowIfCancellationRequested();

                if (process.ExitCode != 0)
                {
                    return false;
                }

                if (!File.Exists(
                        thumbnailPath))
                {
                    return false;
                }

                FileInfo thumbnailInfo =
                    new FileInfo(
                        thumbnailPath);

                // Telegram custom thumb için JPEG'in küçük kalmasını istiyoruz.
                // Çok büyük bir thumbnail oluşursa güvenli şekilde thumbsuz devam et.
                return
                    thumbnailInfo.Length > 0 &&
                    thumbnailInfo.Length <
                    200 * 1024;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // FFPROBE VIDEO METADATA
        // =========================================================

        private static async Task<VideoMetadata?>
            GetVideoMetadataAsync(
                string ffprobePath,
                string filePath,
                CancellationToken cancellationToken)
        {
            string extension =
                Path.GetExtension(
                    filePath)
                .ToLowerInvariant();

            // Şimdilik Telegram'ın video oynatıcısına
            // uygun dosyalar için video metadata gönderiyoruz.
            bool supportedVideoExtension =
                extension == ".mp4" ||
                extension == ".m4v" ||
                extension == ".mov";

            if (!supportedVideoExtension)
            {
                return null;
            }

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
                "-select_streams");

            info.ArgumentList.Add(
                "v:0");

            info.ArgumentList.Add(
                "-show_entries");

            info.ArgumentList.Add(
                "stream=width,height:format=duration");

            info.ArgumentList.Add(
                "-of");

            info.ArgumentList.Add(
                "json");

            info.ArgumentList.Add(
                filePath);

            using Process process =
                new Process
                {
                    StartInfo =
                        info
                };

            process.Start();

            using CancellationTokenRegistration registration =
                cancellationToken.Register(
                    () =>
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(
                                    entireProcessTree: true);
                            }
                        }
                        catch
                        {
                        }
                    });

            Task<string> outputTask =
                process.StandardOutput
                .ReadToEndAsync();

            Task<string> errorTask =
                process.StandardError
                .ReadToEndAsync();

            await process.WaitForExitAsync();

            string output =
                await outputTask;

            string error =
                await errorTask;

            cancellationToken
                .ThrowIfCancellationRequested();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    "FFprobe video metadata hatası:\n\n" +
                    error);
            }

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(
                        output);

                JsonElement root =
                    document.RootElement;

                if (!root.TryGetProperty(
                        "streams",
                        out JsonElement streams) ||
                    streams.GetArrayLength() ==
                    0)
                {
                    return null;
                }

                JsonElement videoStream =
                    streams[0];

                int width =
                    videoStream.TryGetProperty(
                        "width",
                        out JsonElement widthElement)
                        ? widthElement.GetInt32()
                        : 0;

                int height =
                    videoStream.TryGetProperty(
                        "height",
                        out JsonElement heightElement)
                        ? heightElement.GetInt32()
                        : 0;

                double duration =
                    0;

                if (root.TryGetProperty(
                        "format",
                        out JsonElement format) &&
                    format.TryGetProperty(
                        "duration",
                        out JsonElement durationElement))
                {
                    string? durationText =
                        durationElement.GetString();

                    double.TryParse(
                        durationText,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out duration);
                }

                int durationSeconds =
                    Math.Max(
                        1,
                        (int)duration);

                string mimeType =
                    extension switch
                    {
                        ".mov" =>
                            "video/quicktime",

                        _ =>
                            "video/mp4"
                    };

                bool supportsStreaming =
                    extension == ".mp4" ||
                    extension == ".m4v";

                return new VideoMetadata
                {
                    Width =
                        width,

                    Height =
                        height,

                    DurationSeconds =
                        durationSeconds,

                    MimeType =
                        mimeType,

                    SupportsStreaming =
                        supportsStreaming
                };
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // CLIENT
        // =========================================================

        private static Client CreateClient(
            TelegramStoredSettings settings,
            int parallelTransfers)
        {
            string? Config(
                string what)
            {
                return what switch
                {
                    "api_id" =>
                        settings.ApiId.ToString(),

                    "api_hash" =>
                        settings.ApiHash,

                    "session_pathname" =>
                        SessionPath,

                    "phone_number" =>
                        settings.Phone,

                    _ =>
                        null
                };
            }

            Client client =
                new Client(
                    Config);

            client.ParallelTransfers =
                Math.Clamp(
                    parallelTransfers,
                    1,
                    16);

            return client;
        }

        // =========================================================
        // ACCOUNT TEXT
        // =========================================================

        private static string GetAccountText(
            User user)
        {
            if (!string.IsNullOrWhiteSpace(
                    user.username))
            {
                return
                    "@" +
                    user.username;
            }

            string result =
                user.first_name ??
                "";

            if (!string.IsNullOrWhiteSpace(
                    user.last_name))
            {
                result +=
                    " " +
                    user.last_name;
            }

            return
                result.Trim();
        }

        // =========================================================
        // SETTINGS
        // =========================================================

        private static TelegramStoredSettings?
            LoadSettings()
        {
            try
            {
                byte[] encryptedData =
                    File.ReadAllBytes(
                        SettingsPath);

                byte[] rawData =
                    ProtectedData.Unprotect(
                        encryptedData,
                        null,
                        DataProtectionScope.CurrentUser);

                string json =
                    Encoding.UTF8.GetString(
                        rawData);

                return
                    JsonSerializer.Deserialize
                        <TelegramStoredSettings>(
                            json);
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // MODELS
        // =========================================================

        private sealed class TelegramStoredSettings
        {
            public int ApiId { get; set; }

            public string? ApiHash { get; set; }

            public string? Phone { get; set; }
        }

        private sealed class VideoMetadata
        {
            public int Width { get; init; }

            public int Height { get; init; }

            public int DurationSeconds { get; init; }

            public string MimeType { get; init; } =
                "video/mp4";

            public bool SupportsStreaming { get; init; }
        }
    }
}