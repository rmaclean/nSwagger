namespace API
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class HTTP
    {
        public async Task<WrappedResponse<Stream>> GetStream(Uri uri, int attempt = -1)
        {
            attempt++;
            var response = await Get(uri);
            if (response != null)
            {
                var result = await response.Content.ReadAsStreamAsync();
                return new WrappedResponse<Stream>(response.IsSuccessStatusCode, result);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await GetStream(uri, attempt);
            }

            return null;
        }

        public async Task<WrappedResponse<string>> GetString(Uri uri, int attempt = -1, string token = null)
        {
            attempt++;
            var response = await Get(uri, token);
            if (response != null)
            {
                var result = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Uri: " + uri);
                Debug.WriteLine("Response: " + result);
                return new WrappedResponse<string>(response.IsSuccessStatusCode, result);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await GetString(uri, attempt, token);
            }

            return null;
        }

        private async Task<HttpResponseMessage> Get(Uri uri, string token = null)
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
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
}