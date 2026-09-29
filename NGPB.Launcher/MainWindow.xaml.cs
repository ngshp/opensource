using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

using NGPB.Launcher.Models;
using NGPB.Launcher.Services;
using NGPB.Launcher.Security;


namespace NGPB.Launcher;

public partial class MainWindow : Window
{
    // ============================================================
    // UPDATE
    // ============================================================

    private readonly PatchDownloader downloader;
    private readonly UpdateService updateService;


    // ============================================================
    // SECURITY
    // ============================================================

    private readonly LauncherShield shield;
    private readonly MainSecure mainSecure;
    private readonly AntiCheatService antiCheat;


    // ============================================================
    // CONFIG
    // ============================================================

    private readonly AppConfigService configService;
    private AppConfig? appConfig;


    // ============================================================
    // SESSION
    // ============================================================

    private readonly SessionManager sessionManager;
    private string? currentToken;


    // ============================================================
    // STATE
    // ============================================================

    private bool isUpdating;
    private bool launcherReady;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            LoadingScreen.Visibility =
                Visibility.Visible;

            LoadingScreen.Opacity = 1;

            LoadingText.Text =
                "Starting NGPB Launcher...";


            SecurityLogger.Info(
                "========================================"
            );

            SecurityLogger.Info(
                "NGPB Launcher Starting"
            );

            SecurityLogger.Info(
                "========================================"
            );


            // ----------------------------------------------------
            // VIDEO
            // ----------------------------------------------------

            try
            {
                BackgroundVideo.Play();
            }
            catch (Exception ex)
            {
                SecurityLogger.Error(
                    $"Background video failed: {ex.Message}"
                );
            }


            // ----------------------------------------------------
            // SERVICES
            // ----------------------------------------------------

            downloader =
                new PatchDownloader();

            updateService =
                new UpdateService();


            // ----------------------------------------------------
            // SECURITY SERVICES
            // ----------------------------------------------------

            shield =
                new LauncherShield();

            mainSecure =
                new MainSecure();

            antiCheat =
                new AntiCheatService();


            // ----------------------------------------------------
            // CONFIG + SESSION
            // ----------------------------------------------------

            configService =
                new AppConfigService();

            sessionManager =
                new SessionManager();


            // ----------------------------------------------------
            // MAIN.SECURE
            // ----------------------------------------------------

            LoadingText.Text =
                "Checking Security...";


            if (!mainSecure.Validate())
            {
                SecurityLogger.Error(
                    "MainSecure validation FAILED"
                );

                ShowSecurityError(
                    "Launcher Security Failed.\n\n" +
                    "The secure configuration could not be verified."
                );

                return;
            }


            SecurityLogger.Security(
                "MainSecure validation passed"
            );


            // ----------------------------------------------------
            // LOAD VERIFIED CONFIG
            // ----------------------------------------------------

            LoadingText.Text =
                "Loading Configuration...";


            LoadAppConfig();


            if (appConfig == null)
            {
                return;
            }


            // ----------------------------------------------------
            // SESSION
            // ----------------------------------------------------

            LoadingText.Text =
                "Loading Session...";


            LoadSession();


            // ----------------------------------------------------
            // LAUNCHER INTEGRITY
            // ----------------------------------------------------

            LoadingText.Text =
                "Checking Launcher Integrity...";


            if (!shield.RunSecurityCheck())
            {
                SecurityLogger.Error(
                    "LauncherShield integrity FAILED"
                );

                ShowSecurityError(
                    "Launcher integrity verification failed."
                );

                return;
            }


            SecurityLogger.Security(
                "LauncherShield integrity passed"
            );


            // ----------------------------------------------------
            // ANTI CHEAT INITIALIZATION
            // ----------------------------------------------------

            LoadingText.Text =
                "Starting Anti Cheat...";


            antiCheat.Initialize();


            SecurityLogger.Security(
                "AntiCheat initialized"
            );


            // ----------------------------------------------------
            // UI INITIAL STATE
            // ----------------------------------------------------

            PauseButton.IsEnabled = false;
            ResumeButton.IsEnabled = false;


            Progress.Value = 0;

            DownloadText.Text =
                "Ready";


            launcherReady = true;


