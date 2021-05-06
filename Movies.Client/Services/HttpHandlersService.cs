using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Client.Services
{
    public class HttpHandlersService : IIntegrationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();


        // Builds pipeline to retry for HttpClient
        private static HttpClient _notSoNicelyInstantiatedHttpClient = new HttpClient(new RetryPolicyDelegatingHandler(new HttpClientHandler()
                                                                                                                    {
                                                                                                                        AutomaticDecompression = DecompressionMethods.GZip
            
                                                                                                                    },2));


        public HttpHandlersService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task Run()
        {
            await GetMoviesWithRetryPolicy(_cancellationTokenSource.Token);
        }

        public async Task GetMoviesWithRetryPolicy(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("MoviesClient");

            //var request = new HttpRequestMessage(HttpMethod.Get, "api/movies/030a43b0-f9a5-405a-811c-bf342524b2be");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/movies/5b1c2b4d-48c7-402a-80c3-cc796ad49c6b");


            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        //Log response
                        Console.WriteLine("The requested movie cannot be found");
                        return;

                    case HttpStatusCode.Unauthorized:
                        // Trigger the authentication flow
                        return;
                    default:
                        response.EnsureSuccessStatusCode();
                        break;
                }
            }
        }
    }
}
