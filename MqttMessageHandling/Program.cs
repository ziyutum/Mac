using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static List<string> positionMessages = new List<string>();
    private static List<(DateTime timestamp, string position, double x, double y, double angle)> positionList = new List<(DateTime timestamp, string position, double x, double y, double angle)>();
    private static DateTime startTime;
    private static string lastPosition = "";
    private static double x = 0, y = 0, angle = 0;
    private static readonly object lockObj = new object();

    static async Task Main(string[] args)
    {
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

        // Keep the application running
        Console.WriteLine("Press any key to exit.");
        Console.ReadLine();

        await mqttClient.DisconnectAsync();
    }

    static IMqttClient SetupMqttClient()
    {
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        mqttClient.UseConnectedHandler(async e =>
        {
            Console.WriteLine("Connected successfully with MQTT Brokers.");

            // Subscribe to the topics
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
            // 
            if (currentPosition != null && currentPosition != lastPosition)
            {
                positionMessages.Add(currentPosition);
                lastPosition = currentPosition;
                startTime = DateTime.Now;
            }

            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
            //Console.WriteLine($"Elapsed time: {elapsedTime:F3} seconds");

            //  if (currentPosition != null && currentPosition != lastPosition)
            // {
            //     // if the position is ON_CONVEYER_1，cumulate the time
            //     if (lastPosition == "ON_CONVEYER_1")
            //     {
            //         totalDurationConveyer1 += elapsedTime;
            //         Console.WriteLine($"Total duration on ON_CONVEYER_1: {totalDurationConveyer1:F3} seconds");
            //     }

            //     positionMessages.Add(currentPosition);
            //     lastPosition = currentPosition;
            //     startTime = DateTime.Now;
            // }


            switch (lastPosition)
            {
                case "ON_CONVEYER_1":
                    x += 0 * elapsedTime;
                    y += 10 * elapsedTime;
                    angle += 0;
                    Console.WriteLine($"ON_CONVEYER_1: x = {x:F2}, y = {y:F3}, angle ={angle:F3} ");
                    break;
                case "In_Switch_1":
                    angle += Math.PI / 2 * (elapsedTime / 2); // 
                    x +=  7* Math.Sin(angle);
                    y +=7-7* Math.Cos(angle) ;
                    Console.WriteLine($"In_Switch_1: x = {x:F3}, y = {y:F3}, angle ={angle:F3} ");
                    break;
                case "ON_CONVEYER_2":
                    x += 10 * elapsedTime;
                    y += 0 * elapsedTime;
                    angle = 0;
                    break;
                case "In_Switch_2":
                    angle =-1*Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2); // 
                    x +=  7* Math.Sin(angle);
                    y +=7-7* Math.Cos(angle) ;
                    break;
                case "ON_CONVEYER_3":
                    x += 10 * elapsedTime;
                    y += 0 * elapsedTime;
                    angle = 0;

                    break;
                case "In_Switch_3":
                    angle =-1*Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2); // 
                     x +=  7* Math.Sin(angle);
                    y +=7-7* Math.Cos(angle) ;
                    break;
                case "ON_CONVEYER_4":
                    x += 0 * elapsedTime;
                    y += -10 * elapsedTime;
                    angle = 0;
                    break;
                case "At_Output":
                    x += 0 * elapsedTime;
                    y += 0;
                    angle += 0;
                    break;
                default:
                    x = 0;
                    y = 0;
                    angle = 0;
                    break;
            }

            positionList.Add((timestamp, lastPosition, x, y, angle));
            SaveToCsv(positionList, "position_data.csv");
        }
    }

    static void SaveToCsv(List<(DateTime timestamp, string position, double x, double y, double angle)> data, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Timestamp,Position,X,Y,Angle");
            foreach (var item in data)
            {
                writer.WriteLine($"{item.timestamp:yyyy-MM-dd HH:mm:ss},{item.position},{item.x},{item.y},{item.angle}");
            }
        }
    }
}
