using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    /// <summary>
    /// Control channel.
    /// </summary>
    public static class ControlChannel
    {
        /// <summary>
        /// Enables the control channel which provides a dedicated bi-directional link between the endpoint and ServiceControl.
        /// </summary>
        /// <typeparam name="T">Type of transport to use for the control channel.</typeparam>
        /// <param name="endpointConfiguration">Endpoint configuration.</param>
        /// <param name="transportConfiguration">Allow to configure the transport used for the control channel.</param>
        /// <returns></returns>
        public static ControlChannelSettings UseControlChannel<T>(this EndpointConfiguration endpointConfiguration, Action<TransportExtensions<T>> transportConfiguration)
            where T : TransportDefinition, new()
        {
            var settings = endpointConfiguration.GetSettings();

            if (transportConfiguration == null)
            {
                throw new ArgumentNullException(nameof(transportConfiguration));
            }
            settings.EnableFeatureByDefault<ControlChannelFeature>();

            Action<RawEndpointConfiguration> transportConfigurator = c =>
            {
                var extensions = c.UseTransport<T>();
                transportConfiguration(extensions);
            };

            settings.Set(ControlChannelFeature.TransportConfigKey, transportConfigurator);
            return new ControlChannelSettings(settings);
        }
    }
}