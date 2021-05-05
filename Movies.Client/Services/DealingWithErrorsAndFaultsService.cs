﻿using Marvin.StreamExtensions;
using Movies.Client.Models;
using Newtonsoft.Json;
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
    public class DealingWithErrorsAndFaultsService : IIntegrationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DealingWithErrorsAndFaultsService(IHttpClientFactory httpClientFactory )
        {
            _httpClientFactory = httpClientFactory;
        }


        public async Task Run()
        {
            //await GetMovieAndDealWithInvalidResponse(_cancellationTokenSource.Token);
            await PostMovieAndHandleValidationErrors(_cancellationTokenSource.Token);
        }

        private async Task GetMovieAndDealWithInvalidResponse(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("MoviesClient");

            var request = new HttpRequestMessage(HttpMethod.Get, "api/movies/030a43b0-f9a5-405a-811c-bf342524b2be");
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
               

                var stream = await response.Content.ReadAsStreamAsync();
                var movie = stream.ReadAndDeserializeFromJson<Movie>();
            }

        }

        private async Task PostMovieAndHandleValidationErrors(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("MoviesClient");

            var movieForCreation = new MovieForCreation()
            {
                Title = "Pulp Fiction",
                Description = "Fury",
                DirectorId = Guid.Parse("d28888e9-2ba9-473a-a40f-e38cb54f9b35"),
                ReleaseDate = new DateTimeOffset(new DateTime(1992, 9, 2)),
                Genre = "Crime, Drama"
            };

            var serializedMovieForCreation = JsonConvert.SerializeObject(movieForCreation);

            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/movies"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Content = new StringContent(serializedMovieForCreation);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                        {
                            var errorStream = await response.Content.ReadAsStreamAsync();
                            var validationErrors = errorStream.ReadAndDeserializeFromJson();
                            Console.WriteLine(validationErrors);
                            return;
                        }
                        else
                        {
                            response.EnsureSuccessStatusCode();
                        }
                    }

                   

                    var stream = await response.Content.ReadAsStreamAsync();
                    var movie = stream.ReadAndDeserializeFromJson<Movie>();
                }


            }
            
        }
    }
}
