using Marvin.StreamExtensions;
using Movies.Client.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Movies.Client
{
    public class TestableClassWithApiAccess
    {
        private readonly HttpClient _httpClient;

        public TestableClassWithApiAccess(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Movie> GetMovie(CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/movies/030a43b0-f9a5-405a-811c-bf342524b2be");


            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        //Log response
                        Console.WriteLine("The requested movie cannot be found");
                        return null;

                    case HttpStatusCode.Unauthorized:
                        // Trigger the authentication flow
                        throw new UnauthorizedApiAccessException();

                    default:
                        response.EnsureSuccessStatusCode();
                        break;
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return stream.ReadAndDeserializeFromJson<Movie>();
            }
        }

    }
}
