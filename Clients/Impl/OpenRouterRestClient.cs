using System.Text.Json;
using KT_Learn.Clients.Dto;
using KT_Learn.Options;
using Microsoft.Extensions.Options;

namespace KT_Learn.Clients.Impl
{
    public class OpenRouterRestClient : IAIPdfTestClient
    {
        private readonly string _url;
        private readonly string _api;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterRestClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public OpenRouterRestClient(
            HttpClient client,
            IOptions<OpenRouterOptions> options,
            ILogger<OpenRouterRestClient> logger)
        {
            _url = options.Value.Url;
            _api = options.Value.Api;
            _httpClient = client;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(_url);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _api);
        }

        public async Task<AiPdfTestReponse> CreateTest(AiPdfTestRequest request, CancellationToken ct)
        {
            _logger.LogInformation(
                "Запрос теста в OpenRouter: модель {Model}, файл {FileName} ({Size} байт)",
                request.Model, request.File.FileName, request.File.Length);

            // PDF -> base64 data URL, чтобы отправить сам файл прямо в OpenRouter.
            var pdfDataUrl = await ToDataUrlAsync(request.File, ct);

            // Инструкция модели: вернуть строго JSON нужной структуры.
            var instruction =
                "Ты составляешь тест по содержимому PDF. " +
                "Верни СТРОГО JSON без markdown в формате и без ```json```: " +
                "{\"questions\":[{\"question\":string,\"options\":[string,...],\"correctAnswer\":string, \"type\":[string (multiple or single answer)]}]}. " +
                "Поле correctAnswer должно совпадать с одним из options." + 
                "Ответы могут лежать в конце файла, а может и нет";

            var userPrompt = string.IsNullOrWhiteSpace(request.AdditionalPrompt)
                ? "Прочитав пдф файл составь."
                : request.AdditionalPrompt;

            var rq = new
            {
                model = request.Model,
                // Заставляем модель отвечать валидным JSON-объектом.
                response_format = new { type = "json_object" },
                // Плагин извлекает текст из PDF (движок pdf-text — бесплатный).
                plugins = new object[]
                {
                    new { id = "file-parser", pdf = new { engine = "mistral-ocr" } }
                },
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = $"{instruction}\n\n{userPrompt}" },
                            new
                            {
                                type = "file",
                                file = new
                                {
                                    filename = request.File.FileName,
                                    file_data = pdfDataUrl
                                }
                            }
                        }
                    }
                }
            };

            // _url — уже полный адрес /chat/completions, поэтому шлём на абсолютный URL.
            using var responseFromAi = await _httpClient.PostAsJsonAsync(_url, rq, ct);
            if (!responseFromAi.IsSuccessStatusCode)
            {
                var error = await responseFromAi.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"OpenRouter {(int)responseFromAi.StatusCode}: {error}");
            }

            var parsed = await responseFromAi.Content.ReadFromJsonAsync<AiResponse>(ct);
            var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HttpRequestException("OpenRouter вернул пустой ответ");
            }

            _logger.LogInformation("AI response: {}", content);
            // content — это JSON-строка с тестом; парсим её в объекты.
            var test = JsonSerializer.Deserialize<GeneratedTest>(StripJsonFences(content), JsonOptions)
                       ?? new GeneratedTest();

            var questions = test.Questions
                .Select(q => new TestQuestion(q.Question, q.Options, q.CorrectAnswer, q.Type))
                .ToList();

            return new AiPdfTestReponse(questions);
        }

        private static async Task<string> ToDataUrlAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var base64 = Convert.ToBase64String(ms.ToArray());
            return $"data:application/pdf;base64,{base64}";
        }

        // На случай если модель всё же обернёт JSON в ```json ... ```.
        private static string StripJsonFences(string content)
        {
            var trimmed = content.Trim();
            if (trimmed.StartsWith("```"))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline >= 0)
                {
                    trimmed = trimmed[(firstNewline + 1)..];
                }
                if (trimmed.EndsWith("```"))
                {
                    trimmed = trimmed[..^3];
                }
            }
            return trimmed.Trim();
        }
    }
}
