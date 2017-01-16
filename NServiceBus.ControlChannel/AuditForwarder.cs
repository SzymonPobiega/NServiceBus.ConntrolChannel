using NServiceBus.ControlChannel.API;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;

    class AuditForwarder : Behavior<IRoutingContext>
    {
        IControlChannelSender controlChannelSender;

        public AuditForwarder(IControlChannelSender controlChannelSender)
        {
            this.controlChannelSender = controlChannelSender;
        }

        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            Marker m;
            if (context.Extensions.TryGet(out m))
            {
                //Forward via control channel
                return controlChannelSender.Send(m.AuditQueue, context.Message, context.Extensions.Get<TransportTransaction>(), new ContextBag());
            }
            return next();
        }

        public class Marker
        {
            public string AuditQueue { get; }

            public Marker(string auditQueue)
            {
                AuditQueue = auditQueue;
            }
        }
    }
}