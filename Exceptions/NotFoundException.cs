using System.Net;

namespace KT_Learn.Exceptions
{
    /// <summary>404 — запрошенный ресурс не найден.</summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(HttpStatusCode.NotFound, message) { }
    }
}
