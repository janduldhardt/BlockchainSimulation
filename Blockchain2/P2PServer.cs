namespace Blockchain2 {
    using WebSocketSharp;
    using WebSocketSharp.Server;

    public class P2PServer : WebSocketBehavior {
        private bool _chainSynched;

        private readonly Node _node;
        private WebSocketServer _wss;

        public P2PServer(Node node) { _node = node; }

        public void SendBack(string data) { Send(data); }

        // public void Start(int port)
        // {
        //     _wss = new WebSocketServer($"ws://127.0.0.1:{port}");
        //     _wss.AddWebSocketService<P2PServer>("/Blockchain");
        //     _wss.Start();
        //     Console.WriteLine($"Started server at ws://127.0.0.1:{port}");
        // }

        protected override void OnMessage(MessageEventArgs e) { _node?.MessageReceived?.Invoke(this, e); }
    }
}