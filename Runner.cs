using Microsoft.Extensions.Logging;
using VRC.OSCQuery;

namespace ParamVR;

internal class Runner(ILogger<OSCQueryService> logger)
{
    private readonly ILogger<OSCQueryService> _logger = logger;

    public ILogger<OSCQueryService> GetLogger()
    {
        return _logger;
    }
}