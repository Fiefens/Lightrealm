using Lightrealm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Color = Microsoft.Xna.Framework.Color;

namespace Lightrealm
{
    [Serializable]

    public class World : Entity
    {
        public List<string> ItemTypesInCirculation = new List<string>();

        public bool FirstNewCivPlaced = false;
        public bool SecondNewCivPlaced = false;
        public bool ThirdNewCivPlaced = false;

        public List<Object> AllWrittenContent = new List<Object>();

        public List<string> CalamityStructures = new List<string>() { "tower", "keep", "monument", "fortress" };

        int ContinentalPortMaximum = Game1.r.Next(5, 8);

        public Architect Hypernexus;
        public Architect Icosidodecahedron;
        public Architect Shadeheart;

        public (int, int) GetCalamityGrievances()
        {
            int Grievances = 0;
            HashSet<Architect> UniqueArchitects = new HashSet<Architect>(); // Using HashSet to store unique architects

            foreach (Architect a in AllArchitects)
            {
                bool hasGrievance = false; // Flag to check if the current architect has any grievances
                foreach ((Entity, string) s in a.Grievances)
                {
                    if (Calamity.Contains(s.Item1)) // Checking if the grievance is part of Calamity
                    {
                        Grievances++;
                        hasGrievance = true; // Setting the flag to true if there's a grievance
                    }
                }
                if (hasGrievance) // Only add the architect to the set if they have a grievance
                {
                    UniqueArchitects.Add(a);
                }
            }

            return (UniqueArchitects.Count, Grievances); // Returning the count of unique architects and total grievances
        }
        public void RevealNearbyTiles(int centerX, int centerZ)
        {
            // Define the radius of the detection sphere
            int radius = 3;  // Radius of 2 tiles

            // Convert hex coordinates to cube coordinates
            int ConvertToCubeX(int x, int z) => x - (z - (z & 1)) / 2;
            int ConvertToCubeY(int x, int z) => -x - z;
            int ConvertToCubeZ(int z) => z;

            // Calculate the distance between two points in cube coordinates
            int CubeDistance(int x1, int y1, int z1, int x2, int y2, int z2)
                => (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2)) / 2;

            // Iterate over all tiles within the game world
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    int cubeX = ConvertToCubeX(x, z);
                    int cubeY = ConvertToCubeY(cubeX, z);
                    int cubeZ = ConvertToCubeZ(z);

                    int centerX_cube = ConvertToCubeX(centerX, centerZ);
                    int centerY_cube = ConvertToCubeY(centerX_cube, centerZ);
                    int centerZ_cube = ConvertToCubeZ(centerZ);

                    // Calculate distance in cube coordinates
                    int distance = CubeDistance(cubeX, cubeY, cubeZ, centerX_cube, centerY_cube, centerZ_cube);

