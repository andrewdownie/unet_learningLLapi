using System;
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

	private int m_ConnectionId = 0;
	private int m_WebSocketHostId = 0;
	private int m_GenericHostId = 0;

	private string m_SendString = "";
	private string m_RecString  = "";
	private ConnectionConfig m_Config = null;
	private byte m_CommunicationChannel = 0;

    char msg;//a copy of the last message we got
    int recievedCount = 0;
    string textString = "";

	void Start()
	{
		m_Config = new ConnectionConfig();                                         //create configuration containing one reliable channel
		m_CommunicationChannel = m_Config.AddChannel(QosType.Reliable);
	}

	void OnGUI () {
		GUI.Box(new Rect(5, 5, 450, 450), "window");		
		if( !_isStarted )
		{
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
			}
#endif
			if (GUI.Button(new Rect(10, 100, 250, 30), "start client"))
			{
				_isStarted = true;
				_isServer = false;
				NetworkTransport.Init();

				HostTopology topology = new HostTopology(m_Config, 12);
				m_GenericHostId = NetworkTransport.AddHost(topology, 0); //any port for udp client, for websocket second parameter is ignored, as webgl based game can be client only
				byte error;
				m_ConnectionId = NetworkTransport.Connect(m_GenericHostId, ip, port, 0, out error);
			}
		}
		else//Is started
		{
			GUI.Label(new Rect(10, 20, 250, 500), "Sent: " + m_SendString);
			GUI.Label(new Rect(10, 70, 250, 50), "Recv: " + recievedCount);

            GUI.Label(new Rect(300, 300, 100, 25), "Message is: " + msg.ToString());

			if (GUI.Button(new Rect(10, 120, 250, 50), "stop"))
			{
				_isStarted = false;
				NetworkTransport.Shutdown();
			}
            textString = GUI.TextField(new Rect(400, 100, 150, 25), textString);
            if (GUI.Button(new Rect(10, 175, 250, 50), "send"))
            {
                

                Byte error;
                Byte[] data = new Byte[5];
                data[0] = Convert.ToByte(textString.ToString()[0]);

                NetworkTransport.Send(hostId, connId, m_CommunicationChannel,  data, 1, out error);
            }
        }
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
					if (!_isServer)
					{
                        hostId = recHostId;
                        connId = connectionId;

						string message = "message " + _messageIdx.ToString();
						_messageIdx++;
						byte[] bytes = new byte[message.Length * sizeof(char)];
						System.Buffer.BlockCopy(message.ToCharArray(), 0, bytes, 0, bytes.Length);
						Debug.Log(String.Format("connect event and Sent message host {0} connection {1} message length {2}", recHostId, connectionId, bytes.Length));

						NetworkTransport.Send(recHostId, connectionId, m_CommunicationChannel, bytes, bytes.Length, out error);		//when client received connection signal it starts send echo				
					}
                    else
                    {
                        hostId = recHostId;
                        connId = connectionId;
                    }
					Debug.Log(String.Format("Connect from host {0} connection {1}", recHostId, connectionId));
					break;
				}

			case NetworkEventType.DataEvent:  //if server will receive echo it will send it back to client, when client will receive echo from serve wit will send other message
                {
                    recievedCount++;

                    Debug.Log(String.Format("MEOW Received event host {0} connection {1} channel {2} message length {3}", recHostId, connectionId, channelId, dataSize));

                    msg = Convert.ToChar(recBuffer[0]);
                    Debug.Log("First char of message: " + msg);

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
                        break;
                    }
            }
		}
	}
}
