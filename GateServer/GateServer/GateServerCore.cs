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
        BlockingCollection<ClientPacketMessage> ClientPacketMessagesQueue;
        List<Task> ClientTasks;
        const int HeaderSize = sizeof(int);
        public GateServerForm MainForm { get; }
        Socket ListenSocket;
        CancellationTokenSource SocketCancelToken;
        CancellationTokenSource QueueCancelToken;
        bool IsLoginServerConnected = false;
        bool IsUserWantCancel = false;
        Socket? LoginSock;
        ConcurrentDictionary<string, Socket> ConnectedUsers;
        Task? ProcessLoginTask;
        Task? MessageQueueTask;
        public GateServerCore(GateServerForm Owner)
        {
            MainForm = Owner;
            ConnectedUsers = new ConcurrentDictionary<string, Socket>();
            ClientTasks = new List<Task>();
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketCancelToken = new CancellationTokenSource();
            ClientPacketMessagesQueue = new BlockingCollection<ClientPacketMessage>();
            QueueCancelToken = new CancellationTokenSource();
        }

        public bool InitServer()
        {
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
                    MainForm.AddLogWithTime(line);
                }
                MainForm.AddLogWithTime(ex.Message);
                return false;
            }
            return true;
        }
        public async Task Run()
        {
            try
            {
                while (!SocketCancelToken.IsCancellationRequested && !IsUserWantCancel)
                {
                    if (!IsLoginServerConnected)
                    {
                        LogAndSetConnectionStatus("로그인 서버의 연결을 대기중입니다.", MainForm.SetLoginServerConnecting);
                        LoginSock = await ListenSocket.AcceptAsync(SocketCancelToken.Token);
                        if (!CheckConnectionLoginServer(LoginSock))
                            continue;
                        IsLoginServerConnected = true;
                        LogAndSetConnectionStatus("로그인 서버와 연결성공!", MainForm.SetLoginServerConnected);
                        ProcessLoginTask = ProcessLoginDataAsync(LoginSock);
                        MessageQueueTask = RunQueueAsync();
                    }
                    else
                    {
                        Socket ClientSock = await ListenSocket.AcceptAsync(SocketCancelToken.Token);
                        ClientTasks.Add(ClientRunAsync(ClientSock));
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                LogExceptionAndSetCancel(ex);
            }
            catch (SocketException ex)
            {
                LogException(ex);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private void LogAndSetConnectionStatus(string message, Action SetStatus)
        {
            MainForm.AddLogWithTime(message);
            SetStatus();
        }

        private async Task ProcessLoginDataAsync(Socket LoginSock)
        {
            await Task.Run(() => RecvDataFromLoginServer(LoginSock), SocketCancelToken.Token);
        }

        private async Task RunQueueAsync()
        {
            await Task.Run(RunQueue, QueueCancelToken.Token);
        }

        private async Task ClientRunAsync(Socket ClientSock)
        {
            await ClientRun(ClientSock);
        }

        private void LogException(Exception ex)
        {
            MainForm.AddLogWithTime(ex.Message);
        }

        private void LogExceptionAndSetCancel(OperationCanceledException ex)
        {
            MainForm.AddLogWithTime(ex.Message);
            IsUserWantCancel = true;
        }

        private async Task CleanupAsync()
        {
            ListenSocket.Close();
            MainForm.AddLogWithTime("게이트 서버의 통신을 종료합니다.");
            MainForm.SetAllServerStopConnect();
            if (ClientTasks != null && ClientTasks.Count != 0)
            {
                await Task.WhenAll(ClientTasks);
                MainForm.AddLogWithTime($"{ClientTasks.Count} Client tasks 종료완료");
            }
            if (LoginSock != null && LoginSock!.Connected)
            {
                LoginSock.Close();
            }
            if (MessageQueueTask != null && ProcessLoginTask != null)
            {
                await Task.WhenAll(MessageQueueTask, ProcessLoginTask);
            }
            MainForm.AddLogWithTime("게이트 서버 종료 완료");
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
                byte[] HeadByte = new byte[HeaderSize];
                byte[] ID = new byte[sizeof(LOGIN_TO_GATE_PACKET_ID)];

                while (!SocketCancelToken.IsCancellationRequested && LoginSock.Connected)
                {
                    if (!TryReceiveData(LoginSock, HeadByte, HeaderSize) ||
                        !TryReceiveData(LoginSock, ID, sizeof(LOGIN_TO_GATE_PACKET_ID)))
                    {
                        DisconnectWithLog("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }

                    int PacketSize = BitConverter.ToInt32(HeadByte, 0);
                    LOGIN_TO_GATE_PACKET_ID IDNumber = (LOGIN_TO_GATE_PACKET_ID)BitConverter.ToUInt32(ID, 0);
                    byte[] Data = new byte[PacketSize - sizeof(LOGIN_TO_GATE_PACKET_ID)];

                    if (!TryReceiveData(LoginSock, Data, PacketSize - sizeof(LOGIN_TO_GATE_PACKET_ID)))
                    {
                        DisconnectWithLog("LoginServer와 연결이 종료되었습니다.");
                        break;
                    }

                    ProcessLoginData(IDNumber, ref Data, LoginSock);
                }

                IsLoginServerConnected = false;
                SocketCancelToken.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException ex)
            {
                DisconnectWithLog(ex.Message);
                IsUserWantCancel = true;
            }
            catch (SocketException ex)
            {
                DisconnectWithLog(ex.Message);
                MainForm.AddLogWithTime("로그인 서버와의 연결이 끊겼습니다.");
                MainForm.SetLoginServerStopConnected();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            finally
            {
                MainForm.AddLogWithTime("로그인 서버와 연결 종료 완료");
            }
        }

        private bool TryReceiveData(Socket socket, byte[] buffer, int size)
        {
            int receivedData = socket.Receive(buffer, size, SocketFlags.None);
            return receivedData > 0;
        }

        private void DisconnectWithLog(string message)
        {
            MainForm.AddLogWithTime(message);
            IsLoginServerConnected = false;
        }
        private void ProcessLoginData(LOGIN_TO_GATE_PACKET_ID ID ,ref byte[] Data, Socket ClientSock)
        {
            switch(ID)
            {
                case LOGIN_TO_GATE_PACKET_ID.ID_NEW_USER_TRY_CONNECT:
                    LoginToGateServer PacketData; 
                    PacketData = SocketDataSerializer.DeSerialize<LoginToGateServer>(Data);
                    MainForm.AddLogWithTime($"{PacketData.UserName}님이 접속하셨습니다.");
                    MainForm.IncDecUserCount(true);
                    ConnectedUsers.TryAdd(PacketData.UserName, ClientSock);
                    break;
                default:
                    MainForm.AddLogWithTime("알수 없는 ID입니다. ProcessLoginData");
                    break;
            }
        }
        public void Cancel()
        {
            SocketCancelToken.Cancel();
            QueueCancelToken.Cancel();
            ClientPacketMessagesQueue.CompleteAdding();
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
            ClientPacketMessagesQueue.Add(WrappingMessage);
        }
        private void RunQueue()
        {
            try
            {
                while (!ClientPacketMessagesQueue.IsCompleted)
                {
                    ClientPacketMessage WrappingMessage = ClientPacketMessagesQueue.Take(QueueCancelToken.Token);
                    switch (WrappingMessage.ID)
                    {
                        case CLIENT_TO_GATE_PACKET_ID.ID_NEW_USER_TRY_CONNECT:
                            FUNC_ClientConnectTry(WrappingMessage.Data!);
                            break;
                        default:
                            MainForm.AddLogWithTime($"알수 없는 ID 값입니다. RunQueue ID : {WrappingMessage.ID}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                MainForm.AddLogWithTime("Message Queue를 종료합니다.");
            }
            finally
            {
                MainForm.AddLogWithTime("메세지 큐 종료 완료");
            }
        }

        private void FUNC_ClientConnectTry(byte[] Data)
        {
            ClientConnectTry Packet = SocketDataSerializer.DeSerialize<ClientConnectTry>(Data!);
            MainForm.AddLogWithTime($"{Packet.UserName} 님이 접속하셨습니다.");
        }
    }
}
