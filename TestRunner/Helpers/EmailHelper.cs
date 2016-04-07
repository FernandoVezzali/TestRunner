using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using TestRunner.Properties;

namespace TestRunner.Helpers
{
    public class EmailHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void SendSmtpEmail(string subject, TestResults results)
        {
            try
            {
                using (var sr = new StreamReader(results.SummaryHtmlFile))
                {
                    string msgBody = sr.ReadToEnd();
                    SendSmtpPlainEmail(subject, msgBody, results);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error sending email to {0} Exception: {1}", Settings.Default.SmtpTo, ex);
            }
        }

        public static void SendSmtpPlainEmail(string subject, string msgBody, TestResults results = null)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Settings.Default.SmtpFrom)
                };
                foreach (string emailTo in Settings.Default.SmtpTo.Split(';'))
                {
                    mailMessage.To.Add(emailTo);
                }

                mailMessage.Subject = subject;
                mailMessage.IsBodyHtml = true;

                AlternateView htmlAv = AlternateView.CreateAlternateViewFromString(msgBody, null,
                    MediaTypeNames.Text.Html);
                mailMessage.AlternateViews.Add(htmlAv);
                string textBody = Regex.Replace(msgBody, @"<[^>]*>", String.Empty);
                AlternateView textAv = AlternateView.CreateAlternateViewFromString(textBody, null,
                    MediaTypeNames.Text.Plain);
                mailMessage.AlternateViews.Add(textAv);

                var smtp = new SmtpClient(Settings.Default.SmtpServer, Settings.Default.SmtpPort)
                {
                    EnableSsl = Settings.Default.SmtpSsl
                };
                if (!string.IsNullOrEmpty(Settings.Default.SmtpUser))
                    smtp.Credentials = new NetworkCredential(Settings.Default.SmtpUser,
                        Settings.Default.SmtpPassword);
                smtp.Send(mailMessage);
                Log.InfoFormat("Email sent to: {0}", Settings.Default.SmtpTo);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error sending email to {0} Exception: {1}", Settings.Default.SmtpTo, ex);
            }
        }
    }
}
