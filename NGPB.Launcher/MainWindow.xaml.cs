using System;
using System.IO;
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

    private readonly BackupManager backupManager;

    private readonly RollbackService rollbackService;



    // SECURITY

    private readonly LauncherShield shield;

    private readonly ClientValidator validator;



    private bool isUpdating;



    public MainWindow()
    {

        InitializeComponent();



        downloader =
            new PatchDownloader();



        updateService =
            new UpdateService();



        backupManager =
            new BackupManager();



        rollbackService =
            new RollbackService();



        shield =
            new LauncherShield();



        validator =
            new ClientValidator();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;



        // =========================
        // SECURITY STARTUP CHECK
        // =========================


        if(!shield.RunSecurityCheck())
        {

            MessageBox.Show(

                "NGPB Security Check Failed",

                "Security"

            );


            Application.Current.Shutdown();

            return;

        }



        // LOAD SESSION

        LoadSecureSession();


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

            MessageBox.Show(

                ex.Message,

                "NGPB Update Error"

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





    // ===================================
    // UPDATE ENGINE
    // ===================================


    private async Task StartUpdate()
    {


        // SERVER VALIDATION


        DownloadText.Text =
            "Checking security...";



        bool serverOK =
            await validator.Validate();



        if(!serverOK)
        {

            MessageBox.Show(

                "Server validation failed",

                "NGPB Security"

            );


            return;

        }





        DownloadText.Text =
            "Checking update...";



        Progress.Value = 0;



        PatchManifest? manifest;



        manifest =
            await updateService.GetManifest();



        if(manifest == null)
        {

            MessageBox.Show(

                "Manifest tidak tersedia"

            );


            return;

        }





        foreach(PatchFile file in manifest.Files)
        {


            DownloadText.Text =
                $"Preparing:\n{file.Name}";



            // BACKUP OLD FILE


            backupManager.Backup(

                file.Name

            );





            var progress =
                new Progress<DownloadProgressInfo>(p =>
                {


                    Progress.Value =
                        p.Percentage;



                    DownloadText.Text =


                        $"Downloading\n\n"+

                        $"{p.FileName}\n\n"+

                        $"{p.Percentage:F2}%\n"+

                        $"{p.Speed:F2} MB/s";


                });





            // DOWNLOAD


            await downloader.Download(

                file,

                progress

            );






            // SHA256 CHECK


            DownloadText.Text =
                $"Verifying:\n{file.Name}";




            var verify =
                await SHA256Verifier.Verify(

                    file.Name,

                    file.SHA256

                );




            if(!verify.Success)

            {


                MessageBox.Show(

                    "SHA256 Failed\nRollback",

                    "NGPB Security"

                );



                rollbackService.Restore(

                    file.Name

                );



                return;


            }




            DownloadText.Text =
                $"Verified ✅\n{file.Name}";


        }




        Progress.Value = 100;



        DownloadText.Text =
            "UPDATE COMPLETE";



        MessageBox.Show(

            "Patch berhasil!"

        );


    }





    // ===================================
    // PAUSE
    // ===================================


    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Pause();



        DownloadText.Text =
            "Download Paused";


    }





    // ===================================
    // RESUME
    // ===================================


    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Resume();



        DownloadText.Text =
            "Download Resumed";


    }





    // ===================================
    // SECURE SESSION SAVE
    // ===================================


    public void SaveSecureSession(
        string token)
    {


        byte[] encrypted =
            ConfigProtector.Encrypt(

                token

            );



        File.WriteAllBytes(

            "session.secure",

            encrypted

        );


    }





    // ===================================
    // SECURE SESSION LOAD
    // ===================================


    private string? LoadSecureSession()
    {


        if(!File.Exists(

            "session.secure"

        ))

            return null;



        byte[] data =
            File.ReadAllBytes(

                "session.secure"

            );



        string token =
            ConfigProtector.Decrypt(

                data

            );



        return token;


    }



}
