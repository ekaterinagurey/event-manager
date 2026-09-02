namespace EventManager.Domain.Exceptions
{
    public class AccessDeniedException : Exception
    {
        public AccessDeniedException()
         : base("Отсутствуют права на операцию.")
        {
        }

        public AccessDeniedException(string message)
          : base(message)
        {
        }
    }
}
