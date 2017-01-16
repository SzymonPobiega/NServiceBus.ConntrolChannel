using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.ControlChannel
{
    class AuditMarker : Behavior<IAuditContext>
    {
        public override Task Invoke(IAuditContext context, Func<Task> next)
        {

            context.Extensions.Set(new AuditForwarder.Marker(context.AuditAddress));
            return next();
        }
    }
}