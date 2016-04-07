using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI;
using Microsoft.TeamFoundation.Build.Client;
using TestRunner.Properties;
using System;
using System.Linq;
using TestRunner.Helpers;

namespace TestRunner
{
	enum TestCategory
	{
        Unit,
        Configuration,
        Integration
	};

    public class Report
    {
        private IBuildDetail _build;

        public Report(IBuildDetail build)
        {
            _build = build;
        }

        public List<TestResults> RunTest(string buildName, string testCategories)
        {
            var results = new List<TestResults>();
            var categories = testCategories.Split('|');
            foreach (var category in categories)
            {
                RunTestCategory(buildName, category, results);
            }

            return results;
        }

        private void RunTestCategory(string buildName, string category, List<TestResults> results)
        {
            Helper.Write(String.Format("Running {0} tests...", category), LogLevel.Debug);

            var arg = new List<string>
            {
                string.Format(CultureInfo.InvariantCulture, Settings.Default.VsTestArguments,
                    Settings.Default.TeamProject, buildName, TeamFoundationIntegration.PlatformConfigurations(_build)),
                "\"/settings:" + Path.GetFullPath(".\\Local.testsettings") + "\""
            };

            var result = GetResult(arg, category);
            result.TestCategory = category;
            result.ExecutionOrder = SetOrder(category);
            results.Add(result);
        }

        private int SetOrder(string category)
        {
            switch (category)
            {
                case "TestCategory=UnitTest":
                    return 0;
                case "TestCategory=ConfigurationTest":
                    return 1;
                case "TestCategory=IntegrationTest":
                    return 2;
            }
            return 0;
        }

