using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
    

/*   This is the UDPSend console app.  It's designed with the connectionless protocols of UDP in mind.
 *   For testing you need a sender and a receiver.  This is the sender, which can be easily paired
 *   with UDPListener for end to end testing.  It can also be called from a batch or a script.
 *   Authored by Joe Morehead                                                                             */

namespace UDPSend
{
    class Program
    {
        /* This is the main module.  The user will submit the command UDPSend <IP Address> <Port>
           This module grabs the IP Address and port as command line arguments (args[0] and args[1] 
           respectively.
           If the user omits the destination IP Address or the port number, throw an exception.
           It uses the other modules for Hostname and IP Address validation, and name lookup
           as well as sending the IP address and port to the sendpackets module                     */

        public static int Main(string[] args)
        {
            try
            {
                string DestinationIPAddress = args[0];
                string DestinationPort = args[1];

                // if the user enters localhost, FQDN or a hostname replace it with an IP Address

                if (!ValidateIPv4(args[0]))  
                {
                    DestinationIPAddress = ConvertToIPAddress(args[0]);
                }
                
                // Send the host IP address and the destination port to the SendUDPPacket module to format and send
                
                SendUDPPacket(DestinationIPAddress, DestinationPort);
            }

            // what to do if the user omits IP address or the port, and IndexOutOfRange Ex thrown

            catch (IndexOutOfRangeException)  
            {
                PrintOutput("\nMissing command line parameters:  UDPSend <Hostname or IP Address> <port> ");
            }

            // if the hostname is not recognized through DNS, maybe a typo, or wrong hostname or FQDN

            catch (Exception ex)  
            {
                PrintOutput("\nAn error occurred:  " + ex.Message + "\n");
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
    /*  This sets the default to loopback tests incoming value for a formatted IP Address, 
        makes sure the destination is an IP address instead of a host name or FQDN
        The ! override is used to ignore the nullable reference warning.                   */
        
        public static bool ValidateIPv4(string IncomingAddress)
        {
            IPAddress AddressToReturn; // = IPAddress.Parse("127.0.0.1");
            return IncomingAddress != null && IncomingAddress.Count(c => c == '.') == 3 &&
                IPAddress.TryParse(IncomingAddress, out AddressToReturn!);
        }
        
    /*  This is the module that will process a FQDN, a hostname or localhost and convert it to 
        an IP Address for the console app to use, since it works with IP Addresses instead of 
        Host names or other references                                                           */

        
        public static string ConvertToIPAddress(string FullHostName)
        {
            string IPAddressToReturn;
            if (FullHostName.ToUpper() == "LOCALHOST" || FullHostName.ToLower() == "localhost")
            {
                IPAddressToReturn = "127.0.0.1";
            }
            else
            {
                string HostNameToInterrogate = FullHostName;
                IPAddress[] FullAddressList = Dns.GetHostAddresses(HostNameToInterrogate);
                IPAddressToReturn = "";
                foreach (IPAddress IndividualAddress in FullAddressList)
                {
                    IPAddressToReturn = (IndividualAddress.ToString());
                }
            }
            return IPAddressToReturn;
        }


        /* This module prints the error line, used if an exception is thrown.  
           Set the color to yellow, print then back to white                            */

        public static void PrintOutput(string ErrorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(ErrorMessage);
            Console.ResetColor();
        }

        /* This is the module that formats the UDP Packet and sends it to destination, 
           notify the user the send operation is complete                                */

        public static void SendUDPPacket(string DestinationIPAddress, string DestinationPort)
        {
            
            // Prep the socket connection

            System.Net.Sockets.UdpClient sock = new System.Net.Sockets.UdpClient();  
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(DestinationIPAddress), (Convert.ToInt32(DestinationPort)));
            byte[] data2 = Encoding.ASCII.GetBytes("UDP sent from " + GetLocalIPAddress());
            
            // Send the packet toward the destination

            sock.Send(data2, data2.Length, iep);  
            
            // close the connection and print the send completion message to the local user
            
            sock.Close();  
            Console.ForegroundColor = ConsoleColor.Blue;
            string tstamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            Console.WriteLine("\n" + tstamp + " Message sent to " + DestinationIPAddress + " on port " + Convert.ToString(DestinationPort));
            Console.ResetColor();
        }
    }
}

