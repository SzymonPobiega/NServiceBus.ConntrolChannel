using System.Transactions;
using NServiceBus.Transport;

namespace NServiceBus.ControlChannel
{
    static class TransportTransactionExtensions
    {
        /// <summary>
        /// Copies the ambient transaction context.
        /// </summary>
        public static TransportTransaction FlowOnlyAmbientTransactionContext(this TransportTransaction transportTransaction)
        {
            var newTransportTransaction = new TransportTransaction();
            Transaction ambientTransaction;
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                newTransportTransaction.Set(ambientTransaction);
            }
            return newTransportTransaction;
        }
    }
}