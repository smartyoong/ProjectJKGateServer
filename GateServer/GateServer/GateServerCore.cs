using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GateServer.Properties;

namespace GateServer
{
    public class GateServerCore
    {
        public GateServerForm? MainForm { get; set; }
        Socket? ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public bool InitServer()
        {
            if(ListenSocket == null)
                return false;
            try
            {
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Default.Port));
            }
            catch (Exception ex) 
            {

            }
        }
    }
}
