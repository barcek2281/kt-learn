using System.Text.Json;
using KT_Learn.Models;
using KT_Learn.Models.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KT_Learn.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) :base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        // Без этой строки EF вообще не знает о сущности PdfToTest: раньше её можно
        // было найти по коллекции Uploads в User, но её нет — путей больше нет.
        public DbSet<PdfToTest> PdfToTests => Set<PdfToTest>();

        // Черновики теста, сгенерированные ИИ, и их задачи.
        public DbSet<AiDraftTest> AiDraftTests => Set<AiDraftTest>();
        public DbSet<AiDraftTask> AiDraftTasks => Set<AiDraftTask>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Role — нативный enum-тип PostgreSQL user_role, а не строка и не число.
            // Имена значений C# Npgsql переводит в snake_case: Role.Student -> 'student'.
            modelBuilder.HasPostgresEnum<Role>(name: "user_role");
            modelBuilder.HasPostgresEnum<PdfStatus>(name: "pdf_status");
            modelBuilder.HasPostgresEnum<DraftReviewStatus>(name: "draft_review_status");
            modelBuilder.HasPostgresEnum<TaskType>(name: "task_type");

            // Одного [DatabaseGenerated(Identity)] мало: для Guid EF включает
            // собственный клиентский генератор (с EF 9 он выдаёт UUIDv7) и шлёт
            // значение в INSERT, так что DEFAULT в БД не срабатывает.
            // HasDefaultValueSql помечает колонку как заполняемую БД — EF убирает
            // её из INSERT и забирает результат через RETURNING.
            modelBuilder.Entity<User>()
                .Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<PdfToTest>()
                .Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // Внимание, тут EF ведёт себя не как JPA. Раз UploadedBy не nullable,
            // связь считается обязательной, а для обязательных связей EF по
            // умолчанию включает каскадное удаление: удаляя пользователя, он
            // сам удалит все его PDF. В схеме же стоит ON DELETE RESTRICT —
            // историю загрузок терять нельзя. Приводим модель к схеме.
            modelBuilder.Entity<PdfToTest>()
                .HasOne(p => p.Uploader)
                .WithMany()                       // на стороне User коллекции нет
                .HasForeignKey(p => p.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // --- AI-черновики ---

            // Id генерирует БД (DEFAULT gen_random_uuid()), как у остальных таблиц.
            modelBuilder.Entity<AiDraftTest>()
                .Property(d => d.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<AiDraftTask>()
                .Property(t => t.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // Связи и поведение при удалении приводим к схеме (Script0005).
            // Черновик -> PDF: ON DELETE CASCADE.
            modelBuilder.Entity<AiDraftTest>()
                .HasOne(d => d.Pdf)
                .WithMany()
                .HasForeignKey(d => d.PdfToTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Черновик -> рецензент (users): ON DELETE SET NULL.
            modelBuilder.Entity<AiDraftTest>()
                .HasOne(d => d.Reviewer)
                .WithMany()
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Задача -> черновик: ON DELETE CASCADE, навигация через Tasks.
            modelBuilder.Entity<AiDraftTask>()
                .HasOne(t => t.DraftTest)
                .WithMany(d => d.Tasks)
                .HasForeignKey(t => t.AiDraftTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // options и correct_answer — колонки jsonb. Npgsql по умолчанию мапит
            // List<string> на массив text[], поэтому храним как JSON через конвертер.
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            // Без компаратора EF не замечает изменения внутри списка (мутации).
            var stringListComparer = new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v == null ? 0 : v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                v => v.ToList());

            modelBuilder.Entity<AiDraftTask>()
                .Property(t => t.Options)
                .HasColumnType("jsonb")
                .HasConversion(stringListConverter, stringListComparer);

            modelBuilder.Entity<AiDraftTask>()
                .Property(t => t.CorrectAnswer)
                .HasColumnType("jsonb")
                .HasConversion(stringListConverter, stringListComparer);
        }
    }
}
