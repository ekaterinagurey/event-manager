namespace EventManager.Domain.Exceptions
{
    public class PastEventBookingException : Exception
    {
        public PastEventBookingException()
         : base("Бронирование прошедшего события невозможно.")
        {
        }

        public PastEventBookingException(string message)
          : base(message)
        {
        }
    }
}
