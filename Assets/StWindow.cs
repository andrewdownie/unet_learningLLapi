using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/*
 * Simple echo server and client
 * For editor or standalone player it supports eiter client and server, for webgl client only supported
 */
public class StWindow : MonoBehaviour {

	private bool _isStarted = false;
	private bool _isServer = false;
	string ip  = "127.0.0.1";
	int   port = 7075;
	private int _messageIdx = 0;
    string myName = "";

	private int m_ConnectionId = 0;
	private int m_WebSocketHostId = 0;
	private int m_GenericHostId = 0;

	private string m_SendString = "";
	private string m_RecString  = "";
	private ConnectionConfig m_Config = null;
	private byte m_CommunicationChannel = 0;
    
    int recievedCount = 0;
    string textString = "";

    //(Server Only) keep track of all the clients who have connected
    Dictionary<int, string> connectedUsers;//connectionID : playerName


    //_Chat area stuff
    Vector2 scrollPosition;
    List<string> chatMessages = new List<string>();

	void Start()
	{
		m_Config = new ConnectionConfig();                                         //create configuration containing one reliable channel
		m_CommunicationChannel = m_Config.AddChannel(QosType.Reliable);

        myName = "Alan" + UnityEngine.Random.Range(100, 999);
	}

	void OnGUI () {
		//GUI.Box(new Rect(5, 5, 450, 450), "window");
		if( !_isStarted )
		{
            if (myName == "")
            {
                GUI.Label(new Rect(274, 12, 100, 25), "YOUR NAME");
            }
            myName = GUI.TextField(new Rect(270, 10, 150, 25), myName);

            

			ip = GUI.TextField(new Rect(10, 10, 250, 30), ip, 25);
			port = Convert.ToInt32( GUI.TextField(new Rect(10, 40, 250, 30), port.ToString(), 25) );
#if !(UNITY_WEBGL && !UNITY_EDITOR)
			if ( GUI.Button( new Rect(10, 70, 250, 30), "start server" ) )
			{
				_isStarted = true;
				_isServer = true;
				NetworkTransport.Init();

				HostTopology topology = new HostTopology(m_Config, 12);
				m_WebSocketHostId = NetworkTransport.AddWebsocketHost(topology, port, null);           //add 2 host one for udp another for websocket, as websocket works via tcp we can do this
				m_GenericHostId = NetworkTransport.AddHost(topology, port, null);

                connectedUsers = new Dictionary<int, string>();
                //connectedUsers.Add(m_ConnectionId, "host");//this should have the host send messages to themseleves but it doesnt work...
			}
#endif
			if (GUI.Button(new Rect(10, 100, 250, 30), "start client"))
			{
				_isStarted = true;
				_isServer = false;
				NetworkTransport.Init();

				HostTopology topology = new HostTopology(m_Config, 12);

				//I don't get what needs to be done for web clients to connect?
				m_GenericHostId = NetworkTransport.AddHost(topology, 0); //any port for udp client, for websocket second parameter is ignored, as webgl based game can be client only
				byte error;
				m_ConnectionId = NetworkTransport.Connect(m_GenericHostId, ip, port, 0, out error);
			}
		}
		else//Is started
		{
			GUI.Label(new Rect(10, 20, 250, 500), "Sent: " + m_SendString);
			GUI.Label(new Rect(10, 70, 250, 50), "Recv: " + recievedCount);
            

			if (GUI.Button(new Rect(10, 120, 250, 50), "stop"))
			{
				_isStarted = false;
				NetworkTransport.Shutdown();
			}
            GUI.Label(new Rect(400, 75, 150, 25), "Message...");
            textString = GUI.TextField(new Rect(400, 100, 150, 25), textString);
            if (GUI.Button(new Rect(10, 175, 250, 50), "send"))
            {


               // Byte[] data = new Byte[5];
               // data[0] = Convert.ToByte(textString.ToString()[0]);

                if (_isServer)//send to all players
                {
                    chatMessages.Add(myName + ": " + textString);
                    foreach (var connection in connectedUsers)
                    {
                        //  NetworkTransport.Send(hostId, connection.Key, m_CommunicationChannel, NetworkConverter.StrToNet(textString), textString.Length, out error);

                        NetworkMessage.Send(hostId, connection.Key, m_CommunicationChannel, MessageType.message, myName + ": " + textString);
                    }
                }
                else//send to server, and have server send to all players (later)
                {

                    //NetworkTransport.Send(hostId, connId, m_CommunicationChannel, NetworkConverter.StrToNet(textString), textString.Length, out error);

                    NetworkMessage.Send(hostId, connId, m_CommunicationChannel, MessageType.message, textString);
                }


               
            }
            GUI.Label(new Rect(Screen.width - 500, 120, 450, 400), "Messages:");
            scrollPosition = GUI.BeginScrollView(new Rect(Screen.width - 500, 150, 450, 400), scrollPosition, new Rect(0, 0, 425, 30 * chatMessages.Count + 60));

            for (int i = 0; i < chatMessages.Count; i++)
            {
                GUI.Label(new Rect(0, i * 30, 400, 25), chatMessages[i]);
            }

            GUI.EndScrollView();
        }//Is started
	}

