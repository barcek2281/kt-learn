using KT_Learn.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    [Table("pdf_to_test")]
    public class PdfToTest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("uploaded_by")]
        public Guid UploadedBy { get; set; }

        [ForeignKey(nameof(UploadedBy))]
        public User? Uploader { get; set; }

        [Column("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        [Column("file_name")]
        public string FileName { get; set; } = string.Empty;

        [Column("file_size")]
        public int FileSize { get; set; }

        [Column("status")]
        public PdfStatus Status { get; set; } = PdfStatus.Uploaded;

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("model")]
        public string? Model { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
    }
}
