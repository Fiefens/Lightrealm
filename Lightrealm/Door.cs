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
        private int _sourceRoomId;

        [JsonIgnore]
        public Room SourceRoom
        {
            get => EntityGet<Room>(_sourceRoomId);
            set => _sourceRoomId = value?.ID ?? 0;
        }

        private int _destinationRoomId;

        [JsonIgnore]
        public Room DestinationRoom
        {
            get => EntityGet<Room>(_destinationRoomId);
            set => _destinationRoomId = value?.ID ?? 0;
        }

        public string Direction { get; set; }

        public int Number { get; set; }

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

            Room = SourceRoom;

            ReferredToNames = new List<string>() { "Placeholder" };

            UpdateNames();
        }

        public Door()
        {
        }
    }
}
