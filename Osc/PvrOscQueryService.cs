using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ParamVR.Http;
using VRC.OSCQuery;

namespace ParamVR.Osc;

internal class PvrChatOscQueryService: IDisposable
{
    public static PvrChatOscQueryService Instance { get; private set; } = new();

    public int OscQueryPort { get; private set; }
    public int OscPortOut { get; private set; }

    private readonly ILogger<OSCQueryService> logger;
    public OSCQueryService? OscQueryService;

    private PvrChatOscQueryService()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        using var servicesProvider = new ServiceCollection()
            .AddTransient<Runner>()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog(config);
            }).BuildServiceProvider();

        var runner = servicesProvider.GetRequiredService<Runner>();
        logger = runner.GetLogger();
    }

    public void StartListening()
    {
        OscQueryService?.Dispose();
        var tcpPort = Extensions.GetAvailableTcpPort();
        var udpPort = OscListener.Instance.StartListening();

        OscQueryService = new OSCQueryServiceBuilder()
            .WithTcpPort(tcpPort)
            .WithUdpPort(udpPort)
            .WithServiceName("ParamVRChat")
            .WithLogger(logger)
            .StartHttpServer()
            .AdvertiseOSCQuery()
            .AdvertiseOSC()
            .Build();

        OscQueryService.AddEndpoint("/avatar", "s", Attributes.AccessValues.WriteOnly);
        _ = ListenForServices();
    }

    private async Task ListenForServices()
    {
        try
        {
            while (CheckForOscServices())
                await Task.Delay(5000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listening for OSC & OSCQuery services.");
        }
    }

    private bool CheckForOscServices()
    {
        if (OscQueryService == null)
            return true;

        var oscServices = GetVrcServices(OscQueryService.GetOSCServices());
        var oscQueryServices = GetVrcServices(OscQueryService.GetOSCQueryServices());

        if (oscServices.Count > 1 || oscQueryServices.Count > 1)
        {
            logger.LogWarning("Too many services. Restarting.");
            Instance.StartListening();
            return false;
        }
        else
        {
            if (oscServices.Count == 1)
            {
                var oscSvc = oscServices.First();
                if (OscPortOut != oscSvc.port)
                {
                    logger.LogInformation("VRChat OSC service {name} found on port {port}", oscSvc.name, oscSvc.port);
                    OscPortOut = oscSvc.port;
                    OscSender.Instance.InitSender(OscPortOut);
                }
            }

            if (oscQueryServices.Count == 1)
            {
                var oscQuerySvc = oscQueryServices.First();
                if (OscQueryPort != oscQuerySvc.port)
                {
                    logger.LogInformation("VRChat OSCQuery service {name} found on port {port}", oscQuerySvc.name, oscQuerySvc.port);
                    OscQueryPort = oscQuerySvc.port;
                    OscQueryHttpClient.Instance.SetPortAsync(OscQueryPort);
                }
            }
        }
        return true;
    }

    private HashSet<OSCQueryServiceProfile> GetVrcServices(HashSet<OSCQueryServiceProfile> services)
    {
        if (services is null || services.Count == 0)
            return [];

        var ret = services
            .Where(s => s?.name?.StartsWith("VRChat-Client") == true)
            .ToHashSet();

        if (ret.Count > 1)
            logger.LogWarning("More than one VRC {type} service found. Services = {services}", ret.First().serviceType, string.Join(", ", ret.Select(s => s.name)));
        return ret;
    }

    public void Dispose() => OscQueryService?.Dispose();
}
