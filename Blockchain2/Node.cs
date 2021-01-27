namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using WebSocketSharp;
    using WebSocketSharp.Server;

    public class Node {

        public EventHandler MessageReceived;
        private readonly string _baseUrl = "ws://127.0.0.1";

        private readonly string _blockchainEndPoint = "/Blockchain";

        private readonly IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();


        private WebSocketServer _webSocketServer;

        private IList<string> knownNodeAddresses = new List<string>();

        public string IpAddress { get; set; }

        public bool IsMining { get; set; }

        public string BlockchainFilePath => Path.Combine(Directory.GetCurrentDirectory(), Name + ".json");

        public Blockchain MyBlockchain { get; set; } = new();

        public string Name { get; set; }

        public void Broadcast(string data) {
            foreach (KeyValuePair<string, WebSocket> item in wsDict) {
                item.Value.Send(data);
            }
        }

        public void BroadcastBlockchain() {
            var message = new Message() { MessageTypeEnum = MessageTypeEnum.BlockchainMessage, SenderAddress = IpAddress, Data = JsonConvert.SerializeObject(MyBlockchain) };
            Broadcast(Message.GetSerializedMessage(message));
        }

        public void BroadcastTransaction(Transaction transaction) {
            var message = new Message() { MessageTypeEnum = MessageTypeEnum.TransactionMessage, SenderAddress = IpAddress, Data = JsonConvert.SerializeObject(transaction) };
            Broadcast(Message.GetSerializedMessage(message));
        }


        public void Close() {
            foreach (KeyValuePair<string, WebSocket> item in wsDict) {
                item.Value.Close();
            }
        }

        public void Connect(string url) {
            try {
                if (!wsDict.ContainsKey(url)) {
                    url = $"{url}{_blockchainEndPoint}";
                    var ws = new WebSocket($"{url}");
                    ws.OnClose += (sender, e) => { wsDict.Remove(url); };
                    ws.OnMessage += OnMessageReceived;
                    ws.Connect();
                    wsDict.Add(url, ws);
                }
            } catch (Exception e) {
                Console.Error.WriteLine(
                    $"Current Method: {MethodBase.GetCurrentMethod()} - Message: {e.Message}");
            }
        }


        private bool IsBlockchainNewer(Blockchain newChain) {
            // Check whether this nodes our the other nodes Blockchain is the current
            if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count) {
                return true;
            }

            return false;
        }

        public void Start(int port) {
            // Init blockchain or load from file
            // TODO: Reactivate
            try {
                MyBlockchain = JsonConvert.DeserializeObject<Blockchain>(File.ReadAllText(BlockchainFilePath));
            } catch (Exception e) {
                MyBlockchain.InitializeChain();
            }

            IpAddress = $"{_baseUrl}:{port}";

            // Start new thread which mines the new block each full minute

            _webSocketServer = new WebSocketServer($"{_baseUrl}:{port}");
            _webSocketServer.AddWebSocketService(_blockchainEndPoint, () => new P2PServer(this));
            _webSocketServer.Start();

            MessageReceived += OnMessageReceived;
        }

        public void MineBlockOnBackgroundThread() {
            IsMining = true;
            Task.Run(
                () => {
                    Console.WriteLine("Started mining new block in the background...");
                    MyBlockchain.MineNewBlock();
                    IsMining = false;
                    Console.WriteLine("Success: New Block mined! Broadcasting to other nodes...");
                });
            BroadcastBlockchain();
        }


        private void OnMessageReceived(object? sender, EventArgs eventArgs) {
            try {
                var e = (MessageEventArgs)eventArgs;

                var message = Message.GetDeserializedMessage(e.Data);

                if (message.MessageTypeEnum == MessageTypeEnum.BlockchainMessage) {
                    var newChain = JsonConvert.DeserializeObject<Blockchain>(message.Data);
                    UpdatePendingTransactions(newChain);
                    if (IsBlockchainNewer(newChain)) {
                        UpdateMyBlockchain(newChain);
                    } else {
                        if (message.Flag) {
                            return;
                        }

                        var answer = new Message() { MessageTypeEnum = MessageTypeEnum.BlockchainMessage, Flag = true, SenderAddress = IpAddress, Data = JsonConvert.SerializeObject(MyBlockchain) };
                        SendBack(sender, JsonConvert.SerializeObject(answer));
                    }
                }

                if (message.MessageTypeEnum == MessageTypeEnum.TransactionMessage) {
                    var transaction = JsonConvert.DeserializeObject<Transaction>(message.Data);
                    MyBlockchain.PendingTransactions.Add(transaction);
                }
            } catch (Exception e) {
                Console.Error.WriteLine(
                    $"Current Method: {MethodBase.GetCurrentMethod()} - Message: {e.Message}");
            }
        }


        private void SendBack(object sender, string data) {
            if (sender.GetType() == typeof(P2PServer)) {
                ((P2PServer)sender).SendBack(data);
            } else {
                ((WebSocket)sender).Send(data);
            }
        }

        private void UpdateMyBlockchain(Blockchain newChain) {
            newChain.PendingTransactions = MyBlockchain.PendingTransactions;
            MyBlockchain = newChain;
        }

        private void UpdatePendingTransactions(Blockchain newChain) {
            var currentTransactions = MyBlockchain.PendingTransactions.Select(x => x.Id);
            var newTransactions = newChain.PendingTransactions.Where(t => !currentTransactions.Contains(t.Id));
            IEnumerable<Transaction> transactions = MyBlockchain.PendingTransactions.Concat(newTransactions);
            MyBlockchain.PendingTransactions = transactions.ToList();
        }
    }
}