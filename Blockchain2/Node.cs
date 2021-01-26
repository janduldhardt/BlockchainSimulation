using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Blockchain2
{
    public class Node
    {
        public string Name { get; set; }

        private readonly string _baseUrl = "ws://127.0.0.1";

        private readonly string _blockchainEndPoint = "/Blockchain";

        public string BlockchainFilePath
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), Name + ".json"); }
        }


        private WebSocketServer _webSocketServer;

        IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();

        public Blockchain MyBlockchain { get; set; } = new Blockchain();

        public EventHandler MessageReceived;

        public Node()
        {
        }

        public void Start(int port)
        {
            // Init blockchain or load from file
            try
            {
                MyBlockchain = JsonConvert.DeserializeObject<Blockchain>(File.ReadAllText(BlockchainFilePath));
            }
            catch (Exception e)
            {
                MyBlockchain.InitializeChain();
            }

            _webSocketServer = new WebSocketServer($"{_baseUrl}:{port}");
            _webSocketServer.AddWebSocketService<P2PServer>(_blockchainEndPoint, (() => new P2PServer(this)));
            _webSocketServer.Start();

            MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object? sender, EventArgs eventArgs)
        {
            try
            {
                var e = (MessageEventArgs) eventArgs;
                var p2PServer = (P2PServer) sender;
                if (e.Data == "Hi Server")
                {
                    Console.WriteLine(e.Data);
                    p2PServer?.SendBack("Hi Client");
                }
                else
                {
                    var newChain = JsonConvert.DeserializeObject<Blockchain>(e.Data);
                    UpdateMyBlockchain(newChain);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void UpdateMyBlockchain(Blockchain newChain)
        {
            // Check whether this nodes our the other nodes Blockchain is the current
            if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count)
            {
                List<Transaction> newTransactions = new List<Transaction>();
                newTransactions.AddRange(newChain.PendingTransactions);
                newTransactions.AddRange(MyBlockchain.PendingTransactions);

                newChain.PendingTransactions = newTransactions;
                MyBlockchain = newChain;
                
            }

            // if (!IsChainSynced)
            // {
            //     p2PServer?.SendBack(JsonConvert.SerializeObject(MyBlockchain));
            //     IsChainSynced = true;
            // }
        }

        public bool IsChainSynced { get; set; } = false;

        public void Connect(string url)
        {
            try
            {
                if (!wsDict.ContainsKey(url))
                {
                    url = $"{url}{_blockchainEndPoint}";
                    WebSocket ws = new WebSocket($"{url}");
                    ws.OnClose += (sender, e) => { wsDict.Remove(url); };

                    ws.OnMessage += (sender, e) =>
                    {
                        if (e.Data == "Hi Client")
                        {
                            Console.WriteLine(e.Data);
                        }
                        else
                        {
                            Blockchain newChain = JsonConvert.DeserializeObject<Blockchain>(e.Data);
                            if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count)
                            {
                                List<Transaction> newTransactions = new List<Transaction>();
                                newTransactions.AddRange(newChain.PendingTransactions);
                                newTransactions.AddRange(MyBlockchain.PendingTransactions);

                                newChain.PendingTransactions = newTransactions;
                                MyBlockchain = newChain;
                            }
                        }
                    };
                    ws.Connect();
                    // ws.Send("Hi Server");
                    // ws.Send(JsonConvert.SerializeObject(MyBlockchain));
                    wsDict.Add(url, ws);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Method Name: {MethodBase.GetCurrentMethod()?.Name} Error: {e.Message}");
            }
        }

        public void BroadcastBlockchain()
        {
            var data = JsonConvert.SerializeObject(MyBlockchain);
            Broadcast(data);
        }

        public void Broadcast(string data)
        {
            foreach (var item in wsDict)
            {
                item.Value.Send(data);
            }
        }

        public void Close()
        {
            foreach (var item in wsDict)
            {
                item.Value.Close();
            }
        }

        public bool OpenConnection()
        {
            foreach (var (_, value) in wsDict)
            {
                if (!value.IsAlive)
                {
                    return false;
                }
            }

            return true;
        }
    }
}