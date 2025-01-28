using System.Net.Http;
using System.Text;
using System.Text.Json;
using WpfApp1.Models;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace WpfApp1.Services
{
    public class OpenAIService
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private const int MAX_TOKENS = 4000; // 根据模型调整

        public OpenAIService(AppSettings settings)
        {
            _apiKey = settings.ApiKey;
            _baseUrl = settings.BaseUrl;  // 已修改为BaseUrl
            _model = settings.Model;      // 已修改为Model
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);
            if (!string.IsNullOrEmpty(_baseUrl))
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }
            else 
            {
                _httpClient.BaseAddress = new Uri("https://api.openai.com/");
            }
        }

        public async Task<string> GetCompletion(string prompt)
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API调用失败: {response.StatusCode} - {errorContent}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseString);
            return responseObject.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        public async IAsyncEnumerable<string> GetCompletionStream(List<ChatMessage> messages)
        {
            var requestMessages = messages.Select(m => new
            {
                role = m.IsUser ? "user" : "assistant",
                content = m.Content
            }).ToList();

            var requestBody = new
            {
                model = _model ?? "gpt-3.5-turbo",
                messages = requestMessages,
                stream = true,
                temperature = 0.7,
                max_tokens = MAX_TOKENS
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, 
                HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"API调用失败: {response.StatusCode} - {errorContent}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                string contentStr = null;
                
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    
                    // 检查是否存在错误信息
                    if (doc.RootElement.TryGetProperty("error", out var error))
                    {
                        var errorMessage = error.GetProperty("message").GetString();
                        throw new Exception($"API返回错误: {errorMessage}");
                    }

                    // 获取choices数组
                    if (!doc.RootElement.TryGetProperty("choices", out var choices))
                    {
                        continue; // 跳过没有choices的消息
                    }

                    if (choices.GetArrayLength() == 0)
                    {
                        continue; // 跳过空choices数组的消息
                    }

                    var choice = choices[0];
                    if (!choice.TryGetProperty("delta", out var delta))
                    {
                        continue; // 跳过没有delta的消息
                    }

                    // 如果delta中没有content，说明这是一个控制消息（如role），跳过
                    if (!delta.TryGetProperty("content", out var content))
                    {
                        continue;
                    }

                    contentStr = content.GetString();
                }
                catch (JsonException ex)
                {
                    throw new Exception($"解析API响应时出错: {ex.Message}\nResponse: {data}");
                }

                if (!string.IsNullOrEmpty(contentStr))
                {
                    yield return contentStr;
                }
            }
        }
    }
} 