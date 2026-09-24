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

    private readonly BackupManager backupManager;

    private readonly RollbackService rollbackService;


    private bool isUpdating;



    public MainWindow()
    {

        InitializeComponent();


        downloader = new PatchDownloader();

        updateService = new UpdateService();

        backupManager = new BackupManager();

        rollbackService = new RollbackService();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;

    }




    // =====================================
    // START UPDATE
    // =====================================

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

                "NGPB UPDATE ERROR"

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





    // =====================================
    // UPDATE ENGINE
    // =====================================

    private async Task StartUpdate()
    {


        DownloadText.Text =
            "Checking update...";


        Progress.Value = 0;



        PatchManifest? manifest;



        try
        {

            manifest =
                await updateService.GetManifest();

        }

        catch(Exception ex)
        {

            MessageBox.Show(

                "Tidak bisa connect server\n\n"+
                ex.Message

            );


            return;

        }




        if(manifest == null)
        {

            MessageBox.Show(

                "Manifest tidak ditemukan"

            );


            return;

        }




        foreach(PatchFile file in manifest.Files)
        {


            DownloadText.Text =
                $"Preparing:\n{file.Name}";



            // BACKUP FILE LAMA

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





            // DOWNLOAD PATCH

            await downloader.Download(

                file,

                progress

            );





            // ============================
            // SHA256 VERIFY
            // ============================


            DownloadText.Text =
                $"Checking integrity:\n{file.Name}";



            var verify =
                await SHA256Verifier.Verify(

                    file.Name,

                    file.SHA256

                );





            if(!verify.Success)

            {


                DownloadText.Text =
                    "Verification Failed";



                MessageBox.Show(

                    "SHA256 tidak cocok\n"+
                    "Melakukan rollback",

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





    // =====================================
    // PAUSE
    // =====================================

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();



        DownloadText.Text =
            "Download Paused";


    }





    // =====================================
    // RESUME
    // =====================================

    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();



        DownloadText.Text =
            "Download Resumed";


    }



}
