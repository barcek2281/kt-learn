using KT_Learn.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    // ai_draft_tasks — отдельная задача (вопрос) внутри черновика теста.
    [Table("ai_draft_tasks")]
    public class AiDraftTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("ai_draft_test_id")]
        public Guid AiDraftTestId { get; set; }

        [ForeignKey(nameof(AiDraftTestId))]
        public AiDraftTest? DraftTest { get; set; }

        [Column("theme_id")]
        public Guid? ThemeId { get; set; }

        [Column("theme_raw")]
        public string? ThemeRaw { get; set; }

        [Column("type")]
        public TaskType Type { get; set; }

        [Column("question")]
        public string Question { get; set; } = string.Empty;

        [Column("options")]
        public List<string> Options { get; set; } = new();

        [Column("correct_answer")]
        public List<string> CorrectAnswer { get; set; } = new();

        [Column("points")]
        public int Points { get; set; } = 1;

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
