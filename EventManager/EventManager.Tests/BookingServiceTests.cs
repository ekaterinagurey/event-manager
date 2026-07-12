using EventManager.DTOs.Events;
using EventManager.Models;
using EventManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Tests
{
    public class BookingServiceTests
    {
        private readonly EventService _eventService;
        private readonly BookingService _bookingService;

        public BookingServiceTests(EventService eventService, BookingService bookingService)
        {
            _eventService = eventService;
            _bookingService = bookingService;
        }

        private Event CreateEvent()
        {
            return _eventService.AddEvent(new EventDTO { Title = "Event 1", 
                                                      StartAt = DateTime.Now,
                                                      EndAt = DateTime.Now.AddHours(1)
            });
        }
    }
}
