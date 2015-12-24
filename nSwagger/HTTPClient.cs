﻿    public class SwaggerHTTPClientOptions
    {
        public TimeSpan Timeout { get; }
    
        public SwaggerHTTPClientOptions(TimeSpan timeout)
        {
            Timeout = timeout;
        }
    }
    
    public interface ISwaggerHTTPClient
    {
        Task<HttpResponseMessage> PutAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content = null, string token = null);
        Task<HttpResponseMessage> PostAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content = null, string token = null);
        Task<HttpResponseMessage> HeadAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null);
        Task<HttpResponseMessage> OptionsAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null);
        Task<HttpResponseMessage> PatchAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content, string token = null);
        Task<HttpResponseMessage> DeleteAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null);
        Task<HttpResponseMessage> GetAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null);
    }
    
    class SwaggerHTTPClient : ISwaggerHTTPClient
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This is done interntionally as each place that calls this will dispose it")]
        private static HttpClient CreateClient()
        {
            var cookieJar = new CookieContainer();
            var httpHandler = new HttpClientHandler
            {
                CookieContainer = cookieJar,
                AllowAutoRedirect = true,
                UseCookies = true
            };
    
            var client = new HttpClient(httpHandler, true);
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                Private = true,
                ProxyRevalidate = true,
                MustRevalidate = true
            };
    
            return client;
        }
    
        public async Task<HttpResponseMessage> PutAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content = null, string token = null) => await HTTPCallAsync("put", uri, httpOptions, content, token);
    
        public async Task<HttpResponseMessage> PostAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content = null, string token = null) => await HTTPCallAsync("post", uri, httpOptions, content, token);
    
        private static async Task<HttpResponseMessage> HTTPCallAsync(string method, Uri uri, SwaggerHTTPClientOptions options, HttpContent content = null, string token = null)
        {
            using (var client = CreateClient())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(options.Timeout))
                {
                    var errorMessage = string.Empty;
                    try
                    {
                        if (content != null)
                        {
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        }
    
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }
    
                        var response = default(HttpResponseMessage);
                        switch (method.ToUpperInvariant())
                        {
                            case "DELETE":
                                {
                                    response = await client.DeleteAsync(uri, cancellationTokenSource.Token);
                                    break;
                                }
                            case "POST":
                                {
                                    response = await client.PostAsync(uri, content, cancellationTokenSource.Token);
                                    break;
                                }
                            case "PUT":
                                {
                                    response = await client.PutAsync(uri, content, cancellationTokenSource.Token);
                                    break;
                                }
                            case "GET":
                                {
                                    response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationTokenSource.Token);
                                    break;
                                }
                            case "HEAD":
                                {
                                    response = await client.SendAsync(new HttpRequestMessage
                                    {
                                        Method = new HttpMethod(method),
                                        RequestUri = uri
                                    }, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
    
                                    break;
                                }
                            case "OPTIONS":
                                {
                                    response = await client.SendAsync(new HttpRequestMessage
                                    {
                                        Method = new HttpMethod(method),
                                        RequestUri = uri
                                    }, HttpCompletionOption.ResponseContentRead, cancellationTokenSource.Token);
    
                                    break;
                                }
                            case "PATCH":
                                {
                                    response = await client.SendAsync(new HttpRequestMessage
                                    {
                                        Method = new HttpMethod(method),
                                        RequestUri = uri,
                                        Content = content
                                    }, HttpCompletionOption.ResponseContentRead, cancellationTokenSource.Token);
    
                                    break;
                                }
                        }
    
    #if DEBUG
                                    Debug.WriteLine($"HTTP {method} to {uri} returned {response.StatusCode} with content {await response.Content?.ReadAsStringAsync()}");
    #endif
                        return response;
                    }
                    catch (FileNotFoundException) { errorMessage = $"HTTP {method} exception - file not found exception"; /* this can happen if WP cannot resolve the server */ }
                    catch (WebException) { errorMessage = $"HTTP {method} exception - web exception"; }
                    catch (HttpRequestException) { errorMessage = $"HTTP {method} exception - http exception"; }
                    catch (TaskCanceledException) { errorMessage = $"HTTP {method} exception - task cancelled exception"; }
                    catch (UnauthorizedAccessException) { errorMessage = $"HTTP {method} exception - unauth exception"; }
    
    #if DEBUG
                                Debug.WriteLine(errorMessage);
    #endif
                }
            } 
    
            return null;
        }
    
        public async Task<HttpResponseMessage> HeadAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null) => await HTTPCallAsync("head", uri, httpOptions, token: token);
    
        public async Task<HttpResponseMessage> OptionsAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null) => await HTTPCallAsync("options", uri, httpOptions, token: token);
    
        public async Task<HttpResponseMessage> PatchAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, HttpContent content, string token = null) => await HTTPCallAsync("patch", uri, httpOptions, content, token: token);
    
        public async Task<HttpResponseMessage> DeleteAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null) => await HTTPCallAsync("delete", uri, httpOptions, token: token);
    
        public async Task<HttpResponseMessage> GetAsync(Uri uri, SwaggerHTTPClientOptions httpOptions, string token = null) => await HTTPCallAsync("get", uri, httpOptions, token: token);
    }
    
    public class APIResponse<T>
    {
        public APIResponse(dynamic data, HttpStatusCode statusCode) : this(statusCode)
        {
            Data = data;
        }
    
        public APIResponse(T successData, HttpStatusCode statusCode) : this(statusCode)
        {
            SuccessData = successData;
            SuccessDataAvailable = true;
        }
    
        public bool Success { get; }
    
        public APIResponse(bool success)
        {
            Success = success;
        }
    
        public APIResponse(HttpStatusCode statusCode) : this((int)statusCode >= 200 && (int)statusCode <= 299)
        {
            HTTPStatusCode = statusCode;
        }
    
        public dynamic Data { get; }
    
        public T SuccessData { get; }
    
        public HttpStatusCode? HTTPStatusCode { get; }
    
        public bool SuccessDataAvailable { get; }
    }
