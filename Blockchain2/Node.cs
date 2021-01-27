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

        private IList<string> knownNodeAddresses = new List<string>();

        private readonly IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();


        private WebSocketServer _webSocketServer;

        public string Address { get; set; }

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
                    ws.OnMessage += OnMessageReceived;
                    ws.Connect();
                    wsDict.Add(url, ws);
                    
                }
            } catch (Exception e) {
                Console.WriteLine($"Method Name: {MethodBase.GetCurrentMethod()?.Name} Error: {e.Message}");
            }
        }

        public bool IsBlockchainNewer(Blockchain newChain) {
            // Check whether this nodes our the other nodes Blockchain is the current
            if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count) {
                return true;
            }

            return false;
        }


        public bool OpenConnection() {
            foreach ((string _, WebSocket value) in wsDict) {
                if (!value.IsAlive) {
                    return false;
                }
            }

            return true;
        }

        public void RequestBlockchain() {
            // var ws = DictHelper.RandomValues(wsDict).First();
            var message = new Message() { MessageTypeEnum = MessageTypeEnum.RequestBlockchainMessage, SenderAddress = Address };
            // ws.Send(JsonConvert.SerializeObject(message));
            Broadcast(JsonConvert.SerializeObject(message));
        }

        public void Start(int port) {
            // Init blockchain or load from file
            // TODO: Reactivate
            // try {
            //     MyBlockchain = JsonConvert.DeserializeObject<Blockchain>(File.ReadAllText(BlockchainFilePath));
            // } catch (Exception e) {
            //     MyBlockchain.InitializeChain();
            // }
            MyBlockchain.InitializeChain();

            Address = $"{_baseUrl}:{port}";

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


        private void OnMessageReceived(object? sender, EventArgs eventArgs) {
            try {
                var e = (MessageEventArgs)eventArgs;

                var message = Message.GetDeserializedMessage(e.Data);

                if (message.MessageTypeEnum == MessageTypeEnum.BlockchainMessage) {
                    var newChain = JsonConvert.DeserializeObject<Blockchain>(message.Data);
                    if (IsBlockchainNewer(newChain)) {
                        UpdateMyBlockchain(newChain);
                    } else {
                        var answer = new Message() { MessageTypeEnum = MessageTypeEnum.BlockchainMessage, SenderAddress = Address, Data = JsonConvert.SerializeObject(MyBlockchain) };
                        SendBack(sender, JsonConvert.SerializeObject(answer));
                    }

                    ;
                }

                // if (message.MessageTypeEnum == MessageTypeEnum.RequestBlockchainMessage) {
                //     var answer = new Message() { MessageTypeEnum = MessageTypeEnum.BlockchainMessage, SenderAddress = Address, Data = JsonConvert.SerializeObject(MyBlockchain) };
                //     p2PServer?.SendBack(JsonConvert.SerializeObject(answer));
                // }
            } catch (Exception) {
                // ignored
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
            var newTransactions = new List<Transaction>();
            newTransactions.AddRange(newChain.PendingTransactions);
            newTransactions.AddRange(MyBlockchain.PendingTransactions);

            newChain.PendingTransactions = newTransactions;
            MyBlockchain = newChain;
        }
    }
}