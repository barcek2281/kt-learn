using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    [Table("themes")]
    public class Themes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        private Guid Id { get; set; } = Guid.CreateVersion7();

        [Column("code")]
        private string Code { get; set; } = string.Empty;

        [Column("title")]
        private string Title { get; set;  }     = string.Empty;

        [Column("is_active")]
        private bool IsActive { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
