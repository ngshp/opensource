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


    }



    // START UPDATE

    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {

        UpdateButton.IsEnabled = false;


        try
        {

            await StartUpdate();

        }

        catch(Exception ex)
        {

            MessageBox.Show(

                ex.Message,

                "Update Error"

            );

        }


        finally
        {

            UpdateButton.IsEnabled = true;

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
                "Downloading " + file.Name;



            var progress =
                new Progress<DownloadProgressInfo>(p =>
                {


                    Progress.Value =
                        p.Percentage;



                    DownloadText.Text =

                        $"{p.FileName}\n" +

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




    // PAUSE DOWNLOAD

    private void Pause_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Pause();


        DownloadText.Text =
            "Download Paused";

    }




    // RESUME DOWNLOAD

    private void Resume_Click(
        object sender,
        RoutedEventArgs e)
    {

        downloader.Resume();


        DownloadText.Text =
            "Download Resumed";

    }


}
