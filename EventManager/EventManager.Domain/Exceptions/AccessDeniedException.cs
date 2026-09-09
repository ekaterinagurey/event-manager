namespace EventManager.Domain.Exceptions
{
    public class AccessDeniedException : Exception
    {
        public AccessDeniedException()
         : base("Insufficient permissions for the operation")
        {
        }

        public AccessDeniedException(string message)
          : base(message)
        {
        }
    }
}
