namespace Orchestrator.Mock
{
    using Orchestrator.Abstraction;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class MockHttpClientHandler : IHttpHandler
    {
        public HttpResponseMessage Get(string url)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public HttpResponseMessage Post(string url, HttpContent content)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
