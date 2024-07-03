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
    private static List<(string position, double x, double y, double angle)> positionList = new List<(string position, double x, double y, double angle)>();
    private static DateTime startTime;
    private static string position1111 = "onnnnnnnnnnnnncong";
    private static double x = 100, y = 5000, angle = 9800;
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
        process_and_upload_fci_data();
        Console.WriteLine("Press Enter to end program ...");
        Console.ReadLine();



        
    }

   static void process_and_upload_fci_data()
    {
        Console.WriteLine("[INFO] Beginn reading, processing and uploading of device stream.");
        positionList.Add((position1111, x, y, angle));
        List<JsonPatchDocument> patchList = new List<JsonPatchDocument>();
        JsonPatchDocument operational_data_patch = new JsonPatchDocument();
         for(int i=0; i<4; i++){

        JsonPatchDocument operational_data_patch = new JsonPatchDocument(); // Create new in every run   
        operational_data_patch.AppendAdd<string>("", positionList(i).ToString());
        // Console.WriteLine("patchList");
        patchList.Add(operational_data_patch);
        // Hou: print the patchList and operational_data_patch to see the difference
        Console.WriteLine("patchList should be:"+patchList);

        Console.WriteLine("operational_data_patch should be:"+operational_data_patch); // e.g.: [{"op":"add","path":"","value":"1.415947"}]

        }
        update_twin_from_patches(patchList, updateTwinIds);



    }

static void update_twin_from_patches(List<JsonPatchDocument> patches, List<string> twinIds)
{

            Console.WriteLine("Displaying Patches");
            Console.WriteLine(patches);
            for(int i=0; i<patches.Count; i++){
                // Hou: change twinIds[i] to twinIds as a whole string
                m_azureClient.UpdateComponent(twinIds[i], "value", patches[i]); 

            }
            //Hou: Print the timestamp of the process data being uploaded
            DateTime currentTime2 = DateTime.Now;
            string timestamp2 = currentTime2.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine("timestamp for uploading data stream"+timestamp2);
            //

            Console.WriteLine("Twin graph updated");
}


    