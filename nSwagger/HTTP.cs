namespace nSwagger.HTTP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public static class HTTP
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

        public static async Task<HTTPResponse<string>> DeleteStringAsync(Uri uri, int attempt = -1, string token = null)
        {
            attempt++;
            var response = await DeleteAsync(uri, token);

#if DEBUG
            Debug.WriteLine("URI: " + uri);
            Debug.WriteLine("Status Code: " + response.StatusCode);
#endif

            if (response != null)
            {
                var result = await response.Content.ReadAsStringAsync();
                return new HTTPResponse<string>(result, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await DeleteStringAsync(uri, attempt, token);
            }

            return null;
        }

        public static async Task<HTTPResponse<string>> PostFormAsync(Uri uri, IEnumerable<KeyValuePair<string, string>> values, int attempt = -1, string token = null)
        {
            attempt++;
#if DEBUG
            Debug.WriteLine("POST:");
            foreach (var item in values)
            {
                Debug.WriteLine("{0} = {1}", item.Key, item.Value);
            }
#endif
            using (var form = new FormUrlEncodedContent(values))
            {
                var response = await PostAsync(uri, form, token, "application/x-www-form-urlencoded");
                if (response != null)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return new HTTPResponse<string>(result, response.StatusCode);
                }
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostFormAsync(uri, values, attempt, token);
            }

            return null;
        }

        public static async Task<HTTPResponse<string>> PutStringAsync(Uri uri, string content = null, int attempt = -1, string token = null)
        {
            attempt++;
            var modifiedContent = string.IsNullOrWhiteSpace(content) ? "" : content;
            Debug.WriteLine(modifiedContent);

            var response = await PutAsync(uri, new StringContent(modifiedContent), token);

#if DEBUG
            Debug.WriteLine(uri);
            if (modifiedContent.GetType() == typeof(StringContent))
            {
                Debug.WriteLine(modifiedContent);
            }

            Debug.WriteLine(await response.Content.ReadAsStringAsync());
#endif

            if (response != null)
            {
                var result = await response.Content.ReadAsStringAsync();
                return new HTTPResponse<string>(result, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PutStringAsync(uri, modifiedContent, attempt, token);
            }

            return null;
        }

        public static async Task<HTTPResponse<string>> PostStringAsync(Uri uri, string content = null, int attempt = -1, string token = null)
        {
            attempt++;
            var modifiedContent = string.IsNullOrWhiteSpace(content) ? "" : content;
            Debug.WriteLine(modifiedContent);

            var response = await PostAsync(uri, new StringContent(modifiedContent), token);

#if DEBUG
            Debug.WriteLine(uri);
            if (modifiedContent.GetType() == typeof(StringContent))
            {
                Debug.WriteLine(modifiedContent);
            }

            Debug.WriteLine(await response.Content.ReadAsStringAsync());
#endif

            if (response != null)
            {
                var result = await response.Content.ReadAsStringAsync();
                return new HTTPResponse<string>(result, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostStringAsync(uri, modifiedContent, attempt, token);
            }

            return null;
        }

        public static async Task<HTTPResponse<object>> PostBoolAsync(Uri uri, string content = null, int attempt = -1, string token = null)
        {
            attempt++;
            var modifiedContent = string.IsNullOrWhiteSpace(content) ? "" : content;
            Debug.WriteLine(modifiedContent);

            var response = await PostAsync(uri, new StringContent(modifiedContent), token);

#if DEBUG
            Debug.WriteLine(uri);
            if (modifiedContent.GetType() == typeof(StringContent))
            {
                Debug.WriteLine(modifiedContent);
            }

            Debug.WriteLine(await response.Content.ReadAsStringAsync());
#endif

            if (response != null)
            {
                return new HTTPResponse<object>(response.IsSuccessStatusCode, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostBoolAsync(uri, modifiedContent, attempt, token);
            }

            return null;
        }

        private static async Task<HttpResponseMessage> PutAsync(Uri uri, HttpContent content, string token = null, string mediaType = "application/json")
        {
            using (var client = CreateClient())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    var errorMessage = string.Empty;
                    try
                    {
                        content.Headers.ContentType.MediaType = mediaType;

                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }

                        var response = await client.PutAsync(uri, content, cancellationTokenSource.Token);
#if DEBUG
                        Debug.WriteLine(uri);
                        if (content.GetType() == typeof(StringContent))
                        {
                            Debug.WriteLine(content);
                        }

                        Debug.WriteLine(await response.Content.ReadAsStringAsync());
#endif
                        return response;
                    }
                    catch (FileNotFoundException) { errorMessage = "HTTP PUT exception - file not found exception"; /* this can happen if WP cannot resolve the server */ }
                    catch (WebException) { errorMessage = "HTTP PUT exception - web exception"; }
                    catch (HttpRequestException) { errorMessage = "HTTP PUT exception - http exception"; }
                    catch (TaskCanceledException) { errorMessage = "HTTP PUT exception - task cancelled exception"; }
                    catch (UnauthorizedAccessException) { errorMessage = "HTTP PUT exception - unauth exception"; }

#if DEBUG
                    Debug.WriteLine(errorMessage);
#endif
                }
            }

            return null;
        }

        private static async Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content, string token = null, string mediaType = "application/json")
        {
            using (var client = CreateClient())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    var errorMessage = string.Empty;
                    try
                    {
                        content.Headers.ContentType.MediaType = mediaType;

                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }

                        var response = await client.PostAsync(uri, content, cancellationTokenSource.Token);
#if DEBUG
                        Debug.WriteLine(uri);
                        if (content.GetType() == typeof(StringContent))
                        {
                            Debug.WriteLine(content);
                        }

                        Debug.WriteLine(await response.Content.ReadAsStringAsync());
#endif
                        return response;
                    }
                    catch (FileNotFoundException) { errorMessage = "HTTP Post exception - file not found exception"; /* this can happen if WP cannot resolve the server */ }
                    catch (WebException) { errorMessage = "HTTP Post exception - web exception"; }
                    catch (HttpRequestException) { errorMessage = "HTTP Post exception - http exception"; }
                    catch (TaskCanceledException) { errorMessage = "HTTP Post exception - task cancelled exception"; }
                    catch (UnauthorizedAccessException) { errorMessage = "HTTP Post exception - unauth exception"; }

