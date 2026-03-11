namespace TodoApp.Api.Exceptions;

public class ConflictException(string message) : Exception(message);
