using System;
using System.IO;
using System.Security.Cryptography;

namespace NGPB.Launcher.Security;

public sealed class LauncherShield
{
    private readonly string launcherDirectory;
    private readonly string launcherExecutable;

    public LauncherShield()
    {
        launcherDirectory =
            Path.GetFullPath(AppContext.BaseDirectory);

        launcherExecutable =
            Environment.ProcessPath
            ?? Path.Combine(
                launcherDirectory,
                "NG PB Launcher.exe"
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
                "LauncherShield: integrity check started"
            );


            if (!IsPathInsideLauncherDirectory(
                    launcherExecutable))
            {
                SecurityLogger.Error(
                    "LauncherShield: invalid launcher path"
                );

                return false;
            }


            if (!File.Exists(launcherExecutable))
            {
                SecurityLogger.Error(
                    $"LauncherShield: launcher executable missing: " +
                    $"{launcherExecutable}"
                );

                return false;
            }


            string actualHash =
                CalculateSha256(
                    launcherExecutable
                );


            SecurityLogger.Info(
                $"Launcher SHA-256: {actualHash}"
            );


            /*
             * IMPORTANT:
             *
             * Jangan menganggap hash yang baru dihitung
             * sebagai expected hash.
             *
             * Expected hash harus berasal dari trusted
             * release metadata / signed configuration.
             */

            string? expectedHash =
                GetExpectedLauncherHash();


            if (string.IsNullOrWhiteSpace(
                    expectedHash))
            {
                SecurityLogger.Error(
                    "LauncherShield: expected launcher hash unavailable"
                );

                return false;
            }


            expectedHash =
                NormalizeHash(
                    expectedHash
                );


            bool valid =
                CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(actualHash),
                    Convert.FromHexString(expectedHash)
                );


            if (!valid)
            {
                SecurityLogger.Error(
                    "LauncherShield: HASH MISMATCH"
                );

                SecurityLogger.Security(
                    "Launcher integrity validation FAILED"
                );

                return false;
            }


            SecurityLogger.Security(
                "Launcher integrity validation PASSED"
            );


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"LauncherShield exception: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // VERIFY ARBITRARY TRUSTED FILE
    // ============================================================

    public bool VerifyFile(
        string relativePath,
        string expectedSha256)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    relativePath))
            {
                SecurityLogger.Error(
                    "LauncherShield: empty file path"
                );

                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    expectedSha256))
            {
                SecurityLogger.Error(
                    "LauncherShield: empty expected hash"
                );

                return false;
            }


            string fullPath =
                GetSafePath(
                    relativePath
                );


            if (!File.Exists(fullPath))
            {
                SecurityLogger.Error(
                    $"LauncherShield: file not found: {relativePath}"
                );

                return false;
            }


            string actualHash =
                CalculateSha256(
                    fullPath
                );


            string normalizedExpected =
                NormalizeHash(
                    expectedSha256
                );


            bool valid =
                CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(actualHash),
                    Convert.FromHexString(
                        normalizedExpected
                    )
                );


            SecurityLogger.Security(
                $"Integrity {relativePath}: " +
                $"{(valid ? "VALID" : "INVALID")}"
            );


            return valid;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"VerifyFile failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // SHA-256
    // ============================================================

    private static string CalculateSha256(
        string filePath)
    {
        using FileStream stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 128,
                FileOptions.SequentialScan
            );


        using SHA256 sha256 =
            SHA256.Create();


        byte[] hash =
            sha256.ComputeHash(
                stream
            );


        return Convert.ToHexString(
            hash
        );
    }


    // ============================================================
    // EXPECTED HASH
    // ============================================================

    private static string? GetExpectedLauncherHash()
    {
        /*
         * DEVELOPMENT PLACEHOLDER.
         *
         * Untuk production, jangan hardcode hash di sini
         * jika tujuanmu adalah rotasi release.
         *
         * Hash sebaiknya berasal dari signed release metadata
         * yang sudah diverifikasi RSA oleh MainSecure.
         */

        const string expectedHash =
            "";

        if (string.IsNullOrWhiteSpace(
                expectedHash))
        {
            return null;
        }


        return expectedHash;
    }


    // ============================================================
    // SAFE PATH
    // ============================================================

    private string GetSafePath(
        string relativePath)
    {
        string fullPath =
            Path.GetFullPath(
                Path.Combine(
                    launcherDirectory,
                    relativePath
                )
            );


        if (!IsPathInsideLauncherDirectory(
                fullPath))
        {
            throw new UnauthorizedAccessException(
                "Path escapes launcher directory."
            );
        }


        return fullPath;
    }


    // ============================================================
    // DIRECTORY CONTAINMENT
    // ============================================================

    private bool IsPathInsideLauncherDirectory(
        string path)
    {
        string root =
            Path.GetFullPath(
                launcherDirectory
            );


        string candidate =
            Path.GetFullPath(
                path
            );


        if (!root.EndsWith(
                Path.DirectorySeparatorChar))
        {
            root +=
                Path.DirectorySeparatorChar;
        }


        return candidate.StartsWith(
            root,
            StringComparison.OrdinalIgnoreCase
        );
    }


    // ============================================================
    // NORMALIZE SHA-256
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
                "SHA-256 must contain 64 hexadecimal characters."
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


        return normalized;
    }
}
