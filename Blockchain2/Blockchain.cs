#region

#endregion

namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Blockchain {
        public IList<Transaction> PendingTransactions = new List<Transaction>();

        public int Reward = 10;

        public IList<Block> Chain { set; get; }

        public int Difficulty { set; get; } = 2;

        public void AddTransactionIfValid(Transaction transaction) {
            PendingTransactions.Add(transaction);
        }

        public int GetBalance(string address) {
            var balance = 0;

            for (var i = 0; i < Chain.Count; i++) {
                for (var j = 0; j < Chain[i].Transactions.Count; j++) {
                    var transaction = Chain[i].Transactions[j];

                    if (transaction.Sender == address) {
                        balance -= transaction.Amount;
                    }

                    if (transaction.Receiver == address) {
                        balance += transaction.Amount;
                    }
                }
            }

            return balance;
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
            var transactions = PendingTransactions.OrderBy(x => x.TimeStamp);
            var validTransactions = new List<Transaction>();
            foreach (var transaction in transactions) {
                if (transaction.Status == TransactionStatusEnum.Pending) {
                    continue;
                }

                PendingTransactions.Remove(transaction);
                validTransactions.Add(transaction);
            }

            // validTransactions.Add(MiningRewardTransaction(minerAddress));

            var block = new Block(DateTime.Now, GetLatestBlock().Hash, validTransactions);
            AddBlock(block);

            // PendingTransactions = new List<Transaction>();
        }

        private void AddBlock(Block block) {
            var latestBlock = GetLatestBlock();
            block.Height = latestBlock.Height + 1;
            block.PreviousHash = latestBlock.Hash;
            block.Mine(Difficulty);
            Chain.Add(block);
        }

        private void AddGenesisBlock() { Chain.Add(CreateGenesisBlock()); }

        private Block CreateGenesisBlock() {
            var block = new Block(DateTime.Now, null, PendingTransactions);
            block.Mine(Difficulty);
            PendingTransactions = new List<Transaction>();
            return block;
        }

        private Block GetLatestBlock() { return Chain[Chain.Count - 1]; }

        private Transaction MiningRewardTransaction(string minerAddress) { return new(null, minerAddress, Reward); }
    }
}