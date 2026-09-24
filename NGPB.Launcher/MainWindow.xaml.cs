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



    // SECURITY

    private readonly SessionManager sessionManager;


    private readonly LauncherShield shield;



    // SESSION

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



        sessionManager =
            new SessionManager();



        shield =
            new LauncherShield();




        SecurityLogger.Security(
            "Security Module Loaded"
        );



        // SECURITY CHECK

        if(!shield.RunSecurityCheck())
        {


            SecurityLogger.Error(
                "Security Check Failed"
            );



            MessageBox.Show(
                "Security Check Failed"
            );


            Application.Current.Shutdown();

            return;

        }




        SecurityLogger.Security(
            "Security Check Success"
        );



        // LOAD SESSION

        LoadSession();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;


    }






    // ===================================
    // SESSION LOAD
    // ===================================


    private void LoadSession()
    {


        try
        {


            currentToken =
                sessionManager.LoadSession();



            if(currentToken != null)
            {


                SecurityLogger.Security(
                    "Session Restored"
                );


                DownloadText.Text =
                    "Session Loaded";


            }

            else
            {


                SecurityLogger.Info(
                    "No Session Found"
                );


            }


        }

        catch(Exception ex)
        {


            SecurityLogger.Error(

                "Session Load Error : "
                + ex.Message

            );


        }


    }





    // ===================================
    // LOGIN SUCCESS
    // ===================================


    public void LoginSuccess(
        string jwtToken)
    {


        try
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

        catch(Exception ex)
        {


            SecurityLogger.Error(

                "Login Save Error : "
                + ex.Message

            );


        }


    }







    // ===================================
    // LOGOUT
    // ===================================


    public void Logout()
    {


        sessionManager.ClearSession();



        currentToken = null;



        SecurityLogger.Security(
            "Logout Success"
        );



        DownloadText.Text =
            "Logged Out";


    }







    // ===================================
    // START UPDATE
    // ===================================


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








    private async Task StartUpdate()
    {


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



            DownloadText.Text =
                $"Downloading {file.Name}";





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







    // ===================================
    // PAUSE
    // ===================================


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







    // ===================================
    // RESUME
    // ===================================


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
