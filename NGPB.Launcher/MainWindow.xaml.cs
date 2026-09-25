using System;
using System.Threading.Tasks;
using System.Windows;

using NGPB.Launcher.Models;
using NGPB.Launcher.Services;
using NGPB.Launcher.Security;


namespace NGPB.Launcher;


public partial class MainWindow : Window
{


    // ===============================
    // UPDATE SERVICES
    // ===============================

    private readonly PatchDownloader downloader;

    private readonly UpdateService updateService;



    // ===============================
    // SECURITY SERVICES
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
        // MAIN.SECURE CHECK
        // ===============================


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






        // ===============================
        // LOAD APP.SECURE
        // ===============================


        LoadAppConfig();






        // ===============================
        // LOAD SESSION.SECURE
        // ===============================


        LoadSession();







        // ===============================
        // LAUNCHER INTEGRITY
        // ===============================


        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Launcher integrity failed"
            );


            MessageBox.Show(
                "Launcher Modified"
            );


            Application.Current.Shutdown();

            return;


        }






        // ===============================
        // ANTI CHEAT START
        // ===============================


        antiCheat.Initialize();





        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;




        SecurityLogger.Security(
            "NGPB Launcher Ready"
        );


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
                "Config Missing"
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
            "Logout Success"
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
    // UPDATE FLOW
    // ===============================


    private async Task StartUpdate()
    {


        SecurityLogger.Security(
            "Security Scan Before Update"
        );





        // ANTI CHEAT CHECK

        if(!antiCheat.CheckSafe())
        {


            SecurityLogger.Error(
                "Anti Cheat Violation"
            );



            MessageBox.Show(
                "Security violation detected"
            );



            return;


        }






        // INTEGRITY CHECK


        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Integrity Failed"
            );


            return;


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



        SecurityLogger.Security(
            "UPDATE COMPLETE"
        );



        DownloadText.Text =
            "UPDATE COMPLETE";


    }









    // ===============================
    // PAUSE
    // ===============================


    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();



        SecurityLogger.Info(
            "Download Paused"
        );


        DownloadText.Text =
            "Paused";


    }









    // ===============================
    // RESUME
    // ===============================


    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();



        SecurityLogger.Info(
            "Download Resumed"
        );


        DownloadText.Text =
            "Resumed";


    }



}
