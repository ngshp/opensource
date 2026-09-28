using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NGPB.Launcher.Security;

public sealed class AntiCheatService
{
    private readonly object sync = new();

    private bool initialized;
    private bool violationDetected;

    private readonly HashSet<string> blockedProcesses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Isi dengan nama executable yang memang
            // dilarang oleh policy game kamu.
            //
            // Contoh:
            // "example-cheat.exe"
        };


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize()
    {
        lock (sync)
        {
            if (initialized)
                return;

            initialized = true;
            violationDetected = false;
        }

        SecurityLogger.Security(
            "AntiCheatService initialized"
        );
    }


    // ============================================================
    // PRE-UPDATE / PRE-LAUNCH CHECK
    // ============================================================

    public bool CheckSafe()
    {
        try
        {
            EnsureInitialized();

            SecurityLogger.Security(
                "AntiCheat preflight started"
            );


            if (!CheckLauncherEnvironment())
            {
                ReportViolation(
                    "Launcher environment validation failed"
                );

                return false;
            }


            if (!CheckBlockedProcesses())
            {
                ReportViolation(
                    "Blocked process detected"
                );

                return false;
            }


            SecurityLogger.Security(
                "AntiCheat preflight passed"
            );


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"AntiCheat check failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // ENVIRONMENT
    // ============================================================

    private bool CheckLauncherEnvironment()
    {
        try
        {
            string? processPath =
                Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(
                    processPath))
            {
                SecurityLogger.Error(
                    "Unable to determine launcher process path"
                );

                return false;
            }


            string fullPath =
                Path.GetFullPath(
                    processPath
                );


            if (!File.Exists(fullPath))
            {
                SecurityLogger.Error(
                    "Launcher executable does not exist"
                );

                return false;
            }


            string extension =
                Path.GetExtension(
                    fullPath
                );


            if (!string.Equals(
                    extension,
                    ".exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                SecurityLogger.Error(
                    "Unexpected launcher executable type"
                );

                return false;
            }


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Environment check error: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // PROCESS CHECK
    // ============================================================

    private bool CheckBlockedProcesses()
    {
        if (blockedProcesses.Count == 0)
        {
            return true;
        }


        Process[] processes =
            Process.GetProcesses();


        try
        {
            foreach (Process process in processes)
            {
                string processName;

                try
                {
                    processName =
                        process.ProcessName;
                }
                catch
                {
                    continue;
                }


                if (blockedProcesses.Contains(
                        processName))
                {
                    SecurityLogger.Security(
                        $"Blocked process detected: {processName}"
                    );

                    return false;
                }


                if (blockedProcesses.Contains(
                        processName + ".exe"))
                {
                    SecurityLogger.Security(
                        $"Blocked process detected: {processName}.exe"
                    );

                    return false;
                }
            }
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }


        return true;
    }


    // ============================================================
    // GAME PRE-LAUNCH
    // ============================================================

    public bool CheckBeforeLaunch(
        string gameExecutable)
    {
        try
        {
            EnsureInitialized();


            if (string.IsNullOrWhiteSpace(
                    gameExecutable))
            {
                ReportViolation(
                    "Game executable path is empty"
                );

                return false;
            }


            string fullPath =
                Path.GetFullPath(
                    gameExecutable
                );


            if (!File.Exists(fullPath))
            {
                SecurityLogger.Error(
                    $"Game executable not found: {fullPath}"
                );

                return false;
            }


            if (!string.Equals(
                    Path.GetExtension(fullPath),
                    ".exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                SecurityLogger.Error(
                    "Game target is not an executable"
                );

                return false;
            }


            if (!CheckSafe())
            {
                return false;
            }


            SecurityLogger.Security(
                $"Pre-launch security passed: {fullPath}"
            );


            return true;
        }
        catch (Exception ex)
        {
            SecurityLogger.Error(
                $"Pre-launch check failed: {ex.Message}"
            );

            return false;
        }
    }


    // ============================================================
    // VIOLATION STATE
    // ============================================================

    private void ReportViolation(
        string reason)
    {
        lock (sync)
        {
            violationDetected = true;
        }


        SecurityLogger.Error(
            $"AntiCheat violation: {reason}"
        );
    }


    // ============================================================
    // STATUS
    // ============================================================

    public bool HasViolation()
    {
        lock (sync)
        {
            return violationDetected;
        }
    }


    public void ResetViolation()
    {
        lock (sync)
        {
            violationDetected = false;
        }


        SecurityLogger.Security(
            "AntiCheat violation state reset"
        );
    }


    // ============================================================
    // INITIALIZATION GUARD
    // ============================================================

    private void EnsureInitialized()
    {
        lock (sync)
        {
            if (initialized)
                return;

            initialized = true;
        }
    }
}
