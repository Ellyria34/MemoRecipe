using System.Net;

namespace MemoRecipe.Application.Exceptions;

public class OcrServiceUnavailableException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public OcrServiceUnavailableException(HttpStatusCode statusCode)
        : base($"OCR function returned non-success status: {(int)statusCode} {statusCode}.")
    {
        StatusCode = statusCode;
    }
}