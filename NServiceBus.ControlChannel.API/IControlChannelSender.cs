using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel.API
{
    public interface IControlChannelSender
    {
        Task Send(string destination, OutgoingMessage message, TransportTransaction localReceiverTransaction, ContextBag context);
    }
}