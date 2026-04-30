using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using ParamVR.Ws;

namespace ParamVR.Http;

internal class OscQueryHttpClient: IDisposable
{
    public static OscQueryHttpClient Instance { get; private set; } = new();

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient client = new();
    public int Port { get; private set; } = -1;

    private OscQueryHttpClient() {}

    private async Task<AvatarData?> GetAvatarData()
    {
        if (Port == -1)
            return null;
        try {
            var resp = await client.GetAsync($"http://127.0.0.1:{Port}/avatar");
            logger.Info("Status code = {status}", resp.StatusCode);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                // Try again in five seconds.
                await Task.Delay(5000);
                return await GetAvatarData();
            }
            else
            {
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AvatarData>(json);
            }
        } catch (Exception ex) {
            logger.Error(ex, "http error");
            return null;
        }
    }

    public async Task<string?> GetAvatarId()
    {
        var data = await GetAvatarData();
        if (data == null)
            return null;
        return data.CONTENTS.change.VALUE[0];
    }

    public void SetPortAsync(int port)
    {
        _ = SetPort(port);
    }

    public async Task SetPort(int port)
    {
        Port = port;
        WsSender.Instance.Enqueue("/avatar/change", await GetAvatarId() ?? "");
    }

    public void Dispose() => client.Dispose();
}

internal class AvatarData
{
    public AvatarContent CONTENTS { get; set; }
}

internal class AvatarContent
{
    public AvatarChange change { get; set; }
    public AvatarParameters parameters { get; set; }
}

internal class AvatarChange
{
    public string[] VALUE {  get; set; }
}

internal class AvatarParameters
{
    public Dictionary<string, Parameter> CONTENTS { get; set; }
}

internal class Parameter
{
    public JsonElement[] VALUE { get; set; }
}
