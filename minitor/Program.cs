﻿using Minitor.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

//------------------------------------------------------------------------------
// Edit version numbers here
[assembly: AssemblyInformationalVersion("0.1.1")]
[assembly: AssemblyVersion("0.1.1.0")]

//------------------------------------------------------------------------------
[assembly: AssemblyTitle("minitor")]
[assembly: AssemblyProduct("minitor - simple status dashboard server")]
[assembly: AssemblyDescription("https://github.com/phende/minitor")]
[assembly: AssemblyCopyright("MIT license, minitor contributors")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

namespace Minitor
{
    //--------------------------------------------------------------------------
    class Program
    {
        //----------------------------------------------------------------------
        static void Heading()
        {
            Assembly assembly;
            string product, version, banner, config, dashes;

            assembly = typeof(Program).Assembly;

            product = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            version = assembly.GetName().Version.ToString();
            config = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;

            banner = $"| {product} (v{version} {config}) |";
            dashes = new string('-', banner.Length);

            Console.WriteLine(dashes);
            Console.WriteLine(banner);
            Console.WriteLine(dashes);
            Console.WriteLine();
        }

        //----------------------------------------------------------------------
        static void Usage()
        {
            string line;

            using (Stream stream = Helpers.GetResourceStream("usage.txt"))
            using (TextReader reader = new StreamReader(stream))
                while ((line = reader.ReadLine()) != null)
                    Console.WriteLine(line);
        }

        //----------------------------------------------------------------------
        static bool Server(string[] args)
        {
            ConsoleCancelEventHandler handler;
            CancellationTokenSource cts;
            Web.Server server;
            Task task;

            if (args.Length > 1)
                return false;

            using (server = new Web.Server())
            {
                cts = new CancellationTokenSource();
                task = server.Run(Configuration.Binding, cts.Token);

                handler = (object o, ConsoleCancelEventArgs e) =>
                {
                    Console.WriteLine("Server stopping...");
                    e.Cancel = true;
                    cts.Cancel();
                };

                Console.CancelKeyPress += handler;
                Console.WriteLine($"Server started at {Configuration.Binding}, press Ctrl+C to stop.");
                task.Wait();
                Console.CancelKeyPress -= handler;
            }
            return true;
        }

        //----------------------------------------------------------------------
        static void Main(string[] args)
        {
            Heading();
            if (args.Length >= 1)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "server": if (Server(args)) return; break;
                }
            }
            Usage();
        }
    }
}
