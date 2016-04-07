using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using TestRunner.Helpers;
using TestRunner.Properties;

namespace TestRunner
{
    internal class FileSystemHelper
    {
        internal void RemoveReadOnly(string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);

                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                    file.Attributes &= ~FileAttributes.ReadOnly;
            }
            catch (Exception ex)
            {
                Helper.Write(ex.Message,LogLevel.Warn);
            }
        }

        internal void DeleteFiles(string path, List<string> preservedFiles = null)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                var files = directoryInfo.GetFiles();

                foreach (var file in files)
                {
                    if (preservedFiles == null || !preservedFiles.Contains(file.Name))
                    {
                        file.Delete();
                        Helper.Write(string.Format("The file {0} has been deleted.", file.Name),LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.Write(ex.Message,LogLevel.Warn);
            }
        }

        internal void DeleteDBOldFiles()
        {
            var preservedFiles = new List<string>()
            {
                "deploy1.bat",
                "deploy.bat",
                "delta.bat",
                "build.bat",
                "Deployment-Drop.sql",
                "CreateScript-Footer.sql",
                "CreateScript-Header.sql"
            };

            DeleteFiles(dbDirectory(), preservedFiles);
        }

        internal void BuildScripts()
        {
            try
            {
                var changeDirectory = String.Concat("cd ", dbDirectory());
                var buildScripts = "build.bat";
                var command = String.Concat(changeDirectory, "&", buildScripts);

                var ph = new ProcessHelper();
                string output = ph.LaunchProcess("cmd.exe", "/c " + command);
                Helper.Write(output, LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Helper.Write(ex.Message,LogLevel.Warn);
            }
        }

        internal static void CopyFilesToBaseFolder(TestResults results)
        {
            CopyFileToBaseFolder(results.TrxHtmlFile);
            CopyFileToBaseFolder(results.SummaryHtmlFile);
            
        }

        internal static DirectoryInfo GetTemporaryDirectory(TestResults results)
        {
            if (results.CovFile != null)
            {
                if (File.Exists(results.CovFile))
                {
                    var f = new FileInfo(results.CovFile);
                    if (f.Directory != null) return f.Directory;
                }
            }

            throw new FileNotFoundException();
        }

        internal static string CopyFilesToSharedFolder(TestResults results)
        {
            CreateTestResultsFolderIfDoesNotExist();

            var sourceDirectory = GetTemporaryDirectory(results);
            var directoryName = GetTemporaryDirectoryName(sourceDirectory);
            
            var newDirectory = CreateDirectoryIfDoesntExist(Settings.Default.TestResultsFolder, directoryName);

            var files = sourceDirectory.GetFiles();
            foreach (var file in files)
            {
                file.CopyTo(Path.Combine(newDirectory.FullName, file.Name));
            }
            return directoryName;
        }

        private static string GetTemporaryDirectoryName(DirectoryInfo sourceDirectory)
        {
            return sourceDirectory.Name;
        }

        internal static DirectoryInfo CreateDirectoryIfDoesntExist(string path, string directoryName)
        {
            var pathToCreate = Path.Combine(path, directoryName);
            DirectoryInfo sourceDirectory;

            if (!Directory.Exists(pathToCreate))
                sourceDirectory = Directory.CreateDirectory(Path.Combine(path, directoryName));
            else
                sourceDirectory = new DirectoryInfo(pathToCreate);
            
            return sourceDirectory;
        }

        internal static bool AssemblyHaventGotRecompiled(string dllPath)
        {
            if (File.Exists(dllPath))
            {
                if (IsNewAssembly(dllPath))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsNewAssembly(string dllPath)
        {
            var file = new FileInfo(dllPath);
            var creationTime = file.CreationTime;
            var difference = DateTime.Now.Subtract(creationTime);
            var seconds = (int)difference.TotalSeconds;
            if (seconds < 120)
                return true;

            return false;
        }

        internal static void SetPermissions(AccessControlType acType)
        {
            Helper.Write("Setting Permissions ...",LogLevel.Debug);

            string path = Path.GetFullPath(".\\TestResults\\");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var dSecurity = Directory.GetAccessControl(path);

            var ar = new FileSystemAccessRule("Everyone",
                FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None,
                AccessControlType.Deny);

            if (acType == AccessControlType.Deny)
                dSecurity.AddAccessRule(ar);
            else
                dSecurity.RemoveAccessRule(ar);

            Directory.SetAccessControl(path, dSecurity);
        }

        internal static void MergeFiles()
        {
            var directoryInfo = new DirectoryInfo(dbDirectory());

            var header = Path.Combine(directoryInfo.FullName, "CreateScript-Header.sql");
            var mainFile = Path.Combine(directoryInfo.FullName, "FenergoOracle_Create.sql");
            var footer = Path.Combine(directoryInfo.FullName, "CreateScript-Footer.sql");
            var outPutFile = Path.Combine(directoryInfo.FullName, "CreateScript.sql");

            MergeFiles(header, mainFile, footer, outPutFile);
        }

        internal static void CopyDBLogFiles(string directoryName)
        {
            var newDirectory = CreateDirectoryIfDoesntExist(Settings.Default.TestResultsFolder, directoryName);
            var directoryInfo = new DirectoryInfo(dbDirectory());
            var files = directoryInfo.GetFiles().Where(x => x.Extension == ".txt");

            foreach (var file in files)
            {
                file.CopyTo(Path.Combine(newDirectory.FullName, file.Name));
            }
        }

        internal static string dbDirectory()
        {
            var workingDirectory = Settings.Default.WorkingDirectory;
            var path = string.Concat(workingDirectory, @"\Database\FenergoOracle\_Deployment");
            return path;
        }

        private static void MergeFiles(string header, string mainFile, string footer, string outPutFile)
        {
            var fi = new FileInfo(header);
            var filesToMerge = new List<string>() { mainFile, footer };
            var fiLines = File.ReadAllLines(fi.FullName).ToList();
            fiLines.AddRange(filesToMerge.SelectMany(file => File.ReadAllLines(file)));
            File.WriteAllLines(outPutFile, fiLines.ToArray());
        }

        internal static void CopyFileToBaseFolder(string path)
        {
            if (File.Exists(path))
            {
                var file = new FileInfo(path);
                var dest = Path.Combine(Settings.Default.TestResultsFolder, file.Name);
                file.CopyTo(dest);
            }
        }

        private static void CreateTestResultsFolderIfDoesNotExist()
        {
            if (!Directory.Exists(Settings.Default.TestResultsFolder))
            {
                Directory.CreateDirectory(Settings.Default.TestResultsFolder);
            }
        }

        internal void RemoveLogFiles()
        {
            var workingDirectory = Settings.Default.WorkingDirectory;
            var logFolder = new DirectoryInfo(workingDirectory).Parent.FullName;
            var path = string.Concat(logFolder, @"\Logs");

            if (Directory.Exists(path))
            {
                RemoveReadOnly(path);
                DeleteFiles(path);
            }
        }

        internal static string GetErrorLogFile()
        {
            var workingDirectory = Settings.Default.WorkingDirectory;
            var logFolder = new DirectoryInfo(workingDirectory).Parent.FullName;
            var path = string.Concat(logFolder, @"\Logs\error.log");

            if (File.Exists(path))
            {
                return path;
            }
            return null;
        }

        public bool FileExistsOnIIS(TestResults results, string fileName)
        {
            var sourceDirectory = GetTemporaryDirectory(results);
            var directoryName = GetTemporaryDirectoryName(sourceDirectory);
            string url = Path.Combine(Settings.Default.BaseUrl, directoryName) + "/" + fileName;
            return Helper.IsReachable(url);
        }

        internal static void CopyFileToBaseIISFolder(string path, TestResults results)
        {
            if (File.Exists(path))
            {
                var sourceDirectory = GetTemporaryDirectory(results);
                var directoryName = GetTemporaryDirectoryName(sourceDirectory);

                var file = new FileInfo(path);
                var dest = Path.Combine(Settings.Default.TestResultsFolder, directoryName,"error.txt");
                file.CopyTo(dest);
            }
        }
    }
}
