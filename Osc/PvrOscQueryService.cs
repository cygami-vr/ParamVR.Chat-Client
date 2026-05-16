using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using ParamVR.Http;
using VRC.OSCQuery;

namespace ParamVR.Osc;

internal class PvrChatOscQueryService: IDisposable
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static PvrChatOscQueryService Instance { get; private set; } = new();

    public int OscQueryPort { get; private set; }
    public int OscPortOut { get; private set; }

    private readonly ILogger<OSCQueryService> ilogger;
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
                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
                loggingBuilder.AddNLog(config);
            }).BuildServiceProvider();

        var runner = servicesProvider.GetRequiredService<Runner>();
        ilogger = runner.GetLogger();
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
            .WithLogger(ilogger)
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
            while (await CheckForOscServices())
                await Task.Delay(5000);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error listening for OSC & OSCQuery services.");
        }
    }

    private async Task<bool> CheckForOscServices()
    {
        if (OscQueryService == null)
            return true;

        var oscServices = GetVrcServices(OscQueryService.GetOSCServices());
        var oscQueryServices = GetVrcServices(OscQueryService.GetOSCQueryServices());

        if (oscServices.Count > 1 || oscQueryServices.Count > 1)
        {
            var responsiveServices = new HashSet<OSCQueryServiceProfile>();

            foreach (var oscQueryService in oscQueryServices) {
                var responsive = await OscQueryHttpClient.Instance.IsServiceResponsive(oscQueryService.port);
                logger.Info("OSCQuery service {name} responsive = {responsive}", oscQueryService.name, responsive);
                if (responsive)
                    responsiveServices.Add(oscQueryService);
            }
            
            logger.Info("Responsive OSCQuery services = {services}", ToString(responsiveServices));
            if (responsiveServices.Count == 0)
            {
                StartListening();
                return false;
            }
            else
            {
                var oscQueryService = oscQueryServices.First();
                var oscService = oscServices.FirstOrDefault(s => s.name == oscQueryService.name);
                if (oscService == null)
                {
                    logger.Warn("{name} was responsive but no matching OSC service found. Restarting.", oscQueryService.name);
                    StartListening();
                    return false;
                }
                else
                {
                    HandleOscQueryProfile(oscQueryService);
                    HandleOscProfile(oscService);   
                }
            }
        }
        else
        {
            if (oscServices.Count == 1)
                HandleOscProfile(oscServices.First());

            if (oscQueryServices.Count == 1)
                HandleOscQueryProfile(oscQueryServices.First());
        }
        return true;
    }

    private static string ToString(ICollection<OSCQueryServiceProfile> services)
        => "[" + string.Join(", ", services.Select(s => s.name + " on port " + s.port)) + "]";

    private HashSet<OSCQueryServiceProfile> GetVrcServices(HashSet<OSCQueryServiceProfile> services)
    {
        if (services is null || services.Count == 0)
            return [];

        var ret = services
            .Where(s => s?.name?.StartsWith("VRChat-Client") == true)
            .ToHashSet();

        if (ret.Count > 1)
            logger.Warn("More than one VRC {type} service found. Services = {services}", ret.First().serviceType, ToString(services));
        return ret;
    }

    private void HandleOscProfile(OSCQueryServiceProfile profile)
    {
        if (OscPortOut != profile.port)
        {
            logger.Info("VRChat OSC service {name} found on port {port}", profile.name, profile.port);
            OscPortOut = profile.port;
            OscSender.Instance.InitSender(OscPortOut);
        }
    }

    private void HandleOscQueryProfile(OSCQueryServiceProfile profile)
    {
        if (OscQueryPort != profile.port)
        {
            logger.Info("VRChat OSCQuery service {name} found on port {port}", profile.name, profile.port);
            OscQueryPort = profile.port;
            OscQueryHttpClient.Instance.SetPortAsync(OscQueryPort);
        }
    }

    public void Dispose() => OscQueryService?.Dispose();
}
