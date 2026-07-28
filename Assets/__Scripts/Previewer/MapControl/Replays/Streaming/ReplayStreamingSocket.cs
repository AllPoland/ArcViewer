using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ReplayStreamingSocket : IDisposable
{
    public const int MaxMessageSize = 4096;

    public Uri Source;

    public Task StreamTask { get; private set; }
    public bool Streaming { get; private set; }

    public float StreamTime { get; protected set; }

    protected ClientWebSocket socket;


    public abstract void HandleMessage(byte[] message);


#pragma warning disable CS1998 //Suppress warnings about lack of await
    public virtual async Task OnSocketConnect() { }
#pragma warning restore CS1998


    public void ConnectAndStreamData(Uri uri)
    {
        if (Streaming)
        {
            return;
        }

        Source = uri;
        StreamTask = Stream();
    }


    private async Task Stream()
    {
        Streaming = true;

        try
        {
            Debug.Log($"Starting WebSocket");
            socket = new ClientWebSocket();
            await socket.ConnectAsync(Source, default);

            Debug.Log("Socket connected");
            await OnSocketConnect();

            List<byte> scalableBuffer = new List<byte>(MaxMessageSize);
            byte[] buffer = new byte[MaxMessageSize];

            // Streaming variable can be changed outside of this task to stop streaming
            while (Streaming)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, default);
                if (result.CloseStatus != null)
                {
                    Debug.LogWarning($"WebSocket closed unexpectedly with status: {result.CloseStatus}\n    {result.CloseStatusDescription}");

                    await socket.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.Empty, result.CloseStatusDescription, default);
                    socket = null;
                    Dispose();

                    return;
                }

                if (result.Count > MaxMessageSize)
                {
                    Debug.LogWarning($"WebSocket received too large of a message! ({result.Count} > {MaxMessageSize})");

                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, $"Message size of {result.Count} exceeds maximum {MaxMessageSize}", default);
                    socket = null;
                    Dispose();

                    return;
                }

                // Message received successfully
                scalableBuffer.AddRange(buffer.Take(result.Count));

                if (!result.EndOfMessage)
                {
                    // The chunk needs to come in multiple messages, so wait for them before parsing
                    continue;
                }

                // Complete message received, parse and process it
                byte[] message = scalableBuffer.ToArray();
                scalableBuffer.Clear();

                HandleMessage(message);

                await Task.Yield();
            }
        }
        catch (Exception err)
        {
            Debug.LogError($"Uncaught WebSocket error: {err.Message}, {err.StackTrace}");

            await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Client error", default);
            socket = null;
            Dispose();
        }

        socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", default);
    }


    public void Dispose()
    {
        Streaming = false;

        socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", default);
        socket = null;

        ReplaySourceHandler.HandleStreamClosed(this);
    }


    public void DisposeTask()
    {
        StreamTask?.Dispose();
        StreamTask = null;
    }
}