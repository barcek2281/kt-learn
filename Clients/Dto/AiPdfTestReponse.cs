namespace KT_Learn.Clients.Dto
{
    public record AiPdfTestReponse(List<TestQuestion> Questions);

    public record TestQuestion(
        string Question,
        List<string> Options,
        string CorrectAnswer,
        string type
    );
}
