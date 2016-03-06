using System;
using UnityEngine;
using UnityEngine.Networking;

public static class NetworkMessage {


    public static bool Send(int hostid, int connectionid, int channelid, MessageType type, string playerName)
    {
        string message = type.ToString() + "--" + playerName;
        byte error;

        return NetworkTransport.Send(hostid, connectionid, channelid, NetworkConverter.StrToNet(message), message.Length, out error);
    }
}



public enum MessageType
{
    setName         = 0,
    message         = 1,
    none            = 2,
}
