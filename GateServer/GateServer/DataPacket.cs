using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GateServer
{
    public static class SocketDataSerializer
    {
        public static T DeSerialize<T>(byte[] data)
        {
            var ByteData = new Utf8JsonReader(data);
            return JsonSerializer.Deserialize<T>(ref ByteData)!;
        }

        public static byte[] Serialize<T>(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }
    public enum LOGIN_TO_GATE_PACKET_ID : uint
    {
        ID_NEW_USER_TRY_CONNECT = 0
    }

    public enum CLIENT_TO_GATE_PACKET_ID : ushort
    {
        ID_NEW_USER_TRY_CONNECT = 0
    }
    public class ClientPacketMessage
    {
        public byte[]? Data;
        public CLIENT_TO_GATE_PACKET_ID ID;
        public Socket? ResponeSock;
    }

    [Serializable]
    public class LoginToGateServer
    {
        public string UserName { get; set; } = string.Empty;
    }
    [Serializable]
    public class ClientConnectTry
    {
        public string UserName { get; set; } = string.Empty;
    }
}
