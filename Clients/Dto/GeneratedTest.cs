using System.Text.Json.Serialization;

namespace KT_Learn.Clients.Dto
{
    // JSON-контракт, который мы просим модель вернуть в message.content
    // и который затем десериализуем в этот тип.
    public class GeneratedTest
    {
        [JsonPropertyName("questions")]
        public List<GeneratedQuestion> Questions { get; set; } = new();
    }

    public class GeneratedQuestion
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<string> Options { get; set; } = new();

        [JsonPropertyName("correctAnswer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set;  } = string.Empty;
    }
}
