namespace EventManager.Domain.Exceptions
{
    public class BookingLimitExceededException : Exception
    {
        public BookingLimitExceededException()
         : base("The limit for active reservations has been exceeded")
        {
        }

        public BookingLimitExceededException(string message)
          : base(message)
        {
        }
    }
}
