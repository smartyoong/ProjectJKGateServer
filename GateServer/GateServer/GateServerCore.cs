using System;
using System.Collections.Concurrent;
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
        BlockingCollection<ClientPacketMessage>? ClientPacketMessagesQueue;
        List<Task>? ClientTasks;
        const int HeaderSize = sizeof(int);
        public GateServerForm? MainForm { get; }
        Socket? ListenSocket;
        CancellationTokenSource? SocketCancelToken;
        CancellationTokenSource? QueueCancelToken;
        bool IsLoginServerConnected = false;
        bool IsUserWantCancel = false;
        Socket? LoginSock;
        Dictionary<string, Socket> ConnectedUsers;
        Task? ProcessLoginTask;
        public GateServerCore(GateServerForm? Owner)
        {
            MainForm = Owner;
            ConnectedUsers = new Dictionary<string, Socket>();
        }

        public bool InitServer()
        {
            ClientTasks = new List<Task>();
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketCancelToken = new CancellationTokenSource();
            ClientPacketMessagesQueue = new BlockingCollection<ClientPacketMessage>();
            QueueCancelToken = new CancellationTokenSource();
            IsUserWantCancel = false;
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
                while (!SocketCancelToken!.IsCancellationRequested && !IsLoginServerConnected && !IsUserWantCancel)
                {
                    MainForm!.AddLogWithTime("로그인 서버의 연결을 대기중입니다.");
                    MainForm!.SetLoginServerConnecting();
                    LoginSock = await ListenSocket!.AcceptAsync(SocketCancelToken.Token);
                    if (!IsLoginServerConnected)
                    {
                        if (!CheckConnectionLoginServer(LoginSock))
                            continue;
                        IsLoginServerConnected = true;
                        MainForm!.AddLogWithTime("로그인 서버와 연결성공!");
                        MainForm!.SetLoginServerConnected();
                        ProcessLoginTask = Task.Run(() => { RecvDataFromLoginServer(LoginSock); }, SocketCancelToken.Token);
                        RunQueue();
                    }
                    else if(IsLoginServerConnected)
                    {
                        ClientTasks!.Add(Task.Run(() => ClientRun(LoginSock!),SocketCancelToken.Token));
                    }
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
                if (ClientTasks != null && ClientTasks.Count != 0)
                {
                    await Task.WhenAll(ClientTasks);
                    MainForm!.AddLogWithTime($"{ClientTasks.Count} Client tasks 종료완료");
                }
                MainForm!.AddLogWithTime("게이트 서버 종료 완료");
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
                    ReceviedData =  LoginSock.Receive(ID, sizeof(LOGIN_TO_GATE_PACKET_ID), SocketFlags.None);
                    if (ReceviedData <= 0)
                    {
                        MainForm!.AddLogWithTime("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }
                    LOGIN_TO_GATE_PACKET_ID IDNumber = (LOGIN_TO_GATE_PACKET_ID)BitConverter.ToUInt32(ID,0);
                    byte[] Data = new byte[PacketSize - sizeof(LOGIN_TO_GATE_PACKET_ID)];
                    ReceviedData = LoginSock.Receive(Data,PacketSize-sizeof(LOGIN_TO_GATE_PACKET_ID),SocketFlags.None);
                    if (ReceviedData <= 0)
                    {
                        MainForm!.AddLogWithTime("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }
                    ProcessLoginData(IDNumber, ref Data, LoginSock);
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
        private void ProcessLoginData(LOGIN_TO_GATE_PACKET_ID ID ,ref byte[] Data, Socket ClientSock)
        {
            switch(ID)
            {
                case LOGIN_TO_GATE_PACKET_ID.ID_NEW_USER_TRY_CONNECT:
                    LoginToGateServer PacketData; 
                    PacketData = SocketDataSerializer.DeSerialize<LoginToGateServer>(Data);
                    MainForm!.AddLogWithTime($"{PacketData.UserName}님이 접속하셨습니다.");
                    MainForm!.IncDecUserCount(true);
                    ConnectedUsers.Add(PacketData.UserName, ClientSock);
                    break;
                default:
                    MainForm!.AddLogWithTime("알수 없는 ID입니다. ProcessLoginData");
                    break;
            }
        }
        public void Cancel()
        {
            SocketCancelToken!.Cancel();
            QueueCancelToken!.Cancel();
            ClientPacketMessagesQueue!.CompleteAdding();
        }
        public async Task ClientRun(Socket ClientSocket)
        {
            byte[] HeadBuffer = new byte[sizeof(int)];
            int RecvDataLength = 0;
            do
            {
                RecvDataLength = await ClientSocket.ReceiveAsync(HeadBuffer, SocketFlags.None);
                if(RecvDataLength == 0)
                {
                    break;
                }
                int PacketSize = BitConverter.ToInt32(HeadBuffer, 0);
                byte[] Packet = new byte[PacketSize];
                RecvDataLength = ClientSocket.Receive(Packet, PacketSize, SocketFlags.None);
                if (RecvDataLength  == 0)
                {
                    break;
                }
                Array.Clear(HeadBuffer, 0, HeadBuffer.Length);
                AddQueueMessage(Packet, ClientSocket);
            }
            while (RecvDataLength > 0);
        }

        private void AddQueueMessage(byte[] Message, Socket Sock)
        {
            byte[] IDData = new byte[sizeof(uint)];
            byte[] Data = new byte[Message.Length - sizeof(uint)];
            Array.Copy(Message,IDData,sizeof(int));
            Array.Copy(Message,IDData.Length,Data,0,Data.Length);
            CLIENT_TO_GATE_PACKET_ID ID = (CLIENT_TO_GATE_PACKET_ID)BitConverter.ToUInt32(IDData);
            ClientPacketMessage WrappingMessage = new ClientPacketMessage();
            WrappingMessage.Data = Data;
            WrappingMessage.ID = ID;
            WrappingMessage.ResponeSock = Sock;
            ClientPacketMessagesQueue!.Add(WrappingMessage);
        }
        private void RunQueue()
        {
            try
            {
                while (!ClientPacketMessagesQueue!.IsCompleted)
                {
                    ClientPacketMessage WrappingMessage = new ClientPacketMessage();
                    WrappingMessage = ClientPacketMessagesQueue.Take(QueueCancelToken!.Token);
                    switch (WrappingMessage.ID)
                    {
                        case CLIENT_TO_GATE_PACKET_ID.ID_NEW_USER_TRY_CONNECT:
                            FUNC_ClientConnectTry(WrappingMessage.Data!);
                            break;
                        default:
                            MainForm!.AddLogWithTime($"알수 없는 ID 값입니다. RunQueue ID : {WrappingMessage.ID}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm!.AddLogWithTime(ex.Message);
                MainForm!.AddLogWithTime("Message Queue를 종료합니다.");
            }
        }

        private void FUNC_ClientConnectTry(byte[] Data)
        {
            ClientConnectTry Packet = SocketDataSerializer.DeSerialize<ClientConnectTry>(Data!);
            MainForm!.AddLogWithTime($"{Packet.UserName} 님이 접속하셨습니다.");
        }
    }
}
