
namespace Orchestrator.Service
{
    using Orchestrator.Abstraction;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class HttpClientHandler : IHttpHandler
    {
        public HttpClient Client { get; set; }
        public HttpClientHandler(string baseAddress)
        {
            Client = new HttpClient() { BaseAddress = new System.Uri(baseAddress) };
        }

        public HttpResponseMessage Get(string url)
        {
            return GetAsync(url).Result;
        }

        public HttpResponseMessage Post(string url, HttpContent content)
        {
            return PostAsync(url, content).Result;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await Client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return await Client.PostAsync(url, content);
        }
    }
}
