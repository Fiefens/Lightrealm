using Lightrealm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lightrealm
{
    [Serializable]
    public class District : Entity
    {
        public bool IsPrimary { get; set; } = false;
        public Location Location { get; set; }

        public List<Architect> AllArchitectsInDistrict()
        {
            List<Architect> Architects = new List<Architect>();
            for(int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    Architects.AddRange(DistrictMap[i + j*7].Architects);
                }
            }
            return Architects;
        }

        public int UnplacedPopulation { get; set; }
        public List<Architect> Architects { get; set; } = new List<Architect>();
        public List<Architect> ArchitectsToRemove { get; set; } = new List<Architect>();
        public List<Architect> ArchitectsToAdd { get; set; } = new List<Architect>();

        public List<Object> Objects { get; set; } = new List<Object>();

        public List<Object> GeneralItemsWeHave = new List<Object>();
        public bool IsLoaded { get; set; }
        public bool HasBeenLoadedEver { get; set; } = false;


        public Block[] DistrictMap { get; set; } = new Block[49];

        public string Industry = "";

        public bool HasBeenLoaded { get; set; }

        public District(bool isPrimary, Location l, int unplacedPopulation)
        {
            Location = l;
            Name = Location.Region.World.GenerateUniqueName("1S" + (Game1.r.Next(3, 4) - 1) + "s", this);
            ReferredToNames.Add(Name);
            IsPrimary = isPrimary;

            string dockside = Location.Dockside;

            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    DistrictMap[DistrictX + DistrictZ * 7] = new Block(DistrictX, DistrictZ, this);

                    if (Location.Type == "preserve")
                    {
                        DistrictMap[DistrictX + DistrictZ * 7].Biome = new List<string>() { "water", "taiga", "forest", "plains" }[Game1.r.Next(4)];
                    }
                    else if (Location.Type == "cove")
                    {
                        if (dockside == "north" && DistrictZ < 2)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                        }
                        else if (dockside == "north" && DistrictZ < 4)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                        else if (dockside == "north")
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                        }
                        else if (dockside == "south" && DistrictZ >= 5)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                        }
                        else if (dockside == "south" && DistrictZ >= 3)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                        else if (dockside == "south")
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                        }
                        else if (dockside == "west" && DistrictX < 2)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                        }
                        else if (dockside == "west" && DistrictX < 4)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                        else if (dockside == "west")
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                        }
                        else if (dockside == "east" && DistrictX >= 5)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                        }
                        else if (dockside == "east" && DistrictX >= 3)
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                        else if (dockside == "east")
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                        }
                        else
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                    } 
                }
            }

            UnplacedPopulation = unplacedPopulation;
            Industry = Game1.Industries[Game1.r.Next(Game1.Industries.Count)];
        }


        public District()
        {

        }

        public int Population()
        {
            return Architects.Count + UnplacedPopulation;
        }

        public void SupplyLocation(int Intensity)
        {
            if (Industry == null)
            {
                return;
            }

            string DecidedProduction = Industry;

            // 40% chance to make something different
            if (Game1.r.Next(1, 6) <= 2)
            {
                DecidedProduction = Game1.Industries[Game1.r.Next(Game1.Industries.Count)];
            }

            List<Object> itemsToBeAdded = GenerateItems(DecidedProduction, Intensity);

            // Adding items and increasing crafts within the same method
            foreach (var item in itemsToBeAdded)
            {
                if (Game1.r.Next(1, 3) == 1)
                {
                    Location.Districts[0].GeneralItemsWeHave.Add(item);
                }
                else
                {
                    GeneralItemsWeHave.Add(item);
                }
            }

            Location.HomeCivilization.World.TotalCrafts += itemsToBeAdded.Count;
        }


        public List<Object> GenerateItems(string industry, int intensity)
        {
            List<Object> Objects = new List<Object>();

            for(int x = 0; x < intensity; x++)
            {
                switch (industry)
                {
                    case "textiles":
                        if (Game1.r.Next(0, 30) == 0) // General chance for any clothing item
                        {
                            // Instead of directly adding objects, we'll decide which to add based on a random selection
                            int clothingPiece = Game1.r.Next(1, 10); // Adjust the range based on the number of clothing pieces available

                            switch (clothingPiece)
                            {
                                case 1:
                                    // Adding a shirt with sleeves, could be short or long based on additional randomness
                                    Objects.Add(new Object(null, Game1.r.Next(0, 2) == 0 ? "shortsleeve shirt" : "longsleeve shirt", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 2:
                                    // Pants
                                    Objects.Add(new Object(null, "pants", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 3:
                                    // Skirt
                                    Objects.Add(new Object(null, "skirt", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 4:
                                    // Gloves, considering handedness
                                    Objects.Add(new Object(null, "left glove", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    Objects.Add(new Object(null, "right glove", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 5:
                                    // Boots, considering handedness
                                    Objects.Add(new Object(null, "left boot", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    Objects.Add(new Object(null, "right boot", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 6:
                                    // Hood
                                    Objects.Add(new Object(null, "hood", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 7:
                                    // Cape
                                    Objects.Add(new Object(null, "cape", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 8:
                                    // Amulet, as a decorative item
                                    Objects.Add(new Object(null, "amulet", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 9:
                                    // Flair, another decorative item
                                    Objects.Add(new Object(null, "flair", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case > 19:
                                    // A cloth roll.
                                    Objects.Add(new Object(null, "bolt", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                    // Add cases for any other specific clothing items listed
                            }
                        }
                        break;

                    case "spices":
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            Object saltPouch = new Object(null, "salt pouch", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null);
                            saltPouch.ContainedObjects.Add(new Object(null, "stone", new List<Material>() { new Material("salt", "stone", 1, 1, "white") }, null));
                            Objects.Add(saltPouch);
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            Object pepperPouch = new Object(null, "pepper pouch", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null);
                            pepperPouch.ContainedObjects.Add(new Object(null, "spice", new List<Material>() { new Material("pepper", "plant", 1, 1, "black") }, null));
                            Objects.Add(pepperPouch);
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            Object paprikaPouch = new Object(null, "paprika pouch", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null);
                            paprikaPouch.ContainedObjects.Add(new Object(null, "spice", new List<Material>() { new Material("paprika", "plant", 1, 1, "maroon") }, null));
                            Objects.Add(paprikaPouch);
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            Object isodustPouch = new Object(null, "isodust pouch", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null);
                            isodustPouch.ContainedObjects.Add(new Object(null, "spice", new List<Material>() { new Material("isodust", "plant", 1, 1, "purple") }, null));
                            Objects.Add(isodustPouch);
                        }
                        break;

                    case "metal":
                        if (Game1.r.Next(0, 2) == 0)
                            Objects.Add(new Object(null, "bar", new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                        break;

                    case "jewelry":
                        if (Game1.r.Next(0, 3) == 0)
                            Objects.Add(new Object(null, "gem", new List<Material>() { Location.HomeCivilization.CulturalGemstone }, null));
                        break;

                    case "tools":
                        if (Game1.r.Next(0, 5) == 0)
                            Objects.Add(new Object(null, "pickaxe", new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                        if (Game1.r.Next(0, 5) == 0)
                            Objects.Add(new Object(null, "scythe", new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                        if (Game1.r.Next(0, 5) == 0)
                            Objects.Add(new Object(null, "axe", new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                        break;

                    case "military":
                        string[] weaponTypes = { "shortsword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };
                        foreach (string weapon in weaponTypes)
                            if (Game1.r.Next(0, 12) == 0) // Random chance to add a weapon
                                Objects.Add(new Object(null, weapon, new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));

                        string[] armorTypes = { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                        foreach (string armor in armorTypes)
                        {
                            if (Game1.r.Next(0, 12) == 0) // Random chance to add armor
                            {
                                if (armor == "gauntlet" || armor == "boot")
                                {
                                    // Create and add left and right variants for gauntlets or boots
                                    Objects.Add(new Object(null, "left " + armor, new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                                    Objects.Add(new Object(null, "right " + armor, new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                                }
                                else
                                {
                                    // For other armor types, just create and add one object
                                    Objects.Add(new Object(null, armor, new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                                }
                            }
                        }
                        break;

                    case "coffee":
                        for (int i = Game1.r.Next(0, 3); i != 0; i--)
                        {
                            Object coffeeCrate = new Object(null, "coffee crate", new List<Material>() { Location.HomeCivilization.CulturalWood }, null);
                            for (int j = Game1.r.Next(10, 15); j != 0; j--)
                                coffeeCrate.ContainedObjects.Add(new Object(null, "spice", new List<Material>() { Location.Region.World.Coffee }, null));
                            Objects.Add(coffeeCrate);
                        }
                        break;

                    case "tea":
                        for (int i = Game1.r.Next(0, 3); i != 0; i--)
                        {
                            Object teaCrate = new Object(null, "tea crate", new List<Material>() { Location.HomeCivilization.CulturalWood }, null);
                            for (int j = Game1.r.Next(10, 15); j != 0; j--)
                                teaCrate.ContainedObjects.Add(new Object(null, "spice", new List<Material>() { Location.Region.World.Tea }, null));
                            Objects.Add(teaCrate);
                        }
                        break;

                    case "wood":
                        for (int i = Game1.r.Next(0, 4); i != 0; i--)
                            Objects.Add(new Object(null, "log", new List<Material> { Location.HomeCivilization.CulturalWood }, null));
                        break;

                    case "ceramics":
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small urn", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big urn", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small pot", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big pot", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small mug", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big mug", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small bowl", new List<Material>() { Game1.GameWorld.Clay }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big bowl", new List<Material>() { Game1.GameWorld.Clay }, null));
                        break;

                    case "glassmaking":
                        if (Game1.r.Next(0, 2) == 0)
                            Objects.Add(new Object(null, "sheet", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small mug", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big mug", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small chalice", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big chalice", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small bowl", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big bowl", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "small cup", new List<Material>() { Game1.GameWorld.Glass }, null));
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "big cup", new List<Material>() { Game1.GameWorld.Glass }, null));
                        break;

                    case "dye":
                        if (Game1.r.Next(0, 2) == 0)
                        {
                            string DyeColor = Game1.GetFamilyColors(Location.HomeCivilization.Color)[Game1.r.Next(3)];
                            Object dyeBottle = new Object(null, "bottle", new List<Material>() { Game1.GameWorld.Glass }, null);
                            dyeBottle.ContainedObjects.Add(new Object(null, "dye", new List<Material>() { Game1.GameWorld.MaterialsFromColors[DyeColor][Game1.r.Next(3)] }, null));
                            Objects.Add(dyeBottle);
                        }
                        break;

                    case "waspkeeping":
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            Object honeyJar = new Object(null, "jar", new List<Material>() { Game1.GameWorld.Glass }, null);
                            honeyJar.ContainedObjects.Add(new Object(null, "portion", new List<Material>() { Game1.GameWorld.Honey }, null));
                            Objects.Add(honeyJar);
                        }
                        if (Game1.r.Next(0, 10) == 0)
                            Objects.Add(new Object(null, "tablet", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                        if (Game1.r.Next(0, 5) == 0)
                            Objects.Add(new Object(null, "candle", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                        if (Game1.r.Next(0, 2) == 0)
                            Objects.Add(new Object(null, "cube", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                        break;

                    case "fuel":
                        for (int i = Game1.r.Next(0, 9); i != 0; i--)
                            Objects.Add(new Object(null, "fragment", new List<Material> { Game1.GameWorld.Vitalium }, null));
                        break;

                    case "masonry":
                        for (int i = Game1.r.Next(0, 6); i != 0; i--)
                            Objects.Add(new Object(null, "brick", new List<Material> { Location.HomeCivilization.CulturalStone }, null));
                        break;

                    case "healing":
                        if (Game1.r.Next(0, 5) == 0) // General chance for any healing item
                        {
                            // Instead of directly adding objects, we'll decide which to add based on a random selection
                            int healingItem = Game1.r.Next(1, 4); // Adjust the range based on the number of healing items available

                            switch (healingItem)
                            {
                                case 1:
                                    // Adding a salve
                                    Objects.Add(new Object(null, "salve", new List<Material>() { Location.Region.HarvestableFiber }, null));
                                    break;
                                case 2:
                                    // Adding a bandage
                                    Objects.Add(new Object(null, "bandage", new List<Material>() { Location.HomeCivilization.CulturalCloth }, null));
                                    break;
                                case 3:
                                    // Adding a vitality vial
                                    Objects.Add(new Object(null, "vial", new List<Material>() { Game1.GameWorld.Glass, Game1.GameWorld.Vitalium }, null));
                                    break;
                            }
                        }
                        break;

                    default:
                        // Handle default case
                        break;
                }
            }

            foreach (Object o in Objects)
            {
                o.IsGeneralGood = true;
            }

            return Objects;
        }


        public void Load()
        {
            IsLoaded = true;

            Game1.AllSubjects = Game1.CollectAllSubjects(Game1.MostRecentPartyTurnArchitect, "none");

            List<Architect> allArchitects = new List<Architect>();
            allArchitects.AddRange(Architects);
            List<Structure> structures = new List<Structure>();

            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    structures.AddRange(DistrictMap[x + z * 7].Structures);
                }
            }

            if (!HasBeenLoadedEver)
            {
                List<Structure> allDistrictStructures = new List<Structure>();

                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                {
                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                    {
                        allDistrictStructures.AddRange(DistrictMap[DistrictX + DistrictZ * 7].Structures);
                    }
                }

                foreach (Structure s in Location.AllStructures)
                {
                    if (s.Block.District == this && !allDistrictStructures.Contains(s))
                    {
                        allDistrictStructures.Add(s);
                    }
                }

                if(!allDistrictStructures.Contains(Location.Prism) && Location.Prism != null)
                {
                    //idk how this is happenign btu whaverver

                    allDistrictStructures.Add(Location.Prism);
                }

                foreach (Structure s in allDistrictStructures)
                {
                    Room coreRoom = new Room(s, new List<Object>(), new List<Architect>(), new List<Architect>());
                    s.Rooms.Add(coreRoom);

                    string Layout = s.Type;

                    if(Location.Layout != null && Location.Layout != "")
                    {
                        Layout = Location.Layout;
                    }

                    int extraRoomCount = Layout switch
                    {
                        "house" or "ship" or "cove" or "mound" or "monastery" => Game1.r.Next(0, 4),
                        "spire" or "archway" or "hallway" or "fortress" or "monument" or "toroid" or "towers" or "outpost" or "pyramid" or "sanctum" => Game1.r.Next(10, 20),
                        "keep" => Game1.r.Next(3, 7),
                        "core" or "heart" or "stronghold" => Game1.r.Next(20, 30),
                        "tower" or "commune" => Game1.r.Next(10, 13),
                        _ => 2,
                    };

                    for (int i = 0; i < extraRoomCount; i++)
                    {
                        s.Rooms.Add(new Room(s, new List<Object>(), new List<Architect>(), new List<Architect>()));
                    }

                    foreach (Room r in s.Rooms)
                    {
                        r.PopulateRoom();
                    }

                    foreach (Object o in s.HistoricalObjects)
                    {
                        s.Rooms[Game1.r.Next(s.Rooms.Count)].Objects.Add(o);
                    }
                    s.HistoricalObjects.Clear();
                }
            }

            if (Location.Market != null)
            {
                if (Location.DebtShibas.Count == 0)
                {
                    int shibas = Game1.r.Next(4, 8);
                    for (int i = 0; i < shibas; i++)
                    {
                        Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], Location.Region.World.GetRace("debtshiba"), Game1.r.Next(9999999), "debtshiba", new List<Object>(), Location, this, Location.Market.Block, "", 4);
                        a.Name = Location.Region.World.GenerateUniqueArchitectName(a);
                        a.HomeStructure = Location.Market;
                        Location.DebtShibas.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                }

                allArchitects.AddRange(Location.DebtShibas);
                Location.Market.Block.Architects.AddRange(Location.DebtShibas);
            }

            for (int i = 0; i < UnplacedPopulation; i++)
            {
                string sex = Game1.r.Next(1, 3) == 1 ? "male" : "female";
                string role = Game1.WeightedRandomNormalProfessions[Game1.r.Next(Game1.WeightedRandomNormalProfessions.Count)];
                Race race;

                if (Location.PrimaryRace.Name == "luminarch" || Location.PrimaryRace.Name == "nightfell" || Location.PrimaryRace.Name == "archaix")
                {
                    if (Game1.r.Next(1, 101) <= 95) // 95% chance
                    {
                        race = Location.PrimaryRace;
                    }
                    else
                    {
                        // Pick one of the other two humanoid races
                        List<Race> otherHumanoidRaces = Location.Region.World.HumanoidRaces.Where(r => r.Name != Location.PrimaryRace.Name && (r.Name == "luminarch" || r.Name == "nightfell" || r.Name == "archaix")).ToList();
                        race = otherHumanoidRaces[Game1.r.Next(otherHumanoidRaces.Count)];
                    }
                }
                else if (Location.PrimaryRace.Name == "shade" || Location.PrimaryRace.Name == "isofractal" || Location.PrimaryRace.Name == "photonexus")
                {
                    race = Location.PrimaryRace;
                }
                else
                {
                    // 3:3:1 bias towards luminarchs and nightfells over archaix
                    int bias = Game1.r.Next(7); // 0-6

                    if (bias < 3)
                    {
                        race = Location.Region.World.HumanoidRaces.First(r => r.Name == "luminarch");
                    }
                    else if (bias < 6)
                    {
                        race = Location.Region.World.HumanoidRaces.First(r => r.Name == "nightfell");
                    }
                    else
                    {
                        race = Location.Region.World.HumanoidRaces.First(r => r.Name == "archaix");
                    }
                }


                string destiny = Game1.r.Next(1, 5000) switch
                {
                    < 3 => "wizard",
                    < 5 when race == Location.Region.World.GetRace("nightfell") => "warlock",
                    < 7 when race == Location.Region.World.GetRace("luminarch") => "sorcerer",
                    < 8 => "parasite",
                    _ => ""
                };

                Architect a = new Architect("", sex, race, Game1.r.Next(14, 90), role, new List<Object>(), Location, this, null, destiny, 1);
                a.Name = Location.Region.World.GenerateUniqueArchitectName(a);
                allArchitects.Add(a);
            }

            UnplacedPopulation = 0;

            if (Location.Market != null && Location.Market.Block.District == this)
            {
                foreach (Group g in Location.TradersAtThisLocation)
                {
                    foreach (Architect a in g.Architects)
                    {
                        if (!Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            Location.Market.Rooms[0].Architects.Add(a);
                            a.Room = Location.Market.Rooms[0];
                            a.Block = a.Room.Structure.Block;
                            Game1.LoadedArchitects.Add(a);
                        }
                    }
                }
            }

            foreach (Architect a in allArchitects)
            {
                if (!Game1.GamePlayerParty.Architects.Contains(a) && !Location.DebtShibas.Contains(a))
                {
                    a.Loaded = true;
                    a.UpdateNames();

                    List<Structure> possibleStructures = a.Bound
                        ? new List<Structure> { this.Location.AllStructures.FirstOrDefault(s => s.Block.District == this) }
                        : GetPossibleStructures(a);

                    if (possibleStructures.Count > 0)
                    {
                        Structure chosenStructure = possibleStructures[Game1.r.Next(possibleStructures.Count)];
                        Room chosenRoom = chosenStructure.Rooms[0];
                        chosenRoom.Architects.Add(a);
                        a.Room = chosenRoom;
                        a.Block = chosenRoom.Structure.Block;
                        a.Structure = chosenRoom.Structure;
                    }
                    else
                    {
                        Block b = DistrictMap[Game1.r.Next(0, 49)];
                        b.Architects.Add(a);
                        a.Block = b;
                    }

                    a.District = this;
                    a.UpdateNames();
                    Game1.LoadedArchitects.Add(a);
                }

                bool isCoolProfession = a.Profession == "artist" ||
                             a.Profession == "curator" ||
                             a.Profession == "perfectionist" ||
                             a.Profession == "manager" ||
                             a.Profession == "brute" ||
                             a.Profession == "cluster";

                if(!isCoolProfession)
                {
                    if (a.Race.Name == "photonexus")
                    {
                        a.Profession = Game1.r.Next(100) < 20 ? "curator" : "artist";
                    }
                    else if (a.Race.Name == "isofractal")
                    {
                        a.Profession = Game1.r.Next(100) < 20 ? "manager" : "perfectionist";
                    }
                    else if (a.Race.Name == "shade")
                    {
                        a.Profession = Game1.r.Next(100) < 20 ? "cluster" : "brute";
                    }
                }
            }
            

            if (Location.Districts.Count == 1 && Location.AllStructures.Count == 1)
            {
                Structure structureInSameDistrict = Location.AllStructures.FirstOrDefault(s => s.Block.District == this);
                if (structureInSameDistrict != null)
                {
                    for (int i = Location.GuardiansInNetwork; i > 0; i--)
                    {
                        Block b = structureInSameDistrict.Block;
                        Race race = Location.Type == "sanctum"
                            ? Game1.GameWorld.ConstructRaces[Game1.r.Next(Game1.GameWorld.ConstructRaces.Count)]
                            : Location.GuardianType;

                        Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], race, 10, "construct", new List<Object>(), Location, this, b, "", 5);
                        a.Inventory.Add(Game1.GameWorld.MagicalSuperLoot(Game1.r.Next(3, 7)));
                        a.Name = Location.Region.World.GenerateUniqueArchitectName(a);
                        a.Room = structureInSameDistrict.Rooms[Game1.r.Next(structureInSameDistrict.Rooms.Count)];
                        a.Block = a.Room.Structure.Block;
                        a.HomeLocation = Location;
                        a.Room.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                }
            }


            foreach (Object o in GeneralItemsWeHave)
            {
                Room targetRoom = Location.Market != null && Location.Market.Block.District == this && Game1.r.Next(1, 3) == 1 ? Location.Market.Rooms[0] : GetRandomRoom(structures);
                targetRoom.Objects.Add(o);
                o.UpdateNames();
            }

            Game1.LoadedArchitects.AddRange(Game1.GamePlayerParty.Architects);


            //go over everything and fix the object jstuff so that its properly assigned to wherev er the hell its supposed to go.


            bool EverythingBelongsToTheQueen = false;

            if(Location.Type == "core" || Location.Type == "heart")
            {
                EverythingBelongsToTheQueen = true;
            }

            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    foreach (Architect a in DistrictMap[x + z * 7].Architects)
                    {
                        a.Block = DistrictMap[x + z * 7];
                        Game1.LoadedArchitects.Add(a);
                    }
                    foreach (Object o in DistrictMap[x + z * 7].Objects)
                    {
                        o.Block = DistrictMap[x + z * 7];
                        if(EverythingBelongsToTheQueen)
                        {
                            o.Owner = Location.Government;
                        }
                    }

                    foreach (Structure s in DistrictMap[x + z * 7].Structures)
                    {
                        foreach (Room r in s.Rooms)
                        {
                            foreach (Architect a in r.Architects)
                            {
                                a.Block = DistrictMap[x + z * 7];
                                Game1.LoadedArchitects.Add(a);
                                a.Room = r;
                            }
                            foreach (Object o in r.Objects)
                            {
                                o.Block = DistrictMap[x + z * 7];
                                o.Room = r;
                                if (EverythingBelongsToTheQueen)
                                {
                                    o.Owner = Location.Government;
                                }
                            }
                        }
                    }
                }
            }

            foreach(Architect a in Game1.LoadedArchitects)
            {
                if(a.Profession == "soverign" || a.Profession == "heart")
                {
                    a.Task = "sentinel";
                    a.CyclesLeftInTask = 99999;

                    if(a.Room != null)
                    {
                        a.Room.Architects.Remove(a);
                    }
                    a.Block.Architects.Remove(a);

                    a.Room = Location.Prism.Rooms[0];
                    a.Block = Location.Prism.Block;
                    a.Room.Architects.Add(a);
                }
            }



            Architects.Clear();
            HasBeenLoadedEver = true;
            Game1.TicksSinceLoad = 0;


            //purge the duplicates

            Game1.LoadedArchitects = Game1.LoadedArchitects.Distinct().ToList();


            List<Structure> GetPossibleStructures(Architect a)
            {
                List<Structure> possibleStructures = new List<Structure>();
                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                {
                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                    {
                        if(Game1.ConvertProfessionToBuilding.ContainsKey(a.Profession))
                        {
                            possibleStructures.AddRange(DistrictMap[DistrictX + DistrictZ * 7].Structures.Where(s => s.Type == Game1.ConvertProfessionToBuilding[a.Profession]));
                        }
                        else
                        {
                            possibleStructures.AddRange(DistrictMap[DistrictX + DistrictZ * 7].Structures);
                        }
                    }
                }
                return possibleStructures;
            }

            Room GetRandomRoom(List<Structure> structures)
            {
                Structure randomStructure = structures[Game1.r.Next(structures.Count)];
                return randomStructure.Rooms[Game1.r.Next(randomStructure.Rooms.Count)];
            }
        }



        public void Unload()
        {
            IsLoaded = false;
            int TotalArchitects = 0;

            // Remove architects and round up the population
            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    foreach (Architect a in DistrictMap[DistrictX + DistrictZ * 7].Architects)
                    {
                        if (!Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            if (!a.IsLoadedTrader && !Game1.GameWorld.ConstructRaces.Contains(a.Race))
                            {
                                if (a.Race == Game1.GameWorld.GetRace("debtshiba"))
                                {
                                    Location.DebtShibas.Add(a);
                                    a.Task = "";
                                    a.Loaded = false;
                                }
                                else
                                {
                                    Architects.Add(a);
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
                    DistrictMap[DistrictX + DistrictZ * 7].Architects.Clear();

                    // Handle objects
                    foreach (Object o in DistrictMap[DistrictX + DistrictZ * 7].Objects)
                    {
                        if (o.IsGeneralGood)
                        {
                            if (o.Owner is Group && ((Group)o.Owner).Type == "trade")
                            {
                                ((Group)o.Owner).CaravanItems.Add(o);
                            }
                            else
                            {
                                GeneralItemsWeHave.Add(o);
                            }
                        }
                        else
                        {
                            if (Location.Prism != null)
                            {
                                Location.Prism.HistoricalObjects.Add(o);
                            }
                            else
                            {
                                Location.AllStructures[0].HistoricalObjects.Add(o);
                            }
                        }
                    }
                    DistrictMap[DistrictX + DistrictZ * 7].Objects.Clear();

                    // Handle structures and rooms
                    foreach (Structure s in DistrictMap[DistrictX + DistrictZ * 7].Structures)
                    {
                        foreach (Room r in s.Rooms)
                        {
                            List<Architect> ArchitectsToRemove = new List<Architect>();
                            foreach (Architect a in r.Architects)
                            {
                                if (!Game1.GamePlayerParty.Architects.Contains(a) && !a.IsLoadedTrader)
                                {
                                    if (a.Race == Game1.GameWorld.GetRace("debtshiba"))
                                    {
                                        Location.DebtShibas.Add(a);
                                        a.Task = "";
                                        a.Loaded = false;
                                        TotalArchitects++;
                                    }
                                    else if (!a.Race.Name.EndsWith("guardian"))
                                    {
                                        Architects.Add(a);
                                        a.Task = "";
                                        a.Loaded = false;
                                        TotalArchitects++;
                                    }
                                }
                            }
                            r.Architects.Clear();

                            // Handle objects in the room
                            foreach (Object o in r.Objects)
                            {
                                if (o.IsGeneralGood)
                                {
                                    if (o.Owner is Group && ((Group)o.Owner).Type == "trade")
                                    {
                                        ((Group)o.Owner).CaravanItems.Add(o);
                                    }
                                    else
                                    {
                                        GeneralItemsWeHave.Add(o);
                                    }
                                }
                                else
                                {
                                    r.Structure.HistoricalObjects.Add(o);
                                }
                            }
                            r.Objects.Clear();
                        }
                    }
                }
            }

            Game1.LoadedArchitects.Clear();
        }

    }
}
