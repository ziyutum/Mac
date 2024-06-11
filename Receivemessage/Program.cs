using System;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
 class Program
{
    static async Task Main(string[] args)
    {
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();
         var options = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com", 1883)
            .Build();
         mqttClient.UseConnectedHandler(async e =>
        {
            Console.WriteLine("Connected to MQTT broker");
             await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("test/topic").Build());
        });
         mqttClient.UseDisconnectedHandler(async e =>
        {
            Console.WriteLine("Disconnected from MQTT broker");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await mqttClient.ConnectAsync(options);
        });
         mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            Console.WriteLine($"Received message: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
        });
         await mqttClient.ConnectAsync(options);
         while (true)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("test/topic")
                .WithPayload("Hello, MQTT!")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();
             await mqttClient.PublishAsync(message);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}