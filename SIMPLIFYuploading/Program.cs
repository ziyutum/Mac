using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;

class Program
{
    private static DigitalTwinsClient m_azureClient;
    private static List<string> updateTwinIds = new List<string> { "JointPosition1", "JointPosition2", "JointPosition3", "JointPosition4" };

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting the Client-App");
        Console.WriteLine("Beginning with authentication ...");

        string adtInstanceUrl = "https://FrankaMyJoghurtDTCreation.api.weu.digitaltwins.azure.net";
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });

        m_azureClient = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
        Console.WriteLine("[SUCCESS] Authentication finished");

        await ProcessAndUploadData();

        Console.WriteLine("Press Enter to end the program ...");
        Console.ReadLine();
    }

    static async Task ProcessAndUploadData()
    {
        Console.WriteLine("[INFO] Begin reading, processing, and uploading of device stream.");

        string position = "845ftgh";
        string position2 = "8888888888888888";
        string position3 = "0000000000000";
        string position4 = "87777777777777";

        // Iterate through each twin ID and update the respective twin
        foreach (var twinId in updateTwinIds)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument();
            
            switch (twinId)
            {
                case "JointPosition1":
                    patchDocument.AppendReplace("/value", position);
                    break;
                case "JointPosition2":
                    patchDocument.AppendReplace("/value", position2);
                    break;
                case "JointPosition3":
                    patchDocument.AppendReplace("/value", position3);
                    break;
                case "JointPosition4":
                    patchDocument.AppendReplace("/value", position4);
                    break;
                default:
                    Console.WriteLine($"Unknown twin ID: {twinId}. Skipping.");
                    continue;
            }

            try
            {
                await m_azureClient.UpdateDigitalTwinAsync(twinId, patchDocument);
                Console.WriteLine($"Successfully updated twin {twinId}");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error updating twin {twinId}: {ex.Message}");
            }
        }

        Console.WriteLine("All twins updated.");
    }
}
