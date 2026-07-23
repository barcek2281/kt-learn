using KT_Learn.Clients.Dto;

namespace KT_Learn.Clients
{
    public interface IAIPdfTestClient
    {
        Task<AiPdfTestReponse> CreateTest(AiPdfTestRequest request, CancellationToken ct);
    }
}
