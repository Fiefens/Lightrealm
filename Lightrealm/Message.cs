using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Lightrealm
{
    [Serializable]

    public class Message : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public Architect Sender;
        public Architect Receiver;

        public List<Entity> Subjects = new List<Entity>();

        public string MessageContent = "";
        public string MessageType = "";
        public string MessageID = "";

        public string PositiveResponse = "";
        public string DirectRefusalResponse = "";
        public string IgnorantResponse = "";
        public string DerailingResponse = "";
        public string FlatteringResponse = "";

        public List<Location> StoredRevealLocations = new List<Location>();

        public Message(Architect sender, Architect reciever, List<Entity> subjects, string messageType, string messageID, string messageContent, string truthfulResponse, string madeUpResponse, string ignorantResponse, string derailingResponse, string flatteringResponse)
        {
            Sender = sender;
            MessageID = messageID;
            Receiver = reciever;
            Subjects = subjects;
            MessageType = messageType;
            MessageContent = Game1.Capitalize(messageContent);
            PositiveResponse = Game1.Capitalize(truthfulResponse);
            DirectRefusalResponse = Game1.Capitalize(madeUpResponse);
            IgnorantResponse = Game1.Capitalize(ignorantResponse);
            DerailingResponse = Game1.Capitalize(derailingResponse);
            FlatteringResponse = Game1.Capitalize(flatteringResponse);
        }
    }
}
