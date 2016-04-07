using System;
using System.Diagnostics;
using log4net;
using Polly;
using TestRunner.Exceptions;
using TestRunner.Helpers;
using TestRunner.Properties;

namespace TestRunner
{
    public class DBIntegration
    {
        public bool BuildAndDeploy()
        {
            Helper.Write("Dropping DB ...",LogLevel.Debug);
            TryDropDatabase();

            Helper.Write("Merging files ...", LogLevel.Debug);
            FileSystemHelper.MergeFiles();

            Helper.Write("Deploying DB ...", LogLevel.Debug);
            bool deployDatabaseSucceed = TryDeployDatabase();

            if (deployDatabaseSucceed)
            {
                Helper.Write("Updating Local User ...", LogLevel.Debug);
                UpdateLocalUser();
            }

            return deployDatabaseSucceed;
        }

        private static string GetDbCommandFromFile(string scriptFile)
        {
            var changeDirectory = String.Concat("cd ", FileSystemHelper.dbDirectory());
            var buildScript = "exit | sqlplus sys/ergo.1234@vmora AS SYSDBA @";
            var command = String.Concat(changeDirectory, "&", buildScript, scriptFile);
            return command;
        }

        private static string GetDbCommand(string sqlCommand)
        {
            var changeDirectory = String.Concat("cd ", FileSystemHelper.dbDirectory());
            var buildScript = string.Format("exit | @echo {0} | sqlplus sys/ergo.1234@vmora AS SYSDBA", sqlCommand);
            var command = String.Concat(changeDirectory, "&", buildScript);
            return command;
        }

        private static void RestartOracleService()
        {
            string serviceName = Settings.Default.OracleServiceName;
            WindowsIntegration.StopWindowService(serviceName);
            WindowsIntegration.StartWindowService(serviceName);
        }

        private static void TryUpdateLocalUser(string sqlCommand)
        {
            try
            {
                Policy.Handle<DataBaseDeployException>().WaitAndRetry(new[]{
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
              }, (exception, timeSpan, context) =>
              {
                  Helper.Write("Database user update has failed.",LogLevel.Warn);
                  RestartOracleService();
              }).Execute(() => TryToUpdateLocalUser(sqlCommand));
            }
            catch (DataBaseDeployException) { }
        }

        private static void TryDropDatabase()
        {
            try
            {
                Policy.Handle<DataBaseDeployException>().WaitAndRetry(new[]{
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
              }, (exception, timeSpan, context) =>
              {
                  Helper.Write("Database drop has failed.", LogLevel.Warn);
                  RestartOracleService();
              }).Execute(() => TryToExecuteDatabaseScriptFromFile("Deployment-Drop.sql"));
            }
            catch (DataBaseDeployException) { }
        }

        private static bool TryDeployDatabase()
        {
            try
            {
                Policy.Handle<DataBaseDeployException>().WaitAndRetry(new[]{
                TimeSpan.FromSeconds(5), 
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
              }, (exception, timeSpan, context) =>
              {
                  Helper.Write("Database deployment has failed.", LogLevel.Warn);
                  RestartOracleService();
              }).Execute(() => TryToExecuteDatabaseScriptFromFile("CreateScript.sql"));
            }
            catch (DataBaseDeployException)
            {
                return false;
            }
            return true;
        }

        private static string TryToUpdateLocalUser(string sqlCommand)
        {
            Helper.Write(String.Format("The execution of the command {0} has been started...", sqlCommand),LogLevel.Debug);

            string command = GetDbCommand(sqlCommand);

            var watch = Stopwatch.StartNew();
            string output = ProcessHelper.ExecuteCommand(command);
            watch.Stop();

            if (!output.Contains("1 row updated"))
            {
                throw new DataBaseDeployException();
            }

            Helper.Write(String.Format("The execution of the command {0} has been finished.", sqlCommand), LogLevel.Debug);
            return output;
        }

        private static string TryToExecuteDatabaseScriptFromFile(string scriptName)
        {
            Helper.Write(string.Format("The execution of the script {0} has been started...", scriptName),LogLevel.Debug);

            string command = GetDbCommandFromFile(scriptName);

            var watch = Stopwatch.StartNew();
            string output = ProcessHelper.ExecuteCommand(command);
            watch.Stop();

            if (watch.Elapsed < TimeSpan.FromMinutes(1) && scriptName == "CreateScript.sql")
            {
                throw new DataBaseDeployException();
            }

            Helper.Write(string.Format("The execution of the script {0} has been finished.", scriptName), LogLevel.Debug);
            return output;
        }

        private static void UpdateLocalUser()
        {
            if (Settings.Default.UpdateLocalUser == "True")
            {
                var domainUpper = Settings.Default.Workspace.ToUpper();
                var domainLower = Settings.Default.Workspace.ToLower();

                var sqlCommand = string.Format("UPDATE AUTHORISATION.\"USER\" SET USERNAME = '{0}\\workflow', LOWEREDUSERNAME = '{1}\\workflow' WHERE ID = 1;", domainUpper, domainLower);
                TryUpdateLocalUser(sqlCommand);

                var localUserUpper = Settings.Default.LocalUser;
                var localUserLower = Settings.Default.LocalUser.ToLower();

                sqlCommand = string.Format("UPDATE AUTHORISATION.\"USER\" SET USERNAME = '{0}\\{1}', LOWEREDUSERNAME = '{2}\\{3}' WHERE ID = 3;", domainUpper, localUserUpper, domainLower, localUserLower);
                TryUpdateLocalUser(sqlCommand);
            }
        }
    }
}
