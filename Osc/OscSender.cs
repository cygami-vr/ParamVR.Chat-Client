using System;
using BuildSoft.OscCore;
using NLog;

namespace ParamVR.Osc;
internal class OscSender: IDisposable
{
    public static OscSender Instance { get; private set; } = new();

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private OscClient? sender;

    private OscSender() {}

    public void InitSender(int portOut)
    {
        sender?.Dispose();
        logger.Info("Creating OscClient. Out = {portOut}", portOut);
        sender = new OscClient("127.0.0.1", portOut);
    }

    public void Send(string addr, int elem)
    {
        try
        {
            sender?.Send(addr, elem);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error sending OSC message");
        }
    }

    public void Send(string addr, float elem)
    {
        try
        {
            sender?.Send(addr, elem);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error sending OSC message");
        }
    }

    public void Send(string addr, bool elem)
    {
        try
        {
            sender?.Send(addr, elem);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error sending OSC message");
        }
    }

    public void Send(string addr, string elem)
    {
        try
        {
            sender?.Send(addr, elem);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error sending OSC message");
        }
    }

    public void Dispose() => sender?.Dispose();
}
