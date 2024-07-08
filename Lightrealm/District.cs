using Lightrealm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lightrealm
{
    [Serializable]
    public class District : Entity
    {
        public bool IsPrimary { get; set; } = false;

        private int _locationId;

        
        public Location Location
        {
            get => EntityGet<Location>(_locationId);
            set => _locationId = value?.ID ?? 0;
        }

        public EntityList<Architect> AllArchitectsInDistrict()
        {
            EntityList<Architect> architects = new EntityList<Architect>();
            foreach (var block in DistrictMap)
            {
                architects.UnionWith(block.Architects);
            }
            return architects;
        }

        public int UnplacedPopulation { get; set; }

        public EntityHashSet<Architect> Architects { get; set; } = new EntityHashSet<Architect>();

        public EntityHashSet<Architect> ArchitectsToRemove { get; set; } = new EntityHashSet<Architect>();

        public EntityHashSet<Architect> ArchitectsToAdd { get; set; } = new EntityHashSet<Architect>();

        public List<string> GeneralItemsWeHave { get; set; } = new List<string>();

        public bool IsLoaded { get; set; } = false;
        public bool HasBeenLoadedEver { get; set; } = false;

        public EntityList<Block> DistrictMap = new EntityList<Block>();

        public string Industry { get; set; } = "";

        public bool HasBeenLoaded { get; set; } = false;

        public District(bool isPrimary, Location l, int unplacedPopulation)
        {
            Location = l;
            Name = Game1.GameWorld.GenerateUniqueName("1S" + (Game1.r.Next(3, 4) - 1) + "s", this);
            AddReferredToName(Name);
            IsPrimary = isPrimary;

            int index = 0;
            for (int z = 0; z < 7; z++) // z is south, so it increases as we go down the rows
            {
                for (int x = 0; x < 7; x++) // x is east, so it increases as we go across the columns
                {
                    DistrictMap.Add(new Block(x, z, this));
                    index++;
                }
            }

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
                        if (dockside == "north")
                        {
                            if (DistrictZ < 2)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                            }
                            else if (DistrictZ < 4)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                            }
                            else
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                            }
                        }
                        else if (dockside == "south")
                        {
                            if (DistrictZ >= 5)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                            }
                            else if (DistrictZ >= 3)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                            }
                            else
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                            }
                        }
                        else if (dockside == "west")
                        {
                            if (DistrictX < 2)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                            }
                            else if (DistrictX < 4)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                            }
                            else
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                            }
                        }
                        else if (dockside == "east")
                        {
                            if (DistrictX >= 5)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "desert";
                            }
                            else if (DistrictX >= 3)
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.r.Next(2) == 0 ? "desert" : "ocean";
                            }
                            else
                            {
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                            }
                        }
                        else
                        {
                            DistrictMap[DistrictX + DistrictZ * 7].Biome = "ocean";
                        }
                    }
                }
            }

            UnplacedPopulation = unplacedPopulation;
            Industry = Game1.Industries[Game1.r.Next(Game1.Industries.Count())];
        }



        public District()
        {

        }


        public int Population()
        {
            return Architects.Count() + UnplacedPopulation;
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
                DecidedProduction = Game1.Industries[Game1.r.Next(Game1.Industries.Count())];
            }

            List<string> itemsToBeAdded = GenerateItems(DecidedProduction, Intensity);

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

            Game1.GameWorld.TotalCrafts += itemsToBeAdded.Count();
        }

        public List<string> GenerateItems(string industry, int intensity)
        {
            Dictionary<string, int> itemDict = new Dictionary<string, int>();

            void AddOrUpdateItem(string itemType, List<string> materials, int count, string containedItems = "")
            {
                // Format the base key with type and materials
                string baseKey = $"{itemType},{string.Join(",", materials)}";

                // If contained items exist, add them to the key
                string key = baseKey;
                if (!string.IsNullOrEmpty(containedItems))
                {
                    key += $"&cont({containedItems})&";
                }

                // Check if the item already exists in the dictionary
                if (itemDict.TryGetValue(key, out int existingCount))
                {
                    // Update the count by adding the new count
                    itemDict[key] = existingCount + count;
                }
                else
                {
                    // Add the new item with the count
                    itemDict[key] = count;
                }
            }


            string GenerateContainedItems(List<string> items)
            {
                var containedDict = new Dictionary<string, (int Count, string Material)>();

                foreach (var item in items)
                {
                    // Split the item string to extract the type and count
                    string[] itemParts = item.Split(',');
                    string itemType = itemParts[0];
                    int itemCount = int.Parse(itemParts[1]);
                    string itemMaterial = itemParts[2];

                    // Create a key without the count
                    string key = $"{itemType},{itemMaterial}";

                    // Check if the item already exists in the dictionary
                    if (containedDict.ContainsKey(key))
                    {
                        // Update the count by adding the new count
                        containedDict[key] = (containedDict[key].Count + itemCount, itemMaterial);
                    }
                    else
                    {
                        // Add the new item with the count
                        containedDict[key] = (itemCount, itemMaterial);
                    }
                }

                // Format the contained items correctly with count as the second argument
                return string.Join(",", containedDict.Select(kvp => $"{kvp.Key.Split(',')[0]},{kvp.Value.Count},{kvp.Value.Material}"));
            }


            for (int x = 0; x < intensity; x++)
            {
                switch (industry)
                {
                    case "textiles":
                        if (Game1.r.Next(0, 30) == 0)
                        {
                            int clothingPiece = Game1.r.Next(1, 10);
                            List<string> materials = new List<string> { Location.HomeCivilization.CulturalCloth.Name };

                            switch (clothingPiece)
                            {
                                case 1:
                                    AddOrUpdateItem("shortsleeve shirt", materials, 1);
                                    break;
                                case 2:
                                    AddOrUpdateItem("pants", materials, 1);
                                    break;
                                case 3:
                                    AddOrUpdateItem("skirt", materials, 1);
                                    break;
                                case 4:
                                    AddOrUpdateItem("left glove", materials, 1);
                                    AddOrUpdateItem("right glove", materials, 1);
                                    break;
                                case 5:
                                    AddOrUpdateItem("left boot", materials, 1);
                                    AddOrUpdateItem("right boot", materials, 1);
                                    break;
                                case 6:
                                    AddOrUpdateItem("hood", materials, 1);
                                    break;
                                case 7:
                                    AddOrUpdateItem("cape", materials, 1);
                                    break;
                                case 8:
                                    AddOrUpdateItem("amulet", materials, 1);
                                    break;
                                case 9:
                                    AddOrUpdateItem("flair", materials, 1);
                                    break;
                                case > 19:
                                    AddOrUpdateItem("bolt", materials, 1);
                                    break;
                            }
                        }
                        break;

                    case "spices":
                        List<string> spiceMaterials = new List<string> { Location.HomeCivilization.CulturalCloth.Name };

                        void AddSpicePouch(string spiceType, string spiceMaterial)
                        {
                            List<string> containedItems = new List<string> { $"{spiceType},{Game1.r.Next(10, 20)},{spiceMaterial}" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem($"{spiceMaterial} pouch", spiceMaterials, 1, contained);
                        }

                        if (Game1.r.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "salt");
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "pepper");
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "paprika");
                        }
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "isodust");
                        }
                        break;

                    case "metal":
                        if (Game1.r.Next(0, 2) == 0)
                            AddOrUpdateItem("bar", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        break;

                    case "jewelry":
                        if (Game1.r.Next(0, 3) == 0)
                            AddOrUpdateItem("cut gem", new List<string> { Location.HomeCivilization.CulturalGemstone.Name }, 1);
                        break;

                    case "tools":
                        if (Game1.r.Next(0, 5) == 0)
                            AddOrUpdateItem("pickaxe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.r.Next(0, 5) == 0)
                            AddOrUpdateItem("scythe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.r.Next(0, 5) == 0)
                            AddOrUpdateItem("axe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.r.Next(0, 5) == 0)
                            AddOrUpdateItem("shovel", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        break;

                    case "military":
                        string[] weaponTypes = { "shortsword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };
                        foreach (string weapon in weaponTypes)
                            if (Game1.r.Next(0, 12) == 0)
                                AddOrUpdateItem(weapon, new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);

                        string[] armorTypes = { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                        foreach (string armor in armorTypes)
                        {
                            if (Game1.r.Next(0, 12) == 0)
                            {
                                if (armor == "gauntlet" || armor == "boot")
                                {
                                    AddOrUpdateItem($"left {armor}", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                                    AddOrUpdateItem($"right {armor}", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                                }
                                else
                                {
                                    AddOrUpdateItem(armor, new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                                }
                            }
                        }
                        break;

                    case "coffee":
                        for (int i = Game1.r.Next(0, 3); i != 0; i--)
                        {
                            List<string> materials = new List<string> { Location.HomeCivilization.CulturalWood.Name };
                            List<string> containedItems = new List<string> { $"spice,{Game1.r.Next(10, 15)},coffee" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem("coffee crate", materials, 1, contained);
                        }
                        break;

                    case "tea":
                        for (int i = Game1.r.Next(0, 3); i != 0; i--)
                        {
                            List<string> materials = new List<string> { Location.HomeCivilization.CulturalWood.Name };
                            List<string> containedItems = new List<string> { $"spice,{Game1.r.Next(10, 15)},tea" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem("tea crate", materials, 1, contained);
                        }
                        break;

                    case "wood":
                        AddOrUpdateItem("log", new List<string> { Location.HomeCivilization.CulturalWood.Name }, Game1.r.Next(1, 4));
                        break;

                    case "ceramics":
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small urn", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big urn", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small pot", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big pot", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small mug", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big mug", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small bowl", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big bowl", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        break;

                    case "glassmaking":
                        if (Game1.r.Next(0, 2) == 0)
                            AddOrUpdateItem("sheet", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small mug", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big mug", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small chalice", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big chalice", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small bowl", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big bowl", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("small cup", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("big cup", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        break;

                    case "dye":
                        if (Game1.r.Next(0, 2) == 0)
                        {
                            string DyeColor = Game1.GetFamilyColors(Location.HomeCivilization.Color)[Game1.r.Next(3)];
                            List<string> materials = new List<string> { Game1.GameWorld.Glass.Name };
                            string contained = $"dye,1,{Game1.GameWorld.MaterialsFromColors[DyeColor][Game1.r.Next(3)].Name}";
                            AddOrUpdateItem("bottle", materials, 1, contained);
                        }
                        break;

                    case "waspkeeping":
                        if (Game1.r.Next(0, 3) == 0)
                        {
                            List<string> materials = new List<string> { Game1.GameWorld.Glass.Name };
                            string contained = $"portion,1,honey";
                            AddOrUpdateItem("jar", materials, 1, contained);
                        }
                        if (Game1.r.Next(0, 10) == 0)
                            AddOrUpdateItem("tablet", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        if (Game1.r.Next(0, 5) == 0)
                            AddOrUpdateItem("candle", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        if (Game1.r.Next(0, 2) == 0)
                            AddOrUpdateItem("cube", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        break;

                    case "fuel":
                        AddOrUpdateItem("fragment", new List<string> { Game1.GameWorld.Vitalium.Name }, Game1.r.Next(1, 9));
                        break;

                    case "masonry":
                        AddOrUpdateItem("brick", new List<string> { Location.HomeCivilization.CulturalStone.Name }, Game1.r.Next(1, 6));
                        break;

                    case "healing":
                        if (Game1.r.Next(0, 5) == 0)
                        {
                            int healingItem = Game1.r.Next(1, 4);

                            switch (healingItem)
                            {
                                case 1:
                                    AddOrUpdateItem("salve", new List<string> { Location.Region.HarvestableFiber.Name }, 1);
                                    break;
                                case 2:
                                    AddOrUpdateItem("bandage", new List<string> { Location.HomeCivilization.CulturalCloth.Name }, 1);
                                    break;
                                case 3:
                                    AddOrUpdateItem("vial", new List<string> { Game1.GameWorld.Glass.Name, Game1.GameWorld.Vitalium.Name }, 1);
                                    break;
                            }
                        }
                        break;

                    default:
                        // Handle default case
                        break;
                }
            }

            // Construct the final string list with the correct counts
            var finalItems = new List<string>();
            foreach (var kvp in itemDict)
            {
                string itemTypeAndMaterials = kvp.Key;
                int count = kvp.Value;

                // Add the count in the correct position
                var parts = itemTypeAndMaterials.Split('&');
                string mainPart = parts[0];
                string containedPart = parts.Length > 1 ? parts[1] : "";

                string[] mainParts = mainPart.Split(',');
                string itemType = mainParts[0];
                string materials = string.Join(",", mainParts.Skip(1));

                string finalString = $"{itemType},{count},{materials}";
                if (!string.IsNullOrEmpty(containedPart))
                {
                    finalString += $"&{containedPart}";
                }

                finalItems.Add(finalString);
            }

            return finalItems;
        }



        void AssignInitialTask(Architect architect)
        {
            if(!(Game1.GameWorld.HumanoidRaces.Contains(architect.Race)))
            {
                return;
            }

            if (architect.Profession == "druidcrafter" || (architect.Profession == "gardener" && Game1.r.Next(3) == 0))
            {
                architect.Task = "druidcrafting";
                architect.CyclesLeftInTask = 500;

                return;
            }

            if (architect.Room == null)
            {
                if(Game1.r.Next(2) != 0) //33 percent chance you pretend like you were on your way somewhere
                {
                    if (architect.Block.Architects.Count() == 1)
                    {
                        architect.Task = "contemplate";
                    }
                    else
                    {
                        architect.Task = "socializing";
                    }

                    architect.CyclesLeftInTask = Game1.r.Next(1, 500);
                }

                return;
            }

            string roomType = architect.Room.Structure.Type;
            List<string> possibleTasks = new List<string>();

            // Determine possible tasks based on room type
            switch (roomType)
            {
                case "house":
                case "ship":
                case "cove":
                case "mound":
                case "monastery":
                    possibleTasks.AddRange(new List<string> { "sleeping", "eating", "drinking", "socializing" });
                    break;
                case "spire":
                case "archway":
                case "hallway":
                case "fortress":
                case "monument":
                case "toroid":
                case "towers":
                case "outpost":
                case "pyramid":
                case "sanctum":
                    possibleTasks.AddRange(new List<string> { "study", "discussion" });
                    break;
                case "keep":
                    possibleTasks.AddRange(new List<string> { "discussion", "study" });
                    break;
                case "core":
                case "heart":
                case "stronghold":
                    possibleTasks.AddRange(new List<string> { "discussion", "study" });
                    break;
                case "tower":
                case "commune":
                    possibleTasks.AddRange(new List<string> { "study", "discussion" });
                    break;
                default:
                    possibleTasks.AddRange(new List<string> { "industry", "contemplate" });
                    break;
            }

            if (possibleTasks.Count() > 0)
            {
                architect.Task = possibleTasks[Game1.r.Next(possibleTasks.Count())];
                int maxCycles = architect.Task switch
                {
                    "sleeping" => 350000,
                    "eating" => 500,
                    "drinking" => 500,
                    "socializing" => 500,
                    "drinkingcaffeine" => 500,
                    "discussion" => 500,
                    "study" => 500,
                    "performmusic" => 500,
                    "performpoetry" => 500,
                    "performdance" => 500,
                    "cook" => 500,
                    "industry" => 500,
                    "contemplate" => 500,
                    _ => 500
                };

                architect.CyclesLeftInTask = Game1.r.Next(1, maxCycles);
            }
        }



        EntityList<Structure> GetPossibleStructures(Architect a)
        {
            EntityList<Structure> possibleStructures = new EntityList<Structure>();
            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    if (Game1.ConvertProfessionToBuilding.ContainsKey(a.Profession))
                    {
                        var structures = DistrictMap[DistrictX + DistrictZ * 7].Structures
                                            .Where(s => s.Type == Game1.ConvertProfessionToBuilding[a.Profession]);
                        foreach (var structure in structures)
                        {
                            possibleStructures.Add(structure);
                        }
                    }
                    else
                    {
                        var structures = DistrictMap[DistrictX + DistrictZ * 7].Structures;
                        foreach (var structure in structures)
                        {
                            possibleStructures.Add(structure);
                        }
                    }
                }
            }
            return possibleStructures;
        }

        Room GetRandomRoom(EntityList<Structure> structures)
        {
            // Filter structures to include only those with at least one room
            var structuresWithRooms = structures.Where(s => s.Rooms.Count() > 0);

            // Check if there are any structures with rooms
            if (structuresWithRooms.Count() == 0)
            {
                throw new InvalidOperationException("No structures with rooms available.");
            }

            // Randomly select a structure with at least one room
            Structure randomStructure = structuresWithRooms[Game1.r.Next(structuresWithRooms.Count())];

            // Randomly select a room from the selected structure
            return randomStructure.Rooms[Game1.r.Next(randomStructure.Rooms.Count())];
        }


        public void Load()
        {
            IsLoaded = true;

            Game1.AllSubjects = Game1.CollectAllSubjects(Game1.MostRecentPartyTurnArchitect, "none");

            EntityList<Architect> allArchitects = new EntityList<Architect>();
            allArchitects.AddRange(Architects);

            EntityList<Structure> allDistrictStructures = new EntityList<Structure>();

            for (int x = 0; x < 7; x++)
            {
                for (int z = 0; z < 7; z++)
                {
                    allDistrictStructures.AddRange(DistrictMap[x + z * 7].Structures);
                }
            }

            foreach (Structure s in Location.AllStructures)
            {
                if (s.Block.District == this && !allDistrictStructures.Contains(s))
                {
                    allDistrictStructures.Add(s);
                }
            }

            if (Location.Prism != null)
            {
                allDistrictStructures.Add(Location.Prism);
            }

            if (!HasBeenLoadedEver)
            {
                if(Location.Type == "preserve")
                {
                    for(int I = Game1.r.Next(40, 80); I != 0; I--)
                    {
                        string Type = new List<string>() { "tree", "plant", "bush" }[Game1.r.Next(3)];
                        Object o = new Object(null, Type, new EntityList<Material>() { Game1.GameWorld.Membrane }, null);
                        DistrictMap[Game1.r.Next(49)].Objects.Add(o);
                    }
                }


                foreach (Structure s in allDistrictStructures)
                {
                    Room coreRoom = new Room(s, new EntityList<Object>(), new EntityList<Architect>(), new EntityList<Architect>());
                    s.Rooms.Add(coreRoom);

                    string Layout = s.Type;

                    if (Location.Layout != null && Location.Layout != "")
                    {
                        Layout = Location.Layout;
                    }

                    int extraRoomCount = Layout switch
                    {
                        "house" or "ship" or "cove" or "mound" or "monastery" => Game1.r.Next(0, 4),
                        "spire" or "archway" or "hallway" or "fortress" or "monument" or "toroid" or "towers" or "outpost" or "pyramid" or "sanctum" => Game1.r.Next(10, 20),
                        "keep" => Game1.r.Next(1, 4),
                        "core" or "heart" or "stronghold" => 20,
                        "tower" or "commune" => Game1.r.Next(10, 13),
                        _ => 2,
                    };

                    for (int i = 0; i < extraRoomCount; i++)
                    {
                        s.Rooms.Add(new Room(s, new EntityList<Object>(), new EntityList<Architect>(), new EntityList<Architect>()));
                    }

                    foreach (Room r in s.Rooms)
                    {
                        r.PopulateRoom();
                    }

                    foreach (Object o in s.HistoricalObjects)
                    {
                        s.Rooms[Game1.r.Next(s.Rooms.Count())].Objects.Add(o);
                    }
                    s.HistoricalObjects.Clear();
                }
            }

            if (Location.Market != null)
            {
                if (Location.DebtShibas.Count() == 0)
                {
                    int shibas = Game1.r.Next(4, 8);
                    for (int i = 0; i < shibas; i++)
                    {
                        Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], Game1.GameWorld.GetRace("debtshiba"), Game1.r.Next(9999999), "debtshiba", new EntityList<Object>(), Location, this, Location.Market.Block, "", 4);
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        a.HomeStructure = Location.Market;
                        a.Block = Location.Market.Block;
                        Location.DebtShibas.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                }

                allArchitects.AddRange(Location.DebtShibas);
                Location.Market.Block.Architects.AddRange(Location.DebtShibas);
            }

            // Define the outcast civilization types and their professions
            Dictionary<string, List<string>> outcastProfessions = new Dictionary<string, List<string>>()
{
    { "druid", new List<string> { "gardener", "druidcrafter", "archdruid" } },
    { "scavenger", new List<string> { "salvager", "constructor", "scraplord" } },
    { "cultist", new List<string> { "cultist", "priest", "intermediary" } },
    { "pirate", new List<string> { "swashbuckler", "deadeye", "captain" } },
    { "anarchist", new List<string> { "disruptor", "bomber", "inspiration" } }
};

            for (int i = 0; i < UnplacedPopulation; i++)
            {
                string sex = Game1.r.Next(1, 3) == 1 ? "male" : "female";
                string role = Game1.WeightedRandomNormalProfessions[Game1.r.Next(Game1.WeightedRandomNormalProfessions.Count())];
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
                        EntityList<Race> otherHumanoidRaces = Game1.GameWorld.HumanoidRaces.Where(r => r.Name != Location.PrimaryRace.Name && (r.Name == "luminarch" || r.Name == "nightfell" || r.Name == "archaix"));
                        race = otherHumanoidRaces[Game1.r.Next(otherHumanoidRaces.Count())];
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
                        race = Game1.GameWorld.HumanoidRaces.First(r => r.Name == "luminarch");
                    }
                    else if (bias < 6)
                    {
                        race = Game1.GameWorld.HumanoidRaces.First(r => r.Name == "nightfell");
                    }
                    else
                    {
                        race = Game1.GameWorld.HumanoidRaces.First(r => r.Name == "archaix");
                    }
                }

                string destiny = Game1.r.Next(1, 5000) switch
                {
                    < 3 => "wizard",
                    < 5 when race == Game1.GameWorld.GetRace("nightfell") => "warlock",
                    < 7 when race == Game1.GameWorld.GetRace("luminarch") => "sorcerer",
                    < 8 => "parasite",
                    _ => ""
                };

                // Check if Location.HomeCivilization is one of the outcast civilizations
                if (Location.HomeCivilization != null && outcastProfessions.ContainsKey(Location.HomeCivilization.Type))
                {
                    List<string> professions = outcastProfessions[Location.HomeCivilization.Type];
                    int professionRoll = Game1.r.Next(100);

                    if (professionRoll < 80)
                    {
                        role = professions[0];
                    }
                    else if (professionRoll < 95)
                    {
                        role = professions[1];
                    }
                    else
                    {
                        role = professions[2];
                    }
                }

                Architect a = new Architect("", sex, race, Game1.r.Next(14, 90), role, new EntityList<Object>(), Location, this, null, destiny, 1);
                a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                allArchitects.Add(a);
            }


            UnplacedPopulation = 0;

            if (Location.Market != null && Location.Market.Block.District == this)
            {
                foreach (Group g in Location.TradersAtThisLocation)
                {
                    foreach (Architect a in g.Architects)
                    {
                        if (!Game1.GameWorld.GamePlayerParty.Architects.Contains(a))
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
                if (!Game1.GameWorld.GamePlayerParty.Architects.Contains(a) && !Location.DebtShibas.Contains(a) && a.NextMigrationLocation == null)
                {
                    a.Loaded = true;
                    a.UpdateNames();

                    EntityList<Structure> possibleStructures = (a.Bound && this.Location.AllStructures.Count() > 0)
                        ? new EntityList<Structure> { this.Location.AllStructures.FirstOrDefault(s => s.Block.District == this) }
                        : GetPossibleStructures(a);

                    if (possibleStructures.Count() > 0)
                    {
                        Structure chosenStructure = possibleStructures[Game1.r.Next(possibleStructures.Count())];
                        Room chosenRoom = chosenStructure.Rooms[0];
                        chosenRoom.Architects.Add(a);
                        a.Room = chosenRoom;
                        a.Block = chosenRoom.Structure.Block;
                    }
                    else if (a.IsCalamity == false || Game1.r.Next(3) == 0)
                    {
                        Block b = DistrictMap[Game1.r.Next(0, 49)];
                        b.Architects.Add(a);
                        a.Block = b;
                    }
                    else
                    {
                        Structure chosenStructure = Location.AllStructures[0];
                        Room chosenRoom = chosenStructure.Rooms[Game1.r.Next(chosenStructure.Rooms.Count())];
                        chosenRoom.Architects.Add(a);
                        a.Room = chosenRoom;
                        a.Block = chosenRoom.Structure.Block;
                    }

                    a.District = this;
                    a.UpdateNames();
                    Game1.LoadedArchitects.Add(a);

                    // Assign a task if possible
                    AssignInitialTask(a);
                }

                bool isCoolProfession = a.Profession == "artist" ||
                                 a.Profession == "curator" ||
                                 a.Profession == "perfectionist" ||
                                 a.Profession == "manager" ||
                                 a.Profession == "brute" ||
                                 a.Profession == "cluster";

                if (!isCoolProfession)
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

            if (Location.Districts.Count() == 1 && Location.AllStructures.Count() == 1)
            {
                Structure structureInSameDistrict = Location.AllStructures.FirstOrDefault(s => s.Block.District == this);
                if (structureInSameDistrict != null)
                {
                    for (int i = Location.GuardiansInNetwork; i > 0; i--)
                    {
                        Block b = structureInSameDistrict.Block;
                        Race race = Location.Type == "sanctum"
                            ? Game1.GameWorld.ConstructRaces[Game1.r.Next(Game1.GameWorld.ConstructRaces.Count())]
                            : Location.GuardianType;

                        Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], race, 10, "construct", new EntityList<Object>(), Location, this, b, "", 5);
                        a.Inventory.Add(Game1.GameWorld.MagicalSuperLoot(Game1.r.Next(3, 7)));
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        a.Room = structureInSameDistrict.Rooms[Game1.r.Next(structureInSameDistrict.Rooms.Count())];
                        a.Block = a.Room.Structure.Block;
                        a.HomeLocation = Location;
                        a.Room.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                }
            }

            // Load General Items
            EntityList<Object> itemsToAdd = new EntityList<Object>();
            foreach (string itemString in GeneralItemsWeHave)
            {
                EntityList<Object> items = Game1.ConvertStringToObjects(itemString);
                itemsToAdd.AddRange(items);
            }

            // Determine the target room for each item
            foreach (Object item in itemsToAdd)
            {
                Room targetRoom = Location.Market != null && Location.Market.Block.District == this && Game1.r.Next(1, 3) == 1
                    ? Location.Market.Rooms[0]
                    : GetRandomRoom(allDistrictStructures);

                targetRoom.Objects.Add(item);
                item.UpdateNames();
            }


            Game1.LoadedArchitects.AddRange(Game1.GameWorld.GamePlayerParty.Architects);

            bool EverythingBelongsToTheQueen = false;

            if (Location.Type == "core" || Location.Type == "heart")
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
                        if (EverythingBelongsToTheQueen)
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

            foreach (Architect a in Game1.LoadedArchitects)
            {
                if (a.Profession == "sovereign" || a.Profession == "heart")
                {
                    a.Task = "sentinel";
                    a.CyclesLeftInTask = 99999;

                    if (a.Room != null)
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

            Game1.LoadedArchitects = Game1.LoadedArchitects.Distinct();
        }

        void AddOrUpdateItemString(List<string> itemList, string itemString)
        {
            string[] itemParts = itemString.Split(new[] { ',' }, 3);
            string itemType = itemParts[0];
            int itemCount = int.Parse(itemParts[1]);
            string itemMaterialsAndContained = itemParts[2];

            string existingItem = itemList.FirstOrDefault(item => item.StartsWith($"{itemType},{itemMaterialsAndContained}"));
            if (existingItem != null)
            {
                string[] existingItemParts = existingItem.Split(new[] { ',' }, 3);
                int existingItemCount = int.Parse(existingItemParts[1]);
                int newCount = existingItemCount + itemCount;

                itemList.Remove(existingItem);
                itemList.Add($"{itemType},{newCount},{itemMaterialsAndContained}");
            }
            else
            {
                itemList.Add(itemString);
            }
        }




        public void Unload()
        {
            IsLoaded = false;
            int TotalArchitects = 0;

            // Remove tasks
            foreach (Architect a in Game1.LoadedArchitects)
            {
                a.Task = "";
                a.CooldownCycles = 0;
                a.CyclesLeftInTask = 0;
            }

            // Remove architects and round up the population
            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    foreach (Architect a in DistrictMap[DistrictX + DistrictZ * 7].Architects)
                    {
                        if (!Game1.GameWorld.GamePlayerParty.Architects.Contains(a))
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

                    // Create a list to hold the objects to remove
                    EntityList<Object> objectsToRemove = new EntityList<Object>();

                    foreach (Object o in DistrictMap[DistrictX + DistrictZ * 7].Objects)
                    {
                        if (o.Type == "shadow storage" || o.Type == "well")
                        {
                            continue;
                        }

                        objectsToRemove.Add(o);

                        if (o.IsGeneralGood)
                        {
                            string itemString = Game1.ConvertObjectToString(o);
                            AddOrUpdateItemString(GeneralItemsWeHave, itemString);
                        }
                        else
                        {
                            if (Location.Prism != null)
                            {
                                Location.Prism.HistoricalObjects.Add(o);
                            }
                            else if (Location.AllStructures.Count() > 0)
                            {
                                Location.AllStructures[0].HistoricalObjects.Add(o);
                            }
                        }
                    }

                    // Remove the collected objects from the DistrictMap
                    foreach (Object o in objectsToRemove)
                    {
                        DistrictMap[DistrictX + DistrictZ * 7].Objects.Remove(o);

                        if(Location.AllStructures.Count() > 0)
                        {
                            Location.AllStructures[Game1.r.Next(Location.AllStructures.Count())].HistoricalObjects.Add(o);
                        }
                    }

                    // Handle structures and rooms
                    foreach (Structure s in DistrictMap[DistrictX + DistrictZ * 7].Structures)
                    {
                        foreach (Room r in s.Rooms)
                        {
                            EntityList<Architect> ArchitectsToRemove = new EntityList<Architect>();
                            foreach (Architect a in r.Architects)
                            {
                                if (!Game1.GameWorld.GamePlayerParty.Architects.Contains(a) && !a.IsLoadedTrader)
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

                            // Create a list to hold objects that are general goods
                            EntityList<Object> RoomObjectsToRemove = new EntityList<Object>();

                            // Handle objects in the room
                            foreach (Object o in r.Objects)
                            {
                                if (o.IsGeneralGood)
                                {
                                    string itemString = Game1.ConvertObjectToString(o);
                                    AddOrUpdateItemString(GeneralItemsWeHave, itemString);
                                    RoomObjectsToRemove.Add(o);
                                }
                            }

                            foreach (Object o in RoomObjectsToRemove)
                            {
                                r.Objects.Remove(o);
                            }

                        }
                    }
                }
            }


            bool AllArchitectsDeadOrInParty = Game1.LoadedArchitects.All(architect => !architect.IsAlive || Game1.GameWorld.GamePlayerParty.Architects.Contains(architect));

            if (Game1.GameWorld.GamePlayerParty.CurrentEvent != null)
            {
                if (AllArchitectsDeadOrInParty)
                {
                    Game1.GameWorld.GamePlayerParty.CurrentEvent.Region.Events.Remove(Game1.GameWorld.GamePlayerParty.CurrentEvent);
                }
                Game1.GameWorld.GamePlayerParty.CurrentEvent = null;
            }


            Game1.LoadedArchitects.Clear();

        }
    }
}
