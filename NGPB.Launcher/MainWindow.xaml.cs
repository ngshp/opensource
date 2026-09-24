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


    // CONFIG

    private readonly AppConfigService configService;

    private AppConfig? appConfig;


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



        configService =
            new AppConfigService();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;



        LoadConfig();



        SecurityLogger.Write(
            "Launcher Started"
        );



        if(!shield.RunSecurityCheck())
        {

            SecurityLogger.Write(
                "Security Check Failed"
            );


            MessageBox.Show(
                "NGPB Security Failed"
            );


            Application.Current.Shutdown();

            return;

        }


        SecurityLogger.Write(
            "Security Check OK"
        );


        LoadSecureSession();

    }





    // ====================================
    // CONFIG APP.SECURE
    // ====================================


    private void LoadConfig()
    {


        appConfig =
            configService.Load();



        if(appConfig == null)
        {

            CreateDefaultConfig();


            appConfig =
                configService.Load();


        }



        SecurityLogger.Write(
            "Config Loaded"
        );


    }





    private void CreateDefaultConfig()
    {


        AppConfig config =
            new AppConfig

            {

                LauncherVersion =
                "1.0.0",


                GameName =
                "NGPB",


                GameExe =
                "NGPB.exe",


                ApiUrl =
                "https://api.ngpb.com",


                PatchUrl =
                "https://patch.ngpb.com",


                Manifest =
                "manifest.json",


                AutoUpdate = true,


                RequireSHA256 = true

            };



        configService.Save(config);



        SecurityLogger.Write(
            "Default Config Created"
        );


    }






    // ====================================
    // UPDATE BUTTON
    // ====================================


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


            SecurityLogger.Write(
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





    // ====================================
    // UPDATE ENGINE
    // ====================================


    private async Task StartUpdate()
    {


        DownloadText.Text =
            "Checking Server...";



        SecurityLogger.Write(
            "Update Started"
        );



        bool serverOK =
            await validator.Validate();



        if(!serverOK)
        {

            SecurityLogger.Write(
                "Server Validation Failed"
            );


            MessageBox.Show(
                "Server validation failed"
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
            $"Preparing {file.Name}";



            SecurityLogger.Write(
                $"Backup {file.Name}"
            );



            backupManager.Backup(
                file.Name
            );





            var progress =
            new Progress<DownloadProgressInfo>(p =>
            {


                Progress.Value =
                    p.Percentage;



                DownloadText.Text =

                $"Downloading\n\n" +

                $"{p.FileName}\n\n" +

                $"{p.Percentage:F2}%\n" +

                $"{p.Speed:F2} MB/s";


            });





            await downloader.Download(

                file,

                progress

            );





            SecurityLogger.Write(
                $"Verify {file.Name}"
            );



            var verify =
            await SHA256Verifier.Verify(

                file.Name,

                file.SHA256

            );





            if(!verify.Success)
            {


                SecurityLogger.Write(
                    "SHA256 Failed"
                );


                rollbackService.Restore(
                    file.Name
                );



                MessageBox.Show(
                    "Patch corrupt, rollback"
                );


                return;

            }





            SecurityLogger.Write(
                $"Verified {file.Name}"
            );


        }





        Progress.Value = 100;



        DownloadText.Text =
            "UPDATE COMPLETE";



        SecurityLogger.Write(
            "Update Complete"
        );


        MessageBox.Show(
            "Patch berhasil"
        );


    }





    // ====================================
    // PAUSE RESUME
    // ====================================


    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();


        SecurityLogger.Write(
            "Download Pause"
        );


        DownloadText.Text =
            "Paused";

    }





    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();


        SecurityLogger.Write(
            "Download Resume"
        );


        DownloadText.Text =
            "Resumed";

    }






    // ====================================
    // SESSION.SECURE
    // ====================================


    public void SaveSecureSession(
        string token)
    {


        byte[] data =
            ConfigProtector.Encrypt(
                token
            );



        File.WriteAllBytes(

            "session.secure",

            data

        );



        SecurityLogger.Write(
            "Session Saved"
        );


    }





    private string? LoadSecureSession()
    {


        if(!File.Exists(
            "session.secure"))
        {

            SecurityLogger.Write(
                "No Session Found"
            );


            return null;

        }




        byte[] data =
            File.ReadAllBytes(
                "session.secure"
            );



        string token =
            ConfigProtector.Decrypt(
                data
            );



        SecurityLogger.Write(
            "Session Loaded"
        );


        return token;


    }


}
