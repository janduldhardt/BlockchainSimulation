using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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

            var node = new Node() {Name = name};
            node.Start(port);

            Console.WriteLine("=========================");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. Connect");
            Console.WriteLine("2. Add a transaction");
            Console.WriteLine("3. Display Blockchain");
            Console.WriteLine("4. Show Pending Transactions");
            Console.WriteLine("5. Mine Block");
            Console.WriteLine("6. Get Balance");
            Console.WriteLine("=========================");

            var selection = -1;
            do
            {
                Console.WriteLine("Please select an action");
                string action = Console.ReadLine();
                selection = int.Parse(action ?? string.Empty);

                switch (selection)
                {
                    case 1:
                        ConnectToAll(node, port);
                        break;
                    case 2:
                        Console.WriteLine("Please enter the receiver name");
                        string receiverName = Console.ReadLine();
                        Console.WriteLine("Please enter the amount");
                        string amount = Console.ReadLine();
                        node.MyBlockchain.AddTransactionIfValid(new Transaction(name, receiverName, int.Parse(amount)));
                        break;
                    case 3:
                        Console.WriteLine("Blockchain");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
                        break;

                    case 4:
                        Console.WriteLine("Pending Transactions");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain.PendingTransactions,
                            Formatting.Indented));
                        break;
                    case 5:
                        Console.WriteLine("Mine Block");
                        node.MyBlockchain.ProcessPendingTransactions(name);
                        break;
                    case 6:
                        var balance = node.MyBlockchain.GetBalance(name);
                        Console.WriteLine($"Your balance is: {balance}");
                        break;
                    case 7:
                        Console.WriteLine($"Broadcast Blockchain");
                        node.BroadcastBlockchain();
                        break;

                    case 8:
                        Console.WriteLine($"Check open connection");
                        node.OpenConnection();
                        break;
                }

                File.WriteAllText(node.BlockchainFilePath, JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
            } while (selection != 0);

            node.Close();
        }

        private static async void ConnectToAll(Node node, int port)
        {
            for (var i = 3; i > 0; i--)
            {
                if (i.ToString() == port.ToString().Last().ToString())
                    continue;
                var connectUrl = $"ws://127.0.0.1:600{i}";
                node.Connect(connectUrl);
                await Task.Delay(1000);
            }
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