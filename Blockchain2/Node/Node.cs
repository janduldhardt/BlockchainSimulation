using System;
using System.Collections.Generic;
using Blockchain2.Node.WebSocketBehavior;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Blockchain2.Node
{
    public class Node
    {
        private readonly string _baseUrl = "ws://127.0.0.1";
        
        private readonly string _blockchainEndPoint = "/Blockchain";
        

        private readonly WebSocketServer _webSocketServer;
        
        IDictionary<string, WebSocket> wsDict = new Dictionary<string, WebSocket>();

        public Blockchain MyBlockchain = new Blockchain();


        public Node(int port)
        {
            _webSocketServer = new WebSocketServer($"{_baseUrl}:{port}");
            _webSocketServer.AddWebSocketService(_blockchainEndPoint, () => new BlockchainWebSocketBehavior(this));
            _webSocketServer.Start();
        }

        public bool IsChainSynced { get; set; } = false;
        
        public void Connect(string url)
        {
            if (!wsDict.ContainsKey(url))
            {
                WebSocket ws = new WebSocket(url);
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
        
        public void Close()
        {
            foreach (var item in wsDict)
            {
                item.Value.Close();
            }
        }
    }
}