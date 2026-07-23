using System.Text.Json.Serialization;

namespace KT_Learn.Clients.Dto
{
    public class AiResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }
    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
