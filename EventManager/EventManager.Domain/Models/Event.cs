using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventManager.Domain.Models
{
    public class Event
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string? Description { get; private set; }
        public DateTime StartAt { get; private set; }
        public DateTime EndAt { get; private set; }
        public int? TotalSeats { get; private set; }
        public int? AvailableSeats { get; private set; }
        public List<Booking> Bookings { get; private set; } = new List<Booking>();

        private Event(Guid id,
                      string title,
                      DateTime startAt,
                      DateTime endAt,
                      int totalSeats,
                      string? description = null)
        {
            Id = id;
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
            AvailableSeats = totalSeats;
        }

        private Event()
        {
        }

        public static Event Create(string title,
                                   DateTime startAt,
                                   DateTime endAt,
                                   int totalSeats,
                                   string? description = null)
        {
            if (totalSeats <= 0)
                throw new ValidationException("Общее количество мест должно быть больше 0.");

            if (endAt <= startAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            return new Event(Guid.NewGuid(), title.Trim(), startAt, endAt, totalSeats, description);
        }

        public void Update(string? title,
                           DateTime? startAt,
                           DateTime? endAt,
                           string? description = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (!startAt.HasValue)
                throw new ArgumentException("StartAt не может быть пустым.");

            if (!endAt.HasValue)
                throw new ArgumentException("EndAt не может быть пустым.");

            if (endAt <= startAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            Title = title!.Trim();
            StartAt = startAt!.Value;
            EndAt = endAt!.Value;
            Description = description!;
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
    }
}
