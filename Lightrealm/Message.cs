using Humanizer;
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
        public Architect Sender;
        public Architect Receiver;

        public string MessageContent { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string MessageID { get; set; } = "";

        public string PositiveResponse { get; set; } = "";
        public string DirectRefusalResponse { get; set; } = "";
        public string IgnorantResponse { get; set; } = "";
        public string DerailingResponse { get; set; } = "";
        public string FlatteringResponse { get; set; } = "";

        public bool IgnoreHeader = false;

        public EntityList<Entity> ResponseEntitiesForOne = new EntityList<Entity>();
        public EntityList<Entity> ResponseEntitiesForTwo = new EntityList<Entity>();
        public EntityList<Entity> ResponseEntitiesForThree = new EntityList<Entity>();
        public EntityList<Entity> ResponseEntitiesForFour = new EntityList<Entity>();
        public EntityList<Entity> ResponseEntitiesForFive = new EntityList<Entity>();

        public EntityList<Location> StoredRevealLocations { get; set; } = new EntityList<Location>();
        public EntityList<Architect> StoredKnownArchs { get; set; } = new EntityList<Architect>();
        public EntityList<Entity> Subjects { get; set; } = new EntityList<Entity>();

        public Message(Architect sender, Architect receiver, EntityList<Entity> subjects, string messageType, string messageID, string messageContent, string truthfulResponse, string madeUpResponse, string ignorantResponse, string derailingResponse, string flatteringResponse)
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

            // Set both sender and receiver as important if either is in the player's party
            if (Game1.GameWorld.GamePlayerAssociation != null)
            {
                var partyArchitects = Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects;
                if (partyArchitects.Contains(Sender) || partyArchitects.Contains(Receiver))
                {
                    Sender.ImportantThisLoad = true;
                    Receiver.ImportantThisLoad = true;
                }
            }

            Game1.MessagesThisLoad.Add(this);
        }


        public Message()
        {

        }
    }
}
