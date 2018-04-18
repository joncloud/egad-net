using Newtonsoft.Json;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Egad.UnitTests
{
    static class NetPlatformTester
    {
        static readonly JsonSerializerSettings _settings = new JsonSerializerSettings().UseEgad();

        static ProcessStartInfo CreateProcessStartInfo(string command, bool redirectStandardOutput = false, bool redirectStandardInput = false)
        {
            var unitTestAssembly = new FileInfo(
                new Uri(typeof(NetPlatformTester).Assembly.GetName().CodeBase).LocalPath
            );

            var configuration = unitTestAssembly.Directory.Parent.Name;

            var unitTestDirectory = new FileInfo(
                new Uri(typeof(NetPlatformTester).Assembly.GetName().CodeBase).LocalPath
            ).Directory;

            return new ProcessStartInfo(
                "dotnet",
                $"run {command}"
            )
            {
                RedirectStandardInput = redirectStandardInput,
                RedirectStandardOutput = redirectStandardOutput,
                WorkingDirectory = new DirectoryInfo(
                    Path.Combine(
                        unitTestAssembly.Directory.Parent.Parent.Parent.Parent.FullName,
                        "Egad.NetFrameworkPlat"
                    )
                ).FullName
            };
        }

        public static DataSet Passthrough(DataSet dataSet)
        {
            var processStartInfo = CreateProcessStartInfo(
                "passthrough",
                redirectStandardOutput: true,
                redirectStandardInput: true
            );

            using (var process = Process.Start(processStartInfo))
            {
                string json = JsonConvert.SerializeObject(
                    dataSet,
                    _settings
                );
                process.StandardInput.WriteLine(json);

                json = process.StandardOutput.ReadToEnd();
                process.WaitForExit(200);

                Console.WriteLine(json);

                if (process.ExitCode == 0)
                    return JsonConvert.DeserializeObject<DataSet>(json, _settings);

                throw new InvalidOperationException($"Unexpected exit code {process.ExitCode}\r\n{json}");
            }
        }

        public static DataSet Generate()
        {
            var processStartInfo = CreateProcessStartInfo(
                "generate", 
                redirectStandardOutput: true
            );

            using (var process = Process.Start(processStartInfo))
            {
                string json = process.StandardOutput.ReadToEnd();
                process.WaitForExit(200);

                Console.WriteLine(json);

                if (process.ExitCode == 0)
                    return JsonConvert.DeserializeObject<DataSet>(json, _settings);

                throw new InvalidOperationException($"Unexpected exit code {process.ExitCode}\r\n{json}");
            }
        }
    }
}
