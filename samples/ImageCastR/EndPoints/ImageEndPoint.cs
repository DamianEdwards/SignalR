using System.Collections.Generic;
using System.IO;
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
                            using (var image = Image.Load(data.ToArray()))
                            {
                                foreach (var c in Connections)
                                {
                                    if (string.Equals(c.Metadata.Get<string>("format"), "ascii"))
                                    {
                                        var sb = new StringBuilder();
                                        var grayScale = image.Resize(100, 30).Grayscale();
                                        for (int i = 0; i < grayScale.Pixels.Length; i++)
                                        {
                                            var pixel = grayScale.Pixels[i];
                                            var ch = ToAscii(pixel.R);
                                            sb.Append(ch);
                                            if (i > 0 && i % grayScale.Width == 0)
                                            {
                                                sb.AppendLine();
                                            }
                                        }
                                        var ascii = Encoding.ASCII.GetBytes(sb.ToString());

                                        await c.Transport.Output.WriteAsync(new Message(ascii, MessageType.Text));
                                    }
                                    else
                                    {
                                        await c.Transport.Output.WriteAsync(new Message(data.ToArray(), MessageType.Binary));
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

        private const char Black = '#';
        private const char Medium = '&';
        private const char MediumLight = '*';
        private const char Light = '.';
        private const char White = ' ';

        private static char ToAscii(int pixelValue)
        {
            char asciiSymbol;

            if (pixelValue >= 200)
            {
                asciiSymbol = White;
            }
            else if (pixelValue >= 150)
            {
                asciiSymbol = Light;
            }
            else if (pixelValue >= 100)
            {
                asciiSymbol = MediumLight;
            }
            else if (pixelValue >= 50)
            {
                asciiSymbol = Medium;
            }
            else
            {
                asciiSymbol = Black;
            }
            return asciiSymbol;
        }
    }
}
