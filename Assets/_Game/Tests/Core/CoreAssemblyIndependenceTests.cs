using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpringAutumn.Core;
using SpringAutumn.Core.Utils;

namespace SpringAutumn.Tests.Core
{
    /// <summary>
    /// 验证 SpringAutumn.Core 程序集为纯 C#，不依赖任何 Unity 程序集（需求 1.3）。
    /// </summary>
    public class CoreAssemblyIndependenceTests
    {
        private static readonly string[] ForbiddenAssemblyPrefixes =
        {
            "UnityEngine",
            "UnityEditor"
        };

        [Test]
        public void CoreAssembly_DoesNotReference_UnityAssemblies()
        {
            Assembly coreAssembly = typeof(CoreAssemblyMarker).Assembly;

            Assert.AreEqual(
                CoreAssemblyMarker.AssemblyName,
                coreAssembly.GetName().Name,
                "定位到的程序集名称应为 SpringAutumn.Core");

            var referenced = coreAssembly.GetReferencedAssemblies();

            var offending = referenced
                .Where(a => ForbiddenAssemblyPrefixes.Any(
                    prefix => a.Name.StartsWith(prefix, System.StringComparison.Ordinal)))
                .Select(a => a.Name)
                .ToArray();

            Assert.IsEmpty(
                offending,
                "SpringAutumn.Core 不应引用 Unity 程序集，但发现: " + string.Join(", ", offending));
        }

        [Test]
        public void GameLogger_Format_AddsModulePrefix()
        {
            string formatted = GameLogger.Format(LogModule.Config, "loaded 89 settlements");
            Assert.AreEqual("[Config] loaded 89 settlements", formatted);
        }

        [Test]
        public void GameLogger_RoutesMessages_ToInstalledSink()
        {
            var sink = new CapturingSink();
            GameLogger.SetSink(sink);
            try
            {
                GameLogger.Log(LogModule.Battle, "resolve");
                GameLogger.Warn(LogModule.AI, "threat");
                GameLogger.Error(LogModule.Save, "corrupt");

                Assert.AreEqual(3, sink.Entries.Count);
                Assert.AreEqual(LogLevel.Info, sink.Entries[0].Level);
                Assert.AreEqual("[Battle] resolve", sink.Entries[0].Message);
                Assert.AreEqual(LogLevel.Warning, sink.Entries[1].Level);
                Assert.AreEqual("[AI] threat", sink.Entries[1].Message);
                Assert.AreEqual(LogLevel.Error, sink.Entries[2].Level);
                Assert.AreEqual("[Save] corrupt", sink.Entries[2].Message);
            }
            finally
            {
                // 还原默认 sink，避免影响其它测试。
                GameLogger.SetSink(null);
            }
        }

        private sealed class CapturingSink : ILogSink
        {
            public readonly System.Collections.Generic.List<(LogLevel Level, string Message)> Entries
                = new System.Collections.Generic.List<(LogLevel, string)>();

            public void Write(LogLevel level, string message)
            {
                Entries.Add((level, message));
            }
        }
    }
}
