using System;
using System.Collections.Generic;
using System.Linq;
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

    [Serializable]
    public class LoginToGateConnect
    {
        public string UserName = string.Empty;
    }
}
