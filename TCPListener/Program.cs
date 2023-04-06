using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

/*   This is the TCPListener console app.  It's designed with the connection-oriented protocols of TCP in mind.
     For testing you need a sender and a receiver.  This is the receiver, which can be easily paired
     with TCPSend for end to end testing.  It can also be called from a batch or a script. 
     Authored by Joe Morehead                                                                             */

public class TCPListener
{

    /*  This is the main module, grab the port number to listen on, and trap the exceptions
        if the user inadvertently omits the port number or enters a bogus port number                     */
    public static int Main(String[] args)
    {
       string ErrorMessage = "";
 
        try
        {
            int intPort = Convert.ToInt32(args[0]);
            StartListener(intPort);
        }

        //  This is the exception thrown if the port command line parameter is missing

        catch (IndexOutOfRangeException)
        {
            ErrorMessage += ("Missing command line parameters:  TCPListener <port number> ");
            PrintOutput(ErrorMessage);
        }

        //  This is the exception thrown if the user enters an invalid port on the command line

        catch (FormatException)
        {
            ErrorMessage = ("Please check your command line parameters:  TCPListener <port number> ");
            PrintOutput(ErrorMessage);
        }
        return 0;
    }

    /* Since the user does not enter the local IP Address, grab the IP Address from the system
       for simplifying analysis of the output.                                                  */

    public static IPAddress GetLocalIPAddress()
    {
        StringBuilder sb = new StringBuilder();
        IPAddress TargetAddress = IPAddress.Parse("127.0.0.1");
        
        // Get a list of all the network interfaces, we need one working, for sure
        
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
    
        foreach (NetworkInterface network in networkInterfaces)
        {
            // Read the IP configuration for each interface

            IPInterfaceProperties properties = network.GetIPProperties();
    
            // Each network interface may have multiple IP addresses
        
            foreach (IPAddressInformation address in properties.UnicastAddresses)
            {

             /* Weed out tunnel adapters, loopback and IPv6             */

                if (network.OperationalStatus.ToString() == "Down")
                    continue;
                if (network.NetworkInterfaceType.ToString() == "Loopback")
                    continue;
                if (network.Name == "(lo)")
                    continue;
                if (network.Description == "lo")
                    continue; 
                if (address.Address.AddressFamily == AddressFamily.InterNetworkV6) 
                    continue;
                if (!(address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                    continue;
            
            /*  If the data makes it this far in the flow, set the TargetAddress
                set the output address and return it                                          */
                
                TargetAddress = address.Address;
                return TargetAddress;
            }
        }
        return TargetAddress;
    }

    //  This is the print module for errors and exceptions

    public static void PrintOutput(string ErrorMsg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(ErrorMsg);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void StartListener(int ListenPort)
    {

        /* Grab the actual IP Address of the local interface
           Set up the endpoints for the TCP Socket / Thread              */
        
        IPAddress LocalIPAddress = GetLocalIPAddress();
        IPEndPoint localEndPoint = new IPEndPoint(LocalIPAddress, ListenPort);

        try {

            // Create a Socket that will use Tcp protocol

            Socket listener = new Socket(LocalIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Have to associate the socket with an endpoint using bind 

            listener.Bind(localEndPoint);

        /*  Specify how many requests a Socket can listen before it gives Server busy response.
            Handle 10 requests at a time                                                        */

            listener.Listen(10);
            Console.WriteLine("\nHost IP Address: " + LocalIPAddress 
                    + " :: Listening for TCP on port " + 
                    Convert.ToString(ListenPort) 
                    + "                         ...(CTRL^C to exit)");
            while (true)
            {
                Socket client = listener.Accept();

            /*  Incoming data from the client. Using multiple threads and sockets
                to eliminate issues with the loop and freeing ports                         */

                string data = ""; 
                byte[] bytes; 
                var childsocketThread = new Thread(() =>
                {
                    bytes = new byte[1024];
                    int bytesRec = client.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    string tstamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                    string ResultsMessage = "\n" + tstamp + " ";

                    //  Format the message and remove "<EOF>" from the end for clarity
                    
                    for (int i = 0; i < bytesRec; i++)
                    {
	                    String strEOF = "<EOF>";
                        Boolean result = strEOF.Contains(data[i].ToString());
                        if(!result)
                        {
                            ResultsMessage += (Convert.ToChar(data[i]));  
                        }
                    }
                    
                    // Print the [SUCCESS] confirmation to the console 

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(ResultsMessage);
                    Console.ResetColor();

                    //  Send the confirmation response to the client

                    byte[] msg = Encoding.ASCII.GetBytes(data);
                    client.Send(msg);
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                });

            /*  The process was comprehensively defined in the previous steps 
                now start the process                                            */

                childsocketThread.Start();
            }

        }

    /*  Should the user inadvertently choose a port that is in use 
        -- notify the user
        
        On Linux, if you enter a port lower than 1025 it will throw 
        this error.   

        On Linux, the TCPListener command requires 'sudo' to run on
        ports lower than 1025 due to Operating system design                       */
        
        catch (SocketException)
        {
            
                PrintOutput("Socket already in use by another TCP service.");
        }
        catch (Exception e)
        {

        //  Print any other errors or exceptions  
        
            PrintOutput(e.ToString());
        }
        
    }
}