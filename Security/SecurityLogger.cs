namespace NGPB.Launcher.Security;


public static class SecurityLogger
{


private static string path =
"Logs/security.log";



public static void Write(
string message)

{


Directory.CreateDirectory(
"Logs"
);



File.AppendAllText(

path,

DateTime.Now+

" | "+

message+

Environment.NewLine

);


}


}
