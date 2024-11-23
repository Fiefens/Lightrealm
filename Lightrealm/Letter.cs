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
        private int _author;
        public Architect Author
        {
            get => EntityGet<Architect>(_author);
            set => _author = value?.ID ?? 0;
        }
        private int _recipient;
        public Entity Recipient
        {
            get => EntityGet<Entity>(_recipient);
            set => _recipient = value?.ID ?? 0;
        }
        private int _text;
        public TextStorage Text
        {
            get => EntityGet<TextStorage>(_text);
            set => _text = value?.ID ?? 0;
        }



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
