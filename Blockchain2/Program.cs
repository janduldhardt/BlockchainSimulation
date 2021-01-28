namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    class Program {
        private static async void ConnectToAll(Node node, int port) {
            for (var i = 3; i > 0; i--) {
                if (i.ToString() == port.ToString().Last().ToString()) {
                    continue;
                }

                var connectUrl = $"ws://127.0.0.1:600{i}";
                node.Connect(connectUrl);
                Console.WriteLine($"Connected with new node at {connectUrl}");
                await Task.Delay(1000);
            }
        }

        static int FreeTcpPort() {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        static void Main(string[] args) {
            int port = FreeTcpPort();
            string name = "Unknown";

            if (args.Length >= 1) {
                port = int.Parse(args[0]);
            }

            if (args.Length >= 2) {
                name = args[1];
            }

            if (name != "Unkown") {
                Console.WriteLine($"Current user is {name}");
            }

            var node = new Node() { Name = name };
            node.Start(port);

            Console.WriteLine("=========================");
            Console.WriteLine("0. Exit");
            Console.WriteLine("Online:");
            Console.WriteLine("1. Connect");
            Console.WriteLine("2. Display Blockchain");
            Console.WriteLine("3. Show Pending Transactions");
            Console.WriteLine("4. Add a transaction");
            Console.WriteLine("5. My pending transactions");
            Console.WriteLine("6. Mine Block");
            Console.WriteLine("7. Find transactions");
            Console.WriteLine("8. Find transaction by id");
            Console.WriteLine("9. Get Balances");
            Console.WriteLine("10. Get Trustlevel");
            Console.WriteLine("=========================");

            var selection = -1;
            do {
                Console.WriteLine("Please select an action");
                string action = Console.ReadLine();
                selection = int.Parse(action ?? string.Empty);

                switch (selection) {
                    case 1:
                        ConnectToAll(node, port);
                        Task.Delay(2000);
                        node.BroadcastBlockchain();
                        break;
                    case 2:
                        Console.WriteLine("Blockchain");
                        Console.WriteLine(JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
                        break;
                    case 3:
                        Console.WriteLine("Pending Transactions");
                        Console.WriteLine(
                            JsonConvert.SerializeObject(
                                node.MyBlockchain.PendingTransactions,
                                Formatting.Indented));
                        break;
                    case 4:
                        var transaction = HandleNewTransaction(node);
                        node.BroadcastTransaction(transaction);
                        break;
                    case 5:
                        Console.WriteLine($"Verify my incoming transactions");
                        HandleMyIncomingTransactions(node);
                        break;
                    case 6:
                        Console.WriteLine("Mine Block");
                        HandleMining(node);
                        break;
                    case 7:
                        HandleFindTransactions(node);
                        break;
                    case 8:
                        HandleFindTransactionById(node);
                        break;
                    case 9:
                        Console.WriteLine($"Balances:");
                        HandleShowBalances(node);
                        break;
                    case 10:
                        Console.WriteLine($"Balances:");
                        HandleGetTrustLevel(node);
                        break;

                    case 11:
                        Console.WriteLine($"Broadcast Blockchain");
                        node.BroadcastBlockchain();
                        break;
                }

                File.WriteAllText(node.BlockchainFilePath, JsonConvert.SerializeObject(node.MyBlockchain, Formatting.Indented));
            }
            while (selection != 0);

            node.Close();
        }

        private static bool _isMining;

        private static void HandleMining(Node node) {
            if (node.IsMining) {
                Console.WriteLine("You are currently mining the new block!");
                return;
            }

            node.MineBlockOnBackgroundThread();
        }

        private static void HandleGetTrustLevel(Node node) {
            Console.WriteLine("Please enter name:");
            var address = Console.ReadLine() ?? node.Name;
            var rating = BlockchainUtils.GetRating(address, node.MyBlockchain);
            Console.WriteLine($"{address} rating: {rating.ToString("N")}%");
        }

        private static void HandleFindTransactionById(Node node) {
            var id = Console.ReadLine();
            var transaction = BlockchainUtils.FindTransactionById(node.MyBlockchain, id);
            Console.WriteLine(JsonConvert.SerializeObject(transaction, Formatting.Indented));
        }

        private static void HandleShowBalances(Node node) {
            var balanceDict = BlockchainUtils.GetBalanceDict(node.Name, node.MyBlockchain);
            var total = balanceDict.Sum(x => x.Value);
            Console.WriteLine("Owed money:");
            foreach (var (name, amount) in balanceDict.Where(x => x.Value > 0)) {
                Console.WriteLine($"Name: {node.Name.PadRight(10, ' ')} | Amount: {amount.ToString().PadLeft(6, ' ')}");
            }

            Console.WriteLine("");
            Console.WriteLine("Gets money from");
            foreach (var (name, amount) in balanceDict.Where(x => x.Value < 0)) {
                var newamount = amount * -1;
                Console.WriteLine($"Name: {node.Name.PadRight(10, ' ')} | Amount: {newamount.ToString().PadLeft(6, ' ')}");
            }
            Console.WriteLine($"Sum: {total}");
        }


        private static Transaction HandleNewTransaction(Node node) {
            Console.WriteLine("Please enter the receiver name");
            string receiverName = Console.ReadLine();
            Console.WriteLine("Please enter the amount");
            string amount = Console.ReadLine();
            var transaction = new Transaction(node.Name, receiverName, int.Parse(amount));
            node.MyBlockchain.AddTransaction(transaction);
            return transaction;
        }

        private static void HandleFindTransactions(Node node) {
            Console.WriteLine($"Find transactions");
            Console.WriteLine($"Enter sender name or press enter");
            var sender = Console.ReadLine();
            Console.WriteLine($"Enter receiver name or press enter");
            var receiver = Console.ReadLine();
            Console.WriteLine($"Enter amount or press enter");
            var transactionAmount = Console.ReadLine();
            var transactions = BlockchainUtils.FindTransactions(node.MyBlockchain, sender, receiver, transactionAmount);
            Console.WriteLine(JsonConvert.SerializeObject(transactions, Formatting.Indented));
        }

        private static void HandleMyIncomingTransactions(Node node) {
            var transactions = BlockchainUtils.GetTransactionsToVerify(node.MyBlockchain, node.Name);
            foreach (var transaction in transactions) {
                Console.WriteLine(transaction.CombinedString());
                Console.WriteLine("Accept? (y/n) or skip");
                var isAccept = Console.ReadLine()?.Trim().ToLower();
                if (isAccept == "y") {
                    transaction.Status = TransactionStatusEnum.Accepted;
                    node.BroadcastTransaction(transaction);
                }

                if (isAccept == "n") {
                    transaction.Status = TransactionStatusEnum.Declined;
                    node.BroadcastTransaction(transaction);
                }
            }
        }
    }
}