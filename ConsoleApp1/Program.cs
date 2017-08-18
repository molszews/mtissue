using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Topshelf;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var useInMemory = false; //InMemory transport is working just fine
            var bus = useInMemory ? CreateUsingInMemory() : CreateUsingRabbitMq();
            
            //connect ISendObserver to SendEndpoint - no effect
            bus.GetSendEndpoint(new Uri("rabbitmq://localhost/output")).Result.ConnectSendObserver(new SendObserver());
            //connect ISendObserver to Bus - everything works fine, I could filter endpoint in the observer itself
//            bus.ConnectSendObserver(new SendObserver());
            Console.WriteLine("SendObserver connected to SendEndpoint 'output'");
            bus.Start();
            Console.WriteLine("Bus started");

            var endpoint = bus.GetSendEndpoint(new Uri("rabbitmq://localhost/input"));
            endpoint.Result.Send<Message>(new {});
            Console.WriteLine("Message sent to 'input' endpoint");

            Console.ReadLine();

            bus.Stop();
        }

        private static IBusControl CreateUsingInMemory()
        {
            return Bus.Factory.CreateUsingInMemory(cfg => cfg.ReceiveEndpoint("input", ec => ec.Consumer<MessageConsumer>()));
        }

        private static IBusControl CreateUsingRabbitMq()
        {
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri("rabbitmq://localhost/"), h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("input", ec =>
                {
                    ec.Consumer<MessageConsumer>();
                });
            });
        }
    }

    public class SendObserver : ISendObserver
    {
        public Task PreSend<T>(SendContext<T> context) where T : class
        {
            Console.WriteLine($"Observer PreSend");
            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context) where T : class
        {
            Console.WriteLine($"Observer PostSend");
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
            Console.WriteLine($"Observer SendFault");
            return Task.CompletedTask;
        }
    }

    public interface Message
    {
    }

    public class MessageConsumer : IConsumer<Message>
    {
        public async Task Consume(ConsumeContext<Message> context)
        {
            Console.WriteLine($"Consumer received message on input endpoint");
            var sendEndpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/output"));
            await sendEndpoint.Send<Message>(new {});
            Console.WriteLine($"Consumer sent message to output endpoint");
        }
    }
}
