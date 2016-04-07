using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;
using TestRunner.Properties;

namespace TestRunner.Helpers
{
    public enum LogLevel
    {
        Debug,
        Error,
        Warn,
        Fatal,
        DebugFormat,
        ErrorFormat,
        Info,
        InfoFormat
    };

    public static class Helper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Write(string logMessage, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    Log.Debug(logMessage);
                    Console.WriteLine(logMessage);
                    break;
                case LogLevel.Error:
                    Log.Error(logMessage);
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                    break;
                case LogLevel.Warn:
                    Log.Warn(logMessage);
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                    break;
                case LogLevel.DebugFormat:
                    Log.DebugFormat(logMessage);
                    Console.WriteLine(logMessage);
                    break;
                case LogLevel.ErrorFormat:
                    Log.ErrorFormat(logMessage);
                    Console.WriteLine(logMessage);
                    break;
                case LogLevel.Fatal:
                    Log.Fatal(logMessage);
                    Console.WriteLine(logMessage);
                    break;
                case LogLevel.Info:
                    Log.Info(logMessage);
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(logMessage);
                    Console.ResetColor();
                    break;
                case LogLevel.InfoFormat:
                    Log.InfoFormat(logMessage);
                    Console.WriteLine(logMessage);
                    break;
            }
        }

        internal static string Quote(string input)
        {
            return String.Format(CultureInfo.InvariantCulture, "\"{0}\"", input);
        }

        internal static string BuildSubject(string buildName, TestCategory testCategory)
        {
            string subject = "";

            switch (testCategory)
            {
                case TestCategory.Unit:
                    subject = String.Concat("Unit Test results for build ", buildName);
                    break;
                case TestCategory.Integration:
                    subject = String.Concat("Integration Test results for build ", buildName);
                    break;
                case TestCategory.Configuration:
                    subject = String.Concat("Configuration Test results for build ", buildName);
                    break;
            }

            return subject;
        }

        internal static void AddDLLToArgumentList(string dllPath, List<string> arg)
        {
            if (File.Exists(dllPath))
            {
                arg.Add(Quote(dllPath));
            }
        }

        internal static void AddDLLsToArgumentList(List<string> arg, string category)
        {
            switch (category)
            {
                case "TestCategory=UnitTest":
                    AddDLLToArgumentList(Settings.Default.UnitTestDll, arg);
                    break;
                case "TestCategory=ConfigurationTest":
                    AddDLLToArgumentList(Settings.Default.ConfigurationTestDll, arg);
                    break;
                case "TestCategory=IntegrationTest":
                    AddDLLToArgumentList(Settings.Default.IntegrationTestDll, arg);
                    break;
            }
        }

        internal static bool IsReachable(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}
