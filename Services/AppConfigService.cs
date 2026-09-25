using System;
using System.IO;
using System.Text.Json;
using NGPB.Launcher.Models;

namespace NGPB.Launcher.Services;

public class AppConfigService
{
    private readonly string configFile;

    private readonly JsonSerializerOptions jsonOptions =
        new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

    public AppConfigService()
    {
        string appDirectory =
            AppContext.BaseDirectory;

        configFile =
            Path.Combine(
                appDirectory,
                "app.secure"
            );
    }

    public AppConfig? Load()
    {
        try
        {
            if (!File.Exists(configFile))
            {
                return null;
            }

            string json =
                File.ReadAllText(configFile);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<AppConfig>(
                json,
                jsonOptions
            );
        }
        catch
        {
            return null;
        }
    }

    public bool Save(AppConfig config)
    {
        try
        {
            string json =
                JsonSerializer.Serialize(
                    config,
                    jsonOptions
                );

            string tempFile =
                configFile + ".tmp";

            File.WriteAllText(
                tempFile,
                json
            );

            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }

            File.Move(
                tempFile,
                configFile
            );

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetPath()
    {
        return configFile;
    }
}
