﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Net; // For client side connection to omniverse
using System.Net.Sockets; // For server side socket connection with the Robot controller
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json; // For deserialization of the dtdl description of the twin instances and for deserialization of operational data to objects where we don't have a defined type
using System.Timers; // To define fixed time intervalls when twin is updated or data is from twin is read

// Azure, Azure Digital Twins realted imports
//       https://docs.microsoft.com/de-de/azure/digital-twins/tutorial-code
// we are using netcore version 8 on ubuntu, check the config before running to ensure the NetCore version is 8.0

using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;

class Program
{
    private static List<string> positionMessages = new List<string>();
    private static List<(DateTime timestamp, string position, double x, double y, double angle)> positionList = new List<(DateTime timestamp, string position, double x, double y, double angle)>();
    private static DateTime startTime;
    private static string lastPosition = "";
    private static double x = 0, y = 0, angle = 0;
    private static readonly object lockObj = new object();
    // The Azure Digital Twins Client
    private static DigitalTwinsClient m_azureClient;
   private static List<string> updateTwinIds = new List<string>{"JointPosition1", "JointPosition2", "JointPosition3", "JointPosition4"};

    // Vars for debugging
   

    static async Task Main(string[] args)
    {
        Console.WriteLine("Started the Client-App");
        Console.WriteLine("Beginning with authentication ...");
        string adtInstanceUrl = "https://FrankaMyJoghurtDTCreation.api.weu.digitaltwins.azure.net";

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });
        DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
        m_azureClient = client;
        Console.WriteLine("[SUCCESS] Authentication finished");

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
            if (currentPosition != null && currentPosition != lastPosition)
            {
                positionMessages.Add(currentPosition);
                lastPosition = currentPosition;
                startTime = DateTime.Now;
            }

            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;

            switch (lastPosition)
            {
                case "ON_CONVEYER_1":
                    x += 0 * elapsedTime;
                    y += 10 * elapsedTime;
                    angle += 0;
                    Console.WriteLine($"ON_CONVEYER_1: x = {x:F2}, y = {y:F3}, angle = {angle:F3}");
                    break;
                case "In_Switch_1":
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    Console.WriteLine($"In_Switch_1: x = {x:F3}, y = {y:F3}, angle = {angle:F3}");
                    break;
                case "ON_CONVEYER_2":
                    x += 10 * elapsedTime;
                    y += 0 * elapsedTime;
                    angle = 0;
                    break;
                case "In_Switch_2":
                    angle = -Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    break;
                case "ON_CONVEYER_3":
                    x += 10 * elapsedTime;
                    y += 0 * elapsedTime;
                    angle = 0;
                    break;
                case "In_Switch_3":
                    angle = -Math.PI / 2;
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
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
            int funcCount =0;
            positionList.Add((timestamp, lastPosition, x, y, angle));
            Console.WriteLine("positionlist is"+positionList);

            List<JsonPatchDocument> patchList = CreatePatchList(positionList);
            Console.WriteLine(patchList);
            update_twin_from_patches(patchList, updateTwinIds, funcCount);
           // SaveToCsv(positionList, "position_data.csv");

            
        }
    }

    // static void SaveToCsv(List<(DateTime timestamp, string position, double x, double y, double angle)> data, string filePath)
    // {
    //     using (var writer = new StreamWriter(filePath))
    //     {
    //         writer.WriteLine("Timestamp,Position,X,Y,Angle");
    //         foreach (var item in data)
    //         {
    //             writer.WriteLine($"{item.timestamp:yyyy-MM-dd HH:mm:ss},{item.position},{item.x},{item.y},{item.angle}");
    //         }
    //     }
    // }

    static List<JsonPatchDocument> CreatePatchList(List<(DateTime timestamp, string position, double x, double y, double angle)> positionList)
    {
        List<JsonPatchDocument> patchList = new List<JsonPatchDocument>();

        foreach (var item in positionList)
        {
            // 
            JsonPatchDocument patch = new JsonPatchDocument();

            // 
            //patch.AppendAdd<string>("/timestamp", item.timestamp.ToString());
            patch.AppendAdd<string>("/position", item.position);
            patch.AppendAdd<string>("/x", item.x.ToString());
            patch.AppendAdd<string>("/y", item.y.ToString());
            patch.AppendAdd<string>("/angle", item.angle.ToString());

            // 
            patchList.Add(patch);
            Console.WriteLine("patchlist is "+patchList);
        }

        return patchList;
    }
    
// An object reference is required for the non-static field, method, or property 'Program.updateTwinIds'
// The error message you're encountering indicates that you're trying to access an instance member from a static context. In C#, you cannot access non-static fields, methods, or properties from a static method without an instance of the containing class.
// Here are a few potential solutions to resolve this issue:

// Make updateTwinIds a static member if it's meant to be shared across all instances of the class.
// Pass updateTwinIds as a parameter to the method if it's supposed to be an instance-specific member.
// Create an instance of the class to access the instance member.
    static void update_twin_from_patches(List<JsonPatchDocument> patches, List<string> twinIds, int funcCount)
    {
        Console.WriteLine("Displaying Patches");
        Console.WriteLine(patches);
        for (int i = 0; i < patches.Count; i++)
        {
            try
            {
                m_azureClient.UpdateComponent(twinIds[i], "value", patches[i]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating twin {twinIds[i]}: {ex.Message}");
            }
        }
        Console.WriteLine("Twin graph updated for Num: " + funcCount);
    }
}
