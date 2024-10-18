namespace CoreService.Application.Common.Exceptions;

public class CreateTokenException : Exception
{
    public CreateTokenException(string message) : base(message) { }    
}