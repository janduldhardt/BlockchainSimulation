namespace Blockchain2 {
    using System;

    public class Transaction {

        public Transaction(string sender, string receiver, int amount) {
            Sender = sender;
            Receiver = receiver;
            Amount = amount;
            TimeStamp = DateTime.Now;
            Status = TransactionStatusEnum.Pending;
        }

        public TransactionStatusEnum Status { get; set; }

        public int Amount { get; set; }

        public string Sender { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Receiver { get; set; }

        /// <inheritdoc />
        public string CombinedString() {
            return $"Sender: {Sender} | Amount: ${Amount} | Date: {TimeStamp.ToString("f")}";
        }
    }
}