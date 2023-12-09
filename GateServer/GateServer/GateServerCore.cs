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
        Socket? ListenSocket;
        CancellationTokenSource? SocketCancelToken;
        bool IsLoginServerConnected = false;
        bool IsUserWantCancel = false;
        Socket? LoginSock;
        public GateServerCore(GateServerForm? Owner)
        {
            MainForm = Owner;
        }

        public bool InitServer()
        {
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketCancelToken = new CancellationTokenSource();
            if (ListenSocket == null)
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
        public async Task Run()
        {
            try
            {
                MainForm!.AddLogWithTime("로그인 서버의 연결을 대기중입니다.");
                MainForm!.SetLoginServerConnecting();
                while(!SocketCancelToken!.IsCancellationRequested && !IsLoginServerConnected)
                {
                    LoginSock = await ListenSocket!.AcceptAsync(SocketCancelToken.Token);
                    if(!IsLoginServerConnected)
                    {
                        if (!CheckConnectionLoginServer(LoginSock))
                            continue;
                        else
                            break;
                    }
                }
                IsLoginServerConnected = true;
                MainForm!.AddLogWithTime("로그인 서버와 연결성공!");
                MainForm!.SetLoginServerConnected();
                while(!SocketCancelToken!.IsCancellationRequested)
                {
                    // 임시 체크용
                    Socket TempS = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    TempS.Bind(new IPEndPoint(IPAddress.Any, 15523));
                    TempS.Listen(1000);
                    Socket Sock = await TempS!.AcceptAsync(SocketCancelToken.Token);
                }
            }
            catch(OperationCanceledException ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
            }
            catch(SocketException ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
            }
            catch(Exception ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
            }
            finally
            {
                ListenSocket!.Close();
                MainForm!.AddLogWithTime("게이트 서버의 통신을 종료합니다.");
                MainForm!.SetAllServerStopConnect();
            }
        }
        private bool CheckConnectionLoginServer(Socket Sock)
        {
            if (Sock == null)
                return false;
            if (Sock.RemoteEndPoint == null)
                return false;
            if (!(Sock.RemoteEndPoint is IPEndPoint CheckAddr))
                return false;
            if (CheckAddr == null)
                return false;
            if (CheckAddr.Address.ToString() == Settings.Default.LoginServerAddr)
                return true;
            return false;
        }
    }
}
