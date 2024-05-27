using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Object : Entity
    {
        public string Type { get; set; }

        public List<Material> Materials { get; set; } = new List<Material>();
        public string Description = "???";
        public bool IsContainer { get; set; }
        public List<Object> ContainedObjects { get; set; } = new List<Object>();
        public bool IfTrueUseInIfFalseUseOn { get; set; }

        public double LatestUpdateCycle = 0;

        public List<string> Tags = new List<string>();

        public string DyedColor = "none";

        public int YLevelInFeet { get; set; } = 0;
        public int YVelocity { get; set; } = 0;

        public double Weight { get; set; } = 0;

        public int WeaponMaximumRange = 0;

        public Structure Structure;
        public Block Block;
        public Room Room;

        public int HeatInCelsius = 20; //average heat. 45 is too hot to hold.

        public bool IsConsumable = false;
        public string VariableToChange = "";
        public int VariableChange;

        public List<Imbuement> Imbuements = new List<Imbuement>();

        public bool IsWearable;

        public bool RealityAugumented = false;

        public string Rarity;

        public bool IsBodyPart = false;
        public bool MajorArteryIsSevered = false;

        public Entity AirborneTarget { get; set; }
        public int AirbornePower { get; set; } = 0;
        public int AirborneCyclesToHitTarget = 0;
        public Architect Thrower { get; set; }

        public Entity Creator { get; set; }

        public string WordCount { get; set; } = "";
        public string Subject { get; set; }

        public int FireCycles { get; set; } = 0;
        public int WetCycles { get; set; } = 0;
        public int DestabilizedCycles { get; set; } = 0;
        public int FractalCycles = 0;
        public (Region, Location, District, Block, Structure, Room) RematerializeLocation = (null, null, null, null, null, null);

        public bool IsCoveredInPlants { get; set; } = false;

        public List<(string, int)> CoverageValues = new List<(string, int)>();
        public int Coverage = 0;
        public string CoverageName = "";


        public bool IsWeapon { get; set; } = false;
        public string DamageType { get; set; } = "bashing";
        public bool ProjectileAerodynamic { get; set; } = false;
        public int Strength { get; set; } = 1;

        public bool Dissipating = false;

        public int Integrity { get; set; } = 100;

        public bool IsWritable { get; set; } = false;
        public Composition CompositionContent { get; set; } = null;
        public string SpellContained { get; set; } = "";

        public bool IsGeneralGood = false;
        public Entity Owner = null;

        public int Value()
        {
            if (Materials.Count == 0)
            {
                return 0;
            }

            int totalRarity = 0;
            foreach (Material m in Materials)
            {
                totalRarity += m.Rarity;
            }

            double averageMaterialRarity = (double)totalRarity / Materials.Count;
            int value = (int)Math.Round(((averageMaterialRarity * Weight) + (5 * (averageMaterialRarity + 1))));

            return value;
        }

        public int FindObjectGenericStrength()
        {
            List<string> MaterialStrings = new List<string>();
            foreach(Material m in Materials)
            {
                MaterialStrings.Add(m.Type);
            }

            if (MaterialStrings.Contains("metal"))
            {
                return 5;
            }
            else if (MaterialStrings.Contains("gemstone"))
            {
                return 4;
            }
            else if (MaterialStrings.Contains("stone"))
            {
                return 3;
            }
            else if (MaterialStrings.Contains("glass"))
            {
                return 3;
            }
            else if (MaterialStrings.Contains("wood"))
            {
                return 2;
            }
            else if (MaterialStrings.Contains("cloth"))
            {
                return 1;
            }
            else
            {
                return 3;
            }
        }

        public List<TextStorage> TakeDamageFromObject(Object o, int WielderProficiency)
        {
            // Base damage values
            double basePain = 10;
            double baseIntegrityLoss = 10;
            double baseBleeding = 2;
            double baseEnergyLoss = 5;

            // Modifiers based on weapon type
            double painModifier = 1;
            double integrityModifier = 1;
            double bleedingModifier = 1;
            double energyLossModifier = 1;

            // Adjust modifiers based on weapon type
            switch (o.DamageType)
            {
                case "piercing":
                    integrityModifier = 1.5;
                    bleedingModifier = 0.5;
                    energyLossModifier = 1.2;
                    break;
                case "slashing":
                    bleedingModifier = 1.5;
                    integrityModifier = 0.8;
                    painModifier = 1.2;
                    break;
                case "bashing":
                    integrityModifier = 1.2;
                    painModifier = 1.1;
                    energyLossModifier = 1.3;
                    bleedingModifier = 0.3;
                    break;
                case "scourging":
                    bleedingModifier = 2;
                    painModifier = 1.5;
                    integrityModifier = 0.5;
                    break;
            }

            // Proficiency factor
            double proficiencyFactor = 1 + (WielderProficiency * 0.05);

            // Combat intensity factor (example dynamic adjustment)
            double combatIntensityMultiplier = 3; // This value should be dynamically adjusted based on combat conditions

            // Calculate damage outcomes
            int Pain = (int)(basePain * painModifier * proficiencyFactor * combatIntensityMultiplier * (1 - (0.1)*(((Architect)Owner).Focus)));
            int IntegrityDamage = (int)(baseIntegrityLoss * integrityModifier * proficiencyFactor * combatIntensityMultiplier);
            int Bleeding = (int)(baseBleeding * bleedingModifier * proficiencyFactor * combatIntensityMultiplier);
            int EnergyLoss = (int)(baseEnergyLoss * energyLossModifier * proficiencyFactor * combatIntensityMultiplier);

            List<TextStorage> Announcements = new List<TextStorage>();

            // Determine if the attack gets blocked by coverage or armor
            double coverageMissChance = (Coverage / 22.5) * 75;
            if (Game1.r.Next(100) < coverageMissChance)
            {
                Announcements.Add(new TextStorage("The attack is deflected by " + CoverageName + "!", Color.Green));
                return Announcements;
            }

            if (Owner != null && IsBodyPart && Game1.r.Next(100) < ((Architect)Owner).NaturalArmor)
            {
                Announcements.Add(new TextStorage("The attack breaks through and damages " + ((Architect)Owner).ReferredToNames[0] + "'s natural armor!", Color.Green));
                ((Architect)Owner).NaturalArmor -= Game1.r.Next(1, Math.Max(WielderProficiency, 1));
            }
            else if (((Architect)Owner).NaturalArmor > 0)
            {
                Announcements.Add(new TextStorage("The attack damages, but does not pierce " + ((Architect)Owner).ReferredToNames[0] + "'s natural armor!", Color.Green));
                ((Architect)Owner).NaturalArmor -= Game1.r.Next(1, Math.Max(WielderProficiency, 1));
                return Announcements;
            }


            // Apply damage to integrity

            Integrity = Math.Max(0, o.Integrity - IntegrityDamage);

            if (IntegrityDamage > 0)
            {
                Announcements.Add(new TextStorage("The attack damages " + ReferredToNames[0] + "!", Color.Orange));
            }
            else
            {
                Announcements.Add(new TextStorage(ReferredToNames[0] + " is a broken, lifeless husk!", Color.Red));
            }

            if (Bleeding > 5)
            {
                Announcements.Add(new TextStorage("The attack pierces multiple membranes, causing heavy bleeding!", Color.Green));
            }
            else if (Bleeding > 3)
            {
                Announcements.Add(new TextStorage("The attack pierces a membrane, causing bleeding!", Color.Green));
            }
            else if (Bleeding > 1)
            {
                Announcements.Add(new TextStorage("The attack draws a small amount of blood!", Color.Green));
            }

            if (Pain > 10)
            {
                Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " yelps very audibly!", Color.Green));
            }
            else if (Pain > 7)
            {
                Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " winces!", Color.Green));
            }
            else if (Pain > 5)
            {
                Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " takes a breath...", Color.Green));
            }

            ((Architect)Owner).Bleeding += Bleeding;
            ((Architect)Owner).Pain += Pain;
            ((Architect)Owner).Energy -= EnergyLoss;

            return Announcements;
        }


        public void UpdateNames()
        {
            if(LatestUpdateCycle == Game1.GameWorld.Cycle)
            {
                return;
            }

            LatestUpdateCycle = Game1.GameWorld.Cycle;

            ReferredToNames = new List<string>();

            if (this is Door)
            {
                Door door = (Door)this;
                ReferredToNames.Add(door.Direction + " " + Game1.FormatMaterialList(Materials) + " " + Type + " (door " + door.Number + ")");
                ReferredToNames.Add("door " + door.Number);
                return;
            }
            else if (this.IsBodyPart)
            {
                foreach(string s in Owner.ReferredToNames)
                {
                    ReferredToNames.Add(s + "'s " + Type);
                }
                return;
            }

            if (Name != null)
            {
                ReferredToNames.Add(Name + ", " + Game1.FormatMaterialList(Materials) + " " + Type);
                ReferredToNames.Add(Name);
            }
            else
            {
                ReferredToNames.Add(Game1.FormatMaterialList(Materials) + " " + Type);
                ReferredToNames.Add("the " + Game1.FormatMaterialList(Materials) + " " + Type);
            }

            if (Game1.MostRecentPartyTurnArchitect != null)
            {
                if (Game1.GamePlayerParty != null &&
                    (Game1.MostRecentPartyTurnArchitect.LeftHandObject == this ||
                     Game1.MostRecentPartyTurnArchitect.RightHandObject == this ||
                     Game1.MostRecentPartyTurnArchitect.Inventory.Contains(this)))
                {
                    List<string> newItems = new List<string>();

                    foreach (string s in ReferredToNames)
                    {
                        newItems.Add("my " + s);
                    }

                    ReferredToNames.AddRange(newItems);
                }
            }

            if(Type == "shadow storage" && ReferredToNames.Contains("shadow storage"))
            {
                ReferredToNames.Remove("shadow storage");
            }

            if(AirborneTarget != null)
            {
                List<string> Newnames = new List<string>();
                foreach(string s in ReferredToNames)
                {
                    Newnames.Add("airborne " + s);
                }
                ReferredToNames.Clear();
                ReferredToNames.AddRange(Newnames);
            }


            //inside items


            foreach(Object o in ContainedObjects)
            {
                o.UpdateNames();
            }

            ReferredToNames.RemoveAll(s => string.IsNullOrEmpty(s));
        }

        public void UpdateSelfActionsAndSuch()
        {
            //remove bad spells

            if(Game1.GameWorld.DeletedSpells.Contains(SpellContained))
            {
                SpellContained = null;
            }
            if (Game1.GameWorld.DeletedCompositions.Contains(CompositionContent))
            {
                CompositionContent = null;
            }


            //remove bad materials, if this is the first removal add void.

            List<Material> MaterialsToReplace = new List<Material>();

            foreach (Material m in Game1.GameWorld.DeletedMaterials)
            {
                if (Materials.Contains(m) && !MaterialsToReplace.Contains(m))
                {
                    MaterialsToReplace.Add(m);
                }
            }

            int originalCount = Materials.Count;
            Materials.RemoveAll(item => MaterialsToReplace.Contains(item));
            int newCount = Materials.Count;

            if (newCount < originalCount && !Materials.Contains(Game1.GameWorld.Void))
            {
                Materials.Add(Game1.GameWorld.Void);
            }



            //gravity

            if (YLevelInFeet > 0)
            {
                YVelocity += Math.Min(0, YLevelInFeet - 300);
                YLevelInFeet = Math.Min(0, YLevelInFeet - 300);

                if (YLevelInFeet == 0)
                {
                    Integrity = Integrity - YVelocity;

                    if(Game1.LoadedArchitects[Game1.ArchitectIndex].Structure == this.Structure || Game1.LoadedArchitects[Game1.ArchitectIndex].Block == Block)
                    {
                        if (Integrity > 0)
                        {
                            Game1.MakeObservation(ReferredToNames[0] + " vibrates intensely!", Color.Orange);
                        }
                        else
                        {
                            Game1.MakeObservation(ReferredToNames[0] + " vibrates intensely!", Color.Orange);
                        }
                    }
                }
            }

            //burn

            if(FireCycles > 0)
            {
                Integrity -= FireCycles;
                FireCycles--;
            }
        }
        
        public Object (string name, string type, List<Material> materials, bool InOrOn, bool isContainer, Composition content, Entity creator, double weight, bool isGeneralGood, Block b, Structure s, Room r, bool IsWearable)
        {
            Weight = weight;
            Type = type;
            Materials = materials;
            IfTrueUseInIfFalseUseOn = InOrOn;
            IsContainer = isContainer;
            IsGeneralGood = isGeneralGood;
            Creator = creator;

            Block = b;
            Structure = s;
            Room = r;

            Name = name;

            if (IsContainer)
            {
                ContainedObjects = new List<Object>();
            }

            if (Type == "scroll" || Type == "book" || Type == "sheet" || Type == "waxtablet")
            {
                IsWritable = true;
                CompositionContent = content;

                if (Type == "scroll")
                {
                    WordCount = "~"+Game1.r.Next(5, 500)+"0";
                }
                else if (Type == "book")
                {
                    WordCount = "~" + Game1.r.Next(30, 150) + "000";
                }
                else if (Type == "sheet")
                {
                    WordCount = "~" + Game1.r.Next(1, 30) + "0";
                }
                else if (Type == "waxtablet")
                {
                    WordCount = "~" + Game1.r.Next(1, 10) + "0";
                }
            }


            if (Type == "sword" || Type == "greatsword" || Type == "axe" || Type == "greataxe" || Type == "knife")
            {
                IsWeapon = true;
                DamageType = "slashing";
            }
            else if (Type == "rapier" || Type == "spear" || Type == "pike" || Type == "dagger")
            {
                IsWeapon = true;
                DamageType = "piercing";
            }
            else if (Type == "mace" || Type == "hammer" || Type == "shield" || Type == "objectUsedAsWeapon")
            {
                IsWeapon = true;
                DamageType = "bashing";
            }
            else if (Type == "whip" || Type == "flail" || Type == "chain")
            {
                IsWeapon = true;
                DamageType = "scourging";
            }

            ApplyImbuements(0);
            UpdateNames();
        }

        public Object(string name, string type, List<Material> materials, Entity creator)
        {
            Name = name;
            Type = type;
            Materials = materials;
            Creator = creator;

            // Default values for common properties
            IfTrueUseInIfFalseUseOn = false;
            IsContainer = false;
            IsGeneralGood = false;
            IsWritable = false;
            IsWeapon = false;

            // Determine properties based on Type
            switch (Type.ToLower())
            {
                // Basic resource types
                case "bar":
                    Weight = 10000; // e.g., in grams
                    Description = "An ingot used to efficiently store /m.";
                    break;
                case "falling star":
                    Weight = 10000; // e.g., in grams
                    Description = "A bright concentration of energy. It seems highly unstable.";
                    break;
                case "fragment":
                    Weight = 10;
                    Description = "A small shard of /m.";
                    break;
                case "log":
                    Weight = 3000;
                    Description = "A long, cylinderical piece of /m.";
                    break;
                case "stone":
                    Weight = 3000;
                    Description = "A hand-sized chunk of /m.";
                    break;
                case "ore":
                    Weight = 3000;
                    Description = "A hand-sized chunk of /m.";
                    break;
                case "pile":
                    Weight = 3000;
                    Description = "A pile of /m.";
                    break;
                case "bunch":
                    Weight = 300;
                    Description = "A bunch of /m";
                    break;

                //writables, make sure you implement content afterward if using this!!!
                case "scroll":
                    IsWritable = true;
                    CompositionContent = null;
                    WordCount = "~" + Game1.r.Next(5, 500) + "0";
                    Weight = 100; // example weight in grams
                    Description = "A sheet attached to a roller.";
                    break;
                case "book":
                    IsWritable = true;
                    CompositionContent = null;
                    WordCount = "~" + Game1.r.Next(30, 150) + "000";
                    Description = "A bound collection of sheets.";
                    Weight = 500; // example weight
                    break;
                case "sheet":
                    IsWritable = true;
                    CompositionContent = null;
                    WordCount = "~" + Game1.r.Next(1, 30) + "0";
                    Description = "A sheet of /m";
                    Weight = 50; // example weight
                    break;
                case "waxtablet":
                    IsWritable = true;
                    CompositionContent = null;
                    WordCount = "~" + Game1.r.Next(1, 10) + "0";
                    Description = "A tablet on which can be scratched to inscribe.";
                    Weight = 300; // example weight
                    break;


                case "small hat":
                    Weight = 100;
                    IsWearable = true;
                    Description = "A small head covering made of /m.";
                    CoverageValues.Add(("head", 4)); // Smaller coverage than a large hat
                    break;

                case "large hat":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A larger head covering made of /m.";
                    CoverageValues.Add(("head", 6)); // Smaller coverage than a large hat
                    break;
                case "hood":
                    Weight = 150;
                    IsWearable = true;
                    Description = "A head and neck covering made of /m.";
                    CoverageValues.Add(("head", 5));
                    CoverageValues.Add(("neck", 5));
                    break;
                case "cape":
                    Weight = 150;
                    IsWearable = true;
                    Description = "A long, flowing back garment made of /m.";
                    CoverageValues.Add(("left shoulder", 3));
                    CoverageValues.Add(("right shoulder", 3));
                    CoverageValues.Add(("torso", 2)); // Minimal torso coverage as it's open at the front
                    break;
                case "robe":
                    Weight = 150;
                    IsWearable = true;
                    Description = "A long, flowing back garment made of /m.";
                    CoverageValues.Add(("left shoulder", 5));
                    CoverageValues.Add(("right shoulder", 5));
                    CoverageValues.Add(("torso", 5)); // Minimal torso coverage as it's open at the front
                    CoverageValues.Add(("left leg", 3));
                    CoverageValues.Add(("right leg", 3));
                    break;
                case "amulet":
                    Weight = 50;
                    IsWearable = true;
                    Description = "A small, wearable ornament made of /m.";
                    // Amulets do not provide coverage in the context of physical protection
                    break;
                case "flair":
                    Weight = 50;
                    IsWearable = true;
                    Description = "A decorative, wearable item made of /m.";
                    // Flairs, like amulets, typically do not provide coverage
                    break;
                case "left glove":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A left-hand covering made of /m.";
                    CoverageValues.Add(("left hand", 8));
                    break;
                case "right glove":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A left-hand covering made of /m.";
                    CoverageValues.Add(("left hand", 8));
                    break;
                case "left wristwrap":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A left wrapping for a wrist made of /m.";
                    CoverageValues.Add(("left arm", 2)); // Assuming wrist wraps cover a bit of the lower arm
                    break;
                case "right wristwrap":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A right wrapping for a wrist made of /m.";
                    CoverageValues.Add(("right arm", 2));
                    break;
                case "skirt":
                    Weight = 200;
                    IsGeneralGood = true;
                    Description = "A free-flowing lower body garment made of /m.";
                    CoverageValues.Add(("left leg", 4)); // Assuming wrist wraps cover a bit of the lower arm
                    CoverageValues.Add(("right leg", 4));
                    break;
                case "shortsleeve shirt":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A shirt with short sleeves made of /m.";
                    CoverageValues.Add(("torso", 7));
                    CoverageValues.Add(("left shoulder", 4));
                    CoverageValues.Add(("right shoulder", 4));
                    break;
                case "longsleeve shirt":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A shirt with long sleeves made of /m.";
                    CoverageValues.Add(("torso", 7));
                    CoverageValues.Add(("left arm", 7));
                    CoverageValues.Add(("right arm", 7));
                    break;
                case "uppershirt":
                    Weight = 250;
                    IsWearable = true;
                    Description = "An upper body garment made of /m.";
                    CoverageValues.Add(("torso", 8));
                    break;
                case "uppergarment":
                    Weight = 50;
                    IsWearable = true;
                    Description = "An undergarment for the upper body made of /m.";
                    break;
                case "undergarment":
                    Weight = 50;
                    IsWearable = true;
                    Description = "An undergarment for the lower body made of /m.";
                    break;
                case "straps":
                    Weight = 300;
                    IsWearable = true;
                    Description = "Body straps made of /m.";
                    CoverageValues.Add(("torso", 2));
                    break;
                case "pants":
                    Weight = 400;
                    IsWearable = true;
                    Description = "A lower body garment made of /m.";
                    CoverageValues.Add(("left leg", 9));
                    CoverageValues.Add(("right leg", 9));
                    break;
                case "shorts":
                    Weight = 400;
                    IsWearable = true;
                    Description = "A shorter lower body garment made of /m.";
                    CoverageValues.Add(("left leg", 5));
                    CoverageValues.Add(("right leg", 5));
                    break;
                case "kilt":
                    Weight = 350;
                    IsWearable = true;
                    Description = "A knee-length skirt made of /m.";
                    CoverageValues.Add(("left leg", 6)); // Assuming it covers both legs equally
                    CoverageValues.Add(("right leg", 6));
                    break;
                case "wraps":
                    Weight = 350;
                    IsWearable = true;
                    Description = "Body wraps made of /m.";
                    CoverageValues.Add(("torso", 2));
                    break;
                case "left boot":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A sturdy, tall foot covering made of /m.";
                    CoverageValues.Add(("left foot", 9));
                    CoverageValues.Add(("right foot", 9));
                    break;
                case "right boot":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A sturdy, tall foot covering made of /m.";
                    CoverageValues.Add(("left foot", 9));
                    CoverageValues.Add(("right foot", 9));
                    break;
                case "left shoe":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A standard foot covering made of /m.";
                    CoverageValues.Add(("left foot", 7));
                    CoverageValues.Add(("right foot", 7));
                    break;
                case "right shoe":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A standard foot covering made of /m.";
                    CoverageValues.Add(("left foot", 7));
                    CoverageValues.Add(("right foot", 7));
                    break;
                // The trade and craft items below are not wearable in the sense that they provide coverage, so they do not need CoverageValues entries.

                case "jar":
                    Weight = 200;
                    IsGeneralGood = true;
                    IsContainer = true;
                    Description = "A small container made of /m.";
                    break;
                case "salt pouch":
                    Weight = 300;
                    IsGeneralGood = true;
                    IsContainer = true;
                    Description = "A small pouch. The inscription shows it is intended to contain salt.";
                    break;
                case "bottle":
                    Weight = 200;
                    IsGeneralGood = true;
                    IsContainer = true;
                    Description = "A narrow-necked container made of /m.";
                    break;
                case "pepper pouch":
                    Weight = 150;
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A small pouch. The inscription shows it is intended to contain pepper.";
                    break;
                case "paprika pouch":
                    Weight = 150;
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A small pouch. The inscription shows it is intended to contain paprika.";
                    break;
                case "isodust pouch":
                    Weight = 150;
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A small pouch. The inscription shows it is intended to contain isodust.";
                    break;
                case "coffee crate":
                    Weight = 2000;
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A large crate. The icon on the side shows it is intended to contain coffee.";
                    break;
                case "tea crate":
                    Weight = 2000;
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A large crate. The icon on the side shows it is intended to contain tea.";
                    break;
                case "small urn":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A small decorative urn made of /m.";
                    break;
                case "big urn":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A large decorative urn made of /m.";
                    break;
                case "small pot":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A small cooking pot made of /m.";
                    break;
                case "big pot":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A large cooking pot made of /m.";
                    break;
                case "small mug":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small mug made of /m.";
                    break;
                case "big mug":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large mug made of /m.";
                    break;
                case "small bowl":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small bowl made of /m.";
                    break;
                case "big bowl":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large bowl made of /m.";
                    break;
                case "small cup":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small cup made of /m.";
                    break;
                case "big cup":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large cup made of /m.";
                    break;
                case "wax tablet":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A small tablet made of wax for writing.";
                    break;
                case "candle":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A wax candle for lighting.";
                    break;
                case "wax cube":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A cube of wax.";
                    break;
                case "brick":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A solid block made of /m.";
                    break;
                case "honey":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A portion of honey.";
                    break;
                case "spice":
                    Weight = 5;
                    IsGeneralGood = true;
                    Description = "A small quantity of ground /m.";
                    break;
                case "cut gem":
                    Weight = 50;
                    IsGeneralGood = true;
                    Description = "A finely cut /m gemstone.";
                    break;
                case "dye":
                    Weight = 50;
                    IsGeneralGood = true;
                    Description = "A dye made from /m.";
                    break;
                case "gem":
                    Weight = 50;
                    IsGeneralGood = true;
                    Description = "A raw, uncut /m gemstone.";
                    break;

                case "sword":
                    Weight = 1500;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A balanced weapon for slashing, causing bleeding and pain.";
                    break;
                case "knife":
                    Weight = 300;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A versatile tool used for cutting, or as a weapon.";
                    break;
                case "dagger":
                    Weight = 300;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "An aerodynamic weapon often used for throwing or stabbing.";
                    ProjectileAerodynamic = true;
                    break;
                case "greatsword":
                    Weight = 2000;
                    WeaponMaximumRange = 1;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A large sword excelling in causing significant bleeding and damage.";
                    break;
                case "battle axe":
                    Weight = 1500;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A small axe designed for powerful slashing, causing extensive bleeding.";
                    break;
                case "axe":
                    Weight = 1500;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A small axe, versatile as both a tool and weapon for chopping and slashing.";
                    break;
                case "greataxe":
                    WeaponMaximumRange = 1;
                    Weight = 2000;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "An enormous axe, ideal for causing severe bleeding and pain.";
                    break;
                case "rapier":
                    Weight = 1200;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A non-bladed pointed weapon, ideal for quick stabs to the head.";
                    break;
                case "spear":
                    WeaponMaximumRange = 1;
                    Weight = 1200;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A long spear, ideal for quick stabs to the head.";
                    break;
                case "pike":
                    WeaponMaximumRange = 2;
                    Weight = 1200;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A lengthy weapon for focused piercing damage, particularly to the head. It can strike at a significant distance.";
                    break;
                case "pickaxe":
                    Weight = 1200;
                    DamageType = "piercing";
                    Description = "A useful mining tool. Can be used to gather materials.";
                    break;
                case "scythe":
                    Weight = 1200;
                    DamageType = "piercing";
                    Description = "A too lwith a long curved blade used to gather plants.";
                    break;
                case "mace":
                    Weight = 1800;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A solid weapon causing high damage with less bleeding.";
                    break;
                case "hammer":
                    Weight = 1800;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A blunt weapon designed for powerful bashing, inflicting severe damage.";
                    break;
                case "staff":
                    Weight = 800;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A long polished rod suitable for both physical combat and magical use.";
                    break;
                case "shield":
                    Weight = 2500;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A defensive tool that can be used to bash, causing significant damage.";
                    break;
                case "whip":
                    Weight = 600;
                    IsWeapon = true;
                    DamageType = "scourging";
                    Description = "A flexible weapon that can cause high levels of pain and bleeding.";
                    break;
                case "scourge":
                    Weight = 700;
                    IsWeapon = true;
                    DamageType = "scourging";
                    Description = "A multi-tailed whip, causing extensive bleeding and pain.";
                    break;
                case "flail":
                    Weight = 800;
                    IsWeapon = true;
                    DamageType = "scourging";
                    Description = "A medium, spiked ball attached to a chain and rod, causing major bleeding and pain.";
                    break;
                case "chain":
                    Weight = 400;
                    WeaponMaximumRange = 1;
                    IsWeapon = true;
                    DamageType = "scourging";
                    Description = "A lengthy chain attached to a rod, capable causing extensive bleeding and pain.";
                    break;
                case "energy bolt":
                    Weight = 10;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A small projectile fired from an enchanted object.";
                    break;
                case "spark":
                    Weight = 10;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A mysterious concentration of photonic energy.";
                    break;


                //furniture

                // Inside the switch statement of your object constructor

                case "salt":
                    Weight = 100; // Example weight for a pouch of salt
                    IsContainer = true;
                    Description = "A pouch of salt, essential for seasoning and preservation.";
                    break;

                case "pepper":
                    Weight = 100; // Example weight for a pouch of pepper
                    IsContainer = true;
                    Description = "A pouch of pepper, used to spice up meals.";
                    break;

                case "paprika":
                    Weight = 100; // Example weight for a pouch of paprika
                    IsContainer = true;
                    Description = "A pouch of paprika, adding flavor and color to dishes.";
                    break;

                case "isodust":
                    Weight = 100; // Example weight for a pouch of isodust
                    IsContainer = true;
                    Description = "A pouch of isodust, a magical powder with various uses.";
                    break;

                case "urn":
                    Weight = 1500; // Example weight for an urn
                    IsContainer = true;
                    Description = "An urn, often used for storing ashes or as a decorative piece.";
                    break;

                case "pot":
                    Weight = 1000; // Example weight for a pot
                    IsContainer = true;
                    Description = "A pot, commonly used for cooking or storage.";
                    break;

                case "mug":
                    Weight = 300; // Example weight for a mug
                    IsContainer = true;
                    Description = "A mug, suitable for drinking beverages.";
                    break;

                case "bowl":
                    Weight = 400; // Example weight for a bowl
                    IsContainer = true;
                    Description = "A bowl, versatile for serving food.";
                    break;

                case "cup":
                    Weight = 200; // Example weight for a cup
                    IsContainer = true;
                    Description = "A cup, used for drinking various liquids.";
                    break;

                case "small chalice":
                    Weight = 200; // Example weight for a chalice
                    IsContainer = true;
                    Description = "A small chalice, often used for ceremonial purposes.";
                    break;

                case "big chalice":
                    Weight = 500; // Example weight for a chalice
                    IsContainer = true;
                    Description = "A big chalice, typically used in rituals or as a decorative item.";
                    break;


                // Add any additional missing types here...
                case "bookcase":
                    Weight = 15000;
                    IsContainer = true;
                    Description = "A large furniture piece for storing books.";
                    break;

                case "altar":
                    Weight = 20000;
                    IsContainer = true;
                    Description = "A sacred table used for religious or ceremonial purposes.";
                    break;

                case "table":
                    Weight = 10000;
                    IsContainer = true;
                    Description = "A sturdy piece of furniture for dining or work.";
                    break;

                case "chair":
                    Weight = 5000;
                    IsContainer = false;
                    Description = "A seating furniture for one person.";
                    break;

                case "door":
                    Weight = 30000;
                    IsContainer = false;
                    Description = "A large, swinging barrier for entry or exit.";
                    break;

                case "bed":
                    Weight = 20000;
                    IsContainer = true;
                    Description = "A piece of furniture for sleeping or resting.";
                    break;

                case "keg":
                    Weight = 10000;
                    IsContainer = true;
                    Description = "A barrel-like container for storing liquids, often alcoholic.";
                    break;

                case "chest":
                    Weight = 8000;
                    IsContainer = true;
                    Description = "A storage box for valuables and other items.";
                    break;

                case "shadow storage":
                    Weight = 8000;
                    IsContainer = false;
                    Description = "A pedestal upon which sits a glowing void. It hungers for isodust.";
                    break;

                case "forge":
                    Weight = 100000;
                    IsContainer = false;
                    Description = "A heavy structure for smelting and metalworking.";
                    break;

                case "weapon rack":
                    Weight = 10000;
                    IsContainer = true;
                    Description = "A rack for storing and displaying weapons.";
                    break;

                case "tool rack":
                    Weight = 7000;
                    IsContainer = true;
                    Description = "A rack designed for organizing tools.";
                    break;

                case "armor stand":
                    Weight = 7000;
                    IsContainer = true;
                    Description = "A stand for holding and displaying armor.";
                    break;

                case "magma channel":
                    Weight = 30000;
                    IsContainer = true;
                    Description = "A large, structured passage for directing magma flow.";
                    break;

                case "pedestal":
                    Weight = 15000;
                    IsContainer = true;
                    Description = "A base structure for displaying significant objects or statues.";
                    break;

                case "pillar":
                    Weight = 25000;
                    IsContainer = false;
                    Description = "A large column used for structural support or decoration.";
                    break;

                case "barrel":
                    Weight = 12000;
                    IsContainer = true;
                    Description = "A cylindrical container for storing and transporting goods.";
                    break;

                case "bin":
                    Weight = 6000;
                    IsContainer = true;
                    Description = "A container for storing various items.";
                    break;

                case "target board":
                    Weight = 3000;
                    IsContainer = false;
                    Description = "A board used for target practice.";
                    break;

                case "rug":
                    Weight = 2000;
                    IsContainer = false;
                    Description = "A decorative floor covering.";
                    break;

                case "plant":
                    Weight = 1000;
                    IsContainer = false;
                    Description = "A large living organism, typically used for decoration or herbal purposes.";
                    break;
                    
                case "orb":
                    Weight = 0;
                    IsContainer = false;
                    Description = "A levitating, glistening orb. It feels weightless.";
                    break;


                // other amror

                // Inside the switch statement of your object constructor
                case "helmet":
                    Weight = 1500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "A protective headgear made of /m.";
                    CoverageValues.Add(("head", 9)); // High protection for the head
                    break;

                case "chestplate":
                    Weight = 3000;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "A sturdy torso armor made of /m.";
                    CoverageValues.Add(("torso", 10)); // Maximum protection for the torso
                    break;

                case "left gauntlet":
                    Weight = 500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armored gloves for hand and wrist protection made of /m.";
                    CoverageValues.Add(("left hand", 9)); // High protection for the left hand
                    CoverageValues.Add(("left arm", 4)); // Additional protection for the lower arm
                    break;

                case "right gauntlet":
                    Weight = 500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armored gloves for hand and wrist protection made of /m.";
                    CoverageValues.Add(("right hand", 9)); // High protection for the right hand
                    CoverageValues.Add(("right arm", 4)); // Additional protection for the lower arm
                    break;

                case "leggings":
                    Weight = 2000;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armor for leg protection made of /m.";
                    CoverageValues.Add(("left leg", 10)); // Maximum protection for the left leg
                    CoverageValues.Add(("right leg", 10)); // Maximum protection for the right leg
                    break;
                case "textile":
                    Weight = 200;
                    IsGeneralGood = true;
                    Description = "A piece of fabric made of /m.";
                    break;

                case "drink":
                    Weight = 300; // Approximate weight for a mug-sized liquid in grams
                    IsContainer = false; // Assuming the object is just the liquid, not the container
                    Description = "An average portion of /m.";
                    break;

                case "cube":
                    Weight = 10; // Approximate weight for a small cube, like an ice cube, in grams
                    IsContainer = false; // This is a solid object, not a container
                    Description = "A small cube of /m.";
                    break;
                case "block":
                    Weight = 1000; // Approximate weight for a small cube, like an ice cube, in grams
                    IsContainer = false; // This is a solid object, not a container
                    Description = "A block of /m.";
                    break;

                // Case for a "spatial grenade"
                case "spatial grenade":
                    Weight = 250; 
                    IsWeapon = false; // Considering its grenade nature
                    Description = "A strange /m capsule with an unknown function.";
                    break;

                // Case for a "lightning grenade"
                case "lightning grenade":
                    Weight = 250;
                    IsWeapon = false;
                    Description = "A strange /m capsule with an unknown function.";
                    break;



                // potions
                case "lesser energy potion":
                    Weight = 150;
                    IsWeapon = false;
                    IsConsumable = true;
                    VariableToChange = "energy";
                    VariableChange = 30;
                    Description = "A small potion made of /m, filled with pure energy.";
                    break;
                case "energy potion":
                    Weight = 250;
                    IsWeapon = false;
                    IsConsumable = true;
                    VariableToChange = "energy";
                    VariableChange = 60;
                    Description = "A medium sized potion made of /m, filled with pure energy.";
                    break;
                case "greater energy potion":
                    Weight = 400;
                    IsWeapon = false;
                    IsConsumable = true;
                    VariableToChange = "energy";
                    VariableChange = 90;
                    Description = "A large potion made of /m, filled with pure energy.";
                    break;


                default:
                    throw new Exception("Trying to create an unimplemented object!");

            }

            List<string> Mater = new List<string>();
            foreach(Material m in Materials)
            {
                Mater.Add(m.Name);
            }
            string MaterialList = Game1.FormatList(Mater);

            Description.Replace("/m", MaterialList);

            //rarity

            if(IsWearable || IsWeapon)
            {
                int CoolioNumber = 500;
                if(Creator is Architect)
                {
                    CoolioNumber -= ((Architect)Creator).Creativity * 25;
                }

                

                int rarityInt = Game1.r.Next(1, CoolioNumber);

                if (rarityInt == 1)
                {
                    Rarity = "epic";
                }
                else if (rarityInt < 5)
                {
                    Rarity = "rare";
                }
                else if (rarityInt < 200)
                {
                    Rarity = "uncommon";
                }
                else
                {
                    Rarity = "common";
                }
            }


            ApplyImbuements(0);
            UpdateNames();
        }

        public void ApplyImbuements(int Extra)
        {
            int Imbuements = Extra;

            this.Imbuements.Clear();

            switch (this.Rarity)
            {
                case "common": Imbuements += 0; break;
                case "uncommon": Imbuements += 1; break;
                case "rare": Imbuements += 2; break;
                case "epic": Imbuements += 3; break;
                case "legendary": Imbuements += 4; break;
                case "mythical": Imbuements += 5; break;
                case "exalted": Imbuements += 7; break;
                default: break; // Handle unknown rarity
            }
            List<string> PassiveConditions = new List<string>()
            {
                "multipleenemies", // when fighting multiple enemies
                "grounded",        // when on the ground
                "diminished",      // when energy is below 40-60%
                "lowlight",        // when in low light
                "stagnant",        // when you have spent 5 cycles in one block or room
                "maxenergy"        // when energy is maxed
            };
            List<string> PassiveEffects = new List<string>()
            {
                "+attack",         // +% attack power
                "+shield",         // +% shield effectiveness
                "+dodge",          // +% dodge chance
                "+redirection",    // +% redirection chance
                "+bash",           // +% bashing resistance
                "+pierce",         // +% piercing resistance
                "+slash",          // +% slashing resistance
                "+scourge",        // +% scourging resistance
                "+stealth",          // become harder to see and target
                "+heal",           // +% enhanced healing capability
                "+regen"           // +X energy regen/cycle
            };
            List<string> TriggerConditions = new List<string>()
            {
                "ondodge",         // when you dodge
                "onblock",         // when you block
                "onparry",         // when you parry
                "onredirect",      // when you redirect
                "oncast",          // when you cast a spell
                //"onhpgain",        // when you gain non-regeneration HP
                "ondamage",        // when you take damage
                "onattack"         // when you successfully attack
            };
            List<string> TriggerEffects = new List<string>()
            {
                "barrier",         // generate a barrier stack
                "projectile",      // launch projectile at nearest hostile
                "ignite",          // ignite the nearest hostile
                "destabilize",     // destabilize the nearest hostile
                "dismiss"          // gain dismissal for one turn
            };
            for (int i = 0; i < Imbuements; i++)
            {
                bool isTrigger = Game1.r.Next(2) == 0;
                string conditionOrTrigger = "";
                string buffOrEffect = "";
                int firstPower = 0;
                int secondPower = 0;

                if (isTrigger)
                {
                    int triggerIndex = Game1.r.Next(TriggerConditions.Count);
                    conditionOrTrigger = TriggerConditions[triggerIndex];
                    int effectIndex = Game1.r.Next(TriggerEffects.Count);
                    buffOrEffect = TriggerEffects[effectIndex];
                    // No power values assigned for triggers in this example
                }
                else
                {
                    int passiveIndex = Game1.r.Next(PassiveConditions.Count);
                    conditionOrTrigger = PassiveConditions[passiveIndex];
                    int buffIndex = Game1.r.Next(PassiveEffects.Count);
                    buffOrEffect = PassiveEffects[buffIndex];
                    // Example power calculation for passive buffs
                    if (buffOrEffect.Equals("+heal") || buffOrEffect.Equals("+regen"))
                    {
                        // Assuming heal and regen buffs require specific power values
                        secondPower = Game1.r.Next(5, 21); // Example range from 5 to 20
                    }
                    // For 'diminished' condition, set a specific range for the firstPower
                    if (conditionOrTrigger.Equals("diminished"))
                    {
                        firstPower = Game1.r.Next(40, 61); // Randomly choose a value between 40 to 60
                    }
                }

                this.Imbuements.Add(new Imbuement(isTrigger, conditionOrTrigger, buffOrEffect, firstPower, secondPower));
            }
        }

        public void Fractallize(int Cycles)
        {
            FractalCycles = Cycles;
            RematerializeLocation = (this.Block.District.Location.Region, this.Block.District.Location, this.Block.District, this.Block, this.Structure, this.Room);
            Game1.GameWorld.FractalObjects.Add(this);

            if (Room != null)
            {
                Room.Objects.Remove(this);
            }
            else if (Block != null)
            {
                Block.Objects.Remove(this);
            }
        }

        public Object()
        {

        }
    }
}
