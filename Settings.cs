using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using NLog;
using System.IO;
using System.Text;
using System;
using ParamVR.Ws;

namespace ParamVR;

internal class Settings
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static Settings Instance { get; } = new Settings();

    private readonly IConfiguration config;
    public SettingsData SettingsData { get; private set; }

    private Settings()
    {

        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParamVR.Chat");
        var appSettings = Path.Combine(basePath, "ParamVR.Chat-Client.json");
        if (!File.Exists(appSettings))
        {
            logger.Info("Initializing ParamVR.Chat-Client.json");
            var defaultJson = JsonSerializer.Serialize(new SettingsData(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appSettings, defaultJson);
        }

        config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("ParamVR.Chat-Client.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.Configure<SettingsData>(config);
        var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<SettingsData>>();
        monitor.OnChange(OnSettingsChanged);
        SettingsData = monitor.CurrentValue;
    }

    private void OnSettingsChanged(SettingsData settings)
    {
        logger.Info("Settings changed.");
        SettingsData = settings;
        _ = WsController.Instance.Restart();
    }

    public void SetConnectionInfo(string targetUser, string listenKey)
    {
        logger.Info("Setting connection info to {targetUser}:{listenKey}", targetUser, listenKey);
        SettingsData.targetUser = targetUser;
        SettingsData.listenKey = listenKey;
        var json = JsonSerializer.Serialize(SettingsData, new JsonSerializerOptions { WriteIndented = true });
        var appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParamVR.Chat");
        File.WriteAllText(Path.Combine(appdata, "ParamVR.Chat-Client.json"), json);
    }

    public string GetAuthorization()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SettingsData.targetUser}:{SettingsData.listenKey}"));
    }
}

internal class SettingsData
{
    public string host { get; set; } = "paramvr.chat";
    public int port { get; set; } = 443;
    public string targetUser { get; set; } = "";
    public string listenKey { get; set; } = "";
}
