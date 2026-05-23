using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Door : Object
    {
        public string Direction { get; set; }
        public Room SourceRoom;
        public Room DestinationRoom;

        public int Number { get; set; }

        public bool IsQuickestExit = false;

        public static List<string> OrthogonalDoorDirections { get; set; } = new List<string>() { "north", "south", "east", "west" };
        public static List<string> VerticalDoorDirections { get; set; } = new List<string>() { "up", "down" };
        public static List<string> AllDoorDirections { get; set; } = new List<string>() { "north", "south", "east", "west", "up", "down" };

        public Door(Room sourceRoom, Room destinationRoom, string direction, string name, string type, EntityList<Material> materials, bool InOrOn, bool isContainer, Composition content, Architect creator, int weight, bool isGeneralGood, int number, Block b, Structure s, Room r) : base(name, type, materials, InOrOn, isContainer, content, creator, weight, isGeneralGood, b, s, r, false)
        {
            Number = number;
            SourceRoom = sourceRoom;
            DestinationRoom = destinationRoom;
            Direction = direction;
            IsWearable = false;

            Weight = 100000;

            Room = SourceRoom;

            ReferredToNames = new List<string>() { Materials[0].Name + " " + direction + " door" };

            UpdateNames(false, null, false);
        }

        public Door()
        {
        }
    }
}
