using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

/*   This is the UDPSend console app.  It's designed with the connectionless protocols of UDP in mind.
 *   For testing you need a sender and a receiver.  This is the receiver, which can be easily paired
 *   with UDPSend for end to end testing.  It can also be called from a batch or a script.
 *   Authored by Joe Morehead                                                                             */
public class UDPListener
{

    /*  This is the main module, grab the port number to listen on, and trap the exceptions
        if the user inadvertently omits the port number or enters a bogus port number        */

    public static int Main(String[] args)
    {
        string ErrorMessage;
 
        try
        {
            int intPort = Convert.ToInt32(args[0]);
            StartListener(intPort);
        }
        
        //  This is the exception thrown if the port command line parameter is missing
        
        catch (IndexOutOfRangeException)
        {
            ErrorMessage = ("\nMissing command line parameters:  UDPListener <port number> \n");
            PrintOutput(ErrorMessage);
        }
        
        //  This is the exception thrown if the user enters an invalid port on the command line
        
        catch (FormatException)
        {
            ErrorMessage = ("\nPlease check your command line parameters:  UDPListener <port number> \n");
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
            Console.WriteLine("\n" + ErrorMsg);
            Console.ForegroundColor = ConsoleColor.White;
        }

    //  Take the port from the command line and set up the listener

    public static void StartListener(int ListenPort)
    {
        string ErrorMessage;
 
        // Most of the issues will be caught as exceptions in the main module,
        // but this module still uses try / catch to mitigate any unanticipated exceptions
        
        try
        {
 
            int listenPort;
            listenPort = ListenPort;
            IPAddress LocalIPAddress; // = "";
            UdpClient listener = new UdpClient(listenPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            
            // This while loop keeps the process listening, CTRL^C to exit
            
            while (true)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                LocalIPAddress = GetLocalIPAddress();
                Console.WriteLine("\nHost IP Address: " 
                    + LocalIPAddress 
                    + " :: Listening for UDP on port " 
                    + Convert.ToString(listenPort) + "                         ...(CTRL^C to exit)");
                Console.ForegroundColor = ConsoleColor.White;

                // Set up a buffer to grab any incoming messages from the remote client

                byte[] bytes = listener.Receive(ref groupEP);
                Console.ForegroundColor = ConsoleColor.Green;
                string tstamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                Console.WriteLine("\n" + tstamp 
                    + $" Received UDP packet from {groupEP} :" 
                    + $" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
 
    /*  Should the user inadvertently choose a port that is in use 
        -- notify the user
            
        On Linux, if you enter a port lower than 1025 it will throw 
        this error.   

        On Linux, the UDPListener command requires 'sudo' to run on
        ports lower than 1025 due to Operating system design                       */

        catch (SocketException)
        {

                PrintOutput("\nSocket already in use by another UDP service.\n");
        }
        
        catch (Exception ex)
        {
            ErrorMessage = ("\nUnknown error: " + ex + "\n");
            PrintOutput(ErrorMessage);
        }
    }
}