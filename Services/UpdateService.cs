using System;
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

    private const string ManifestFile =
        "manifest.secure";

    // ============================================================
    // RSA PUBLIC KEY
    //
    // PRIVATE KEY TIDAK BOLEH ADA DI LAUNCHER.
    // Private key hanya digunakan server/CI untuk signing.
    // ============================================================

    private const string ManifestPublicKey = """
-----BEGIN PUBLIC KEY-----
REPLACE_WITH_MANIFEST_RSA_PUBLIC_KEY
-----END PUBLIC KEY-----
""";


    public UpdateService()
    {
        httpClient =
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(30)
            };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "NGPB-Launcher"
        );
    }


    // ============================================================
    // GET MANIFEST
    // ============================================================

    public async Task<UpdateManifest?> GetManifest(
        string manifestUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    manifestUrl))
            {
                SecurityLogger.Error(
                    "Manifest URL is empty"
                );

                return null;
            }


            if (!Uri.TryCreate(
                    manifestUrl,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                SecurityLogger.Error(
                    "Manifest URL invalid"
                );

                return null;
            }


            if (!uri.Scheme.Equals(
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase))
            {
                SecurityLogger.Error(
                    "Manifest must use HTTPS"
                );

                return null;
            }


            SecurityLogger.Info(
                "Downloading signed manifest"
            );


            using HttpResponseMessage response =
                await httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );


            response.EnsureSuccessStatusCode();


            string document =
                await response.Content.ReadAsStringAsync(
                    cancellationToken
                );


            if (string.IsNullOrWhiteSpace(
                    document))
            {
                SecurityLogger.Error(
                    "Manifest is empty"
                );

                return null;
            }


            SignedManifest? signed =
                JsonSerializer.Deserialize<SignedManifest>(
                    document
                );


            if (signed == null)
            {
                SecurityLogger.Error(
                    "Manifest format invalid"
                );

                return null;
            }


            if (string.IsNullOrWhiteSpace(
                    signed.Payload))
            {
                SecurityLogger.Error(
                    "Manifest payload missing"
                );

                return null;
            }


            if (string.IsNullOrWhiteSpace(
                    signed.Signature))
            {
                SecurityLogger.Error(
                    "Manifest signature missing"
                );

                return null;
            }


            // ----------------------------------------------------
            // RSA SIGNATURE
            // ----------------------------------------------------

            if (!VerifySignature(
                    signed.Payload,
                    signed.Signature))
            {
                SecurityLogger.Security(
                    "MANIFEST RSA SIGNATURE INVALID"
                );

                return null;
            }


            SecurityLogger.Security(
                "Manifest RSA signature VALID"
            );


            UpdateManifest? manifest =
                JsonSerializer.Deserialize<UpdateManifest>(
                    signed.Payload
                );


            if (manifest == null)
            {
                SecurityLogger.Error(
                    "Manifest payload invalid"
                );

                return null;
            }


            if (!ValidateManifest(
                    manifest))
            {
                SecurityLogger.Error(
                    "Manifest validation failed"
                );

                return null;
            }


            SecurityLogger.Security(
                $"Signed manifest accepted: {manifest.Version}"
            );


            return manifest;
        }
        catch (OperationCanceledException)
        {
            SecurityLogger.Info(
                "Manifest request cancelled"
            );

            return null;
        }
        catch (HttpRequestException ex)
        {
            SecurityLogger.Error(
                $"Manifest HTTP error: {ex.Message}"
            );

            return null;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Manifest error: {ex.Message}"
            );

            return null;
        }
    }


    // ============================================================
    // RSA VERIFY
    // ============================================================

    private static bool VerifySignature(
        string payload,
        string signatureBase64)
    {
        try
        {
            if (ManifestPublicKey.Contains(
                    "REPLACE_WITH_MANIFEST_RSA_PUBLIC_KEY",
                    StringComparison.Ordinal))
            {
                SecurityLogger.Error(
                    "Manifest RSA public key not configured"
                );

                return false;
            }


            byte[] payloadBytes =
                Encoding.UTF8.GetBytes(
                    payload
                );


            byte[] signatureBytes =
                Convert.FromBase64String(
                    signatureBase64
                );


            using RSA rsa =
                RSA.Create();


            rsa.ImportFromPem(
                ManifestPublicKey
            );


            return rsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Manifest RSA verification error: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // MANIFEST VALIDATION
    // ============================================================

    private static bool ValidateManifest(
        UpdateManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(
                manifest.Version))
        {
            return false;
        }


        if (manifest.Files == null ||
            manifest.Files.Count == 0)
        {
            SecurityLogger.Error(
                "Manifest contains no files"
            );

            return false;
        }


        foreach (PatchFile file in manifest.Files)
        {
            if (!ValidatePatchFile(file))
            {
                return false;
            }
        }


        return true;
    }


    // ============================================================
    // PATCH FILE VALIDATION
    // ============================================================

    private static bool ValidatePatchFile(
        PatchFile file)
    {
        if (string.IsNullOrWhiteSpace(
                file.Name))
        {
            SecurityLogger.Error(
                "Patch file name missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                file.Url))
        {
            SecurityLogger.Error(
                $"Patch URL missing: {file.Name}"
            );

            return false;
        }


        if (!Uri.TryCreate(
                file.Url,
                UriKind.Absolute,
                out Uri? uri))
        {
            SecurityLogger.Error(
                $"Patch URL invalid: {file.Name}"
            );

            return false;
        }


        if (!uri.Scheme.Equals(
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            SecurityLogger.Error(
                $"Patch URL must use HTTPS: {file.Name}"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                file.Sha256))
        {
            SecurityLogger.Error(
                $"SHA-256 missing: {file.Name}"
            );

            return false;
        }


        if (!IsSha256(file.Sha256))
        {
            SecurityLogger.Error(
                $"Invalid SHA-256: {file.Name}"
            );

            return false;
        }


        if (file.Size < 0)
        {
            SecurityLogger.Error(
                $"Invalid file size: {file.Name}"
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // SHA-256 FORMAT
    // ============================================================

    private static bool IsSha256(
        string value)
    {
        if (value.Length != 64)
            return false;


        foreach (char c in value)
        {
            bool hex =
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');

            if (!hex)
                return false;
        }


        return true;
    }


    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        httpClient.Dispose();
    }


    // ============================================================
    // SIGNED MANIFEST MODEL
    // ============================================================

    private sealed class SignedManifest
    {
        public string Payload { get; set; } = "";

        public string Signature { get; set; } = "";
    }
}
