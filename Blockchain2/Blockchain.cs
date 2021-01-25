using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using MoreLinq.Extensions;

namespace Blockchain2
{
    public class Blockchain
    {
        public IList<Transaction> PendingTransactions = new List<Transaction>();
        public IList<Block> Chain { set; get; }
        public int Difficulty { set; get; } = 2;

        public int Reward = 10;

        public Blockchain()
        {
            InitializeChain();
        }

        public void InitializeChain()
        {
            Chain = new List<Block>();
            AddGenesisBlock();
        }

        public Block CreateGenesisBlock()
        {
            Block block = new Block(DateTime.Now, null, PendingTransactions);
            block.Mine(Difficulty);
            PendingTransactions = new List<Transaction>();
            return block;
        }

        public void AddGenesisBlock()
        {
            Chain.Add(CreateGenesisBlock());
        }

        public Block GetLatestBlock()
        {
            return Chain[Chain.Count - 1];
        }

        public void AddTransactionIfValid(Transaction transaction)
        {
            if (transaction.FromAddress != null && !IsTransactionValid(transaction))
            {
                Console.WriteLine("Transaction invalid - Insufficient Funds");
                return;
            }

            PendingTransactions.Add(transaction);
        }

        public void ProcessPendingTransactions(string minerAddress)
        {
            PendingTransactions.Add(MiningRewardTransaction(minerAddress));
            var transactions = PendingTransactions.OrderBy(x => x.TimeStamp);
            var validTransactions = new List<Transaction>();
            foreach (var transaction in transactions)
            {
                if (!IsTransactionValid(transaction))
                {
                    continue;
                }

                validTransactions.Add(transaction);
            }

            validTransactions.Add(MiningRewardTransaction(minerAddress));

            Block block = new Block(DateTime.Now, GetLatestBlock().Hash, validTransactions);
            AddBlock(block);

            PendingTransactions = new List<Transaction>();
        }

        public Transaction MiningRewardTransaction(string minerAddress)
        {
            return new(null, minerAddress, Reward);
        }

        public void AddBlock(Block block)
        {
            Block latestBlock = GetLatestBlock();
            block.Index = latestBlock.Index + 1;
            block.PreviousHash = latestBlock.Hash;
            block.Mine(this.Difficulty);
            Chain.Add(block);
        }

        public bool IsTransactionValid(Transaction transaction)
        {
            var user = transaction.FromAddress;
            var userMoney = 0;
            foreach (var block in Chain)
            {
                foreach (var blockTransaction in block.Transactions)
                {
                    if (transaction.FromAddress == blockTransaction.FromAddress)
                    {
                        userMoney -= blockTransaction.Amount;
                    }

                    if (transaction.FromAddress == blockTransaction.ToAddress)
                    {
                        userMoney += blockTransaction.Amount;
                    }
                }
            }

            foreach (var pendingTransaction in PendingTransactions)
            {
                if (transaction.FromAddress == pendingTransaction.FromAddress)
                {
                    userMoney -= pendingTransaction.Amount;
                }

                if (transaction.FromAddress == pendingTransaction.ToAddress)
                {
                    userMoney += pendingTransaction.Amount;
                }
            }

            return userMoney - transaction.Amount >= 0;
        }

        public bool IsValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                Block currentBlock = Chain[i];
                Block previousBlock = Chain[i - 1];

                if (currentBlock.Hash != currentBlock.CalculateHash())
                {
                    return false;
                }

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetBalance(string address)
        {
            int balance = 0;

            for (int i = 0; i < Chain.Count; i++)
            {
                for (int j = 0; j < Chain[i].Transactions.Count; j++)
                {
                    var transaction = Chain[i].Transactions[j];

                    if (transaction.FromAddress == address)
                    {
                        balance -= transaction.Amount;
                    }

                    if (transaction.ToAddress == address)
                    {
                        balance += transaction.Amount;
                    }
                }
            }

            return balance;
        }
    }
}