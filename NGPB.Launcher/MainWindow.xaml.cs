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


    // ===============================
    // UPDATE SERVICE
    // ===============================

    private readonly PatchDownloader downloader;

    private readonly UpdateService updateService;



    // ===============================
    // SECURITY
    // ===============================

    private readonly LauncherShield shield;

    private readonly MainSecure mainSecure;

    private readonly AntiCheatService antiCheat;



    // ===============================
    // CONFIG
    // ===============================

    private readonly AppConfigService configService;

    private AppConfig? appConfig;



    // ===============================
    // SESSION
    // ===============================

    private readonly SessionManager sessionManager;

    private string? currentToken;



    private bool isUpdating;







    // ===============================
    // CONSTRUCTOR
    // ===============================


    public MainWindow()
    {


        InitializeComponent();



        // VIDEO START

        BackgroundVideo.Play();



        LoadingText.Text =
            "Starting NGPB Launcher...";



        SecurityLogger.Info(
            "NGPB Launcher Starting"
        );





        downloader =
            new PatchDownloader();



        updateService =
            new UpdateService();






        shield =
            new LauncherShield();



        mainSecure =
            new MainSecure();



        antiCheat =
            new AntiCheatService();







        configService =
            new AppConfigService();




        sessionManager =
            new SessionManager();







        // ===============================
        // MAIN.SECURE
        // ===============================


        LoadingText.Text =
            "Checking Security...";



        if(!mainSecure.Validate())
        {


            SecurityLogger.Error(
                "main.secure failed"
            );


            MessageBox.Show(
                "Launcher Security Failed"
            );


            Application.Current.Shutdown();

            return;


        }









        // ===============================
        // APP.SECURE
        // ===============================


        LoadingText.Text =
            "Loading Configuration...";



        LoadAppConfig();








        // ===============================
        // SESSION
        // ===============================


        LoadingText.Text =
            "Loading Session...";



        LoadSession();







        // ===============================
        // INTEGRITY
        // ===============================


        LoadingText.Text =
            "Checking Launcher Integrity...";



        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Integrity Failed"
            );



            MessageBox.Show(
                "Launcher Modified"
            );



            Application.Current.Shutdown();

            return;


        }








        // ===============================
        // ANTI CHEAT
        // ===============================


        LoadingText.Text =
            "Starting Anti Cheat...";



        antiCheat.Initialize();







        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;





        SecurityLogger.Security(
            "NGPB Launcher Ready"
        );





        FadeOutLoading();



    }










    // ===============================
    // APP.SECURE
    // ===============================


    private void LoadAppConfig()
    {


        appConfig =
            configService.Load();




        if(appConfig == null)
        {


            SecurityLogger.Error(
                "app.secure missing"
            );



            MessageBox.Show(
                "Configuration Error"
            );



            Application.Current.Shutdown();

            return;


        }




        SecurityLogger.Info(
            "app.secure loaded"
        );


    }









    // ===============================
    // SESSION.SECURE
    // ===============================


    private void LoadSession()
    {


        currentToken =
            sessionManager.LoadSession();




        if(currentToken != null)
        {


            SecurityLogger.Security(
                "Session Restored"
            );


        }
        else
        {


            SecurityLogger.Info(
                "No Session"
            );


        }


    }









    // ===============================
    // LOGIN
    // ===============================


    public void LoginSuccess(
        string jwtToken)
    {


        currentToken =
            jwtToken;



        sessionManager.SaveSession(
            jwtToken
        );



        SecurityLogger.Security(
            "Login Success"
        );


    }









    // ===============================
    // LOGOUT
    // ===============================


    public void Logout()
    {


        sessionManager.ClearSession();



        currentToken = null;



        SecurityLogger.Security(
            "Logout"
        );


    }









    // ===============================
    // START UPDATE
    // ===============================


    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {


        if(isUpdating)

            return;



        isUpdating = true;



        UpdateButton.IsEnabled = false;

        PauseButton.IsEnabled = true;

        ResumeButton.IsEnabled = true;





        try
        {


            await StartUpdate();


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(
                ex.Message
            );


            MessageBox.Show(
                ex.Message
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









    // ===============================
    // UPDATE PROCESS
    // ===============================


    private async Task StartUpdate()
    {


        SecurityLogger.Security(
            "Security Scan Before Update"
        );





        if(!antiCheat.CheckSafe())
        {


            SecurityLogger.Error(
                "Anti Cheat Violation"
            );


            MessageBox.Show(
                "Security Violation"
            );


            return;


        }







        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Integrity Failed"
            );


            return;


        }








        DownloadText.Text =
            "Checking Update...";



        var manifest =
            await updateService.GetManifest();






        if(manifest == null)
        {


            MessageBox.Show(
                "Manifest unavailable"
            );


            return;


        }







        foreach(PatchFile file in manifest.Files)
        {



            DownloadText.Text =
                $"Downloading {file.Name}";



            var progress =
            new Progress<DownloadProgressInfo>(
            p =>
            {


                Progress.Value =
                    p.Percentage;



                DownloadText.Text =

                $"{p.FileName}\n\n" +

                $"{p.Percentage:F2}%\n" +

                $"{p.Speed:F2} MB/s";



            });








            await downloader.Download(
                file,
                progress
            );




            SecurityLogger.Security(
                $"Download Complete {file.Name}"
            );


        }







        Progress.Value = 100;



        DownloadText.Text =
            "UPDATE COMPLETE";



        SecurityLogger.Security(
            "UPDATE COMPLETE"
        );


    }









    // ===============================
    // PAUSE
    // ===============================


    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();



        DownloadText.Text =
            "Paused";



        SecurityLogger.Info(
            "Download Paused"
        );


    }









    // ===============================
    // RESUME
    // ===============================


    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();



        DownloadText.Text =
            "Resumed";



        SecurityLogger.Info(
            "Download Resumed"
        );


    }









    // ===============================
    // LOADING ANIMATION
    // ===============================


    private void FadeOutLoading()
    {


        DoubleAnimation fade =
            new DoubleAnimation();



        fade.From = 1;

        fade.To = 0;



        fade.Duration =
            new Duration(
                TimeSpan.FromSeconds(1)
            );





        fade.Completed +=
        (s,e)=>
        {


            LoadingScreen.Visibility =
                Visibility.Collapsed;


        };






        LoadingScreen.BeginAnimation(
            OpacityProperty,
            fade
        );



    }



}
