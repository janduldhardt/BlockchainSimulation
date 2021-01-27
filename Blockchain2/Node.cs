namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using Newtonsoft.Json;
    using WebSocketSharp;
    using WebSocketSharp.Server;

    public class Node {

        public EventHandler MessageReceived;
        private readonly string _baseUrl = "ws://127.0.0.1";

        private readonly string _blockchainEndPoint = "/Blockchain";

        private readonly IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();


        private WebSocketServer _webSocketServer;

        public string BlockchainFilePath => Path.Combine(Directory.GetCurrentDirectory(), Name + ".json");

        public Blockchain MyBlockchain { get; set; } = new();

        public string Name { get; set; }

        public void Broadcast(string data) {
            foreach (KeyValuePair<string, WebSocket> item in wsDict) {
                item.Value.Send(data);
            }
        }

        public void BroadcastBlockchain() {
            var message = new Message() { MessageTypeEnum = MessageTypeEnum.BlockchainMessage, SenderAddress = Address, Data = JsonConvert.SerializeObject(MyBlockchain) };
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

                    ws.OnMessage += (sender, e) => {
                        if (e.Data == "Hi Client") {
                            Console.WriteLine(e.Data);
                        } else {
                            var newChain = JsonConvert.DeserializeObject<Blockchain>(e.Data);
                            UpdateMyBlockchain(newChain);
                        }
                    };
                    ws.Connect();
                    wsDict.Add(url, ws);
                }
            } catch (Exception e) {
                Console.WriteLine($"Method Name: {MethodBase.GetCurrentMethod()?.Name} Error: {e.Message}");
            }
        }

        public bool OpenConnection() {
            foreach ((string _, WebSocket value) in wsDict) {
                if (!value.IsAlive) {
                    return false;
                }
            }

            return true;
        }

        public void Start(int port) {
            // Init blockchain or load from file

            Address = $"{_baseUrl}:{port}";

            try {
                MyBlockchain = JsonConvert.DeserializeObject<Blockchain>(File.ReadAllText(BlockchainFilePath));
            } catch (Exception e) {
                MyBlockchain.InitializeChain();
            }

            // Start new thread which mines the new block each full minute
            int startin = 60 - DateTime.Now.Second;
            var t = new Timer(
                o => { MyBlockchain.MineNewBlock(Name); },
                null,
                startin * 1000,
                60000);

            _webSocketServer = new WebSocketServer($"{_baseUrl}:{port}");
            _webSocketServer.AddWebSocketService(_blockchainEndPoint, () => new P2PServer(this));
            _webSocketServer.Start();

            MessageReceived += OnMessageReceived;
        }

        public string Address { get; set; }

        public void UpdateMyBlockchain(Blockchain newChain) {
            // Check whether this nodes our the other nodes Blockchain is the current
            if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count) {
                var newTransactions = new List<Transaction>();
                newTransactions.AddRange(newChain.PendingTransactions);
                newTransactions.AddRange(MyBlockchain.PendingTransactions);

                newChain.PendingTransactions = newTransactions;
                MyBlockchain = newChain;
            }
        }

        private void OnMessageReceived(object? sender, EventArgs eventArgs) {
            try {
                var e = (MessageEventArgs)eventArgs;
                var p2PServer = (P2PServer)sender;

                var message = Message.GetDeserializedMessage(e.Data);

                if (message.MessageTypeEnum == MessageTypeEnum.BlockchainMessage) {
                    var newChain = JsonConvert.DeserializeObject<Blockchain>(message.Data);
                    UpdateMyBlockchain(newChain);
                }
            } catch (Exception) {
                // ignored
            }
        }
    }
}