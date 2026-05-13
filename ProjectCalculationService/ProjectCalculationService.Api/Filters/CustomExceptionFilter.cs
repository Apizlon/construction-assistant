using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectCalculationService.Application.Exceptions;
using Serilog;

namespace ProjectCalculationService.Api.Filters;

public class CustomExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        Log.Error(context.Exception, "Unhandled exception occurred: {ExceptionMessage}",
            context.Exception.Message);

        var errorResponse = new ErrorResponse
        {
            Message = context.Exception.Message,
            RequestId = context.HttpContext.TraceIdentifier
        };

        var statusCode = context.Exception switch
        {
            BadRequestException _ => (int)HttpStatusCode.BadRequest,
            ArgumentException _ => (int)HttpStatusCode.BadRequest,
            InvalidOperationException _ => (int)HttpStatusCode.BadRequest,
            NotFoundException _ => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Result = new JsonResult(errorResponse)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}

