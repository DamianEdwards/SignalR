using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharp;
using Microsoft.AspNetCore.Http;
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
                            using (var image = Image.Load(data.ToArray()))
                            {
                                var output = new MemoryStream();
                                var result = image.Resize(image.Width / 5, image.Height / 5)
                                     .Grayscale()
                                     .Save(output);

                                foreach (var c in Connections)
                                {
                                    if (string.Equals(c.Metadata.Get<string>("format"), "ascii"))
                                    {
                                        var sb = new StringBuilder();
                                        for (int i = 0; i < result.Pixels.Length; i++)
                                        {
                                            var pixel = result.Pixels[i];
                                            var ch = GetChar(pixel);
                                            sb.Append(ch);
                                            if (i % result.Width == 0)
                                            {
                                                sb.AppendLine();
                                            }
                                        }
                                        var ascii = Encoding.ASCII.GetBytes(sb.ToString());

                                        await c.Transport.Output.WriteAsync(new Message(ascii, MessageType.Text));
                                    }
                                    else
                                    {
                                        await c.Transport.Output.WriteAsync(new Message(output.ToArray(), MessageType.Binary));
                                    }
                                }
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

        private char GetChar(Rgba32 pixelColor)
        {
            char[] asciiChars = { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };

            int red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
            int green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
            int blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
            var gray = new Rgba32(red, green, blue);
            int index = (gray.R * 10) / 255;

            return asciiChars[index];
        }
    }
}
