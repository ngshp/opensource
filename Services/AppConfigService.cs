using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NGPB.Launcher.Models;
using NGPB.Launcher.Security;

namespace NGPB.Launcher.Services;

public sealed class AppConfigService
{
    private const string ConfigFileName = "app.secure";

    private readonly string configPath;

    // ============================================================
    // IMPORTANT:
    // Public key saja yang boleh berada di launcher.
    // Private key JANGAN dimasukkan ke aplikasi/client.
    //
    // Ganti placeholder ini dengan RSA PUBLIC KEY milik release
    // production kamu.
    // ============================================================

    private const string PublicKeyPem = """
-----BEGIN PUBLIC KEY-----
REPLACE_WITH_YOUR_RSA_PUBLIC_KEY
-----END PUBLIC KEY-----
""";


    public AppConfigService()
    {
        configPath =
            Path.Combine(
                AppContext.BaseDirectory,
                ConfigFileName
            );
    }


    // ============================================================
    // LOAD + VERIFY
    // ============================================================

    public AppConfig? Load()
    {
        try
        {
            if (!File.Exists(configPath))
            {
                SecurityLogger.Error(
                    "app.secure not found"
                );

                return null;
            }


            string documentText =
                File.ReadAllText(
                    configPath,
                    Encoding.UTF8
                );


            if (string.IsNullOrWhiteSpace(
                    documentText))
            {
                SecurityLogger.Error(
                    "app.secure is empty"
                );

                return null;
            }


            SecureConfigDocument? document =
                JsonSerializer.Deserialize<SecureConfigDocument>(
                    documentText
                );


            if (document == null)
            {
                SecurityLogger.Error(
                    "app.secure JSON invalid"
                );

                return null;
            }


            if (string.IsNullOrWhiteSpace(
                    document.Payload))
            {
                SecurityLogger.Error(
                    "app.secure payload missing"
                );

                return null;
            }


            if (string.IsNullOrWhiteSpace(
                    document.Signature))
            {
                SecurityLogger.Error(
                    "app.secure signature missing"
                );

                return null;
            }


            if (!VerifySignature(
                    document.Payload,
                    document.Signature))
            {
                SecurityLogger.Security(
                    "app.secure RSA signature INVALID"
                );

                return null;
            }


            AppConfig? config =
                JsonSerializer.Deserialize<AppConfig>(
                    document.Payload
                );


            if (config == null)
            {
                SecurityLogger.Error(
                    "app.secure payload could not be parsed"
                );

                return null;
            }


            if (!ValidateConfig(config))
            {
                SecurityLogger.Error(
                    "app.secure configuration validation failed"
                );

                return null;
            }


            SecurityLogger.Security(
                "app.secure RSA signature VALID"
            );

            SecurityLogger.Info(
                $"Config version: {config.LauncherVersion}"
            );


            return config;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"app.secure load failed: {ex.Message}"
            );

            return null;
        }
    }


    // ============================================================
    // RSA SIGNATURE VERIFICATION
    // ============================================================

    private static bool VerifySignature(
        string payload,
        string signatureBase64)
    {
        try
        {
            if (PublicKeyPem.Contains(
                    "REPLACE_WITH_YOUR_RSA_PUBLIC_KEY",
                    StringComparison.Ordinal))
            {
                SecurityLogger.Error(
                    "RSA public key has not been configured"
                );

                return false;
            }


            byte[] data =
                Encoding.UTF8.GetBytes(
                    payload
                );


            byte[] signature =
                Convert.FromBase64String(
                    signatureBase64
                );


            using RSA rsa =
                RSA.Create();


            rsa.ImportFromPem(
                PublicKeyPem
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
                $"RSA verification failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // CONFIG VALIDATION
    // ============================================================

    private static bool ValidateConfig(
        AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(
                config.LauncherVersion))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.GameName))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.GameExe))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.ApiUrl))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.PatchUrl))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.Manifest))
        {
            return false;
        }


        if (!IsHttpsUrl(config.ApiUrl))
        {
            SecurityLogger.Error(
                "app.secure ApiUrl must use HTTPS"
            );

            return false;
        }


        if (!IsHttpsUrl(config.PatchUrl))
        {
            SecurityLogger.Error(
                "app.secure PatchUrl must use HTTPS"
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // HTTPS VALIDATION
    // ============================================================

    private static bool IsHttpsUrl(
        string value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out Uri? uri)
               &&
               uri.Scheme.Equals(
                   Uri.UriSchemeHttps,
                   StringComparison.OrdinalIgnoreCase
               );
    }


    // ============================================================
    // PATH
    // ============================================================

    public string GetConfigPath()
    {
        return configPath;
    }


    // ============================================================
    // SECURE DOCUMENT MODEL
    // ============================================================

    private sealed class SecureConfigDocument
    {
        public string Payload { get; set; } = "";

        public string Signature { get; set; } = "";
    }
}
