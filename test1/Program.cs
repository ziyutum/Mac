using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

class Program
{
    static async Task Main(string[] args)
    {
        var mqttBroker = "localhost";
        int mqttPort = 1883;
        string mqttTopic = "MyJoghurt2Panda/ProvideBottle";
        
        // Create an MQTT client
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        // Configure MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttBroker, mqttPort)
            .WithClientId("CX-2040")
            .Build();

        // Register callback for received messages
        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            Console.WriteLine("Received message:");
            Console.WriteLine($"Topic: {e.ApplicationMessage.Topic}");
            Console.WriteLine($"Payload: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            Console.WriteLine($"QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
            Console.WriteLine($"Retain: {e.ApplicationMessage.Retain}");
        });

        try
        {
            // Connect to the broker
            await mqttClient.ConnectAsync(options, CancellationToken.None);

            // Subscribe to the topic
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mqttTopic).Build());

            Console.WriteLine($"Subscribed to topic: {mqttTopic}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }

        Console.WriteLine("Press any key to exit");
        Console.ReadLine();

        // Disconnect from the broker
        if (mqttClient.IsConnected)
        {
            await mqttClient.DisconnectAsync();
        }
    }
}
