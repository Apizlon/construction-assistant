namespace AuthService.Application.Exceptions;

public class BadRequestException(string message) : Exception(message);