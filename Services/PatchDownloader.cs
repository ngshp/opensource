using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NGPB.Launcher.Models;
using NGPB.Launcher.Security;

namespace NGPB.Launcher.Services;

public sealed class PatchDownloader
{
    private readonly HttpClient httpClient;

    private readonly object stateLock = new();

    private CancellationTokenSource? downloadCts;

    private bool isPaused;
    private bool isDownloading;


    public PatchDownloader()
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "NGPB-Launcher/1.0"
        );
    }


    // ============================================================
    // PUBLIC STATE
    // ============================================================

    public bool IsDownloading
    {
        get
        {
            lock (stateLock)
            {
                return isDownloading;
            }
        }
    }


    public bool IsPaused
    {
        get
        {
            lock (stateLock)
            {
                return isPaused;
            }
        }
    }


    // ============================================================
    // PAUSE
    // ============================================================

    public void Pause()
    {
        lock (stateLock)
        {
            if (!isDownloading)
                return;

            isPaused = true;
        }

        SecurityLogger.Info(
            "Patch download paused"
        );
    }


    // ============================================================
    // RESUME
    // ============================================================

    public void Resume()
    {
        lock (stateLock)
        {
            if (!isDownloading)
                return;

            isPaused = false;
        }

        SecurityLogger.Info(
            "Patch download resumed"
        );
    }


    // ============================================================
    // CANCEL
    // ============================================================

    public void Cancel()
    {
        try
        {
            downloadCts?.Cancel();

            SecurityLogger.Info(
                "Patch download cancelled"
            );
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Cancel error: {ex.Message}"
            );
        }
    }


    // ============================================================
    // DOWNLOAD
    // ============================================================

    public async Task Download(
        PatchFile file,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        if (string.IsNullOrWhiteSpace(file.Name))
            throw new InvalidOperationException(
                "Patch file name is empty."
            );

        if (string.IsNullOrWhiteSpace(file.Url))
            throw new InvalidOperationException(
                $"Patch URL missing: {file.Name}"
            );

        if (string.IsNullOrWhiteSpace(file.SHA256))
            throw new InvalidOperationException(
                $"SHA-256 missing: {file.Name}"
            );


        ValidateHttpsUrl(file.Url);


        lock (stateLock)
        {
            if (isDownloading)
            {
                throw new InvalidOperationException(
                    "Another patch download is already running."
                );
            }

            isDownloading = true;
            isPaused = false;
        }


        downloadCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );


        var token =
            downloadCts.Token;


        string? tempPath = null;
        string? backupPath = null;


        try
        {
            SecurityLogger.Security(
                $"Patch download started: {file.Name}"
            );


            string gameDirectory =
                AppContext.BaseDirectory;


            string targetPath =
                GetSafeTargetPath(
                    gameDirectory,
                    file.Name
                );


            tempPath =
                targetPath + ".tmp";


            backupPath =
                targetPath + ".backup";


            Directory.CreateDirectory(
                Path.GetDirectoryName(targetPath)!
            );


            // ====================================================
            // DOWNLOAD TO TEMP FILE
            // ====================================================

            await DownloadWithResumeAsync(
                file,
                tempPath,
                progress,
                token
            );


            // ====================================================
            // SHA256 VERIFICATION
            // ====================================================

            SecurityLogger.Security(
                $"SHA-256 verification started: {file.Name}"
            );


            string actualHash =
                await CalculateSha256Async(
                    tempPath,
                    token
                );


            string expectedHash =
                NormalizeHash(
                    file.SHA256
                );


            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(actualHash),
                    Convert.FromHexString(expectedHash)))
            {
                SecurityLogger.Error(
                    $"SHA-256 verification FAILED: {file.Name}"
                );


                SafeDelete(tempPath);


                throw new InvalidDataException(
                    $"SHA-256 verification failed for {file.Name}."
                );
            }


            SecurityLogger.Security(
                $"SHA-256 verification OK: {file.Name}"
            );


            // ====================================================
            // SIZE VERIFICATION
            // ====================================================

            if (file.Size > 0)
            {
                long actualSize =
                    new FileInfo(tempPath).Length;


                if (actualSize != file.Size)
                {
                    SecurityLogger.Error(
                        $"File size mismatch: {file.Name}"
                    );


                    SafeDelete(tempPath);


                    throw new InvalidDataException(
                        $"File size verification failed for {file.Name}."
                    );
                }
            }


            // ====================================================
            // INSTALL WITH BACKUP + ROLLBACK
            // ====================================================

            await InstallWithRollbackAsync(
                tempPath,
                targetPath,
                backupPath,
                token
            );


            SecurityLogger.Security(
                $"Patch installed successfully: {file.Name}"
            );


            progress?.Report(
                new DownloadProgressInfo
                {
                    FileName = file.Name,
                    Percentage = 100,
                    Speed = 0
                }
            );
        }
        catch (OperationCanceledException)
        {
            SecurityLogger.Info(
                $"Patch download cancelled: {file.Name}"
            );

            throw;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Patch download failed: {file.Name} - {ex.Message}"
            );

            throw;
        }
        finally
        {
            lock (stateLock)
            {
                isDownloading = false;
                isPaused = false;
            }


            downloadCts?.Dispose();
            downloadCts = null;
        }
    }


    // ============================================================
    // DOWNLOAD WITH HTTP RANGE RESUME
    // ============================================================

    private async Task DownloadWithResumeAsync(
        PatchFile file,
        string tempPath,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        long existingBytes = 0;


        if (File.Exists(tempPath))
        {
            existingBytes =
                new FileInfo(tempPath).Length;
        }


        long totalBytes =
            file.Size > 0
                ? file.Size
                : 0;


        bool resume =
            existingBytes > 0;


        using HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Get,
                file.Url
            );


        if (resume)
        {
            request.Headers.Range =
                new System.Net.Http.Headers.RangeHeaderValue(
                    existingBytes,
                    null
                );


            SecurityLogger.Info(
                $"Resuming {file.Name} from {existingBytes} bytes"
            );
        }


        using HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );


        // ========================================================
        // SERVER IGNORES RANGE
        // ========================================================

        if (resume &&
            response.StatusCode ==
            System.Net.HttpStatusCode.OK)
        {
            SecurityLogger.Info(
                $"Server does not support resume: {file.Name}"
            );


            response.Dispose();


            existingBytes = 0;


            SafeDelete(tempPath);


            using HttpRequestMessage freshRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    file.Url
                );


            using HttpResponseMessage freshResponse =
                await httpClient.SendAsync(
                    freshRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );


            freshResponse.EnsureSuccessStatusCode();


            await WriteDownloadAsync(
                freshResponse,
                tempPath,
                0,
                totalBytes,
                file.Name,
                progress,
                cancellationToken
            );


            return;
        }


        response.EnsureSuccessStatusCode();


        await WriteDownloadAsync(
            response,
            tempPath,
            existingBytes,
            totalBytes,
            file.Name,
            progress,
            cancellationToken
        );
    }


    // ============================================================
    // STREAM DOWNLOAD
    // ============================================================

    private async Task WriteDownloadAsync(
        HttpResponseMessage response,
        string tempPath,
        long existingBytes,
        long totalBytes,
        string fileName,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        long downloaded =
            existingBytes;


        if (totalBytes <= 0 &&
            response.Content.Headers.ContentLength.HasValue)
        {
            totalBytes =
                existingBytes +
                response.Content.Headers.ContentLength.Value;
        }


        await using Stream networkStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken
            );


        FileMode fileMode =
            existingBytes > 0
                ? FileMode.Append
                : FileMode.Create;


        await using FileStream fileStream =
            new FileStream(
                tempPath,
                fileMode,
                FileAccess.Write,
                FileShare.None,
                1024 * 64,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan
            );


        byte[] buffer =
            new byte[1024 * 128];


        var stopwatch =
            System.Diagnostics.Stopwatch.StartNew();


        long speedBytes =
            0;


        long lastBytes =
            downloaded;


        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();


            await WaitIfPausedAsync(
                cancellationToken
            );


            int read =
                await networkStream.ReadAsync(
                    buffer,
                    cancellationToken
                );


            if (read == 0)
                break;


            await fileStream.WriteAsync(
                buffer.AsMemory(0, read),
                cancellationToken
            );


            downloaded += read;


            speedBytes += read;


            double percentage = 0;


            if (totalBytes > 0)
            {
                percentage =
                    downloaded *
                    100.0 /
                    totalBytes;


                if (percentage > 100)
                    percentage = 100;
            }


            double speedMb =
                0;


            if (stopwatch.Elapsed.TotalSeconds > 0)
            {
                speedMb =
                    speedBytes /
                    1024.0 /
                    1024.0 /
                    stopwatch.Elapsed.TotalSeconds;
            }


            // Keep speed graph/progress smooth by reporting
            // every received chunk.

            progress?.Report(
                new DownloadProgressInfo
                {
                    FileName = fileName,
                    Percentage = percentage,
                    Speed = speedMb
                }
            );


            // Reset speed calculation every ~500 ms.

            if (stopwatch.ElapsedMilliseconds >= 500)
            {
                lastBytes = downloaded;
                speedBytes = 0;
                stopwatch.Restart();
            }
        }


        await fileStream.FlushAsync(
            cancellationToken
        );


        fileStream.Flush(true);


        if (totalBytes > 0 &&
            downloaded != totalBytes)
        {
            throw new IOException(
                $"Incomplete download: {fileName}. " +
                $"Expected {totalBytes} bytes, " +
                $"received {downloaded} bytes."
            );
        }
    }


    // ============================================================
    // PAUSE WAIT
    // ============================================================

    private async Task WaitIfPausedAsync(
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();


            bool paused;


            lock (stateLock)
            {
                paused = isPaused;
            }


            if (!paused)
                return;


            await Task.Delay(
                150,
                cancellationToken
            );
        }
    }


    // ============================================================
    // SHA256
    // ============================================================

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using FileStream stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 128,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan
            );


        using SHA256 sha256 =
            SHA256.Create();


        byte[] hash =
            await sha256.ComputeHashAsync(
                stream,
                cancellationToken
            );


        return Convert.ToHexString(
            hash
        );
    }


    // ============================================================
    // INSTALL + BACKUP + ROLLBACK
    // ============================================================

    private static async Task InstallWithRollbackAsync(
        string tempPath,
        string targetPath,
        string backupPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();


        bool hadOriginal =
            File.Exists(targetPath);


        try
        {
            // -----------------------------------------------
            // BACKUP CURRENT FILE
            // -----------------------------------------------

            if (hadOriginal)
            {
                SafeDelete(backupPath);


                File.Move(
                    targetPath,
                    backupPath
                );
            }


            cancellationToken.ThrowIfCancellationRequested();


            // -----------------------------------------------
            // MOVE VERIFIED TEMP INTO PLACE
            // -----------------------------------------------

            File.Move(
                tempPath,
                targetPath
            );


            // -----------------------------------------------
            // DELETE BACKUP AFTER SUCCESS
            // -----------------------------------------------

            if (hadOriginal)
            {
                SafeDelete(backupPath);
            }


            await Task.CompletedTask;
        }
        catch
        {
            // -----------------------------------------------
            // ROLLBACK
            // -----------------------------------------------

            SecurityLogger.Error(
                $"Installation failed. Starting rollback: {targetPath}"
            );


            try
            {
                SafeDelete(targetPath);


                if (hadOriginal &&
                    File.Exists(backupPath))
                {
                    File.Move(
                        backupPath,
                        targetPath
                    );


                    SecurityLogger.Security(
                        $"Rollback successful: {targetPath}"
                    );
                }
            }
            catch (Exception rollbackEx)
            {
                SecurityLogger.Error(
                    $"ROLLBACK FAILED: {rollbackEx.Message}"
                );
            }


            throw;
        }
    }


    // ============================================================
    // SAFE TARGET PATH
    // ============================================================

    private static string GetSafeTargetPath(
        string rootDirectory,
        string relativeFile)
    {
        if (string.IsNullOrWhiteSpace(relativeFile))
            throw new InvalidDataException(
                "Patch file path is empty."
            );


        relativeFile =
            relativeFile.Replace(
                '/',
                Path.DirectorySeparatorChar
            );


        relativeFile =
            relativeFile.Replace(
                '\\',
                Path.DirectorySeparatorChar
            );


        if (Path.IsPathRooted(relativeFile))
        {
            throw new InvalidDataException(
                "Absolute patch paths are not allowed."
            );
        }


        string fullRoot =
            Path.GetFullPath(
                rootDirectory
            );


        string fullPath =
            Path.GetFullPath(
                Path.Combine(
                    fullRoot,
                    relativeFile
                )
            );


        if (!fullPath.StartsWith(
                fullRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Invalid patch path."
            );
        }


        return fullPath;
    }


    // ============================================================
    // HTTPS VALIDATION
    // ============================================================

    private static void ValidateHttpsUrl(
        string url)
    {
        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri? uri))
        {
            throw new InvalidDataException(
                "Invalid patch URL."
            );
        }


        if (!string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Patch downloads require HTTPS."
            );
        }
    }


    // ============================================================
    // NORMALIZE SHA256
    // ============================================================

    private static string NormalizeHash(
        string hash)
    {
        string normalized =
            hash
                .Trim()
                .Replace("-", "")
                .Replace(" ", "")
                .ToUpperInvariant();


        if (normalized.Length != 64)
        {
            throw new InvalidDataException(
                "Invalid SHA-256 hash."
            );
        }


        try
        {
            _ = Convert.FromHexString(
                normalized
            );
        }
        catch
        {
            throw new InvalidDataException(
                "Invalid SHA-256 hexadecimal value."
            );
        }


        return normalized;
    }


    // ============================================================
    // SAFE DELETE
    // ============================================================

    private static void SafeDelete(
        string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"File cleanup failed: {path} - {ex.Message}"
            );
        }
    }
}
