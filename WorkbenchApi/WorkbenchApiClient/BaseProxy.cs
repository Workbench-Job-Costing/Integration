using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WorkbenchApiClient
{
    public abstract class BaseProxy : ProxyGeneratorBaseProxy
    {
        protected BaseProxy(Uri baseUrl) : base(baseUrl)
        {
        }

        /// <summary>
        /// Builds the HTTP client.
        /// </summary>
        /// <returns></returns>
        protected virtual HttpClient BuildHttpClient()
        {
            var httpClient = base.BuildHttpClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", "ZGVtbzE6dGVzdA==");

            return httpClient;
        }
    }
}
