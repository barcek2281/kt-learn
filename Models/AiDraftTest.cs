using KT_Learn.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    // ai_draft_test — черновик теста, сгенерированный ИИ из PDF, до ревью админом.
    [Table("ai_draft_test")]
    public class AiDraftTest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("pdf_to_test_id")]
        public Guid PdfToTestId { get; set; }

        [ForeignKey(nameof(PdfToTestId))]
        public PdfToTest? Pdf { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("review_status")]
        public DraftReviewStatus ReviewStatus { get; set; } = DraftReviewStatus.Pending;

        [Column("review_comment")]
        public string? ReviewComment { get; set; }

        [Column("reviewed_by")]
        public Guid? ReviewedBy { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public User? Reviewer { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        // Ссылка на созданный тест (tests) появляется в Script0007. Модели Test
        // пока нет, поэтому здесь только скалярная колонка без навигации.
        [Column("test_id")]
        public Guid? TestId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Задачи черновика (one-to-many, ON DELETE CASCADE в схеме).
        public List<AiDraftTask> Tasks { get; set; } = new();
    }
}