#if DEBUG
                    Debug.WriteLine(errorMessage);
#endif
                }
            }

            return null;
        }

        private static async Task<HttpResponseMessage> DeleteAsync(Uri uri, string token = null)
        {
            using (var client = CreateClient())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    var errorMessage = string.Empty;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }

                        return await client.DeleteAsync(uri, cancellationTokenSource.Token);
                    }
                    catch (FileNotFoundException) { errorMessage = "HTTP DELETE exception - file not found exception"; /* this can happen if WP cannot resolve the server */ }
                    catch (WebException) { errorMessage = "HTTP DELETE exception - web exception"; }
                    catch (HttpRequestException) { errorMessage = "HTTP DELETE exception - http exception"; }
                    catch (TaskCanceledException) { errorMessage = "HTTP DELETE exception - task cancelled exception"; }
                    catch (UnauthorizedAccessException) { errorMessage = "HTTP DELETE exception - unauth exception"; }

#if DEBUG
                    Debug.WriteLine(errorMessage);
#endif
                }
            }

            return null;
        }

        public static async Task<HTTPResponse<Stream>> GetStreamAsync(Uri uri, int attempt = -1)
        {
            attempt++;
            var response = await GetAsync(uri);
            Debug.WriteLine("Uri: " + uri);
            if (response != null)
            {
                Debug.WriteLine("HTTP Status Code: " + response.StatusCode);
                var result = await response.Content.ReadAsStreamAsync();
                return new HTTPResponse<Stream>(result, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await GetStreamAsync(uri, attempt);
            }

            return null;
        }

        public static async Task<HTTPResponse<string>> GetStringAsync(Uri uri, int attempt = -1, string token = null)
        {
            attempt++;
            Debug.WriteLine("Uri: " + uri);
            var response = await GetAsync(uri, token);
            if (response != null)
            {
                Debug.WriteLine("HTTP Status Code: " + response.StatusCode);
                var result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Response: " + result);
                return new HTTPResponse<string>(result, response.StatusCode);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await GetStringAsync(uri, attempt, token);
            }

            return null;
        }

        private static async Task<HttpResponseMessage> GetAsync(Uri uri, string token = null)
        {
            using (var client = CreateClient())
            {
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    var errorMessage = string.Empty;
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        }

                        return await client.GetAsync(uri, cancellationTokenSource.Token);
                    }
                    catch (WebException) { errorMessage = "HTTP Get exception - web exception"; }
                    catch (HttpRequestException) { errorMessage = "HTTP Get exception - http exception"; }
                    catch (TaskCanceledException) { errorMessage = "HTTP Get exception - task exception"; }
                    catch (UnauthorizedAccessException) { errorMessage = "HTTP Get exception - un auth exception"; }
#if DEBUG
                    Debug.WriteLine(errorMessage);
#endif
                }
            }

            return null;
        }
    }

    public class HTTPResponse<T>
    {
        public HTTPResponse(T data, HttpStatusCode statusCode) : this(statusCode)
        {
            Data = data;
        }

        public HTTPResponse(HttpStatusCode statusCode)
        {
            HTTPStatusCode = statusCode;
        }

        public T Data { get; }

        public HttpStatusCode? HTTPStatusCode { get; }
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

        public APIResponse()
        {
        }

        public APIResponse(HttpStatusCode statusCode) : this()
        {
            HTTPStatusCode = statusCode;
        }

        public dynamic Data { get; }

        public T SuccessData { get; }

        public HttpStatusCode? HTTPStatusCode { get; }

        public bool SuccessDataAvailable { get; }
    }
}