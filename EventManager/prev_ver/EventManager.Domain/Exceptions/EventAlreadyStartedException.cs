using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Domain.Exceptions
{
    public class EventAlreadyStartedException : Exception
    {
        public EventAlreadyStartedException(Guid eventId)
          : base($"The booking cannot be cancelled: the event '{eventId}' has already started")
        {
        }
    }
}
