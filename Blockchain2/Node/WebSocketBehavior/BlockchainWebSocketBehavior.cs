using WebSocketSharp;

namespace Blockchain2.Node.WebSocketBehavior
{
    public class BlockchainWebSocketBehavior : WebSocketSharp.Server.WebSocketBehavior
    {
        private Node _node;

        public BlockchainWebSocketBehavior() : this(null)
        {
        }
        
        public BlockchainWebSocketBehavior(Node node)
        {
            _node = node;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            
            
        }
    }
}