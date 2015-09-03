namespace API
{
    using System.Net;
    using System.Net.Http;

    public partial class HTTP
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This is done interntionally as each place that calls this will dispose it")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2930:\"IDisposables\" should be disposed", Justification = "<Pending>")]
        private HttpClient CreateClient()
        {
            var cookieJar = new CookieContainer();
            var httpHandler = new HttpClientHandler
            {
                CookieContainer = cookieJar,
                AllowAutoRedirect = true,
                UseCookies = true
            };

            var client = new HttpClient(httpHandler, true);
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true,
                Private = true,
                ProxyRevalidate = true,
                MustRevalidate = true,
            };

            return client;
        }
    }
}