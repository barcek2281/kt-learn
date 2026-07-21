using System.Net;

namespace KT_Learn.Exceptions
{
    /// <summary>
    /// Базовое исключение приложения. Каждое наследники знает, каким HTTP-статусом
    /// оно должно вернуться клиенту — глобальный обработчик читает StatusCode.
    /// </summary>
    public abstract class AppException : Exception
    {
        protected AppException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