        private TestResults GetResult(List<string> arg, string category)
        {
            arg.Add("/TestCaseFilter:" + Helper.Quote(category));
            Helper.AddDLLsToArgumentList(arg, category);

            var ph = new ProcessHelper(_build);
            var output = ph.LaunchProcess(Settings.Default.VsTestPath, string.Join(" ", arg));
            var matches = Regex.Match(output, @".*Results File: (?<trxFile>.+\.trx).*Test Results: (?<mtm>mtm:.+)Attachments:(?<covFile>.+\.coverage).*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (matches.Success)
            {
                return GetResult(matches);
            }
            return null;
        }

        private TestResults GetResult(Match matches)
        {
            var result = new TestResults
            {
                TrxFile = matches.Groups["trxFile"].Value.Trim(),
                CovFile = matches.Groups["covFile"].Value.Trim(),
                MtmUrl = matches.Groups["mtm"].Value.Trim().Trim('.')
            };
            return result;
        }

        public void ProcessResult(List<TestResults> results, string testCategory)
        {
            foreach (var result in results.OrderBy(x=>x.ExecutionOrder))
            {
                switch (result.TestCategory)
                {
                    case "TestCategory=UnitTest":
                        ProcessUnitTestResult(result);
                        break;
                    case "TestCategory=IntegrationTest":
                        ProcessIntegrationTest(result);
                        break;
                    case "TestCategory=ConfigurationTest":
                        ProcessConfigurationTest(result);
                        break;
                }
            }
        }

        private void ProcessConfigurationTest(TestResults result)
        {
            Helper.Write("Processing configuration test result...", LogLevel.Debug);
            RunTestHtmlReport(result);
            string directoryName = FileSystemHelper.CopyFilesToSharedFolder(result);
            FileSystemHelper.CopyFileToBaseIISFolder(FileSystemHelper.GetErrorLogFile(), result);
            FileSystemHelper.CopyDBLogFiles(directoryName);
            BuildReport(_build.BuildNumber, _build.RequestedFor, result, directoryName, TestCategory.Configuration);
            FileSystemHelper.CopyFilesToBaseFolder(result);
            EmailHelper.SendSmtpEmail(Helper.BuildSubject(_build.BuildNumber,TestCategory.Configuration), result);
        }

        private void ProcessIntegrationTest(TestResults result)
        {
            Helper.Write("Processing integration test result...", LogLevel.Debug);
            RunTestHtmlReport(result);
            string directoryName = FileSystemHelper.CopyFilesToSharedFolder(result);
            FileSystemHelper.CopyFileToBaseIISFolder(FileSystemHelper.GetErrorLogFile(), result);
            FileSystemHelper.CopyDBLogFiles(directoryName);
            BuildReport(_build.BuildNumber, _build.RequestedFor, result, directoryName, TestCategory.Integration);
            FileSystemHelper.CopyFilesToBaseFolder(result);
            EmailHelper.SendSmtpEmail(Helper.BuildSubject(_build.BuildNumber,TestCategory.Integration), result);
        }

        private void ProcessUnitTestResult(TestResults result)
        {
            Helper.Write("Processing unit test result ...",LogLevel.Debug);
            RunTestHtmlReport(result);
            RunCoverage(result);
            RunCoverageHtmlReport(result);
            string directoryName = FileSystemHelper.CopyFilesToSharedFolder(result);
            FileSystemHelper.CopyFileToBaseIISFolder(FileSystemHelper.GetErrorLogFile(), result);
            FileSystemHelper.CopyDBLogFiles(directoryName);
            BuildReport(_build.BuildNumber, _build.RequestedFor, result, directoryName, TestCategory.Unit);
            FileSystemHelper.CopyFilesToBaseFolder(result);
            EmailHelper.SendSmtpEmail(Helper.BuildSubject(_build.BuildNumber,TestCategory.Unit), result);
        }

        private void BuildReport(string buildNumber, string requestFor, TestResults results, string directoryName, TestCategory testCategory)
        {
            results.SummaryHtmlFile = Path.GetDirectoryName(results.TrxFile) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(results.TrxFile) + "_summary.html";
            using (var textWriter = File.CreateText(results.SummaryHtmlFile))
            using (var writer = new HtmlTextWriter(textWriter))
            {
                WriteHeader(buildNumber, requestFor, results, testCategory, writer);
                WriteBody(results, directoryName, testCategory, writer);
                WriteFooter(writer);

                writer.Flush();
            }
        }

        private static void WriteFooter(HtmlTextWriter writer)
        {
            writer.WriteBreak();
            writer.WriteBreak();
            writer.Write(ReportHelper.PrintVersionNumber());
            writer.WriteBreak();
            writer.WriteLine(Resources.SummaryFooter);
        }

        private void WriteBody(TestResults results, string directoryName, TestCategory testCategory, HtmlTextWriter writer)
        {
            if (testCategory == TestCategory.Unit)
                WriteUnitTestBody(results, directoryName, writer);

            ReportHelper.BuildHtmlLink(writer, results.TrxHtmlFile, "See full test report");
            writer.Write(" | ");
            ReportHelper.BuildHtmlLink(writer, results.SummaryHtmlFile, "See summary");

            if (testCategory == TestCategory.Integration)
                WriteIntegrationTestLinks(results, directoryName, writer);
        }

        private void WriteIntegrationTestLinks(TestResults results, string directoryName, HtmlTextWriter writer)
        {
            if (OracleLogHasBeenUploaded(results))
            {
                writer.Write(" | ");
                ReportHelper.BuildHtmlLink(directoryName, writer, "See Oracle Log", "/output-create.txt");
            }

            if (ErrorLogHasBeenUploaded(results))
            {
                writer.Write(" | ");
                ReportHelper.BuildHtmlLink(directoryName, writer, "Error Log", "/error.txt");
            }
        }

        private static void WriteUnitTestBody(TestResults results, string directoryName, HtmlTextWriter writer)
        {
            writer.WriteBreak();
            writer.WriteLine(ReportHelper.ParseCoverageResults(results.CovHtmlFile));
            writer.WriteBreak();
            writer.WriteBreak();
            ReportHelper.BuildHtmlLink(directoryName, writer, "See full coverage report", "/index.htm");
            writer.Write(" | ");
        }

        private static void WriteHeader(string buildNumber, string requestFor, TestResults results, TestCategory testCategory,
            HtmlTextWriter writer)
        {
            writer.WriteLine(Resources.SummaryHeader);
            string preheader = Helper.BuildSubject(buildNumber, testCategory);
            string header = string.Format("{0} requested by {1}", preheader, requestFor);
            ReportHelper.BuildHtmlHeader(writer, header);
            writer.WriteBreak();
            writer.WriteBreak();
            ReportHelper.BuildHtmlHeader(writer, "Test results");
            writer.WriteLine(ReportHelper.ParseTestResults(results.TrxHtmlFile));
            writer.WriteBreak();
            writer.WriteBreak();
        }

        private bool ErrorLogHasBeenUploaded(TestResults results)
        {
            var fs = new FileSystemHelper();
            return fs.FileExistsOnIIS(results, "error.txt");
        }

        private bool OracleLogHasBeenUploaded(TestResults results)
        {
            var fs = new FileSystemHelper();
            return fs.FileExistsOnIIS(results, "output-create.txt");
        }

        private void RunTestHtmlReport(TestResults results)
        {
            var ph = new ProcessHelper(_build);
            string output = ph.LaunchProcess(".\\lib\\trx2html.exe", Helper.Quote(results.TrxFile));
            var matches = Regex.Match(output, @".*OutputFile: (?<trxHtmlFile>.+\.trx\.htm).*", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (matches.Success)
            {
                results.TrxHtmlFile = matches.Groups["trxHtmlFile"].Value.Trim();
            }
        }

        private void RunCoverage(TestResults results)
        {
            results.CovXmlFile = results.CovFile + "xml";
            string[] arg =
            {
                "analyze",
                Helper.Quote("/output:" + results.CovXmlFile),
                Helper.Quote(results.CovFile)
            };
            var ph = new ProcessHelper(_build);
            ph.LaunchProcess(Settings.Default.CodeCoveragePath, string.Join(" ", arg));
        }

        private void RunCoverageHtmlReport(TestResults results)
        {
            results.CovHtmlFile = Path.GetDirectoryName(results.CovXmlFile);

            string[] arg =
            {
                Helper.Quote("-reports:" + results.CovXmlFile),
                Helper.Quote("-targetdir:" + results.CovHtmlFile),
                Helper.Quote("-historydir:" + Settings.Default.TestResultsFolder),
                "-reporttypes:Html",
                "-filters:" + Settings.Default.CoverageReportFilters
            };

            var ph = new ProcessHelper(_build);
            string reportPath = ReportHelper.GetReportPath();
            ph.LaunchProcess(reportPath, string.Join(" ", arg));
            results.CovHtmlFile += Path.DirectorySeparatorChar + "index.htm";
        }
    }
}
