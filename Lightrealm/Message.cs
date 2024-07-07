using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Message : Entity
    {
        private int _senderId;

        
        public Architect Sender
        {
            get => EntityGet<Architect>(_senderId);
            set => _senderId = value?.ID ?? 0;
        }

        private int _receiverId;

        
        public Architect Receiver
        {
            get => EntityGet<Architect>(_receiverId);
            set => _receiverId = value?.ID ?? 0;
        }

        public string MessageContent { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string MessageID { get; set; } = "";

        public string PositiveResponse { get; set; } = "";
        public string DirectRefusalResponse { get; set; } = "";
        public string IgnorantResponse { get; set; } = "";
        public string DerailingResponse { get; set; } = "";
        public string FlatteringResponse { get; set; } = "";

        public List<Location> StoredRevealLocations { get; set; } = new List<Location>();
        public List<Entity> Subjects { get; set; } = new List<Entity>();

        public Message(Architect sender, Architect receiver, List<Entity> subjects, string messageType, string messageID, string messageContent, string truthfulResponse, string madeUpResponse, string ignorantResponse, string derailingResponse, string flatteringResponse)
        {
            Sender = sender;
            MessageID = messageID;
            Receiver = receiver;
            Subjects = subjects;
            MessageType = messageType;
            MessageContent = Game1.Capitalize(messageContent);
            PositiveResponse = Game1.Capitalize(truthfulResponse);
            DirectRefusalResponse = Game1.Capitalize(madeUpResponse);
            IgnorantResponse = Game1.Capitalize(ignorantResponse);
            DerailingResponse = Game1.Capitalize(derailingResponse);
            FlatteringResponse = Game1.Capitalize(flatteringResponse);
        }

        public Message()
        {

        }
    }
}
