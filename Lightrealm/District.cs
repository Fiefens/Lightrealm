using Lightrealm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class District : Entity
    {
        public bool IsPrimary { get; set; } = false;
        public Location Location;

        public EntityList<Architect> AllArchitectsInDistrict()
        {
            EntityList<Architect> architects = new EntityList<Architect>();
            foreach (var block in DistrictMap)
            {
                architects.UnionWith(block.Architects);
            }
            return architects;
        }


        public Room PylonRoom = null;
        public Block PylonBlock1 = null;
        public Block PylonBlock2 = null;
        public bool PylonsPlaced = false;

        public int UnplacedPopulation { get; set; }

        public int MaxPopulation { get; set; }

        public EntityHashSet<Architect> DistrictArchitects { get; set; } = new EntityHashSet<Architect>();

        public EntityHashSet<Architect> ArchitectsToRemove { get; set; } = new EntityHashSet<Architect>();

        public EntityHashSet<Structure> Taverns { get; set; } = new EntityHashSet<Structure>();

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
            Name = Game1.GameWorld.GenerateUniqueName("1S" + (Game1.GameWorld.rnd.Next(3, 4) - 1) + "s", this, Game1.GameWorld.rnd);
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
                        DistrictMap[DistrictX + DistrictZ * 7].Biome = new List<string>() { "water", "taiga", "forest", "plains" }[Game1.GameWorld.rnd.Next(4)];
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
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.GameWorld.rnd.Next(2) == 0 ? "desert" : "ocean";
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
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.GameWorld.rnd.Next(2) == 0 ? "desert" : "ocean";
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
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.GameWorld.rnd.Next(2) == 0 ? "desert" : "ocean";
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
                                DistrictMap[DistrictX + DistrictZ * 7].Biome = Game1.GameWorld.rnd.Next(2) == 0 ? "desert" : "ocean";
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
            Industry = Game1.Industries[Game1.GameWorld.rnd.Next(Game1.Industries.Count())];
        }



        public District()
        {

        }


        public int Population()
        {
            return DistrictArchitects.Count() + UnplacedPopulation;
        }

        public void SupplyLocation(int Intensity)
        {
            if (Industry == null)
            {
                return;
            }

            string DecidedProduction = Industry;

            // 40% chance to make something different
            if (Game1.GameWorld.rnd.Next(1, 6) <= 2)
            {
                DecidedProduction = Game1.Industries[Game1.GameWorld.rnd.Next(Game1.Industries.Count())];
            }

            List<string> itemsToBeAdded = GenerateItems(DecidedProduction, Intensity);

            // Adding items and increasing crafts within the same method
            foreach (var item in itemsToBeAdded)
            {
                if (Game1.GameWorld.rnd.Next(1, 3) == 1)
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
                        if (Game1.GameWorld.rnd.Next(0, 30) == 0)
                        {
                            int clothingPiece = Game1.GameWorld.rnd.Next(1, 10);
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
                                    AddOrUpdateItem("roll", materials, 1);
                                    break;
                            }
                        }
                        break;

                    case "spices":
                        List<string> spiceMaterials = new List<string> { Location.HomeCivilization.CulturalCloth.Name };

                        void AddSpicePouch(string spiceType, string spiceMaterial)
                        {
                            List<string> containedItems = new List<string> { $"{spiceType},{10},{spiceMaterial}" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem($"{spiceMaterial} pouch", spiceMaterials, 1, contained);
                        }

                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "salt");
                        }
                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "pepper");
                        }
                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "paprika");
                        }
                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                        {
                            AddSpicePouch("spice", "isodust");
                        }
                        break;

                    case "coffee":
                        List<string> coffeeMaterials = new List<string> { Location.HomeCivilization.CulturalWood.Name };

                        void AddCoffeeCrate(string spiceType, string spiceMaterial)
                        {
                            List<string> containedItems = new List<string> { $"{spiceType},{10},{spiceMaterial}" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem("coffee crate", coffeeMaterials, 1, contained);
                        }

                        for (int i = Game1.GameWorld.rnd.Next(0, 3); i != 0; i--)
                        {
                            AddCoffeeCrate("spice", "coffee");
                        }
                        break;

                    case "tea":
                        List<string> teaMaterials = new List<string> { Location.HomeCivilization.CulturalWood.Name };

                        void AddTeaCrate(string spiceType, string spiceMaterial)
                        {
                            List<string> containedItems = new List<string> { $"{spiceType},{10},{spiceMaterial}" };
                            string contained = GenerateContainedItems(containedItems);
                            AddOrUpdateItem("tea crate", teaMaterials, 1, contained);
                        }

                        for (int i = Game1.GameWorld.rnd.Next(0, 3); i != 0; i--)
                        {
                            AddTeaCrate("spice", "tea");
                        }
                        break;


                    case "metal":
                        if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                            AddOrUpdateItem("bar", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        break;

                    case "jewelry":
                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                            AddOrUpdateItem("cut gem", new List<string> { Location.HomeCivilization.CulturalGemstone.Name }, 1);
                        break;

                    case "tools":
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                            AddOrUpdateItem("pickaxe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                            AddOrUpdateItem("scythe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                            AddOrUpdateItem("work axe", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                            AddOrUpdateItem("shovel", new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);
                        break;

                    case "military":
                        string[] weaponTypes = { "shortsword", "longsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "war hammer", "shield", "whip", "scourge" };
                        foreach (string weapon in weaponTypes)
                            if (Game1.GameWorld.rnd.Next(0, 12) == 0)
                                AddOrUpdateItem(weapon, new List<string> { Location.HomeCivilization.CulturalMetal.Name }, 1);

                        string[] armorTypes = { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                        foreach (string armor in armorTypes)
                        {
                            if (Game1.GameWorld.rnd.Next(0, 12) == 0)
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

                    
                    case "wood":
                        AddOrUpdateItem("log", new List<string> { Location.HomeCivilization.CulturalWood.Name }, Game1.GameWorld.rnd.Next(1, 4));
                        break;

                    case "ceramics":
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small urn", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big urn", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small pot", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big pot", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small mug", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big mug", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small bowl", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big bowl", new List<string> { Game1.GameWorld.Clay.Name }, 1);
                        break;

                    case "glassmaking":
                        if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                            AddOrUpdateItem("sheet", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small mug", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big mug", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small chalice", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big chalice", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small bowl", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big bowl", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("small cup", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("big cup", new List<string> { Game1.GameWorld.Glass.Name }, 1);
                        break;

                    case "dye":
                        if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                        {
                            string DyeColor = Game1.GetFamilyColors(Location.HomeCivilization.EntityColor)[Game1.GameWorld.rnd.Next(3)];
                            List<string> materials = new List<string> { Game1.GameWorld.Glass.Name };
                            string contained = $"dye,1,{Game1.GameWorld.MaterialsFromColors[DyeColor][Game1.GameWorld.rnd.Next(3)].Name}";
                            AddOrUpdateItem("bottle", materials, 1, contained);
                        }
                        break;

                    case "waspkeeping":
                        if (Game1.GameWorld.rnd.Next(0, 3) == 0)
                        {
                            List<string> materials = new List<string> { Game1.GameWorld.Glass.Name };
                            string contained = $"portion,1,honey";
                            AddOrUpdateItem("jar", materials, 1, contained);
                        }
                        if (Game1.GameWorld.rnd.Next(0, 10) == 0)
                            AddOrUpdateItem("wax tablet", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                            AddOrUpdateItem("candle", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        if (Game1.GameWorld.rnd.Next(0, 2) == 0)
                            AddOrUpdateItem("cube", new List<string> { Game1.GameWorld.Waspwax.Name }, 1);
                        break;

                    case "fuel":
                        AddOrUpdateItem("fragment", new List<string> { Game1.GameWorld.Vitalium.Name }, Game1.GameWorld.rnd.Next(1, 9));
                        break;

                    case "masonry":
                        AddOrUpdateItem("brick", new List<string> { Location.HomeCivilization.CulturalStone.Name }, Game1.GameWorld.rnd.Next(1, 6));
                        break;

                    case "healing":
                        if (Game1.GameWorld.rnd.Next(0, 5) == 0)
                        {
                            int healingItem = Game1.GameWorld.rnd.Next(1, 4);

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
            string roomType = architect.Room != null ? architect.Room.Structure.Type : "none";
            List<string> possibleTasks = new List<string>();

            if (!(Game1.GameWorld.HumanoidRaces.Contains(architect.Race)))
            {
                if(architect.Race.Name == "photonexus")
                {
                    possibleTasks.AddRange(new List<string>() { "meditation", "polishing", "oversight"});
                }
                else if (architect.Race.Name == "shade")
                {
                    possibleTasks.AddRange(new List<string>() { "vibrating", "pumping", "convulsing" });
                }
                else if (architect.Race.Name == "isofractal")
                {
                    possibleTasks.AddRange(new List<string>() { "sculpting", "engraving", "radiating" });
                }
            }
            else
            {
                if (architect.Profession == "druidcrafter" || (architect.Profession == "gardener" && Game1.GameWorld.rnd.Next(3) == 0))
                {
                    architect.Task = "druidcrafting";

                    architect.Target = (architect.Location, architect.District, architect.Block, architect.Room);
                    architect.CyclesLeftInTask = 500;

                    return;
                }

                if (architect.Room == null)
                {
                    if (Game1.GameWorld.rnd.Next(2) != 0) //33 percent chance you pretend like you were on your way somewhere
                    {
                        if (architect.Block.Architects.Count() == 1)
                        {
                            possibleTasks.Add("contemplate");
                        }
                        else
                        {
                            possibleTasks.Add("socializing");
                        }
                    }

                    return;
                }

                // Determine possible tasks based on room type
                switch (roomType)
                {
                    case "house":
                    case "ship":
                    case "cove":
                    case "hoard":
                    case "monastery":
                    case "bastion":
                    case "fort":
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
                        possibleTasks.AddRange(new List<string> { "study", "discussion", "waitforgame" });
                        break;
                    case "library":
                        possibleTasks.AddRange(new List<string> { "study" });
                        break;
                    case "tavern":
                        possibleTasks.AddRange(new List<string> { "waitforgame", "waitforgame", "waitforgame", "sleeping", "eating", "drinking", "socializing", "performmusic", "performpoetry" });
                        break;
                    default:
                        possibleTasks.AddRange(new List<string> { "industry", "contemplate" });
                        break;
                }
            }


            if (possibleTasks.Count() > 0)
            {
                architect.Task = possibleTasks[Game1.GameWorld.rnd.Next(possibleTasks.Count())];
                architect.CyclesLeftInTask = architect.Task switch
                {
                    "vacantfortrade" => 600, 
                    "druidcrafting" => 300, // druidcraft for 30 seconds
                    "drinking" => 300, // drinking a glass or bucketish cup water takes roughly around 30 seconds
                    "drinkingcaffeine" => 500, // savor caffeine a bit, though. also it's hot
                    "eating" => 500, // this takes longer for a similar reason, also it's food
                    "sleeping" => 350000,
                    "discussion" => 500, // chat chat chat chat
                    "study" => 10000, // takes five minutes at minimum
                    "socializing" => 300, // conversations don't last too long, but I want them going in and out often if it's the well
                    "performmusic" => 500,
                    "waitforgame" =>  2000,
                    "performdance" => 500,
                    "performtheater" => 500,
                    "performpoetry" => 500,
                    "industry" => 300, // one single instance might take half a minute
                    "contemplate" => 300, // stare off into the sunset for about a minute
                    _ => 1000
                };

                if(architect.Task == "contemplate")
                {
                    architect.CurrentContemplationTopic = Game1.GameWorld.Domains[Game1.GameWorld.rnd.Next(Game1.GameWorld.Domains.Count)].Name;
                }

                if(architect.Task == "study")
                {
                    architect.AssignStudyTopic();
                }

                // Randomly adjust CyclesLeftInTask with a variation of -50 to +50, ensuring a minimum of 50
                architect.CyclesLeftInTask = Math.Max(architect.CyclesLeftInTask + Game1.GameWorld.rnd.Next(-50, 51), 50);
            }
            architect.Target = (architect.Location, architect.District, architect.Block, architect.Room);
        }



        EntityList<Structure> GetPossibleStructures(Architect a)
        {
            EntityList<Structure> possibleStructures = new EntityList<Structure>();

            if (a.IsAlive == false && Location.Type == "tomb")
            {
                possibleStructures.Add(Location.AllStructures[0]);
                return possibleStructures;
            }

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
            Structure randomStructure = structuresWithRooms[Game1.GameWorld.rnd.Next(structuresWithRooms.Count())];

            // Randomly select a room from the selected structure
            return randomStructure.Rooms[Game1.GameWorld.rnd.Next(randomStructure.Rooms.Count())];
        }


        public void Load()
        {
            IsLoaded = true;
            Game1.LastNameUpdateCycle = 0.0;
            //this is requried for allsubjects
            Game1.LastLoadedDistrict = this;

            Game1.ArchitectIndex = 0;

            Game1.AllSubjects = Game1.CollectAllSubjects(Game1.MostRecentPartyTurnArchitect, "none");

            EntityList<Architect> allArchitectsThatIThinkNeedSpecialProcessing = new EntityList<Architect>();


            foreach(Architect a in DistrictArchitects)
            {
                a.PopulateSelf(Game1.GameWorld.HumanoidRaces.Contains(a.Race));
                a.Task = "";
                a.Target = (null, null, null, null);
            }


            allArchitectsThatIThinkNeedSpecialProcessing.AddRange(DistrictArchitects);

            EntityList<Structure> allDistrictStructures = new EntityList<Structure>();

            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty != null)
                Game1.GameWorld.GamePlayerAssociation.ActiveParty.ReceivedPlanMessageThisLoad = false;

            List<int> outskirtsBlockIndexes = new List<int>()
{
    // Top row
    0, 1, 2, 3, 4, 5, 6,
    
    // Right column (without corners already added)
    13, 20, 27, 34, 41,

    // Bottom row
    42, 43, 44, 45, 46, 47, 48,

    // Left column (without corners already added)
    7, 14, 21, 28, 35
};

            List<int> allBlockIndexes = new List<int>()
{
    0, 1, 2, 3, 4, 5, 6,
    7, 8, 9, 10, 11, 12, 13,
    14, 15, 16, 17, 18, 19, 20,
    21, 22, 23, 24, 25, 26, 27,
    28, 29, 30, 31, 32, 33, 34,
    35, 36, 37, 38, 39, 40, 41,
    42, 43, 44, 45, 46, 47, 48
};

            //scaffold blocks


            if (Game1.GameWorld.AllFactions.Any(f => f.CurrentPlan != null && f.CurrentPlan.PlanLocation == this.Location))
            {
                foreach(int i in outskirtsBlockIndexes)
                {
                    if(Game1.r.Next(2) == 1)
                    {
                        int Scaffolds = Game1.r.Next(2, 5);

                        for (int Z = 0; Z < Scaffolds; Z++)
                            DistrictMap[i].Objects.Add(new Object(null, "scaffold", new EntityList<Material>() { Location.Region.HarvestableMetal }, false, false, null, null, 1000, false, DistrictMap[i], null, null, false));
                    }
                }
            }






            // Populate AllImportantEntities_Domains
            Game1.AllImportantEntities_Domains = new EntityList<Entity>();
            foreach (Entity domain in Game1.GameWorld.Domains)
            {
                Game1.AllImportantEntities_Domains.Add(domain);
            }

            // Populate AllImportantEntities_Locations
            Game1.AllImportantEntities_Locations = new EntityList<Entity>(
                Game1.GameWorld.AllLocations
                    .SelectMany(location => location.AllStructures.Concat(new EntityList<Entity> { location }))
                    .Concat(Game1.GameWorld.AllHistoricalArchitects)
            );



            //because traders can be in a unit somewhere in Nebraska, we want to make sure if we load them here we are certain ablout their whereabouts.

            foreach (Group g in this.Location.TradersAtThisLocation)
            {
                foreach(Architect a in g.Architects)
                {
                    a.Location = this.Location;
                    a.District = this;

                    if(!a.SelfPopulated)
                    {
                        a.PopulateSelf(true);
                    }
                }
            }



            //max of 4 objectives, prioritize those that are active.

            var sortedObjectives = Location.Objectives.Where(o => o.IsActiveObjective)
    .OrderByDescending(o => o.ParentQuest.Objectives.IndexOf(o) == 1) // true (1) comes before false (0)
    .ThenBy(o => o.ParentQuest.Objectives.IndexOf(o)) // sort within groups by index (0 or 1)
    .Take(4)
    .ToList();

            if (Location.Districts[0] == this)
            {
                foreach (Objective o in sortedObjectives)
                {
                    Game1.LoadedHooks.Add(o.Hook);

                    o.Hook.AnnouncedSelfThisLoad = false;

                    int randomIndex = Game1.GameWorld.rnd.Next(allBlockIndexes.Count);
                    int selectedBlockIndex = allBlockIndexes[randomIndex];
                    Block b = DistrictMap[selectedBlockIndex];

                    if (o.Hook is Object O)
                    {
                        b.Objects.Add(O);
                    }
                    else if (o.Hook is Architect A)
                    {
                        b.Architects.Add(A);
                        Game1.LoadedArchitects.Add(A);
                        A.PopulateSelf(Game1.GameWorld.HumanoidRaces.Contains(A.Race));
                        A.District = this;
                        A.Location = this.Location;
                        A.Block = b;
                    }

                    allBlockIndexes.RemoveAt(randomIndex);
                }
            }




            if (this.Location.AllStructures.Count > 0)
            {
                for (int x = 0; x < 7; x++)
                {
                    for (int z = 0; z < 7; z++)
                    {
                        allDistrictStructures.AddRange(DistrictMap[x + z * 7].Structures);

                        int Decider = Game1.GameWorld.rnd.Next(1, 4);

                        if (Decider == 1)
                            DistrictMap[x + z * 7].SocializationTopic = this.Location.AllStructures[Game1.GameWorld.rnd.Next(this.Location.AllStructures.Count)];
                        else if (Decider == 2 && this.DistrictArchitects.Count > 0)
                            DistrictMap[x + z * 7].SocializationTopic = this.DistrictArchitects.GetRandomItem();
                        else
                            DistrictMap[x + z * 7].SocializationTopic = this.Location.GroupsAtThisLocation.Count > 0 ? (this.Location.GroupsAtThisLocation[Game1.GameWorld.rnd.Next(this.Location.GroupsAtThisLocation.Count)]) : (this.Location.Government != null ? this.Location.Government : this.Location);


                        Decider = Game1.GameWorld.rnd.Next(1, 7);

                        if (Decider == 1)
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.AllHistoricalArchitects.GetRandomItem();
                        else if (Decider == 2 && Game1.GameWorld.AllLocations.Count > 0)
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.AllLocations[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllLocations.Count)];
                        else if (Decider == 3 && Game1.GameWorld.AllSpells.Count > 0)
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.AllSpells[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllSpells.Count)];
                        else if (Decider == 4 && Game1.GameWorld.AllFactions.Count > 0)
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.AllFactions[Game1.GameWorld.rnd.Next(Game1.GameWorld.AllFactions.Count)];
                        else if (Decider == 5 && Game1.GameWorld.Groups.Count > 0)
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.Groups[Game1.GameWorld.rnd.Next(Game1.GameWorld.Groups.Count)];
                        else
                            DistrictMap[x + z * 7].DiscussionTopic = Game1.GameWorld.Domains[Game1.GameWorld.rnd.Next(Game1.GameWorld.Domains.Count)];

                    }
                }
            }

            foreach (Structure s in Location.AllStructures)
            {
                if (s.Block.District == this && !allDistrictStructures.Contains(s))
                {
                    allDistrictStructures.Add(s);
                }
            }

          

            if (!HasBeenLoadedEver)
            {
                if(Location.Type == "preserve")
                {
                    for(int I = Game1.GameWorld.rnd.Next(40, 80); I != 0; I--)
                    {
                        string Type = new List<string>() { "tree", "plant", "bush" }[Game1.GameWorld.rnd.Next(3)];
                        Object o = new Object(null, Type, new EntityList<Material>() { Game1.GameWorld.Membrane }, null);
                        DistrictMap[Game1.GameWorld.rnd.Next(49)].Objects.Add(o);
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
                        "house" or "ship" or "cove" or "hoard" or "monastery" => Game1.GameWorld.rnd.Next(0, 4),
                        "spire" or "archway" or "hallway" or "towers" or "toroid" or "outpost" or "bastion" or "fort" or "pyramid" => Game1.GameWorld.rnd.Next(8, 13),
                        "fortress" or "monument" or "sanctum" => Game1.GameWorld.rnd.Next(12, 19),
                        "keep" => Game1.GameWorld.rnd.Next(1, 4),
                        "core" or "heart" or "stronghold" => 18,
                        "tower" or "commune" => Game1.GameWorld.rnd.Next(10, 13),
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

                }
            }

            //pylons

            if (Location.Type == "stronghold")
            {
                if(!PylonsPlaced)
                {
                    List<int> innerBlockIndexes = new List<int>()
                    {
                        8, 9, 10, 11, 12,
                        15, 16, 17, 18, 19,
                        22, 23, 24, 25, 26,
                        29, 30, 31, 32, 33,
                        36, 37, 38, 39, 40
                    };

                    int index1 = Game1.GameWorld.rnd.Next(innerBlockIndexes.Count);
                    Block pylonBlock1 = DistrictMap[innerBlockIndexes[index1]];

                    int index2;
                    do
                    {
                        index2 = Game1.GameWorld.rnd.Next(innerBlockIndexes.Count);
                    } while (index2 == index1);

                    Block pylonBlock2 = DistrictMap[innerBlockIndexes[index2]];

                    PylonBlock1 = pylonBlock1;
                    PylonBlock2 = pylonBlock2;


                    int roomCount = Location.AllStructures[0].Rooms.Count;
                    if (roomCount > 5)
                    {
                        PylonRoom = Location.AllStructures[0].Rooms[Game1.GameWorld.rnd.Next(5, roomCount)];
                    }
                    else
                    {
                        PylonRoom = Location.AllStructures[0].Rooms[0]; // fallback in case there's only one room
                    }

                    Object p1 = new Object(null, "pylon", new EntityList<Material>() { Game1.GameWorld.Shadesteel }, null);
                    Object p2 = new Object(null, "pylon", new EntityList<Material>() { Game1.GameWorld.Shadesteel }, null);
                    Object p3 = new Object(null, "pylon", new EntityList<Material>() { Game1.GameWorld.Shadesteel }, null);

                    PylonBlock1.Objects.Add(p1);
                    PylonBlock2.Objects.Add(p2);
                    PylonRoom.Objects.Add(p3);

                    PylonsPlaced = true;
                }
            }

            //place pylons




            //pls still do this

            foreach (Structure s in allDistrictStructures)
            {
                foreach (Object o in s.HistoricalObjects)
                {
                    s.Rooms[Game1.GameWorld.rnd.Next(s.Rooms.Count())].Objects.Add(o);
                }
                s.HistoricalObjects.Clear();
            }



            if (Location.Market != null && Location.Market.Block.District == this)
            {
                if (Location.DebtShibas.Count() == 0)
                {
                    int shibas = Game1.GameWorld.rnd.Next(4, 8);
                    for (int i = 0; i < shibas; i++)
                    {
                        Architect a = new Architect("", Game1.Sexes[Game1.GameWorld.rnd.Next(2)], Game1.GameWorld.GetRace("debtshiba"), Game1.GameWorld.rnd.Next(9999999), "debtshiba", new EntityList<Object>(), Location, this, Location.Market.Block, "", 4, false);

                        a.PopulateSelf(false);

                        a.AssignSpells();
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        a.HomeStructure = Location.Market;
                        a.Block = Location.Market.Block;
                        Location.DebtShibas.Add(a);
                        Game1.LoadedArchitects.Add(a);
                    }
                }

                Location.Market.Block.Architects.AddRange(Location.DebtShibas);
                Location.DebtShibas.Clear();
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
                string sex = Game1.GameWorld.rnd.Next(1, 3) == 1 ? "male" : "female";
                string role = Game1.WeightedRandomNormalProfessions[Game1.GameWorld.rnd.Next(Game1.WeightedRandomNormalProfessions.Count())];
                Race race;

                bool WillFightYou = false;

                if (Location.PrimaryRace.Name == "luminarch" || Location.PrimaryRace.Name == "nightfell" || Location.PrimaryRace.Name == "archaix")
                {
                    if (Game1.GameWorld.rnd.Next(1, 101) <= 95) // 95% chance
                    {
                        race = Location.PrimaryRace;
                    }
                    else
                    {
                        // Pick one of the other two humanoid races
                        EntityList<Race> otherHumanoidRaces = Game1.GameWorld.HumanoidRaces.Where(r => r.Name != Location.PrimaryRace.Name && (r.Name == "luminarch" || r.Name == "nightfell" || r.Name == "archaix"));
                        race = otherHumanoidRaces[Game1.GameWorld.rnd.Next(otherHumanoidRaces.Count())];
                    }
                }
                else if (Location.PrimaryRace.Name == "shade" || Location.PrimaryRace.Name == "isofractal" || Location.PrimaryRace.Name == "photonexus")
                {
                    race = Location.PrimaryRace;
                    WillFightYou = true;
                }
                else
                {
                    // 3:3:1 bias towards luminarchs and nightfells over archaix
                    int bias = Game1.GameWorld.rnd.Next(7); // 0-6

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

                string destiny = Game1.GameWorld.rnd.Next(1, 5000) switch
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
                    int professionRoll = Game1.GameWorld.rnd.Next(100);

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

                Architect a = new Architect("", sex, race, Game1.GameWorld.rnd.Next(14, 90), role, new EntityList<Object>(), Location, this, null, destiny, WillFightYou ? 2 : 1, false);
                a.PopulateSelf(Game1.GameWorld.HumanoidRaces.Contains(a.Race));
                a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                allArchitectsThatIThinkNeedSpecialProcessing.Add(a);
                a.ImportantThisLoad = false;
            }


            UnplacedPopulation = 0;

            if (Location.Market != null && Location.Market.Block.District == this)
            {
                foreach (Group g in Location.TradersAtThisLocation)
                {
                    // Do specificlaly market items


                    EntityList<Object> CaravanItems = new EntityList<Object>();

                    foreach (string itemString in g.CaravanItems)
                    {
                        EntityList<Object> items = Game1.ConvertStringToObjects(itemString);
                        CaravanItems.AddRange(items);
                    }

                    g.CaravanItems.Clear();

                    // Determine the target room for each item
                    foreach (Object item in CaravanItems)
                    {
                        Room targetRoom = Location.Market.Rooms[Game1.r.Next(Location.Market.Rooms.Count)];
                        targetRoom.Objects.Add(item);
                        item.Owner = g;
                        item.UpdateNames(false, null, true);
                    }





                    foreach (Architect a in g.Architects)
                    {
                        if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                        {
                            Location.Market.Rooms[0].Architects.Add(a);
                            a.Room = Location.Market.Rooms[0];
                            a.Block = a.Room.Structure.Block;
                            a.Location = this.Location;
                            a.IsLoadedTrader = true;
                            Game1.LoadedArchitects.Add(a);
                            a.PopulateSelf(true);
                        }
                    }
                }
            }


            if (Game1.MostRecentPartyTurnArchitect.District == null)
            {
                Game1.MostRecentPartyTurnArchitect.District = this; 
                Game1.MostRecentPartyTurnArchitect.DistrictMarked = true;
            }

            if (Location.Prism != null && Game1.GameWorld.SettlementTypes.Contains(Location.Type))
            {
                allDistrictStructures.Add(Location.Prism);

                if (Location.Prism.Block.District == this)
                {
                    foreach (Room r in Location.Prism.Rooms)
                    {
                        r.Objects.Add(new Object(null, "shock mine", new EntityList<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                    }
                }
            }

            //i hate this issue

            foreach (Structure s in Location.AllStructures)
            {
                if (s.Block.District == this && s.Rooms.Count == 0)
                {
                    s.Rooms.Add(new Room(s, new EntityList<Object>(), new EntityList<Architect>(), new EntityList<Architect>()));
                    s.Rooms[0].PopulateRoom();
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
                            ? Game1.GameWorld.ConstructRaces[Game1.GameWorld.rnd.Next(Game1.GameWorld.ConstructRaces.Count())]
                            : Location.GuardianType;

                        Architect a = new Architect("", Game1.Sexes[Game1.GameWorld.rnd.Next(2)], race, 10, "construct", new EntityList<Object>(), Location, this, b, "", 5, false);
                        a.PopulateSelf(false);
                        a.Inventory.Add(Game1.GameWorld.MagicalSuperLoot(Game1.GameWorld.rnd.Next(3, 7)));
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        a.HomeLocation = Location;

                        // Get rooms with fewer than 2 constructs
                        var validRooms = structureInSameDistrict.Rooms
                            .Where(r => r.Architects.Count() < 2)
                            .ToList();

                        // If there are valid rooms, pick one randomly
                        if (validRooms.Count > 0)
                        {
                            a.Room = validRooms[Game1.GameWorld.rnd.Next(validRooms.Count)];
                        }
                        else
                        {
                            // Fallback: place in a random room anyway
                            a.Room = structureInSameDistrict.Rooms[Game1.GameWorld.rnd.Next(structureInSameDistrict.Rooms.Count)];
                            // Optional: log or flag that this is overflow
                        }

                        a.Block = a.Room.Structure.Block;
                        a.Room.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);

                        a.AddToNumberOnReload = true;
                    }

                    Location.GuardiansInNetwork = 0;

                    List<Room> untakenRooms = structureInSameDistrict.Rooms.Skip(1).ToList(); // All rooms except the first
                    List<Room> usedRooms = new List<Room> { structureInSameDistrict.Rooms[0] }; // Start with room[0] used


                    for (int i = Location.AnimalsInNetwork; i > 0; i--)
                    {
                        Block b = structureInSameDistrict.Block;
                        Race race = Location.AnimalRace;

                        Architect a = new Architect("", Game1.Sexes[Game1.GameWorld.rnd.Next(2)], race, 10, "beast", new EntityList<Object>(), Location, this, b, "", 2, false);
                        a.Name = Game1.GameWorld.GenerateUniqueArchitectName(a);
                        a.PopulateSelf(false);

                        if (i == Location.AnimalsInNetwork) // First animal → first room
                        {
                            a.Room = structureInSameDistrict.Rooms[0];
                        }
                        else if (untakenRooms.Count > 0) // Randomly choose from remaining untaken rooms
                        {
                            int index = Game1.GameWorld.rnd.Next(untakenRooms.Count);
                            a.Room = untakenRooms[index];
                            usedRooms.Add(a.Room);
                            untakenRooms.RemoveAt(index);
                        }
                        else // If all rooms are used, allow stacking
                        {
                            a.Room = structureInSameDistrict.Rooms[Game1.GameWorld.rnd.Next(structureInSameDistrict.Rooms.Count)];
                        }

                        a.Block = a.Room.Structure.Block;
                        a.HomeLocation = Location;
                        a.Room.Architects.Add(a);
                        Game1.LoadedArchitects.Add(a);

                        a.AddToNumberOnReload = true;
                    }


                    Location.AnimalsInNetwork = 0;
                }
            }




            foreach (Architect a in allArchitectsThatIThinkNeedSpecialProcessing)
            {
                if(a.Location != this.Location)
                {
                    //somethieng weird is happening and you shouldn't be here

                    continue;
                }

                a.District = this;
                a.DistrictMarked = true;


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
                        a.Profession = Game1.GameWorld.rnd.Next(100) < 20 ? "manager" : "perfectionist";
                    }
                    else if (a.Race.Name == "isofractal")
                    {
                        a.Profession = Game1.GameWorld.rnd.Next(100) < 20 ? "curator" : "artist";
                    }
                    else if (a.Race.Name == "shade")
                    {
                        a.Profession = Game1.GameWorld.rnd.Next(100) < 20 ? "cluster" : "brute";
                    }
                }



                List<Plan> PlansIShouldBeWorriedAbout = Game1.GameWorld.AllFactions
    .Where(f => f.CurrentPlan != null && f.CurrentPlan.PlanLocation == this.Location)
    .Select(f => f.CurrentPlan)
    .ToList();


                if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a) && a.Race.Name != "debtshiba" && a.NextMigrationLocation == null && !a.IsLoadedTrader)
                {
                    a.Loaded = true;
                    a.UpdateNames();

                    EntityList<Structure> possibleStructures = GetPossibleStructures(a);


                    if (PlansIShouldBeWorriedAbout.Any(p => p.PlanInitiators.Contains(a)))
                    {
                        Block b = DistrictMap[outskirtsBlockIndexes[Game1.r.Next(outskirtsBlockIndexes.Count)]];
                        b.Architects.Add(a);
                        a.Block = b;

                        a.Task = "idle";
                        a.CyclesLeftInTask = 9999999;

                        Game1.PlanLoadedArchitects.Add(a);
                    }
                    else if (PylonBlock1 != null && PylonBlock1.Objects.Where(o => o.Type == "pylon").Count > 0 &&
         PylonBlock1.Architects.Count(a => a.IsCalamity) < 3 &&
         a.IsCalamity &&
         a != Game1.GameWorld.Calamity[0] &&
         (!a.SpellsKnown.Any() || PylonBlock1.Architects.Count(x => x.SpellsKnown.Count > 0) == 0))
                    {
                        PylonBlock1.Architects.Add(a);
                        a.Block = PylonBlock1;
                        a.Task = "idle";
                        a.CyclesLeftInTask = 9999999;
                    }
                    else if (PylonBlock2 != null && PylonBlock2.Objects.Where(o => o.Type == "pylon").Count > 0 &&
                             PylonBlock2.Architects.Count(a => a.IsCalamity) < 3 &&
                             a.IsCalamity &&
                             a != Game1.GameWorld.Calamity[0] &&
                             (!a.SpellsKnown.Any() || PylonBlock2.Architects.Count(x => x.SpellsKnown.Count > 0) == 0))
                    {
                        PylonBlock2.Architects.Add(a);
                        a.Block = PylonBlock2;
                        a.Task = "idle";
                        a.CyclesLeftInTask = 9999999;
                    }
                    else if (PylonRoom != null && PylonRoom.Objects.Where(o => o.Type == "pylon").Count > 0 &&
                             PylonRoom.Architects.Count(a => a.IsCalamity && Game1.GameWorld.ConstructRaces.Contains(a.Race)) < 3 &&
                             a.IsCalamity &&
                             a != Game1.GameWorld.Calamity[0] &&
                             (!a.SpellsKnown.Any() || PylonRoom.Architects.Count(x => x.SpellsKnown.Count > 0) == 0))
                    {
                        PylonRoom.Architects.Add(a);
                        a.Block = PylonRoom.Structure.Block;
                        a.Room = PylonRoom;
                        a.Task = "idle";
                        a.CyclesLeftInTask = 9999999;
                    }

                    else if (possibleStructures.Count() > 0 && (Game1.GameWorld.rnd.Next(3) != 0 || possibleStructures[0].Type == "tomb" || possibleStructures[0].Type == "core"))
                    {
                        Structure chosenStructure = possibleStructures[Game1.GameWorld.rnd.Next(possibleStructures.Count())];
                        Room chosenRoom;

                        if (Game1.GameWorld.Calamity[0] == a)
                        {
                            // Ensure we choose a room that is NOT the first one
                            int roomCount = chosenStructure.Rooms.Count;
                            if (roomCount > 5)
                            {
                                chosenRoom = chosenStructure.Rooms[Game1.GameWorld.rnd.Next(5, roomCount)];
                            }
                            else
                            {
                                chosenRoom = chosenStructure.Rooms[0]; // fallback in case there's only one room
                            }
                        }
                        else
                        {
                            chosenRoom = chosenStructure.Type == "tomb"
                                ? chosenStructure.Rooms[Game1.GameWorld.rnd.Next(chosenStructure.Rooms.Count)]
                                : chosenStructure.Rooms[0];
                        }

                        chosenRoom.Architects.Add(a);
                        a.Room = chosenRoom;
                        a.Block = chosenRoom.Structure.Block;
                    }
                    else if (a.IsCalamity == false || Game1.GameWorld.rnd.Next(6) == 0)
                    {
                        Block b = DistrictMap[Game1.GameWorld.rnd.Next(0, 49)];
                        b.Architects.Add(a);
                        a.Block = b;
                    }
                    else
                    {
                        Structure chosenStructure = Location.AllStructures[0];
                        Room chosenRoom = chosenStructure.Rooms[Game1.GameWorld.rnd.Next(chosenStructure.Rooms.Count())];
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
            }



            // Load General Items
            EntityList<Object> itemsToAdd = new EntityList<Object>();
            foreach (string itemString in GeneralItemsWeHave)
            {
                EntityList<Object> items = Game1.ConvertStringToObjects(itemString);
                itemsToAdd.AddRange(items);
            }

            GeneralItemsWeHave.Clear();

            // Determine the target room for each item
            foreach (Object item in itemsToAdd)
            {
                Room targetRoom = Location.Market != null && Location.Market.Block.District == this && Game1.GameWorld.rnd.Next(1, 3) == 1
                    ? Location.Market.Rooms[0]
                    : GetRandomRoom(allDistrictStructures);

                targetRoom.Objects.Add(item);
                item.UpdateNames(false, null, true);
            }









            Game1.LoadedArchitects.AddRange(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects);

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

                                if(o is Door d)
                                {
                                    d.IsQuickestExit = false;
                                }
                            }

                            r.FixRotation();


                            //find quickest exit door

                            Door DD = r.FindQuickestExitDoor();

                            if(DD != null)
                                DD.IsQuickestExit = true;
                        }
                    }
                }
            }

            //protect the idiots, this should save the debtshibas, beasts, and more from being addd to the list twice

            Game1.LoadedArchitects = Game1.LoadedArchitects.Distinct().ToList();


            foreach (Architect a in Game1.LoadedArchitects)
            {
                if ((a.Profession == "sovereign" || a.Profession == "heart") && !Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                {
                    a.Task = "sentinel";
                    a.CyclesLeftInTask = 99999;
                    a.Target = (a.Location, a.District, a.Block, a.Room);

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

            DistrictArchitects.Clear();
            HasBeenLoadedEver = true;
            Game1.TicksSinceLoad = 0;



            Game1.LoadedArchitects = Game1.LoadedArchitects.Distinct().ToList();

            foreach(Architect a in Game1.LoadedArchitects)
            {
                a.SpellsKnown = a.SpellsKnown.Distinct();
                a.SkillsKnown = a.SkillsKnown.Distinct();

                foreach(Object o in a.BodyParts)
                {
                    o.Owner = a;
                }

                if(a.Race.Accursed && a.Race.Name != "shade")
                {
                    a.BodyParts.Clear();
                    a.Race = Game1.GameWorld.GetRace("shade");

                    a.PopulateSelf(false);
                }

                if(a.Block != null)
                {
                    var target = a.Block.FindNearestThing("tavern");

                    if (target.Item2 == a.District)
                        a.NearestTavernThisLoad = target;
                }
            }



            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                if(a.Inventory.Any(o => o.Type == "raft"))
                {
                    a.AnnounceToParty("You are still carrying a raft. You might consider dropping it to save weight.", Color.Orange, new EntityList<Entity>());
                }

                if(a.Invisible)
                {
                    a.AnnounceToParty("You are still invisible. Watch your energy.", Color.Magenta, new EntityList<Entity>());
                }
            }


            foreach (Architect a in Game1.LoadedArchitects)
            {
                // Clean up deleted entities from ArchitectsForOpinions and their corresponding Opinions
                for (int i = a.ArchitectsForOpinions._entityIds.Count - 1; i >= 0; i--)
                {
                    int entityId = a.ArchitectsForOpinions._entityIds[i];
                    if (!Game1.GameWorld.EntityLedger.ContainsKey(entityId))
                    {
                        a.ArchitectsForOpinions._entityIds.RemoveAt(i);
                        a.Opinions.RemoveAt(i);
                    }
                }

                // Clean up deleted entities from KnownArchitects
                for (int i = a.KnownArchitects._entityIds.Count - 1; i >= 0; i--)
                {
                    int entityId = a.KnownArchitects._entityIds[i];
                    if (!Game1.GameWorld.EntityLedger.ContainsKey(entityId))
                    {
                        a.KnownArchitects._entityIds.RemoveAt(i);
                        a.KnownArchitects._cachedEntities.Clear(); // Optional: if clearing cache fully every time
                    }
                }
            }


            Game1.UpdateNonPlayerWorld();
        }

        void AddOrUpdateItemString(List<string> itemList, string itemString)
        {
            // Split into type, count, and rest (materials + maybe &cont(...)&)
            string[] itemParts = itemString.Split(new[] { ',' }, 3);
            string itemType = itemParts[0];
            int itemCount = int.Parse(itemParts[1]);
            string itemRest = itemParts[2]; // materials + possibly &cont(...)&

            // Find match by checking equality ignoring count
            string existingItem = itemList.FirstOrDefault(item =>
            {
                string[] parts = item.Split(new[] { ',' }, 3);
                return parts.Length == 3 && parts[0] == itemType && parts[2] == itemRest;
            });

            if (existingItem != null)
            {
                string[] existingItemParts = existingItem.Split(new[] { ',' }, 3);
                int existingCount = int.Parse(existingItemParts[1]);
                int newCount = existingCount + itemCount;

                itemList.Remove(existingItem);
                itemList.Add($"{itemType},{newCount},{itemRest}");
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
            Game1.LoadedFinalPointers.Clear();

            //ditch shock mines

            List<Architect> DeletedArchitects = new List<Architect>();


            if (Location.Prism != null && Location.Prism.Block.District == this)
            {
                foreach (Room r in Location.Prism.Rooms)
                {
                    foreach (var obj in r.Objects.Where(o => o.Type == "shock mine").ToList())
                    {
                        obj.Delete();
                        r.Objects.Remove(obj);
                    }
                }
            }


            //get rid of hooks and dont count them

            foreach (Block b in DistrictMap)
            {
                foreach(Architect a in b.Architects)
                {
                    if(Game1.LoadedHooks.Contains(a))
                    {
                        b.ArchitectsToRemove.Add(a);
                    }
                }
                foreach(Architect a in b.ArchitectsToRemove)
                {
                    b.Architects.Remove(a);
                }
                b.ArchitectsToRemove.Clear();

                //dont save scaffold objects
                foreach (var obj in b.Objects.Where(o => o.Type == "scaffold").ToList())
                {
                    obj.Delete();
                    b.Objects.Remove(obj);
                }
            }

            if (Location.Market != null)
                Location.Market.MarketDebtToUs = 0;


            Game1.LoadedHooks.Clear();
            Game1.GaveInactiveWarningMessage = false;


            // Remove tasks
            foreach (Architect a in Game1.LoadedArchitects)
            {
                a.Task = "";
                a.CooldownCycles = 0;
                a.CyclesLeftInTask = 0;

                a.MovementMode = "walking";

                if (a.MainInteractionAppendage != null)
                    a.MainInteractionAppendage.OopsIDroppedIt = false;
                if (a.OffInteractionAppendage != null)
                    a.OffInteractionAppendage.OopsIDroppedIt = false;

                //clear distancing/rotoation data
                a._distances.Clear();
                a._rotations.Clear();
                a._architects.Clear();
            }

            int TraderItemsProcessed = 0;

            // Remove architects and round up the population
            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    foreach (Architect a in DistrictMap[DistrictX + DistrictZ * 7].Architects)
                    {
                        if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a))
                        {
                            if(a.AddToNumberOnReload)
                            {
                                if(Game1.GameWorld.ConstructRaces.Contains(a.Race))
                                {
                                    Location.GuardiansInNetwork++;
                                    DeletedArchitects.Add(a);
                                }
                                else if (a.IsAlive)
                                {
                                    Location.AnimalsInNetwork++;
                                    DeletedArchitects.Add(a);
                                }
                            }
                            else if (a.Transient)
                            {
                                DeletedArchitects.Add(a);
                            }
                            else if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a) && !a.IsLoadedTrader)
                            {
                                if (a.Race == Game1.GameWorld.GetRace("debtshiba"))
                                {
                                    Location.DebtShibas.Add(a);
                                    a.Task = "";
                                    a.Loaded = false;
                                }
                                else
                                {
                                    DistrictArchitects.Add(a);
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

                    // Create lists to hold the objects to remove and to add
                    List<Object> objectsToRemove = new();
                    List<Object> objectsToAddToDistrict24 = new();
                    List<Object> objectsToDelete = new();

                    // First pass: decide what to remove and what to add elsewhere
                    foreach (Object o in DistrictMap[DistrictX + DistrictZ * 7].Objects.ToList())
                    {
                        if (o.Type == "shadow fountain" || o.Type == "well" || o.Type == "chromaweaver" || o.Type == "console" || o.Type == "pylon")
                            continue;

                        bool shouldRemove = true;

                        if (o.IsGeneralGood && o.Significance == 0 && Game1.GameWorld.GlobalResidenceTypes.Contains(Location.Type) && o.HookedObjective == null && o.IAmAFinalPointerThatFollowsAfterThisObjective == null)
                        {
                            if(Location.TradersAtThisLocation.Contains(o.Owner))
                            {
                                string itemString = Game1.ConvertObjectToString(o);
                                AddOrUpdateItemString((o.Owner as Group).CaravanItems, itemString);
                                objectsToDelete.Add(o); // queue for deletion
                                TraderItemsProcessed++;
                            }
                            else
                            {
                                string itemString = Game1.ConvertObjectToString(o);
                                AddOrUpdateItemString(GeneralItemsWeHave, itemString);
                                objectsToDelete.Add(o); // queue for deletion
                            }
                        }
                        else
                        {
                            if (Location.Prism != null)
                                Location.Prism.HistoricalObjects.Add(o);
                            else if (Location.AllStructures.Count() > 0)
                                Location.AllStructures[0].HistoricalObjects.Add(o);
                            else
                            {
                                objectsToAddToDistrict24.Add(o);
                                shouldRemove = false;
                            }
                        }

                        if (shouldRemove)
                            objectsToRemove.Add(o);
                    }

                    // Second pass: remove from district
                    foreach (Object o in objectsToRemove)
                    {
                        DistrictMap[DistrictX + DistrictZ * 7].Objects.Remove(o);
                    }

                    // Final pass: delete
                    foreach (Object o in objectsToDelete)
                    {
                        o.Delete();
                    }


                    // Third pass: add to District 24
                    foreach (Object o in objectsToAddToDistrict24)
                    {
                        DistrictMap[24].Objects.Add(o);
                    }


                    // Handle structures and rooms
                    foreach (Structure s in DistrictMap[DistrictX + DistrictZ * 7].Structures)
                    {
                        foreach (Room r in s.Rooms)
                        {
                            EntityList<Architect> ArchitectsToRemove = new EntityList<Architect>();
                            foreach (Architect a in r.Architects)
                            {
                                if (a.AddToNumberOnReload)
                                {
                                    if (Game1.GameWorld.ConstructRaces.Contains(a.Race))
                                    {
                                        Location.GuardiansInNetwork++;
                                    }
                                    else if (a.IsAlive)
                                    {
                                        Location.AnimalsInNetwork++;
                                    }
                                }
                                else if (a.Transient)
                                {
                                    DeletedArchitects.Add(a);
                                }
                                else if (!Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(a) && !a.IsLoadedTrader)
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
                                        DistrictArchitects.Add(a);
                                        a.Task = "";
                                        a.Loaded = false;
                                        TotalArchitects++;
                                    }
                                }
                            }
                            r.Architects.Clear();

                            // Create a list to hold objects to remove (some will be deleted, some saved to history)
                            EntityList<Object> RoomObjectsToRemove = new();
                            EntityList<Object> RoomObjectsToDelete = new();

                            // Handle objects in the room
                            foreach (Object o in r.Objects)
                            {
                                if (o.IsGeneralGood && o.Significance == 0 && Game1.GameWorld.GlobalResidenceTypes.Contains(Location.Type) && o.HookedObjective == null && o.IAmAFinalPointerThatFollowsAfterThisObjective == null)
                                {
                                    if (Location.TradersAtThisLocation.Contains(o.Owner))
                                    {
                                        string itemString = Game1.ConvertObjectToString(o);
                                        AddOrUpdateItemString((o.Owner as Group).CaravanItems, itemString);
                                        RoomObjectsToDelete.Add(o); // queue for deletion from room
                                        TraderItemsProcessed++;
                                    }
                                    else
                                    {
                                        string itemString = Game1.ConvertObjectToString(o);
                                        AddOrUpdateItemString(GeneralItemsWeHave, itemString);
                                        RoomObjectsToDelete.Add(o); // queue for deletion from room
                                    }
                                }
                                else if (o.Name != null)
                                {
                                    s.HistoricalObjects.Add(o);
                                    RoomObjectsToRemove.Add(o); // Just remove it
                                }
                                // else: leave it in the room (like unnamed objects for shibas)
                            }

                            // Remove from room
                            foreach (Object o in RoomObjectsToRemove)
                            {
                                r.Objects.Remove(o);
                            }

                            foreach (Object o in RoomObjectsToDelete)
                            {
                                r.Objects.Remove(o);
                                o.Delete(); // Actual deletion
                            }

                        }
                    }
                }
            }


            bool AllArchitectsDeadOrInParty = Game1.LoadedArchitects.All(architect => !architect.IsAlive || Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(architect));

            if (Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent != null)
            {
                if (AllArchitectsDeadOrInParty)
                {
                    Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent.Region.Units.Remove(Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent);
                }
                Game1.GameWorld.GamePlayerAssociation.ActiveParty.CurrentEvent = null;
            }

            foreach(Architect a in Game1.LoadedArchitects)
            {
                a.Room = null;
                a.Block = null;
            }


            //if nobody knows who you are get tf out of my house


            foreach (Architect a in Game1.LoadedArchitects)
            {
                if (!a.ImportantThisLoad)
                {
                    Game1.GameWorld.AllHistoricalArchitects.Remove(a);
                    a.District.DistrictArchitects.Remove(a);
                    UnplacedPopulation++;

                    DeletedArchitects.Add(a); // Mark for deletion, but don't delete yet

                    a.ResponseDatabase.Clear();
                    a.MessagesNotRespondedTo.Clear();
                }
                else
                {
                    a.ResponseDatabase.Clear();
                    a.MessagesNotRespondedTo.Clear();
                }
            }

            foreach(Message m in Game1.MessagesThisLoad)
            {
                m.Delete();
            }
            Game1.MessagesThisLoad.Clear();

            List<int> DeletedIDs = DeletedArchitects.Select(a => a.ID).ToList();


            foreach (Architect a in Game1.LoadedArchitects.Union(Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects))
            {
                //iterate through all existing architects to detlete opinions from them
                if (DeletedArchitects.Contains(a)) continue; // but skip architects that will be deleted

                // Ensure opinions are cleared before proceeding
                for (int i = a.ArchitectsForOpinions.Count - 1; i >= 0; i--)
                {
                    // Check if the _entityId of the architect is valid (i.e., it exists in the EntityLedger)
                    if (DeletedIDs.Contains(a.ArchitectsForOpinions._entityIds[i]))
                    {
                        // Remove the entity and its associated opinion if the ID is invalid
                        a.ArchitectsForOpinions.RemoveAt(i);
                        a.Opinions.RemoveAt(i);
                    }
                }

                for (int i = a.KnownArchitects._entityIds.Count - 1; i >= 0; i--)
                {
                    int entityId = a.KnownArchitects._entityIds[i];
                    if (DeletedIDs.Contains(entityId))
                    {
                        a.KnownArchitects._entityIds.RemoveAt(i);
                        a.KnownArchitects._cachedEntities.Clear();
                    }
                }
            }


            foreach (Architect a in DeletedArchitects)
            {
                a.DepopulateSelf();
                a.Delete();
            }

            foreach(Object o in Game1.ObjectsToDeleteOnUnload)
            {
                o.Delete();
            }
            Game1.ObjectsToDeleteOnUnload.Clear();


            foreach (Architect a in Game1.LoadedAttackers)
            {
                if(a.IsAlive)
                {
                    a.NextMigrationLocation = a.HomeLocation;
                }
            }
            Game1.LoadedAttackers.Clear();


            foreach (TextStorage t in Game1.Announcements)
            {
                t.Entities._entityIds.RemoveAll(id => Game1.AnnouncementEntitiesToDeleteThisCycle.Contains(id));
            }
            Game1.AnnouncementEntitiesToDeleteThisCycle.Clear();

            Game1.LoadedArchitects.Clear();
        }
    }
}
