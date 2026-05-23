using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Letter : Entity
    {
        public TextStorage Text = null;

        public Architect Author;
        public Entity Recipient;


        public Letter(Architect author, Entity recipient, TextStorage text, bool underground)
        {
            Author = author;
            Recipient = recipient;
            Text = text;

            Game1.GameWorld.TotalLetters++;

            if(underground)
            {
                Game1.GameWorld.UndergroundLetters.Add(this);
            }
            else
            {
                Game1.GameWorld.RegularLetters.Add(this);
            }
        }
    }
}
