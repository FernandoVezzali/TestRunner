using System;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Web.Administration;

namespace TestRunner.Helpers
{
    internal static class WindowsIntegration
    {
        public static ObjectState StopIIS(string siteName)
        {
            using (var server = new ServerManager())
            {
                Site site = server.Sites.FirstOrDefault(s => s.Name == siteName);
                if (site != null)
                {
                    return site.Stop();
                }
                throw new InvalidOperationException("Could not find website!");
            }
        }

        public static ObjectState StartIIS(string siteName)
        {
            using (var server = new ServerManager())
            {
                Site site = server.Sites.FirstOrDefault(s => s.Name == siteName);
                if (site != null)
                {
                    return site.Start();
                }
                throw new InvalidOperationException("Could not find website!");
            }
        }

        public static void StartWindowService(string serviceName)
        {
            using (var service = new ServiceController(serviceName))
            {
                var status = service.Status;
                if (status == ServiceControllerStatus.Stopped)
                {
                    var timeout = TimeSpan.FromMinutes(1);
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }                
            }
        }

        public static void StopWindowService(string serviceName)
        {
            using (var service = new ServiceController(serviceName))
            {
                var status = service.Status;
                if (status == ServiceControllerStatus.Running)
                {
                    var timeout = TimeSpan.FromMinutes(1);
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }                
            }
        }
    }
}
