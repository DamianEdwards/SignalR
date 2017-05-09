using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace ImageCastR.EndPoints
{
    public class ImageEndPoint : EndPoint
    {
        public ConnectionList Connections { get; } = new ConnectionList();

        public async override Task OnConnectedAsync(Connection connection)
        {
            Connections.Add(connection);

            try
            {
                while (await connection.Transport.Input.WaitToReadAsync())
                {
                    Message message;
                    if (connection.Transport.Input.TryRead(out message))
                    {
                        await Broadcast(message.Payload, message.Type, message.EndOfMessage);
                    }
                }
            }
            finally
            {
                Connections.Remove(connection);
            }
        }

        private Task Broadcast(string text)
        {
            return Broadcast(Encoding.UTF8.GetBytes(text), MessageType.Text, endOfMessage: true);
        }

        private Task Broadcast(byte[] payload, MessageType format, bool endOfMessage)
        {
            var tasks = new List<Task>(Connections.Count);

            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Output.WriteAsync(new Message(
                    payload,
                    format,
                    endOfMessage)));
            }
            return Task.WhenAll(tasks);
        }
    }
}
