namespace EventManager.Domain.Exceptions
{
    public class BookingLimitExceededException : Exception
    {
        public BookingLimitExceededException()
         : base("Лимит активных броней превышен.")
        {
        }

        public BookingLimitExceededException(string message)
          : base(message)
        {
        }
    }
}
