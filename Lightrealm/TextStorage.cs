using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class TextStorage
    {
        public string Data { get; set; }

        private byte red;
        private byte green;
        private byte blue;


        public bool Visible = false;

        public Quest AttachedQuest = null;

        public EntityList<Entity> Entities { get; set; } = new EntityList<Entity>();

        public TextStorage(string data, Color color, EntityList<Entity> Entities)
        {
            this.Data = data;
            this.Red = color.R;
            this.Green = color.G;
            this.Blue = color.B;
            this.Entities = Entities;
        }

        public TextStorage()
        {

        }

        
        public Color Color
        {
            get => new Color(red, green, blue);
            set
            {
                red = value.R;
                green = value.G;
                blue = value.B;
            }
        }

        public byte Red
        {
            get => red;
            set => red = value;
        }

        public byte Green
        {
            get => green;
            set => green = value;
        }

        public byte Blue
        {
            get => blue;
            set => blue = value;
        }
    }
}
