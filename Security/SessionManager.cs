using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NGPB.Launcher.Security;

public sealed class SessionManager
{
    private const string SessionFileName = "session.secure";

    private readonly string sessionDirectory;
    private readonly string sessionPath;

    private static readonly byte[] Entropy =
        Encoding.UTF8.GetBytes(
            "NGPB-Launcher-Session-v1"
        );


    public SessionManager()
    {
        sessionDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "NGPB",
                "Launcher"
            );

        sessionPath =
            Path.Combine(
                sessionDirectory,
                SessionFileName
            );
    }


    // ============================================================
    // SAVE SESSION
    // ============================================================

    public bool SaveSession(
        string jwtToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                SecurityLogger.Error(
                    "SessionManager: empty JWT"
                );

                return false;
            }


            Directory.CreateDirectory(
                sessionDirectory
            );


            SessionData session =
                new SessionData
                {
                    Token = jwtToken,

                    CreatedUtc =
                        DateTimeOffset.UtcNow,

                    ExpiresUtc =
                        GetJwtExpiration(jwtToken)
                        ?? DateTimeOffset.UtcNow.AddDays(7)
                };


            string json =
                JsonSerializer.Serialize(
                    session
                );


            byte[] plainBytes =
                Encoding.UTF8.GetBytes(
                    json
                );


            byte[] encryptedBytes =
                ProtectedData.Protect(
                    plainBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser
                );


            string encoded =
                Convert.ToBase64String(
                    encryptedBytes
                );


            string tempPath =
                sessionPath + ".tmp";


            File.WriteAllText(
                tempPath,
                encoded,
                Encoding.UTF8
            );


            if (File.Exists(sessionPath))
            {
                File.Delete(sessionPath);
            }


            File.Move(
                tempPath,
                sessionPath
            );


            SecurityLogger.Security(
                "session.secure saved"
            );


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Session save failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // LOAD SESSION
    // ============================================================

    public string? LoadSession()
    {
        try
        {
            if (!File.Exists(sessionPath))
            {
                SecurityLogger.Info(
                    "session.secure not found"
                );

                return null;
            }


            string encoded =
                File.ReadAllText(
                    sessionPath,
                    Encoding.UTF8
                );


            if (string.IsNullOrWhiteSpace(encoded))
            {
                ClearSession();

                return null;
            }


            byte[] encryptedBytes =
                Convert.FromBase64String(
                    encoded
                );


            byte[] plainBytes =
                ProtectedData.Unprotect(
                    encryptedBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser
                );


            string json =
                Encoding.UTF8.GetString(
                    plainBytes
                );


            SessionData? session =
                JsonSerializer.Deserialize<SessionData>(
                    json
                );


            if (session == null ||
                string.IsNullOrWhiteSpace(session.Token))
            {
                SecurityLogger.Error(
                    "Invalid session.secure"
                );

                ClearSession();

                return null;
            }


            if (session.ExpiresUtc <=
                DateTimeOffset.UtcNow)
            {
                SecurityLogger.Security(
                    "session.secure expired"
                );

                ClearSession();

                return null;
            }


            SecurityLogger.Security(
                "session.secure restored"
            );


            return session.Token;
        }
        catch (CryptographicException)
        {
            SecurityLogger.Error(
                "session.secure DPAPI validation failed"
            );

            ClearSession();

            return null;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Session load failed: {ex.Message}"
            );

            ClearSession();

            return null;
        }
    }


    // ============================================================
    // CLEAR SESSION
    // ============================================================

    public void ClearSession()
    {
        try
        {
            if (File.Exists(sessionPath))
            {
                File.Delete(
                    sessionPath
                );
            }


            SecurityLogger.Security(
                "session.secure cleared"
            );
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Session clear failed: {ex.Message}"
            );
        }
    }


    // ============================================================
    // SESSION EXISTS
    // ============================================================

    public bool HasValidSession()
    {
        return !string.IsNullOrWhiteSpace(
            LoadSession()
        );
    }


    // ============================================================
    // SESSION PATH
    // ============================================================

    public string GetSessionPath()
    {
        return sessionPath;
    }


    // ============================================================
    // JWT EXPIRATION
    // ============================================================

    private static DateTimeOffset? GetJwtExpiration(
        string jwt)
    {
        try
        {
            string[] parts =
                jwt.Split('.');

            if (parts.Length != 3)
                return null;


            string payload =
                parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');


            while (payload.Length % 4 != 0)
            {
                payload += "=";
            }


            byte[] bytes =
                Convert.FromBase64String(
                    payload
                );


            using JsonDocument document =
                JsonDocument.Parse(bytes);


            if (!document.RootElement.TryGetProperty(
                    "exp",
                    out JsonElement expElement))
            {
                return null;
            }


            if (!expElement.TryGetInt64(
                    out long exp))
            {
                return null;
            }


            return DateTimeOffset.FromUnixTimeSeconds(
                exp
            );
        }
        catch
        {
            return null;
        }
    }


    // ============================================================
    // SESSION MODEL
    // ============================================================

    private sealed class SessionData
    {
        public string Token { get; set; } = "";

        public DateTimeOffset CreatedUtc { get; set; }

        public DateTimeOffset ExpiresUtc { get; set; }
    }
}
