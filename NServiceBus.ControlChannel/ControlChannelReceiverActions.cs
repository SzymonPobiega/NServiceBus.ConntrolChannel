using System;
using System.Threading.Tasks;
using NServiceBus.ControlChannel.API;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    class ControlChannelReceiverActions : IControlChannelReceiverActions
    {
        Task<IDispatchMessages> localDispatcherFuture;
        Task<IRawEndpointInstance> controlChannelFuture;
        string localAddress;

        public ControlChannelReceiverActions(Task<IDispatchMessages> localDispatcherFuture, Task<IRawEndpointInstance> controlChannelFuture, string localAddress)
        {
            this.localDispatcherFuture = localDispatcherFuture;
            this.controlChannelFuture = controlChannelFuture;
            this.localAddress = localAddress;
        }

        public async Task SendLocal(MessageContext context)
        {
            var dispatcher = await localDispatcherFuture.ConfigureAwait(false);
            var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
            var newTransportTransaction = context.TransportTransaction.FlowOnlyAmbientTransactionContext();
            var operations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(localAddress)));
            await dispatcher.Dispatch(operations, newTransportTransaction, new ContextBag());
        }

        public async Task ForwardToServiceControl(string destinationQueue, MessageContext context)
        {
            var controlChannel = await controlChannelFuture.ConfigureAwait(false);
            var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
            var operations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(destinationQueue)));
            await controlChannel.SendRaw(operations, context.TransportTransaction, context.Context).ConfigureAwait(false);
        }
    }
}