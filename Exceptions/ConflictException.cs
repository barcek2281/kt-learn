using System.Net;

namespace KT_Learn.Exceptions
{
    /// <summary>409 — ресурс уже существует (например, занятый email).</summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(HttpStatusCode.Conflict, message) { }
    }
}
