namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;

    public class Block {

        public Block(DateTime timeStamp, string previousHash, IList<Transaction> transactions) {
            Height = 0;
            TimeStamp = timeStamp;
            PreviousHash = previousHash;
            Transactions = transactions;
        }

        public string Hash { get; set; }

        public int Height { get; set; }

        public int Nonce { get; set; } = 0;

        public string PreviousHash { get; set; }

        public DateTime TimeStamp { get; set; }

        public IList<Transaction> Transactions { get; set; }

        public string CalculateHash() {
            SHA256 sha256 = SHA256.Create();

            byte[] inputBytes = Encoding.ASCII.GetBytes($"{TimeStamp}-{PreviousHash ?? ""}-{JsonConvert.SerializeObject(Transactions)}-{Nonce}");
            byte[] outputBytes = sha256.ComputeHash(inputBytes);

            return Convert.ToBase64String(outputBytes);
        }

        public void Mine(int difficulty) {
            var leadingZeros = new string('0', difficulty);
            while (Hash == null || Hash.Substring(0, difficulty) != leadingZeros) {
                Nonce++;
                Hash = CalculateHash();
            }
        }
    }
}