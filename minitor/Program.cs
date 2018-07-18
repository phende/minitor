using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor
{
    //--------------------------------------------------------------------------
    class Program
    {
        //----------------------------------------------------------------------
        static void Heading()
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("- Minitor = Simplest mini monitor -");
            Console.WriteLine("-----------------------------------");
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

            server = new Web.Server();
            cts = new CancellationTokenSource();
            task = server.Run(Configuration.Binding, cts.Token);

            handler = (object o, ConsoleCancelEventArgs e) =>
            {
                Console.WriteLine("Server stopping...");
                e.Cancel = true;
                cts.Cancel();
            };

            Console.CancelKeyPress += handler;
            Console.WriteLine("Server started, press Ctrl+C to stop.");
            task.Wait();
            Console.CancelKeyPress -= handler;

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
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine();
                    Console.WriteLine("Done, press Enter to quit.");
                    Console.ReadLine();
                }
                return;
            }
            Usage();
        }
    }
}
