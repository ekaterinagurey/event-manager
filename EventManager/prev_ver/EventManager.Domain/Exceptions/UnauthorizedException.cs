namespace EventManager.Domain.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Invalid username or password")
        {
        }
        public UnauthorizedException(string message)
          : base(message)
        {
        }
    }
}
