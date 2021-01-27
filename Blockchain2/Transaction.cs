namespace Blockchain2 {
    using System;

    public class Transaction {

        public Transaction(string fromAddress, string toAddress, int amount) {
            FromAddress = fromAddress;
            ToAddress = toAddress;
            Amount = amount;
            TimeStamp = DateTime.Now;
        }

        public int Amount { get; set; }

        public string FromAddress { get; set; }

        public DateTime TimeStamp { get; set; }

        public string ToAddress { get; set; }
    }
}