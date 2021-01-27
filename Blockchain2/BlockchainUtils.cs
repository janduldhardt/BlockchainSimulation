namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class BlockchainUtils {
        public static double GetRating(string address, Blockchain blockchain) {
            var relevantTransactions = blockchain.Chain.SelectMany(x => x.Transactions).Where(x => x.Receiver == address || x.Sender == address);
            var positveCount = relevantTransactions.Count(x => x.Status == TransactionStatusEnum.Accepted);
            var negCount = relevantTransactions.Count(x => x.Status == TransactionStatusEnum.Declined);

            try {
                return (double)positveCount / negCount;
            } catch (ArithmeticException e) {
                Console.WriteLine(e);
                return 100;
            }
        }
        public static IEnumerable<Transaction> GetTransactionsToVerify(Blockchain blockchain, string address) {
            var transactions = blockchain.PendingTransactions.Where(x => x.Status == TransactionStatusEnum.Pending && x.Receiver == address);
            return transactions;
        }
        
        public static List<Transaction> FindTransactions(Blockchain blockchain, string sender, string receiver, string amount) {
            var allTransactions = blockchain.Chain.SelectMany(x => x.Transactions);
            if (!String.IsNullOrEmpty(sender)) {
                allTransactions = allTransactions.Where(x => sender.ToLower().Trim() == x.Sender?.ToLower());
            }

            if (!String.IsNullOrEmpty(receiver)) {
                allTransactions = allTransactions.Where(x => receiver.ToLower().Trim() == x.Receiver.ToLower());
            }

            if (!String.IsNullOrEmpty(amount)) {
                allTransactions = allTransactions.Where(x => amount.ToLower().Trim() == x.Amount.ToString());
            }

            return allTransactions.ToList();
        }

        public static Dictionary<string, int> GetBalanceDict(string nodeName, Blockchain blockchain) {
            var transactions = blockchain.Chain.SelectMany(t => t.Transactions).Where(x => x.Status == TransactionStatusEnum.Accepted && (x.Receiver == nodeName || x.Sender == nodeName));
            var debtDict = new Dictionary<string, int>();
            foreach (var transaction in transactions) {
                var name = transaction.Receiver == nodeName ? transaction.Sender : transaction.Receiver;
                if (!debtDict.ContainsKey(name)) {
                    debtDict.Add(name, 0);
                }

                debtDict[name] += transaction.Receiver == nodeName ? transaction.Amount : transaction.Amount * -1;
            }

            return debtDict;
        }

        public static Transaction FindTransactionById(Blockchain blockchain, string id) {
            return blockchain.Chain.SelectMany(x => x.Transactions).FirstOrDefault(x => x.Id.ToString() == id);
        }
    }
}