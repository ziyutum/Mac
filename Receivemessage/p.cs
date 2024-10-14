/* EXTENSIBLE TWIN COMMUNICATION INTERFACE MODULE (ETCIM)
 * This program presents the central interface between the physical assset
 * and its virtual representation. 
 * 
 * Documentation of Azure Digital Twins SDK:
 * https://docs.microsoft.com/de-de/dotnet/api/azure.digitaltwins.core?view=azure-dotnet
 * Doc Azure Digital Twins REST API:
 * https://docs.microsoft.com/de-DE/rest/api/azure-digitaltwins/
 * 
 * Author: Josua Hoefgen
 */

using System;
using System.Net; // For client side connection to omniverse
using System.Net.Sockets; // For server side socket connection with the Robot controller
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json; // For deserialization of the dtdl description of the twin instances
 //  and for deserialization of operational data to objects where we don't have a defined type
using System.Timers; // To define fixed time intervalls when twin is updated or data is from twin is read

// Azure, Azure Digital Twins realted imports
// Note: The packages must be added to the project first, see
//       https://docs.microsoft.com/de-de/azure/digital-twins/tutorial-code
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;

namespace CSClient
{
    class Program
    {
        // Variables to time processes, initially for debugging
        private static Timer sysTimer;
        private static int iTimerCounter = 0; // Counts how often timer elpased, to stop process at some point
        private static bool bSendFromRobotTcpStream = false; // Set to true when timer elapsed; send to false after data was send

        // The Azure Digital Twins Client
        static DigitalTwinsClient m_azureClient;

        // Members for the device connection
        static string m_deviceIP = "10.157.175.17";    // Localhost if control programm runs on the same machine
        static int m_devicePort = 8080;               // Port for communication with device
        static TcpClient m_deviceClient;              // Tcp socket for connection establishment
        static NetworkStream m_deviceClientStream;    // Datastream of the device
        static Timer m_tryConnectTimer;               // Timer to retry a connection establishment
        static bool m_bConnectedToDevice;             // Flag for device connection
        
        // Members for the consumer connection
        static int m_serverPort = 32323; // 32323
        static TcpListener m_tcpServer;
        static TcpClient m_tcpClientConsumer;
        static NetworkStream m_consumerStream;
        static Timer m_timerAcceptClient;
        static bool m_bConsumerIsConnected;
        
        // Members to configure the downstream access
        static Timer m_downstreamTimer = new System.Timers.Timer(100); // Max length in ms
        static Timer m_maxTimeDownstream = new System.Timers.Timer(60000*5000);
        static List<string> m_downstreamTwins = new List<string>{"PositionJoint1", "PositionJoint2", "PositionJoint3", "PositionJoint4", "PositionJoint5", "PositionJoint6", "PositionJoint7", "GripperClosingWidth", "GripperOpeningWidht"};
        
        // ENTRY POINT 
        static async Task Main(string[] args)
        {
            Console.WriteLine("Started the Client-App.\nBeginning with authentication ...\n");
            // Instace URL found in the Azure Portal
            // string adtInstanceUrl = "https://PandaDT.api.weu.digitaltwins.azure.net";
            string adtInstanceUrl = "https://Mir.api.weu.digitaltwins.azure.net";
            // Authenticate with the Azure service
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });
            DigitalTwinsClient client = new DigitalTwinsClient(new Uri(adtInstanceUrl), credential);
            m_azureClient = client;
            Console.WriteLine("Service client succsessfully created.");
            
            //------ Setup and Initalization Functions --------
            //initialize_device_connection();     // This is the ETCIM-1 instance
            initialize_consumer_connection(); // Consumer is Isaac Sim 
            //-------------------------------------------------
            
            //----- Specify conditions for communication start -----
            while( /*!m_bConnectedToDevice ||*/ !m_bConsumerIsConnected ) { 
                // Console.WriteLine("Waiting until conditions are satisfied");
                await Task.Delay(1000);}
               // N.B.: We use a slow poll loop instead of a busy-wait loop which is possible 
               //       because the main method is specified as a async task.
            //------------------------------------------------------ 

            //------ Communication Functions -----------------
            //process_and_forward_fci_data();
            //log_live_data();
            //update_component_test_function();
            //process_and_upload_fci_data();
            downstream_simulation_forwarding();
            //process_and_forward_to_simulation();
            //-------------------------------------------------
            
