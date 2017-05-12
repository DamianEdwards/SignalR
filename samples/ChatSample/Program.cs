// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Mutli-node support
            int port = 5000;
            int serverId = 0;
            if (!File.Exists("svr"))
            {
                File.WriteAllText("svr", "1");
            }
            else
            {
                serverId = Int32.Parse(File.ReadAllText("svr")) + 1;
                File.WriteAllText("svr", serverId.ToString());
                port = 5000 + serverId;
            }

            var id = new ServerId(serverId.ToString());

            Console.Title = $"SERVER: {id}, at http://localhost:{port}";

            var host = new WebHostBuilder()
                .ConfigureLogging((context, factory) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    factory.AddConsole(LogLevel.Information);
#pragma warning restore CS0618 // Type or member is obsolete
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IServerId>(id);
                    services.AddSingleton<UserCreator>();
                })
                .UseUrls($"http://*:{port}")
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            Console.WriteLine($"SERVER INSTANCE: {id}");

            host.Run();
        }
    }

    public interface IServerId
    {
        string Id { get; }
    }

    public class ServerId : IServerId
    {
        public ServerId(string id)
        {
            Id = id;
        }
        public string Id { get; }

        public override string ToString()
        {
            return Id;
        }
    }

    public class UserCreator
    {
        private static int _id;
        private readonly IServerId _serverId;

        public UserCreator(IServerId serverId)
        {
            _serverId = serverId;
        }

        public ClaimsPrincipal GetNextUser()
        {
            int id = Interlocked.Increment(ref _id);
            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, $"user_id{_serverId.Id}_{id}"));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, $"user_name_{_serverId.Id}_{id}"));
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
