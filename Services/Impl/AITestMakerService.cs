using System.IdentityModel.Tokens.Jwt;
using KT_Learn.Clients;
using KT_Learn.Clients.Dto;
using KT_Learn.Controllers.Dtos;
using KT_Learn.Data;
using KT_Learn.Models;
using KT_Learn.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace KT_Learn.Services.Impl
{
    public class AITestMakerService : IAITestMakerService
    {
        private readonly IAIPdfTestClient _client;
        private readonly AppDBContext _db;
        private readonly IHttpContextAccessor _http;

        public AITestMakerService(
            IAIPdfTestClient client,
            AppDBContext db,
            IHttpContextAccessor http)
        {
            _client = client;
            _db = db;
            _http = http;
        }

        public async Task<AiDraftTest> AITestMakeFromPDF(AITestMakerRequest request, CancellationToken ct)
        {
            var author = await GetCurrentUserAsync(ct);

            var pdfTest = new PdfToTest
            {
                Uploader = author,
                // TODO: заменить на реальный URL
                FileUrl = request.PdfFile.FileName,
                FileName = request.PdfFile.FileName,
                FileSize = (int)request.PdfFile.Length,
                Model = request.Model
            };
            _db.PdfToTests.Add(pdfTest);
            await _db.SaveChangesAsync(ct);

            // 3. Просим ИИ сгенерировать тест по PDF.
            var aiRequest = new AiPdfTestRequest(request.Model, request.PdfFile, request.Prompt);
            var response = await _client.CreateTest(aiRequest, ct);

            // 4. Раскладываем ответ ИИ в черновик теста и его задачи.
            var draftTest = new AiDraftTest
            {
                Pdf = pdfTest,
                Title = string.IsNullOrWhiteSpace(request.Prompt)
                    ? $"Черновик теста по {request.PdfFile.FileName}"
                    : request.Prompt
            };

            var order = 0;
            foreach (var question in response.Questions)
            {
                if (question.Options is not { Count: >= 2 })
                    continue;

                var correctAnswers = question.CorrectAnswer
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                if (correctAnswers.Count == 0)
                    correctAnswers.Add(question.CorrectAnswer);

                // Тип приводим к числу правильных ответов — так CHECK в БД
                // (single => ровно 1, multi => >= 2) выполняется всегда.
                var type = correctAnswers.Count >= 2 ? TaskType.Multi : TaskType.Single;

                draftTest.Tasks.Add(new AiDraftTask
                {
                    Type = type,
                    Question = question.Question,
                    Options = question.Options,
                    CorrectAnswer = correctAnswers,
                    OrderIndex = order++,
                    Points = 1
                });
            }

            _db.AiDraftTests.Add(draftTest); // задачи из Tasks EF вставит каскадно

            // 5. Помечаем PDF как обработанный.
            pdfTest.Status = PdfStatus.Generated;
            pdfTest.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return draftTest;
        }

        public async Task<List<AiDraftTest>> GetAIDraftTest()
        {
            return await _db.AiDraftTests
                .AsNoTracking()                                    // только чтение
                .Include(d => d.Tasks.OrderBy(t => t.OrderIndex))  // иначе Tasks не загрузятся
                .ToListAsync();
        }

        // Текущий пользователь из JWT: claim sub хранит user.Id (см. TokenService).
        private async Task<User> GetCurrentUserAsync(CancellationToken ct)
        {
            var sub = _http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Не удалось определить пользователя из токена.");

            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                ?? throw new UnauthorizedAccessException("Пользователь из токена не найден.");
        }
    }
}
