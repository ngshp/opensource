using System.Windows;
using NGPB.Launcher.Services;


namespace NGPB.Launcher;


public partial class LoginWindow : Window
{


public LoginWindow()

{

InitializeComponent();

}



private async void Login_Click(

object sender,

RoutedEventArgs e)

{


AuthService auth =
new();



bool success =
await auth.Login(

UsernameBox.Text,

PasswordBox.Password

);



if(success)

{


MainWindow main =
new();


main.Show();


Close();


}

else

{


MessageBox.Show(

"Login Failed"

);


}


}



}
