using System.Text.Json;
using NGPB.Launcher.Models;


namespace NGPB.Launcher.Services;


public static class SessionManager
{


private static readonly string FileName =

"session.json";



public static void Save()

{


var data = new

{

UserSession.Username,

UserSession.Token

};



File.WriteAllText(

FileName,

JsonSerializer.Serialize(data)

);


}



public static void Load()

{


if(!File.Exists(FileName))

return;



var json =
File.ReadAllText(FileName);



var data =
JsonSerializer.Deserialize<SessionData>(json);



if(data == null)

return;



UserSession.Username =
data.Username;



UserSession.Token =
data.Token;


}



public static void Logout()

{


UserSession.Username="";

UserSession.Token="";


if(File.Exists(FileName))

File.Delete(FileName);


}



private class SessionData

{

public string Username {get;set;}="";

public string Token {get;set;}="";

}


}
