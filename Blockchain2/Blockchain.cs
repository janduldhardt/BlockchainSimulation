namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;

    public class Blockchain {

        public int MaxTransactions = 10;
        public IList<Transaction> PendingTransactions = new List<Transaction>();

        public int Reward = 10;

        public IList<Block> Chain { set; get; }

        public int Difficulty { set; get; } = 2;

        public TimeSpan TimeBeetweenMining { get; } = new TimeSpan(0,1,0);


        public void AddTransaction(Transaction transaction) {
            // if (PendingTransactions.Count(x => x.Status != TransactionStatusEnum.Pending) >= MaxTransactions) {
            //     Console.WriteLine("Maximum transaction reached! Transaction not added. Please mine the new block");
            //     return;
            // }
            PendingTransactions.Add(transaction);
        }


        public void InitializeChain() {
            Chain = new List<Block>();
            AddGenesisBlock();
        }

        public bool IsValid() {
            for (var i = 1; i < Chain.Count; i++) {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != currentBlock.CalculateHash()) {
                    return false;
                }

                if (currentBlock.PreviousHash != previousBlock.Hash) {
                    return false;
                }
            }

            return true;
        }

        public void MineNewBlock() {
            var transactions = PendingTransactions.OrderBy(x => x.TimeStamp).Where(t => t.Status != TransactionStatusEnum.Pending).Take(MaxTransactions);
            var validTransactions = new List<Transaction>();
            foreach (var transaction in transactions) {
                PendingTransactions.Remove(transaction);
                validTransactions.Add(transaction);
            }

            var block = new Block(DateTime.Now, GetLatestBlock().Hash, validTransactions);
            AddBlock(block);
            // Increase difficulty if blocks are mined too fast
        }

        private void AddBlock(Block block) {
            var latestBlock = GetLatestBlock();
            block.Height = latestBlock.Height + 1;
            block.PreviousHash = latestBlock.Hash;
            block.Mine(Difficulty);
            Chain.Add(block);

            if (block.TimeStamp - latestBlock.TimeStamp < TimeBeetweenMining) {
                Difficulty += 1;
            } else {
                Difficulty = Difficulty == 1 ? Difficulty : Difficulty - 1;
            }
        }

        private void AddGenesisBlock() { Chain.Add(CreateGenesisBlock()); }

        private Block CreateGenesisBlock() {
            var block = new Block(DateTime.Now, null, PendingTransactions);
            block.Mine(Difficulty);
            PendingTransactions = new List<Transaction>();
            return block;
        }

        public Block GetLatestBlock() { return Chain[Chain.Count - 1]; }

        private Transaction MiningRewardTransaction(string minerAddress) { return new(null, minerAddress, Reward); }
    }
}