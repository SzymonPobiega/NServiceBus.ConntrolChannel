using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.ControlChannel.API;
using NServiceBus.Extensibility;
using NServiceBus.Faults;
using NServiceBus.Features;
using NServiceBus.Pipeline;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    class ControlChannelFeature : Feature
    {
        const string KeyBase = "NServiceBus.ControlChannel.";
        public static readonly string ErrorAddressKey = KeyBase + "ErrorAddress";
        public static readonly string TransportConfigKey = KeyBase + "TransportConfig";

        public ControlChannelFeature()
        {
            DependsOn("NServiceBus.Receiving");
            Defaults(s =>
            {
                s.SetDefault(ErrorAddressKey, "error"); //The actual error queue name

                //Forward errors to control queue
                s.Set("errorQueue", ErrorQueueForwardAddress(s));
            });
        }

        static string ErrorQueueForwardAddress(ReadOnlySettings s)
        {
            var logicalAddress = s.LogicalAddress();
            var errorQueueForwardAddress = logicalAddress.CreateQualifiedAddress("ForwardError");
            var controlChannelQueue = s.GetTransportAddress(errorQueueForwardAddress);
            return controlChannelQueue;
        }

        static string ControlChannelQueueName(ReadOnlySettings settings)
        {
            return settings.EndpointName() + ".Control";
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            var controlChannelQueueName = ControlChannelQueueName(settings);

            var transportConfigurator = settings.Get<Action<RawEndpointConfiguration>>(TransportConfigKey);

            var controlChannelFuture = new TaskCompletionSource<IRawEndpointInstance>();
            var localDispatcherFuture = new TaskCompletionSource<IDispatchMessages>();

            var sender = new ControlChannelSender(controlChannelFuture.Task);
            var errorQueue = settings.Get<string>(ErrorAddressKey);

            context.AddSatelliteReceiver("FailedMessageForwarder", settings.Get<string>("errorQueue"), new PushRuntimeSettings(1),
                (config, errorContext) => RecoverabilityAction.ImmediateRetry(), (builder, messageContext) =>
                {
                    messageContext.Headers["ServiceControl.RetryTo"] = controlChannelQueueName;
                    var outgoingMessage = new OutgoingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);
                    return sender.Send(errorQueue, outgoingMessage, messageContext.TransportTransaction.FlowOnlyAmbientTransactionContext(), new ContextBag());
                });

            context.Container.ConfigureComponent<IControlChannelSender>(() => sender, DependencyLifecycle.SingleInstance);
            context.RegisterStartupTask(b =>
            {
                var receivers = b.BuildAll<IControlChannelReceiver>().ToList();

                //Forward retries coming from ServiceControl to local queue
                receivers.Add(new RetryMessageForwarder());

                var receiverActions = new ControlChannelReceiverActions(localDispatcherFuture.Task, controlChannelFuture.Task, settings.LocalAddress());
                var rawEndpointConfig = RawEndpointConfiguration.Create(controlChannelQueueName, (c, d) => OnMessage(c, receiverActions, receivers), "poison");
                transportConfigurator(rawEndpointConfig);
                rawEndpointConfig.AutoCreateQueue();
                return new ControlChannelTask(rawEndpointConfig, b.Build<IDispatchMessages>(), controlChannelFuture, localDispatcherFuture);
            });

            //Forward audits
            context.Pipeline.Register(new AuditMarker(), "Marks outgoing audit messages");
            context.Pipeline.Register(new AuditForwarder(sender), "Sends outgoing messages marked as audits to ServiceControl via control channel");
        }

        async Task OnMessage(MessageContext context, IControlChannelReceiverActions actions, List<IControlChannelReceiver> receivers)
        {
            foreach (var receiver in receivers)
            {
                await receiver.Handle(context, actions);
            }
        }

        class ControlChannelTask : FeatureStartupTask
        {
            RawEndpointConfiguration config;
            IDispatchMessages localDispatcher;
            TaskCompletionSource<IRawEndpointInstance> controlChannelFuture;
            TaskCompletionSource<IDispatchMessages> localDispatcherFuture;
            IRawEndpointInstance instance;

            public ControlChannelTask(RawEndpointConfiguration config, IDispatchMessages localDispatcher, TaskCompletionSource<IRawEndpointInstance> controlChannelFuture, TaskCompletionSource<IDispatchMessages> localDispatcherFuture)
            {
                this.config = config;
                this.localDispatcher = localDispatcher;
                this.controlChannelFuture = controlChannelFuture;
                this.localDispatcherFuture = localDispatcherFuture;
            }

            protected override async Task OnStart(IMessageSession session)
            {
                instance = await RawEndpoint.Start(config).ConfigureAwait(false);
                controlChannelFuture.SetResult(instance);
                localDispatcherFuture.SetResult(localDispatcher);
            }

            protected override Task OnStop(IMessageSession session)
            {
                return instance.Stop();
            }
        }
    }
}
