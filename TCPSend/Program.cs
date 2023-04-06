using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

/*   This is the TCPSend console app.  It's designed with the connection-oriented 
     protocols of TCP in mind.   For testing you need a sender and a receiver.  
     This is the sender, which can be easily paired with TCPListener for end-to-end 
     testing.  It can also be called from the command line, a batch or a script.
     Authored by Joe Morehead                                                         */

public class TCPSend
{
    /* This is the main routine.
       Grab the Target IP Address and target port from the command line
       Send to the validation module for the IP Address
       Send to the conversion module if it is a host name
       Feed both to the StartClient routine                                           */

    public static int Main(string[] args)
    {
        
        try
        {
            int ListenPort = Convert.ToInt32(args[1]);
            IPAddress TargetAddress = IPAddress.Parse("127.0.0.1"); 
            if (ValidateIPv4(args[0]))
            {
                TargetAddress = IPAddress.Parse(args[0]);
            }
            else
            {
                TargetAddress = IPAddress.Parse(ConvertToIPAddress(args[0]));
            }
            if ((args[0]) == ("127.0.0.1") || (args[0]) == ("::1"))
            {
                TargetAddress = GetLocalIPAddress();
            }
            StartClient(TargetAddress, ListenPort, 700);    
        }

        // what to do if the user omits IP address or the port, and IndexOutOfRange Ex thrown

        catch (IndexOutOfRangeException)  
        {
            PrintOutput("\nMissing command line parameters:  TCPSend <Hostname or IP Address> <port>", true);
        }

        // if the hostname is not recognized through DNS, maybe a typo, or wrong hostname or FQDN

        catch (Exception)  
        {
            PrintOutput("\nFormat error:  TCPSend <Hostname or IP Address> <port>", true);
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
 /* This is the validation function to make sure the process has a valid IP address
    to use for the destination, since the TryPing module only works with IP Addresses  */

    public static bool ValidateIPv4(string IPAddressToTest)
    {
        IPAddress ResultAddress = null!;
        return IPAddressToTest != null && IPAddressToTest.Count(c => c == '.') == 3 &&
            IPAddress.TryParse(IPAddressToTest, out ResultAddress!);
    }
    
/* This is the module that will process a (host reference) FQDN, a hostname or localhost 
   and convert it to an IP Address, and verify there is an IP Address resolution 
   for the host reference. Basically making sure there is a host where the user has 
   specified the destination
   NOTE:  Replace "LOCALHOST" with the actual network address for the StartClient module  */
        
    public static string ConvertToIPAddress(string FullHostName)
    {
        string IPAddressToReturn = "";
        if (FullHostName.ToUpper() == "LOCALHOST" || FullHostName.ToLower() == "localhost")
        {

            // if the user enters

            string HostNameToInterrogate = System.Net.Dns.GetHostName();
            IPAddress[] FullAddressList = Dns.GetHostAddresses(HostNameToInterrogate);
            foreach (IPAddress IndividualAddress in FullAddressList)
            {
                IPAddressToReturn = Convert.ToString(IndividualAddress)!;
            }
        }
        else
        {
            string HostNameToInterrogate = FullHostName;
            IPAddress[] FullAddressList = Dns.GetHostAddresses(HostNameToInterrogate);
            foreach (IPAddress IndividualAddress in FullAddressList)
            {
                IPAddressToReturn = Convert.ToString(IndividualAddress)!;
            }
        }
        return IPAddressToReturn;
    }

/*  This is the print output module for exceptions and connection errors
    exceptions are printed in yellow and connection errors in red              */

    public static void PrintOutput(string PrintMessage, bool errorflag)
    {
        if (errorflag)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else 
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        Console.WriteLine(PrintMessage);
        Console.ResetColor();
    }

/*  This is the client routine that formats a TCP packet and sends
    information to the destination node after getting the destination
    address and port.  It uses a socket, sends a message which is 
    formatted and displayed on the destination via the accompanying
    TCPListener console app.  An actual connection is formed and 
    acknowledged on both ends, sender and receiver.                     */
        
    public static void StartClient(IPAddress TargetAddress, int ListenPort, int Timeout)
    {
        string ErrorMessage = "";
        string TimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        try
        {
            
    /*
        Create a socket, feed the Destination IP Address, port and timeout length
        Use IAsyncResult interface along with the AsyncWaitHandle to facilitate the 
        timeout feature.  That's to speed up the response when no connection is available. 
                                                                                                 */ 

            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
   
            IAsyncResult result = sck.BeginConnect( TargetAddress, ListenPort, null, null );

            bool success = result.AsyncWaitHandle.WaitOne( Timeout, true );

            // If the socket connects = Success, exchange TCP comms with the destination

            if ( sck.Connected )
            {
                byte[] bytes = new byte[1024];
                byte[] message = Encoding.ASCII.GetBytes(
                    " (incoming) TCP connection established from " 
                    + GetLocalIPAddress() + ":" 
                    + ((IPEndPoint)sck.LocalEndPoint!).Port.ToString() + "<EOF>");

            /*  Send the data through the socket.
                The data will be processed as 'proof'
                on the opposite end                        */
   
                int bytesSent = sck.Send(message);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n" + TimeStamp 
                + " (outgoing) TCP connection established to " 
                + TargetAddress + ":" + Convert.ToString(ListenPort) 
                + " [SUCCESS]");
                Console.ResetColor();
                int bytesRec = sck.Receive(bytes);

                // Release the socket.

                sck.Shutdown(SocketShutdown.Both);
                sck.Close();
            }

            //  If the connection fails, clean up and send a message to the console

            else 
            {
               sck.Close();
               sck.Dispose();
               ErrorMessage += ("\n" + TimeStamp + " (outgoing) TCP connection to " 
                    + TargetAddress + " on port " 
                    + Convert.ToString(ListenPort) + " [FAILED]");
                PrintOutput(ErrorMessage, false); 
            }
            
        }

        //  Catch any other errors and send the output to the console

        catch(Exception e)
        {
            PrintOutput(e.Message, false);    
        }
    }
}