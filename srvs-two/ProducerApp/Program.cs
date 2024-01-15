using System;
using System.Text;
using Confluent.Kafka;
using Prometheus;
using RabbitMQ.Client;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up Prometheus HTTP server on port 1234
        using var server = new KestrelMetricServer(port: 4000);
        server.Start();

        var recordsProcessed = Metrics.CreateCounter(
            "message_produced",
            "Total number of message produced.",
            "target"
        );

        var task1 = Task.Run(
            async () =>
            {
                // Kafka producer configuration
                var config = new ProducerConfig
                {
                    BootstrapServers = "kafka:9092", // Change this if your Kafka broker is running on a different address
                };

                // Create a producer instance
                using (var producer = new ProducerBuilder<Null, string>(config).Build())
                {
                    // Produce a message to the specified topic

                    var index = 1;
                    while (true)
                    {
                        var message = new Message<Null, string> {Value = $"Hello from producer {index++}!"};
                        await producer.ProduceAsync("example-topic", message);

                        recordsProcessed.WithLabels("kafka")
                            .Inc();

                        Console.WriteLine($"Produced message {index}");

                        // Wait for any outstanding messages to be delivered and delivery reports to be received
                        producer.Flush(TimeSpan.FromSeconds(10));
                    }
                }
            }
        );

        var task2 = Task.Run(
            () =>
            {
// RabbitMQ server connection settings
                var factory = new ConnectionFactory()
                {
                    HostName = "rabbit", // Replace with your RabbitMQ server hostname or IP address
                    Port = 5672, // Default RabbitMQ port
                    UserName = "admin",
                    Password = "admin"
                };

                // Create a connection to the RabbitMQ server
                using (var connection = factory.CreateConnection())
                {
                    // Create a channel
                    using (var channel = connection.CreateModel())
                    {
                        var index = 1;
                        while (true)
                        {
                            // Declare a queue
                            channel.QueueDeclare(
                                queue: "example-queue",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null
                            );

                            // Message to be sent
                            string message = $"Hello, RabbitMQ {index++}!";

                            // Convert the message to bytes
                            var body = Encoding.UTF8.GetBytes(message);

                            // Publish the message to the queue
                            channel.BasicPublish(
                                exchange: "",
                                routingKey: "example-queue",
                                basicProperties: null,
                                body: body
                            );

                            recordsProcessed.WithLabels("rabbit")
                                .Inc();

                            Console.WriteLine($" [x] Sent '{message}'");
                        }
                    }
                }
            }
        );

        await Task.WhenAll(task1, task2);
    }
}