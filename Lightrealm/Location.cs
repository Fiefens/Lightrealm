using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Location : Entity
    {
        public string Type { get; set; } //city<10000, town <1000, village<200, camp <10
        public Race PrimaryRace { get; set; } = Game1.GameWorld.GetRace("");

        public List<Object> GeneralItemsWeHave = new List<Object>();

        public bool Explored = false;

        public List<District> DistrictsToAdd { get; set; } = new List<District>();
        public List<District> Districts { get; set; } = new List<District>();

        public List<(Entity, int)> Embezzlements = new List<(Entity, int)>();

        public Structure Market;
        public Structure Prism;

        public bool Active { get; set; } = false;
        public bool IsSavingUpToSettle { get; set; } = false;

        public List<Structure> AllStructures { get; set; } = new List<Structure>();

        public List<string> PrimaryLightingStyles { get; set; } = new List<string>();

        public List<string> LocationHistoricalEvents { get; set; } = new List<string>();
        public int Wealth { get; set; } // value is measured in Shobes, an arbitrary unit
        public Civilization HomeCivilization { get; set; }
        public int ColonizationDesire { get; set; }
        public int MaxColonizationDesire { get; set; }

        public bool IsCapital = false;

        public List<Architect> DebtShibas = new List<Architect>();

        public List<Group> TradersAtThisLocation { get; set; } = new List<Group>();
        public List<Group> TradersAtThisLocationToRemove { get; set; } = new List<Group>();
        public List<Group> TradersAtThisLocationToAdd { get; set; } = new List<Group>();


        public List<Group> GroupsAtThisLocation { get; set; } = new List<Group>();
        public List<Group> GroupsAtThisLocationToRemove { get; set; } = new List<Group>();

        public List<Object> UnplacedArtifacts { get; set; } = new List<Object>();

        public int X { get; set; }
        public int Z { get; set; }

        public Entity Government { get; set; }

        public Region Region;

        // THESE VALUES ARE USED IF THE LOCATION IS LOADED

        public bool IsLoaded { get; set; }
        public bool HasBeenLoadedEver { get; set; } = false;

        public int TruePopulation()
        {
            int Population = 0;

            foreach(District d in Districts)
            {
                Population = Population + d.UnplacedPopulation;
                foreach(Architect a in d.Architects)
                {
                    if((a.Group != null && a.Group.Type != "trade") || a.Group == null)
                    {
                        Population++;
                    }
                }
            }

            return Population;
        }


        
        public Location(string type, Race primaryrace, int population, int wealth, int colonizationDesire, int x, int z, Civilization HomeCiv, Region r)
        {
            Region = r;
            Type = type;

            if (Type == "spire")
            {
                //if you want a different name scheme use this but whatever for now works
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }
            else
            {
                Name = r.World.GenerateUniqueName("1S" + (Game1.r.Next(2, 4)) + "s1w", this);
            }

            PrimaryRace = primaryrace;
            Wealth = wealth;
            ColonizationDesire = colonizationDesire;
            MaxColonizationDesire = colonizationDesire;
            X = x;
            Z = z;
            HomeCivilization = HomeCiv;
            Districts.Add(new District(true, this, population));

            if(Game1.GameWorld.SettlementTypes.Contains(this.Type))
            {
                Explored = true;
            }

            PrimaryLightingStyles = new List<string>
            {
                Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)]
            };
        }
        public Location()
        {

        }
        public void Load()
        {
            IsLoaded = true;

            List<Architect> PlacedArchitects = new List<Architect>();

            int TotalArchitects = 0;

            foreach (District d in Districts)
            {
                foreach (Architect a in d.Architects)
                {
                    TotalArchitects++;
                }
            }

            Game1.LoadedArchitects = new List<Architect>();

            //create structure details

            foreach (District d in Districts)
            {
                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                {
                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                    {
                        if (!HasBeenLoadedEver)
                        {
                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ*7].Structures)
                            {
                                //add rooms, objects, doors to the structure

                                Room CoreRoom = new Room(s, new List<Object>(), new List<Architect>(), new List<Architect>());
                                s.Rooms.Add(CoreRoom);

                                int ExtraRoomCount;

                                if (s.Type == "house")
                                {
                                    ExtraRoomCount = Game1.r.Next(0, 4);
                                }
                                else if(s.Type == "spire")
                                {
                                    ExtraRoomCount = Game1.r.Next(10, 20);
                                }
                                else if (s.Type == "keep")
                                {
                                    ExtraRoomCount = Game1.r.Next(3, 7);
                                }
                                else if (s.Type == "tower")
                                {
                                    ExtraRoomCount = Game1.r.Next(10, 13);
                                }
                                else if (s.Type == "fortress")
                                {
                                    ExtraRoomCount = Game1.r.Next(10, 20);
                                }
                                else if (s.Type == "monument")
                                {
                                    ExtraRoomCount = Game1.r.Next(10, 20);
                                }
                                else if (s.Type == "outpost" || s.Type == "sanctum")
                                {
                                    ExtraRoomCount = Game1.r.Next(10, 20);
                                }
                                else
                                {
                                    ExtraRoomCount = Game1.r.Next(2, 2);
                                }

                                for (int i = 0; i < ExtraRoomCount; i++)
                                {
                                    s.Rooms.Add(new Room(s, new List<Object>(), new List<Architect>(), new List<Architect>()));
                                }

                                foreach (Room R in s.Rooms)
                                {
                                    R.PopulateRoom();
                                }

                                foreach(Object o in s.HistoricalObjects)
                                {
                                    s.Rooms[Game1.r.Next(s.Rooms.Count)].Objects.Add(o);
                                }
                                s.HistoricalObjects.Clear();
                            }
                        }


                        foreach(Structure s in d.DistrictMap[DistrictX + DistrictZ*7].Structures)
                        {
                            if(s.Type == "market")
                            {
                                if(DebtShibas.Count == 0)
                                {
                                    int shibas = Game1.r.Next(4, 8);

                                    for (int i = 0; i < shibas; i++)
                                    {
                                        Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], Region.World.GetRace("debtshiba"), Game1.r.Next(9999999), "debtshiba", new List<Object>(), this, d, d.DistrictMap[DistrictX + DistrictZ * 7], "", 4);
                                        a.Name = this.Region.World.GenerateUniqueArchitectName(a);
                                        a.HomeStructure = Market;
                                        DebtShibas.Add(a);
                                        

                                    }
                                }
                                d.DistrictMap[DistrictX + DistrictZ * 7].Architects.AddRange(DebtShibas);
                            }
                        }
                    }
                }

                //turn population into architects

                for (int i = 0; i < d.UnplacedPopulation; i++)
                {
                    string Sex = "";

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        Sex = "male";
                    }
                    else
                    {
                        Sex = "female";
                    }

                    string Role = Game1.WeightedRandomNormalProfessions[Game1.r.Next(Game1.WeightedRandomNormalProfessions.Count)];
                    Race Race;

                    if (Game1.r.Next(1, 20) == 1 && (PrimaryRace.Name != "shade" && PrimaryRace.Name != "photonexus"))
                    {
                        Race = Region.World.HumanoidRaces[Game1.r.Next(Region.World.HumanoidRaces.Count)];
                    }
                    else
                    {
                        Race = PrimaryRace;
                    }

                    string Destiny = "";
                    int DestinyDecider = Game1.r.Next(1, 5000);

                    if (DestinyDecider < 3)
                    {
                        Destiny = "wizard";
                    }
                    else if (DestinyDecider < 5 && Race == Region.World.GetRace("nightfell"))
                    {
                        Destiny = "warlock";
                    }
                    else if (DestinyDecider < 7 && Race == Region.World.GetRace("luminarch"))
                    {
                        Destiny = "sorcerer";
                    }
                    else if (DestinyDecider < 8)
                    {
                        Destiny = "parasite";
                    }

                    Architect a = new Architect("", Sex, Race, Game1.r.Next(14, 90), Role, new List<Object>(), this, null, null, Destiny, 1);
                    Name = Region.World.GenerateUniqueArchitectName(a);
                    a.Name = Name;

                    //find a place

                    d.Architects.Add(a);
                    Game1.LoadedArchitects.Add(a);
                }

                d.UnplacedPopulation = 0;


                //place/replace architects

                foreach (Architect a in d.Architects)
                {
                    if(Game1.GamePlayerParty.Architects.Contains(a))
                    {
                        continue;
                    }
                    a.Loaded = true;

                    a.UpdateNames();

                    //find a stupid structure

                    List<Structure> PossibleStructures = new List<Structure>();

                    if(DebtShibas.Contains(a))
                    {
                        Market.Block.Architects.Add(a);
                        a.Block = Market.Block;
                        a.District = d;
                        continue;
                    }

                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                    {
                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                        {
                            if (a.Group == Government || a == Government)
                            {
                                foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                {
                                    if (s.Type == "prism")
                                    {
                                        PossibleStructures.Add(s);
                                    }
                                }
                            }
                            else
                            {
                                foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                {
                                    if (s.Type == Game1.ConvertProfessionToBuilding[a.Profession])
                                    {
                                        PossibleStructures.Add(s);
                                    }
                                }
                            }
                        }
                    }

                    if (PossibleStructures.Count > 0)
                    {
                        foreach (Structure s in PossibleStructures)
                        {
                            if (s.Owner == a.Group || s.Owner == a)
                            {
                                a.Structure = s;
                                a.Room = s.Rooms[0];
                                a.Block = s.Block;
                                break;
                            }
                        }

                        if (a.Structure == null)
                        {
                            Room ArchRoom = PossibleStructures[Game1.r.Next(PossibleStructures.Count)].Rooms[0];
                            ArchRoom.Architects.Add(a);
                            a.Room = ArchRoom;
                            a.Block = ArchRoom.Structure.Block;
                            a.Structure = ArchRoom.Structure;
                            PlacedArchitects.Add(a);
                        }
                    }
                    else
                    {
                        Block b = d.DistrictMap[Game1.r.Next(0, 49)];
                        b.Architects.Add(a);
                        a.Block = b;
                    }

                    a.District = d;
                    Game1.LoadedArchitects.Add(a);
                }

                Game1.LoadedArchitects.AddRange(Game1.GamePlayerParty.Architects);

                
                d.Architects = new List<Architect>();
            }

            //traders

            foreach(Group g in TradersAtThisLocation)
            {
                foreach(Architect a in g.Architects)
                {
                    a.IsLoadedTrader = true;

                    if(Market != null)
                    {
                        a.Room = Market.Rooms[0];
                        a.Structure = Market;
                        a.Block = Market.Block;
                        Market.Rooms[0].Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                    else
                    {
                        //for now lets just say they dont exist, maybe they'll just forget...  
                    }
                }
                foreach (Object o in g.CaravanItems)
                {
                    if (Market != null)
                    {
                        Market.Rooms[Game1.r.Next(Market.Rooms.Count)].Objects.Add(o);
                    }
                }
            }

            //place all the nonesneseneneense objects they crafted lmao

            foreach(Object o in GeneralItemsWeHave)
            {
                if (Game1.r.Next(1, 3) == 1 || Market == null)
                {
                    Structure s = AllStructures[Game1.r.Next(AllStructures.Count)];
                    Room R = s.Rooms[Game1.r.Next(s.Rooms.Count)];
                    R.Objects.Add(o);
                }
                else
                {
                    Structure s = Market;
                    Room R = s.Rooms[Game1.r.Next(s.Rooms.Count)];
                    R.Objects.Add(o);
                }
            }
            GeneralItemsWeHave.Clear();

            //THEN say the place has been loaded, NOT after one district

            Game1.TicksSinceLoad = 0;

            HasBeenLoadedEver = true;

        }
        public void Unload()
        {
            IsLoaded = false;

            int TotalArchitects = 0;

            Game1.LoadedArchitects = new List<Architect>();

            //remove architects
            //also round up the architects and calculate the population

            foreach (District d in Districts)
            {
                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                {
                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                    {
                        foreach(Architect a in d.DistrictMap[DistrictX + DistrictZ * 7].Architects)
                        {
                            if(!Game1.GamePlayerParty.Architects.Contains(a))
                            {
                                if (!a.IsLoadedTrader)
                                {
                                    if (a.Race == Game1.GameWorld.GetRace("debtshiba"))
                                    {
                                        d.Location.DebtShibas.Add(a);
                                        a.Task = "";
                                        a.Loaded = false;
                                    }
                                    else
                                    {
                                        d.Architects.Add(a);
                                        a.Task = "";
                                        a.Loaded = false;
                                        TotalArchitects++;
                                    }
                                }
                                else
                                {
                                    a.IsLoadedTrader = false;
                                }
                            }
                        }
                        d.DistrictMap[DistrictX + DistrictZ*7].Architects.Clear();

                        foreach (Object o in d.DistrictMap[DistrictX + DistrictZ * 7].Objects)
                        {
                            if (o.IsGeneralGood)
                            {
                                if(o.Owner is Group && ((Group)o.Owner).Type == "trade")
                                {
                                    d.DistrictMap[DistrictX + DistrictZ * 7].Objects.Remove(o);
                                    ((Group)o.Owner).CaravanItems.Add(o);
                                }
                                else
                                {
                                    d.DistrictMap[DistrictX + DistrictZ * 7].Objects.Remove(o);
                                    GeneralItemsWeHave.Add(o);
                                }
                            }
                        }

                        foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                        {
                            foreach(Room r in s.Rooms)
                            {
                                List<Architect> ArchitectsToRemove = new List<Architect>();
                                foreach (Architect a in r.Architects)
                                {
                                    if (!Game1.GamePlayerParty.Architects.Contains(a))
                                    {
                                        if (!a.IsLoadedTrader)
                                        {
                                            if (a.Race == Game1.GameWorld.GetRace("debtshiba"))
                                            {
                                                d.Architects.Add(a);
                                                a.Task = "";
                                                a.Loaded = false;
                                                TotalArchitects++;
                                            }
                                            else
                                            {
                                                d.Location.DebtShibas.Add(a);
                                                a.Task = "";
                                                a.Loaded = false;
                                                TotalArchitects++;
                                            }
                                        }
                                        else
                                        {
                                            a.IsLoadedTrader = false;
                                        }
                                    }
                                }
                                r.Architects.Clear();


                                foreach(Object o in r.Objects)
                                {
                                    if(o.IsGeneralGood)
                                    {
                                        if (o.Owner is Group && ((Group)o.Owner).Type == "trade")
                                        {
                                            r.ObjectsToRemove.Add(o);
                                            ((Group)o.Owner).CaravanItems.Add(o);
                                        }
                                        else
                                        {
                                            r.ObjectsToRemove.Add(o);
                                            GeneralItemsWeHave.Add(o);
                                        }
                                    }
                                }
                                foreach(Object o in r.ObjectsToRemove)
                                {
                                    r.Objects.Remove(o);
                                }
                                r.ObjectsToRemove = new List<Object>();
                            }
                        }
                    }
                }
            }
        }
    }
}

