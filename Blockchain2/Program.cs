using System;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Blockchain2
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = FreeTcpPort();
            string name = "Unknown";

            if (args.Length >= 1)
                port = int.Parse(args[0]);
            if (args.Length >= 2)
                name = args[1];

            if (name != "Unkown")
            {
                Console.WriteLine($"Current user is {name}");
            }

            var node = new Node.Node(port);

            Console.WriteLine("=========================");
            Console.WriteLine("1. Connect to a server");
            Console.WriteLine("2. Add a transaction");
            Console.WriteLine("3. Display Blockchain");
            Console.WriteLine("4. Exit");
            Console.WriteLine("=========================");

            int selection = 0;
            while (selection != 4)
            {
                switch (selection)
                {
                    case 1:
                        Console.WriteLine("Please enter the server URL");
                        string serverURL = Console.ReadLine();
                        node.Connect($"{serverURL}");
                        break;
                    case 2:
                        // Console.WriteLine("Please enter the receiver name");
                        // string receiverName = Console.ReadLine();
                        // Console.WriteLine("Please enter the amount");
                        // string amount = Console.ReadLine();
                        // PhillyCoin.CreateTransaction(new Transaction(name, receiverName, int.Parse(amount)));
                        // PhillyCoin.ProcessPendingTransactions(name);
                        // Client.Broadcast(JsonConvert.SerializeObject(PhillyCoin));
                        break;
                    case 3:
                        Console.WriteLine("Blockchain");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
                        break;
                }

                Console.WriteLine("Please select an action");
                string action = Console.ReadLine();
                selection = int.Parse(action);
            }

            node.Close();
        }


        static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}