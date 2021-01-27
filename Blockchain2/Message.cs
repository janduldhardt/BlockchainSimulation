namespace Blockchain2 {
    using System;
    using Newtonsoft.Json;

    public class Message {
        public MessageTypeEnum MessageTypeEnum { get; set; }

        public string SenderAddress { get; set; }

        public string Data { get; set; }

        public static string GetSerializedMessage(Message message) { return JsonConvert.SerializeObject(message); }

        public static Message GetDeserializedMessage(string data) { return JsonConvert.DeserializeObject<Message>(data); }
    }
}