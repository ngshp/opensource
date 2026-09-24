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



    public MainWindow()
    {
        InitializeComponent();


        downloader = new PatchDownloader();

        updateService = new UpdateService();


        PauseButton.IsEnabled = false;

        ResumeButton.IsEnabled = false;

    }



    // =========================
    // START UPDATE
    // =========================

    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {

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

            UpdateButton.IsEnabled = true;

            PauseButton.IsEnabled = false;

            ResumeButton.IsEnabled = false;

        }

    }




    private async Task StartUpdate()
    {


        DownloadText.Text =
            "Checking update...";



        var manifest =
            await updateService.GetManifest();



        if(manifest == null)
        {

            MessageBox.Show(
                "Manifest tidak tersedia");


            return;

        }



        foreach(var file in manifest.Files)
        {


            DownloadText.Text =
                $"Preparing {file.Name}";



            var progress =
                new Progress<DownloadProgressInfo>(p =>
                {


                    Progress.Value =
                        p.Percentage;



                    DownloadText.Text =

                        $"Downloading:\n" +

                        $"{p.FileName}\n\n" +

                        $"{p.Percentage:F2}%\n" +

                        $"{p.Speed:F2} MB/s";


                });



            await downloader.Download(

                file,

                progress

            );


        }



        Progress.Value = 100;



        DownloadText.Text =
            "UPDATE COMPLETE";



        MessageBox.Show(

            "Patch berhasil!"

        );


    }




    // =========================
    // PAUSE
    // =========================

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Pause();


        DownloadText.Text =
            "Download Paused";

    }




    // =========================
    // RESUME
    // =========================

    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Resume();


        DownloadText.Text =
            "Download Resumed";

    }


}
