using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.ControlChannel;

namespace EndpointA
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().GetAwaiter().GetResult();
        }

        static async Task Run()
        {
            var config = new EndpointConfiguration("EndpointA");
            config.UseTransport<SqlServerTransport>()
                .ConnectionString(@"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True;");
            config.UsePersistence<InMemoryPersistence>();
            config.Recoverability().Immediate(i => i.NumberOfRetries(0));
            config.Recoverability().Delayed(d => d.NumberOfRetries(0));
            
            //No failed message config. To run, you have to create "audit" and "error" queues manually in MSMQ
            config.AuditProcessedMessagesTo("audit");
            config.UseControlChannel<MsmqTransport>(e => { }).ErrorQueueAddress("error");

            var endpoint = await Endpoint.Start(config);

            while (true)
            {
                Console.WriteLine("Press <enter> to send message");
                Console.ReadLine();

                await endpoint.SendLocal(new MyMessage());
            }
        }
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        static readonly Random r = new Random();

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            if (r.Next(2) == 0)
            {
                throw new Exception("Simulated!");
            }
            Console.WriteLine("Processed");
            return Task.CompletedTask;
        }
    }

    class MyMessage : IMessage
    {
    }
}
