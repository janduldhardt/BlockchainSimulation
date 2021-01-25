using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Blockchain2
{
    public class Node : WebSocketBehavior
    {
        
        public string Name { get; set; }
        
        private readonly string _baseUrl = "ws://127.0.0.1";

        private readonly string _blockchainEndPoint = "/Blockchain";


        private WebSocketServer _webSocketServer;

        IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();

        public Blockchain MyBlockchain = new();

        public void Start(int port)
        {
            _webSocketServer = new WebSocketServer($"{_baseUrl}:{port}");
            _webSocketServer.AddWebSocketService<Node>(_blockchainEndPoint);
            _webSocketServer.Start();
        }

        public bool IsChainSynced { get; set; } = false;

        public void Connect(string url)
        {
            try
            {
                if (!wsDict.ContainsKey(url))
                {
                    WebSocket ws = new WebSocket($"{url}{_blockchainEndPoint}");
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
                    ws.Send("Hi Server");
                    ws.Send(JsonConvert.SerializeObject(MyBlockchain));
                    wsDict.Add(url, ws);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Method Name: {MethodBase.GetCurrentMethod()?.Name} Error: {e.Message}");
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "Hi Server")
            {
                Console.WriteLine(e.Data);
                Send("Hi Client");
            }
            else
            {
                // Check whether this nodes our the other nodes Blockchain is the current
                Blockchain newChain = JsonConvert.DeserializeObject<Blockchain>(e.Data);

                if (newChain.IsValid() && newChain.Chain.Count > MyBlockchain.Chain.Count)
                {
                    List<Transaction> newTransactions = new List<Transaction>();
                    newTransactions.AddRange(newChain.PendingTransactions);
                    newTransactions.AddRange(MyBlockchain.PendingTransactions);

                    newChain.PendingTransactions = newTransactions;
                    MyBlockchain = newChain;
                }

                if (!IsChainSynced)
                {
                    Send(JsonConvert.SerializeObject(MyBlockchain));
                    IsChainSynced = true;
                }
            }
        }

        public void BroadcastBlockchain()
        {
            Broadcast(JsonConvert.SerializeObject(MyBlockchain));
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
    }
}