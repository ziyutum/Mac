



/*
dotnet add package MQTTnet
dotnet add package Azure.DigitalTwins.Core
dotnet add package Azure.Identity
*/

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Azure;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Azure.DigitalTwins.Core.Models;

class Program
{
    static bool startTimer = false;
    static DateTime startTime;
    static DigitalTwinsClient client;
    static string twinId = "BottlePosion";

    static async Task Main(string[] args)
    {
        string mqttBroker = "your_mqtt_broker_address";
        int mqttPort = 1883;
        string mqttTopic = "MyJoghurt2Panda/ProvideBottle";

        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithClientId("YourClientID")
            .WithTcpServer(mqttBroker, mqttPort)
            .Build();

        mqttClient.UseConnectedHandler(async e =>
        {
            Console.WriteLine("Connected to MQTT broker");
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(mqttTopic).Build());
            Console.WriteLine($"Subscribed to topic {mqttTopic}");
        });

        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"Received message: {message}");
            // begin to count down the timer when receiving the message
            if (message.Contains("The Bottle is : On the Conveyer 1"))
            {
                startTimer = true;
                startTime = DateTime.Now;
                Console.WriteLine("Timer started.");
            }
            else if (message.Contains("Position : On the right"))
            {
                startTimer = false;
                Console.WriteLine("Timer stopped.");
            }
        });

        // connect to Azure Digital Twins
        string adtInstanceUrl = "https://your-digital-twins-instance.api.wcus.digitaltwins.azure.net";
        var credential = new DefaultAzureCredential();
        client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);

        // 启动 MQTT 客户端
        await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);
        Console.WriteLine("Press any key to exit...");
        
        // 开始实时发送位置信息
        while (true)
        {
            if (startTimer)
            {
                double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                double position = 0.1 * elapsedSeconds;
                await SendPositionToDigitalTwinsAsync(position);
            }
            await Task.Delay(1000); // 每秒发送一次
        }

        // await mqttClient.DisconnectAsync();
    }

    static async Task SendPositionToDigitalTwinsAsync(double position)
    {
        try
        {
            var updateTwinData = new JsonPatchDocument();
            updateTwinData.AppendReplace("/value", position);

            await client.UpdateDigitalTwinAsync(twinId, updateTwinData);
            Console.WriteLine($"Updated Digital Twin {twinId} with position {position}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Digital Twin: {ex.Message}");
        }
    }
}
