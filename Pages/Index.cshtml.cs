using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ServiceProcess;

namespace Mir_4_Server_Manager.Pages
{
    public class IndexModel : PageModel
    {
        public string CouchbaseStatus { get; set; }
        public string MemuraiStatus { get; set; }
        public void OnGet()
        {
            CouchbaseStatus = GetServiceStatus("CouchbaseServer");
            MemuraiStatus = GetServiceStatus("memurai");
        }
        private string GetServiceStatus(string serviceName)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    return sc.Status.ToString();
                }
            }
            catch
            {
                return "Not Installed";
            }
        }
    }
}