                    if (distance <= radius)
                    {
                        // Set the tile to explored
                        WorldMap[x + z * Width].Explored = true;

                        // Check if there's a location at this tile and determine if it should be revealed
                        var location = WorldMap[x + z * Width].MyLocation;
                        if (location != null)
                        {
                            if (location.Type == "outpost" && distance <= 1)
                            {
                                // Reveal Outposts within 1 tile radius
                                location.Explored = true;
                            }
                            else if (distance == 0)
                            {
                                // Reveal all other structures when directly on the tile
                                location.Explored = true;
                            }
                        }
                    }
                }
            }
        }


        public List<Architect> AllArchitects = new List<Architect>();

        public Blight Purity;

        //a world has ONE adversary. This is the guy, he will mess up everrything. BUT HE DOES RECRUIT FREINDS.
        public List<Architect> Calamity = new List<Architect>();
        public string CalamityReasoning = "";
        int CalamityStartingYear = Game1.r.Next(30, 80);
        public List<string> CalamityLore = new List<string>();
        public string CalamityIdeologicalObsession = "";

        public int ReactionModifierInt = Game1.r.Next(1, 100000);

        public List<Race> Races = new List<Race>();
        public List<Race> HumanoidRaces = new List<Race>();
        public List<Race> ExtraRaces { get; set; } = new List<Race>();
        public List<Race> ConstructRaces { get; set; } = new List<Race>();
        public List<Race> WildRaces { get; set; } = new List<Race>();

        public List<string> DeletedSpells = new List<string>();
        public List<Race> DeletedRaces = new List<Race>();
        public List<Composition> DeletedCompositions = new List<Composition>();
        public List<Material> DeletedMaterials = new List<Material>();
        public List<Object> DeletedObjects = new List<Object>();

        static void Shuffle<T>(List<T> list)
        {
            Random random = new Random();
            int n = list.Count;

            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                // Swap list[i] and list[j]
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        public bool IsNightTime()
        {
            double cycleDurationInSeconds = 0.1;
            int totalSeconds = (int)Math.Round(Cycle * cycleDurationInSeconds);

            // Calculate hours and minutes from total seconds
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;

            // Adjust for 24-hour format
            hours = hours % 24;

            // Determine if it's night time (6:00 PM to 6:00 AM)
            // 6:00 PM is 18:00 in 24-hour format
            if (hours >= 18 || hours < 6)
            {
                return true; // It's night time
            }
            else
            {
                return false; // It's day time
            }
        }

        public Dictionary<string, Entity> SubjectCatalogue = new Dictionary<string, Entity>() { };

        public int SeaLevel { get; set; }

        public List<Blight> Blights = new List<Blight>();

        public static Architect Unknown; 
        public List<string> SettlementTypes = new List<string>() { "camp", "village", "town", "city" };

        public List<(int, int, int, int)> ChaosPoints = new List<(int, int, int, int)>();

        public int TotalCrafts = 0;

        public List<Object> LootTableMachine(string TableName)
        {
            List<Object> Loot = new List<Object>();
            
            switch (TableName)
            {
                case "general": //found VERY commonly in any building really, as pilferable loot
                    Loot.AddRange(MisplacedLoot(2));
                    break;
                case "bosstreasure1": //found for <=lvl2 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(SuperLoot(2));
                    break;
                case "bosstreasure2": //found for <=lvl4 bosses , contains one super loot
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(SuperLoot(4));
                    break;
                case "bosstreasure3": //found for <=lvl6 bosses , contains one super loot
                    Loot.AddRange(MisplacedLoot(2));
                    Loot.Add(SuperLoot(6));
                    break;
                case "bosstreasure4": //found for <=lvl8 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(3));
                    Loot.Add(SuperLoot(8));
                    break;
                case "bosstreasure5": //found for <=lvl10 bosses, contains one super loot
                    Loot.AddRange(MisplacedLoot(5));
                    Loot.Add(SuperLoot(10));
                    break;
                case "magictreasure12": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(MagicalSuperLoot(2));
                    break;
                case "magictreasure34": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(MagicalSuperLoot(4));
                    break;
                case "magictreasure56": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(MagicalSuperLoot(6));
                    break;
                case "magictreasure78": //found uncommonly as a powerful reward, contains magical items this level
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(MagicalSuperLoot(8));
                    break;
                case "magictreasure910": //found uncommonly as a powerful reward, contains magical items for this level
                    Loot.AddRange(MisplacedLoot(1));
                    Loot.Add(MagicalSuperLoot(10));
                    break;
            }

            return Loot;
        }

        public void TriggerRupture(int X, int Z, Architect Activator, int radius)
        {
            int Month = (int)Math.Round((decimal)(Cycle / 24192000)) % 12 + 1;
            int Year = (int)Math.Round((decimal)(Cycle / 290304000));

            string Date = "(" + Month + "/" + Year + ")";

            Vector2 activationPoint = new Vector2(X, Z);
            float hexHeight = 1.0f; // Assuming each hexagon has a unit height, adjust as necessary
            float hexWidth = (float)(Math.Sqrt(3) / 2 * hexHeight); // Calculate width based on height

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {if (WorldMap[x + z * Width].Biome == "void" || WorldMap[x + z * Width].Biome == "ocean")
                continue;
                    // Calculate the center position of each hex
                    float offsetX = x * hexWidth + (z % 2) * (hexWidth / 2);
                    float offsetZ = z * (hexHeight * 0.75f); // 0.75 accounts for hex vertical stacking

                    Vector2 currentHexCenter = new Vector2(offsetX, offsetZ);
                    float distance = Vector2.Distance(activationPoint, currentHexCenter);

                    if (distance < radius) // Check if within the radius
                    {
                        // Sporadic effect: decreasing chance with increasing distance
                        float chance = (radius - distance) / radius; // Linear decrease in chance
                        double randomValue = Game1.r.NextDouble(); // Generate a random value between 0.0 and 1.0

                        if (randomValue < chance) // Compare the random value with the calculated chance
                        {
                            WorldMap[x + z * Width].Biome = "ethereal";

                            HistoricalEvents.Add(Date + " " + Activator.Name + " decimated the landscape of the world with a catastrophic ethereal rupture.");

                            if (WorldMap[x + z * Width].MyLocation != null)
                            {
                                HistoricalEvents.Add(Date + " " + WorldMap[x + z * Width].MyLocation.Name + " was consumed in the rupture.");

                                foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                                {
                                    foreach (Architect a in d.Architects)
                                    {
                                        HistoricalEvents.Add(Date + " " + a.Name + " was consumed in the rupture.");
                                        a.IsAlive = false;
                                    }
                                }

                                AllLocations.Remove(WorldMap[x + z * Width].MyLocation);
                                WorldMap[x + z * Width].MyLocation = null;
                            }
                        }
                    }
                }
            }
        }




        public static Dictionary<int, List<string>> RarityDistribution = new Dictionary<int, List<string>>
        {
            {1, new List<string> {"common"}},
            {2, new List<string> {"common"}},
            {3, new List<string> {"common", "uncommon"}},
            {4, new List<string> {"common", "uncommon"}},
            {5, new List<string> {"common", "uncommon"}},
            {6, new List<string> {"common", "uncommon", "rare"}},
            {7, new List<string> {"common", "uncommon", "rare"}},
            {8, new List<string> {"uncommon", "rare"}},
            {9, new List<string> {"uncommon", "rare", "epic"}},
            {10, new List<string> {"rare", "epic"}}
        };

        public static string GenerateItemRarity(int level)
        {
            if (!RarityDistribution.ContainsKey(level))
                return "Invalid level";

            var rarities = RarityDistribution[level];
            var rarityWeights = new List<int>();

            foreach (var rarity in rarities)
            {
                switch (rarity)
                {
                    case "common":
                        rarityWeights.Add(level < 8 ? 100 - (level - 1) * 10 : 0); // Decreases as level increases, until 8
                        break;
                    case "uncommon":
                        rarityWeights.Add(level < 8 ? 10 + (level - 1) * 10 : 60 + (level - 8) * 10); // Increases as level increases
                        break;
                    case "rare":
                        rarityWeights.Add(level <= 7 ? 20 + (level - 6) * 15 : 80 + (level - 8) * 10); // More likely at higher levels
                        break;
                    case "epic":
                        rarityWeights.Add(100); // Always most rare
                        break;
                }
            }

            // Select a rarity based on the weighted chances
            int totalWeight = rarityWeights.Sum();
            int randomNumber = Game1.r.Next(0, totalWeight);
            int weightSum = 0;

            for (int i = 0; i < rarities.Count; i++)
            {
                weightSum += rarityWeights[i];
                if (randomNumber < weightSum)
                    return rarities[i];
            }

            return "Error: Rarity selection failed";
        }

        public Object SuperLoot(int Level)
        {
            List<string> usefulstuff = new List<string>
{
    "sword",
    "knife",
    "greatsword",
    "battle axe",
    "axe",
    "greataxe",
    "rapier",
    "spear",
    "pike",
    "mace",
    "hammer",
    "shield",
    "whip",
    "scourge",
    "flail",
    "chain",
    "large hat",
    "small hat",
    "hood",
    "cape",
    "amulet",
    "flair",
    "left glove",
    "right glove",
    "shortsleeve shirt",
    "longsleeve shirt",
    "uppershirt",
    "straps",
    "pants",
    "shorts",
    "kilt",
    "wraps",
    "left boot",
    "right boot",
    "left shoe",
    "right shoe",
    "helmet",
    "chestplate",
    "left gauntlet",
    "right gauntlet"
};

            string ChosenItem = usefulstuff[Game1.r.Next(usefulstuff.Count)];

            List<Material> Materials = new List<Material>();

            if(Game1.AllWeapons.Contains(ChosenItem))
            {
                Materials.Add(Metals[Game1.r.Next(Metals.Count)]);
            }
            else
            {
                Materials.Add(Cloths[Game1.r.Next(Cloths.Count)]);
            }

            while(true)
            {
                if(Game1.r.Next(0,2) == 0)
                {
                    break;
                }

                int Chooser = Game1.r.Next(1, 4);

                if(Chooser == 1)
                {
                    Materials.Add(Gemstones[Game1.r.Next(Gemstones.Count)]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[Game1.r.Next(Stones.Count)]);
                }
                else
                {
                    Materials.Add(Woods[Game1.r.Next(Woods.Count)]);
                }
            }

            Object o = new Object(null, ChosenItem, Materials, null);
            o.Rarity = GenerateItemRarity(Level);
            o.ApplyImbuements(0);
            o.Name = GenerateUniqueName("1S" + Game1.r.Next(4) + "s1w", o);
            return (o);
        }

        public Object MagicalSuperLoot(int Level)
        {
            List<string> potentialMagicalItems = new List<string>
            {
                "amulet",
                "bottle",
                "jar",
                "small mug",
                "big mug",
                "small bowl",
                "big bowl",
                "small cup",
                "big cup",
                "knife",
                "chain",
                "candle",
                "scroll",
                "small chalice",
                "big chalice",
                "left gauntlet",
                "right gauntlet"
            };

            string ChosenItem = potentialMagicalItems[Game1.r.Next(potentialMagicalItems.Count)];

            List<Material> Materials = new List<Material>();

            Materials.Add(Metals[Game1.r.Next(Metals.Count)]);

            while (true)
            {
                if (Game1.r.Next(0, 2) == 0)
                {
                    break;
                }

                int Chooser = Game1.r.Next(1, 4);

                if (Chooser == 1)
                {
                    Materials.Add(Gemstones[Game1.r.Next(Gemstones.Count)]);
                }
                else if (Chooser == 2)
                {
                    Materials.Add(Stones[Game1.r.Next(Stones.Count)]);
                }
                else
                {
                    Materials.Add(Woods[Game1.r.Next(Woods.Count)]);
                }
            }

            Object o = new Object(null, ChosenItem, Materials, null);
            o.Rarity = GenerateItemRarity(Level);
            o.ApplyImbuements(0);
            o.Name = GenerateUniqueName("1S" + Game1.r.Next(4) + "s1w", o);
            return (o);
        }

        public List<Object> MisplacedLoot(int Quality)
        {
            List<Object> list = new List<Object>();

            // Initial fragments addition
            for (int i = Game1.r.Next(1, 20); i != 0; i--)
            {
                list.Add(new Object(null, "fragment", new List<Material>() { Vitalium }, null));
            }

            // Loot generation based on Quality
            for (int q = 0; q < Quality; q++)
            {
                // Generate a weapon with random material
                if (Game1.r.Next(1, 100) <= 10) // 10% chance
                {
                    var weapons = new List<string> { "sword", "knife", "greatsword", "battle axe", "axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge", "flail", "chain" };
                    list.Add(new Object(null, weapons[Game1.r.Next(weapons.Count)], new List<Material>() { Metals[Game1.r.Next(Metals.Count)] }, null));
                }

                if(Game1.r.Next(1,100) <= 4) //4 percent chance
                {
                    List<Material> m = new List<Material>() { Metals[Game1.r.Next(Metals.Count)] };

                    for (int i = Game1.r.Next(10, 30); i != 0; i--)
                    {
                        list.Add(new Object(null, "dagger", m, null));
                    }
                }

                // Generate a piece of armor with random material
                if (Game1.r.Next(1, 100) <= 5) // 5% chance
                {
                    var armors = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "pants", "large hat", "small hat", "hood", "cape", "left glove", "right glove", "shortsleeve shirt", "longsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left boot", "right boot", "left shoe", "right shoe" };
                    list.Add(new Object(null, armors[Game1.r.Next(armors.Count)], new List<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null));
                }

                // Generate a piece of jewelry with random material
                if (Game1.r.Next(1, 100) <= 15) // 15% chance
                {
                    var jewelry = new List<string> { "amulet", "flair", "cut gem", "gem" };
                    list.Add(new Object(null, jewelry[Game1.r.Next(jewelry.Count)], new List<Material>() { Gemstones[Game1.r.Next(Gemstones.Count)], Metals[Game1.r.Next(Metals.Count)] }, null));
                }

                // Generate a household item with random material
                if (Game1.r.Next(1, 100) <= 30) // 30% chance
                {
                    var householdItems = new List<string> { "small pot", "big mug", "small cup", "big bowl" };
                    list.Add(new Object(null, householdItems[Game1.r.Next(householdItems.Count)], new List<Material>() { Stones[Game1.r.Next(Stones.Count)] }, null));
                }

                // Generate a piece of clothing with random material
                if (Game1.r.Next(1, 100) <= 20) // 20% chance
                {
                    var clothes = new List<string> { "cape", "hood", "longsleeve shirt", "pants", "shortsleeve shirt", "uppershirt", "straps", "shorts", "kilt", "wraps", "left glove", "right glove", "large hat", "small hat", "right boot", "left boot", "right shoe", "left shoe" };
                    list.Add(new Object(null, clothes[Game1.r.Next(clothes.Count)], new List<Material>() { Cloths[Game1.r.Next(Cloths.Count)] }, null));
                }
            }

            return list;
        }



        public List<Location> AllLocations { get; set; } = new List<Location>();
        public List<string> UnusedCivColors = new List<string>();
        public int ChanceToAddHistoricalEventToAbridgedCatalog { get; set; } = 50;

        public string SubjectCatalogueInActuallyReadableForm;

        public string CountEntities(Dictionary<string, Entity> subjectCatalogue)
        {
            // Dictionary to store the count of each entity type
            Dictionary<string, int> entityTypeCount = new Dictionary<string, int>();

            // Iterate through the SubjectCatalogue
            foreach (var keyValue in subjectCatalogue)
            {
                var entity = keyValue.Value;

                // Check for null values
                if (entity != null)
                {
                    string entityType = entity.GetType().Name;

                    // Increment the count for the entity type
                    if (entityTypeCount.ContainsKey(entityType))
                    {
                        entityTypeCount[entityType]++;
                    }
                    else
                    {
                        entityTypeCount[entityType] = 1;
                    }
                }
            }

            // Create the result string
            string result = string.Join(", ",
                entityTypeCount.Select(kv => $"{kv.Value} {kv.Key}s"));

            return result;
        }

        public string GenerateUniqueName(string pattern, Entity e)
        {
            string GenerateName(int WordsToCombine, bool Caps)
            {
                string word = string.Empty;
                Random random = new Random();

                for (int i = 0; i < WordsToCombine; i++)
                {
                    word += Game1.Words[random.Next(Game1.Words.Count)];
                }

                if (Caps)
                {
                    word = word.Substring(0, 1).ToUpper() + word.Substring(1);
                }

                return word;
            }

            string GenerateSyllableName(int SyllablesCount, bool Caps)
            {
                string word = string.Empty;
                for (int i = 0; i < SyllablesCount; i++)
                {
                    word += Game1.Syllables[Game1.r.Next(Game1.Syllables.Count)];
                }

                if (Caps)
                {
                    word = word.Substring(0, 1).ToUpper() + word.Substring(1);
                }

                return word;
            }

            string generatedName = "";
            int repeatCount = 0;

            do
            {
                generatedName = ""; // Reset generatedName for each attempt
                foreach (char c in pattern)
                {
                    if (char.IsDigit(c))
                    {
                        repeatCount = repeatCount * 10 + (c - '0');
                    }
                    else
                    {
                        if (repeatCount == 0)
                        {
                            repeatCount = 1;
                        }

                        switch (c)
                        {
                            case 'S':
                                generatedName += GenerateSyllableName(repeatCount, true);
                                break;
                            case 'W':
                                generatedName += GenerateName(repeatCount, true);
                                break;
                            case 's':
                                generatedName += GenerateSyllableName(repeatCount, false);
                                break;
                            case 'w':
                                generatedName += GenerateName(repeatCount, false);
                                break;
                            case ' ':
                                generatedName += ' ';
                                break;
                        }
                        repeatCount = 0; // Reset repeat count after processing
                    }
                }
            } while (SubjectCatalogue.ContainsKey(generatedName)); // Continue if name is not unique

            if (!(e is World)) // Exception for 'World' because it's handled manually
            {
                SubjectCatalogue.Add(generatedName, e);
            }

            return generatedName;
        }


        public string GenerateUniqueArchitectName(Entity e)
        {
            string generatedName = "";

            while (SubjectCatalogue.ContainsKey(generatedName))
            {
                string firstName = Game1.FirstNames[Game1.r.Next(Game1.FirstNames.Count)] + Game1.NameSuffixes[Game1.r.Next(Game1.NameSuffixes.Count)];
                string lastName = ((Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(0, 1)).ToUpper() + (Game1.LastNames[Game1.r.Next(Game1.LastNames.Count)]).Substring(1).ToLower();
                //OK so this system litterally takes a last name from the list, and then takes a random letter from a random last name and replaces the first letter. Its not supposed to do that, BUT IT WORKS SO WELL WHAT

                generatedName = $"{firstName} {lastName}";
            }

            SubjectCatalogue.Add(generatedName, e);
            return generatedName;
        }

        public int Length { get; set; }
        public int Width { get; set; }

        public Region[] WorldMap { get; set; }
        
        public float[] TreeNoiseValues { get; set; }
        public float[,] TrueTreeNoiseValues()
        {
            float[,] TrueTreeNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueTreeNoise[x, z] = TreeNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueTreeNoise;
        }

        public float[] ElevationNoiseValues { get; set; }
        public float[,] TrueElevationNoiseValues()
        {
            float[,] TrueElevationNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueElevationNoise[x, z] = ElevationNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueElevationNoise;
        }

        public float[] TemperatureNoiseValues { get; set; }
        public float[,] TrueTemperatureNoiseValues()
        {
            float[,] TrueTemperatureNoise = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueTemperatureNoise[x, z] = TemperatureNoiseValues[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueTemperatureNoise;
        }

        public float[] RadialArray { get; set; }
        public float[,] TrueRadialArray()
        {
            float[,] TrueRadialArray = new float[Width, Length];
            int ArrayNum = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    TrueRadialArray[x, z] = RadialArray[ArrayNum];
                    ArrayNum++;
                }
            }
            return TrueRadialArray;
        }

        public Race GetRace(string race)
        {
            foreach(Race r in Races)
            {
                if(r.Name == race)
                {
                    return r;
                }
            }
            return null;
        }

        public int LostFoundingYear { get; set; }

        public List<string> UndiscoveredSpells { get; set; } = new List<string>();
        public List<string> UndiscoveredLegendarySpells { get; set; } = new List<string>();
        public List<string> DiscoveredSpells { get; set; } = new List<string>();
        public List<string> DiscoveredLegendarySpells { get; set; } = new List<string>();

        public List<string> WritingStyles { get; set; } = new List<string> { "profound", "poignant", "thought-provoking", "insightful", "captivating", "masterful", "evocative", "compelling", "engaging", "unique", "innovative", "skillful", "artistic", "authentic", "impactful", "riveting", "meticulous", "expressive" };
        public List<string> WritingMoods { get; set; } = new List<string> { "joyful", "melancholic", "humorous", "mysterious", "reflective", "suspenseful", "inspirational", "eloquent", "soothing", "serious", "optimistic", "nostalgic", "intense", "dark", "hopeful", "whimsical", "enthusiastic", "provocative" };
        public List<string> WritingUnderstandings { get; set; } = new List<string> { "is incredibly easy to understand.", "has some obscurities, but is very simple overall.", "stumbles over some details, but gets the important information well.", "goes off on many unnecessary tangents, but isn't too unreadable.", "floats around the original subject matter, but rambles on about somewhat related, but not important topics.", "is fairly informative, but is full of many extra unrelated opinions.", "seems very coherent, but the topics must be beyond your mind.", "doesn't have a very defined flow, and is rather difficult to read and understand", "has absolutely no coherence whatsoever." };
        public List<string> WriterTypes { get; set; } = new List<string> { "has no idea what they're talking about.", "can't pinpoint many specific instances.", "is well informed on the subject matter.", "really enjoys this subject.", "doesn't care that much about what they are writing about." };

        public int TotalWrittenObjects { get; set; }

        public int MaxAge = 0;
        public double ProsperityMultiplier = 1.0;
        public string LockedInThreat = "";

        public int LivingArchitects { get; set; }
        public int DeadArchitects { get; set; }
        public int TotalArchitects { get; set; }

        public List<Architect> Colossals { get; set; } = new List<Architect>();

        public List<Group> Groups { get; set; } = new List<Group>();
        public List<Group> TradingGroups { get; set; } = new List<Group>();
        public List<Group> GroupsToRemove { get; set; } = new List<Group>();

        public static int CalculateDistance(int x1, int z1, int x2, int z2)
        {
            Vector2 p1 = new Vector2(x1, z1);
            Vector2 p2 = new Vector2(x2, z2);
            return (int)Math.Round(Vector2.Distance(p1, p2));
        }

        public Deity LightDeity { get; set; }
        public Deity DarkDeity { get; set; }

        public List<Architect> FractalArchitects = new List<Architect>();
        public List<Object> FractalObjects = new List<Object>();

        public List<Material> Woods { get; set; } = new List<Material>();
        public List<Material> Stones { get; set; } = new List<Material>();
        public List<Material> Metals { get; set; } = new List<Material>();
        public List<Material> SpecialMetals { get; set; } = new List<Material>();
        public List<Material> Cloths { get; set; } = new List<Material>();
        public List<Material> Sheets { get; set; } = new List<Material>();
        public List<Material> Gemstones { get; set; } = new List<Material>();
        public List<Material> Sands { get; set; } = new List<Material>();
        public List<Material> Ices { get; set; } = new List<Material>();
        public List<Material> Fibers { get; set; } = new List<Material>();

        public Material Enchromalite { get; set; } = new Material("enchromalite", "metal", 3, 4, "black");
        public Material Illuminite { get; set; } = new Material("illuminite", "stone", 3, 4, "white");
        public Material Darkstone { get; set; } = new Material("darkstone", "stone", 3, 4, "black");
        public Material Prismite { get; set; } = new Material("prismite", "metal", 3, 4, "white");
        public Material Shadesteel { get; set; } = new Material("shadesteel", "metal", 3, 4, "black");
        public Material Archaeon { get; set; } = new Material("archaeon", "glass", 1337, 5, "white");
        public Material Membrane { get; set; } = new Material("membrane", "membrane", 3, 0, "gray");
        public Material Biocrystal { get; set; } = new Material("biocrystal", "stone", 3, 0, "white");
        public Material Glass { get; set; } = new Material("glass", "stone", 1, 1, "white");
        public Material Clay { get; set; } = new Material("clay", "stone", 1, 1, "brown"); 
        public Material Steel { get; set; } = new Material("steel", "metal", 3, 4, "gray");
        public Material ShadeSludge { get; set; } = new Material("shadesludge", "sludge", 5, 2, "black");
        public Material Coffee { get; set; } = new Material("coffee", "plant", 1, 1, "brown");
        public Material Tea { get; set; } = new Material("coffee", "plant", 1, 1, "green");
        public Material Vitalium { get; set; } = new Material("vitalium", "rock", 1, 1, "magenta");
        public Material Spectre { get; set; } = new Material("spectre", "metaphysic", 1, 1, "cyan");
        public Material Energy { get; set; } = new Material("energy", "metaphysic", 1, 1, "white");
        public Material Flame { get; set; } = new Material("flame", "metaphysic", 1, 1, "orange");
        public Material Void { get; set; } = new Material("void", "metaphysic", 1, 1, "purple");
        public Material Honey { get; set; } = new Material("honey", "animal", 1, 1, "orange");
        public Material Waspwax { get; set; } = new Material("waspwax", "animal", 1, 1, "yellow");

        public List<Material> CoreMaterials()
        {
            List<Material> Mats = new List<Material>()
            {
                Enchromalite, Illuminite, Darkstone, Prismite, Shadesteel, Archaeon,
                Membrane, Biocrystal, Glass, Steel, ShadeSludge, Coffee, Tea, Vitalium,
                Spectre, Energy, Flame
            };

            Mats.AddRange(Woods);
            Mats.AddRange(Stones);
            Mats.AddRange(Metals);
            Mats.AddRange(SpecialMetals);
            Mats.AddRange(Cloths);
            Mats.AddRange(Sheets);
            Mats.AddRange(Gemstones);
            Mats.AddRange(Sands);
            Mats.AddRange(Ices);
            Mats.AddRange(Fibers);

            return Mats;
        }


        public double Cycle { get; set; }

        public List<Race> ColossalTypes { get; set; } = new List<Race>();

        public List<string> HistoricalEvents { get; set; } = new List<string>();
        public List<string> AbridgedHistoricalEvents { get; set; } = new List<string>();

        public List<Civilization> Civilizations { get; set; } = new List<Civilization>();
        public int InitialCivCount { get; set; }
        public World()
        {
            // Default constructor with no parameters
        }

        public void ClaimSwathOfTerritory(Civilization c, int X, int Z, int Radius)
        {
            List<(int, int)> BannedCoords = new List<(int, int)> { ((-1 * Radius), (-1 * Radius)), (Radius, Radius), ((-1 * Radius), Radius), (Radius, (-1 * Radius)) };
            for (int x = X - Radius; x < X + (Radius + 1); x++)
            {
                for (int z = Z - Radius; z < Z + (Radius + 1); z++)
                {
                    if (!BannedCoords.Contains(((x - X), (z - Z))))
                    {
                        if (x + z * Width < Width * Length && x + z * Width >= 0)
                        {
                            if (WorldMap[x + z * Width].Biome != "void" && WorldMap[x + z * Width].Owner == null && (WorldMap[x + z * Width].MyLocation == null || WorldMap[x + z * Width].MyLocation.Type != "spire"))
                            {
                                WorldMap[x + z * Width].Owner = c;
                            }
                        }
                    }
                }
            }
        }



        public World(int width, int length, int civCount, int maxAge, string dedicatedThreat, double prosperityMultiplier)
        {
            foreach (string c in Game1.Colors)
            {
                UnusedCivColors.Add(c);
            }

            string baseName = GenerateUniqueName("1S" + Game1.r.Next(5) + "s", this);

            Name = "The Continent of " + baseName;
            ReferredToNames.Add(Name);
            ReferredToNames.Add(baseName);
            Purity = new Blight(this);

            LockedInThreat = dedicatedThreat;
            MaxAge = maxAge;
            ProsperityMultiplier = prosperityMultiplier;

            //add materials, collapsed
            //add materials, collapsed
            {
                Woods.Add(new Material("oak", "wood", 3, 0, "brown"));
                Woods.Add(new Material("spruce", "wood", 3, 0, "brown"));
                Woods.Add(new Material("birch", "wood", 3, 0, "white"));
                Woods.Add(new Material("palm", "wood", 3, 0, "brown"));
                Woods.Add(new Material("maple", "wood", 3, 0, "brown"));
                Woods.Add(new Material("walnut", "wood", 3, 0, "brown"));
                Woods.Add(new Material("cedar", "wood", 3, 0, "brown"));
                Woods.Add(new Material("hickory", "wood", 3, 0, "brown"));
                Woods.Add(new Material("ash", "wood", 3, 0, "gray"));
                Woods.Add(new Material("fir", "wood", 3, 0, "green"));

                Stones.Add(new Material("granite", "stone", 3, 0, "gray"));
                Stones.Add(new Material("marble", "stone", 3, 0, "white"));
                Stones.Add(new Material("slate", "stone", 3, 0, "gray"));
                Stones.Add(new Material("sandstone", "stone", 3, 0, "yellow"));
                Stones.Add(new Material("basalt", "stone", 3, 0, "black"));
                Stones.Add(new Material("gneiss", "stone", 3, 0, "gray"));
                Stones.Add(new Material("soapstone", "stone", 3, 0, "gray"));
                Stones.Add(new Material("diorite", "stone", 3, 0, "white"));
                Stones.Add(new Material("mudstone", "stone", 3, 0, "brown"));

                Metals.Add(new Material("iron", "metal", 3, 2, "gray"));
                Metals.Add(new Material("copper", "metal", 1, 1, "brown"));
                Metals.Add(new Material("silver", "metal", 2, 3, "gray"));
                Metals.Add(new Material("gold", "metal", 2, 3, "yellow"));
                Metals.Add(new Material("titanium", "metal", 4, 4, "gray"));
                Metals.Add(new Material("platinum", "metal", 2, 4, "white"));
                Metals.Add(new Material("iridium", "metal", 4, 5, "gray"));
                Metals.Add(new Material("steel", "metal", 5, 5, "gray"));

                Cloths.Add(new Material("fleece", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("silk", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("cotton", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("linen", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("wool", "cloth", 3, 4, "white"));
                Cloths.Add(new Material("leather", "cloth", 3, 4, "brown"));
                Cloths.Add(new Material("hemp", "cloth", 3, 4, "brown"));
                Cloths.Add(new Material("flax", "cloth", 3, 4, "brown"));
                Cloths.Add(new Material("jute", "cloth", 3, 4, "brown"));

                Sheets.Add(new Material("parchment", "cloth", 3, 4, "white"));
                Sheets.Add(new Material("papyrus", "cloth", 3, 4, "white"));
                Sheets.Add(new Material("enchromalite", "cloth", 3, 4, "white"));

                Gemstones.Add(new Material("ruby", "gemstone", 3, 4, "red"));
                Gemstones.Add(new Material("diamond", "gemstone", 3, 4, "white"));
                Gemstones.Add(new Material("emerald", "gemstone", 3, 4, "green"));
                Gemstones.Add(new Material("sapphire", "gemstone", 3, 4, "blue"));
                Gemstones.Add(new Material("aquamarine", "gemstone", 3, 4, "lightblue"));
                Gemstones.Add(new Material("peridot", "gemstone", 3, 4, "green"));
                Gemstones.Add(new Material("tanzanite", "gemstone", 3, 4, "purple"));
                Gemstones.Add(new Material("turquoise", "gemstone", 3, 4, "cyan"));
                Gemstones.Add(new Material("topaz", "gemstone", 3, 4, "yellow"));
                Gemstones.Add(new Material("opal", "gemstone", 3, 4, "white"));
                Gemstones.Add(new Material("lapis lazuli", "gemstone", 3, 4, "blue"));
                Gemstones.Add(new Material("amethyst", "gemstone", 3, 4, "purple"));

                Sands.Add(new Material("red sand", "sand", 3, 4, "red"));
                Sands.Add(new Material("sand", "sand", 3, 4, "yellow"));
                Sands.Add(new Material("rocky sand", "sand", 3, 4, "brown"));

                Fibers.Add(new Material("hemp", "fiber", 3, 4, "brown"));
                Fibers.Add(new Material("flax", "fiber", 3, 4, "brown"));
                Fibers.Add(new Material("jute", "fiber", 3, 4, "brown"));

                Ices.Add(new Material("blue ice", "ice", 3, 4, "lightblue"));
                Ices.Add(new Material("crystal ice", "ice", 3, 4, "white"));
                Ices.Add(new Material("clear ice", "ice", 3, 4, "white"));
            }


            Length = length;
            Width = width;

            Shuffle(ExtraRaces);

            for(int i = Game1.r.Next(3,6); i != 0; i--)
            {
                Blights.Add(new Blight(this));
            }

            WorldMap = new Region[length * width];

            LostFoundingYear = Game1.r.Next(35, 65);

            Races = new List<Race>();

            List<(string, Material)> LuminarchBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> NightfellBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> LostBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left shoulder", Membrane), ("right shoulder", Membrane),
                ("left arm", Membrane), ("right arm", Membrane),
                ("left hand", Membrane), ("right hand", Membrane),
                ("left leg", Membrane), ("right leg", Membrane),
                ("left foot", Membrane), ("right foot", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> IsofractalBodyParts = new List<(string, Material)>
            {
                ("core", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass)
            };
            List<(string, Material)> PhotonexusBodyParts = new List<(string, Material)>
            {
                ("core", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel)
            };
            List<(string, Material)> ShadeBodyParts = new List<(string, Material)>
            {
                ("sludge", ShadeSludge)
            };

            List<(string, Material)> MoariBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("eye", Membrane), ("neck", Membrane),
                ("left front leg", Membrane), ("right front leg", Membrane),
                ("left back leg", Membrane), ("right back leg", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> ShibaBodyParts = new List<(string, Material)>
            {
                ("head", Membrane), ("left eye", Membrane), ("right eye", Membrane), ("neck", Membrane),
                ("left front leg", Membrane), ("right front leg", Membrane),
                ("left back leg", Membrane), ("right back leg", Membrane),
                ("torso", Membrane)
            };

            List<(string, Material)> CassartraeBodyParts = new List<(string, Material)>
            {
                ("core", Steel)
            };

            Races.Add(new Race("", "average", new List<(string, Material)>(), "white", new List<string>() { }, new List<string>() { }, 0));

            Races.Add(new Race("luminarch", "average", LuminarchBodyParts, "white", new List<string>() {"head", "torso", "neck"}, new List<string>() { "allevil" }, 0));
            Races.Add(new Race("nightfell", "average", NightfellBodyParts, "black", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0));
            Races.Add(new Race("archaix", "average", LostBodyParts, "gray", new List<string>() { "head", "torso", "neck" }, new List<string>() { "allevil" }, 0));
            HumanoidRaces.AddRange(new List<Race>() { Races[1], Races[2], Races[3] });

            Races.Add(new Race("moari", "large", MoariBodyParts, "white", new List<string>() { "head", "torso", "neck" }, new List<string>() {}, 30));
            Races.Add(new Race("cassartrae", "smaller", CassartraeBodyParts, "black", new List<string>() { "core" }, new List<string>() { "allevil" }, 30));
            Races.Add(new Race("debtshiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "indebted" }, 1337));
            Races.Add(new Race("shiba", "medium", ShibaBodyParts, "orange", new List<string>() { "head", "torso" }, new List<string>() { "allevil" }, 1337));

            Races.Add(new Race("isofractal", "average", IsofractalBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allevil" }, 15));
            Races.Add(new Race("photonexus", "small", PhotonexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allunalike" }, 25));
            Races.Add(new Race("shade", "average", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 5));
            ExtraRaces.Add(GetRace("isofractal"));
            ExtraRaces.Add(GetRace("photonexus"));
            ExtraRaces.Add(GetRace("shade"));

            List<(string, Material)> IcosidodecahedronParts = new List<(string, Material)>
            {
                ("core", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass), ("shard", Glass)
            };
            List<(string, Material)> HypernexusBodyParts = new List<(string, Material)>
            {
                ("core", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel), ("sphere", Steel)
            };
            List<(string, Material)> ShadeheartBodyParts = new List<(string, Material)>
            {
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge),
                ("sludge", ShadeSludge)
            };

            Races.Add(new Race("icosidodecahedron", "huge", IcosidodecahedronParts, "gray", new List<string>() { "core" }, new List<string>() { "allevil" }, 110));
            Races.Add(new Race("hypernexus", "huge", HypernexusBodyParts, "gray", new List<string>() { "core" }, new List<string>() { "allunalike" }, 125));
            Races.Add(new Race("shadeheart", "huge", ShadeheartBodyParts, "black", new List<string>() { "sludge" }, new List<string>() { "alllife" }, 80));

            Races.Add(new Race("shadebeast", "large", ShadeBodyParts, "black", new List<string>() { "sludge" }, new List<string>(){ "alllife" }, 30));

            for(int i = Game1.r.Next(5,10); i != 0; i--)
            {
                Material ChosenMetal = Metals[Game1.r.Next(Metals.Count)];
                Race r = new Race("", new List<string>() { "medium", "average", "large" }[Game1.r.Next(3)], new List<(string, Material)>() { ("head", ChosenMetal), ("body", ChosenMetal), ("front left leg", ChosenMetal), ("front right leg", ChosenMetal), ("back left leg", ChosenMetal), ("back right leg", ChosenMetal), ("tail", ChosenMetal) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body"}, new List<string>(){ "allunalike" }, 50);
                r.Name = GenerateUniqueName("8s", r) + " construct";
                Races.Add(r);
                ConstructRaces.Add(r);
            }

            ColossalTypes.Add(new Race("quetzal", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left wing", Membrane), ("right wing", Membrane), ("tail", Membrane), ("left leg", Biocrystal), ("right leg", Biocrystal) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 50));
            ColossalTypes.Add(new Race("wyrm", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("front left fin", Membrane), ("front right fin", Membrane), ("back left fin", Membrane), ("back right fin", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 60));
            ColossalTypes.Add(new Race("serpent", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 70));
            ColossalTypes.Add(new Race("shobe", "colossal", new List<(string, Material)>() { ("head", Membrane), ("body", Membrane), ("left front leg", Membrane), ("right front leg", Membrane), ("left back leg", Membrane), ("right back leg", Membrane), ("tail", Membrane) }, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "head", "body" }, new List<string>() { "allunalike" }, 80));
            ColossalTypes.Add(new Race("cnidriarch", "colossal", new List<(string, Material)>() { ("bell", Membrane), ("mantle", Membrane) }.Concat(Enumerable.Range(1, 12).Select(i => ($"tentacle{i}", Membrane))).ToList(), Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string>() { "bell", "mantle" }, new List<string>() { "allunalike" }, 50));
            Races.AddRange(ColossalTypes);

            //generate random animal races

            for (int i = Game1.r.Next(15, 25); i != 0; i--)
            {
                List<(string, Material)> BodyParts = new List<(string, Material)>();

                // Randomizing the number of legs, wings, and other body parts
                int Legs = Game1.r.Next(1, 6) * 2;
                int Wings = Game1.r.Next(0, 3); // Fixed syntax error here
                int Horns = RandomChance(10) ? Game1.r.Next(1, 3) : 0; // 10% chance to have 1-2 horns
                int Eyes = Game1.r.Next(1, 6);
                int Antennae = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Tentacles = RandomChance(10) ? Game1.r.Next(1, 5) : 0;
                int Tails = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Humps = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Fins = RandomChance(10) ? Game1.r.Next(1, 4) : 0;
                int Tusks = RandomChance(10) ? Game1.r.Next(1, 3) : 0;
                int Spikes = RandomChance(10) ? Game1.r.Next(1, 6) : 0; // 10% chance to have 1-6 spikes
                int Teeth = RandomChance(80) ? Game1.r.Next(12, 48) : 0;

                // Randomizing other attributes
                string Size = Game1.AnimalSizes[Game1.r.Next(Game1.AnimalSizes.Count)];
                bool HasScales = Game1.r.Next(0, 2) == 1;
                bool HasGills = Game1.r.Next(0, 2) == 1;
                bool HasFur = Game1.r.Next(0, 2) == 1;
                bool HasFeathers = Game1.r.Next(0, 2) == 1;
                bool HasHooves = Game1.r.Next(0, 2) == 1;
                bool HasShell = Game1.r.Next(0, 2) == 1;

                // Always include head and body
                BodyParts.Add(("head", Membrane));
                BodyParts.Add(("body", Membrane));

                // Add extra body parts based on the generated integers
                for (int j = 0; j < Legs; j++)
                    BodyParts.Add(("leg", Membrane));

                for (int j = 0; j < Wings; j++)
                    BodyParts.Add(("wing", Membrane));

                for (int j = 0; j < Horns; j++)
                    BodyParts.Add(("horn", Biocrystal));

                for (int j = 0; j < Eyes; j++)
                    BodyParts.Add(("eye", Membrane));

                for (int j = 0; j < Antennae; j++)
                    BodyParts.Add(("antenna", Membrane));

                for (int j = 0; j < Tentacles; j++)
                    BodyParts.Add(("tentacle", Membrane));

                for (int j = 0; j < Tails; j++)
                    BodyParts.Add(("tail", Membrane));

                for (int j = 0; j < Humps; j++)
                    BodyParts.Add(("hump", Membrane));

                for (int j = 0; j < Fins; j++)
                    BodyParts.Add(("fin", Membrane));

                for (int j = 0; j < Tusks; j++)
                    BodyParts.Add(("tusk", Biocrystal));

                for (int j = 0; j < Spikes; j++)
                    BodyParts.Add(("spike", Biocrystal));

                for (int j = 0; j < Teeth; j++)
                    BodyParts.Add(("tooth", Membrane));

                // Add to list (assuming GenerateUniqueName and Race constructor are defined elsewhere)
                Race r = new Race("", Size, BodyParts, Game1.Colors[Game1.r.Next(Game1.Colors.Count)], new List<string> { "head", "body" }, new List<string>() { "allunalike" }, Game1.r.Next(10,20));
                r.Name = GenerateUniqueName("1S" + Game1.r.Next(5) + "s", r);
                WildRaces.Add(r);
            }

            // Helper method to determine if a feature is present based on a percentage chance
            bool RandomChance(int percent)
            {
                return Game1.r.Next(0, 100) < percent;
            }

            // Add the RandomSize method and other necessary components as per your game's requirements




            float scale;

            LightDeity = new Deity(GenerateUniqueName("1S"+Game1.r.Next(7, 9)+"s", LightDeity), "light");
            DarkDeity = new Deity(GenerateUniqueName("1S" + Game1.r.Next(7, 9) + "s", DarkDeity), "dark");

            InitialCivCount = civCount;

            ElevationNoiseValues = new float[Width * Length];

            scale = 0.01f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array1 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.02f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array2 = SimplexNoise.Noise.Calc2D(width, length, scale);
            scale = 0.08f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] Array3 = SimplexNoise.Noise.Calc2D(width, length, scale);


            //construct radial array


            RadialArray = new float[Width * Length];
            float GreatestArrayValue = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    float Distance = CalculateDistance(Width / 2, Length / 2, x, z);
                    RadialArray[x + z * Width] = Distance;
                    if (Distance > GreatestArrayValue)
                    {
                        GreatestArrayValue = Distance;
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    RadialArray[x + z * Width] = (((1 - (RadialArray[x + z * Width] / GreatestArrayValue)) * 256));
                }
            }
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Length; z++)
                {
                    ElevationNoiseValues[x + z * Width] = ((((Array1[x, z] + Array2[x, z] + Array3[x, z]) / 3) + RadialArray[x + z * Width]) / 2);
                }
            }

            float[] ConvertTo1DArray(float[,] array2D)
            {
                int length = array2D.GetLength(1);
                int width = array2D.GetLength(0);
                float[] array1D = new float[length * width];

                int index = 0;
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        array1D[index++] = array2D[x, z];
                    }
                }

                return array1D;
            }

            scale = 0.08f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] treeNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TreeNoiseValues = ConvertTo1DArray(treeNoise2D);

            scale = 0.05f;
            SimplexNoise.Noise.Seed = Game1.r.Next(0, 10000000);
            float[,] temperatureNoise2D = SimplexNoise.Noise.Calc2D(width, length, scale);
            TemperatureNoiseValues = ConvertTo1DArray(temperatureNoise2D);


            foreach (Race r in Races)
            {
                r.RaceLetter = Race.GenerateUniqueAbbreviation(r.Name, this.Races);
            }

            //utilize elevation map

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    int Elevation = (int)Math.Round(ElevationNoiseValues[x + z * Width] / 2.56);
                    int Temperature = (int)Math.Round(TemperatureNoiseValues[x + z * Width] / 2.56);
                    int Trees = (int)Math.Round(TreeNoiseValues[x + z * Width] / 2.56);

                    string Biome = "";

                    if (RadialArray[x + z * Width] < 70)
                    {
                        Biome = "void";
                    }
                    else if (Elevation > 75)
                    {
                        Biome = "snowpeak";
                    }
                    else if (Elevation > 70)
                    {
                        Biome = "mountain";
                    }
                    else if (Elevation < 50)
                    {
                        Biome = "ocean";
                    }
                    else
                    {
                        if (Temperature > 70)
                        {
                            Biome = "desert";
                        }
                        else if (Temperature < 30)
                        {
                            if (Trees > 50)
                            {
                                Biome = "taiga";
                            }
                            else
                            {
                                Biome = "tundra";
                            }
                        }
                        else
                        {
                            if (Trees > 60)
                            {
                                Biome = "forest";
                            }
                            else if (Trees > 50)
                            {
                                Biome = "lightforest";
                            }
                            else
                            {
                                Biome = "plains";
                            }
                        }
                    }

                    WorldMap[x + z * Width] = new Region(Biome, Elevation, Temperature, x, z, this);

                    //set to purity

                    WorldMap[x + z * Width].Blight = Purity;
                }
            }
        }

        public void ProgressOneMonth()
        {
            Random r = new Random();

            int Month = (int)Math.Round((decimal)(Cycle / 24192000)) % 12 + 1;
            int Year = (int)Math.Round((decimal)(Cycle / 290304000));

            string Date = "(" + Month + "/" + Year + ")";

            int CurrentlyCountingArchitects = 0;

            void DetectIslandsAndPorts(Region[] worldMap, int width, List<List<Region>> islands, List<List<Region>> portLocations)
            {
                int length = worldMap.Length / width; // Assuming worldMap.Length is a multiple of width
                bool[] visited = new bool[worldMap.Length];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < length; y++)
                    {
                        if (!visited[x + y * width] && worldMap[x + y * width].Biome != "ocean" && worldMap[x + y * width].Biome != "void")
                        {
                            List<Region> currentIsland = new List<Region>();
                            DFS(worldMap, x, y, width, visited, currentIsland);
                            islands.Add(currentIsland);

                            // Find potential port locations for this island
                            List<Region> currentPorts = FindPotentialPorts(worldMap, width, currentIsland);
                            portLocations.Add(currentPorts);
                        }
                    }
                }
            }

            void DFS(Region[] worldMap, int x, int y, int width, bool[] visited, List<Region> currentIsland)
            {
                int length = worldMap.Length / width; // Calculate length based on total size and width
                if (x < 0 || y < 0 || x >= width || y >= length || visited[x + y * width] || worldMap[x + y * width].Biome == "ocean" || worldMap[x + y * width].Biome == "void")
                    return;

                visited[x + y * width] = true; // Mark this region as visited
                currentIsland.Add(worldMap[x + y * width]);

                // Explore neighbors (up, down, left, right)
                DFS(worldMap, x + 1, y, width, visited, currentIsland);
                DFS(worldMap, x - 1, y, width, visited, currentIsland);
                DFS(worldMap, x, y + 1, width, visited, currentIsland);
                DFS(worldMap, x, y - 1, width, visited, currentIsland);
            }

            List<Region> FindPotentialPorts(Region[] worldMap, int width, List<Region> island)
            {
                List<Region> ports = new List<Region>();
                int length = worldMap.Length / width;
                foreach (Region region in island)
                {
                    int x = region.X;
                    int y = region.Z;
                    // Check all four directions for ocean biomes
                    foreach (var (dx, dy) in new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int newX = x + dx;
                        int newY = y + dy;

                        if (newX >= 0 && newY >= 0 && newX < width && newY < length && worldMap[newX + newY * width].Biome == "ocean")
                        {
                            if (!ports.Contains(worldMap[newX + newY * width]))
                                ports.Add(worldMap[newX + newY * width]);
                        }
                    }
                }
                return ports;
            }


            // Handle the first month
            if (Cycle == 0)
            {
                Unknown = new Architect("someone lost to time.", null, GetRace(""), 0, null, null, null, null, null, null, 0);
                SubjectCatalogue.Add("someone lost to time", Unknown);

                UndiscoveredSpells.AddRange(Game1.AllSpells);
                UndiscoveredLegendarySpells.AddRange(Game1.AllLegendarySpells);

                foreach (Civilization c in Civilizations)
                {
                    Location l = new Location("village", c.PrimaryInhabiantRace, Game1.r.Next(50, 100), 1000, Game1.r.Next(4, 8), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width]);
                    AllLocations.Add(l);
                    l.IsCapital = true;
                    c.Capitol = l;
                    ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                    Block chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                    Structure Prism = new Structure("prism", new List<Object>(), new List<Room>(), chosenBlock, new List<Material> { c.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4));
                    l.AllStructures.Add(Prism);
                    chosenBlock.Structures.Add(Prism);
                    l.Prism = Prism;

                    for (int i = 0; i < Game1.r.Next(10, 20); i++)
                    {
                        Block ChosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                        Structure s = new Structure("house", new List<Object>(), new List<Room>(), ChosenBlock, new List<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4));
                        l.AllStructures.Add(s);
                        ChosenBlock.Structures.Add(s);
                    }

                    Block b = l.Districts[0].DistrictMap[r.Next(2, 6) + r.Next(2, 5) * 7];

                    b.Objects.Add(new Object(null, "well", new List<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false)); 
                    b.Objects.Add(new Object(null, "shadow storage", new List<Material>() { Shadesteel }, DarkDeity));

                    WorldMap[c.StartX + c.StartZ * Width].MyLocation = l;
                }

                //generate the legendary colossals

                int ColossalCount = Game1.r.Next(8, 12);

                for (int i = 0; i < ColossalCount; i++)
                {

                    Architect a = new Architect("", Game1.Sexes[Game1.r.Next(Game1.Sexes.Count)], ColossalTypes[Game1.r.Next(ColossalTypes.Count)], 0, "end", new List<Object>(), null, null, null, null, 7);

                    string Name = "";

                    if (Game1.r.Next(1, 3) == 1)
                    {
                        Name = GenerateUniqueName("1S" + Game1.r.Next(2, 10) + "s", a);
                    }
                    else
                    {
                        Name = GenerateUniqueName("1S" + Game1.r.Next(2, 5) + "s 1S" + Game1.r.Next(2, 5) + "s", a);
                    }

                    a.Name = Name;
                    a.IsColossal = true;

                    while (WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "ocean" || WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Biome == "void")
                    {
                        a.ColossalMinefieldX = Game1.r.Next(0, Length);
                        a.ColossalMinefieldZ = Game1.r.Next(0, Width);
                    }

                    Colossals.Add(a);

                    InteractableEvent e = new InteractableEvent(WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width], 999999, "colossal", null, new List<Architect>() { a });
                    WorldMap[a.ColossalMinefieldX + a.ColossalMinefieldZ * Width].Events.Add(e);

                    HistoricalEvents.Add(string.Concat(a.Name, ", a colossal ", a.Race.Name, ", began lying in wait in the ", Game1.DeterminePointLocation(Width, Length, a.ColossalMinefieldX, a.ColossalMinefieldZ), "."));

                    if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog) == 1)
                    {
                        AbridgedHistoricalEvents.Add(string.Concat(a.Name, ", a colossal ", a.Race.Name, ", began lying in wait in the ", Game1.DeterminePointLocation(Width, Length, a.ColossalMinefieldX, a.ColossalMinefieldZ), "."));
                    }
                }

                //superlords or whatever

                Hypernexus = new Architect("", "female", GetRace("hypernexus"), Game1.r.Next(5000, 20000), "soverign", new List<Object>(), null, null, null, null, 9);
                Hypernexus.Name = GenerateUniqueArchitectName(Hypernexus);
                Icosidodecahedron = new Architect("", "female", GetRace("icosidodecahedron"), Game1.r.Next(5000, 20000), "soverign", new List<Object>(), null, null, null, null, 9);
                Icosidodecahedron.Name = GenerateUniqueArchitectName(Hypernexus);
                Shadeheart = new Architect("", "female", GetRace("shadeheart"), Game1.r.Next(5000, 20000), "heart", new List<Object>(), null, null, null, null, 9);
                Shadeheart.Name = GenerateUniqueArchitectName(Hypernexus);

            }
            else
            {
                List<LocationBuilderPacket> LocationBuilderPackets = new List<LocationBuilderPacket>();

                //place civilizations that need to be placed.

                SubjectCatalogueInActuallyReadableForm = CountEntities(SubjectCatalogue);

                void PlaceFancyCiv(Race race)
                {
                    //find a location to start at

                    int FoundX = 0;
                    int FoundZ = 0;
                    int Tries = 0;

                    while (true)
                    {
                        int TryX = r.Next(Width);
                        int TryZ = r.Next(Length);


                        // Check a square of size 5 centered around (TryX, TryZ)
                        bool validLocation = true;
                        for (int i = TryX - 3; i <= TryX + 3 && validLocation; i++)
                        {
                            for (int j = TryZ - 3; j <= TryZ + 3 && validLocation; j++)
                            {
                                // Check if any region inside the area's Region.Location is not equal to null
                                if (i <= 0 || i >= Width || j <= 0 || j >= Length ||
                                    WorldMap[i + j * Width].Biome == "ocean" || WorldMap[i + j * Width].Biome == "void" ||
                                    WorldMap[i + j * Width].MyLocation != null)
                                {
                                    validLocation = false;
                                }
                            }
                        }

                        if (validLocation)
                        {
                            FoundX = TryX;
                            FoundZ = TryZ;
                            break;
                        }

                        Tries++;
                        if (Tries > 300)
                        {
                            throw new Exception("yehe thats not enoughe :)");
                        }
                    }

                    Civilization c = new Civilization(race, FoundX, FoundZ, this);

                    if (race.Name == "photonexus" || race.Name == "shade" || race.Name == "isofractal")
                    {
                        Location l = new Location("core", race, Game1.r.Next(50, 100), 1000, Game1.r.Next(1, 5), c.StartX, c.StartZ, c, WorldMap[c.StartX + c.StartZ * Width]);
                        AllLocations.Add(l);
                        ClaimSwathOfTerritory(c, l.X, l.Z, 2);

                        for (int i = 0; i < Game1.r.Next(10, 20); i++)
                        {
                            Block chosenBlock = l.Districts[0].DistrictMap[Game1.r.Next(0, 49)];
                            Structure s = new Structure("house", new List<Object>(), new List<Room>(), chosenBlock, new List<Material> { c.CulturalWood }, new List<string> { c.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4));
                            l.AllStructures.Add(s);
                            chosenBlock.Structures.Add(s);
                        }

                        l.Districts[0].DistrictMap[Game1.r.Next(2, 6) + Game1.r.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new List<Material> { l.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                        WorldMap[c.StartX + c.StartZ * Width].MyLocation = l;

                        if (race.Name == "photonexus")
                        {
                            l.HomeCivilization.Citizens.Add(Hypernexus);
                            l.Government = Hypernexus;
                            l.Districts[0].Architects.Add(Hypernexus);
                            HistoricalEvents.Add("The nexus of perfection, " + l.Name + ", fell to the earth, controlled by the soverign nexus " + Hypernexus.Name + ".");
                        }
                        else if (race.Name == "isofractal")
                        {
                            l.HomeCivilization.Citizens.Add(Icosidodecahedron);
                            l.Government = Icosidodecahedron;
                            l.Districts[0].Architects.Add(Icosidodecahedron);
                            HistoricalEvents.Add("The prism of expression, " + l.Name + ", was forged as a beacon of reality, manifesting under the control of " + Icosidodecahedron.Name + ".");
                        }
                        else if (race.Name == "shade")
                        {
                            l.HomeCivilization.Citizens.Add(Shadeheart);
                            l.Government = Shadeheart;
                            l.Districts[0].Architects.Add(Shadeheart);
                            HistoricalEvents.Add("The heart of corruption, " + l.Name + ", erupted from the depths of the world, establishing a cluster of ruinous veins in " + Shadeheart.Name + ".");
                        }

                    }

                }


                if (Cycle > 14515200000 && FirstNewCivPlaced == false)
                {
                    FirstNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[0]);
                }
                else if (Cycle > 29030400000 && SecondNewCivPlaced == false)
                {
                    SecondNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[1]);
                }
                else if (Cycle > 58060800000 && ThirdNewCivPlaced == false)
                {
                    ThirdNewCivPlaced = true;
                    PlaceFancyCiv(ExtraRaces[2]);
                }


                //THE ADVERSARY RISES


                if(Cycle > 290304000 * CalamityStartingYear && Calamity.Count == 0)
                {
                    List<string> FirstPartNames = new List<string>()
                    {
                        "Dusk", "Dark", "Blight", "Day", "Dawn", "Gloom", "Storm",
                        "Frost", "Mist", "Moon",
                        "Rain", "Star", "Sun", "Thorn", "Wind"
                    };
                    List<string> SecondPartNames = new List<string>()
                    {
                        "fall", "fell", "death", "rise", "veil", "thorn", "blaze", "void", "flame", "tide", "wrath", "bane"
                    };

                    List<string> Adjectives = new List<string>()
                    {
                        "Promised", "Inevitable", "Glorified", "Eternal", "Forsaken", "Unseen", "Sacred", "Fallen",
                        "Cursed", "Hallowed", "Forbidden", "Lost", "Ancient", "Endless", "Silent", "Bound", "Veiled",
                        "Doomed", "Cathartic", "Chosen", "Dark", "Collapsing", "Primeval", "Mystic"
                    };

                    var ideologicalReasonings = new Dictionary<string, List<string>>()
                    {
                        {"disease", new List<string>() {"restorebalance", "popcontrol", "revengesomething"}},
                        {"dominator", new List<string>() {"uniteorder", "enforceutopia", "endconflict"}},
                        {"purifier", new List<string>() {"startanew", "ultimatetranquility", "naturesupremacy", "eliminatecorruption"}},
                        {"killer", new List<string>() {"misguidedmercy", "freedomtodo", "notbelonging"}},
                        {"kidnapper", new List<string>() {"divinedeal", "createnewsociety", "harnesstalent"}},
                        {"corruptor", new List<string>() {"toppleorder", "spreadchaos", "newideology"}},
                        {"diplomancer", new List<string>() {"forfun", "personalhatred", "publicwrong"}},
                        {"inciter", new List<string>() {"forgestrongerworld", "profitconflict"}},
                        {"power", new List<string>() {"supremerule", "transcendmorality", "infinitewisdom"}}
                    };

                    if(LockedInThreat == "random")
                    {
                        CalamityIdeologicalObsession = new List<string>() { "disease", "dominator", "purifier", "killer", "kidnapper", "corruptor", "diplomancer", "inciter", "power" }[r.Next(9)]; //MAKE SURE WE CHANGE THIS BACK R.NEXT(0,9)
                    }
                    else
                    {
                        CalamityIdeologicalObsession = LockedInThreat;
                    }

                    if (ideologicalReasonings.ContainsKey(CalamityIdeologicalObsession))
                    {
                        var reasoningsList = ideologicalReasonings[CalamityIdeologicalObsession];
                        CalamityReasoning = reasoningsList[r.Next(reasoningsList.Count)]; // Select a random reasoning ID from the list
                    }

                    var codeNameThemes = new Dictionary<string, List<string>>
                    {
                        { "disease", new List<string> { "Plague", "Corruption", "Disaster" } },
                        { "dominator", new List<string> { "Tyrant", "Overlord", "Conqueror" } },
                        { "killer", new List<string> { "Exterminator", "Assassin", "Destroyer" } },
                        { "purifier", new List<string> { "Cleanser", "Annihilator", "Eradicator" } },
                        { "kidnapper", new List<string> { "Abductor", "Snatcher", "Harvester" } },
                        { "corruptor", new List<string> { "Defiler", "Degrader", "Influencer" } },
                        { "diplomancer", new List<string> { "Propagandist", "Misinformer", "Deceiver" } },
                        { "inciter", new List<string> { "Agitator", "Provoker", "Instigator", "Accuser" } },
                        { "power", new List<string> { "Beacon", "End", "Monolith" } }
                    };

                    Dictionary<string, string> motivationsExposition = new Dictionary<string, string>
                    {
                        {"restorebalance", "After discovering the terrible imbalance of the world,"},
                        {"popcontrol", "After realizing that life was destroying the entire world,"},
                        {"revengesomething", "Fueled by a personal vendetta against an unforgivable act,"},
                        {"uniteorder", "With a vision to unite the world under a single order,"},
                        {"enforceutopia", "Driven by the belief in creating a perfect society,"},
                        {"endconflict", "Seeking to end all conflicts and bring about lasting peace,"},
                        {"startanew", "Desiring to erase the past and start anew for a better future,"},
                        {"ultimatetranquility", "Chasing the dream of ultimate tranquility and harmony,"},
                        {"naturesupremacy", "Believing in the supremacy of nature over all creations,"},
                        {"eliminatecorruption", "Determined to eliminate corruption and purify society,"},
                        {"misguidedmercy", "Convinced that their actions are a form of misguided mercy,"},
                        {"freedomtodo", "Embracing the freedom to do anything without constraints,"},
                        {"notbelonging", "Feeling an intense sense of not belonging to this world,"},
                        {"divinedeal", "Believing they have struck a divine deal to alter the course of humanity,"},
                        {"createnewsociety", "With the ambition to create a new society from the ashes of the old,"},
                        {"harnesstalent", "Seeing the potential to harness talent for a grand vision,"},
                        {"toppleorder", "Intent on toppling the existing order to make way for chaos,"},
                        {"spreadchaos", "Thriving in the spread of chaos and disorder,"},
                        {"newideology", "Promoting a new ideology believed to be superior to all others,"},
                        {"forfun", "Engaging in manipulation and deceit just for fun,"},
                        {"personalhatred", "Driven by a deep-seated personal hatred and desire for revenge,"},
                        {"publicwrong", "Seeking justice for a public wrong that was never addressed,"},
                        {"forgestrongerworld", "Believing that conflict and challenge forge a stronger world,"},
                        {"profitconflict", "Seeing opportunity for profit in the midst of conflict,"},
                        {"supremerule", "Aspiring to supreme rule and unchecked power,"},
                        {"transcendmorality", "Believing themselves above the constraints of morality,"},
                        {"infinitewisdom", "Convinced of their own infinite wisdom and right to lead,"}
                    };

                    Dictionary<string, List<string>> motivationGoals = new Dictionary<string, List<string>>
                    {
                        { "disease", new List<string> { "sought to flood the world in a terrible plague", "aimed to unleash a devastating epidemic", "planned to spread a lethal contagion" } },
                        { "dominator", new List<string> { "dreamed of ruling with an iron fist", "sought to bring all under their dominion", "aimed to conquer and subjugate the entire world" } },
                        { "purifier", new List<string> { "intended to cleanse the world of existence", "sought to purge the island of all matter", "aimed to eradicate all that existed" } },
                        { "killer", new List<string> { "desired to destroy all life", "chose to execute all life", "sought to deliver their version of misguided, murderous justice" } },
                        { "kidnapper", new List<string> { "planned a terrible abduction spree", "aimed to capture those of value for an unknown purpose", "sought to steal people from their homes by any means necessary" } },
                        { "corruptor", new List<string> { "intended to undermine the very fabric of society", "dreamed of spreading chaos to rebuild from ashes", "sought to corrupt those he deemed responsible for the pain" } },
                        { "diplomancer", new List<string> { "manipulated the world for their personal gain", "sought to deceive and misinform for ultimate control", "aimed to twist the fabric of reason for their personal gain" } },
                        { "inciter", new List<string> { "fueled the flames of conflict for their twisted revenge", "sought to provoke war and pain", "aimed to instigate a great turmoil" } },
                        { "power", new List<string> { "aspired to attain ultimate power", "dreamed of transcending their current limitations", "sought to wield supreme unchallenged magical power" } }
                    };

                    string name = FirstPartNames[r.Next(FirstPartNames.Count)] + SecondPartNames[r.Next(SecondPartNames.Count)] + ", the " + Adjectives[r.Next(Adjectives.Count)] + " " + (codeNameThemes[CalamityIdeologicalObsession])[r.Next(codeNameThemes[CalamityIdeologicalObsession].Count)];
                    Calamity.Add(new Architect(name, Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 34), "calamity", new List<Object>(), Civilizations[r.Next(Civilizations.Count)].Capitol, null, null, "", 10));
                    Calamity[0].HomeLocation = Calamity[0].Location;
                    Calamity[0].InteractionLocation = Calamity[0].Location;
                    CalamityLore.Add(Calamity[0].Name + " was a " + Calamity[0].Race.Name + " from " + Calamity[0].HomeLocation.Name + ".");

                    List<string> expositions = new List<string>()
                    {
                        "grew up in a broken home",
                        "was dealt the bad card of life",
                        "lived through a painful parental divorce",
                        "witnessed injustice and corruption from a young age",
                        "felt isolated and misunderstood by peers",
                        "experienced betrayal by a trusted friend",
                        "suffered from chronic poverty and lack of resources",
                        "endured bullying and discrimination",
                        "lost loved ones to a senseless act of violence",
                        "faced rejection and failure repeatedly",
                        "had dreams and aspirations crushed by societal expectations",
                        "had lived through the degredation of their community",
                        "was indoctrinated with radical views from a vulnerable age",
                        "felt powerless in the face of overwhelming adversity"
                    };

                    List<string> DeterminedExpositions = new List<string>();
                    for(int i = r.Next(2,5); i != 0; i--)
                    {
                        int Index = r.Next(expositions.Count);
                        DeterminedExpositions.Add(expositions[Index]);
                        expositions.RemoveAt(Index);
                    }

                    CalamityLore.Add(Game1.Capitalize(Calamity[0].Pronoun + " " + Game1.FormatList(DeterminedExpositions) + "."));
                    CalamityLore.Add(motivationsExposition[CalamityReasoning] + " " + Calamity[0].Pronoun + " " + motivationGoals[CalamityIdeologicalObsession][r.Next(3)] + ".");

                    //find a spot to build a base

                    bool FoundSpot = false;
                    while (!FoundSpot)
                    {
                        int X = Game1.r.Next(Width);
                        int Z = Game1.r.Next(Length);
                        if (WorldMap[X + Z*Width].Biome != "ocean" && WorldMap[X + Z * Width].Biome != "void")
                        {
                            FoundSpot = true;
                            LocationBuilderPacket l = new LocationBuilderPacket(Calamity[0], X, Z, "stronghold", GetRace(""), 0, 0, Calamity[0].HomeLocation.HomeCivilization, new List<Object>(), null);
                            LocationBuilderPackets.Add(l);
                            foreach(string s in CalamityLore)
                            {
                                HistoricalEvents.Add(Date + " " + s);
                            }
                            HistoricalEvents.Add(Date + " Calamity has risen. " + Calamity[0].Name + " is upon you.");
                        }
                    }

                    Calamity[0].IsCalamity = true;
                    Calamity[0].Level = 10;

                    //set some important vars

                    if(CalamityIdeologicalObsession == "disease")
                    {
                        Calamity[0].BlightManipulated = Blights[r.Next(Blights.Count)];

                        if(Calamity[0].BlightManipulated.FoundingYear > Cycle/ 290304000)
                        {
                            Calamity[0].BlightManipulated.FoundingYear = (int)(Math.Round(Cycle / 290304000));
                            Calamity[0].BlightManipulated.Spawned = true;

                            HistoricalEvents.Add(Date + " " + Calamity[0].Name + " unleashed " + Calamity[0].BlightManipulated + " in " + Calamity[0].Location + ".");
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                        }
                        else
                        {
                            HistoricalEvents.Add(Date + " " + Calamity[0].Name + " began assisting the spread of " + Calamity[0].BlightManipulated + " in " + Calamity[0].Location + ".");
                            Calamity[0].Location.Region.Blight = Calamity[0].BlightManipulated;
                        }
                    }
                }


                //THE CALAMITY IS UPON YOU. WHAT IS IT DOING?

                if(Calamity != null)
                {
                    List<Architect> CalamitiesToAdd = new List<Architect>();

                    foreach (Architect Calamitizer in Calamity)
                    {                               
                        //age

                        Calamitizer.TerminalAge = 999999;

                        Calamitizer.AdversaryAge += (1.0/12.0);

                        //recruit peoples


                        if(Calamitizer.AdversaryAge >= Calamitizer.AdversarySpawnTime && Calamitizer.Level >= 4)
                        {
                            Calamitizer.AdversarySpawnTime = 2140000000; //prevent furutre spawns

                            int Count = 0;
                            List<string> Types = new List<string>();

                            switch (Calamitizer.Level - 2)
                            {
                                case 8:
                                    Count = r.Next(3, 6);
                                    Types.AddRange(new List<string> { "archbard", "archluminary", "archartificer", "archduelist", "warlock", "sorcerer", "elemental", "hypernexus", "icosidodecahedron", "shadeheart", "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer" });
                                    break;
                                case 6:
                                    Count = r.Next(2, 5);
                                    Types.AddRange(new List<string> { "necromancer", "spatiomancer", "perceptomancer", "conjumancer", "fractalmancer", "embezzler", "beast", "knight", "thief", "archmage", "beastmaster", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 4:
                                    Count = r.Next(2, 4);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "magician", "embezzler", "beast", "knight", "thief", "duelist", "luminary", "artificer", "bard", "mage", "largebeast", "spy", "diplomancer" });
                                    break;
                                case 2:
                                    Count = r.Next(4, 7);
                                    Types.AddRange(new List<string> { "scout", "animal", "hunter", "mercenary", "embezzler", "beast", "knight", "thief", "magician" });
                                    break;
                            }

                            for (int i = Count; i != 0; i--)
                            {
                                Architect FoundGuy = null;

                                string ChosenType = Types[r.Next(Types.Count)];
                                List<string> SearchTheWorldTypes = new List<string> { "archartificer", "archbard", "archluminary", "archmage", "artificer", "bard", "icosidodecahedron", "hypernexus", "luminary", "mage", "shadeheart", "sorcerer", "warlock" };
                                List<string> CreateYourOwnTypes = new List<string> { "animal", "beast", "beastmaster", "conjumancer", "diplomancer", "duelist", "elemental", "embezzler", "fractalmancer", "hunter", "knight", "largebeast", "magician", "mercenary", "necromancer", "perceptomancer", "scout", "spatiomancer", "spy", "thief", "archduelist" };

                                bool Breaking = false;

                                if (SearchTheWorldTypes.Contains(ChosenType))
                                {
                                    foreach (Location l in AllLocations)
                                    {
                                        foreach (District d in l.Districts)
                                        {
                                            foreach (Architect a in d.Architects)
                                            {
                                                if (a.Profession == ChosenType && !Calamity.Contains(a))
                                                {
                                                    FoundGuy = a;
                                                    d.Architects.Remove(a);
                                                    a.Level = Calamitizer.Level -= 2;
                                                    Breaking = true;
                                                    break;
                                                }
                                            }
                                            if(Breaking)
                                            {
                                                break;
                                            }
                                        }
                                        if (Breaking)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (CreateYourOwnTypes.Contains(ChosenType))
                                {
                                    //set A to a new GUY
                                    FoundGuy = new Architect("", Game1.Sexes[r.Next(Game1.Sexes.Count)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(10,80), ChosenType, new List<Object>(), null, null, null, "", Calamitizer.Level - 2);
                                    FoundGuy.Name = GenerateUniqueArchitectName(FoundGuy);
                                }
                                else
                                {
                                    throw new Exception("Unknown type. Not inside lists above.");
                                }


                                if(FoundGuy != null)
                                {
                                    //should ALWAYS happen, but on the off chance it doesnt, tragic :(
                                    FoundGuy.Level = Calamitizer.Level - 2;

                                    //moves out and moves to a new locaiton. bring the architet there, and then fixt tem um

                                    string Type = new Dictionary<int, string> { { 2, "keep" }, { 4, "tower" }, { 6, "fortress" }, { 8, "monument" } }[FoundGuy.Level];

                                    if(Calamitizer.BlightManipulated != null)
                                    {
                                        FoundGuy.BlightManipulated = Calamitizer.BlightManipulated;
                                    }

                                    bool Found = false;

                                    int X = 0;
                                    int Z = 0;

                                    while (!Found)
                                    {
                                        X = Game1.r.Next(Width);
                                        Z = Game1.r.Next(Length);

                                        if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean")
                                        {
                                            Found = true;
                                        }
                                    }

                                    FoundGuy.Inventory.AddRange(LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)));

                                    FoundGuy.Master = Calamitizer;
                                    List<string> Relations = new List<string>
        {
            "vanguard",
            "emissary",
            "disciple",
            "informant",
            "conspirator",
            "spy",
            "supplier",
            "strategist",
            "guardian",
            "harbinger",
            "mediator",
            "ally",
            "scout",
            "representative",
            "protector",
            "mentor",
            "liaison",
            "ambassador",
            "negotiator",
            "steward",
            "custodian",
            "herald"
        };
                                    FoundGuy.MasterRelation = Relations[r.Next(Relations.Count)];

                                    LocationBuilderPacket l = new LocationBuilderPacket(FoundGuy, X, Z, Type, GetRace(""), 0, 0, Civilizations[r.Next(Civilizations.Count)], LootTableMachine("bosstreasure" + Math.Round((double)(FoundGuy.Level / 2), MidpointRounding.ToPositiveInfinity)), AllLocations[r.Next(AllLocations.Count)]);
                                    LocationBuilderPackets.Add(l);

                                    HistoricalEvents.Add(Date + " " + Calamitizer.Name + " recruited " + FoundGuy.Name + " as a " + FoundGuy.MasterRelation + " to serve " + Calamitizer.ObjectivePronoun + " and the almighty " + Calamity[0].Name + ".");
                                    CalamitiesToAdd.Add(FoundGuy);
                                }
                            }
                        }

                        //do actions

                        if(r.Next(12 - Calamitizer.Level) == 0)
                        {
                            //determine whether you want to move

                            if (r.Next(10) == 1 || (CalamityIdeologicalObsession == "killer" && Calamitizer.InteractionLocation.TruePopulation() == 0))
                            {
                                //find a spot to move to
                                int Tries = 0;
                                while (Tries < 100)
                                {
                                    int XChange = Game1.r.Next(-10, 11);
                                    int ZChange = Game1.r.Next(-10, 11);

                                    if (!(Calamitizer.InteractionLocation.X + XChange < 0 || Calamitizer.InteractionLocation.X + XChange > Width - 1 || Calamitizer.InteractionLocation.Z + ZChange < 0 || Calamitizer.InteractionLocation.Z + ZChange > Length - 1))
                                    {
                                        if (WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "ocean" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].Biome != "void" && WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation != null && SettlementTypes.Contains(WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Type))
                                        {
                                            HistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Name + ".");
                                            Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation.Name + ".");
                                            Calamitizer.InteractionLocation = WorldMap[(Calamitizer.InteractionLocation.X + XChange) + (Calamitizer.InteractionLocation.Z + ZChange) * Width].MyLocation;
                                            Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Calamitizer.Name + " shifted their focus to " + Calamitizer.InteractionLocation.Name + ".");
                                        }
                                    }
                                    Tries++;
                                }
                            }
                            else if(!CalamityStructures.Contains(Calamitizer.InteractionLocation.Type))
                            {
                                //inflict pain and suffering

                                int GrievanceChance = 4;

                                void LogEvent(string Event)
                                {
                                    Calamitizer.InteractionLocation.LocationHistoricalEvents.Add(Date + " " + Event);
                                    HistoricalEvents.Add(Date + " " + Event);
                                }

                                District ChosenDistrict = Calamitizer.InteractionLocation.Districts
                                        .Where(d => d.Architects.Count > 0)
                                        .OrderBy(_ => r.Next())
                                        .FirstOrDefault();

                                if (ChosenDistrict != null)
                                {
                                    if (CalamityIdeologicalObsession == "disease")
                                    {
                                        LogEvent(Calamitizer.Name + " deliberately spread the " + Calamitizer.BlightManipulated.Name + " to " + Calamitizer.InteractionLocation.Name + ".");

                                        foreach(Architect a in ChosenDistrict.Architects)
                                        {
                                            if(r.Next(GrievanceChance) == 1)
                                            {
                                                a.Grievances.Add((Calamitizer, "plagued " + a.PossessivePronoun + " town, " + a.Location.Name + "."));
                                                Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "dominator")
                                    {
                                        if (Calamitizer.InteractionLocation.Government == null)
                                        {
                                            LogEvent(Calamitizer.Name + " has peacefully taken control of " + Calamitizer.InteractionLocation.Name + ", as there was no governing body to oppose.");
                                            Calamitizer.TakenLocations.Add(Calamitizer.InteractionLocation);
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, "unjustly took control of " + a.PossessivePronoun + " town, " + a.Location.Name + ""));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }

                                        else if (Calamitizer.InteractionLocation.Government != Calamitizer)
                                        {
                                            // Existing logic for when there is a government.
                                            if (r.Next(2) == 1)
                                            {
                                                LogEvent(Calamitizer.Name + " threatened " + Calamitizer.InteractionLocation.Government.Name + ", the government of " + Calamitizer.InteractionLocation.Name + ", demanding they step down. " + Calamitizer.InteractionLocation.Government.Name + " complied.");
                                            }
                                            else
                                            {
                                                LogEvent(Calamitizer.Name + " threatened " + Calamitizer.InteractionLocation.Government.Name + ", the government of " + Calamitizer.InteractionLocation.Name + ", demanding they step down. " + Calamitizer.InteractionLocation.Government.Name + " refused, and " + Calamitizer.Name + " decided to brutally murder them.");

                                                if (Calamitizer.InteractionLocation.Government is Architect)
                                                {
                                                    ((Architect)(Calamitizer.InteractionLocation.Government)).District.Architects.Remove(((Architect)(Calamitizer.InteractionLocation.Government)));
                                                    Calamitizer.KilledPeopleWhoActuallyMatter.Add(((Architect)(Calamitizer.InteractionLocation.Government)));
                                                }
                                                else
                                                {
                                                    foreach (Architect a in ((Group)(Calamitizer.InteractionLocation.Government)).Architects)
                                                    {
                                                        a.District.Architects.Remove(a);
                                                        LogEvent(Calamitizer.Name + " killed " + a.Name + " as they were part of " + Calamitizer.InteractionLocation.Government.Name + ".");

                                                        Calamitizer.KilledPeopleWhoActuallyMatter.Add(a);
                                                    }
                                                }
                                            }

                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, "unjustly took control of " + a.PossessivePronoun + " town, " + a.Location));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }

                                            // This line seems redundant, as it's assigning the government to itself without any change.
                                            // Calamitizer.InteractionLocation.Government = (Calamitizer.InteractionLocation.Government);
                                            Calamitizer.TakenLocations.Add(Calamitizer.InteractionLocation);
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "killer")
                                    {
                                        if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 3) == 1)
                                        {
                                            int InitialPop = ChosenDistrict.UnplacedPopulation;
                                            ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                            LogEvent(Calamitizer.Name + " killed " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " people in " + Calamitizer.InteractionLocation.Name + ".");

                                            int DecideAge = r.Next(1, 6);
                                            if (DecideAge == 1)
                                            {
                                                Calamitizer.KilledChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else if (DecideAge < 4)
                                            {
                                                Calamitizer.KilledMen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else
                                            {
                                                Calamitizer.KilledWomen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                        }
                                        else if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            for (int i = r.Next(0, 3); i != 0; i--)
                                            {
                                                int Index = r.Next(ChosenDistrict.Architects.Count);

                                                if (ChosenDistrict.Architects[Index] == Calamitizer)
                                                {
                                                    continue;
                                                }

                                                ChosenDistrict.ArchitectsToRemove.Add(ChosenDistrict.Architects[Index]);

                                                LogEvent(Calamitizer.Name + " assasinated " + ChosenDistrict.Architects[Index].Name + " in " + Calamitizer.InteractionLocation.Name + ".");

                                                foreach (Architect a in ChosenDistrict.Architects)
                                                {
                                                    if (r.Next(GrievanceChance) == 1)
                                                    {
                                                        a.Grievances.Add((Calamitizer, " murdered a friend of " + a.Name + ", " + ChosenDistrict.Architects[Index].Name));
                                                        Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                    }
                                                }


                                                Calamitizer.KilledPeopleWhoActuallyMatter.Add(ChosenDistrict.Architects[Index]);
                                            }
                                        }

                                    }
                                    else if (CalamityIdeologicalObsession == "kidnapper")
                                    {
                                        if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 3) == 1)
                                        {
                                            int InitialPop = ChosenDistrict.UnplacedPopulation;

                                            ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                            LogEvent(Calamitizer.Name + " kidnapped " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " person(s) in " + Calamitizer.InteractionLocation.Name + ".");

                                            int DecideAge = r.Next(1, 6);
                                            if (DecideAge == 1)
                                            {
                                                Calamitizer.KidnappedChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else if (DecideAge < 4)
                                            {
                                                Calamitizer.KidnappedMen += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else
                                            {
                                                Calamitizer.KidnappedWomen += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, " kidnapped people, causing distress in " + a.PossessivePronoun + " community"));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                        else if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            for (int i = r.Next(1, 3); i != 0; i--)
                                            {
                                                int Index = r.Next(ChosenDistrict.Architects.Count);

                                                ChosenDistrict.ArchitectsToRemove.Add(ChosenDistrict.Architects[Index]);

                                                LogEvent(Calamitizer.Name + " kidnapped " + ChosenDistrict.Architects[Index].Name + " in " + Calamitizer.InteractionLocation.Name + ".");

                                                Calamitizer.KidnappedPeopleWhoActuallyMatter.Add(ChosenDistrict.Architects[Index]);
                                                foreach (Architect a in ChosenDistrict.Architects)
                                                {
                                                    if (r.Next(GrievanceChance) == 1)
                                                    {
                                                        a.Grievances.Add((Calamitizer, " kidnapped " + ChosenDistrict.Architects[Index].Name + ", a valued member of " + a.PossessivePronoun + " community"));
                                                        Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    else if (CalamityIdeologicalObsession == "corruptor")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);

                                            if (ChosenDistrict.Architects[Index] == Calamitizer)
                                            {
                                                continue;
                                            }
                                            
                                            LogEvent(Calamitizer.Name + " corrupted " + ChosenDistrict.Architects[Index].Name + "'s moral values in " + Calamitizer.InteractionLocation.Name + ".");
                                            
                                            (ChosenDistrict.Architects[Index]).MoralCompass -= r.Next(10, 20);
                                            (ChosenDistrict.Architects[Index]).StabilityCompass -= r.Next(10, 20);
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, " was noticed by " + ChosenDistrict.Architects[Index].Name + ", who began to notice a difference in " + ChosenDistrict.Architects[Index].Name + ""));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "diplomancer")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);

                                            if (ChosenDistrict.Architects[Index] == Calamitizer)
                                            {
                                                continue;
                                            }

                                            (ChosenDistrict.Architects[Index]).MoralCompass -= r.Next(15, 35);

                                            LogEvent(Calamitizer.Name + " influenced " + ChosenDistrict.Architects[Index].Name + "'s values towards evil in " + Calamitizer.InteractionLocation.Name + ".");
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, " was noticed by " + a.Name + ", who began to see a major change in " + ChosenDistrict.Architects[Index].Name + " towards evil"));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "inciter")
                                    {
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            int Index = r.Next(ChosenDistrict.Architects.Count);
                                            LogEvent(Calamitizer.Name + " spread propaganda, attempting to influence " + Calamitizer.InteractionLocation.HomeCivilization.Name + " into a conflict.");

                                            Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints += r.Next(0, 5);

                                            if (Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints > 200)
                                            {
                                                Civilization c = Civilizations[r.Next(Civilizations.Count)];
                                                c.CivsAtWar.Add(Calamitizer.InteractionLocation.HomeCivilization);
                                                Calamitizer.InteractionLocation.HomeCivilization.CivsAtWar.Add(c);

                                                foreach(Architect a in AllArchitects)
                                                {
                                                    if(a.HomeLocation != null && (a.HomeLocation.HomeCivilization == c || c.CivsAtWar.Contains(a.HomeLocation.HomeCivilization)))
                                                    {
                                                        if(r.Next(GrievanceChance/2) == 1)
                                                        {
                                                            a.Grievances.Add((Calamitizer, " caused a war that ruined the stability of " + a.Name + "'s life"));
                                                            Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                        }
                                                    }
                                                }

                                                Calamitizer.InteractionLocation.HomeCivilization.WakeUpAndChooseViolencePoints = 0;
                                            }

                                            (ChosenDistrict.Architects[Index]).MoralCompass -= r.Next(1, 3);
                                            (ChosenDistrict.Architects[Index]).StabilityCompass -= r.Next(1, 3);
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "power")
                                    {
                                        if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 3) == 1)
                                        {
                                            int InitialPop = ChosenDistrict.UnplacedPopulation;
                                            ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                            LogEvent(Calamitizer.Name + " killed " + (InitialPop - ChosenDistrict.UnplacedPopulation).ToString() + " people in " + Calamitizer.InteractionLocation.Name + ", and harvested their energy.");

                                            int DecideAge = r.Next(1, 6);
                                            if (DecideAge == 1)
                                            {
                                                Calamitizer.KilledChildren += (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else if (DecideAge < 4)
                                            {
                                                Calamitizer.KilledMen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            else
                                            {
                                                Calamitizer.KilledWomen -= (InitialPop - ChosenDistrict.UnplacedPopulation);
                                            }
                                            foreach (Architect a in ChosenDistrict.Architects)
                                            {
                                                if (r.Next(GrievanceChance) == 1)
                                                {
                                                    a.Grievances.Add((Calamitizer, "harvested energy, causing the death of many in " + a.PossessivePronoun + " town, " + Calamitizer.InteractionLocation.Name + ""));
                                                    Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                }
                                            }
                                        }
                                        if (ChosenDistrict.Architects.Count > 0)
                                        {
                                            for (int i = r.Next(0, 3); i != 0; i--)
                                            {
                                                int Index = r.Next(ChosenDistrict.Architects.Count);

                                                if (ChosenDistrict.Architects[Index] == Calamitizer)
                                                {
                                                    continue;
                                                }

                                                ChosenDistrict.ArchitectsToRemove.Add(ChosenDistrict.Architects[Index]);

                                                LogEvent(Calamitizer.Name + " assassinated " + ChosenDistrict.Architects[Index].Name + " in " + Calamitizer.InteractionLocation.Name + ", and harvested his energy.");

                                                Calamitizer.KilledPeopleWhoActuallyMatter.Add(ChosenDistrict.Architects[Index]);
                                                foreach (Architect a in ChosenDistrict.Architects)
                                                {
                                                    if (r.Next(GrievanceChance) == 1)
                                                    {
                                                        a.Grievances.Add((Calamitizer, "murdered and harvested energy from " + ChosenDistrict.Architects[Index].Name + ", a good friend of theirs."));
                                                        Calamitizer.InteractionLocation.Region.TragedyPoints.Add((r.Next(-10, 11), r.Next(-10, 11)));
                                                    }
                                                }
                                            }
                                        }
                                        if (Calamitizer.KilledChildren + Calamitizer.KilledMen + Calamitizer.KilledWomen + Calamitizer.KilledPeopleWhoActuallyMatter.Count > 500 && Calamitizer.SpellsKnown.Count < 3)
                                        {
                                            Calamitizer.SpellsKnown = Game1.AllSpells.Union(Game1.AllLegendarySpells).ToList();
                                            LogEvent("After harvesting enough energy and renouncing the deities of the land, " + Calamitizer.Name + " became infused with unfathomable power from an unknown origin, but continued on to tempt the universe further.");
                                        }
                                    }
                                    else if (CalamityIdeologicalObsession == "purifier")
                                    {
                                        if (ChosenDistrict.UnplacedPopulation > 0 && r.Next(1, 10) == 1)
                                        {
                                            int InitialPop = ChosenDistrict.UnplacedPopulation;
                                            ChosenDistrict.UnplacedPopulation = Math.Max(0, ChosenDistrict.UnplacedPopulation - 1);

                                            // Trigger random small-scale ruptures
                                            for (int i = 0; i < (InitialPop - ChosenDistrict.UnplacedPopulation); i++)
                                            {
                                                int ruptureX = r.Next(Width);
                                                int ruptureZ = r.Next(Length);
                                                TriggerRupture(ruptureX, ruptureZ, Calamitizer, r.Next(0, 3)); // small radius between 1 and 3

                                                // Scan nearby regions within a certain range but outside the immediate rupture radius
                                                int scanRadius = 8; // Arbitrary scan radius
                                                for (int x = Math.Max(0, ruptureX - scanRadius); x <= Math.Min(Width - 1, ruptureX + scanRadius); x++)
                                                {
                                                    for (int z = Math.Max(0, ruptureZ - scanRadius); z <= Math.Min(Length - 1, ruptureZ + scanRadius); z++)
                                                    {
                                                        if (CalculateDistance(ruptureX, ruptureZ, x, z) > 3 && CalculateDistance(ruptureX, ruptureZ, x, z) <= scanRadius)
                                                        {
                                                            Location nearbyLocation = WorldMap[x + z * Width].MyLocation;
                                                            if (nearbyLocation != null)
                                                            {
                                                                foreach (District district in nearbyLocation.Districts)
                                                                {
                                                                    foreach (Architect architect in district.Architects)
                                                                    {
                                                                        if (r.Next(GrievanceChance) == 1)
                                                                        {
                                                                            architect.Grievances.Add((Calamitizer, "caused a rupture near " + architect.Name + "'s district."));
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }

                    foreach (Architect a in CalamitiesToAdd)
                    {
                        Calamity.Add(a);
                    }
                }

                foreach (Civilization c in Civilizations)
                {
                    if(HumanoidRaces.Contains(c.PrimaryInhabiantRace))
                    {
                        c.CyclesTillElection -= 24192000;
                        if (c.CyclesTillElection < 0 && c.Citizens.Count > 0)
                        {
                            var highestReputation = c.Citizens.Max(citizen => citizen.Reputation);
                            var candidates = c.Citizens.Where(citizen => citizen.Reputation == highestReputation).ToList();

                            Random rnd = new Random();
                            Architect selectedAlpha = candidates[rnd.Next(candidates.Count)];

                            Architect OldAlpha = c.Alpha;
                            if (OldAlpha != null)
                            {
                                OldAlpha.Profession = "prestiged";
                            }
                            c.Alpha = selectedAlpha;

                            if (OldAlpha != selectedAlpha)
                            {
                                if(OldAlpha != null)
                                {
                                    HistoricalEvents.Add(Date + " " + OldAlpha.Name + " ended his service as the alpha of " + c.Name + ".");
                                }
                                HistoricalEvents.Add(Date + " The civilization of " + c.Name + " elected a new alpha, " + c.Alpha.Name + ".");
                                c.Alpha.Profession = "alpha";
                            }

                            c.CyclesTillElection = c.ElectionFrequency;
                        }
                    }
                }

                //summon blight

                foreach (Blight b in Blights)
                {
                    if(!b.Spawned && Cycle > (double)((double)b.FoundingYear * (double)290304000))
                    {
                        while (!b.Spawned)
                        {
                            // 35% chance to try spawning at a spire
                            if (r.Next(1, 101) <= 35)
                            {
                                for (int x = 0; x < Width; x++)
                                {
                                    for (int z = 0; z < Length; z++)
                                    {
                                        // Check for spire and suitable conditions
                                        if (WorldMap[x + z * Width].Biome != "void" &&
                                            WorldMap[x + z * Width].Biome != "ocean" &&
                                            WorldMap[x + z * Width].MyLocation != null &&
                                            WorldMap[x + z * Width].MyLocation.Type == "spire")
                                        {
                                            b.Spawned = true;
                                            WorldMap[x + z * Width].Blight = b;

                                            if (((Architect)(WorldMap[x + z * Width].MyLocation.Government)).Profession == "sorcerer")
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[x + z * Width].MyLocation.Government.Name, ", sorcerer of ", LightDeity.Name, " \"blessed\" the land with the " + b.Name + " from ", WorldMap[x + z * Width].MyLocation.Name, "."));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[x + z * Width].MyLocation.Government.Name, ", warlock of ", DarkDeity.Name, " \"cursed\" the land with the " + b.Name + " from ", WorldMap[x + z * Width].MyLocation.Name, "."));
                                            }
                                            break;
                                        }
                                    }
                                    if (b.Spawned) break;
                                }
                            }

                            // If not spawned at a spire, default to random location
                            if (!b.Spawned)
                            {
                                int SpawnTryX = r.Next(Width);
                                int SpawnTryZ = r.Next(Length);

                                // Existing logic for spawning at a random location
                                if (WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "void" &&
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Biome != "ocean"
                                    ) // Higher spawn chance at spires
                                {
                                    b.Spawned = true;
                                    WorldMap[SpawnTryX + SpawnTryZ * Width].Blight = b;
                                    
                                    //keep history logic just in case it just so happens to start there.

                                    if (WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation != null)
                                    {
                                        if (WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Type == "spire")
                                        {
                                            if (((Architect)(WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government)).Profession == "sorcerer")
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government.Name, ", sorcerer of ", LightDeity.Name, " \"blessed\" the land with the " + b.Name + " from ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation, "."));
                                            }
                                            else
                                            {
                                                HistoricalEvents.Add(String.Concat(Date, " ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Government.Name, ", warlock of ", DarkDeity.Name, " \"cursed\" the land with the " + b.Name + " from ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation, "."));
                                            }
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(String.Concat(Date, " Something began to spread in ", WorldMap[SpawnTryX + SpawnTryZ * Width].MyLocation.Name, ", corrupting all it saw fit. The locals named the blight ", b.Name, "."));
                                        }
                                    }
                                    else
                                    {
                                        HistoricalEvents.Add(String.Concat(Date, " A terrible force of nature brought about the blight of ", b.Name, "."));
                                    }
                                }
                            }
                        }
                    }
                }



                //spread blight
                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if(WorldMap[x + z * Width].Blight != Purity)
                        {
                            if(r.Next(1,150) == 1)
                            {
                                //pick a random number between 1-4 and spread cardinally
                                int Spread = r.Next(4);
                                if(Spread == 1)
                                {
                                    //spread north if available
                                    if(z > 0)
                                    {
                                        WorldMap[(x) + (z-1) * Width].Blight = WorldMap[x + z * Width].Blight;
                                    }
                                }
                                else if (Spread == 2)
                                {
                                    //spread east if available
                                    if (x < Width-1)
                                    {
                                        WorldMap[(x+1) + (z) * Width].Blight = WorldMap[x + z * Width].Blight;
                                    }
                                }
                                else if (Spread == 3)
                                {
                                    //spread south if available
                                    if (z < Length-1)
                                    {
                                        WorldMap[(x) + (z+1) * Width].Blight = WorldMap[x + z * Width].Blight;
                                    }
                                }
                                else
                                {
                                    //spread west if available
                                    if (x > 0)
                                    {
                                        WorldMap[(x-1) + (z) * Width].Blight = WorldMap[x + z * Width].Blight;
                                    }
                                }
                            }
                        }
                    }
                }

                bool Break = false;

                //age groups

                foreach (Group g in Groups)
                {
                    g.MonthsOld++;
                }

                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if (WorldMap[x + z * Width].MyLocation != null)
                        {
                            foreach (Group g in WorldMap[x + z * Width].MyLocation.TradersAtThisLocation)
                            {
                                g.TradedThisMonth = false;
                            }
                        }
                    }
                }


                // Loop through the world map and update locations
                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        Location location = WorldMap[x + z * Width].MyLocation;

                        if (location != null)
                        {
                            // Efficiently add traders to this location
                            location.TradersAtThisLocation.AddRange(location.TradersAtThisLocationToAdd);
                            location.TradersAtThisLocationToAdd.Clear();

                            int WealthIncrease = 50; // Assuming this is used later in your code

                            // Only proceed if location is core or garrison, has enough wealth, and a rare condition is met
                            if ((location.Type == "core" || location.Type == "garrison") && location.Wealth > 10000 && r.Next(1, 1001) == 1)
                            {
                                // Simplify direction decision using a more direct approach
                                int direction = r.Next(4); // 0: Right, 1: Left, 2: Up, 3: Down

                                // Calculate new position based on the direction
                                int NewX = location.X + (direction == 0 ? 1 : direction == 1 ? -1 : 0);
                                int NewZ = location.Z + (direction == 2 ? 1 : direction == 3 ? -1 : 0);

                                // Check if the new position is within bounds and not a "void" biome
                                bool isValidPosition = NewX > 0 && NewX < Width && NewZ > 0 && NewZ < Length;
                                bool isNotVoid = isValidPosition && WorldMap[NewX + NewZ * Width].MyLocation == null && WorldMap[NewX + NewZ * Width].Biome != "void";

                                if (isValidPosition && isNotVoid)
                                {
                                    // Create and add the new location if conditions are met
                                    LocationBuilderPacket l = new LocationBuilderPacket(
                                        location.Government, NewX, NewZ, "garrison", location.PrimaryRace,
                                        r.Next(5, 10), 0, location.HomeCivilization, new List<Object>(), location
                                    );
                                    LocationBuilderPackets.Add(l);
                                    location.Wealth -= 10000; // Deduct wealth as the location is being expanded
                                }
                            }

                            if (SettlementTypes.Contains(location.Type))
                            {
                                foreach (District d in location.Districts)
                                {
                                    for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                    {
                                        for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                        {
                                            foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                            {
                                                if (s.Type == "forge")
                                                {
                                                    WealthIncrease += 1;
                                                }
                                                else if (s.Type == "market")
                                                {
                                                    WealthIncrease += 2;
                                                }
                                            }
                                        }
                                    }

                                    //establish an industry and do industry things :O

                                    if (d.Industry == null && (location.Type == "village" || location.Type == "town" || location.Type == "city"))
                                    {
                                        d.Industry = Game1.Industries[r.Next(Game1.Industries.Count)];
                                        HistoricalEvents.Add(string.Concat(d.Name, " in ", location.Name, " dedicated themselves to the industry of ", d.Industry, "."));
                                        location.LocationHistoricalEvents.Add(string.Concat(d.Name, " in ", location.Name, " dedicated themselves to the industry of ", d.Industry, "."));
                                    }

                                    //foreach district with an industry, use it to increase the capitol's wealth bwzhaaahahahahahahahahahaaaa just kidding its actually the location that theyre based around

                                    if (d.Industry != null && r.Next(1, 20) == 1)
                                    {
                                        d.SupplyLocation(1);
                                    }
                                }
                            }

                            location.Wealth = location.Wealth + (int)Math.Round(WealthIncrease * ProsperityMultiplier);


                            //create interactable events based on history


                            int IEDecider = r.Next(1, 500);

                            int LX = location.X + r.Next(-5, 6);
                            int LZ = location.Z + r.Next(-5, 6);

                            if (LX >= 0 && LX < Width && LZ >= 0 && LZ < Width && WorldMap[LX + LZ * Width].MyLocation == null && WorldMap[LX + LZ * Width].Biome != "void" && WorldMap[LX + LZ * Width].Biome != "ocean" && new string[] { "town", "city", "camp", "village" }.Contains(location.Type))
                            {
                                string DecidedType = "";

                                List<Architect> GuarranteedArch = new List<Architect>();

                                switch (IEDecider)
                                {
                                    case int decider when decider < 2:
                                        DecidedType = "bandits";
                                        for(int Arch = r.Next(4,8); Arch != 0; Arch--)
                                        {
                                            Architect AA = new Architect("", Game1.Sexes[r.Next(2)], location.HomeCivilization.PrimaryInhabiantRace, r.Next(13, 39), "bandit", new List<Object>(), null, null, null, "", 3);
                                            AA.KitOutArchitect("bandit");
                                            AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                            GuarranteedArch.Add(AA);
                                        }
                                        break;
                                    case int decider when decider < 3:
                                        DecidedType = "shadebeast";
                                        Architect SB = new Architect("", Game1.Sexes[r.Next(2)], GetRace("shadebeast"), r.Next(Year), "shadebeast", new List<Object>(), null, null, null, "", 3);
                                        SB.Name = Game1.GameWorld.GenerateUniqueArchitectName(SB);
                                        GuarranteedArch.Add(SB);
                                        break;
                                    case int decider when decider < 4:
                                        DecidedType = "construct"; 
                                        Architect CN = new Architect("", Game1.Sexes[r.Next(2)], ConstructRaces[r.Next(ConstructRaces.Count)], r.Next(Year), "construct", new List<Object>(), null, null, null, "", 3);
                                        CN.Name = Game1.GameWorld.GenerateUniqueArchitectName(CN);
                                        GuarranteedArch.Add(CN);
                                        break;
                                    case int decider when decider < 5:
                                        DecidedType = "wildcreatures";
                                        Race DecidedRace = WildRaces[r.Next(WildRaces.Count)];

                                        for (int Arch = r.Next(4, 8); Arch != 0; Arch--)
                                        {
                                            Architect AA = new Architect("", Game1.Sexes[r.Next(2)], DecidedRace, r.Next(13, 39), "beast", new List<Object>(), null, null, null, "", 2);
                                            AA.Name = Game1.GameWorld.GenerateUniqueArchitectName(AA);
                                            GuarranteedArch.Add(AA);
                                        }
                                        break;
                                    case int decider when decider < 6 && TradingGroups.Count > 0:
                                        DecidedType = "traders";
                                        Group TradingGroup = TradingGroups[r.Next(TradingGroups.Count)];
                                        GuarranteedArch = TradingGroup.Architects; //updates if the group updates\
                                        break;
                                    case int decider when decider < 8:
                                        DecidedType = "vagabond";
                                        Architect VB = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "vagabond", new List<Object>(), null, null, null, "", 3);
                                        VB.KitOutArchitect("vagabond");
                                        VB.Name = Game1.GameWorld.GenerateUniqueArchitectName(VB);
                                        GuarranteedArch.Add(VB);
                                        break;
                                    case int decider when decider < 9:
                                        DecidedType = "adventurer";
                                        Architect AD = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "adventurer", new List<Object>(), null, null, null, "", 3);
                                        AD.KitOutArchitect("adventurer");
                                        AD.Name = Game1.GameWorld.GenerateUniqueArchitectName(AD);
                                        GuarranteedArch.Add(AD);
                                        break;
                                    case int decider when decider < 10:
                                        DecidedType = "priest";
                                        Architect PR = new Architect("", Game1.Sexes[r.Next(2)], HumanoidRaces[r.Next(HumanoidRaces.Count)], r.Next(13, 39), "priest", new List<Object>(), null, null, null, "", 1);
                                        PR.KitOutArchitect("priest");
                                        PR.Name = Game1.GameWorld.GenerateUniqueArchitectName(PR);
                                        GuarranteedArch.Add(PR);
                                        break;
                                }


                                if (DecidedType != "")
                                {
                                    WorldMap[LX + LZ * Width].Events.Add(new InteractableEvent(WorldMap[LX + LZ * Width], r.Next(28, 54), DecidedType, location.HomeCivilization, GuarranteedArch));
                                }
                            }

                            //traders do stuff

                            foreach (Group g in location.TradersAtThisLocation)
                            {
                                if(location.Market != null)
                                {

                                    if (!g.TradedThisMonth)
                                    {
                                        //trade at your current location
                                        for (int i = r.Next(20, 30); i != 0; i--)
                                        {
                                            if (location.Market.Block.District.GeneralItemsWeHave.Count < 5)
                                            {
                                                break;
                                            }

                                            int CaravanIndex = r.Next(g.CaravanItems.Count);
                                            Object CaravanObject = g.CaravanItems[CaravanIndex];

                                            int LocationIndex = r.Next(location.Market.Block.District.GeneralItemsWeHave.Count);
                                            Object LocationObject = location.Market.Block.District.GeneralItemsWeHave[LocationIndex];

                                            g.CaravanItems.Remove(CaravanObject);
                                            location.Market.Block.District.GeneralItemsWeHave.Add(CaravanObject);
                                            CaravanObject.Owner = null;

                                            location.Market.Block.District.GeneralItemsWeHave.Remove(LocationObject);
                                            g.CaravanItems.Add(LocationObject);
                                            LocationObject.Owner = g;

                                            if (!ItemTypesInCirculation.Contains(CaravanObject.Type))
                                            {
                                                ItemTypesInCirculation.Add(CaravanObject.Type);
                                            }
                                            if (!ItemTypesInCirculation.Contains(LocationObject.Type))
                                            {
                                                ItemTypesInCirculation.Add(LocationObject.Type);
                                            }

                                            //make sure the traders make a profit from the general wealth of the community, yeah this system probably will change somewhat

                                            //was originally going to make it so the civilization has to pay to make up for the g.storedviatlium, but im not sure how to do that without plummeting the cost of currency. will need a rework soon.

                                            g.StoredVitalium += r.Next(10, 30);
                                        }


                                        g.TradedThisMonth = true;

                                        if (Cycle % (int)Math.Round((decimal)(Math.Round((decimal)(Cycle / 2903040000), 0, MidpointRounding.ToNegativeInfinity)), 0, MidpointRounding.ToNegativeInfinity) == 0)
                                        {
                                            //i.e. if its the start of a new decade lmao
                                            //consider adding new locations to your route

                                            if (r.Next(1, 3) == 1 && g.TradeRoute.Count <= g.MaxTradeRouteLength)
                                            {
                                                foreach (Location l in AllLocations)
                                                {
                                                    if (Vector2.Distance(new Vector2(l.X, l.Z), new Vector2(location.X, location.Z)) < 20 && !(g.TradeRoute.Contains(l)))
                                                    {
                                                        if (SettlementTypes.Contains(l.Type))
                                                        {
                                                            HistoricalEvents.Add(string.Concat(Date, g.Name, " added ", l.Name, " to their list of trading partners."));
                                                            g.TradeRoute.Add(l);
                                                            break;

                                                        }
                                                    }
                                                }
                                            }
                                        }


                                        //actually MOVE The trader this time!
                                        if (g.TradeRoute.Count > 1)
                                        {
                                            int TravelIndex = g.TradeRoute.IndexOf(location);

                                            if (TravelIndex == g.TradeRoute.Count - 1)
                                            {
                                                TravelIndex = 0;
                                            }
                                            else
                                            {
                                                TravelIndex++;
                                            }

                                            location.TradersAtThisLocationToRemove.Add(g);
                                            g.TradeRoute[TravelIndex].TradersAtThisLocation.Add(g);

                                            foreach (Architect a in g.Architects)
                                            {
                                                a.NextMigrationLocation = g.TradeRoute[TravelIndex];
                                            }
                                        }



                                        // Random chance to build a port
                                        if (r.Next(1900) == 0)
                                        {
                                            // Assuming the existence of static int ContinentalPortMaximum = X; where X is your desired maximum number of ports on the largest island.
                                            // Initialize lists for islands and port locations
                                            List<List<Region>> islands = new List<List<Region>>();
                                            List<List<Region>> portLocations = new List<List<Region>>();

                                            // Detect islands and potential port locations
                                            DetectIslandsAndPorts(WorldMap, Width, islands, portLocations);

                                            // Identify the biggest island
                                            List<Region> biggestIsland = islands.OrderByDescending(island => island.Count).FirstOrDefault();

                                            // Initialize a counter for ports on the biggest island based on current port conditions
                                            int portsOnBiggestIsland = biggestIsland.SelectMany(region => portLocations[islands.IndexOf(biggestIsland)]).Count(port => !string.IsNullOrEmpty(port.PortName));
                                            bool portBuilt = false;

                                            foreach (var island in islands)
                                            {
                                                var potentialPorts = FindPotentialPorts(WorldMap, Width, island);
                                                bool islandHasPort = potentialPorts.Any(port => !string.IsNullOrEmpty(port.PortName));

                                                if (island == biggestIsland && portsOnBiggestIsland < ContinentalPortMaximum)
                                                {
                                                    // For the biggest island, check if it hasn't exceeded the ContinentalPortMaximum
                                                    Region portLocation = potentialPorts.FirstOrDefault(port => string.IsNullOrEmpty(port.PortName));
                                                    if (portLocation != null)
                                                    {
                                                        portLocation.PortName = GenerateUniqueName("1S7s", portLocation);
                                                        HistoricalEvents.Add($"A new port named {portLocation.PortName} was built to facilitate trade.");
                                                        portBuilt = true;
                                                        portsOnBiggestIsland++; // Update the counter for ports on the biggest island
                                                    }
                                                }
                                                else if (!islandHasPort)
                                                {
                                                    // For other islands, build only one port if there isn't already one
                                                    Region portLocation = potentialPorts.FirstOrDefault(port => string.IsNullOrEmpty(port.PortName));
                                                    if (portLocation != null)
                                                    {
                                                        portLocation.PortName = GenerateUniqueName("1S7s", portLocation);
                                                        HistoricalEvents.Add($"A new port named {portLocation.PortName} was built to facilitate trade.");
                                                        portBuilt = true;
                                                    }
                                                }

                                                if (portBuilt) break; // Exit the loop once a port is built
                                            }
                                        }

                                    }
                                }
                            }

                            foreach (Group g in location.TradersAtThisLocationToRemove)
                            {
                                location.TradersAtThisLocation.Remove(g);
                            }
                            location.TradersAtThisLocationToRemove = new List<Group>();

                            //forge two groups together

                            foreach (Group g in location.GroupsAtThisLocation)
                            {
                                foreach (Group G in location.GroupsAtThisLocation)
                                {
                                    if (G != g)
                                    {
                                        if (Game1.r.Next(1, 5) == 1)
                                        {
                                            if (g.Type == "anarchist" && G.Type == "anarchist")
                                            {
                                                HistoricalEvents.Add(string.Concat(Date, g.Name, " and ", G.Name, " started talking about merging their groups."));

                                                Group GG = new Group(new List<Architect>(), g.Type, G.Leader, location);

                                                List<Architect> Joiners = new List<Architect>();
                                                List<Architect> Leavers = new List<Architect>();

                                                foreach (Architect architect in G.Architects)
                                                {
                                                    if (Game1.r.Next(1, 10) == 1 && architect != GG.Leader)
                                                    {
                                                        HistoricalEvents.Add(string.Concat(Date, architect.Name, " disagreed with the idea of merging groups and left them both to settle it themselves."));
                                                        Leavers.Add(architect);
                                                    }
                                                    else
                                                    {
                                                        Joiners.Add(architect);
                                                    }
                                                }
                                                foreach (Architect architect in g.Architects)
                                                {
                                                    if (Game1.r.Next(1, 10) == 1)
                                                    {
                                                        HistoricalEvents.Add(string.Concat(Date, architect.Name, " disagreed with the idea of merging groups and left them both to settle it themselves."));
                                                        Leavers.Add(architect);
                                                    }
                                                    else
                                                    {
                                                        Joiners.Add(architect);
                                                    }
                                                }

                                                foreach (Architect a in Joiners)
                                                {
                                                    a.Group = GG;
                                                    GG.Architects.Add(a);
                                                }
                                                foreach (Architect a in Leavers)
                                                {
                                                    a.Group = null;
                                                    g.Leader.Location.Districts[0].DistrictMap[Game1.r.Next(0, 49)].Architects.Add(a);
                                                }

                                                GG.Reputation = (g.Reputation + G.Reputation) / 2;

                                                HistoricalEvents.Add(Date + g.Name + " and " + G.Name + " noticed the anarchistic similarities of both their groups and forged them into one, going under the name " + GG.Name + ".");
                                                if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog) == 1)
                                                {
                                                    HistoricalEvents.Add(Date + g.Name + " and " + G.Name + " noticed the anarchistic similarities of both their groups and forged them into one, going under the name " + GG.Name + ".");
                                                }

                                                Groups.Remove(g);
                                                Groups.Remove(G);
                                                Groups.Add(GG);

                                                Break = true;
                                                break;
                                            }
                                        }
                                    }

                                }
                                if (Break)
                                {
                                    break;
                                }
                            }

                            //Handle architect/architect group actions regardless of district
                            //this is the meat of the history

                            List<Architect> ArchitectsAtLocation = new List<Architect>();
                            foreach(District d in location.Districts)
                            {
                                ArchitectsAtLocation.AddRange(d.Architects);
                            }

                            //culture formation

                            //      celebrations

                            //      art forms

                            //      games

                            //      food styles

                            //      sport




                            //other actions favorable or not, based on allignment
                            //gather "forces" in the location. each "force" is assigned a power, values, and name which demonstrates its ability to act based on resources and such and tells you who did it. 

                            List<Force> Forces = new List<Force>();

                            foreach(Architect a in ArchitectsAtLocation)
                            {
                                //salary

                                a.Wealth += r.Next(0, 3);

                                if (a.Group == null || r.Next(1,3) == 1 ) //architects are less likely to act by themselves if they have friends they might act with, but they still can :)
                                {
                                    Forces.Add(new Force(a.Name, a.Profession, 1, a.MoralCompass, a.StabilityCompass, a.PropertyValue, a.FamilyValue, a.PowerValue, a.MoneyValue, a.KnowledgeValue, a.SpiritualityValue, a.ProwessValue, a.PatriotismValue, a.CourageValue, a.CreativityValue, a));
                                }
                            }
                            foreach (Group g in location.GroupsAtThisLocation)
                            {
                                //salary

                                g.Wealth += g.Architects.Count * r.Next(0, 4);

                                if(g.Architects.Count > 0)
                                {
                                    //set group values
                                    {
                                        // Initialize accumulators for each value for non-leaders
                                        int totalMoralCompass = 0, totalStabilityCompass = 0, totalPropertyValue = 0,
                                            totalFamilyValue = 0, totalPowerValue = 0, totalMoneyValue = 0,
                                            totalKnowledgeValue = 0, totalSpiritualityValue = 0, totalProwessValue = 0,
                                            totalPatriotismValue = 0, totalCourageValue = 0;

                                        int nonLeaderCount = g.Architects.Count(a => (g.Leader != a)); // Assuming there's always one leader

                                        foreach (var architect in g.Architects)
                                        {
                                            // Skip the leader in this loop
                                            if (g.Leader == architect) continue;

                                            // Accumulate values for non-leaders
                                            totalMoralCompass += architect.MoralCompass;
                                            totalStabilityCompass += architect.StabilityCompass;
                                            totalPropertyValue += architect.PropertyValue;
                                            totalFamilyValue += architect.FamilyValue;
                                            totalPowerValue += architect.PowerValue;
                                            totalMoneyValue += architect.MoneyValue;
                                            totalKnowledgeValue += architect.KnowledgeValue;
                                            totalSpiritualityValue += architect.SpiritualityValue;
                                            totalProwessValue += architect.ProwessValue;
                                            totalPatriotismValue += architect.PatriotismValue;
                                            totalCourageValue += architect.CourageValue;
                                        }

                                        // Find the leader (assuming there's exactly one leader)
                                        var leader = g.Architects.FirstOrDefault(a => (g.Leader != a));

                                        // Calculate average for non-leaders, ensure no division by zero
                                        double avgMoralCompass = nonLeaderCount > 0 ? (double)totalMoralCompass / nonLeaderCount : 0;
                                        double avgStabilityCompass = nonLeaderCount > 0 ? (double)totalStabilityCompass / nonLeaderCount : 0;
                                        double avgPropertyValue = nonLeaderCount > 0 ? (double)totalPropertyValue / nonLeaderCount : 0;
                                        double avgFamilyValue = nonLeaderCount > 0 ? (double)totalFamilyValue / nonLeaderCount : 0;
                                        double avgPowerValue = nonLeaderCount > 0 ? (double)totalPowerValue / nonLeaderCount : 0;
                                        double avgMoneyValue = nonLeaderCount > 0 ? (double)totalMoneyValue / nonLeaderCount : 0;
                                        double avgKnowledgeValue = nonLeaderCount > 0 ? (double)totalKnowledgeValue / nonLeaderCount : 0;
                                        double avgSpiritualityValue = nonLeaderCount > 0 ? (double)totalSpiritualityValue / nonLeaderCount : 0;
                                        double avgProwessValue = nonLeaderCount > 0 ? (double)totalProwessValue / nonLeaderCount : 0;
                                        double avgPatriotismValue = nonLeaderCount > 0 ? (double)totalPatriotismValue / nonLeaderCount : 0;
                                        double avgCourageValue = nonLeaderCount > 0 ? (double)totalCourageValue / nonLeaderCount : 0;

                                        // Assuming leader is not null before accessing its properties
                                        if (leader != null)
                                        {
                                            // Calculate final group values, combining leader's values (50% weight) with non-leader averages
                                            g.MoralCompass = (int)((leader.MoralCompass + avgMoralCompass) / 2);
                                            g.StabilityCompass = (int)((leader.StabilityCompass + avgStabilityCompass) / 2);
                                            g.PropertyValue = (int)((leader.PropertyValue + avgPropertyValue) / 2);
                                            g.FamilyValue = (int)((leader.FamilyValue + avgFamilyValue) / 2);
                                            g.PowerValue = (int)((leader.PowerValue + avgPowerValue) / 2);
                                            g.MoneyValue = (int)((leader.MoneyValue + avgMoneyValue) / 2);
                                            g.KnowledgeValue = (int)((leader.KnowledgeValue + avgKnowledgeValue) / 2);
                                            g.SpiritualityValue = (int)((leader.SpiritualityValue + avgSpiritualityValue) / 2);
                                            g.ProwessValue = (int)((leader.ProwessValue + avgProwessValue) / 2);
                                            g.PatriotismValue = (int)((leader.PatriotismValue + avgPatriotismValue) / 2);
                                            g.CourageValue = (int)((leader.CourageValue + avgCourageValue) / 2);
                                        }
                                    }

                                    Forces.Add(new Force(g.Name, g.Type + " group", g.Architects.Count, g.MoralCompass, g.StabilityCompass, g.PropertyValue, g.FamilyValue, g.PowerValue, g.MoneyValue, g.KnowledgeValue, g.SpiritualityValue, g.ProwessValue, g.PatriotismValue, g.CourageValue, g.CreativityValue, g));
                                }
                            }

                            //ok now we have all our forces lets do stuff with them

                            void LogForceAction(string Event)
                            {
                                HistoricalEvents.Add(Date + " " + Event);
                                location.LocationHistoricalEvents.Add(Date + " " + Event);
                            }

                            void ReputationChange(Force f, int Change)
                            {
                                if(f.Base is Architect)
                                {
                                    ((Architect)f.Base).Reputation += Change;
                                }
                                else if (f.Base is Group)
                                {
                                    ((Group)f.Base).Reputation += Change;
                                }
                                return;
                            }

                            foreach (Force f in Forces)
                            {
                                int AmbitionRank = 200; //decrease this to increase the likelihood of forces acting

                                if(ArchitectsAtLocation.Count > 0)
                                {
                                    if (f.MoneyValue >= 3 && f.StabilityCompass < 40 && r.Next(AmbitionRank) == 1)
                                    {
                                        string industry = location.Districts[r.Next(location.Districts.Count)].Industry;
                                        string profession = Game1.IndustryToProfession[industry];

                                        if (industry == "military" || industry == "waspkeeping")
                                        {
                                            industry += " supplies";
                                        }

                                        LogForceAction(f.Name + " stole " + industry + " from " + location.Name + ".");
                                        ReputationChange(f, -3);
                                        location.Wealth -= r.Next(50, 200) * f.Power;

                                        foreach(District d in location.Districts)
                                        {
                                            foreach (Architect a in d.Architects)
                                            {
                                                if (a.Profession == profession)
                                                {
                                                    a.Grievances.Add((f.Base, "stole " + industry + ", affecting " + a.PossessivePronoun + " livelihood"));
                                                }
                                            }
                                        }
                                    }

                                    else if (f.MoneyValue >= 4 && f.StabilityCompass < 40 && r.Next(AmbitionRank) == 1)
                                    {
                                        LogForceAction(f.Name + " set up means to embezzle extra funding from " + location.Name + "'s governmental structure.");
                                        ReputationChange(f, -1);
                                        location.Embezzlements.Add((f.Base, f.Power + r.Next(4, 8)));

                                        if (location.Government is Group)
                                        {
                                            foreach (Architect a in ((Group)location.Government).Architects)
                                            {
                                                a.Grievances.Add((f.Base, "embezzled funds from the government, undermining " + a.PossessivePronoun + " authority and trust"));
                                            }
                                        }
                                        else if (location.Government is Architect)
                                        {
                                            ((Architect)location.Government).Grievances.Add((f.Base, "embezzled funds, undermining the governance"));
                                        }
                                    }

                                    else if (f.MoneyValue >= 4 && f.KnowledgeValue >= 1 && f.StabilityCompass < 30 && r.Next(AmbitionRank * 5) == 1 && SettlementTypes.Contains(location.Type))
                                    {
                                        if (location.Prism.HistoricalObjects.Count > 1)
                                        {
                                            Object o = location.Prism.HistoricalObjects[r.Next(location.Prism.HistoricalObjects.Count)];
                                            if (r.Next(f.Power) > r.Next(3, 20))
                                            {
                                                LogForceAction($"{f.Name} attempted to steal {o.Name} from the prism in {location.Name}. The plan succeeded.");
                                                ReputationChange(f, -10);
                                                if (f.Base is Group)
                                                {
                                                    ((Group)f.Base).Leader.Inventory.Add(o);
                                                    location.Prism.HistoricalObjects.Remove(o);
                                                }
                                                else
                                                {
                                                    ((Architect)f.Base).Inventory.Add(o);
                                                }
                                            }
                                            else
                                            {
                                                LogForceAction($"{f.Name} attempted to steal {o.Name} from the prism in {location.Name}. The plan failed, but {f.Name} escaped.");
                                                ReputationChange(f, -10);
                                            }

                                            if (location.Government is Group)
                                            {
                                                foreach (Architect a in ((Group)location.Government).Architects)
                                                {
                                                    a.Grievances.Add((f.Base, "attempted to steal " + o.Name + ", a treasured artifact, impacting the heritage and pride of " + location.Name + "."));
                                                }
                                            }
                                            else if (location.Government is Architect)
                                            {
                                                ((Architect)location.Government).Grievances.Add((f.Base, "attempted to steal " + o.Name + ", a treasured artifact, impacting the heritage and pride of " + location.Name + "."));
                                            }
                                            // Additional grievance to the creator of the artifact if known
                                            // Additional grievance to the creator of the artifact if known
                                            if (o.Creator != null)
                                            {
                                                if (o.Creator is Architect)
                                                {
                                                    // If the creator is an individual architect
                                                    ((Architect)(o.Creator)).Grievances.Add((f.Base, "stole " + o.Name + ", a creation of great cultural significance."));
                                                }
                                                else if (o.Creator is Group)
                                                {
                                                    // If the creator is a group, add a grievance to each member of the group
                                                    foreach (Architect member in ((Group)(o.Creator)).Architects)
                                                    {
                                                        member.Grievances.Add((f.Base, "stole " + o.Name + ", a creation of great cultural significance."));
                                                    }
                                                }
                                            }

                                        }
                                    }

                                    else if (f.CreativityValue >= 3 && r.Next(AmbitionRank * 6) == 1 && SettlementTypes.Contains(location.Type))
                                    {
                                        //craftsmanship

                                        Material Metal = Metals[r.Next(Metals.Count)];
                                        List<string> metalObjects = new List<string>
{
    "sword",
    "knife",
    "greatsword",
    "battle axe",
    "axe",
    "greataxe",
    "rapier",
    "spear",
    "pike",
    "pickaxe",
    "mace",
    "hammer",
    "shield",
    "whip",
    "scourge",
    "flail",
    "chain",
    "urn",
    "pot",
    "helmet",
    "forge",
    "jar",
    "bottle",
    "mug",
    "bowl",
    "cup",
    "keg",
    "chest",
    "barrel",
    "bin",
    "door",
    "small chalice",
    "big chalice",
    "altar",
    "table",
    "chair"
};

                                        Object o = new Object("", metalObjects[r.Next(metalObjects.Count)], new List<Material>() { Metal }, f.Base);
                                        o.Name = GenerateUniqueName("1W" + r.Next(4, 7) + "s", o);

                                        int Decider = r.Next(1, 4);

                                        if (Decider == 1)
                                        {
                                            location.Prism.HistoricalObjects.Add(o);
                                            LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. They stored it in the prism of {location.Name} for safekeeping.");
                                            ReputationChange(f, 5);
                                        }
                                        else if (Decider == 2)
                                        {
                                            if (f.Base is Group)
                                            {
                                                ((Group)f.Base).Leader.Inventory.Add(o);
                                                LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Group)f.Base).Leader.Name} held onto it for safekeeping.");
                                                ReputationChange(f, 5);
                                            }
                                            else
                                            {
                                                ((Architect)f.Base).Inventory.Add(o);
                                                LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Architect)f.Base).Name} held onto it for safekeeping.");
                                                ReputationChange(f, 5);
                                            }
                                        }
                                        else
                                        {
                                            Architect Buyer = ArchitectsAtLocation[r.Next(ArchitectsAtLocation.Count)];
                                            int Price = r.Next(0, Buyer.Wealth);
                                            Buyer.Inventory.Add(o);
                                            Buyer.Wealth -= Price;

                                            if (f.Base is Group)
                                            {
                                                ((Group)f.Base).Wealth += Price;
                                                LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {((Group)f.Base).Leader.Name} sold it to {Buyer.Name} for {Price}.");
                                                ReputationChange(f, 5);
                                            }
                                            else
                                            {
                                                ((Architect)f.Base).Wealth += Price;
                                                LogForceAction($"{f.Name} created the legendary {o.Type} {o.Name} in {location.Name}. {(Architect)f.Base} sold it to {Buyer.Name} for {Price}.");
                                                ReputationChange(f, 5);
                                            }
                                        }
                                    }
                                    else if (f.Base is Architect && f.FamilyValue >= 1 && r.Next(AmbitionRank * 5) == 1 && ((Architect)(f.Base)).Spouse == null)
                                    {
                                        bool CaresAboutMarriageRace = true;
                                        if (r.Next(15) == 1)
                                        {
                                            CaresAboutMarriageRace = false;
                                        }

                                        foreach (Architect a in ArchitectsAtLocation)
                                        {
                                            // Skip if the same architect or if sex is the same or if theyre already married lul
                                            if (a.Spouse != null || a == f.Base || a.Sex == ((Architect)f.Base).Sex) continue;

                                            // Check for similarity in compasses within 20
                                            bool similarCompasses = Math.Abs(a.MoralCompass - f.MoralCompass) < 20 && Math.Abs(a.StabilityCompass - f.StabilityCompass) < 20;

                                            // Check for at least 5 values within 2 of each other
                                            int similarValuesCount = 0;
                                            similarValuesCount += Math.Abs(a.PropertyValue - f.PropertyValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.FamilyValue - f.FamilyValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.PowerValue - f.PowerValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.MoneyValue - f.MoneyValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.KnowledgeValue - f.KnowledgeValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.SpiritualityValue - f.SpiritualityValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.ProwessValue - f.ProwessValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.PatriotismValue - f.PatriotismValue) < 2 ? 1 : 0;
                                            similarValuesCount += Math.Abs(a.CourageValue - f.CourageValue) < 2 ? 1 : 0;

                                            // Evaluate race consideration if applicable
                                            bool raceConsideration = CaresAboutMarriageRace ? a.Race == ((Architect)f.Base).Race : true;

                                            if (similarCompasses && similarValuesCount >= 5 && raceConsideration)
                                            {
                                                a.Spouse = ((Architect)f.Base);
                                                ((Architect)f.Base).Spouse = a;

                                                // Initialize a variable to hold the name of the shrine if one is found
                                                string shrineName = string.Empty;

                                                // Iterate through all structures in the location to find a shrine
                                                foreach (var structure in location.AllStructures)
                                                {
                                                    if (structure.Type == "shrine")
                                                    {
                                                        shrineName = structure.Name;
                                                        break; // Exit the loop once a shrine is found
                                                    }
                                                }

                                                // Check if a shrine was found and adjust the log message accordingly
                                                if (!string.IsNullOrEmpty(shrineName))
                                                {
                                                    LogForceAction($"{f.Name} and {a.Name} got married at {shrineName} in {location.Name}.");
                                                }
                                                else
                                                {
                                                    LogForceAction($"{f.Name} and {a.Name} got married in {location.Name}.");
                                                }
                                            }

                                        }
                                    }
                                    else if (f.Base is Architect && ((Architect)f.Base).Spouse != null && ((Architect)f.Base).HadChildren == false)
                                    {
                                        ((Architect)f.Base).HadChildren = true;
                                        ((Architect)f.Base).Spouse.HadChildren = true;

                                        static int GenerateChildrenNumber(Random r)
                                        {
                                            double lambda = 0.2; // Adjust this parameter to tweak the distribution
                                            double uniformRandom = r.NextDouble();
                                            double skewedRandom = -Math.Log(1 - uniformRandom) / lambda;

                                            // Ensure the number is within our desired range but skewed towards lower numbers
                                            int children = (int)Math.Floor(skewedRandom);

                                            // Cap the result at 30 to allow for incredibly uncommon scenarios but not exceed it
                                            if (children > 30) children = 30;

                                            // Ensure at least 1 child
                                            if (children < 1) children = 1;

                                            return children;
                                        }

                                        int Children = GenerateChildrenNumber(r);

                                        int ImportantChildren = r.Next(0, Children / 2);

                                        if (ImportantChildren == 0)
                                        {
                                            LogForceAction(f.Base.Name + " and " + ((Architect)(f.Base)).Spouse.Name + " had " + Children + " children, but none of them actually matter.");
                                            location.Districts[r.Next(location.Districts.Count)].UnplacedPopulation += Children;
                                        }
                                        else
                                        {
                                            Race ChildRace;

                                            if (((Architect)f.Base).Race == ((Architect)f.Base).Spouse.Race)
                                            {
                                                ChildRace = ((Architect)f.Base).Race;
                                            }
                                            else
                                            {
                                                ChildRace = GetRace("archaix");
                                            }

                                            List<string> ImportantChildrenNames = new List<string>();

                                            for (int i = ImportantChildren; i != 0; i--)
                                            {
                                                Architect a = new Architect("", Game1.Sexes[r.Next(2)], ChildRace, 0, "child", new List<Object>(), ((Architect)f.Base).Location, ((Architect)f.Base).District, ((Architect)f.Base).Block, "", 0);
                                                a.Name = GenerateUniqueArchitectName(a);
                                                location.Districts[r.Next(location.Districts.Count)].Architects.Add(a);
                                            }
                                            location.Districts[r.Next(location.Districts.Count)].UnplacedPopulation += (Children - ImportantChildren);

                                            LogForceAction(f.Base.Name + " and " + f.Base.Name + " had " + Children + " children. The ones that actually matter are " + Game1.FormatList(ImportantChildrenNames) + ".");
                                        }
                                    }
                                }
                                
                            }


                            //hahahahaaha embezzlementeeee
                            List<(Entity, int)> EmbezzlToRemove = new List<(Entity, int)>();
                            foreach((Entity, int) e in location.Embezzlements)
                            {
                                if(r.Next(1,10) == 1)
                                {
                                    if (e.Item1 is Architect)
                                    {
                                        ((Architect)e.Item1).Wealth += e.Item2;
                                        location.Wealth -= e.Item2;
                                    }
                                    else
                                    {
                                        ((Group)e.Item1).Wealth += e.Item2;
                                        location.Wealth -= e.Item2;
                                    }
                                }
                                

                                if(r.Next(1,100) == 0)
                                {
                                    HistoricalEvents.Add("A loophole in the governmental structure of " + location.Name + " was discovered and fixed, and " + e.Item1.Name + " lost some ability to embezzle funding.");
                                    EmbezzlToRemove.Add(e);
                                }
                            }
                            foreach((Entity, int) E in EmbezzlToRemove)
                            {
                                location.Embezzlements.Remove(E);
                            }

                            //iterate through districts

                            foreach (District d in location.Districts)
                            {
                                int DistrictMaxPopulation = 0;

                                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                {
                                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                    {
                                        DistrictMaxPopulation += d.DistrictMap[DistrictX + DistrictZ * 7].Structures.Count * 3;

                                        //structure age

                                        foreach(Structure s in d.DistrictMap[DistrictX + DistrictZ*7].Structures)
                                        {
                                            s.AgeInYears += (1 / 12);
                                        }
                                    }
                                }

                                //Handle population increase in the district


                                if(location.Region.Blight != Purity && SettlementTypes.Contains(location.Type))
                                {
                                    if (d.UnplacedPopulation != 0)
                                    {
                                        d.UnplacedPopulation = Math.Max(d.UnplacedPopulation - r.Next(0, 4), 0);

                                        if(d.UnplacedPopulation == 0)
                                        {
                                            HistoricalEvents.Add($"{location.Name} fell to the {location.Region.Blight.Name}.");
                                            location.LocationHistoricalEvents.Add($"{location.Name} fell to the {location.Region.Blight.Name}.");
                                        }
                                    }
                                    foreach (Architect a in d.Architects)
                                    {
                                        if (r.Next(1, 70) == 1)
                                        {
                                            d.ArchitectsToRemove.Add(a);

                                            HistoricalEvents.Add($"{a.Name} died to the {location.Region.Blight.Name} in {location.Name}.");
                                            location.LocationHistoricalEvents.Add($"{a.Name} died to the {location.Region.Blight.Name} in {location.Name}.");
                                        }
                                    }
                                }



                                int BirthProbabilityMod = 100; // higher is less likely, decrease the chance of procreation

                                if (d.Population() < DistrictMaxPopulation && location.TruePopulation() > 4)
                                {
                                    // Calculate birth probability dynamically based on the original function
                                    double baseProbability = 1.0; // Set a base probability
                                    double populationFactor = 1 + (d.Population() / 100.0); // Adjust based on population (modify the factor as needed)

                                    double birthProbability = baseProbability / populationFactor;

                                    int births = 0;

                                    // Calculate births using a random number and the probability
                                    for (int i = 0; i < d.Population(); i++)
                                    {
                                        if (Game1.r.NextDouble() < birthProbability / BirthProbabilityMod)
                                        {
                                            births++;
                                        }
                                    }

                                    // Update the unplaced population with the calculated births
                                    d.UnplacedPopulation += births;
                                }



                                //Designate the Construction of a new District

                                if (d.UnplacedPopulation > 150)
                                {
                                    int Movers = Game1.r.Next(65, 85);

                                    District NewD = new District(false, location, Movers);
                                    d.UnplacedPopulation = d.UnplacedPopulation - Movers;

                                    foreach (Architect a in d.Architects)
                                    {
                                        if (Game1.r.Next(1, 4) == 1 && a.Group != location.Government)
                                        {
                                            NewD.Architects.Add(a);
                                            d.ArchitectsToRemove.Add(a);
                                            Movers--;
                                        }
                                    }
                                    foreach (Architect a in d.ArchitectsToRemove)
                                    {
                                        d.Architects.Remove(a);
                                    }
                                    d.ArchitectsToRemove = new List<Architect>();

                                    //go ahead and build a bunch of houses in the new district instead of moving stuff everywhere.

                                    for (int i = 0; i < Game1.r.Next(6, 10); i++)
                                    {
                                        Block ChosenBlock = NewD.DistrictMap[Game1.r.Next(0, 49)];
                                        Structure s = new Structure("house", new List<Object>(), new List<Room>(), ChosenBlock, new List<Material> { location.HomeCivilization.CulturalWood }, new List<string> { location.HomeCivilization.CulturalWood.Name }, new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4));
                                        location.AllStructures.Add(s);
                                        ChosenBlock.Structures.Add(s);
                                    }

                                    //well
                                    NewD.DistrictMap[r.Next(2, 6) + r.Next(2, 5) * 7].Objects.Add(new Object(null, "well", new List<Material> { location.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));

                                    location.DistrictsToAdd.Add(NewD);
                                }


                                //Handle elevation of a member of the population to Architect, Leaders are done later

                                int decider = Game1.r.Next(1, 75);
                                if (decider == 1 && d.UnplacedPopulation > 0)
                                {
                                    bool Ismale = true;
                                    if (Game1.r.Next(1, 3) == 1)
                                    {
                                        Ismale = false;
                                    }

                                    string Role = "";
                                    string Destiny = "";
                                    Race Race;


                                    if (Game1.r.Next(1, 20) == 1)
                                    {
                                        Race = HumanoidRaces[Game1.r.Next(HumanoidRaces.Count)];
                                    }
                                    else
                                    {
                                        Race = location.PrimaryRace;
                                    }

                                    //decide if the creature will be magical

                                    int DestinyDecider = Game1.r.Next(1, 300);

                                    if (DestinyDecider < 5 && Race == GetRace("nightfell"))
                                    {
                                        Destiny = "warlock";
                                    }
                                    else if (DestinyDecider < 7 && Race == GetRace("luminarch"))
                                    {
                                        Destiny = "sorcerer";
                                    }
                                    else if (DestinyDecider < 8)
                                    {
                                        Destiny = "parasite";
                                    }

                                    string gender = Ismale ? "male" : "female";

                                    string role = Game1.WeightedRandomArchitectProfessions[Game1.r.Next(Game1.WeightedRandomArchitectProfessions.Count)];
                                    Role = role;

                                    Architect architect = new Architect("", gender, Race, Game1.r.Next(14, 60), Role, new List<Object>(), location, d, null, Destiny, 1);
                                    location.HomeCivilization.Citizens.Add(architect);
                                    string Name = GenerateUniqueArchitectName(architect);
                                    architect.Name = Name;

                                    HistoricalEvents.Add($"{Date}{Name} became an influential {Role} in {location.Name}");
                                    location.LocationHistoricalEvents.Add($"{Date}{Name} became an influential {Role} in {location.Name}");

                                    if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog * 3) == 1)
                                    {
                                        AbridgedHistoricalEvents.Add($"{Date}{Name} became an influential {Role} in {location.Name}");
                                    }

                                    d.Architects.Add(architect);

                                    d.UnplacedPopulation = d.UnplacedPopulation - 1;

                                    TotalArchitects = TotalArchitects + 1;
                                }

                                for (int DistrictX = 0; DistrictX < 7; DistrictX++)
                                {
                                    for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                                    {
                                        //wealth 

                                        foreach (Structure s in d.DistrictMap[DistrictX + DistrictZ * 7].Structures)
                                        {
                                            if (s.Type == "forge")
                                            {
                                                WealthIncrease += 1;
                                            }
                                            else if (s.Type == "market")
                                            {
                                                WealthIncrease += 2;
                                            }
                                        }
                                    }
                                }


                                //ok so THIS stuff is all gonna happen once per district

                                //Handle the creation of a Group
                                //make a group
                                //new group
                                //i cant find this area all the time fsr lolloooolooolol

                                if (d.Architects.Count > 0 && SettlementTypes.Contains(location.Type))
                                {
                                    foreach (Architect a in d.Architects)
                                    {
                                        if (a.Group == null)
                                        {
                                            //see if we actually need a group lul

                                            int GroupsAlreadyLikeThis = 0;

                                            foreach (Group g in location.GroupsAtThisLocation)
                                            {
                                                if (g.Type == Game1.ConvertArchitectToGroupType[a.Profession])
                                                {
                                                    GroupsAlreadyLikeThis++;
                                                }
                                            }

                                            if (GroupsAlreadyLikeThis <= Math.Round((decimal)location.TruePopulation() / 500, MidpointRounding.ToNegativeInfinity))
                                            {
                                                if (Game1.r.Next(1, 1000) == 1 && a.Profession != "prophet")
                                                {
                                                    if (Game1.ConvertArchitectToGroupType[a.Profession] != "trade" || location.Market != null)
                                                    {
                                                        Group g = new Group(new List<Architect>(), Game1.ConvertArchitectToGroupType[a.Profession], a, location);
                                                        a.Group = g;
                                                        g.Architects.Add(a);
                                                        Groups.Add(g);

                                                        location.GroupsAtThisLocation.Add(g);
                                                        if (g.Type == "trade")
                                                        {
                                                            for (int i = r.Next(10, 20); i != 0; i--)
                                                            {
                                                                Object bar = new Object(null, "bar", new List<Material> { location.HomeCivilization.CulturalMetal }, Unknown);
                                                                bar.Owner = g;
                                                                g.CaravanItems.Add(bar);
                                                            }
                                                            location.TradersAtThisLocation.Add(g);
                                                            g.TradeRoute.Add(location);
                                                            TradingGroups.Add(g);
                                                        }

                                                        HistoricalEvents.Add(string.Concat(Date, a.Name, " founded ", g.Name, ", a ", g.Type, " group, in ", location.Name));
                                                        if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog) == 1)
                                                        {
                                                            AbridgedHistoricalEvents.Add(string.Concat(Date, a.Name, " founded ", g.Name, ", a ", g.Type, " group, in ", location.Name));
                                                        }
                                                        a.GroupLoyalty = 5;
                                                        location.LocationHistoricalEvents.Add(string.Concat(Date, a.Name, " founded ", g.Name, ", a ", g.Type, " group, in ", location.Name));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                foreach (Architect a in d.ArchitectsToRemove)
                                {
                                    d.Architects.Remove(a);
                                }
                                d.ArchitectsToRemove = new List<Architect>();

                                //Handle civilization leadership changes

                                if (location.Government == null && SettlementTypes.Contains(location.Type))
                                {   
                                    foreach (Group g in location.GroupsAtThisLocation)
                                    {
                                        if(g.Type != "trade")
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, g.Name, " took power in ", location.Name, "."));
                                            if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog) == 1)
                                            {
                                                AbridgedHistoricalEvents.Add(string.Concat(Date, g.Name, " took power in ", location.Name, "."));
                                            }
                                            location.LocationHistoricalEvents.Add(string.Concat(Date, g.Name, " took power in ", location.Name, "."));
                                            location.Government = g;

                                            foreach (Architect a in g.Architects)
                                            {
                                                if (a.District != location.Districts[0])
                                                {
                                                    a.District.Architects.Remove(a);
                                                    location.Districts[0].Architects.Add(a);
                                                    a.District = location.Districts[0];
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                                else if (location.Government == null && SettlementTypes.Contains(location.Type))
                                {

                                }
                                else if (location.Government is Group && SettlementTypes.Contains(location.Type))
                                {
                                    if (((Group)location.Government).Type != "political")
                                    {
                                        foreach (Group g in location.GroupsAtThisLocation)
                                        {
                                            if (g.Type == "political")
                                            {
                                                HistoricalEvents.Add(string.Concat(Date, g.Name, " took power from ", location.Government.Name, " thanks to their credibility and support in ", location.Name, "."));
                                                if (r.Next(1, ChanceToAddHistoricalEventToAbridgedCatalog) == 1)
                                                {
                                                    AbridgedHistoricalEvents.Add(string.Concat(Date, g.Name, " took power from ", location.Government.Name, " thanks to their credibility and support in ", location.Name, "."));
                                                }
                                                location.LocationHistoricalEvents.Add(string.Concat(Date, g.Name, " took power from ", location.Government.Name, " thanks to their credibility and support in ", location.Name, "."));
                                                location.Government = g;

                                                foreach (Architect a in g.Architects)
                                                {
                                                    if (a.District != location.Districts[0])
                                                    {
                                                        a.District.Architects.Remove(a);
                                                        location.Districts[0].Architects.Add(a);
                                                        a.District = location.Districts[0];
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }

                                //Handle architect/architect group actions IN THE DISTRICT


                                //study and write books

                                int Threshold = 500;

                                foreach (Architect a in d.Architects)
                                {
                                    if (a.Profession == "scholar" && a.IsStudying)
                                    {
                                        switch (a.ScholarType)
                                        {
                                            case "mage":
                                                a.MagicStudyPoints += Game1.r.Next(0, 7);
                                                break;
                                            case "engineer":
                                                a.ScienceStudyPoints += Game1.r.Next(0, 7);
                                                break;
                                            case "entertainer":
                                                a.CultureStudyPoints += Game1.r.Next(0, 7);
                                                break;
                                            case "artificer":
                                                a.ScienceStudyPoints += Game1.r.Next(0, 4);
                                                a.MagicStudyPoints += Game1.r.Next(0, 4);
                                                break;
                                            case "bard":
                                                a.CultureStudyPoints += Game1.r.Next(0, 4);
                                                a.MagicStudyPoints += Game1.r.Next(0, 4);
                                                break;
                                            case "sage":
                                                a.CultureStudyPoints += Game1.r.Next(0, 4);
                                                a.ScienceStudyPoints += Game1.r.Next(0, 4);
                                                break;
                                            case "luminary":
                                                a.ScienceStudyPoints += Game1.r.Next(0, 2);
                                                a.CultureStudyPoints += Game1.r.Next(0, 2);
                                                a.MagicStudyPoints += Game1.r.Next(0, 2);
                                                break;
                                            default:
                                                // Handle unknown ScholarType, if needed
                                                break;
                                        }

                                        // Decide to write
                                        if (Game1.r.Next(1, Math.Max(1000 - (a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints), 100)) == 1)
                                        {
                                            string writingType = "";
                                            int totalWeight = a.MagicStudyPoints + a.ScienceStudyPoints + a.CultureStudyPoints;
                                            if (totalWeight == 0) totalWeight = 1;  // Prevent division by zero

                                            // Calculate probabilities based on relative weight of each study point
                                            int magicWeight = (a.MagicStudyPoints * 100) / totalWeight;
                                            int scienceWeight = (a.ScienceStudyPoints * 100) / totalWeight;
                                            int cultureWeight = (a.CultureStudyPoints * 100) / totalWeight;

                                            // Use a random number to decide based on weights
                                            int randomChoice = Game1.r.Next(100);
                                            if (randomChoice < magicWeight)
                                            {
                                                writingType = "poem";
                                            }
                                            else if (randomChoice < magicWeight + scienceWeight)
                                            {
                                                writingType = "book";
                                            }
                                            else
                                            {
                                                writingType = "song";
                                            }

                                            // Create a new Composition object based on the determined type

                                            string Domain = a.AlignedDomains[r.Next(a.AlignedDomains.Count)];

                                            Composition newWork = new Composition(writingType, a, Domain);

                                            if (writingType == "book")
                                            {
                                                string ObjectType = new List<string>() { "scroll", "scroll", "sheet", "book", "book" }[r.Next(5)];

                                                Object o = new Object(newWork.Name, ObjectType, new List<Material>() { d.Location.HomeCivilization.CulturalCloth }, a);
                                                a.StudyBuilding.HistoricalObjects.Add(o);
                                                AllWrittenContent.Add(o);
                                            }
                                            else
                                            {
                                                a.CultureBank.Add(newWork); // Storing poems and songs in the CultureBank
                                            }

                                            // Log historical event
                                            HistoricalEvents.Add(string.Concat(a.Name, " authored a ", writingType, " titled '", newWork.Name, "' in ", location.Name));
                                            location.LocationHistoricalEvents.Add(string.Concat(a.Name, " authored a ", writingType, " titled '", newWork.Name, "' in ", location.Name));
                                        }

                                        //archelevation
                                        if (!a.ScholarType.StartsWith("arch"))
                                        {
                                            if (a.ScholarType == "mage" && a.MagicStudyPoints > Threshold)
                                            {
                                                a.ScholarType = "archmage";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archmage in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archmage in ", location.Name));
                                            }
                                            else if (a.ScholarType == "engineer" && a.ScienceStudyPoints > Threshold)
                                            {
                                                a.ScholarType = "archengineer";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archengineer in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archengineer in ", location.Name));
                                            }
                                            else if (a.ScholarType == "entertainer" && a.CultureStudyPoints > Threshold)
                                            {
                                                a.ScholarType = "archentertainer";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archentertainer in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archentertainer in ", location.Name));
                                            }
                                            else if (a.ScholarType == "artificer" && a.MagicStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                            {
                                                a.ScholarType = "archartificer";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archartificer in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archartificer in ", location.Name));
                                            }
                                            else if (a.ScholarType == "bard" && a.MagicStudyPoints > Threshold / 2 && a.CultureStudyPoints > Threshold / 2)
                                            {
                                                a.ScholarType = "archbard";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archbard in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archbard in ", location.Name));
                                            }
                                            else if (a.ScholarType == "sage" && a.CultureStudyPoints > Threshold / 2 && a.ScienceStudyPoints > Threshold / 2)
                                            {
                                                a.ScholarType = "archsage";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archsage in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archsage in ", location.Name));
                                            }
                                            else if (a.ScholarType == "luminary" && a.CultureStudyPoints > Threshold / 3 && a.ScienceStudyPoints > Threshold / 3 && a.MagicStudyPoints > Threshold / 3)
                                            {
                                                a.ScholarType = "archluminary";
                                                HistoricalEvents.Add(string.Concat(a.Name, " became an archluminary in ", location.Name));
                                                location.LocationHistoricalEvents.Add(string.Concat(a.Name, " became an archluminary in ", location.Name));
                                            }
                                        }

                                        //mage scholars learn spells, each can only discover one in their lifetime, but can learn more from others.

                                        if ((a.ScholarType == "mage" || a.ScholarType == "artificer" || a.ScholarType == "luminary" || a.ScholarType == "bard" || a.ScholarType == "archmage" || a.ScholarType == "archartificer" || a.ScholarType == "archluminary" || a.ScholarType == "archbard") && a.DiscoveredASpell == false && UndiscoveredSpells.Count > 0 && a.MagicStudyPoints > 1800)
                                        {
                                            int SpellID = Game1.r.Next(UndiscoveredSpells.Count);
                                            a.SpellsKnown.Add(UndiscoveredSpells[SpellID]);
                                            location.LocationHistoricalEvents.Add(string.Concat(a.Name, " discovered the secret of ", UndiscoveredSpells[SpellID], " in ", location.Name));
                                            HistoricalEvents.Add(string.Concat(a.Name, " discovered the secret of ", UndiscoveredSpells[SpellID], " in ", location.Name));
                                            DiscoveredSpells.Add(UndiscoveredSpells[SpellID]);
                                            UndiscoveredSpells.RemoveAt(SpellID);
                                            a.DiscoveredASpell = true;
                                        }
                                    }

                                    //destiny

                                    List<string> PossibleInfusionSpells = new List<string>();

                                    if (a.Age >= a.DestinyArrivalYear && a.Profession != a.Destiny && (a.Destiny == "sorcerer" || a.Destiny == "warlock") /*for some reason this is running for everyone and infusing everyone lmaoooo*/)
                                    {
                                        foreach (string s in Game1.AllSpells)
                                        {
                                            if (!a.SpellsKnown.Contains(s))
                                            {
                                                PossibleInfusionSpells.Add(s);
                                            }
                                        }

                                        int SpellCount = Game1.r.Next(3, 8);

                                        for (int i = 0; i < SpellCount; i++)
                                        {
                                            if (PossibleInfusionSpells.Count > 0)
                                            {
                                                int SpellInt = Game1.r.Next(PossibleInfusionSpells.Count);
                                                a.SpellsKnown.Add(PossibleInfusionSpells[SpellInt]);
                                                PossibleInfusionSpells.RemoveAt(SpellInt);
                                            }
                                        }

                                        if (a.Destiny == "warlock")
                                        {
                                            a.Profession = a.Destiny;
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " was infused with incredible power by ", DarkDeity.Name, " in ", location.Name, ", blessed to become an immortal warlock tied to his service."));
                                            location.LocationHistoricalEvents.Add(string.Concat(Date, a.Name, " was infused with incredible power by ", DarkDeity.Name, " in ", location.Name, ", blessed to become an immortal warlock tied to his service."));
                                            a.GroupLoyalty = -10;
                                            a.IsImmortal = true;
                                            a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                        }
                                        else if (a.Destiny == "sorcerer")
                                        {
                                            a.Profession = a.Destiny;
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " was infused with incredible power by ", LightDeity.Name, " in ", location.Name, ", blessed to become an eternal sorcerer tied to his service."));
                                            location.LocationHistoricalEvents.Add(string.Concat(Date, a.Name, " was infused with incredible power by ", LightDeity.Name, " in ", location.Name, ", blessed to become an eternal sorcerer tied to his service."));
                                            a.GroupLoyalty = -10;
                                            a.Inventory.AddRange(LootTableMachine("magictreasure34"));
                                            a.IsImmortal = true;
                                        }
                                    }
                                }


                                //also make sure that architects change their group loyalty based on actions

                                foreach (Architect a in d.Architects)
                                {
                                    //count population
                                    CurrentlyCountingArchitects++;

                                    if (a.Profession == "scholar")
                                    {
                                        //search for a library

                                        if (!a.IsStudying)
                                        {
                                            bool FoundLibrary = false;
                                            for (int DistrictX2 = 0; DistrictX2 < 7; DistrictX2++)
                                            {
                                                for (int DistrictZ2 = 0; DistrictZ2 < 7; DistrictZ2++)
                                                {
                                                    foreach (Structure s in d.DistrictMap[DistrictX2 + DistrictZ2 * 7].Structures)
                                                    {
                                                        if (s.Type == "library")
                                                        {
                                                            FoundLibrary = true;
                                                            a.IsStudying = true;
                                                            a.StudyBuilding = s;
                                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " began studying at ", s.Name, " in ", location.Name));
                                                            break;
                                                        }
                                                    }
                                                    if (FoundLibrary)
                                                    {
                                                        break;
                                                    }
                                                }
                                                if (FoundLibrary)
                                                {
                                                    break;
                                                }
                                            }

                                            if (!FoundLibrary)
                                            {
                                                bool Breakening = false;

                                                //search the world for a civilization library

                                                for (int WorldX = 0; WorldX < Width; WorldX++)
                                                {
                                                    for (int WorldZ = 0; WorldZ < Length; WorldZ++)
                                                    {
                                                        if (WorldMap[WorldX + WorldZ * Width].MyLocation != null)
                                                        {
                                                            if (WorldMap[WorldX + WorldZ * Width].MyLocation.HomeCivilization == a.HomeLocation.HomeCivilization)
                                                            {
                                                                foreach (District D in WorldMap[WorldX + WorldZ * Width].MyLocation.Districts)
                                                                {
                                                                    for (int X = 0; X < 7; X++)
                                                                    {
                                                                        for (int Z = 0; Z < 7; Z++)
                                                                        {
                                                                            foreach (Structure s in D.DistrictMap[X + Z * 7].Structures)
                                                                            {
                                                                                if (s.Type == "library")
                                                                                {
                                                                                    a.NextMigrationLocation = WorldMap[WorldX + WorldZ * Width].MyLocation;
                                                                                    a.IsStudying = true;
                                                                                    a.StudyBuilding = s;
                                                                                    HistoricalEvents.Add(string.Concat(Date, a.Name, " heard of ", s.Name, ", a library in ", WorldMap[WorldX + WorldZ * Width].MyLocation.Name, " and migrated there to study."));
                                                                                    location.LocationHistoricalEvents.Add(string.Concat(Date, a.Name, " heard of ", s.Name, ", a library in ", WorldMap[WorldX + WorldZ * Width].MyLocation.Name, " and migrated there to study."));
                                                                                    HistoricalEvents.Add(string.Concat(Date, a.Name, " began studying at ", s.Name, " in ", location.Name));
                                                                                    location.LocationHistoricalEvents.Add(string.Concat(Date, a.Name, " began studying at ", s.Name, " in ", location.Name));
                                                                                    Breakening = true;
                                                                                    break;
                                                                                }
                                                                            }
                                                                            if (Breakening)
                                                                            {
                                                                                break;
                                                                            }
                                                                        }
                                                                        if (Breakening)
                                                                        {
                                                                            break;
                                                                        }
                                                                    }
                                                                    if (Breakening)
                                                                    {
                                                                        break;
                                                                    }
                                                                }
                                                                if (Breakening)
                                                                {
                                                                    break;
                                                                }
                                                            }
                                                            if (Breakening)
                                                            {
                                                                break;
                                                            }
                                                        }
                                                        if (Breakening)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    if (Breakening)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                foreach (Architect a in d.ArchitectsToAdd)
                                {
                                    d.Architects.Add(a);
                                }
                                d.ArchitectsToAdd = new List<Architect>();


                                // TODO: Handle architect/architect group stability
                                // also utilize group loyalty


                                // TODO: Handle the disbanding of a Group
                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    if (g.Architects.Count == 0)
                                    {
                                        HistoricalEvents.Add(string.Concat(Date, g.Name, " collapsed in ", location.Name, " due to running out of passionate members."));
                                        location.LocationHistoricalEvents.Add(string.Concat(Date, g.Name, " collapsed in ", location.Name, " due to running out of passionate members."));
                                        GroupsToRemove.Add(g);
                                        if (location.Government == g)
                                        {
                                            location.Government = null;
                                        }
                                    }
                                    else if (g.Stability < 1)
                                    {
                                        HistoricalEvents.Add(string.Concat(Date, g.Name, " collapsed in ", location.Name, " due to a disagreement of values."));
                                        location.LocationHistoricalEvents.Add(string.Concat(Date, g.Name, " collapsed in ", location.Name, " due to a disagreement of values."));
                                        foreach (Architect a in g.Architects)
                                        {
                                            a.Group = null;
                                        }
                                        GroupsToRemove.Add(g);
                                        if (location.Government == g)
                                        {
                                            location.Government = null;
                                        }
                                    }
                                    else if (g.MonthsOld > 60 && g.Architects.Count <= 1 && location.Government != g)
                                    {
                                        HistoricalEvents.Add(string.Concat(Date, g.Leader.Name, " disbanded ", g.Name, " to become an individual practitioner in ", location.Name, "."));
                                        location.LocationHistoricalEvents.Add(string.Concat(Date, g.Leader.Name, " disbanded ", g.Name, " to become an individual practitioner in ", location.Name, "."));
                                        g.Leader.Group = null;
                                        GroupsToRemove.Add(g);
                                    }
                                    else if (g.MonthsOld > 180 && g.Architects.Count == 2 && location.Government != g)
                                    {
                                        HistoricalEvents.Add(string.Concat(Date, g.Architects[0].Name, " and ", g.Architects[1].Name, " disbanded ", g.Name, " and stopped traveling together in ", location.Name, "."));
                                        location.LocationHistoricalEvents.Add(string.Concat(Date, g.Architects[0].Name, " and ", g.Architects[1].Name, " disbanded ", g.Name, " and stopped traveling together in ", location.Name, "."));
                                        g.Leader.Group = null;
                                        GroupsToRemove.Add(g);
                                    }
                                }
                                foreach (Group g in GroupsToRemove)
                                {
                                    Groups.Remove(g);
                                    location.GroupsAtThisLocation.Remove(g);
                                }
                                GroupsToRemove = new List<Group>();

                                foreach (Group G in location.GroupsAtThisLocation)
                                {
                                    foreach (Architect a in G.ArchitectsToRemove)
                                    {
                                        a.Group = null;
                                        G.Architects.Remove(a);
                                    }
                                }

                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    g.ArchitectsToRemove = new List<Architect>();
                                }

                                //Handle recruiting of Architects to a group


                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    foreach (Architect a in d.Architects)
                                    {
                                        if (a.Group == null && Game1.ConvertArchitectToGroupType[a.Profession] == g.Type && (!g.ArchitectsWhoDeclined.Contains(a)))
                                        {
                                            if (Game1.r.Next(1, 7) == 1)
                                            {
                                                // denial
                                                if (Game1.r.Next(1, 3) == 1)
                                                {
                                                    HistoricalEvents.Add(string.Concat(Date, a.Name, " requested to join ", g.Name, ", but was denied."));
                                                    g.ArchitectsWhoDeclined.Add(a);
                                                }
                                                else
                                                {
                                                    HistoricalEvents.Add(string.Concat(Date, a.Name, " was invited to join ", g.Name, ", but decided against it."));
                                                    g.ArchitectsWhoDeclined.Add(a);
                                                }
                                            }
                                            else
                                            {
                                                // acceptance
                                                if (Game1.r.Next(1, 3) == 1)
                                                {
                                                    HistoricalEvents.Add(string.Concat(Date, a.Name, " requested to join ", g.Name, ", and was accepted."));
                                                    g.Architects.Add(a);
                                                    d.ArchitectsToRemove.Add(a);
                                                    a.Group = g;
                                                    a.GroupLoyalty = 3;
                                                }
                                                else
                                                {
                                                    HistoricalEvents.Add(string.Concat(Date, g.Name, " requested that ", a.Name, " join them, and ", a.Name, " accepted."));
                                                    g.Architects.Add(a);
                                                    d.ArchitectsToRemove.Add(a);
                                                    a.Group = g;
                                                    a.GroupLoyalty = 3;
                                                }
                                            }
                                        }
                                    }
                                    foreach (Architect a in d.ArchitectsToRemove)
                                    {
                                        d.Architects.Remove(a);
                                    }
                                }

                                // TODO: Handle Architects leaving groups due to loyalty loss or low loyalty and boredom

                                foreach (Architect a in d.Architects)
                                {
                                    if (a.Group != null)
                                    {
                                        if (a.GroupLoyalty <= 0)
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " left ", a.Group.Name, " due to a disagreement with their values."));
                                            a.Group.ArchitectsToRemove.Add(a);
                                            a.Group = null;
                                        }
                                        else if (a.GroupLoyalty <= 2 && Game1.r.Next(1, 100) == 1)
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " left ", a.Group.Name, " due to boredom."));
                                            a.Group.ArchitectsToRemove.Add(a);
                                            a.Group = null;
                                        }
                                    }
                                }

                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    foreach (Architect a in g.ArchitectsToRemove)
                                    {
                                        g.Architects.Remove(a);
                                    }
                                    g.ArchitectsToRemove.Clear();
                                }


                                d.ArchitectsToAdd = new List<Architect>();

                                // TODO: Handle structure building based on population and number of structures, and number of structures city can support.

                                if (!location.IsSavingUpToSettle)
                                {
                                    if (location.Wealth > 5000)
                                    {
                                        if (SettlementTypes.Contains(location.Type))
                                        {
                                            int Build = 0;

                                            if (d.IsPrimary)
                                            {
                                                Build = Game1.r.Next(1, 3);
                                            }
                                            else
                                            {
                                                Build = Game1.r.Next(1, 7);
                                            }

                                            if (Build == 1)
                                            {
                                                int BuildingDecider = Game1.r.Next(1, 30);
                                                string BuildingType = "";
                                                int Windows = 0;
                                                int llOf5 = 0;

                                                List<string> LightingMethods = new List<string>();
                                                List<string> PrimarySmells = new List<string>();
                                                List<Material> Materials = new List<Material>();


                                                if (BuildingDecider < 15)
                                                {
                                                    BuildingType = "house";
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    Windows = Game1.r.Next(0, 2);
                                                    location.Wealth = location.Wealth - 1000;
                                                }
                                                else if (BuildingDecider < 17)
                                                {
                                                    BuildingType = "shrine";
                                                    Windows = Game1.r.Next(0, 3);
                                                    Materials.Add(location.HomeCivilization.CulturalStone);
                                                    location.Wealth = location.Wealth - 2000;
                                                }
                                                else if (BuildingDecider < 19)
                                                {
                                                    BuildingType = "library";
                                                    Windows = Game1.r.Next(0, 4);
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    location.Wealth = location.Wealth - 2000;
                                                }
                                                else if (BuildingDecider < 21)
                                                {
                                                    BuildingType = "tavern";
                                                    Windows = Game1.r.Next(0, 2);
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    location.Wealth = location.Wealth - 2500;
                                                }
                                                else if (BuildingDecider < 22)
                                                {
                                                    BuildingType = "forge";
                                                    Windows = Game1.r.Next(0, 2);
                                                    Materials.Add(location.HomeCivilization.CulturalStone);
                                                    location.Wealth = location.Wealth - 3000;
                                                }
                                                else if (BuildingDecider < 23)
                                                {
                                                    BuildingType = "watchtower";
                                                    Windows = 0;
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    Materials.Add(location.HomeCivilization.CulturalStone);
                                                    location.Wealth = location.Wealth - 2500;
                                                }
                                                else if (location.Market == null)
                                                {
                                                    BuildingType = "market";
                                                    Windows = 0;
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    Materials.Add(location.HomeCivilization.CulturalCloth);
                                                    location.Wealth = location.Wealth - 2500;
                                                }
                                                else
                                                {
                                                    BuildingType = "bighouse";
                                                    Windows = Game1.r.Next(0, 5);
                                                    Materials.Add(location.HomeCivilization.CulturalWood);
                                                    location.Wealth = location.Wealth - 1500;
                                                }

                                                while (LightingMethods.Count == 0)
                                                {
                                                    foreach (string S in location.PrimaryLightingStyles)
                                                    {
                                                        if (Game1.r.Next(1, 4) != 1)
                                                        {
                                                            LightingMethods.Add(S);
                                                        }
                                                    }
                                                }


                                                //find an owner if you want lul
                                                List<Group> PotentialGroups = new List<Group>();

                                                if (BuildingType == "shrine")
                                                {
                                                    foreach (Group g in location.GroupsAtThisLocation)
                                                    {
                                                        if (g.Type == "religious")
                                                        {
                                                            PotentialGroups.Add(g);
                                                        }
                                                    }
                                                }
                                                else if (BuildingType == "market")
                                                {
                                                    foreach (Group g in location.GroupsAtThisLocation)
                                                    {
                                                        if (g.Type == "trade")
                                                        {
                                                            PotentialGroups.Add(g);
                                                        }
                                                    }
                                                }
                                                else if (BuildingType == "watchtower")
                                                {
                                                    foreach (Group g in location.GroupsAtThisLocation)
                                                    {
                                                        if (g.Type == "mercenary" || g.Type == "military")
                                                        {
                                                            PotentialGroups.Add(g);
                                                        }
                                                    }
                                                }
                                                else if (BuildingType != "bighouse")
                                                {
                                                    foreach (Group g in location.GroupsAtThisLocation)
                                                    {
                                                        PotentialGroups.Add(g);
                                                    }
                                                }

                                                Structure s = new Structure(BuildingType, new List<Object>(), new List<Room>(), d.DistrictMap[Game1.r.Next(0, 49)], Materials, PrimarySmells, LightingMethods, llOf5, Windows);
                                                location.AllStructures.Add(s);
                                                if (s.Type == "market")
                                                {
                                                    location.Market = s;
                                                }

                                                if (PotentialGroups.Count == 0)
                                                {
                                                    s.Owner = null;
                                                }
                                                else
                                                {
                                                    s.Owner = PotentialGroups[Game1.r.Next(PotentialGroups.Count)];
                                                }

                                                if (s.Type != "house" && s.Type != "bighouse")
                                                {
                                                    if (s.Owner == null)
                                                    {
                                                        HistoricalEvents.Add(string.Concat(Date, s.Name, ", a ", s.Type, ", was founded by the people of ", location.Name));
                                                    }
                                                    else
                                                    {
                                                        HistoricalEvents.Add(string.Concat(Date, s.Name, ", a ", s.Type, ", was founded by ", s.Owner.Name));
                                                    }
                                                }


                                                d.DistrictMap[Game1.r.Next(0, 49)].Structures.Add(s);
                                            }
                                        }
                                    }
                                }

                                // TODO: Handle architect/architect group creating a new location

                                bool breaken = false;

                                //regular architects leaving to go make sites

                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    if (location.Government != g && location.Wealth > 10000 && location.IsSavingUpToSettle && location.TruePopulation() > 100 && location.ColonizationDesire > 0 && new List<string> { "town", "city", "camp", "village"}.Contains(location.Type))
                                    {
                                        //start looking for a new location to find

                                        int Attempts = 0;

                                        while (Attempts < 10)
                                        {
                                            int XChange = Game1.r.Next(-5, 6);
                                            int ZChange = Game1.r.Next(-5, 6);

                                            if (!(x + XChange < 0 || x + XChange > Width - 1 || z + ZChange < 0 || z + ZChange > Length - 1))
                                            {
                                                if (WorldMap[(x + XChange) + (z + ZChange) * Width].Biome != "ocean" && WorldMap[(x + XChange) + (z + ZChange) * Width].Biome != "void" && WorldMap[(x + XChange) + (z + ZChange) * Width].Biome != "snowpeak" && WorldMap[(x + XChange) + (z + ZChange) * Width].Biome != "mountain" && WorldMap[(x + XChange) + (z + ZChange) * Width].MyLocation == null && WorldMap[(x + XChange) + (z + ZChange) * Width].Blight == Purity)
                                                {
                                                    // Check if group has at least one member from the humanoid races
                                                    bool hasHumanoid = g.Architects.Any(a => HumanoidRaces.Contains(a.Race));

                                                    if (hasHumanoid)
                                                    {
                                                        // Rest of the existing code for settling...
                                                        // Ensure primary race is one of the humanoid races present
                                                        Race primaryRace = g.Architects.First(a => HumanoidRaces.Contains(a.Race)).Race;

                                                        // Code for wealth deduction, race count, and other settlement logic
                                                        int PopulationFollowing = Game1.r.Next(location.TruePopulation() / 10, location.TruePopulation() / 5);

                                                        if (g.Architects.Count() * 200 > 15000)
                                                        {
                                                            location.Wealth -= 14500;
                                                        }
                                                        else
                                                        {
                                                            location.Wealth -= g.Architects.Count() * 200;
                                                        }

                                                        // Dictionary to keep track of race counts
                                                        Dictionary<Race, int> raceCounts = new Dictionary<Race, int>();

                                                        // Counting the races
                                                        foreach (Architect a in g.Architects)
                                                        {
                                                            if (!raceCounts.ContainsKey(a.Race))
                                                            {
                                                                raceCounts[a.Race] = 0;
                                                            }
                                                            raceCounts[a.Race]++;
                                                        }

                                                        // Now primaryRace holds the Race object with the highest count

                                                        LocationBuilderPacket l = new LocationBuilderPacket(g, x + XChange, z + ZChange, "camp", primaryRace, PopulationFollowing, location.MaxColonizationDesire - 1, location.HomeCivilization, new List<Object>(), location);
                                                        LocationBuilderPackets.Add(l);

                                                        // Adjusting population following the settlement
                                                        if (d.UnplacedPopulation > PopulationFollowing)
                                                        {
                                                            d.UnplacedPopulation = (d.UnplacedPopulation - PopulationFollowing) + 5;
                                                        }
                                                        else
                                                        {
                                                            d.UnplacedPopulation = 5;
                                                        }

                                                        location.ColonizationDesire = location.ColonizationDesire - 1;
                                                        location.GroupsAtThisLocationToRemove.Add(g);
                                                        location.IsSavingUpToSettle = false;

                                                        breaken = true;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        continue; // Continue to the next iteration if no settlement occurs
                                                    }
                                                }
                                            }

                                            Attempts++;
                                        }
                                        if (breaken == true)
                                        {
                                            break;
                                        }
                                    }
                                }
                                foreach (Group g in location.GroupsAtThisLocationToRemove)
                                {
                                    location.GroupsAtThisLocation.Remove(g);
                                }

                                //warlock or sorcerer goes to build a spire

                                foreach (Architect a in d.Architects)
                                {
                                    if ((a.Profession == "warlock" || a.Profession == "sorcerer") && location.Type != "spire")
                                    {
                                        //hunt an unnoccupied location

                                        bool Found = false;

                                        int X = 0;
                                        int Z = 0;

                                        while (!Found)
                                        {
                                            X = Game1.r.Next(Width);
                                            Z = Game1.r.Next(Length);

                                            if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void")
                                            {
                                                Found = true;
                                            }
                                        }

                                        LocationBuilderPacket l = new LocationBuilderPacket(a, X, Z, "spire", GetRace(""), 0, 0, null, new List<Object>(), location);
                                        LocationBuilderPackets.Add(l);
                                    }
                                }

                                //adventuring group builds an outpost to store their items
                                List<Group> AdvGroups = new List<Group>();
                                foreach (Group g in Groups)
                                {
                                    if (g.Type == "mercenary")
                                    {
                                        AdvGroups.Add(g);
                                    }
                                }
                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    if (g.Type == "mercenary" && g.Architects.Count >= 3)
                                    {
                                        if (g.Base.Type != "outpost")
                                        {
                                            //find a location
                                            List<(int, int)> PossibleLocations = new List<(int, int)>();

                                            for (int SearchingX = -10; SearchingX < 10; SearchingX++)
                                            {
                                                for (int SearchingZ = -10; SearchingZ < 10; SearchingZ++)
                                                {
                                                    if (g.Leader.Location.X + SearchingX > 0 && g.Leader.Location.X + SearchingX < Width && g.Leader.Location.Z + SearchingZ > 0 && g.Leader.Location.Z + SearchingZ < Length)
                                                    {
                                                        if (WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].MyLocation == null && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "ocean" && WorldMap[(g.Leader.Location.X + SearchingX) + (g.Leader.Location.Z + SearchingZ) * Length].Biome != "void")
                                                        {
                                                            PossibleLocations.Add((g.Leader.Location.X + SearchingX, g.Leader.Location.Z + SearchingZ));
                                                        }
                                                    }
                                                }
                                            }

                                            if (PossibleLocations.Count > 0)
                                            {
                                                (int, int) Coords = PossibleLocations[Game1.r.Next(PossibleLocations.Count)];
                                                LocationBuilderPacket l = new LocationBuilderPacket(g, Coords.Item1, Coords.Item2, "outpost", GetRace(""), 0, 0, g.Leader.Location.HomeCivilization, new List<Object>(), location);
                                                location.GroupsAtThisLocationToRemove.Add(g);

                                                if (location.Government == g)
                                                {
                                                    location.Government = null;
                                                }
                                                LocationBuilderPackets.Add(l);
                                            }
                                        }
                                    }
                                }

                                foreach (Group g in location.GroupsAtThisLocationToRemove)
                                {
                                    location.GroupsAtThisLocation.Remove(g);
                                }
                                location.GroupsAtThisLocationToRemove = new List<Group>();



                                //colossal creates a legendary artifact and builds a sanctum to protect it

                                foreach (Architect a in Colossals)
                                {
                                    if (!a.HasMadeALegendaryArtifact && r.Next(1, 10000) == 1 && UndiscoveredLegendarySpells.Count > 1)
                                    {
                                        string Spell = UndiscoveredLegendarySpells[r.Next(UndiscoveredLegendarySpells.Count)];
                                        UndiscoveredLegendarySpells.Remove(Spell);
                                        DiscoveredLegendarySpells.Add(Spell);

                                        Object o = new Object("", Game1.PossibleMagicalItems[r.Next(Game1.PossibleMagicalItems.Count)], new List<Material> { Metals[r.Next(Metals.Count)] }, false, false, null, a, 5, false, null, null, null, false);
                                        o.Name = GenerateUniqueName("1S" + Game1.r.Next(2, 4) + "s1w", o);
                                        o.SpellContained = Spell;
                                        o.Owner = a;

                                        string MagicPhrase = "";

                                        if (Spell == "ethereal rupture")
                                        {
                                            MagicPhrase = "ravaging the land with fractal exposure";
                                        }
                                        else if (Spell == "emergence")
                                        {
                                            MagicPhrase = "summoning the fallen";
                                        }
                                        else if (Spell == "eternal bind")
                                        {
                                            MagicPhrase = "enslaving all to its will";
                                        }
                                        else if (Spell == "expunge")
                                        {
                                            MagicPhrase = "banishing legends and the memories of them";
                                        }
                                        else if (Spell == "echo")
                                        {
                                            MagicPhrase = "assembling an echo of a legend";
                                        }

                                        HistoricalEvents.Add(string.Concat(Date, a.Name, " created ", o.Name, ", a legendary ", o.Materials[0].Name, " ", o.Type, " capable of ", MagicPhrase, "."));

                                        //find a sanctum location

                                        bool Found = false;

                                        int X = 0;
                                        int Z = 0;

                                        while (!Found)
                                        {
                                            X = Game1.r.Next(Width);
                                            Z = Game1.r.Next(Length);

                                            if (WorldMap[X + Z * Width].MyLocation == null && WorldMap[X + Z * Width].Biome != "void" && WorldMap[X + Z * Width].Biome != "ocean")
                                            {
                                                Found = true;
                                            }
                                        }

                                        LocationBuilderPacket l = new LocationBuilderPacket(a, X, Z, "sanctum", GetRace(""), 0, 0, null, new List<Object> { o }, location);
                                        LocationBuilderPackets.Add(l);
                                    }
                                }


                                //start saving up to settle


                                if (Game1.r.Next(1, 50) == 1 && location.TruePopulation() > 120 && location.ColonizationDesire > 0)
                                {
                                    location.IsSavingUpToSettle = true;
                                }
                                if (location.TruePopulation() < 100)
                                {
                                    location.IsSavingUpToSettle = false;
                                }

                                foreach (Architect a in d.ArchitectsToRemove)
                                {
                                    d.Architects.Remove(a);
                                }
                                d.ArchitectsToRemove = new List<Architect>();

                                //Architects die lul

                                foreach (Architect a in d.Architects)
                                {
                                    //immortality boons

                                    if (a.IsImmortal && a.RecievedImmortalityBuff == false)
                                    {
                                        a.DoIDieOfOldAge = false;
                                        a.RecievedImmortalityBuff = true;
                                        a.TerminalAge = a.TerminalAge + (Game1.r.Next(40, 200));
                                    }


                                    if (a.Age > a.TerminalAge)
                                    {
                                        if (a.DoIDieOfOldAge)
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " died of old age in ", location.Name, "."));
                                        }
                                        else
                                        {
                                            HistoricalEvents.Add(string.Concat(Date, a.Name, " ", Game1.DeathCauses[Game1.r.Next(Game1.DeathCauses.Count)], " in ", location.Name, "."));
                                        }

                                        if (a.Group != null)
                                        {
                                            a.Group.ArchitectsToRemove.Add(a);
                                        }

                                        d.ArchitectsToRemove.Add(a);
                                    }
                                }

                                foreach (Architect a in d.ArchitectsToRemove)
                                {
                                    d.Architects.Remove(a);
                                }
                                d.ArchitectsToRemove = new List<Architect>();


                                foreach (Group g in location.GroupsAtThisLocation)
                                {
                                    foreach (Architect a in g.ArchitectsToRemove)
                                    {
                                        g.Architects.Remove(a);
                                    }
                                }
                                foreach (Group g in GroupsToRemove)
                                {
                                    Groups.Remove(g);
                                    location.GroupsAtThisLocation.Remove(g);
                                }
                                GroupsToRemove = new List<Group>();



                                // TODO: Handle Architect/architect groups moving into/taking structures.

                                // TODO: Handle craftsmen making stuff and instruments
                                // TODO: Handle musicians creating new musical forms
                                // TODO: Handle travel. Remember to remove the group's leader from the location when the leader moves and do the FIFTY OTHER THING  RWHIOWEH RIHIDSAFBISUOEFWSEY



                            }

                            //Handle location type changes

                            if (location.Type == "camp" && location.TruePopulation() > 10)
                            {
                                location.Type = "village";
                            }
                            if (location.Type == "village" && location.TruePopulation() > 200)
                            {
                                location.Type = "town";
                            }
                            if (location.Type == "town" && location.TruePopulation() > 500)
                            {
                                location.Type = "city";
                            }


                            //districts you waited to place

                            foreach (District d in location.DistrictsToAdd)
                            {
                                location.Districts.Add(d);
                            }

                            location.DistrictsToAdd = new List<District>();

                            WorldMap[x + z * Width].MyLocation = location;

                        }
                    }
                }

                //Place the locations you waited to place

                foreach (LocationBuilderPacket l in LocationBuilderPackets)
                {
                    //build locations

                    Location NewLocation = new Location(l.Type, l.PrimaryRace, l.MiscPopulation, Game1.r.Next(1000, 4000), l.ColonizationDesire, l.X, l.Z, l.HomeCivilization, WorldMap[l.X + l.Z * Width]);

                    if(l.Type == "camp")
                    {
                        HistoricalEvents.Add(string.Concat(Date, "After preparing for years, ", l.Government.Name, " left ", l.BaseLocation.Name, " with a following of ", l.MiscPopulation, " people and founded ", NewLocation.Name, "."));
                    }
                    else if (l.Type == "spire")
                    {
                        HistoricalEvents.Add(string.Concat(Date, l.Government.Name, " left ", l.BaseLocation.Name, " and constructed a glorious spire, ", NewLocation.Name));
                        ((Architect)l.Government).OppositionTags.Add("intruders");
                    }
                    else if (l.Type == "sanctum")
                    {
                        HistoricalEvents.Add(string.Concat(Date, l.Government.Name, " built ", NewLocation.Name, " to house ", l.Artifacts[0].Name, "."));
                    }
                    else if (l.Type == "outpost")
                    {
                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.OppositionTags.Add("intruders");
                        }
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        HistoricalEvents.Add(Date + " " + l.Government.Name + " constructed " + NewLocation.Name + " to base his operations.");
                        ((Architect)(l.Government)).Location = NewLocation;
                        ((Architect)(l.Government)).InteractionLocation = NewLocation;

                        ((Architect)(l.Government)).KitOutArchitect(((Architect)(l.Government)).Profession);
                    }

                    AllLocations.Add(NewLocation);
                    NewLocation.UnplacedArtifacts = l.Artifacts;
                    NewLocation.Government = l.Government;
                    
                    if (NewLocation.Type == "camp")
                    {
                        ClaimSwathOfTerritory(NewLocation.HomeCivilization, l.X, l.Z, 2);
                    }

                    WorldMap[l.X + l.Z * Width].MyLocation = NewLocation;
                    WorldMap[l.X + l.Z * Width].Events = new List<InteractableEvent>();

                    if (l.Government is Group)
                    {
                        NewLocation.GroupsAtThisLocation.Add((Group)l.Government);

                        foreach (Architect a in ((Group)l.Government).Architects)
                        {
                            a.NextMigrationLocation = NewLocation;
                        }

                        ((Group)l.Government).Base = NewLocation;
                    }
                    else if (l.Government is Architect)
                    {
                        ((Architect)l.Government).NextMigrationLocation = NewLocation;
                    }

                    //special structures need special stuff
                    if (l.Type == "camp")
                    {
                        //well
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Objects.Add(new Object(null, "well", new List<Material> { NewLocation.HomeCivilization.CulturalStone }, true, true, null, null, 255, false, null, null, null, false));


                        //prism
                        Block chosenBlock = NewLocation.Districts[0].DistrictMap[Game1.r.Next(0, 49)]; 
                        Structure Prism = new Structure("prism", l.Artifacts, new List<Room>(), chosenBlock, new List<Material> { NewLocation.HomeCivilization.CulturalStone }, new List<string>(), new List<string> { Game1.LightingStyles[Game1.r.Next(Game1.LightingStyles.Count)] }, Game1.r.Next(0, 5), Game1.r.Next(0, 4));
                        Prism.Name = GenerateUniqueName("1W 1S2s", Prism);
                        NewLocation.Prism = Prism;
                        NewLocation.AllStructures.Add(Prism);
                        chosenBlock.Structures.Add(Prism);
                    }
                    if (l.Type == "spire")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        //material

                        Material m;


                        if (l.Government is Architect)
                        {
                            if (((Architect)l.Government).Race == GetRace("nightfell"))
                            {
                                m = Darkstone;
                            }
                            else
                            {
                                m = Illuminite;
                            }
                        }
                        else
                        {
                            if (((Group)l.Government).Leader.Race == GetRace("nightfell"))
                            {
                                m = Darkstone;
                            }
                            else
                            {
                                m = Illuminite;
                            }
                        }


                        Structure s = new Structure("spire", l.Artifacts, new List<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new List<Material>() { m }, new List<string>(), new List<string> { "crystals" }, 3, 0);
                        NewLocation.AllStructures.Add(s);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "outpost")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Material m = Stones[Game1.r.Next(Stones.Count)];

                        Structure s = new Structure("outpost", l.Artifacts, new List<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new List<Material>() { m }, new List<string>(), new List<string> { "torches" }, 3, 5);
                        NewLocation.AllStructures.Add(s);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (l.Type == "sanctum")
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure("sanctum", l.Artifacts, new List<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new List<Material>() { Archaeon }, new List<string>(), new List<string> { "crystals" }, 3, 999);
                        NewLocation.AllStructures.Add(s);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }
                    else if (CalamityStructures.Contains(l.Type))
                    {
                        int SX = Game1.r.Next(2, 5);
                        int SZ = Game1.r.Next(2, 5);

                        Structure s = new Structure(l.Type, l.Artifacts, new List<Room>(), NewLocation.Districts[0].DistrictMap[SX + SZ * 7], new List<Material>() { Stones[r.Next(Stones.Count)] }, new List<string>(), new List<string> { "torches" }, 3, 0);
                        NewLocation.AllStructures.Add(s);
                        NewLocation.Districts[0].DistrictMap[SX + SZ * 7].Structures.Add(s);
                    }

                }

                //do all migration
                foreach(Architect a in AllArchitects)
                {
                    if (a.NextMigrationLocation != null)
                    {
                        if(a.District != null)
                        {
                            a.District.ArchitectsToRemove.Add(a);
                        }
                        a.District = a.NextMigrationLocation.Districts[Game1.r.Next(a.NextMigrationLocation.Districts.Count)];
                        a.District.ArchitectsToAdd.Add(a);
                        a.Location = a.NextMigrationLocation;
                        a.NextMigrationLocation = null;
                    }
                }

                //clean up architect locations
                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if (WorldMap[x + z * Width].MyLocation != null)
                        {
                            foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                            {
                                foreach (Architect a in d.ArchitectsToAdd)
                                {
                                    d.Architects.Add(a);
                                }
                                foreach (Architect a in d.ArchitectsToRemove)
                                {
                                    d.Architects.Remove(a);
                                }
                                d.ArchitectsToAdd.Clear();
                                d.ArchitectsToRemove.Clear();
                            }
                        }
                    }
                }

                //polish off locations i hope

                for (int x = 0; x < Width; x++)
                {
                    for (int z = 0; z < Length; z++)
                    {
                        if (WorldMap[x + z * Width].MyLocation != null)
                        {
                            foreach (District d in WorldMap[x + z * Width].MyLocation.Districts)
                            {
                                foreach (Architect a in d.Architects)
                                {
                                    a.District = d;
                                    a.Location = d.Location;
                                }
                            }
                        }
                    }
                }


            }

            Cycle = Cycle + 24192000;
            LivingArchitects = CurrentlyCountingArchitects;
            DeadArchitects = TotalArchitects - LivingArchitects;
        }
    }
}