            SecurityLogger.Security(
                "NGPB Launcher Ready"
            );


            // ----------------------------------------------------
            // LOADING SCREEN
            // ----------------------------------------------------

            FadeOutLoading();
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Launcher startup exception: {ex}"
            );

            ShowSecurityError(
                "Launcher startup failed."
            );
        }
    }


    // ============================================================
    // APP.SECURE
    // ============================================================

    private void LoadAppConfig()
    {
        try
        {
            appConfig =
                mainSecure.GetConfig();


            if (appConfig == null)
            {
                SecurityLogger.Error(
                    "Verified app.secure configuration unavailable"
                );

                ShowSecurityError(
                    "Configuration verification failed."
                );

                return;
            }


            SecurityLogger.Security(
                "app.secure loaded and verified"
            );


            SecurityLogger.Info(
                $"Launcher Version: {appConfig.LauncherVersion}"
            );

            SecurityLogger.Info(
                $"Game: {appConfig.GameName}"
            );

            SecurityLogger.Info(
                $"Game EXE: {appConfig.GameExe}"
            );
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"app.secure load error: {ex.Message}"
            );

            ShowSecurityError(
                "Configuration Error."
            );
        }
    }


    // ============================================================
    // SESSION.SECURE / AUTO LOGIN
    // ============================================================

    private void LoadSession()
    {
        try
        {
            currentToken =
                sessionManager.LoadSession();


            if (!string.IsNullOrWhiteSpace(
                    currentToken))
            {
                SecurityLogger.Security(
                    "session.secure restored"
                );

                DownloadText.Text =
                    "Session restored";
            }
            else
            {
                SecurityLogger.Info(
                    "No valid session.secure found"
                );

                DownloadText.Text =
                    "Ready";
            }
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Session load error: {ex.Message}"
            );

            currentToken = null;
        }
    }


    // ============================================================
    // LOGIN SUCCESS
    // ============================================================

    public bool LoginSuccess(
        string jwtToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                    jwtToken))
            {
                SecurityLogger.Error(
                    "Login failed: empty JWT"
                );

                return false;
            }


            if (!sessionManager.SaveSession(
                    jwtToken))
            {
                SecurityLogger.Error(
                    "Login failed: session could not be saved"
                );

                return false;
            }


            currentToken =
                jwtToken;


            SecurityLogger.Security(
                "Login Success"
            );


            DownloadText.Text =
                "Logged in";


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Login error: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // LOGOUT
    // ============================================================

    public void Logout()
    {
        try
        {
            sessionManager.ClearSession();

            currentToken = null;


            SecurityLogger.Security(
                "Logout Success"
            );


            DownloadText.Text =
                "Logged out";
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Logout error: {ex.Message}"
            );
        }
    }


    // ============================================================
    // UPDATE BUTTON
    // ============================================================

    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!launcherReady)
        {
            SecurityLogger.Error(
                "Update blocked: launcher not ready"
            );

            return;
        }


        if (isUpdating)
        {
            return;
        }


        isUpdating = true;


        UpdateButton.IsEnabled = false;
        PauseButton.IsEnabled = true;
        ResumeButton.IsEnabled = true;


        try
        {
            await StartUpdate();
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Update exception: {ex}"
            );

            MessageBox.Show(
                ex.Message,
                "NGPB Update Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            isUpdating = false;

            UpdateButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            ResumeButton.IsEnabled = false;
        }
    }


    // ============================================================
    // UPDATE SECURITY FLOW
    // ============================================================

    private async Task StartUpdate()
    {
        // --------------------------------------------------------
        // MAIN.SECURE UPDATE GATE
        // --------------------------------------------------------

        if (!mainSecure.ValidateBeforeUpdate())
        {
            SecurityLogger.Error(
                "Update blocked by MainSecure"
            );

            MessageBox.Show(
                "Security validation failed.",
                "NGPB Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            return;
        }


        // --------------------------------------------------------
        // ANTI CHEAT
        // --------------------------------------------------------

        SecurityLogger.Security(
            "Anti Cheat Scan Before Update"
        );


        if (!antiCheat.CheckSafe())
        {
            SecurityLogger.Error(
                "Anti Cheat violation detected"
            );

            MessageBox.Show(
                "Security violation detected.",
                "NGPB Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            return;
        }


        // --------------------------------------------------------
        // LAUNCHER INTEGRITY
        // --------------------------------------------------------

        if (!shield.RunSecurityCheck())
        {
            SecurityLogger.Error(
                "Launcher integrity failed before update"
            );

            MessageBox.Show(
                "Launcher integrity verification failed.",
                "NGPB Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            return;
        }


        // --------------------------------------------------------
        // CONFIG
        // --------------------------------------------------------

        if (appConfig == null)
        {
            SecurityLogger.Error(
                "Update blocked: appConfig unavailable"
            );

            return;
        }


        // --------------------------------------------------------
        // START UPDATE
        // --------------------------------------------------------

        SecurityLogger.Security(
            "Update security checks passed"
        );


        DownloadText.Text =
            "Checking Update...";


        Progress.Value = 0;


        // --------------------------------------------------------
        // MANIFEST
        // --------------------------------------------------------

        var manifest =
            await updateService.GetManifest();


        if (manifest == null)
        {
            SecurityLogger.Error(
                "Manifest unavailable"
            );

            MessageBox.Show(
                "Manifest tidak tersedia.",
                "NGPB Update",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );

            return;
        }


        // --------------------------------------------------------
        // DOWNLOAD FILES
        // --------------------------------------------------------

        foreach (PatchFile file in manifest.Files)
        {
            if (!isUpdating)
            {
                return;
            }


            SecurityLogger.Info(
                $"Preparing download: {file.Name}"
            );


            DownloadText.Text =
                $"Preparing\n{file.Name}";


            var progress =
                new Progress<DownloadProgressInfo>(
                    p =>
                    {
                        Progress.Value =
                            Math.Clamp(
                                p.Percentage,
                                0,
                                100
                            );


                        DownloadText.Text =
                            $"{p.FileName}\n\n" +
                            $"{p.Percentage:F2}%\n" +
                            $"{p.Speed:F2} MB/s";
                    }
                );


            await downloader.Download(
                file,
                progress
            );


            SecurityLogger.Security(
                $"Download Complete: {file.Name}"
            );
        }


        // --------------------------------------------------------
        // COMPLETE
        // --------------------------------------------------------

        Progress.Value = 100;


        DownloadText.Text =
            "UPDATE COMPLETE";


        SecurityLogger.Security(
            "UPDATE COMPLETE"
        );
    }


    // ============================================================
    // PAUSE
    // ============================================================

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!isUpdating)
        {
            return;
        }


        downloader.Pause();


        DownloadText.Text =
            "Download Paused";


        SecurityLogger.Info(
            "Download Paused"
        );
    }


    // ============================================================
    // RESUME
    // ============================================================

    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!isUpdating)
        {
            return;
        }


        downloader.Resume();


        DownloadText.Text =
            "Download Resumed";


        SecurityLogger.Info(
            "Download Resumed"
        );
    }


    // ============================================================
    // LOADING SCREEN
    // ============================================================

    private void FadeOutLoading()
    {
        DoubleAnimation fade =
            new DoubleAnimation
            {
                From = 1,
                To = 0,

                Duration =
                    new Duration(
                        TimeSpan.FromSeconds(1)
                    )
            };


        fade.Completed +=
            (_, _) =>
            {
                LoadingScreen.Visibility =
                    Visibility.Collapsed;

                LoadingScreen.Opacity = 0;
            };


        LoadingScreen.BeginAnimation(
            OpacityProperty,
            fade
        );
    }


    // ============================================================
    // SECURITY ERROR
    // ============================================================

    private void ShowSecurityError(
        string message)
    {
        try
        {
            MessageBox.Show(
                message,
                "NG PB Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            launcherReady = false;

            Application.Current.Shutdown();
        }
    }


    // ============================================================
    // WINDOW CLOSED
    // ============================================================

    protected override void OnClosed(
        EventArgs e)
    {
        try
        {
            BackgroundVideo.Stop();
        }
        catch
        {
            // Ignore video shutdown errors.
        }


        SecurityLogger.Info(
            "NGPB Launcher Closed"
        );


        base.OnClosed(e);
    }
}


