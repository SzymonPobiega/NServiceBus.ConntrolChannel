using System.Threading.Tasks;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel.API
{
    public interface IControlChannelReceiverActions
    {
        Task SendLocal(MessageContext context);
        Task ForwardToServiceControl(string destinationQueue, MessageContext context);
    }
}