using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.TeamFoundation.Build.Client;
using TestRunner.Properties;
using TestRunner.Helpers;

namespace TestRunner
{
    public class Compilation
    {
        public void Run(IBuildDetail build)
        {
            CompileWholeSolution(build);
            ReCompileTestProjectIfNeeded(Settings.Default.ConfigurationTestDll);
            ReCompileTestProjectIfNeeded(Settings.Default.IntegrationTestDll);
            ReCompileTestProjectIfNeeded(Settings.Default.ServiceImplementationTestDll);
            ReCompileTestProjectIfNeeded(Settings.Default.UnitTestDll);
        }

        private void ReCompileTestProjectIfNeeded(string dllPath)
        {
            if (FileSystemHelper.AssemblyHaventGotRecompiled(dllPath))
            {
                ReCompileByCSproj(dllPath);
            }
        }

        private void ReCompileByCSproj(string dllPath)
        {
            var file = new FileInfo(dllPath);
            var csProjFolder = file.Directory.Parent.Parent;
            var csProj = csProjFolder.GetFiles().FirstOrDefault(x => x.Extension == ".csproj");

            if (File.Exists(dllPath))
                File.Delete(dllPath);

            var projectCollection = new ProjectCollection();
            var globalProperty = new Dictionary<String, String>();
            var buildRequest = new BuildRequestData(csProj.FullName, globalProperty, null, new[] { "Clean", "Build" }, null);
            var output = BuildManager.DefaultBuildManager.Build(new BuildParameters(projectCollection), buildRequest);
            BuildResultCode result = output.OverallResult;

            Helper.Write(string.Format("Project: {0}, Compilation Result: {1}", csProj.Name, result),LogLevel.Debug);
        }

        private void CompileWholeSolution(IBuildDetail build)
        {
            var ph = new ProcessHelper(build);
            var compiler = Settings.Default.Compiler;
            string[] arg = { Settings.Default.VsSolutionPath };
            string output = ph.LaunchProcess(compiler, string.Join(" ", arg));

            Helper.Write(string.Format("Solution compilation output: {0}", output),LogLevel.Debug);
        }
    }
}
