using NGPB.Launcher.Models;


namespace NGPB.Launcher.Services;


public class PatchDownloader
{


private bool paused;


public void Pause()

{

paused=true;

}



public void Resume()

{

paused=false;

}



public async Task Download(

PatchFile patch,

IProgress<DownloadProgressInfo> progress)

{


using HttpClient client =
new();



string temp =
patch.Name+".tmp";



long existing=0;



if(File.Exists(temp))

{

existing =
new FileInfo(temp).Length;

}



var request =
new HttpRequestMessage(

HttpMethod.Get,

patch.Url

);



if(existing>0)

{

request.Headers.Range =
new System.Net.Http.Headers.RangeHeaderValue(

existing,

null

);

}



var response =
await client.SendAsync(

request,

HttpCompletionOption.ResponseHeadersRead

);



using var stream =
await response.Content
.ReadAsStreamAsync();



using var output =
new FileStream(

temp,

FileMode.Append

);



byte[] buffer =
new byte[81920];



long total =
patch.Size;



long current =
existing;



DateTime start =
DateTime.Now;



while(true)

{


while(paused)

{

await Task.Delay(300);

}



int read =
await stream.ReadAsync(buffer);



if(read<=0)

break;



await output.WriteAsync(

buffer.AsMemory(0,read)

);



current += read;



double speed =
current /

1024d /

1024d /

(DateTime.Now-start)
.TotalSeconds;



progress.Report(

new DownloadProgressInfo

{

FileName=patch.Name,

CurrentBytes=current,

TotalBytes=total,

Percentage=

(double)current/total*100,

Speed=speed

}

);


}



File.Move(

temp,

patch.Name,

true

);


}



}
