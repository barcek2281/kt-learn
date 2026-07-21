using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    [Table("users")]
    public class User
    {
        // Identity: значение генерирует БД через DEFAULT gen_random_uuid().
        // EF не шлёт id в INSERT и забирает его обратно через RETURNING id.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        
        [Column("password_hash")]
        public string Password { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("role")]
        public Role Role { get; set; } = Role.Student;
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
