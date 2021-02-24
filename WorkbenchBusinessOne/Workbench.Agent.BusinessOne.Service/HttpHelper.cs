using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Agent.BusinessOne.Service
{
    public static class HttpHelper
    {
        private static readonly string apiBasicUri = ConfigurationManager.AppSettings["ManagementConsole"];
        public static object JsonConvert { get; private set; }
        public static async Task Post(string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiBasicUri);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", (ConfigurationManager.AppSettings["BusinessOneUserName"] + ":" + ConfigurationManager.AppSettings["BusinessOnePassword"].FromBase64()).ToBase64());
                           
                StringContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(ConfigurationManager.AppSettings["ManagementConsole"]), Encoding.UTF8, "application/json");
                var result = await client.PostAsync(url, content); 
                result.EnsureSuccessStatusCode();
                var resultString = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)Newtonsoft.Json.JsonConvert.DeserializeObject(result.Content.ReadAsStringAsync().Result)).First).Value).Value;              
                Helpers.LogInfo($"Helper Post: {resultString}");
            }
        }
    }
}
