using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        const int HeaderSize = sizeof(int);
        public GateServerForm? MainForm { get; }
        Socket? ListenSocket;
        CancellationTokenSource? SocketCancelToken;
        bool IsLoginServerConnected = false;
        bool IsUserWantCancel = false;
        Socket? LoginSock;
        Dictionary<string, Socket> ConnectedUsers;
        public GateServerCore(GateServerForm? Owner)
        {
            MainForm = Owner;
            ConnectedUsers = new Dictionary<string, Socket>();
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
                while(!SocketCancelToken!.IsCancellationRequested && !IsLoginServerConnected && !IsUserWantCancel)
                {
                    MainForm!.AddLogWithTime("로그인 서버의 연결을 대기중입니다.");
                    MainForm!.SetLoginServerConnecting();
                    LoginSock = await ListenSocket!.AcceptAsync(SocketCancelToken.Token);
                    if(!IsLoginServerConnected)
                    {
                        if (!CheckConnectionLoginServer(LoginSock))
                            continue;
                        IsLoginServerConnected = true;
                        MainForm!.AddLogWithTime("로그인 서버와 연결성공!");
                        MainForm!.SetLoginServerConnected();
                    }
                    Task ProcessLoginTask = Task.Run(() => { RecvDataFromLoginServer(LoginSock); },SocketCancelToken.Token);
                    await Task.WhenAny(ProcessLoginTask);
                }
            }
            catch(OperationCanceledException ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
                IsUserWantCancel = true;
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
        private void RecvDataFromLoginServer(Socket LoginSock)
        {
            try
            {
                while (!SocketCancelToken!.IsCancellationRequested && LoginSock.Connected)
                {
                    int ReceviedData = 0;
                    byte[] HeadByte = new byte[HeaderSize];
                    ReceviedData = LoginSock.Receive(HeadByte,HeaderSize,SocketFlags.None);
                    if (ReceviedData <= 0)
                    {
                        MainForm!.AddLogWithTime("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }
                    int PacketSize = BitConverter.ToInt32(HeadByte, 0);
                    byte[] ID = new byte[sizeof(LOGIN_TO_GATE_PACKET_ID)];
                    ReceviedData =  LoginSock.Receive(ID,PacketSize,SocketFlags.None);
                    if (ReceviedData <= 0)
                    {
                        MainForm!.AddLogWithTime("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }
                    LOGIN_TO_GATE_PACKET_ID IDNumber = (LOGIN_TO_GATE_PACKET_ID)BitConverter.ToUInt32(ID,0);
                    byte[] Data = new byte[PacketSize = sizeof(LOGIN_TO_GATE_PACKET_ID)];
                    ReceviedData = LoginSock.Receive(Data,PacketSize-sizeof(LOGIN_TO_GATE_PACKET_ID),SocketFlags.None);
                    if (ReceviedData <= 0)
                    {
                        MainForm!.AddLogWithTime("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }
                    ProcessLoginData(IDNumber, Data);
                }
                IsLoginServerConnected = false;
            }
            catch (OperationCanceledException ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
                IsLoginServerConnected = false;
                IsUserWantCancel = true;
            }
            catch (SocketException ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
                MainForm!.AddLogWithTime("로그인 서버와의 연결이 끊겼습니다.");
                MainForm!.SetLoginServerStopConnected();
                IsLoginServerConnected = false;
            }
            catch (Exception ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
            }
        }
        private void ProcessLoginData(LOGIN_TO_GATE_PACKET_ID ID ,byte[] Data)
        {
            switch(ID)
            {
                case LOGIN_TO_GATE_PACKET_ID.ID_NEW_USER_TRY_CONNECT:
                    LoginToGateConnect PacketData = SocketDataSerializer.DeSerialize<LoginToGateConnect>(Data);
                    MainForm!.AddLogWithTime(PacketData.UserName);
                    MainForm!.IncDecUserCount(true);
                    break;
                default:
                    MainForm!.AddLogWithTime("알수 없는 ID입니다. ProcessLoginData");
                    break;
            }
        }
    }
}
