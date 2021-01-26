#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Blockchain2
{
    public class Blockchain
    {
        public IList<Transaction> PendingTransactions = new List<Transaction>();

        public int Reward = 10;

        public IList<Block> Chain { set; get; }
        public int Difficulty { set; get; } = 2;

        public void InitializeChain()
        {
            Chain = new List<Block>();
            AddGenesisBlock();
        }

        private Block CreateGenesisBlock()
        {
            var block = new Block(DateTime.Now, null, PendingTransactions);
            block.Mine(Difficulty);
            PendingTransactions = new List<Transaction>();
            return block;
        }

        private void AddGenesisBlock()
        {
            Chain.Add(CreateGenesisBlock());
        }

        private Block GetLatestBlock()
        {
            return Chain[Chain.Count - 1];
        }

        public void AddTransactionIfValid(Transaction transaction)
        {
            if (transaction.FromAddress != null && !IsTransactionValid(transaction, PendingTransactions))
            {
                Console.WriteLine("Transaction invalid - Insufficient Funds");
                return;
            }

            PendingTransactions.Add(transaction);
        }

        public void ProcessPendingTransactions(string minerAddress)
        {
            var transactions = PendingTransactions.OrderBy(x => x.TimeStamp);
            var validTransactions = new List<Transaction>();
            foreach (var transaction in transactions)
            {
                if (!IsTransactionValid(transaction, validTransactions)) continue;

                validTransactions.Add(transaction);
            }

            validTransactions.Add(MiningRewardTransaction(minerAddress));

            var block = new Block(DateTime.Now, GetLatestBlock().Hash, validTransactions);
            AddBlock(block);

            PendingTransactions = new List<Transaction>();
        }

        private Transaction MiningRewardTransaction(string minerAddress)
        {
            return new(null, minerAddress, Reward);
        }

        private void AddBlock(Block block)
        {
            var latestBlock = GetLatestBlock();
            block.Height = latestBlock.Height + 1;
            block.PreviousHash = latestBlock.Hash;
            block.Mine(Difficulty);
            Chain.Add(block);
        }

        private bool IsTransactionValid(Transaction transaction, IList<Transaction> transactions)
        {
            var userMoney = 0;
            foreach (var block in Chain)
            foreach (var blockTransaction in block.Transactions)
            {
                if (transaction.FromAddress == blockTransaction.FromAddress) userMoney -= blockTransaction.Amount;

                if (transaction.FromAddress == blockTransaction.ToAddress) userMoney += blockTransaction.Amount;
            }

            foreach (var pendingTransaction in transactions)
            {
                if (transaction.FromAddress == pendingTransaction.FromAddress) userMoney -= pendingTransaction.Amount;

                if (transaction.FromAddress == pendingTransaction.ToAddress) userMoney += pendingTransaction.Amount;
            }

            return userMoney - transaction.Amount >= 0;
        }

        public bool IsValid()
        {
            for (var i = 1; i < Chain.Count; i++)
            {
                var currentBlock = Chain[i];
                var previousBlock = Chain[i - 1];

                if (currentBlock.Hash != currentBlock.CalculateHash()) return false;

                if (currentBlock.PreviousHash != previousBlock.Hash) return false;
            }

            return true;
        }

        public int GetBalance(string address)
        {
            var balance = 0;

            for (var i = 0; i < Chain.Count; i++)
            for (var j = 0; j < Chain[i].Transactions.Count; j++)
            {
                var transaction = Chain[i].Transactions[j];

                if (transaction.FromAddress == address) balance -= transaction.Amount;

                if (transaction.ToAddress == address) balance += transaction.Amount;
            }

            return balance;
        }
    }
}