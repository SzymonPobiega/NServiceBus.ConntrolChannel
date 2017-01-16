using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Settings;

namespace NServiceBus.ControlChannel
{
    /// <summary>
    /// Configures the control channel.
    /// </summary>
    public class ControlChannelSettings : ExposeSettings
    {
        public ControlChannelSettings(SettingsHolder settings) : base(settings)
        {
        }

        public void ErrorQueueAddress(string errorQueueAddress)
        {
            if (errorQueueAddress == null)
            {
                throw new ArgumentNullException(nameof(errorQueueAddress));
            }
            this.GetSettings().Set(ControlChannelFeature.ErrorAddressKey, errorQueueAddress);
        }
    }
}