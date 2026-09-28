using System;
using System.IO;
using NGPB.Launcher.Models;
using NGPB.Launcher.Services;

namespace NGPB.Launcher.Security;

public sealed class MainSecure
{
    private readonly AppConfigService configService;

    private AppConfig? config;

    private bool validated;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainSecure()
    {
        configService =
            new AppConfigService();
    }


    // ============================================================
    // MAIN VALIDATION
    // ============================================================

    public bool Validate()
    {
        try
        {
            SecurityLogger.Security(
                "MainSecure validation started"
            );


            // ----------------------------------------------------
            // LOAD + RSA VERIFY app.secure
            // ----------------------------------------------------

            config =
                configService.Load();


            if (config == null)
            {
                SecurityLogger.Error(
                    "MainSecure: app.secure verification failed"
                );

                validated = false;

                return false;
            }


            // ----------------------------------------------------
            // CONFIG VALIDATION
            // ----------------------------------------------------

            if (!ValidateConfig(config))
            {
                SecurityLogger.Error(
                    "MainSecure: configuration validation failed"
                );

                validated = false;

                return false;
            }


            validated = true;


            SecurityLogger.Security(
                "MainSecure validation SUCCESS"
            );


            return true;
        }
        catch (Exception ex)
        {
            validated = false;

            SecurityLogger.Error(
                $"MainSecure validation exception: {ex.Message}"
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
            SecurityLogger.Error(
                "LauncherVersion missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.GameName))
        {
            SecurityLogger.Error(
                "GameName missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.GameExe))
        {
            SecurityLogger.Error(
                "GameExe missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.ApiUrl))
        {
            SecurityLogger.Error(
                "ApiUrl missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.PatchUrl))
        {
            SecurityLogger.Error(
                "PatchUrl missing"
            );

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                config.Manifest))
        {
            SecurityLogger.Error(
                "Manifest missing"
            );

            return false;
        }


        // --------------------------------------------------------
        // HTTPS
        // --------------------------------------------------------

        if (!IsHttps(
                config.ApiUrl))
        {
            SecurityLogger.Error(
                "ApiUrl is not HTTPS"
            );

            return false;
        }


        if (!IsHttps(
                config.PatchUrl))
        {
            SecurityLogger.Error(
                "PatchUrl is not HTTPS"
            );

            return false;
        }


        // --------------------------------------------------------
        // GAME EXECUTABLE
        // --------------------------------------------------------

        string gameExe =
            config.GameExe.Trim();


        if (Path.IsPathRooted(gameExe))
        {
            SecurityLogger.Error(
                "GameExe must be a relative path"
            );

            return false;
        }


        if (!gameExe.EndsWith(
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            SecurityLogger.Error(
                "GameExe must be an EXE"
            );

            return false;
        }


        return true;
    }


    // ============================================================
    // HTTPS CHECK
    // ============================================================

    private static bool IsHttps(
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
    // VALIDATION STATUS
    // ============================================================

    public bool IsValidated()
    {
        return validated;
    }


    // ============================================================
    // CONFIG ACCESS
    // ============================================================

    public AppConfig? GetConfig()
    {
        if (!validated)
            return null;

        return config;
    }


    // ============================================================
    // UPDATE GATE
    // ============================================================

    public bool ValidateBeforeUpdate()
    {
        if (!validated)
        {
            SecurityLogger.Error(
                "Update blocked: MainSecure not validated"
            );

            return false;
        }


        if (config == null)
        {
            SecurityLogger.Error(
                "Update blocked: configuration unavailable"
            );

            return false;
        }


        SecurityLogger.Security(
            "MainSecure update gate passed"
        );


        return true;
    }


    // ============================================================
    // GAME LAUNCH GATE
    // ============================================================

    public bool ValidateBeforeLaunch()
    {
        if (!validated)
        {
            SecurityLogger.Error(
                "Launch blocked: MainSecure not validated"
            );

            return false;
        }


        if (config == null)
        {
            SecurityLogger.Error(
                "Launch blocked: configuration unavailable"
            );

            return false;
        }


        string gamePath =
            Path.Combine(
                AppContext.BaseDirectory,
                config.GameExe
            );


        if (!File.Exists(gamePath))
        {
            SecurityLogger.Error(
                $"Launch blocked: game not found: {gamePath}"
            );

            return false;
        }


        SecurityLogger.Security(
            "MainSecure launch gate passed"
        );


        return true;
    }
}
