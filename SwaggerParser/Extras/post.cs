namespace API
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

    public partial class HTTP
    {
        public async Task<WrappedResponse<string>> PostForm(Uri uri, IEnumerable<KeyValuePair<string, string>> values, int attempt = -1, string token = null)
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
                var response = await Post(uri, form, token, "application/x-www-form-urlencoded");
                if (response != null)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return new WrappedResponse<string>(response.IsSuccessStatusCode, result);
                }
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostForm(uri, values, attempt, token);
            }

            return null;
        }

        public async Task<WrappedResponse<string>> PostString(Uri uri, string content = null, int attempt = -1, string token = null)
        {
            attempt++;
            var modifiedContent = string.IsNullOrWhiteSpace(content) ? "" : content;
            Debug.WriteLine(modifiedContent);

            var response = await Post(uri, new StringContent(modifiedContent), token);

#if DEBUG
            Debug.WriteLine(uri);
            if (modifiedContent.GetType() == typeof(StringContent))
            {
                Debug.WriteLine(modifiedContent);
            }

            Debug.WriteLine(response);
#endif

            if (response != null)
            {
                var result = await response.Content.ReadAsStringAsync();
                return new WrappedResponse<string>(response.IsSuccessStatusCode, result);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostString(uri, modifiedContent, attempt, token);
            }

            return null;
        }

        public async Task<WrappedResponse<object>> PostBool(Uri uri, string content = null, int attempt = -1, string token = null)
        {
            attempt++;
            var modifiedContent = string.IsNullOrWhiteSpace(content) ? "" : content;
            Debug.WriteLine(modifiedContent);

            var response = await Post(uri, new StringContent(modifiedContent), token);

#if DEBUG
            Debug.WriteLine(uri);
            if (modifiedContent.GetType() == typeof(StringContent))
            {
                Debug.WriteLine(modifiedContent);
            }

            Debug.WriteLine(response);
#endif

            if (response != null)
            {
                return new WrappedResponse<object>(response.IsSuccessStatusCode, null);
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return await PostBool(uri, modifiedContent, attempt, token);
            }

            return null;
        }

        private async Task<HttpResponseMessage> Post(Uri uri, HttpContent content, string token = null, string mediaType = "application/json")
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
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }

                        var response = await client.PostAsync(uri, content, cancellationTokenSource.Token);
#if DEBUG
                        Debug.WriteLine(uri);
                        if (content.GetType() == typeof(StringContent))
                        {
                            Debug.WriteLine(content);
                        }

                        Debug.WriteLine(response);
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
    }
}