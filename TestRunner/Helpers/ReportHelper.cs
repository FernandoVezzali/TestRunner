using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Xml;
using HtmlAgilityPack;
using TestRunner.Properties;

namespace TestRunner
{
    public static class ReportHelper
    {
        internal static string PrintVersionNumber()
        {
            var version = GetPublishedVersion();
            string result = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            return result;
        }

        private static Version GetPublishedVersion()
        {
            XmlDocument xmlDoc = new XmlDocument();
            Assembly asmCurrent = Assembly.GetExecutingAssembly();
            string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

            xmlDoc.Load(executePath + ".manifest");
            string retval = string.Empty;
            if (xmlDoc.HasChildNodes)
            {
                retval = xmlDoc.ChildNodes[1].ChildNodes[0].Attributes.GetNamedItem("version").Value;
            }
            return new Version(retval);
        }

        internal static string ParseCoverageResults(string covHtmlFile)
        {
            var docCov = new HtmlDocument();
            docCov.Load(covHtmlFile);
            HtmlNode summaryCov = docCov.DocumentNode.SelectSingleNode("//table[@class='overview']");
            HtmlNode totalHeaderCov = summaryCov.SelectSingleNode("//th[text() ='Line Coverage:']");
            string warningCov = string.Empty;
            totalHeaderCov.NextSibling.Attributes.Add("style", "background-color: #0F0;");
            return warningCov + summaryCov.OuterHtml;
        }

        internal static void BuildHtmlLink(string directoryName, HtmlTextWriter writer, string name, string fileName)
        {
            string newPath = Path.Combine(Settings.Default.BaseUrl, directoryName) + fileName;
            var link = new HyperLink { Target = "_blank", NavigateUrl = newPath, Text = name };
            link.RenderControl(writer);
        }

        internal static void BuildHtmlLink(HtmlTextWriter writer, string href, string name)
        {
            string path = Path.GetFullPath(".\\TestResults\\");
            var link = new HyperLink { Target = "_blank", NavigateUrl = href.Replace(path, Settings.Default.BaseUrl), Text = name };
            link.RenderControl(writer);
        }

        internal static void BuildHtmlHeader(HtmlTextWriter writer, string name)
        {
            var h1 = new HtmlGenericControl("h1") { InnerHtml = name };
            h1.RenderControl(writer);
        }

        internal static string ParseTestResults(string trxHtmlFile)
        {
            var docTest = new HtmlDocument();
            docTest.Load(trxHtmlFile);
            HtmlNode summaryTest = docTest.DocumentNode.SelectSingleNode("//table[@id='tMainSummary']");
            return summaryTest.OuterHtml;
        }

        private static DirectoryInfo GetLibDirectory()
        {
            var baseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var reportVersion = Settings.Default.ReportGeneratorVersion;

            if (baseDirectory.GetDirectories().Any(x => x.Name == "lib"))
            {
                var lib = baseDirectory.GetDirectories().FirstOrDefault(x => x.Name == "lib");
                if (lib.GetDirectories().Any(x => x.FullName.Contains(reportVersion)))
                {
                    return lib;
                }
            }

            return baseDirectory.Parent.Parent.GetDirectories().FirstOrDefault(x => x.Name == "lib");
        }

        internal static string GetReportPath()
        {
            DirectoryInfo lib = GetLibDirectory();
            var reportVersion = Settings.Default.ReportGeneratorVersion;

            var reportPath = lib.GetDirectories().FirstOrDefault(x => x.FullName.Contains(reportVersion)).GetFiles().FirstOrDefault(x => x.Name == "ReportGenerator.exe").FullName;

            return reportPath;
        }
    }
}
