using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using log4net;
using Microsoft.TeamFoundation.Build.Client;
using TestRunner.Helpers;
using TestRunner.Properties;

namespace TestRunner
{
    public class ProcessHelper
    {
        public StringBuilder StdOut { get; set; }

        private IBuildDetail _build;

        public ProcessHelper(IBuildDetail build)
        {
            _build = build;
        }

        public ProcessHelper()
        {

        }

        internal static string ExecuteCommand(string command)
        {
            var ph = new ProcessHelper();
            string output = ph.LaunchProcess("cmd.exe", "/c " + command);
            Helper.Write(output,LogLevel.Debug);
            return output;
        }

        public string LaunchProcess(string process, string arguments)
        {
            Helper.Write(string.Format("LaunchProcess - Process: {0}", process),LogLevel.Debug);
            Helper.Write(string.Format("LaunchProcess - Arguments: {0}", arguments),LogLevel.Debug);

            StdOut = new StringBuilder();
            using (var p = new Process
            {
                StartInfo =
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = process,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    Arguments = arguments,
                }
            })
            {
                p.OutputDataReceived += OnDataReceived;
                p.EnableRaisingEvents = true;

                Helper.Write(String.Format(">{0} {1}", process, arguments),LogLevel.DebugFormat);
                p.Start();
                p.BeginOutputReadLine();


                bool finished = p.WaitForExit(Settings.Default.ProcessTimeout);
                if (!finished)
                {
                    p.Kill();
                    EmailHelper.SendSmtpPlainEmail(BuildErrorSubject(), BuildErrorMessage(process, arguments));
                }
            }
            Helper.Write(String.Format("\nOutput\n{0}", StdOut),LogLevel.Debug);
            return StdOut.ToString();
        }

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            StdOut.Append(e.Data);
        }

        private string BuildErrorSubject()
        {
            return string.Format("Test results for build {0} requested by {1} FAILED", _build.BuildNumber,
                _build.RequestedFor);
        }

        private string BuildErrorMessage(string process, string arguments)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<html><body>Test results for build {0} requested by {1} FAILED.<br /> The process did not respond in a timely fashion.<br /><br />",
                _build.BuildNumber,
                _build.RequestedFor);
            sb.Append("Process details: <br />");
            sb.AppendFormat("&nbsp;&nbsp;&nbsp;<b>FileName:</b> {0}<br />", process);
            sb.AppendFormat("&nbsp;&nbsp;&nbsp;<b>Arguments:</b> {0}<br />", arguments);
            sb.AppendFormat("&nbsp;&nbsp;&nbsp;<b>Standard Output:</b> {0}<br />", StdOut);
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
