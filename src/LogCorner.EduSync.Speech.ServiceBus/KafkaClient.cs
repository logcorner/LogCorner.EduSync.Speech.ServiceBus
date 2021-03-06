﻿using Confluent.Kafka;
using LogCorner.EduSync.Speech.ServiceBus.Mediator;
using LogCorner.EduSync.Speech.SharedKernel.Events;
using LogCorner.EduSync.Speech.SharedKernel.Serialyser;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogCorner.EduSync.Speech.ServiceBus
{
    public class KafkaClient : IServiceBusProvider
    {
        private readonly IProducer<Null, string> _producer;
        private readonly IJsonSerializer _eventSerializer;

        private readonly IConsumer<Null, string> _consumer;

        private readonly INotifierMediatorService _notifierMediatorService;

        public KafkaClient(IProducer<Null, string> producer, IJsonSerializer eventSerializer,
            IConsumer<Null, string> consumer, INotifierMediatorService notifierMediatorService)
        {
            _producer = producer;
            _eventSerializer = eventSerializer;
            _consumer = consumer;
            _notifierMediatorService = notifierMediatorService;
        }

        public async Task SendAsync(string topic, EventStore @event)
        {
            var jsonString = _eventSerializer.Serialize(@event);
            var t = _producer.ProduceAsync(topic, new Message<Null, string>
            { Value = jsonString });

            await t.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"**KafkaClient::SendAsync - error = {task.Exception?.Message}");
                }
                else
                {
                    Console.WriteLine($"**KafkaClient::SendAsync - produced : {@event.Id} - {@event.Name}");

                    Console.WriteLine($"**KafkaClient::SendAsync - wrote to offset: {task.Result?.Offset}");
                }
            });
        }

        public async Task ReceiveAsync(string topic, CancellationToken stoppingToken, bool forever = true)
        {
            _consumer.Subscribe(topic);
            Console.WriteLine($"**KafkaClient::ReceiveAsync - consuming on topic {topic}");
            do
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    forever = false;
                }

                var data = _consumer.Consume();
                Console.WriteLine($"**KafkaClient::ReceiveAsync - key : {data.Message.Key}");
               // Console.WriteLine($"Data : {data.Message.Value}");
                Console.WriteLine($"**KafkaClient::ReceiveAsync - partition : {data.Partition.Value}");
                Console.WriteLine($"**KafkaClient::ReceiveAsync - offset : {data.Offset.Value}");
                var message = new NotificationMessage<string> { Message = data.Message.Value };
                await _notifierMediatorService.Notify(message);
            } while (forever);
        }
    }
}