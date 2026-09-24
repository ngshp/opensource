using NGPB.Launcher.Models;
using NGPB.Launcher.Services;
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

}
