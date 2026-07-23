namespace KT_Learn.Controllers.Dtos
{
    public record AITestMakerRequest(
        string Model,
        //string TestTitle,

        string Prompt,
        IFormFile PdfFile
        );
}