            Console.WriteLine("Press Enter to end program ...");
            Console.ReadLine();
        }
        
        // Establishes connection with a tcp client, for instance the panda robot
        static void initialize_device_connection()
        {
            m_bConnectedToDevice = false;
            m_tryConnectTimer = new System.Timers.Timer(5000);
            m_tryConnectTimer.Elapsed += call_reconnect;
            m_tryConnectTimer.Start();
        }

        // Calls the function for connection establishment
        static void call_reconnect(Object source, ElapsedEventArgs e){
            if(!m_bConnectedToDevice)
                try_device_connection();
        }

        // Creates the tcp client and network stream for the device
        static void try_device_connection()
        {   
            Console.WriteLine("[INFO] Trying to connect to device.");
            try
            {
                // Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // IPAddress subscriberAddr = IPAddress.Parse("10.157.175.17");
                // int subscriberPort = SUBSCRIBER_PORT;
                // IPEndPoint endPoint = new IPEndPoint(subscriberAddr, subscriberPort);
                // socket.Bind(endPoint);


                m_deviceClient = new TcpClient(m_deviceIP, m_devicePort);
                m_deviceClientStream = m_deviceClient.GetStream();
                m_bConnectedToDevice = true;
                Console.WriteLine("[INFO] Connected with device.");
                //if(m_bConnectedToDevice && m_bConsumerIsConnected)
                //    process_and_forward_fci_data();
            }
            catch(SocketException e)
            {
                Console.WriteLine("[WARN] Socket error occured.");
            }
            catch
            {
                Console.WriteLine("[WARN] Error in connection establishment occured.");
            }       
        }

        static void initialize_consumer_connection()
        {
            m_tcpServer = new TcpListener(IPAddress.Any, m_serverPort);
            m_tcpServer.Start();
            m_timerAcceptClient = new System.Timers.Timer(5000);
            m_timerAcceptClient.Elapsed += call_accept;
            m_timerAcceptClient.Start();

        }

        static void call_accept(Object source, ElapsedEventArgs e){
            if(!m_bConsumerIsConnected)
                try_accept_consumer();
        }

        static void try_accept_consumer()
        {
            try
            {
                m_tcpClientConsumer = m_tcpServer.AcceptTcpClient();
                m_consumerStream = m_tcpClientConsumer.GetStream();
                m_bConsumerIsConnected = true;
                Console.WriteLine("[INFO] Consumer is connected");
                //if(m_bConnectedToDevice && m_bConsumerIsConnected)
                //    process_and_forward_fci_data();
            }
            catch
            {
                 Console.WriteLine("[WARN] Error in consumer connection establishment.");
            }
        }

        /*  Reads the data of the network stream in short intervals and
         *  processes the bytes such that the relevant information is
         *  forwarded to the consumer.
         */
       
        // @brief logs the incoming data or parts of it in the console at a specified interval
        static void log_live_data()
        {
            // Analysis variables
            int average_package_size = 2500;     // Good values: 2500 with 5
            int feasible_read_interval_time = 5; // Also 2 ms worked with parsing.
            int necessary_buffer_size = average_package_size*feasible_read_interval_time;

            // Define buffer for the data in both raw bytes and ascii format
            Byte[] fci_data_bytes = new Byte[necessary_buffer_size]; // IMPORTANT TO KNOW THE SIZE HERE
            String fci_data_ascii = String.Empty;
            Int32 num_bytes_read = 0;
                       
            // Time interval for logging
            int log_interval_ms = 1000; 
            long last_log_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            bool continueReading = true; // Flag that can stop the reading process
            Timer maxReadTestTimer = new System.Timers.Timer(10000); // Timer for whole reading process [ms]
            maxReadTestTimer.Elapsed += breakReadLoop; // Connect timer to event that sets stop flag
            maxReadTestTimer.Enabled = true; // Start timer
            Timer timerReadData = new System.Timers.Timer(feasible_read_interval_time);
            timerReadData.Elapsed += readData;
            timerReadData.Enabled = true;
            
            // Variables for stream data processing
            int indexOpen = 0;
            int indexClosed = 0;
            JsonDocument genericJsonObject;
            JsonElement root;
            JsonElement time; // Buffer for the total operating time
            JsonElement q; // Buffer for the joint positions
            JsonElement O_T_EE; // Buffer for the end effector pose in the base frame
            Console.WriteLine("Finished initialization.");

            // Sets flag that stops reading of the data stream
            void breakReadLoop(Object source, ElapsedEventArgs e){
                continueReading = false;
                maxReadTestTimer.Stop();
                maxReadTestTimer.Dispose();
            };
            
            void readData(Object source, ElapsedEventArgs e)
            {
                // Read data from the socket 
                num_bytes_read = m_deviceClientStream.Read(fci_data_bytes, 0, fci_data_bytes.Length);
                // And convert to ascii string
                fci_data_ascii = System.Text.Encoding.ASCII.GetString(fci_data_bytes, 0, num_bytes_read);  
               
                // In this asci string we need to extract the robot state data inside two '{' '}'
                indexOpen = fci_data_ascii.IndexOf('{');
                indexClosed = fci_data_ascii.IndexOf('}');

                // If there is a Json object on it we try to parse it
                // By the if condition we ensure that a valid object exits
                if(indexClosed > indexOpen && indexOpen != -1)
                {
                    genericJsonObject = JsonDocument.Parse(fci_data_ascii);
                    root = genericJsonObject.RootElement;   
                    O_T_EE=  root.GetProperty("O_T_EE");     
                    
                    if(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - last_log_time > log_interval_ms){   
                       
                        //log_data(O_T_EE.ToString(), "test4");
                        last_log_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    }
                }
            }       
        }

        /*  Reads and deserializes the device data stream
         *  and extracts relevant data to subsequently
         *  call the functions that create the JSON Patch Docuemtns to update the twin instance.
         */
        
        static void downstream_simulation_forwarding()
        {
            // Initialize the downstream access timers
            m_downstreamTimer.Elapsed += call_downstream_access;   // Function to handle timer event
            m_downstreamTimer.Enabled = true;                      // Start the timer 
            m_maxTimeDownstream.Elapsed += stop_downstream_access; // Clean up after timeout
            m_maxTimeDownstream.Enabled = true;                    // Start max connection timer
            Console.WriteLine("[INFO] Started accessing the Azure downstream");

            //@brief Called when the timer for downstream access elapsas and handels
            //        the actual call to the methods that accesses Azure.
            void call_downstream_access(Object source, ElapsedEventArgs e){
                donwstream_access();
                //TODO Hou: see if the downstream process is running
                Console.WriteLine("[INFO] Downstream is sucessfully accessed");
            }

            //@brief Implements our azure downstream, i.e. it gets the relevant data from the digitial
            //        twin instance and sends it to further processes, e.g. omniverse.
            void donwstream_access()
            {
                string component_value_result = String.Empty;          
                BasicDigitalTwin twin_value; 
                string buffer = "[";
                        
                foreach (var joint_twin in m_downstreamTwins)
                {

                    Response<BasicDigitalTwin> getTwin_Response = m_azureClient.GetDigitalTwin<BasicDigitalTwin>(joint_twin);
                    twin_value = getTwin_Response.Value;
                    
                    component_value_result = twin_value.Contents["value"].ToString();

                    Console.WriteLine("[INFO] Started accessing the Azure downstream"+twin_value);
                    Console.WriteLine(component_value_result);
                    buffer += component_value_result;
                    buffer += ",";
                }
                buffer = buffer.Remove(buffer.Length -1);
                buffer += "]";

                byte[] byte_buffer = new byte[100]; 
                byte_buffer = Encoding.Default.GetBytes(buffer);  // convert string to an byte array
                m_consumerStream.Write(byte_buffer, 0, byte_buffer.Length);     //sending the message
            }

            // Clean up all timers and close the connections
            void stop_downstream_access(Object source, ElapsedEventArgs e){
                m_downstreamTimer.Stop();
                m_downstreamTimer.Dispose();
                m_maxTimeDownstream.Stop();
                m_maxTimeDownstream.Dispose();
                m_consumerStream.Close();
                m_tcpClientConsumer.Close();
                Console.WriteLine("[END] Finished downstream access");
            }
        }

        // @brief Stores all recieved json packages in a file for replay purposes.
        static void log_data(string json_data_string, string log_file_name="SomeLog"){
            using(StreamWriter log_stream = File.AppendText(log_file_name+".txt"))
            {
                string timestamp = DateTime.Now.ToString("hh.mm.ss.ffffff");
                log_stream.WriteLine(timestamp+" "+json_data_string);
            }
        }
    }
}