using System.Threading.Tasks;
using NServiceBus.ControlChannel.API;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    class ControlChannelSender : IControlChannelSender
    {
        Task<IRawEndpointInstance> channelFuture;

        public ControlChannelSender(Task<IRawEndpointInstance> channelFuture)
        {
            this.channelFuture = channelFuture;
        }

        public async Task Send(string destination, OutgoingMessage message, TransportTransaction localReceiverTransaction, ContextBag context)
        {
            var controlChannel = await channelFuture.ConfigureAwait(false);
            var transportTransaction = localReceiverTransaction.FlowOnlyAmbientTransactionContext();
            var operations = new TransportOperations(new TransportOperation(message, new UnicastAddressTag(destination)));
            await controlChannel.SendRaw(operations, transportTransaction, context).ConfigureAwait(false);
        }
    }
}