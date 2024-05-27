using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Door : Object
    {
        public Room SourceRoom { get; set; }
        public Room DestinationRoom { get; set; }
        public string Direction { get; set; }

        public int Number;

        public static List<string> OrthogonalDoorDirections { get; set; } = new List<string>() { "north", "south", "east", "west" };
        public static List<string> VerticalDoorDirections { get; set; } = new List<string>() { "up", "down" };
        public static List<string> AllDoorDirections { get; set; } = new List<string>() { "north", "south", "east", "west", "up", "down"};


        public Door(Room sourceRoom, Room destinationRoom, string direction, string name, string type, List<Material> materials, bool InOrOn, bool isContainer, Composition content, Architect creator, int weight, bool isGeneralGood, int number, Block b, Structure s, Room r) : base(name, type, materials, InOrOn, isContainer, content, creator, weight, isGeneralGood, b, s, r, false)
        {
            Number = number;
            SourceRoom = sourceRoom;
            DestinationRoom = destinationRoom;
            Direction = direction;
            IsWearable = false;

            Room = SourceRoom;

            ReferredToNames = new List<string>() { "Placeholder" };

            UpdateNames();
        }

        public Door()
        {
        }

    }
}
