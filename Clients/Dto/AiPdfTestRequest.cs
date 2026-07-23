namespace KT_Learn.Clients.Dto
{
    public class AiPdfTestRequest(string model, IFormFile file, string additionalPrompt)
    {
        public string Model = model;
        public IFormFile File = file;
        public string AdditionalPrompt = additionalPrompt;
    }
}