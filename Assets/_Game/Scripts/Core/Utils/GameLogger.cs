using System;

namespace SpringAutumn.Core.Utils
{
    /// <summary>日志级别。</summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 日志模块前缀，对应 prd 代码规范（[Config] [Runtime] [AI] [Battle] [Save] 等）。
    /// </summary>
    public enum LogModule
    {
        General,
        Config,
        Runtime,
        Engine,
        Economy,
        Population,
        Construction,
        Recruit,
        Army,
        Battle,
        Diplomacy,
        AI,
        Save,
        View
    }

    /// <summary>
    /// 日志输出目标抽象。Core 为纯 C#，不依赖 UnityEngine；
    /// 表现层注册转发到 UnityEngine.Debug 的实现，测试可注册捕获实现。
    /// </summary>
    public interface ILogSink
    {
        void Write(LogLevel level, string message);
    }

    /// <summary>
    /// 默认 sink，输出到标准输出/标准错误，便于纯 C# 环境与测试使用。
    /// </summary>
    public sealed class ConsoleLogSink : ILogSink
    {
        public void Write(LogLevel level, string message)
        {
            if (level == LogLevel.Error)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }

    /// <summary>
    /// 全局日志门面。统一带模块前缀格式：<c>[Module] message</c>。
    /// </summary>
    public static class GameLogger
    {
        private static ILogSink _sink = new ConsoleLogSink();

        /// <summary>设置日志输出目标。传入 null 时回退为控制台 sink。</summary>
        public static void SetSink(ILogSink sink)
        {
            _sink = sink ?? new ConsoleLogSink();
        }

        public static void Log(LogModule module, string message)
        {
            Write(LogLevel.Info, module, message);
        }

        public static void Warn(LogModule module, string message)
        {
            Write(LogLevel.Warning, module, message);
        }

        public static void Error(LogModule module, string message)
        {
            Write(LogLevel.Error, module, message);
        }

        /// <summary>构造带模块前缀的消息文本，便于测试断言与复用。</summary>
        public static string Format(LogModule module, string message)
        {
            return string.Concat("[", module.ToString(), "] ", message);
        }

        private static void Write(LogLevel level, LogModule module, string message)
        {
            _sink.Write(level, Format(module, message));
        }
    }
}
