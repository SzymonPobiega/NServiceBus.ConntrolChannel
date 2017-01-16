using System.Threading.Tasks;
using NServiceBus.ControlChannel.API;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    class RetryMessageForwarder : IControlChannelReceiver
    {
        public Task Handle(MessageContext context, IControlChannelReceiverActions actions)
        {
            string target;
            if (context.Headers.TryGetValue("ServiceControl.TargetEndpointAddress", out target))
            {
                return actions.SendLocal(context);
            }
            return Task.CompletedTask;
        }
    }
}