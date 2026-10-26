using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Text;

namespace TCPServer
{
    internal class Server
    {
        public Server()
        {
            
        }

        public async void InitServer()
        {
            var hostName = Dns.GetHostName();
            IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
            // This is the IP address of the local machine
            IPAddress localIpAddress = localhost.AddressList[0];
        }
    }
}
