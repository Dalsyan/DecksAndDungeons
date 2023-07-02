using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Input;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class ActionsManager : MonoBehaviour
{
    private readonly TcpClient Client;
    private readonly ConcurrentQueue<string> ActionsQueue;
    private Thread listener;
    private bool open;
    
    private string agent;

    public ActionsManager(TcpClient client, ConcurrentQueue<string> queue)
    {
        Client = client;
        ActionsQueue = queue;
    }
    public string Agent
    {
        get { return agent; }
        set { agent = value; }
    }

    public void Start()
    {
        listener = new Thread(new ThreadStart(Listen))
        {
            IsBackground = true
        };
        listener.Start();
    }

    public void Stop()
    {
        listener.Abort();
        open = false;
        agent = null;
    }

    public void SendMessageToSpadeSocket(string message)
    {
        try
        {
            var stream = Client.GetStream();
            if (stream.CanWrite)
            {
                var serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception in " + agent + " thread: " + socketException.Message);
        }
        catch (ObjectDisposedException)
        {
            Debug.Log("TcpClient finished for " + agent);
        }
    }

    public bool IsConnected()
    {
        return Client != null && Client.Connected;
    }

    private void Listen()
    {
        open = true;
        try
        {
            while (open)
                RecvMessageFromSocket();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            Client.Close();
            open = false;
        }
    }

    private void RecvMessageFromSocket()
    {
        var bytes = new Byte[1024];
        using NetworkStream stream = Client.GetStream();
        int length;
        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
            var data = new byte[length];
            Array.Copy(bytes, 0, data, 0, length);
            var msg = Encoding.ASCII.GetString(data);
            EnqueueActions(msg);
            Debug.Log(agent + " command message");
        }
    }

    private void EnqueueActions(string msg)
    {
        ActionsQueue.Enqueue(msg);

        //var actions = msg.Split(new string[] { "}{" }, StringSplitOptions.RemoveEmptyEntries);
        //if (actions.Length == 1)
        //    EnqueueAction(msg);
        //else if (actions.Length > 1)
        //    for (int i = 0; i < actions.Length; i++)
        //    {
        //        var action = actions[i];
        //        if (i == 0 || i < actions.Length - 1)
        //            action += "}";
        //        if (i > 0)
        //            action = "{" + action;
        //        EnqueueAction(action);
        //    }
    }

    private void EnqueueAction(string msg)
    {
        //var action = ActionParser.Parse(msg);
        //if (action != null)
        //{
        //    if (Agent == null && action is Create)
        //        Agent = action.Agent;
        //    else
        //        action.Agent = Agent;
        //    ActionsQueue.Enqueue(action);
        //}
    }
}
