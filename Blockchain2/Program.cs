using System;
using System.Collections.Generic;
using System.Linq;
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

            var node = new Node(){Name = name};
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

            int selection = -1;
            while (selection != 0)
            {
                Console.WriteLine("Make your selection:");

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
                        // PhillyCoin.ProcessPendingTransactions(name);
                        // node.Broadcast(JsonConvert.SerializeObject(PhillyCoin));
                        break;
                    case 3:
                        Console.WriteLine("Blockchain");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
                        break;
                    
                    case 4:
                        Console.WriteLine("Pending Transactions");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain.PendingTransactions, Formatting.Indented));
                        break;
                    case 5:
                        Console.WriteLine("Mine Block");
                        node.MyBlockchain.ProcessPendingTransactions(name);
                        break;
                    case 6:
                        Console.WriteLine("Your balance is:");
                        node.MyBlockchain.GetBalance(name);
                        break;
                }

                Console.WriteLine("Please select an action");
                string action = Console.ReadLine();
                selection = int.Parse(action ?? string.Empty);
            }

            node.Close();
        }

        private static void ConnectToAll(Node node, int port)
        {
            var connectionList = new List<string>();
            for (int i = 1; i <= 3; i++)
            {
                if (i.ToString() == port.ToString().Last().ToString())
                    continue;
                connectionList.Add($"ws://127.0.0.1:600{i}");
            }

            connectionList.ForEach(x => node.Connect(x));
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