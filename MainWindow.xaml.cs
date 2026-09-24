using System.Windows;


namespace NGPB.Launcher;


public partial class MainWindow : Window
{


public MainWindow()

{

InitializeComponent();

Loaded += MainWindow_Loaded;

}



private void MainWindow_Loaded(
object sender,
RoutedEventArgs e)

{


ServerText.Text =
"● SERVER ONLINE";


DownloadText.Text =
"Launcher Ready";


}



private void PlayButton_Click(
object sender,
RoutedEventArgs e)

{


MessageBox.Show(

"Launching NGPB..."

);


}


}
