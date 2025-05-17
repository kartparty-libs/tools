
using Serilog;
using System.Collections.Concurrent;

/// <summary>
/// Log类型
/// </summary>
public enum LogType
{
    system,
    sql
}

/// <summary>
/// 打印日志 扩展
/// </summary>
public partial class Debug : Singleton<Debug>
{
    //日志log 扩展
    public ConcurrentDictionary<LogType, ILogger> LoggerExtends = new ConcurrentDictionary<LogType, ILogger>();

    public ILogger GetLoggerExtend(LogType logType)
    {
        return LoggerExtends.GetOrAdd(logType, (key) =>
        {
            ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine("logs", logType.ToString(), $"{logType.ToString()}.log"), rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss}/{Level}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            return logger;
        });
    }

    public void Log(string message, LogType logType)
    {
        GetLoggerExtend(logType).Debug(message);
    }

    public void Log(string format, LogType logType, params object[] args)
    {
        GetLoggerExtend(logType).Debug(string.Format(format, args).ToString());
    }

    public void LogInfo(object message, LogType logType)
    {
        GetLoggerExtend(logType).Information<object>(message.ToString(), message);
    }

    public void LogInfo(string message, LogType logType)
    {
        Logger.Information(message);
    }

    public void LogInfo(string format, LogType logType, params object[] args)
    {
        GetLoggerExtend(logType).Information(string.Format(format, args).ToString());
    }

    public void LogWarn(object message, LogType logType)
    {
        Logger.Warning<object>(message.ToString(), message);
    }

    public void LogWarn(string message, LogType logType)
    {
        GetLoggerExtend(logType).Warning(message);
    }

    public void LogWarn(string format, LogType logType, params object[] args)
    {
        GetLoggerExtend(logType).Warning(string.Format(format, args).ToString());
    }

    public void LogError(object message, LogType logType)
    {
        GetLoggerExtend(logType).Error<object>(message.ToString(), message);
    }

    public void LogError(string message, LogType logType)
    {
        GetLoggerExtend(logType).Error(message);
    }

    public void LogError(string format, LogType logType, params object[] args)
    {
        GetLoggerExtend(logType).Error(string.Format(format, args).ToString());
    }

    public void LogFatal(string message, LogType logType)
    {
        GetLoggerExtend(logType).Fatal(message);
    }

    public void LogFatal(object message, LogType logType)
    {
        GetLoggerExtend(logType).Fatal<object>(message.ToString(), message);
    }

    public void LogFatal(string format, LogType logType, params object[] args)
    {
        GetLoggerExtend(logType).Fatal(string.Format(format, args).ToString());
    }
}