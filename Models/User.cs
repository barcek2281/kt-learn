using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KT_Learn.Models
{
    [Table("users")]
    public class User
    {
        // None, а не Identity: колонка uuid в схеме без DEFAULT, генерировать
        // значение некому. Identity заставил бы EF не слать id и ждать его от БД.
        // v7 упорядочен по времени — вставки идут в конец индекса, а не вразнобой.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        public Guid Id { get; set; } = Guid.CreateVersion7();
        
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
