using askaLib.Line.Notify.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace askaLib.Line.Notify
{
    public class LineNotifyService
    {
        private const string LineNotifyAPI = "https://notify-api.line.me/api/notify";
        private const string LineTokenAPI = "https://notify-bot.line.me/oauth/token";
        private const string LineStatusAPI = "https://notify-api.line.me/api/status";
        private const string LineRevokeAPI = "https://notify-api.line.me/api/revoke";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LineNotifySetting _lineNotifySetting;
        public LineNotifyService(IHttpClientFactory httpClientFactory,
                                 LineNotifySetting lineNotifySetting)
        {
            _httpClientFactory = httpClientFactory;
            _lineNotifySetting = lineNotifySetting;
        }

        public string GetAuthorizationRedirectUrl()
        {
            var URL = "https://notify-bot.line.me/oauth/authorize?";
            URL += "response_type=code";
            URL += $"&client_id={_lineNotifySetting.Client_ID}";
            URL += $"&redirect_uri={_lineNotifySetting.Callback_URL}";
            URL += "&scope=notify";
            URL += "&state=NO_STATE";
            return URL;
        }

        public async Task<string> GetUserTokenAsync(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = new TimeSpan(0, 0, 60);
            client.BaseAddress = new Uri(LineTokenAPI);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", _lineNotifySetting.Callback_URL),
                new KeyValuePair<string, string>("client_id", _lineNotifySetting.Client_ID),
                new KeyValuePair<string, string>("client_secret", _lineNotifySetting.Client_Secret)
            });
            var response = await client.PostAsync("", content);
            if (!response.IsSuccessStatusCode) throw new Exception(response.StatusCode.ToString());

            var data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(data)?["access_token"]?.ToString();
        }

        public async Task<string> GetUserNameAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = new TimeSpan(0, 0, 60);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(LineStatusAPI);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(data)?["target"]?.ToString();
        }

        public async Task SendTextAsync(string token, string message)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = new TimeSpan(0, 0, 60);
            client.BaseAddress = new Uri(LineNotifyAPI);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            StringContent content = new StringContent($"message={message}",
                                                      System.Text.Encoding.UTF8,
                                                      "application/x-www-form-urlencoded");
            var response = await client.PostAsync("", content);

            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception(responseContent);
            }
        }

        public async Task RevokeAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = new TimeSpan(0, 0, 60);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            StringContent content = new StringContent("",
                                                      System.Text.Encoding.UTF8,
                                                      "application/x-www-form-urlencoded");
            var response = await client.PostAsync(LineRevokeAPI, content);
            if (response.IsSuccessStatusCode) return;

            var responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception(responseContent);
        }
    }
}
