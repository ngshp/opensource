using System;
using System.Threading.Tasks;
using System.Windows;

using NGPB.Launcher.Models;
using NGPB.Launcher.Services;
using NGPB.Launcher.Security;


namespace NGPB.Launcher;


public partial class MainWindow : Window
{

    private readonly PatchDownloader downloader;

    private readonly UpdateService updateService;



    // SECURITY SERVICES

    private readonly LauncherShield shield;

    private readonly MainSecure mainSecure;

    private readonly AntiCheatService antiCheat;



    // CONFIG

    private readonly AppConfigService configService;

    private AppConfig? appConfig;



    // SESSION

    private readonly SessionManager sessionManager;

    private string? currentToken;



    private bool isUpdating;




    public MainWindow()
    {

        InitializeComponent();



        SecurityLogger.Info(
            "NGPB Launcher Starting"
        );



        downloader =
            new PatchDownloader();



        updateService =
            new UpdateService();




        // SECURITY INIT


        shield =
            new LauncherShield();



        mainSecure =
            new MainSecure();



        antiCheat =
            new AntiCheatService();





        // CONFIG INIT


        configService =
            new AppConfigService();



        LoadAppConfig();





        // SESSION INIT


        sessionManager =
            new SessionManager();



        LoadSession();





        // MAIN.SECURE


        if(!mainSecure.Validate())
        {

            SecurityLogger.Error(
                "main.secure validation failed"
            );


            MessageBox.Show(
                "Launcher Security Failed"
            );


            Application.Current.Shutdown();

            return;

        }





        // LAUNCHER SHIELD


        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Launcher integrity failed"
            );


            Application.Current.Shutdown();

            return;


        }





        // ANTICHEAT START


        antiCheat.Initialize();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;



        SecurityLogger.Security(
            "Launcher Ready"
        );


    }







    // =====================================
    // APP.SECURE CONFIG
    // =====================================


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
                "Config Error"
            );


            Application.Current.Shutdown();


            return;

        }



        SecurityLogger.Info(
            "app.secure loaded"
        );


    }







    // =====================================
    // SESSION.SECURE
    // =====================================


    private void LoadSession()
    {


        currentToken =
            sessionManager.LoadSession();



        if(currentToken != null)
        {


            SecurityLogger.Security(
                "session.secure restored"
            );


        }

        else
        {


            SecurityLogger.Info(
                "No active session"
            );


        }


    }





    public void LoginSuccess(
        string token)
    {


        currentToken =
            token;



        sessionManager.SaveSession(
            token
        );



        SecurityLogger.Security(
            "Login success"
        );


    }






    public void Logout()
    {


        sessionManager.ClearSession();



        currentToken = null;



        SecurityLogger.Security(
            "Logout success"
        );


    }







    // =====================================
    // UPDATE BUTTON
    // =====================================


    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {


        if(isUpdating)

            return;



        isUpdating = true;



        UpdateButton.IsEnabled=false;

        PauseButton.IsEnabled=true;

        ResumeButton.IsEnabled=true;




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


            isUpdating=false;



            UpdateButton.IsEnabled=true;

            PauseButton.IsEnabled=false;

            ResumeButton.IsEnabled=false;


        }


    }







    // =====================================
    // UPDATE SECURITY FLOW
    // =====================================


    private async Task StartUpdate()
    {


        // ANTI CHEAT CHECK BEFORE UPDATE


        SecurityLogger.Security(
            "Anti Cheat Scan"
        );



        if(!antiCheat.CheckSafe())
        {


            SecurityLogger.Error(
                "Cheat process detected"
            );


            MessageBox.Show(
                "Security violation detected"
            );


            return;


        }






        // SESSION CHECK


        if(currentToken == null)
        {


            SecurityLogger.Warning(
                "No session before update"
            );


        }






        SecurityLogger.Info(
            "Update Started"
        );



        DownloadText.Text =
            "Checking Update...";





        var manifest =
            await updateService.GetManifest();




        if(manifest == null)
        {


            SecurityLogger.Error(
                "Manifest unavailable"
            );


            MessageBox.Show(
                "Manifest tidak tersedia"
            );


            return;

        }





        foreach(PatchFile file in manifest.Files)
        {


            SecurityLogger.Info(

                $"Downloading {file.Name}"

            );



            var progress =
            new Progress<DownloadProgressInfo>(p =>
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



        SecurityLogger.Security(
            "Update Complete"
        );



        DownloadText.Text =
            "UPDATE COMPLETE";


    }






    // =====================================
    // PAUSE
    // =====================================


    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();



        SecurityLogger.Info(
            "Download Paused"
        );


    }






    // =====================================
    // RESUME
    // =====================================


    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();



        SecurityLogger.Info(
            "Download Resumed"
        );


    }


}
