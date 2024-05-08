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

        public Block[] DistrictMap { get; set; } = new Block[49];

        public string Industry = "";

        public bool HasBeenLoaded { get; set; }

        public District(bool isPrimary, Location l, int unplacedPopulation)
        {
            Location = l; 
            Name = Location.Region.World.GenerateUniqueName("1S" + (Game1.r.Next(3, 4) - 1) + "s", this);
            ReferredToNames.Add(Name);
            IsPrimary = isPrimary;

            for (int DistrictX = 0; DistrictX < 7; DistrictX++)
            {
                for (int DistrictZ = 0; DistrictZ < 7; DistrictZ++)
                {
                    DistrictMap[DistrictX + DistrictZ*7] = new Block(DistrictX, DistrictZ, this);
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

        public void AddItemsAndIncreaseCrafts(List<Object> items)
        {
            foreach (var item in items)
            {
                Location.GeneralItemsWeHave.Add(item);
            }

            Location.HomeCivilization.World.TotalCrafts += items.Count;
        }

        public void SupplyLocation(int Intensity)
        {
            if (Industry == null)
            {
                return;
            }

            string DecidedProduction = Industry;

            if (Game1.r.Next(1, 6) <= 2)
            {
                // 40% chance to make something different
                DecidedProduction = Game1.Industries[Game1.r.Next(Game1.Industries.Count)];
            }

            List<Object> itemsToBeAdded = GenerateItems(DecidedProduction, Intensity);
            AddItemsAndIncreaseCrafts(itemsToBeAdded);
        }

        public List<Object> GenerateItems(string industry, int intensity)
        {
            List<Object> Objects = new List<Object>();

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
                    if (Game1.r.Next(0, 20) == 0)
                        Objects.Add(new Object(null, "pickaxe", new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));
                    break;
                // ... Add other tool items

                case "military":
                    string[] weaponTypes = { "sword", "greatsword", "battle axe", "greataxe", "rapier", "spear", "pike", "mace", "hammer", "shield", "whip", "scourge" };
                    foreach (string weapon in weaponTypes)
                        if (Game1.r.Next(0, 60) == 0) // Random chance to add a weapon
                            Objects.Add(new Object(null, weapon, new List<Material>() { Location.HomeCivilization.CulturalMetal }, null));

                    string[] armorTypes = { "helmet", "chestplate", "gauntlet", "leggings", "boot" };
                    foreach (string armor in armorTypes)
                    {
                        if (Game1.r.Next(0, 60) == 0) // Random chance to add armor
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
                        dyeBottle.ContainedObjects.Add(new Object(null, "dye", new List<Material>() { Game1.MaterialsFromColors[DyeColor][Game1.r.Next(3)] }, null));
                        Objects.Add(dyeBottle);
                    }
                    break;

                case "waspkeeping":
                    if (Game1.r.Next(0, 3) == 0)
                    {
                        Object honeyJar = new Object(null, "jar", new List<Material>() { Game1.GameWorld.Glass }, null);
                        honeyJar.ContainedObjects.Add(new Object(null, "honey", new List<Material>() { Game1.GameWorld.Honey }, null));
                        Objects.Add(honeyJar);
                    }
                    if (Game1.r.Next(0, 10) == 0)
                        Objects.Add(new Object(null, "wax tablet", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                    if (Game1.r.Next(0, 5) == 0)
                        Objects.Add(new Object(null, "candle", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                    if (Game1.r.Next(0, 2) == 0)
                        Objects.Add(new Object(null, "wax cube", new List<Material>() { Game1.GameWorld.Waspwax }, null));
                    break;

                case "fuel":
                    for (int i = Game1.r.Next(0, 9); i != 0; i--)
                        Objects.Add(new Object(null, "fragment", new List<Material> { Game1.GameWorld.Vitalium }, null));
                    break;

                case "masonry":
                    for (int i = Game1.r.Next(0, 6); i != 0; i--)
                        Objects.Add(new Object(null, "brick", new List<Material> { Location.HomeCivilization.CulturalStone }, null));
                    break;

                default:
                    // Handle default case
                    break;
            }

            return Objects;
        }
    }
}