    int hostId;
    int connId;

    void Update()
	{
		if (!_isStarted)
			return;
		int recHostId;
		int connectionId;
		int channelId;
		byte[] recBuffer = new byte[1024];
		int bufferSize = 1024;
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
		switch (recData)
		{

			case NetworkEventType.Nothing:
				break;

			case NetworkEventType.ConnectEvent:
				{
                    bool IamConnectingToTheServer = !_isServer;
                    bool IamTheServerBeingConnectedTo = _isServer;

					if (IamConnectingToTheServer)
					{
                        //save the hosts details
                        hostId = recHostId;
                        connId = connectionId;

                        //Send my name to the server

                        NetworkMessage.Send(hostId, connId, m_CommunicationChannel, MessageType.setName, myName);

                    }

                    if(IamTheServerBeingConnectedTo)
                    {//Add person to list here
                        connId = connectionId;
                        connectedUsers.Add(connId, "unset");
                    }


					Debug.Log(String.Format("Connect from host {0} connection {1}", recHostId, connectionId));
					break;
				}

			case NetworkEventType.DataEvent:  //if server will receive echo it will send it back to client, when client will receive echo from serve wit will send other message
                {
                    recievedCount++;



                    Debug.Log(String.Format("MEOW Received event host {0} connection {1} channel {2} message length {3}", recHostId, connectionId, channelId, dataSize));
                    //msg = NetworkConverter.NetToStr(recBuffer, dataSize);
                    // DecodeMessage(msg);

                   DecodeMessage(NetworkConverter.NetToStr(recBuffer, dataSize), connectionId);

                    //this should only happen for messages?
                    if (_isServer)
                    {
                        string recStr = NetworkConverter.NetToStr(recBuffer, dataSize);
                        string[] pieceList = recStr.Split(new string[] { "--" }, StringSplitOptions.None);

                        string type = pieceList[0];
                        string payload = pieceList[1];

                        payload = connectedUsers[connectionId] + ": " + payload;

                        string sendStr = type + "--" + payload;

                        recBuffer = NetworkConverter.StrToNet(sendStr);

                        foreach (var connection in connectedUsers) {
                            NetworkTransport.Send(hostId, connection.Key, m_CommunicationChannel, recBuffer, recBuffer.Length, out error);
                        }

                    }

                }
                break;
			case NetworkEventType.DisconnectEvent:
		    {
                    if (!_isServer)
                    {
                        Debug.Log(String.Format("DisConnect from host {0} connection {1}", recHostId, connectionId));
                        break;
                    }
                    else {
                        //remove person from list here
                        connectedUsers.Remove(connectionId);
                        break;
                    }
            }
		}
	}


    public void DecodeMessage(string networkMessage, int playerID)
    {
        string[] components = networkMessage.Split(new string[] { "--" }, StringSplitOptions.None);

        string typeStr = components[0];
        string msgContents = components[1];


        switch (typeStr)
        {
            case "setName":
                if (!_isServer) { return; }
                Debug.Log("The name to set is: " + msgContents);
                connectedUsers[playerID] = msgContents;
                break;
            case "message":
                Debug.Log("The message to send is: " + msgContents);
                if (!_isServer)
                {
                    chatMessages.Add(msgContents);
                }
                else
                {
                    chatMessages.Add(connectedUsers[playerID] + ": " + msgContents);
                }
                
                break;
            case "none":
                Debug.Log("Wtf is going on?");
                break;
        }
        
    }
}
