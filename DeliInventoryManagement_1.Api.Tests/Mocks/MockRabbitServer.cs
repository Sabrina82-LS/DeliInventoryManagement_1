using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace DelinventoryManagement_1.Api.Tests.Mocks
{
    /// <summary>
    /// A simple mock RabbitMQ server for testing
    /// </summary>
    public class MockRabbitServer
    {
        // This will store messages that are "published"
        public List<PublishedMessage> PublishedMessages { get; } = new();

        // This is the fake channel your code will use
        public Mock<IModel> Channel { get; }

        public MockRabbitServer()
        {
            Channel = new Mock<IModel>();

            // Setup BasicPublish to capture messages
            Channel.Setup(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()))
                .Callback<string, string, bool, IBasicProperties, ReadOnlyMemory<byte>>(
                    (exchange, routingKey, mandatory, props, body) =>
                    {
                        var message = Encoding.UTF8.GetString(body.ToArray());
                        PublishedMessages.Add(new PublishedMessage
                        {
                            Exchange = exchange,
                            RoutingKey = routingKey,
                            Message = message,
                            MessageId = props?.MessageId
                        });
                        Console.WriteLine($"[MOCK] Message published: {message}");
                    });
        }
    }

    public class PublishedMessage
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Message { get; set; }
        public string MessageId { get; set; }
    }
}