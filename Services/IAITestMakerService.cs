using KT_Learn.Clients.Dto;
using KT_Learn.Controllers.Dtos;
using KT_Learn.Models;

namespace KT_Learn.Services
{
    public interface IAITestMakerService
    {
        Task<AiDraftTest> AITestMakeFromPDF(AITestMakerRequest request, CancellationToken ct);
        Task<List<AiDraftTest>> GetAIDraftTest();
    }
}
