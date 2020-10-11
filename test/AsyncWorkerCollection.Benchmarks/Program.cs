using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace AsyncWorkerCollection.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            var config = GetConfig(args);
            switcher.Run(new[] {"--filter", "*"}, config);
        }

        private static IConfig GetConfig(string[] args)
        {
            var config = new CustomConfig();

            if (args.Length > 0)
            {
                return config.WithArtifactsPath(args[0]);
            }
            else
            {
                return config;
            }
        }

        private class CustomConfig : ManualConfig
        {
            public CustomConfig()
            {
                // Diagnosers
                Add(MemoryDiagnoser.Default);

                // Columns
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());

                // Loggers
                Add(ConsoleLogger.Default);

                // Exporters
                Add(MarkdownExporter.GitHub);

                Add(Job.InProcess);
            }
        }
    }
}