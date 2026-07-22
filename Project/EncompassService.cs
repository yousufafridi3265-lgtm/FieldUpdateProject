
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EncompassIntegration
{
    public class EncompassService
    {
        private readonly HttpClient _httpClient;

        public EncompassService(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        // Method 1: XML Config File ko parse karke Configuration Model Return karna
        public AppConfigurationModel LoadConfiguration(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException($"Config file not found: {xmlFilePath}");
            }

            XDocument doc = XDocument.Load(xmlFilePath);

            // Parsing <EncompassInfo>
            XElement infoNode = doc.Root?.Element("EncompassInfo");
            var infoModel = new EncompassInfoModel
            {
                ApiServer = infoNode?.Element("ApiServer")?.Value?.Trim(),
                ClientId = infoNode?.Element("ClientId")?.Value?.Trim(),
                ClientSecret = infoNode?.Element("ClientSecret")?.Value?.Trim(),
                InstanceId = infoNode?.Element("InstanceId")?.Value?.Trim(),
                GrantType = infoNode?.Element("grant_type")?.Value?.Trim(),
                Scope = infoNode?.Element("Scope")?.Value?.Trim()
            };

            // Parsing <FieldUpdate>
            XElement fieldUpdateNode = doc.Root?.Element("FieldUpdate");
            XElement fieldsNode = fieldUpdateNode?.Element("Fields");

            var fieldUpdateModel = new FieldUpdateConfigModel
            {
                FilterJson = fieldUpdateNode?.Element("Filters")?.Value?.Trim(),
                FieldId = fieldsNode?.Attribute("id")?.Value,
                FieldValue = fieldsNode?.Attribute("value")?.Value
            };

            return new AppConfigurationModel
            {
                EncompassInfo = infoModel,
                FieldUpdate = fieldUpdateModel
            };
        }

        // Method 2: Access Token Retrieve karna
        public async Task<string> GetAccessTokenAsync(EncompassInfoModel config)
        {
            string baseUrl = config.ApiServer.TrimEnd('/');
            string requestUrl = $"{baseUrl}/oauth2/v1/token";

            var payload = new Dictionary<string, string>
            {
                { "grant_type", config.GrantType },
                { "client_id", config.ClientId },
                { "client_secret", config.ClientSecret },
                { "instance_id", config.InstanceId },
                { "scope", config.Scope }
            };

            var requestContent = new FormUrlEncodedContent(payload);

            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, requestContent);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Token Request Failed ({response.StatusCode}): {responseBody}");
            }

            var tokenResult = JsonSerializer.Deserialize<TokenResponseModel>(responseBody);

            if (string.IsNullOrEmpty(tokenResult?.access_token))
            {
                throw new Exception("Response received but 'access_token' is missing or empty.");
            }

            return tokenResult.access_token;
        }

        // 1. Pipeline API se Loan GUID dhoondne ka method
        public async Task<string> SearchLoanGuidAsync(string apiServer, string accessToken, string filterJson)
        {
            string baseUrl = apiServer.TrimEnd('/');
            string requestUrl = $"{baseUrl}/encompass/v3/loanPipeline?limit=10&include=LockInfo";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(filterJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Pipeline Search Failed ({response.StatusCode}): {responseBody}");
            }

            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    if (root[0].TryGetProperty("loanGuid", out JsonElement loanGuidElement))
                    {
                        return loanGuidElement.GetString();
                    }
                    if (root[0].TryGetProperty("loanId", out JsonElement loanIdElement))
                    {
                        return loanIdElement.GetString();
                    }
                }
            }

            throw new Exception("Loan search completed but no matching loan was found.");
        }

        // 2. Loan Field update karne ka method
        public async Task<bool> UpdateLoanFieldAsync(string apiServer, string accessToken, string loanGuid, string fieldId, string fieldValue)
        {
            string baseUrl = apiServer.TrimEnd('/');

            // Target the working /fieldWriter endpoint
            string requestUrl = $"{baseUrl}/encompass/v3/loans/{loanGuid}/fieldWriter";


            // Encompass Key-Value Array Format
            var updatePayload = new[]
            {
        new
        {
            id = fieldId,
            value = fieldValue
        }
    };
            Console.WriteLine(updatePayload);
            string jsonBody = JsonSerializer.Serialize(updatePayload);

            // FIX 1: Change HttpMethod.Patch -> HttpMethod.Post
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");


            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Loan Field Update Failed ({response.StatusCode}): {responseBody}");
            }

            return true;
        }

    }
}