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
        public GateServerForm? MainForm { get; }
        Socket? ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public GateServerCore(GateServerForm? Owner)
        {
            MainForm = Owner;
        }

        public bool InitServer()
        {
            if(ListenSocket == null)
                return false;
            try
            {
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Default.Port));
                ListenSocket.Listen(1000);
            }
            catch (Exception ex) 
            {
                string[] lines = ex.StackTrace!.Split('\n');
                foreach (string line in lines)
                {
                    MainForm!.AddLogWithTime(line);
                }
                MainForm!.AddLogWithTime(ex.Message);
                return false;
            }
            return true;
        }
    }
}
