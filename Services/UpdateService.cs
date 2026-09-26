using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NGPB.Launcher.Models;
using NGPB.Launcher.Security;

namespace NGPB.Launcher.Services;

public sealed class UpdateService
{
    private readonly HttpClient httpClient;

    private readonly string manifestUrl;

    // RSA public key.
    // Ganti dengan PUBLIC KEY milik server produksi.
    private const string ManifestPublicKeyPem = """
-----BEGIN PUBLIC KEY-----
REPLACE_WITH_YOUR_RSA_PUBLIC_KEY
-----END PUBLIC KEY-----
""";


    public UpdateService()
    {
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "NGPB-Launcher/1.0"
        );

        manifestUrl =
            "https://patch.ngpb.com/manifest.json";
    }


    // ============================================================
    // GET MANIFEST
    // ============================================================

    public async Task<PatchManifest?> GetManifest(
        CancellationToken cancellationToken = default)
    {
        try
        {
            SecurityLogger.Info(
                "Requesting signed manifest"
            );


            ValidateHttpsUrl(manifestUrl);


            using HttpRequestMessage request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    manifestUrl
                );


            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );


            response.EnsureSuccessStatusCode();


            string json =
                await response.Content.ReadAsStringAsync(
                    cancellationToken
                );


            if (string.IsNullOrWhiteSpace(json))
            {
                SecurityLogger.Error(
                    "Manifest response empty"
                );

                return null;
            }


            // ====================================================
            // VERIFY SIGNATURE BEFORE USING FILE DATA
            // ====================================================

            if (!VerifyManifestSignature(json))
            {
                SecurityLogger.Error(
                    "Manifest signature verification FAILED"
                );

                throw new CryptographicException(
                    "Manifest signature verification failed."
                );
            }


            SecurityLogger.Security(
                "Manifest signature verified"
            );


            // ====================================================
            // PARSE
            // ====================================================

            PatchManifest? manifest =
                JsonSerializer.Deserialize<PatchManifest>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );


            if (manifest == null)
            {
                SecurityLogger.Error(
                    "Manifest parsing failed"
                );

                return null;
            }


            // ====================================================
            // VALIDATE MANIFEST
            // ====================================================

            ValidateManifest(manifest);


            SecurityLogger.Security(
                $"Manifest accepted: {manifest.Files.Count} files"
            );


            return manifest;
        }
        catch (OperationCanceledException)
        {
            SecurityLogger.Info(
                "Manifest request cancelled"
            );

            throw;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Manifest error: {ex.Message}"
            );

            throw;
        }
    }


    // ============================================================
    // SIGNATURE VERIFICATION
    // ============================================================

    private static bool VerifyManifestSignature(
        string manifestJson)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(manifestJson);


            JsonElement root =
                document.RootElement;


            if (!root.TryGetProperty(
                    "signature",
                    out JsonElement signatureElement))
            {
                SecurityLogger.Error(
                    "Manifest signature missing"
                );

                return false;
            }


            string? signatureBase64 =
                signatureElement.GetString();


            if (string.IsNullOrWhiteSpace(
                    signatureBase64))
            {
                SecurityLogger.Error(
                    "Manifest signature empty"
                );

                return false;
            }


            if (!root.TryGetProperty(
                    "payload",
                    out JsonElement payloadElement))
            {
                SecurityLogger.Error(
                    "Manifest payload missing"
                );

                return false;
            }


            string canonicalPayload =
                payloadElement.GetRawText();


            byte[] data =
                Encoding.UTF8.GetBytes(
                    canonicalPayload
                );


            byte[] signature =
                Convert.FromBase64String(
                    signatureBase64
                );


            using RSA rsa =
                RSA.Create();


            rsa.ImportFromPem(
                ManifestPublicKeyPem
            );


            return rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Manifest signature error: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // MANIFEST VALIDATION
    // ============================================================

    private static void ValidateManifest(
        PatchManifest manifest)
    {
        if (manifest.Files == null)
        {
            throw new InvalidDataException(
                "Manifest file list missing."
            );
        }


        if (manifest.Files.Count == 0)
        {
            SecurityLogger.Info(
                "Manifest contains no files"
            );

            return;
        }


        foreach (PatchFile file in manifest.Files)
        {
            if (file == null)
            {
                throw new InvalidDataException(
                    "Manifest contains null file."
                );
            }


            if (string.IsNullOrWhiteSpace(
                    file.Name))
            {
                throw new InvalidDataException(
                    "Manifest contains file without name."
                );
            }


            if (string.IsNullOrWhiteSpace(
                    file.Url))
            {
                throw new InvalidDataException(
                    $"URL missing for {file.Name}."
                );
            }


            if (!Uri.TryCreate(
                    file.Url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                throw new InvalidDataException(
                    $"Invalid URL for {file.Name}."
                );
            }


            if (!string.Equals(
                    uri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"HTTPS required for {file.Name}."
                );
            }


            if (string.IsNullOrWhiteSpace(
                    file.SHA256))
            {
                throw new InvalidDataException(
                    $"SHA-256 missing for {file.Name}."
                );
            }


            ValidateSha256(
                file.SHA256
            );


            if (file.Size < 0)
            {
                throw new InvalidDataException(
                    $"Invalid file size for {file.Name}."
                );
            }
        }
    }


    // ============================================================
    // SHA256 FORMAT
    // ============================================================

    private static void ValidateSha256(
        string hash)
    {
        string normalized =
            hash
                .Trim()
                .Replace("-", "")
                .Replace(" ", "");


        if (normalized.Length != 64)
        {
            throw new InvalidDataException(
                "Invalid SHA-256 length."
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
                "Invalid SHA-256 value."
            );
        }
    }


    // ============================================================
    // HTTPS
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
                "Invalid manifest URL."
            );
        }


        if (!string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Manifest must use HTTPS."
            );
        }
    }
}
