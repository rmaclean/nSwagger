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
        public async Task<WrappedResponse<string>> DeleteString(Uri uri, int attempt = -1, string token = null)
        {
            attempt++;
            var response = await Delete(uri, token);

#if DEBUG
            Debug.WriteLine(uri);
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
                return await DeleteString(uri, attempt, token);
            }

            return null;
        }      

        private async Task<HttpResponseMessage> Delete(Uri uri, string token = null)
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
                        
                        
                        var response = await client.DeleteAsync(uri, cancellationTokenSource.Token);
#if DEBUG
                        Debug.WriteLine(uri);
                        Debug.WriteLine(response);
#endif
                        return response;
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
    }
}