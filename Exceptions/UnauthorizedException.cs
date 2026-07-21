using System.Net;

namespace KT_Learn.Exceptions
{
    /// <summary>401 — неверные учётные данные.</summary>
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(HttpStatusCode.Unauthorized, message) { }
    }
}
