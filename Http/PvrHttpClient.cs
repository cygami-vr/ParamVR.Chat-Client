using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Http;

internal class PvrHttpClient: IDisposable
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static PvrHttpClient Instance { get; } = new PvrHttpClient();

    private readonly HttpClient client = new();

    public async Task Post(string path, string? body, string? contentType)
    {
        try
        {
            logger.Info("Post {body} to {path}", body, path);
            var settings = Settings.Instance.SettingsData;
            var protocol = settings.host.Equals("127.0.0.1") || settings.host.Equals("localhost") ? "http" : "https";
            var url = $"{protocol}://{settings.host}/{path}";

            HttpContent content = new StringContent(body ?? "", Encoding.UTF8, contentType ?? "text/plain");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Settings.Instance.GetAuthorization());

            var resp = await client.SendAsync(request);
            logger.Info("Status code = {code}", resp.StatusCode);
        }
        catch(Exception ex)
        {
            logger.Error(ex, "http error");
        }
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
