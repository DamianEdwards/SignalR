using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharp;
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
                var data = new List<byte>();
                while (await connection.Transport.Input.WaitToReadAsync())
                {
                    while (connection.Transport.Input.TryRead(out var message))
                    {
                        data.AddRange(message.Payload);

                        if (message.EndOfMessage)
                        {
                            using (var image = Image.Load(new MemoryStream(data.ToArray())))
                            {
                                var output = new MemoryStream();
                                image.Resize(image.Width / 2, image.Height / 2)
                                     .Grayscale()
                                     .Save(output);

                                await Broadcast(output.ToArray(), MessageType.Binary, endOfMessage: true);
                            }

                            data.Clear();
                        }
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
