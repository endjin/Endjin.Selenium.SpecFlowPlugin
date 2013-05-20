namespace Endjin.Selenium.SpecFlowPlugin
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

    public class SauceRest
    {
        private readonly HttpClient httpClient;

        private readonly string username;
        private readonly string accessKey;
        private readonly string sauceRestUrl;

        public SauceRest(string username, string accessKey, string sauceRestUrl)
        {
            this.username = username;
            this.accessKey = accessKey;
            this.sauceRestUrl = sauceRestUrl;

            this.httpClient = new HttpClient();

            var auth = EncodeTo64(username + ":" + accessKey);
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        public void SetJobPassed(string jobId)
        {
            var update = new SauceUpdate { passed = true };

            this.UpdateJobInfo(jobId, update);
        }

        public void SetJobFailed(string jobId)
        {
            var update = new SauceUpdate { passed = false };

            this.UpdateJobInfo(jobId, update);
        }

        private void UpdateJobInfo(string jobId, SauceUpdate update)
        {
            var restEndpoint = sauceRestUrl + "/v1/" + username + "/jobs/" + jobId;
            var auth = EncodeTo64(username + ":" + accessKey);

            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var response = httpClient.PutAsJsonAsync(restEndpoint, update).Result;
        }

        private static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.Default.GetBytes(toEncode);

            string encodedTo64 = Convert.ToBase64String(toEncodeAsBytes);

            return encodedTo64;
        }
    }
}