
using Serilog;
/// <summary>
/// 打印日志
/// </summary>
public partial class Debug : Singleton<Debug>
{
    //日志log
    public ILogger Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss}][{Level}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine("logs", "log.log"), rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss}/{Level}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

    public void Log(string message)
    {
        if (!ServerConfig.IsDebug) return;
        Logger.Debug(message);
    }

    public void Log(string format, params object[] args)
    {
        if (!ServerConfig.IsDebug) return;
        Logger.Debug(string.Format(format, args).ToString());
    }

    public void LogInfo(object message)
    {
        Logger.Information<object>(message.ToString(), message);
    }

    public void LogInfo(string message)
    {
        Logger.Information(message);
    }

    public void LogInfo(string format, params object[] args)
    {
        Logger.Information(string.Format(format, args).ToString());
    }

    public void LogWarn(object message)
    {
        Logger.Warning<object>(message.ToString(), message);
    }

    public void LogWarn(string message)
    {
        Logger.Warning(message);
    }

    public void LogWarn(string format, params object[] args)
    {
        Logger.Warning(string.Format(format, args).ToString());
    }

    public void LogError(object message)
    {
        Logger.Error<object>(message.ToString(), message);
    }

    public void LogError(string message)
    {
        Logger.Error(message);
    }

    public void LogError(string format, params object[] args)
    {
        Logger.Error(string.Format(format, args).ToString());
    }

    public void LogFatal(string message)
    {
        Logger.Fatal(message);
    }

    public void LogFatal(object message)
    {
        Logger.Fatal<object>(message.ToString(), message);
    }

    public void LogFatal(string format, params object[] args)
    {
        Logger.Fatal(string.Format(format, args).ToString());
    }
}