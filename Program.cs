using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Timers;
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;

class Program
{
    private static List<(DateTime timestamp, string position, double x, double y, double angle)> positionList = new List<(DateTime timestamp, string position, double x, double y, double angle)>();
    private static DateTime startTime;
    private static string lastPosition = "";
    private static double x = 0, y = 0, angle = 0;
    private static readonly object lockObj = new object();
    static DigitalTwinsClient m_azureClient;
    private static string bottlePositionPropertyPath = "/Bottle-Position";
    private static string locationXPropertyPath = "/LocationX";
    private static string locationYPropertyPath = "/LocationY";
    private static string switchAnglePropertyPath = "/Switchangle";
    static async Task Main(string[] args)
    {
        Console.WriteLine("Started the Client-App");
        Console.WriteLine("Beginning with authentication ...");
        string adtInstanceUrl = "https://FrankaMyJoghurtDTCreation.api.weu.digitaltwins.azure.net";

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });
        DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
        m_azureClient = client;
        Console.WriteLine("[SUCCESS] Authentication finished ");

        var mqttClient = SetupMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("CX-2040-Receiver")
            .WithTcpServer("192.168.80.100", 1883)
            .WithCleanSession()
            .Build();

        try
        {
            await mqttClient.ConnectAsync(options, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        Console.WriteLine("Press any key to exit.");
        Console.ReadLine();

        await mqttClient.DisconnectAsync();

        // Upload the data to Azure Digital Twins
        await UploadDataToAzure();
    }

    static IMqttClient SetupMqttClient()
    {
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        mqttClient.UseConnectedHandler(async e =>
        {
            Console.WriteLine("Connected successfully with MQTT Brokers.");

            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("MyJoghurt2Panda/ProvideBottle").Build());
            Console.WriteLine("Subscribed to the topics.");
        });

        mqttClient.UseDisconnectedHandler(e =>
        {
            Console.WriteLine("Disconnected from MQTT Brokers.");
        });

        mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            var timestamp = DateTime.Now;

            HandleMqttMessage(payload, timestamp);
        });

        return mqttClient;
    }

    static void HandleMqttMessage(string payload, DateTime timestamp)
    {
        string currentPosition = null;

        if (payload.StartsWith("The Bottle is : On the Conveyer 1   ,Position : On the Conveyer 1"))
        {
            currentPosition = "ON_CONVEYER_1";
        }
        else if (payload.StartsWith("The Bottle is: Into the Switch 1      ,  Position: In the Switch 1"))
        {
            currentPosition = "In_Switch_1";
        }
        else if (payload.StartsWith("The Bottle is: On the Conveyer 2   ,  Position : On the Conveyer 2"))
        {
            currentPosition = "ON_CONVEYER_2";
        }
        else if (payload.StartsWith("The Bottle is: Into the Switch 2      ,  Position: In the Switch 2"))
        {
            currentPosition = "In_Switch_2";
        }
        else if (payload.StartsWith("The Bottle is: On the Conveyer 3   ,  Position : On the Conveyer 3"))
        {
            currentPosition = "ON_CONVEYER_3";
        }
        else if (payload.StartsWith("The Bottle is: Into the Switch 3      ,  Position: In the Switch 3"))
        {
            currentPosition = "In_Switch_3";
        }
        else if (payload.StartsWith("The Bottle is: On the Conveyer 4   ,  Position : On the Conveyer 4"))
        {
            currentPosition = "ON_CONVEYER_4";
        }
        else if (payload.StartsWith("The Bottle is: At the Output      ,  Position:  At the Output"))
        {
            currentPosition = "At_Output";
        }

        lock (lockObj)
        {
            if (currentPosition != null && currentPosition != lastPosition)
            {
                lastPosition = currentPosition;
                startTime = DateTime.Now;
            }

            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;

            switch (lastPosition)
            {
                case "ON_CONVEYER_1":
                    y += 10 * elapsedTime;
                    break;
                case "In_Switch_1":
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    break;
                case "ON_CONVEYER_2":
                    x += 10 * elapsedTime;
                    break;
                case "In_Switch_2":
                    angle = -Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    break;
                case "ON_CONVEYER_3":
                    x += 10 * elapsedTime;
                    break;
                case "In_Switch_3":
                    angle = -Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    break;
                case "ON_CONVEYER_4":
                    y -= 10 * elapsedTime;
                    break;
                case "At_Output":
                    break;
                default:
                    x = 0;
                    y = 0;
                    angle = 0;
                    break;
            }

            positionList.Add((timestamp, lastPosition, x, y, angle));
            var data = (timestamp, currentPosition, x, y, angle);
        }
    }



    static void UploadDataToAzure()
    {
        foreach (var data in positionList)
        {
            var bottlePositionPatch = new JsonPatchDocument();
            bottlePositionPatch.AppendReplace(bottlePositionPropertyPath, data.position);

            var locationXPatch = new JsonPatchDocument();
            locationXPatch.AppendReplace(locationXPropertyPath, data.x);

            var locationYPatch = new JsonPatchDocument();
            locationYPatch.AppendReplace(locationYPropertyPath, data.y);

            var switchAnglePatch = new JsonPatchDocument();
            switchAnglePatch.AppendReplace(switchAnglePropertyPath, data.angle);

            try
            {
                await m_azureClient.UpdateDigitalTwinAsync("TwinIdForBottlePosition", bottlePositionPatch);
                await m_azureClient.UpdateDigitalTwinAsync("TwinIdForLocationX", locationXPatch);
                await m_azureClient.UpdateDigitalTwinAsync("TwinIdForLocationY", locationYPatch);
                await m_azureClient.UpdateDigitalTwinAsync("TwinIdForSwitchAngle", switchAnglePatch);

                Console.WriteLine($"Updated twins with position={data.position}, x={data.x}, y={data.y}, angle={data.angle}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update twins: {ex.Message}");
            }
        }
    }
