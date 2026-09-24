using System;
using System.Threading.Tasks;
using System.Windows;

using NGPB.Launcher.Models;
using NGPB.Launcher.Services;


namespace NGPB.Launcher;


public partial class MainWindow : Window
{

    private readonly PatchDownloader downloader;

    private readonly UpdateService updateService;


    private bool isUpdating;



    public MainWindow()
    {

        InitializeComponent();


        downloader = new PatchDownloader();


        updateService = new UpdateService();



        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;


    }



    // ==================================================
    // START UPDATE BUTTON
    // ==================================================

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





    // ==================================================
    // UPDATE ENGINE
    // ==================================================

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

                "Tidak dapat mengambil update server\n\n"
                + ex.Message

            );


            return;


        }




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




            DownloadText.Text =

                $"Completed:\n{file.Name}";



        }




        Progress.Value = 100;



        DownloadText.Text =

            "UPDATE COMPLETE";




        MessageBox.Show(

            "Patch berhasil!"

        );



    }





    // ==================================================
    // PAUSE
    // ==================================================

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Pause();



        DownloadText.Text =

            "Download Paused";


    }





    // ==================================================
    // RESUME
    // ==================================================

    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {


        downloader.Resume();



        DownloadText.Text =

            "Download Resumed";


    }



}
