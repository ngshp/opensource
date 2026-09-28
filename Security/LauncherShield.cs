using System;
using System.IO;
using System.Security.Cryptography;

namespace NGPB.Launcher.Security;

public sealed class LauncherShield
{
    private readonly string launcherPath;

    public LauncherShield()
    {
        launcherPath =
            Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "Unable to determine launcher path."
            );
    }


    // ============================================================
    // MAIN SECURITY CHECK
    // ============================================================

    public bool RunSecurityCheck()
    {
        try
        {
            SecurityLogger.Security(
                "LauncherShield integrity check started"
            );

            if (!File.Exists(launcherPath))
            {
                SecurityLogger.Error(
                    "Launcher executable not found"
                );

                return false;
            }

            if (!VerifyExecutableExtension())
            {
                return false;
            }

            if (!VerifyExecutableReadable())
            {
                return false;
            }

            string hash =
                CalculateSha256(launcherPath);

            if (string.IsNullOrWhiteSpace(hash))
            {
                SecurityLogger.Error(
                    "Launcher SHA-256 calculation failed"
                );

                return false;
            }

            SecurityLogger.Security(
                $"Launcher SHA-256: {hash}"
            );

            SecurityLogger.Security(
                "LauncherShield integrity check passed"
            );

            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"LauncherShield error: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // EXECUTABLE VALIDATION
    // ============================================================

    private bool VerifyExecutableExtension()
    {
        string extension =
            Path.GetExtension(launcherPath);

        if (!extension.Equals(
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            SecurityLogger.Error(
                "Launcher is not an EXE"
            );

            return false;
        }

        return true;
    }


    // ============================================================
    // FILE ACCESS VALIDATION
    // ============================================================

    private bool VerifyExecutableReadable()
    {
        try
        {
            using FileStream stream =
                new FileStream(
                    launcherPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

            return stream.Length > 0;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Launcher file validation failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // SHA-256
    // ============================================================

    public string CalculateSha256(
        string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return string.Empty;

            using SHA256 sha256 =
                SHA256.Create();

            using FileStream stream =
                File.OpenRead(filePath);

            byte[] hash =
                sha256.ComputeHash(stream);

            return Convert.ToHexString(
                hash
            ).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"SHA-256 error: {ex.Message}"
            );

            return string.Empty;
        }
    }


    // ============================================================
    // HASH COMPARISON
    // ============================================================

    public bool VerifySha256(
        string filePath,
        string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(
                expectedHash))
        {
            SecurityLogger.Error(
                "Expected SHA-256 is empty"
            );

            return false;
        }

        string actualHash =
            CalculateSha256(filePath);

        if (string.IsNullOrWhiteSpace(
                actualHash))
        {
            return false;
        }

        bool valid =
            CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(
                    actualHash
                ),
                Convert.FromHexString(
                    expectedHash.Trim()
                )
            );

        if (valid)
        {
            SecurityLogger.Security(
                "Launcher SHA-256 verification passed"
            );
        }
        else
        {
            SecurityLogger.Security(
                "Launcher SHA-256 verification FAILED"
            );
        }

        return valid;
    }


    // ============================================================
    // PATH
    // ============================================================

    public string GetLauncherPath()
    {
        return launcherPath;
    }
}
