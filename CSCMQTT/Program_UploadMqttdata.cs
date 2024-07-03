using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Net; // For client side connection to omniverse
using System.Net.Sockets; // For server side socket connection with the Robot controller
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json; // For deserialization of the dtdl description of the twin instances and for deserialization of operational data to objects where we don't have a defined type
using System.Timers; // To define fixed time intervals when twin is updated or data is from twin is read

// Azure, Azure Digital Twins related imports
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
    private static DigitalTwinsClient m_azureClient;
    private static List<string> updateTwinIds = new List<string> { "JointPosition1", "JointPosition2", "JointPosition3", "JointPosition4" };

    static async Task Main(string[] args)
    {
        Console.WriteLine("Started the Client-App");
        Console.WriteLine("Beginning with authentication ...");
        string adtInstanceUrl = "https://FrankaMyJoghurtDTCreation.api.weu.digitaltwins.azure.net";

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });
        m_azureClient = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
        Console.WriteLine("[SUCCESS] Authentication finished");

        var mqttClient = SetupMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("CX-2040-Receiver")
            .WithTcpServer("192.168.80.100", 1883)
            .WithCleanSession()
            .Build();

        try
        {
            await mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        // Start the timer for periodic updates
        System.Timers.Timer updateTimer = new System.Timers.Timer(100); // Update every seconds
        updateTimer.Elapsed += UpdateTimer_Elapsed;
        updateTimer.Start();

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
                    angle = 0;
                    //Console.WriteLine($"ON_CONVEYER_1: x = {x:F2}, y = {y:F3}, angle = {angle:F3}");
                    break;
                case "In_Switch_1":
                    angle += Math.PI / 2 * (elapsedTime / 2);
                    x += 7 * Math.Sin(angle);
                    y += 7 - 7 * Math.Cos(angle);
                    //Console.WriteLine($"In_Switch_1: x = {x:F3}, y = {y:F3}, angle = {angle:F3}");
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
                    y = 0;
                    angle = 0;
                    break;
                default:
                    x = 0;
                    y = 0;
                    angle = 0;
                    break;
            }

            positionList.Add((timestamp, lastPosition, x, y, angle));
        }
    }

    static async void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        List<(DateTime timestamp, string position, double x, double y, double angle)> dataCopy;

        lock (lockObj)
        {
            dataCopy = new List<(DateTime timestamp, string position, double x, double y, double angle)>(positionList);
            positionList.Clear();
        }

        if (dataCopy.Count > 0)
        {
            List<JsonPatchDocument> patchList = CreatePatchList(dataCopy);
            await UpdateTwinFromPatchesAsync(patchList, updateTwinIds);
            //await update_twin_from_patches(patchList, updateTwinIds);
        }
    }

    static List<JsonPatchDocument> CreatePatchList(List<(DateTime timestamp, string position, double x, double y, double angle)> positionList)
    {
        List<JsonPatchDocument> patchList = new List<JsonPatchDocument>();

        foreach (var item in positionList)
        {
            JsonPatchDocument patchPosition = new JsonPatchDocument();

            patchPosition.AppendAdd<string>("/value", item.position);
            patchList.Add(patchPosition);

            JsonPatchDocument patchX = new JsonPatchDocument();
            patchX.AppendAdd<string>("/value", item.x.ToString());
            patchList.Add(patchX);

            JsonPatchDocument patchY = new JsonPatchDocument();
            patchY.AppendAdd<string>("/value", item.y.ToString());
            patchList.Add(patchY);

            JsonPatchDocument patchAngle = new JsonPatchDocument();
            patchAngle.AppendAdd<string>("/value", item.angle.ToString());
            patchList.Add(patchAngle);
            //Console.WriteLine("patchList should be:"+patchList);

            //Console.WriteLine("operational_data_patch should be:"+patchPosition); 


           
        }

        return patchList;
    }

    static async Task UpdateTwinFromPatchesAsync(List<JsonPatchDocument> patches, List<string> twinIds)
    {
        Console.WriteLine("Displaying Patches"+patches.ToString);
        foreach (var patch in patches)
        {
            Console.WriteLine(patch.ToString());  //not good: [{"op":"add","path":"/position","value":"ON_CONVEYER_1"},{"op":"add","path":"/x","value":"0"},{"op":"add","path":"/y","value":"10.020056"},{"op":"add","path":"/angle","value":"0"}]

        }

        for (int i = 0; i < patches.Count; i++)
        { Console.WriteLine(patches.Count);
            try
            {
                var twinId = twinIds[i % twinIds.Count];
                await m_azureClient.UpdateDigitalTwinAsync(twinId, patches[i]);
                //m_azureClient.UpdateComponent(twinId,"value", patches[i]);
                Console.WriteLine($"Successfully updated twin {twinId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating twin {twinIds[i % twinIds.Count]}: {ex.Message}");
            }
        }

        //Console.WriteLine("Twin graph updated.");
    }
        static void update_twin_from_patches(List<JsonPatchDocument> patches, List<string> twinIds){
            Console.WriteLine("Displaying Patches");
            Console.WriteLine(patches);
            // update the whole twinID as a whole Sting
            for(int i=0; i<patches.Count; i++){
                m_azureClient.UpdateComponent(twinIds[i], "value", patches[i]); 

            }
            Console.WriteLine("Twin graph updated for Num: " );
        }
}
