namespace NGPB.Launcher.Security;


public class LauncherShield
{


public bool RunSecurityCheck()

{


SecurityLogger.Write(
"Starting security check"
);



if(!IntegrityMonitor.VerifyLauncher())

{

SecurityLogger.Write(
"Launcher integrity failed"
);


return false;

}



SecurityLogger.Write(
"Security check OK"
);



return true;


}


}
