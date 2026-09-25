namespace EventManager.Domain.Exceptions
{
    public class PastEventBookingException : Exception
    {
        public PastEventBookingException()
         : base("Booking a past event is not possible")
        {
        }

        public PastEventBookingException(string message)
          : base(message)
        {
        }
    }
}
