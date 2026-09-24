using System.Text.Json;


namespace NGPB.Launcher.Services;


public class DownloadStateManager
{


private string file =
"download.state";



public void Save(long bytes)

{


File.WriteAllText(

file,

bytes.ToString()

);


}



public long Load()

{


if(!File.Exists(file))

return 0;



return long.Parse(

File.ReadAllText(file)

);


}



public void Clear()

{


if(File.Exists(file))

File.Delete(file);


}


}
