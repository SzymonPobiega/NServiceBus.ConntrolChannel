using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel.API
{
    public interface IControlChannelReceiver
    {
        Task Handle(MessageContext context, IControlChannelReceiverActions actions);
    }
}
