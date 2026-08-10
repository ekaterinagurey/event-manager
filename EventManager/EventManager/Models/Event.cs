using System.ComponentModel.DataAnnotations;

namespace EventManager.Models
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int? TotalSeats { get; set; }
        public int? AvailableSeats { get; set; }
        public List<Booking> Bookings { get; private set; } = new List<Booking>();

        public Event(string title,
                      string description,
                      DateTime startAt,
                      DateTime endAt,
                      int totalSeats)
        {
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
        }

        private Event()
        {
        }

        public bool TryReserveSeats(int count = 1)
        {
            if (count <= 0)
                throw new ArgumentException("Количество резервируемых мест должно быть больше 0.");

            if (AvailableSeats < count)
                return false;

            AvailableSeats -= count;
            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            if (count <= 0)
                throw new ArgumentException("Количество резервируемых мест должно быть больше 0.");

            AvailableSeats += count;

            if (AvailableSeats > TotalSeats)
                AvailableSeats = TotalSeats;
        }

        public static Event Create(string title,
                                   string description,
                                   DateTime startAt,
                                   DateTime endAt,
                                   int totalSeats)
        {
            if (totalSeats <= 0)
                throw new ValidationException("Общее количество мест должно быть больше 0.");

            if (endAt <= startAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }
    }
}
