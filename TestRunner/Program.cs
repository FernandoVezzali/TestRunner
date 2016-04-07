using System;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using log4net;
using Microsoft.TeamFoundation.Build.Client;
using Polly;
using TestRunner.Exceptions;
using TestRunner.Helpers;
using TestRunner.Properties;

namespace TestRunner
{
    internal class Program
    {
        private static void Main()
        {
            if (Settings.Default.ForceExecution == "True")
            {
                Helper.Write("Starting (forced)", LogLevel.Debug);
                Start();
            }
            else
            {
                var policy = Policy.Handle<NewBuildNotAvailableException>().RetryForever();
                policy.Execute(Run);
            }
        }

        private static void Start()
        {
            try
            {
                var testCategory = Settings.Default.TestCategory;
                var build = TeamFoundationIntegration.QueryBuild();

                FileSystemHelper.SetPermissions(AccessControlType.Deny); 
                WindowsIntegration.StopIIS("Default Web Site");

                if (Settings.Default.GetLatestAndCompile == "True")
                {
                    GetLatestAndCompile(build);
                }

                if (Settings.Default.DeployDatabase == "True")
                {
                    testCategory = DeployBatabase(testCategory);
                }

                RemoveLogFiles();

                WindowsIntegration.StartIIS("Default Web Site");

                var report = new Report(build);
                var results = report.RunTest(build.BuildNumber, testCategory);
                if (results != null)
                {
                    report.ProcessResult(results, testCategory);
                }
                else
                {
                    Helper.Write("Test result is null.", LogLevel.Warn);
                    Helper.Write(string.Format("Build Number: {0} ", build.BuildNumber),LogLevel.Warn);
                    Helper.Write(string.Format("Filter: {0} ", testCategory),LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Helper.Write(ex.Message, LogLevel.Warn);
            }
        }

        private static void Run()
        {
            if (!NewBuildAvailable())
            {
                throw new NewBuildNotAvailableException();
            }

            Start();

            // Wait 5 minutes and try again
            Thread.Sleep(TimeSpan.FromMinutes(5));
            Main();
        }

        private static bool NewBuildAvailable()
        {
            try
            {
                Thread.Sleep(TimeSpan.FromSeconds(60));
                var build = TeamFoundationIntegration.QueryBuild();

                if (build.Status == BuildStatus.Succeeded)
                {
                    var difference = DateTime.Now.Subtract(build.FinishTime);
                    var minutes = (int)difference.TotalMinutes;

                    // If the build was finished less than 5 minutes ago and it has succeeded, then should return true.
                    if (minutes < 5)
                    {
                        Helper.Write("Starting...",LogLevel.Info);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Helper.Write(string.Format("NewBuildAvailable Exception: {0}", e.Message),LogLevel.Warn);
            }

            Console.Clear();
            Helper.Write("Waiting for the next build...", LogLevel.Info);

            return false;
        }

        private static string DeployBatabase(string testCategory)
        {
            Helper.Write("Deploying database...", LogLevel.Info);
            bool deployDatabaseSucceed = BuildAndDeployDatabase();
            testCategory = ReviewTestCategories(testCategory, deployDatabaseSucceed);
            return testCategory;
        }

        private static void GetLatestAndCompile(IBuildDetail build)
        {
            Helper.Write("Getting latest version ...",LogLevel.Info);
            TeamFoundationIntegration.GetLatestVersion();

            Helper.Write("Compiling ...",LogLevel.Info);
            var compilation = new Compilation();

            compilation.Run(build);
        }

        private static void RemoveLogFiles()
        {
            var fs = new FileSystemHelper();
            Helper.Write("Removing log files ...",LogLevel.Info);
            fs.RemoveLogFiles();
        }

        private static string ReviewTestCategories(string testCategory, bool deployDatabaseSucceed)
        {
            // If the database deploy has failed, the integration and configuration tests shouldn't be run
            var tcFilter = new TestCategoryFilter(deployDatabaseSucceed, testCategory);
            return tcFilter.GetFilteredCategories();
        }

        private static bool BuildAndDeployDatabase()
        {
            var fs = new FileSystemHelper();

            Helper.Write("Removing Read-Only ...",LogLevel.Info);
            fs.RemoveReadOnly(FileSystemHelper.dbDirectory());

            Helper.Write("Deleting old files ...",LogLevel.Info);
            fs.DeleteDBOldFiles();

            Helper.Write("Building scripts ...",LogLevel.Info);
            fs.BuildScripts();

            var dataBase = new DBIntegration();
            return dataBase.BuildAndDeploy();
        }
    }
}