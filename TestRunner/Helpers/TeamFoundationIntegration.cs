using System;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using TestRunner.Properties;

namespace TestRunner.Helpers
{
    internal static class TeamFoundationIntegration
    {
        public static string GetBuildNumber(string tp)
        {
            string uri = Settings.Default.TfsTeamProjectCollection;
            var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(uri), new UICredentialsProvider());
            tfs.EnsureAuthenticated();
            var buildServer = tfs.GetService<IBuildServer>();

            // Get Builds 
            var buildDetails = buildServer.QueryBuilds(tp)
                .Where(x => x.BuildNumber.StartsWith(Settings.Default.BuildDefinition)).
                OrderByDescending(x => x.FinishTime);

            // Get the latest build
            IBuildDetail latest = buildDetails.FirstOrDefault();

            return latest.BuildNumber;
        }

        public static IBuildDetail QueryBuild()
        {
            Helper.Write("Quering Build ...",LogLevel.Debug);

            IBuildServer buildServer;
            using (var tfs = new TfsTeamProjectCollection(new Uri(Settings.Default.TfsTeamProjectCollection)))
            {
                buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));
                string tp = Settings.Default.TeamProject;
                string buildNumber = GetBuildNumber(tp);
                var defSpec = buildServer.CreateBuildDefinitionSpec(tp);
                return buildServer.GetBuild(defSpec, buildNumber, null, QueryOptions.All);
            }
        }

        public static string PlatformConfigurations(IBuildDetail build)
        {
            var process = WorkflowHelpers.DeserializeProcessParameters(build.BuildDefinition.ProcessParameters);
            if (process.Count <= 0)
                return string.Empty;

            var settings = process.First();
            var buildSettings = settings.Value as BuildSettings;
            if (buildSettings == null)
                return string.Empty;

            if (buildSettings.PlatformConfigurations.Count <= 0)
                return string.Empty;

            PlatformConfiguration pc = buildSettings.PlatformConfigurations.First();
            return string.Format("Platform={0};Flavor={1};", pc.Platform, pc.Configuration);
        }

        public static void GetLatestVersion()
        {
            VersionControlServer sourceControl;
            using (var tfs = new TeamFoundationServer(Settings.Default.TfsTeamProjectCollection, System.Net.CredentialCache.DefaultCredentials))
            {
                tfs.EnsureAuthenticated();
                sourceControl = (VersionControlServer)tfs.GetService(typeof(VersionControlServer));
                Workspace[] workspaces = sourceControl.QueryWorkspaces(Settings.Default.Workspace, sourceControl.AuthenticatedUser, Workstation.Current.Name);

                if (workspaces.Length == 1)
                {
                    Workspace workspace = workspaces[0];
                    GetRequest request = new GetRequest(new ItemSpec(Settings.Default.WorkingDirectory, RecursionType.Full), VersionSpec.Latest);
                    GetStatus status = workspace.Get(request, GetOptions.GetAll | GetOptions.Overwrite);
                }
            }
        }
    }
}