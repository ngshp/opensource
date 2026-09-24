namespace NGPB.Launcher.Models;


public class VerificationResult
{

    public bool Success { get; set; }


    public string ExpectedHash { get; set; } = "";


    public string ActualHash { get; set; } = "";


    public string Message { get; set; } = "";

}
