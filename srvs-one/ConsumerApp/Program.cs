using System;
using System.Text;
using Confluent.Kafka;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up Prometheus HTTP server on port 1234
        using var server = new KestrelMetricServer(port: 4000);
        server.Start();

        var recordsProcessed = Metrics.CreateCounter(
            "message_consumed",
            "Total number of message consumed.",
            "target"
        );

        var task1 = Task.Run(
            () =>
            {
                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = "kafka:9092", // Replace with your Kafka bootstrap servers
                    GroupId = "ezamanov-consumer-group-2", // Replace with your consumer group ID
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false // Set to true if you want the consumer to commit offsets automatically
                };

                using (var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build())
                {
                    consumer.Subscribe("example-topic"); // Replace with the topic you want to subscribe to

                    Console.WriteLine("Consumer application is running. Press Ctrl+C to exit.");

                    try
                    {
                        while (true)
                        {
                            var consumeResult = consumer.Consume();

                            if (consumeResult != null)
                            {
                                Console.WriteLine($"Received message: {consumeResult.Value}, Offset: {consumeResult.Offset}");
                                // Process the received message as needed

                                // If auto-commit is set to false, you need to manually commit the offset
                                consumer.Commit(consumeResult);

                                recordsProcessed.WithLabels("kafka")
                                    .Inc();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    finally
                    {
                        consumer.Close();
                    }
                }
            }
        );

        var task2 = Task.Run(
            async () =>
            {
                // RabbitMQ server connection settings
                var factory = new ConnectionFactory()
                {
                    HostName = "rabbit", // Replace with your RabbitMQ server hostname or IP address
                    Port = 5672,             // Default RabbitMQ port
                    UserName = "admin",
                    Password = "admin"
                };

                // Create a connection to the RabbitMQ server
                using var connection = factory.CreateConnection();
                // Create a channel
                using var channel = connection.CreateModel();
                // Declare a queue
                channel.QueueDeclare(queue: "example-queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

                // Set up a consumer
                var consumer = new EventingBasicConsumer(channel);

                // Event handler for received messages
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.ToArray());
                    Console.WriteLine($" [x] Received '{message}'");
                            
                            
                    recordsProcessed.WithLabels("rabbit")
                        .Inc();
                };

                // Start consuming messages from the queue
                channel.BasicConsume(queue: "example-queue", autoAck: true, consumer: consumer);

                await Task.Delay(10000000);
            }
        );

        await Task.WhenAll(task1, task2);
    }
}