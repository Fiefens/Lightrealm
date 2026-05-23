using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Lightrealm
{
    [Serializable]
    public class Object : Entity
    {
        public string Type { get; set; }

        public EntityList<Material> Materials { get; set; } = new EntityList<Material>();

        public bool IsConstructBolt = false;

        public string Description { get; set; } = "???";
        public bool IsContainer { get; set; }

        public bool OopsIDroppedIt { get; set; } = false;

        public bool IsTwoHanded { get; set; } = false;
        public bool IsLight { get; set; } = false;

        public string AmuletGift = "";

        public Location ConsoleDropLocation = null;

        public Quest QuestConsoleUsedFor = null;

        public Letter LetterContent { get; set; } = null;

        public List<string> GetMaterialNames()
        {
            if (Materials == null) return new List<string>(); // Return an empty list if Materials is null

            return Materials.Select(material => material?.Name).Where(name => name != null).ToList();
        }

        public Architect LastToggler = null;

        public void Delete()
        {
            foreach(Object o in ContainedObjects)
            {
                o.Delete();
            }

            foreach(Imbuement i in Imbuements)
            {
                i.Delete();
            }
            Game1.GameWorld.EntityLedger.Remove(ID);
            Game1.AnnouncementEntitiesToDeleteThisCycle.Add(ID);
        }

        public bool ClothingVisible = true;


        public double Rotation = Game1.GameWorld.rnd.NextDouble() * 2 * Math.PI;
        public int Distance;

        public bool SpecialLooted;

        public string StoredInvocation = null;

        public bool ConsoleOn = false;

        public EntityList<Object> ContainedObjects { get; set; } = new EntityList<Object>();

        public bool IfTrueUseInIfFalseUseOn { get; set; } = false;
        public double LatestUpdateCycle { get; set; } = 0;
        public List<string> Tags { get; set; } = new List<string>();
        public string DyedColor { get; set; } = "none";
        public bool IsMagical { get; set; } = false;
        public int YLevelInFeet { get; set; } = 0;
        public int YVelocity { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public int WeaponMaximumRange { get; set; } = 0;
        public bool RealityAugmented { get; set; } = false;

        public Structure TemporaryStructureStorage { get; set; } = null;

        public EntityList<Architect> AwareArchitects = new EntityList<Architect>();

        public EntityList<Entity> CarvedSymbols = new EntityList<Entity>();
        public string Symbols = "";

        public void UpdateCarvedSymbols()
        {
            Symbols = Game1.FormatAndList(CarvedSymbols.Select(s => s.ReferredToNames[0]).ToList());
        }

        public Structure Structure => Room?.Structure;
        public bool Polished = false;
        public bool Cleaned = false;
        public Block Block;
        public Room Room;



        public void PlaySound()
        {
            if (Materials[0].Type == "cloth")
                Game1.SFX.Add(Game1.Cloth);
            else if (Materials[0].Type == "fiber")
                Game1.SFX.Add(Game1.Cloth);
            else if (Materials[0].Type == "gemstone")
                Game1.SFX.Add(Game1.LightMetalOrGlass);
            else if (Materials[0].Type == "glass")
                Game1.SFX.Add(Game1.LightMetalOrGlass);
            else if (Materials[0].Type == "ice")
                Game1.SFX.Add(Game1.Metal);
            else if (Materials[0].Type == "organic")
                Game1.SFX.Add(Game1.Leather);
            else if (Materials[0].Type == "metal")
                Game1.SFX.Add(Game1.Metal);
            else if (Materials[0].Type == "metaphysic")
                Game1.SFX.Add(Game1.Leather);
            else if (Materials[0].Type == "sand")
                Game1.SFX.Add(Game1.Leather);
            else if (Materials[0].Type == "stone")
                Game1.SFX.Add(Game1.OpenInv);
            else if (Materials[0].Type == "wood")
                Game1.SFX.Add(Game1.Leather);
            else if (Materials[0].Name == "vitalium")
                Game1.SFX.Add(Game1.Money);
            else
                Game1.SFX.Add(Game1.Cloth);
        }



        public int HeatInCelsius { get; set; } = 20;
        public bool IsConsumable { get; set; } = false;

        public EntityList<Imbuement> Imbuements { get; set; } = new EntityList<Imbuement>();

        public bool IsWearable { get; set; }
        public string Rarity { get; set; }
        public int Exposure { get; set; } = 0;
        public bool IsBodyPart { get; set; } = false;
        public bool MajorArteryIsSevered { get; set; } = false;
        public Entity AirborneTarget;
        public int AirbornePower { get; set; } = 0;
        public int AirborneCyclesToHitTarget { get; set; } = 0;
        public Architect Thrower;
        public Entity Creator;

        public int FireSeconds { get; set; } = 0;
        public int WetCycles { get; set; } = 0;
        public int DestabilizedCycles { get; set; } = 0;
        public int FractalCycles { get; set; } = 0;
        public (Location, District, Block, Structure, Room) RematerializeLocation;


        public int PlantCycles { get; set; } = 0;

        public List<(string, int)> CoverageValues { get; set; } = new List<(string, int)>();
        public int Coverage { get; set; } = 0;
        public string CoverageName { get; set; } = "";

        public bool IsWeapon { get; set; } = false;
        public string DamageType { get; set; } = "bashing";
        public bool ProjectileAerodynamic { get; set; } = false;
        public int Strength { get; set; } = 1;
        public Composition CompositionContent;
        public Entity SpecialKnowledge;
        public bool IsGeneralGood { get; set; } = false;
        public Entity Owner;

        public bool Dissipating { get; set; } = false;
        public int Integrity { get; set; } = 100;
        public bool IsWritable { get; set; } = false;

        public virtual int Value()
        {
            if (Materials.Count() == 0)
            {
                return 0;
            }


            if (this.Type == "fragment" && Materials[0].Name == "vitalium")
                return 10;
            if (this.Type == "prism" && Materials[0].Name == "vitalium")
                return 500;

            if (this.Type == "log" || this.Type == "stone" || this.Type == "ore" || this.Type == "pile" || this.Type == "bunch" || this.Type == "block")
                return 1;

            double totalRarity = 0;
            foreach (Material m in Materials)
            {
                totalRarity += m.Rarity;
            }

            double averageMaterialRarity = (double)totalRarity / Materials.Count();
            int value = (int)Math.Round(((averageMaterialRarity * (Weight/2)) + (5 * (averageMaterialRarity + 1))));

            if (Polished)
            {
                value = (int)Math.Round(value * 1.2);
            }
            if (Cleaned)
            {
                value = (int)Math.Round(value * 1.1);
            }

            value += ContainedObjects.Sum(o => o.Value());

            return value;
        }

        public virtual int FindObjectGenericStrength()
        {
            List<string> MaterialStrings = new List<string>();
            foreach (Material m in Materials)
            {
                MaterialStrings.Add(m.Type);
            }

            int BasePower = 0;

            if (MaterialStrings.Contains("metal"))
            {
                BasePower = 5;
            }
            else if (MaterialStrings.Contains("gemstone"))
            {
                BasePower = 4;
            }
            else if (MaterialStrings.Contains("stone"))
            {
                BasePower = 3;
            }
            else if (MaterialStrings.Contains("glass"))
            {
                BasePower = 3;
            }
            else if (MaterialStrings.Contains("wood"))
            {
                BasePower = 2;
            }
            else if (MaterialStrings.Contains("cloth"))
            {
                BasePower = 1;
            }
            else
            {
                BasePower = 3;
            }

            return BasePower + Materials.Max(m => m.Toughness);
        }

        public TextStorage UpdateExposure(int IncreaseDecrease)
        {
            int InitialExposure = Exposure;
            Exposure = Math.Max(0, Exposure + IncreaseDecrease); // Ensure exposure doesn't go below 0

            // Related body-part logic
            if (Owner != null && ((Architect)Owner).BodyParts != null)
            {
                if (Type == "left arm")
                {
                    var leftHand = ((Architect)Owner).FindBodyPart("left hand");
                    if (leftHand != null)
                    {
                        leftHand.Exposure = Math.Max(0, leftHand.Exposure + (int)(IncreaseDecrease * 0.5));
                    }
                }
                else if (Type == "right arm")
                {
                    var rightHand = ((Architect)Owner).FindBodyPart("right hand");
                    if (rightHand != null)
                    {
                        rightHand.Exposure = Math.Max(0, rightHand.Exposure + (int)(IncreaseDecrease * 0.5));
                    }
                }
                else if (Type == "left leg")
                {
                    var leftFoot = ((Architect)Owner).FindBodyPart("left foot");
                    if (leftFoot != null)
                    {
                        leftFoot.Exposure = Math.Max(0, leftFoot.Exposure + (int)(IncreaseDecrease * 0.5));
                    }
                }
                else if (Type == "right leg")
                {
                    var rightFoot = ((Architect)Owner).FindBodyPart("right foot");
                    if (rightFoot != null)
                    {
                        rightFoot.Exposure = Math.Max(0, rightFoot.Exposure + (int)(IncreaseDecrease * 0.5));
                    }
                }
                // Inverse relationships
                else if (Type == "left hand")
                {
                    var leftArm = ((Architect)Owner).FindBodyPart("left arm");
                    if (leftArm != null)
                    {
                        leftArm.Exposure = Math.Max(0, leftArm.Exposure + (int)(IncreaseDecrease * 2));
                    }
                }
                else if (Type == "right hand")
                {
                    var rightArm = ((Architect)Owner).FindBodyPart("right arm");
                    if (rightArm != null)
                    {
                        rightArm.Exposure = Math.Max(0, rightArm.Exposure + (int)(IncreaseDecrease * 2));
                    }
                }
                else if (Type == "left foot")
                {
                    var leftLeg = ((Architect)Owner).FindBodyPart("left leg");
                    if (leftLeg != null)
                    {
                        leftLeg.Exposure = Math.Max(0, leftLeg.Exposure + (int)(IncreaseDecrease * 2));
                    }
                }
                else if (Type == "right foot")
                {
                    var rightLeg = ((Architect)Owner).FindBodyPart("right leg");
                    if (rightLeg != null)
                    {
                        rightLeg.Exposure = Math.Max(0, rightLeg.Exposure + (int)(IncreaseDecrease * 2));
                    }
                }
            }

            if (Exposure > 50)
            {
                ((Architect)Creator).CompanionMessage("repositionorretract", this.Type);
            }

            if (Exposure > 50 && InitialExposure <= 50)
            {
                return new TextStorage(ReferredToNames[0] + " is very exposed!", Color.Orange, new EntityList<Entity>() { this });
            }
            else
            {
                return null;
            }
        }

        public void AnnounceToParty(string announcement, Color color, EntityList<Entity> Entities)
        {
            foreach (Architect a in Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects)
            {
                if ((a.Room != null && a.Room.Objects.Contains(this)) || (a.Room == null && a.Block.Objects.Contains(this)))
                {
                    Game1.MakeObservation(announcement, color, Entities);
                    break;
                }
            }
        }

        public virtual List<TextStorage> TakeDamageFromObject(Object o, int Power,  /*only used for announcements*/   Architect MeleeAttacker, string DescriptiveVerb)
        {
            List<TextStorage> Announcements = new List<TextStorage>();

            if (IsBodyPart)
            {
                UpdateExposure(-(15 * ((Architect)Owner).Dexterity));


                // Base damage values
                double basePain = 20;
                double baseIntegrityLoss = 20;
                double baseBleeding = 2;
                double baseEnergyLoss = 10;

                // Get modifiers from Game1
                var (painModifier, integrityModifier, bleedingModifier, energyLossModifier) = Game1.GetModifiers(o.DamageType);

                // Proficiency factor
                double PowerFactor = 1 + (Power * 0.05);
                double weaponFactor = 1 + (o.FindObjectGenericStrength() * 0.05);
                double targetFocusMod = 1 - (0.1 * ((Architect)Owner).Focus);

                // Calculate damage outcomes
                int Pain = (int)(basePain * painModifier * PowerFactor * weaponFactor * targetFocusMod);
                int IntegrityDamage = (int)(baseIntegrityLoss * integrityModifier * weaponFactor * PowerFactor);
                int Bleeding = (int)(baseBleeding * bleedingModifier * PowerFactor * weaponFactor);
                int EnergyLoss = (int)(baseEnergyLoss * energyLossModifier * PowerFactor * weaponFactor);



                if (o.IsConstructBolt || o.Type == "star")
                {
                    Pain /= 2;
                    IntegrityDamage /= 2;
                    Bleeding /= 2;
                    EnergyLoss /= 2;
                }

                //heat damage
                if (o.HeatInCelsius > 50)
                {
                    Announcements.Add(new TextStorage("The weapon sears " + o.ReferredToNames[0] + "!", Color.OrangeRed, new EntityList<Entity>(){}));
                    EnergyLoss += 8;
                }


                string EnsureThePrefix(string phrase)
                {
                    if (string.IsNullOrWhiteSpace(phrase)) return phrase;

                    // Trim and check if it starts with "the" (case-insensitive)
                    string trimmedPhrase = phrase.Trim();
                    if (trimmedPhrase.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                    {
                        return trimmedPhrase;
                    }

                    return "The " + trimmedPhrase;
                }

                if (Game1.GameWorld.rnd.Next(100) < Coverage && Game1.TutorialActive == false)
                {
                    // Roll again to determine full deflection or partial reduction
                    int deflectRoll = Game1.GameWorld.rnd.Next(100);

                    if (deflectRoll < 20)
                    {
                        // Fully deflected
                        Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " is fully deflected by " + CoverageName + "!", Color.Green, new EntityList<Entity>() { o }));
                        return Announcements;
                    }
                    else
                    {
                        // Half damage applied
                        Pain /= 2;
                        IntegrityDamage /= 2;
                        Bleeding /= 2;
                        EnergyLoss /= 2;

                        Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + "'s strike is stifled by " + CoverageName + "!", Color.Green, new EntityList<Entity>() { o }));
                    }
                }

                if (Owner != null && Owner is Architect a && IsBodyPart && Game1.TutorialActive == false)
                {
                    if (Game1.GameWorld.rnd.Next(100) < Math.Min(50, a.BarrierStacks * 10))
                    {
                        Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " is blocked by a barrier stack!", Color.LimeGreen, new EntityList<Entity>() { o }));
                        a.BarrierStacks--;
                        return Announcements;
                    }

                    int armorDamage = Game1.GameWorld.rnd.Next(3, Math.Max(Power, 1) + 4);
                    if (Game1.GameWorld.rnd.Next(100) < a.NaturalArmor)
                    {
                        Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " breaks through " + a.ReferredToNames[0] + "'s natural armor!", Color.Green, new EntityList<Entity>() { o, a }));
                        a.NaturalArmor -= armorDamage;
                    }
                    else if (a.NaturalArmor > 0)
                    {
                        Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " damages, but does not pierce " + a.ReferredToNames[0] + "'s natural armor!", Color.Green, new EntityList<Entity>() { o, a }));
                        a.NaturalArmor -= armorDamage;
                        return Announcements;
                    }
                }

                // Apply damage to integrity
                Integrity = Math.Max(0, o.Integrity - IntegrityDamage);

                if (IntegrityDamage > 0)
                {
                    Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " damages " + ReferredToNames[0] + "!", Color.Orange, new EntityList<Entity>() { o, this }));
                }

                if(IsBodyPart && Integrity <= 0)
                {
                    Announcements.Add(new TextStorage(ReferredToNames[0] + " is a broken, lifeless husk!", Color.Red, new EntityList<Entity>() { this }));
                }

                if (Bleeding > 5)
                {
                    Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " pierces multiple membranes, causing heavy bleeding!", Color.Green, new EntityList<Entity>() { o }));
                }
                else if (Bleeding > 3)
                {
                    Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " pierces a membrane, causing bleeding!", Color.Green, new EntityList<Entity>() { o }));
                }
                else if (Bleeding > 1)
                {
                    Announcements.Add(new TextStorage(EnsureThePrefix(o.ReferredToNames[0]) + " draws a small amount of blood!", Color.Green, new EntityList<Entity>() {  o }));
                }


                if(((Architect)Creator).IsAlive)
                {
                    if (Pain > 20)
                    {
                        Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " yelps very audibly!", Color.Green, new EntityList<Entity>() { ((Architect)Creator) }));
                    }
                    else if (Pain > 14)
                    {
                        Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " winces!", Color.Green, new EntityList<Entity>() { ((Architect)Creator) }));
                    }
                    else if (Pain > 7)
                    {
                        Announcements.Add(new TextStorage(((Architect)Creator).ReferredToNames[0] + " takes a breath...", Color.Green, new EntityList<Entity>() { ((Architect)Creator) }));
                    }
                }

                ((Architect)Owner).Bleeding += Bleeding;
                ((Architect)Owner).Pain += Pain;

                ((Architect)Owner).Energy -= EnergyLoss;
                if (((Architect)Owner).Energy <= 0)
                {
                    if (MeleeAttacker != null)
                    {
                        if (DescriptiveVerb.EndsWith("e"))
                        {
                            ((Architect)Owner).DeathCause = "was " + DescriptiveVerb + "d to death by " + MeleeAttacker.Name;
                        }
                        else
                        {
                            ((Architect)Owner).DeathCause = "was " + DescriptiveVerb + "ed to death by " + MeleeAttacker.Name;
                        }
                    }
                    else if (o.Thrower != null)
                    {
                        ((Architect)Owner).DeathCause = "was brought down by a " + o.Type + " thrown by " + o.Thrower.Name;
                    }
                    else if (!string.IsNullOrEmpty(DescriptiveVerb))
                    {
                        if (DescriptiveVerb.EndsWith("e"))
                        {
                            ((Architect)Owner).DeathCause = "was " + DescriptiveVerb + "d to death";
                        }
                        else
                        {
                            ((Architect)Owner).DeathCause = "was " + DescriptiveVerb + "ed to death";
                        }
                    }
                    else
                    {
                        ((Architect)Owner).DeathCause = "died mysteriously";
                    }



                    if (MeleeAttacker != null)
                    {
                        MeleeAttacker.TryComment("onkill", 50);
                    }
                }
                


                if (((Architect)Owner).Energy < 1 && MeleeAttacker != null && MeleeAttacker.FinaleReady)
                {
                    Announcements.Add(new TextStorage(((Architect)Owner).ReferredToNames[0] + " radiates energy in a grand finale!", Color.Green, new EntityList<Entity>() { ((Architect)Owner) }));

                    EntityList<Architect> nearbyPeoples = ((Architect)Owner).Room != null ? ((Architect)Owner).Room.Architects : ((Architect)Owner).Block.Architects;
                    foreach (Architect A in nearbyPeoples)
                    {
                        if (A.TargetArchitect == MeleeAttacker ||
        (Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Contains(MeleeAttacker) &&
         Game1.GameWorld.GamePlayerAssociation.ActiveParty.Architects.Any(architect =>
             architect.TargetArchitect == A &&
             (architect.Task == "killtarget" || architect.Task == "disabletarget"))))
                        {
                            A.Energy -= 60;
                            A.Bleeding += 10;
                            Announcements.Add(new TextStorage(A.ReferredToNames[0] + " looks critically wounded!", Color.Green, new EntityList<Entity>() { A }));
                        }
                    }
                }

                if(MeleeAttacker != null)
                    MeleeAttacker.FinaleReady = false;
            }
            else
            {
                // Calculate toughness for both the throwing object and the target
                int throwingObjectToughness = o.Materials.Max(material => material.Toughness) / 2 + 5;
                int targetToughness = this.Materials.Max(material => material.Toughness) / 2 + 5;

                // Base integrity loss calculation
                double throwingObjectIntegrityLoss = 0;
                double targetIntegrityLoss = 0;

                // Calculate weight-based factors
                double weightFactor = o.Weight / this.Weight;

                foreach (var material in o.Materials)
                {
                    double integrityLossFactor = GetMaterialIntegrityLossFactor(material.Type);
                    throwingObjectIntegrityLoss += material.Toughness * integrityLossFactor * weightFactor;
                }

                foreach (var material in this.Materials)
                {
                    double integrityLossFactor = GetMaterialIntegrityLossFactor(material.Type);
                    targetIntegrityLoss += material.Toughness * integrityLossFactor / weightFactor;
                }

                // Adjust integrity loss based on airborne power
                throwingObjectIntegrityLoss += o.AirbornePower * 0.4;
                targetIntegrityLoss += o.AirbornePower;

                // Apply general reduction
                throwingObjectIntegrityLoss *= 0.6;
                targetIntegrityLoss *= 1.0;

                // Apply projectile aerodynamic reduction
                if (o.ProjectileAerodynamic)
                {
                    throwingObjectIntegrityLoss *= 0.2;
                }

                // Ensure minimum integrity loss is 0
                throwingObjectIntegrityLoss = Math.Max(throwingObjectIntegrityLoss, 0);
                targetIntegrityLoss = Math.Max(targetIntegrityLoss, 0);

                // Round the integrity loss
                int roundedThrowingObjectIntegrityLoss = (int)Math.Round(throwingObjectIntegrityLoss);
                int roundedTargetIntegrityLoss = (int)Math.Round(targetIntegrityLoss);

                // Announce damage for throwing object
                AnnounceDamage(o, roundedThrowingObjectIntegrityLoss);

                // Announce damage for target (this)
                AnnounceDamage(this, roundedTargetIntegrityLoss);

                // Apply integrity loss
                o.Integrity -= roundedThrowingObjectIntegrityLoss;
                this.Integrity -= roundedTargetIntegrityLoss;

                // Ensure integrity doesn't go below 0
                o.Integrity = Math.Max(o.Integrity, 0);
                this.Integrity = Math.Max(this.Integrity, 0);
            }

            // Helper method to get integrity loss factor based on material type
            double GetMaterialIntegrityLossFactor(string materialType)
            {
                switch (materialType.ToLower())
                {
                    case "metal":
                        return 0.5; // half damage
                    case "cloth":
                        return 0.7; // resists
                    case "wood":
                        return 1.0; // normal
                    case "stone":
                        return 0.8; // slightly resists
                    case "glass":
                        return 10.0; // very susceptible
                    default:
                        return 1.0; // default multiplier
                }
            }

            void AnnounceDamage(Object obj, int damage)
            {
                string severity;
                if (damage < 10)
                {
                    severity = "minor";
                }
                else if (damage < 25)
                {
                    severity = "moderate";
                }
                else if (damage < 50)
                {
                    severity = "considerable";
                }
                else
                {
                    severity = "heavy";
                }

                string message = $"{obj.ReferredToNames[0]} takes {severity} damage!";
                Announcements.Add(new TextStorage(message, Color.Orange, new EntityList<Entity> { obj }));
            }


            return Announcements;
        }

        public void UpdateNames(bool IgnoreLUC, Architect Possessor, bool Loaded)
        {
            if (LatestUpdateCycle == Game1.GameWorld.Cycle && !IgnoreLUC)
                return;

            LatestUpdateCycle = Game1.GameWorld.Cycle;
            _referredToNames.Clear();

            if (!string.IsNullOrWhiteSpace(Name) && !Loaded)
            {
                AddReferredToName(Name);
            }

            string engravedPrefix = string.IsNullOrEmpty(Symbols) ? "" : string.Concat(Symbols, "-engraved ");
            string statueSuffix = string.IsNullOrEmpty(Symbols) ? "statue" : string.Concat("statue of ", Symbols);

            if (this is Door door && door.SourceRoom != null)
            {
                AddReferredToName(string.Concat(engravedPrefix, door.Direction, " ", Game1.FormatMaterialList(Materials), " ", Type, " (door ", door.Number, ")"));
                AddReferredToName(string.Concat("door ", door.Number.ToString()));

                if (door.IsQuickestExit)
                {
                    string quickestName = string.Concat(ReferredToNames[0], " [<]");
                    ReferredToNames.Insert(0, quickestName);
                }

                AddReferredToName(ID.ToString());
                return;
            }
            else if (this.IsBodyPart)
            {
                foreach (string s in Creator.ReferredToNames)
                {
                    AddReferredToName(string.Concat(s, "'s ", Type));
                }

                AddReferredToName(ID.ToString());
                return;
            }
            else if (LetterContent != null)
            {
                AddReferredToName(Name);
                AddReferredToName(ID.ToString());
                return;
            }

            if (!string.IsNullOrEmpty(Name))
            {
                if (Type == "statue")
                {
                    AddReferredToName(string.Concat(Name, ", ", Game1.FormatMaterialList(Materials), " ", statueSuffix));
                }
                else
                {
                    AddReferredToName(string.Concat(engravedPrefix, Name, ", ", Game1.FormatMaterialList(Materials), " ", Type));
                    AddReferredToName(string.Concat(engravedPrefix, Name));
                }
            }
            else
            {
                if (Type == "statue")
                {
                    AddReferredToName(string.Concat(Game1.FormatMaterialList(Materials), " ", statueSuffix));
                    AddReferredToName(statueSuffix);
                    AddReferredToName(string.Concat("the ", Game1.FormatMaterialList(Materials), " ", statueSuffix));
                }
                else
                {
                    AddReferredToName(string.Concat(engravedPrefix, Game1.FormatMaterialList(Materials), " ", Type));
                    AddReferredToName(Type);
                    AddReferredToName(string.Concat("the ", engravedPrefix, Game1.FormatMaterialList(Materials), " ", Type));
                }
            }

            if (Game1.MostRecentPartyTurnArchitect != null && Game1.GameWorld.GamePlayerAssociation != null &&
                Game1.GameWorld.GamePlayerAssociation.ActiveParty != null &&
                (Game1.MostRecentPartyTurnArchitect.OffHeldObject == this ||
                 Game1.MostRecentPartyTurnArchitect.MainHeldObject == this ||
                 Game1.MostRecentPartyTurnArchitect.Inventory.Contains(this)))
            {
                List<string> newItems = new List<string>();

                foreach (string s in ReferredToNames)
                {
                    newItems.Add(string.Concat("my ", engravedPrefix, s));
                }

                foreach (string newItem in newItems)
                {
                    AddReferredToName(newItem);
                }
            }

            if (this.Type == "exit door")
            {
                string quickestName = string.Concat(ReferredToNames[0], " [<]");
                ReferredToNames.Insert(0, quickestName);
            }
            else if (Type == "shadow fountain" && ReferredToNames.Contains("shadow fountain"))
            {
                ReferredToNames.Remove("shadow fountain");
            }

            if (AirborneTarget != null)
            {
                List<string> newNames = new List<string>();
                foreach (string s in ReferredToNames)
                {
                    newNames.Add(string.Concat("airborne ", s));
                }
                _referredToNames.Clear();
                foreach (string newName in newNames)
                {
                    AddReferredToName(newName);
                }
            }
            foreach (Object o in ContainedObjects)
            {
                o.UpdateNames(true, Possessor, Loaded);
            }

            if (Game1.SplitMode)
            {
                if (ReferredToNames.Count > 0)
                {
                    string firstName = ReferredToNames[0];
                    _referredToNames.Clear();
                    AddReferredToName(string.Concat(firstName, " (", ID.ToString(), ")"));
                    AddReferredToName(firstName);
                }
            }

            if (Possessor != null)
            {
                var referredToNamesCopy = new List<string>(ReferredToNames);

                foreach (string s in referredToNamesCopy)
                {
                    if (s.StartsWith("the "))
                        continue;

                    string prefix = Possessor.ReferredToNames[0];
                    if (prefix.EndsWith("s"))
                        AddReferredToName(string.Concat(prefix, "' ", s));
                    else
                        AddReferredToName(string.Concat(prefix, "'s ", s));
                }
            }

            _referredToNames = _referredToNames.Distinct().ToList();

            for (int i = ReferredToNames.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(ReferredToNames[i]))
                {
                    ReferredToNames.RemoveAt(i);
                }
            }

            AddReferredToName(ID.ToString());
        }





        public void UpdateSelfActionsAndSuch(Architect Possessor, bool PossessorNearArch)
        {
            //gravity

            if (YLevelInFeet > 0)
            {
                YVelocity += Math.Min(0, YLevelInFeet - 300);
                YLevelInFeet = Math.Min(0, YLevelInFeet - 300);

                if (YLevelInFeet == 0)
                {
                    Integrity = Integrity - YVelocity;

                    if (Game1.LoadedArchitects[Game1.ArchitectIndex].Structure == this.Structure || Game1.LoadedArchitects[Game1.ArchitectIndex].Block == Block)
                    {
                        if (Integrity > 0)
                        {
                            Game1.MakeObservation(ReferredToNames[0] + " vibrates intensely!", Color.Orange, new EntityList<Entity>() { this });
                        }
                        else
                        {
                            Game1.MakeObservation(ReferredToNames[0] + " vibrates intensely!", Color.Orange, new EntityList<Entity>() { this });
                        }
                    }
                }
            }

            //burn

            if (FireSeconds > 0 && ((ulong)Math.Round(Game1.GameWorld.Cycle)) % 10 == 0)
            {
                Integrity -= FireSeconds;
                FireSeconds--;
            }


            //plants

            if (PlantCycles > 0)
            {
                PlantCycles--;
            }

            //rope traps

            Architect triggeringArchitect = null;

            if(this.Type == "rope trap")
            {
                if (this.Room != null || this.Block != null)
                {
                    EntityList<Architect> ArchRelevant = this.Room != null ? Room.Architects : Block.Architects;

                    if (ArchRelevant.Any(a => !AwareArchitects.Contains(a)))
                    {
                        //activate

                        if (Room != null)
                        {
                            Room.ObjectsToRemove.Add(this);
                            Room.ObjectsToAdd.Add(new Object(null, "bunch", this.Materials, null));
                        }
                        else
                        {
                            Block.ObjectsToRemove.Add(this);
                            Block.ObjectsToAdd.Add(new Object(null, "bunch", this.Materials, null));
                        }

                        foreach (Architect a in ArchRelevant)
                        {
                            if (!this.AwareArchitects.Contains(a))
                            {
                                if (triggeringArchitect == null)
                                {
                                    triggeringArchitect = a;
                                    triggeringArchitect.AnnounceToParty(triggeringArchitect.ReferredToNames[0] + " triggers a rope trap!", Color.Red, new EntityList<Entity>() { triggeringArchitect });
                                }

                                triggeringArchitect.AnnounceToParty(a.ReferredToNames[0] + " is destabilized!", Color.Red, new EntityList<Entity>() { triggeringArchitect });
                                a.DestabilizedCycles += 200;

                                if(Game1.GameWorld.rnd.Next(2) == 1)
                                {
                                    a.Bound = true;
                                    triggeringArchitect.AnnounceToParty(a.ReferredToNames[0] + " was bound!", Color.Red, new EntityList<Entity>() { triggeringArchitect });
                                }
                                else
                                {
                                    triggeringArchitect.AnnounceToParty(a.ReferredToNames[0] + " barely managed to escape.", Color.Red, new EntityList<Entity>() { triggeringArchitect });
                                }
                            }
                        }
                    }
                }
            }

            if(PossessorNearArch)
            {
                UpdateNames(false, Possessor, true);
            }
        }

        public Object(string name, string type, EntityList<Material> materials, bool InOrOn, bool isContainer, Composition content, Entity creator, double weight, bool isGeneralGood, Block b, Structure s, Room r, bool IsWearable)
        {
            Weight = weight;
            Type = type;
            Materials = materials;
            IfTrueUseInIfFalseUseOn = InOrOn;
            IsContainer = isContainer;
            IsGeneralGood = isGeneralGood;
            Creator = creator;

            Distance = Game1.FurnitureItems.Contains(type) ? Game1.GameWorld.rnd.Next(400, 500) : Game1.GameWorld.rnd.Next(200, 400);

            if (type == "console")
            {
                ConsoleOn = Game1.GameWorld.rnd.Next(2) == 0;
            }

            if(name != null && Game1.GameWorld != null)
            {
                Game1.GameWorld.SubjectsToWriteAbout.Add(this);
            }



            Block = b;
            Room = r;

            Name = name;

            if(Name != null)
            {
                Game1.GameWorld.AllArtifacts.Add(this);
            }

            if (IsContainer)
            {
                ContainedObjects = new EntityList<Object>();
            }

            ApplyImbuements(0);
            UpdateNames(true, null, Game1.LoadedArchitects.Count > 0);
        }

        public Object(string name, string type, EntityList<Material> materials, Entity creator)
        {
            Name = name;
            Type = type;
            Materials = materials;
            Creator = creator;

            Distance = Game1.FurnitureItems.Contains(type) ? Game1.GameWorld.rnd.Next(300, 450) : Game1.GameWorld.rnd.Next(100, 300);

            if (Name != null)
            {
                Game1.GameWorld.AllArtifacts.Add(this);
                Game1.GameWorld.SubjectsToWriteAbout.Add(this);
            }


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
                    Weight = 1000; // e.g., in grams
                    IsGeneralGood = true;
                    Description = "An ingot used to efficiently store /m.";
                    break;
                case "star":
                    Weight = 10000; // e.g., in grams
                    Description = "A bright concentration of energy. It seems highly unstable.";
                    break;
                case "fragment":
                    Weight = 1;
                    IsGeneralGood = true;
                    Description = "A small shard of /m.";
                    IsConsumable = true;
                    break;
                case "prism":
                    Weight = 50;
                    Description = "A large shard of /m.";
                    break;
                case "log":
                    Weight = 3000;
                    IsGeneralGood = true;
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
                case "raft":
                    Weight = 10000;
                    Description = "A raft made of /m that allows travel over water. You might not carry this around.";
                    break;
                case "bunch":
                    Weight = 300;
                    Description = "A bunch of /m";
                    break;
                case "roll":
                    Weight = 300;
                    Description = "A roll of cloth, made of /m";
                    IsGeneralGood = true;
                    break;


                //writables, make sure you implement content afterward if using this!!!
                case "scroll":
                    IsWritable = true;
                    CompositionContent = null;
                    IsGeneralGood = true;
                    Weight = 100; // example weight in grams
                    Description = "A sheet attached to a roller.";
                    break;
                case "skill scroll":
                    Weight = 100; // example weight in grams
                    Description = "A sheet attached to a roller. It bears wisdom of a past age.";
                    break;
                case "book":
                    IsWritable = true;
                    CompositionContent = null;
                    Description = "A bound collection of sheets.";
                    Weight = 500; // example weight
                    break;
                case "sheet":
                    IsWritable = true;
                    CompositionContent = null;
                    Description = "A sheet of /m";
                    Weight = 50; // example weight
                    break;
                case "wax tablet":
                    IsWritable = true;
                    CompositionContent = null;
                    IsGeneralGood = true;
                    Description = "A tablet on which can be scratched to inscribe.";
                    Weight = 300; // example weight
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
                    IsContainer = true;
                    break;
                case "big urn":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A large decorative urn made of /m.";
                    IsContainer = true;
                    break;
                case "small pot":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A small cooking pot made of /m.";
                    IsContainer = true;
                    break;
                case "big pot":
                    Weight = 1000;
                    IsGeneralGood = true;
                    Description = "A large cooking pot made of /m.";
                    IsContainer = true;
                    break;
                case "small mug":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small mug made of /m.";
                    IsContainer = true;
                    break;
                case "big mug":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large mug made of /m.";
                    IsContainer = true;
                    break;
                case "small bowl":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small bowl made of /m.";
                    IsContainer = true;
                    break;
                case "big bowl":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large bowl made of /m.";
                    IsContainer = true;
                    break;
                case "small cup":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A small cup made of /m.";
                    IsContainer = true;
                    break;
                case "big cup":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A large cup made of /m.";
                    IsContainer = true;
                    break;
                case "candle":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A wax candle for lighting.";
                    break;
                case "brick":
                    Weight = 500;
                    IsGeneralGood = true;
                    Description = "A solid block made of /m.";
                    break;
                case "portion":
                    Weight = 300;
                    IsGeneralGood = true;
                    Description = "A portion of /m.";
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




                case "shortsword":
                    Weight = 1200;
                    IsGeneralGood = true;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A balanced slashing weapon that causes moderate pain, bleeding, and energy loss.";
                    break;
                case "knife":
                    Weight = 300;
                    IsWeapon = true;
                    IsLight = true;
                    DamageType = "slashing";
                    Description = "A versatile tool used for cutting, or as a weapon.";
                    break;
                case "dagger":
                    Weight = 150;
                    IsWeapon = true;
                    IsLight = true;
                    DamageType = "piercing";
                    Description = "An aerodynamic weapon often used for throwing or stabbing.";
                    ProjectileAerodynamic = true;
                    break;
                case "longsword":
                    Weight = 1800;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "slashing";
                    Description = "A heavy two-handed slashing weapon inflicting moderate pain, energy loss, and bleeding.";
                    IsTwoHanded = true;
                    break;
                case "battle axe":
                    Weight = 1200;
                    IsGeneralGood = true;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "A compact axe that delivers painful slashing strikes and blood loss.";
                    break;
                case "work axe":
                    Weight = 1800;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "slashing";
                    Description = "A heavy two-handed axe, typically used for chopping trees or logs.";
                    IsTwoHanded = true;
                    break;
                case "greataxe":
                    WeaponMaximumRange = 1;
                    IsGeneralGood = true;
                    Weight = 1800;
                    IsWeapon = true;
                    DamageType = "slashing";
                    Description = "An enormous two-handed axe, ideal for causing severe bleeding, pain, and functional loss.";
                    IsTwoHanded = true;
                    break;
                case "rapier":
                    Weight = 1200;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "piercing";
                    Description = "A precision piercing weapon that disables body functions and drains energy.";
                    break;
                case "spear":
                    WeaponMaximumRange = 1;
                    Weight = 1200;
                    IsGeneralGood = true;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A long piercing weapon ideal for disabling specific body parts and energy flow.";
                    break;
                case "pike":
                    WeaponMaximumRange = 2;
                    Weight = 1800;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "piercing";
                    Description = "A long-range two-handed piercing weapon that disables body parts and disrupts energy flow.";
                    break;
                case "pickaxe":
                    Weight = 1200;
                    DamageType = "piercing";
                    IsGeneralGood = true;
                    Description = "A useful mining tool. Can be used to gather various tough materials.";
                    break;
                case "shovel":
                    Weight = 1200;
                    DamageType = "piercing";
                    IsGeneralGood = true;
                    Description = "A tool used for digging. Ideal for gathering sand.";
                    break;
                case "scythe":
                    Weight = 1200;
                    IsGeneralGood = true;
                    DamageType = "piercing";
                    Description = "A tool with a long curved blade used to gather plants.";
                    break;
                case "mace":
                    Weight = 1200;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "bashing";
                    Description = "A solid bashing weapon causing functional damage, pain, and energy loss.";
                    break;
                case "war hammer":
                    Weight = 1200;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "bashing";
                    Description = "A bashing weapon that causes major functional damage and energy loss.";
                    break;
                case "staff":
                    Weight = 800;
                    IsLight = true;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A long polished rod suitable for both physical combat and magical use.";
                    break;
                case "shield":
                    Weight = 1200;
                    IsGeneralGood = true;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A defensive tool that can be used to block attacks or bash.";
                    break;
                case "whip":
                    Weight = 1200;
                    IsWeapon = true;
                    IsGeneralGood = true;
                    DamageType = "thrashing";
                    Description = "A flexible weapon that causes high bleeding and pain.";
                    break;
                case "scourge":
                    Weight = 1200;
                    IsGeneralGood = true;
                    IsWeapon = true;
                    DamageType = "thrashing";
                    Description = "A multi-tailed whip, causing extensive bleeding and pain.";
                    break;
                case "flail":
                    Weight = 1200;
                    IsWeapon = true;
                    DamageType = "thrashing";
                    Description = "A medium, spiked ball attached to a chain and rod, causing major bleeding and pain.";
                    break;
                case "chain":
                    Weight = 1200;
                    WeaponMaximumRange = 1;
                    IsWeapon = true;
                    DamageType = "thrashing";
                    Description = "A lengthy chain, capable causing extensive bleeding and pain.";
                    break;




                case "rope trap":
                    Weight = 1000000;
                    Description = "A trap made of rope.";
                    break;
                case "bolt":
                    Weight = 10;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A small projectile fired from an enchanted object.";
                    break;

                case "shock mine":
                    Weight = 100;
                    Description = "An explosive device that protects certain items.";
                    break;

                case "wave":
                    Weight = 10;
                    IsWeapon = true;
                    DamageType = "bashing";
                    Description = "A flying wave of /m.";
                    break;
                case "spark":
                    Weight = 10;
                    IsWeapon = true;
                    DamageType = "piercing";
                    Description = "A mysterious concentration of photonic energy.";
                    break;
                // other amror


                case "small hat":
                    Weight = 100;
                    IsWearable = true;
                    Description = "A small head covering made of /m.";
                    CoverageValues.Add(("head", 4)); // Smaller coverage than a large hat
                    CoverageValues.Add(("left eye", 2));
                    CoverageValues.Add(("right eye", 2));
                    break;

                case "large hat":
                    Weight = 200;
                    IsWearable = true;
                    Description = "A larger head covering made of /m.";
                    CoverageValues.Add(("head", 6)); // Smaller coverage than a large hat
                    CoverageValues.Add(("left eye", 2));
                    CoverageValues.Add(("right eye", 2));

                    break;
                case "hood":
                    Weight = 150;
                    IsGeneralGood = true;
                    IsWearable = true;
                    Description = "A head and neck covering made of /m.";
                    CoverageValues.Add(("head", 5));
                    CoverageValues.Add(("neck", 6));
                    CoverageValues.Add(("left eye", 8));
                    CoverageValues.Add(("right eye", 8));
                    break;
                case "cape":
                    Weight = 150;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "A long, flowing back garment made of /m.";
                    CoverageValues.Add(("left shoulder", 3));
                    CoverageValues.Add(("right shoulder", 3));
                    CoverageValues.Add(("neck", 5));
                    CoverageValues.Add(("torso", 2)); // Minimal torso coverage as it's open at the front
                    break;
                case "robe":
                    Weight = 150;
                    IsGeneralGood = true;
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
                    IsGeneralGood = true;
                    Description = "A small, wearable ornament made of /m.";
                    // Amulets do not provide coverage in the context of physical protection
                    break;
                case "flair":
                    Weight = 50;
                    IsGeneralGood = true;
                    IsWearable = true;
                    Description = "A decorative, wearable item made of /m.";
                    CoverageValues.Add(("neck", 4));
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
                    CoverageValues.Add(("right hand", 8));
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
                    IsGeneralGood = true;
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
                case "brassiere":
                    Weight = 0;
                    IsWearable = true;
                    Description = "An undergarment for the upper body made of /m.";
                    break;
                case "undergarment":
                    Weight = 0;
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
                    IsGeneralGood = true;
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
                    IsGeneralGood = true;
                    Description = "A sturdy, tall foot covering made of /m.";
                    CoverageValues.Add(("left foot", 9));
                    break;
                case "right boot":
                    Weight = 250;
                    IsGeneralGood = true;
                    IsWearable = true;
                    Description = "A sturdy, tall foot covering made of /m.";
                    CoverageValues.Add(("right foot", 9));
                    break;
                case "left shoe":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A standard foot covering made of /m.";
                    CoverageValues.Add(("left foot", 7));
                    break;
                case "right shoe":
                    Weight = 250;
                    IsWearable = true;
                    Description = "A standard foot covering made of /m.";
                    CoverageValues.Add(("right foot", 7));
                    break;
                // Inside the switch statement of your object constructor
                case "helmet":
                    Weight = 1500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "A protective headgear made of /m.";
                    CoverageValues.Add(("head", 7)); // High protection for the head
                    CoverageValues.Add(("left eye", 7));
                    CoverageValues.Add(("right eye", 7));
                    break;

                case "chestplate":
                    Weight = 3000;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "A sturdy torso armor made of /m.";
                    CoverageValues.Add(("torso", 8)); // Maximum protection for the torso
                    CoverageValues.Add(("left shoulder", 8)); // Maximum protection for the torso
                    CoverageValues.Add(("right shoulder", 8)); // Maximum protection for the torso
                    CoverageValues.Add(("neck", 6)); // Maximum protection for the torso
                    break;

                case "left gauntlet":
                    Weight = 500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armored gloves for hand and wrist protection made of /m.";
                    CoverageValues.Add(("left hand", 7)); // High protection for the left hand
                    CoverageValues.Add(("left arm", 7)); // Additional protection for the lower arm
                    break;

                case "right gauntlet":
                    Weight = 500;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armored gloves for hand and wrist protection made of /m.";
                    CoverageValues.Add(("right hand", 7)); // High protection for the right hand
                    CoverageValues.Add(("right arm", 7)); // Additional protection for the lower arm
                    break;

                case "leggings":
                    Weight = 2000;
                    IsWearable = true;
                    IsGeneralGood = true;
                    Description = "Armor for leg protection made of /m.";
                    CoverageValues.Add(("left leg", 8)); // Maximum protection for the left leg
                    CoverageValues.Add(("right leg", 8)); // Maximum protection for the right leg
                    break;

                case "drink":
                    Weight = 300; // Approximate weight for a mug-sized liquid in grams
                    IsContainer = false; // Assuming the object is just the liquid, not the container
                    Description = "An average portion of /m.";
                    IsConsumable = true;
                    break;

                case "cube":
                    Weight = 10; // Approximate weight for a small cube, like an ice cube, in grams
                    IsContainer = false; // This is a solid object, not a container
                    Description = "A small cube of /m.";
                    IsGeneralGood = true;
                    IsConsumable = true;
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
                    Description = "A strange /m capsule with an unknown function. It moves through space weightlessly, ten times slower.";
                    break;

                // Case for a "lightning grenade"
                case "lightning grenade":
                    Weight = 250;
                    IsWeapon = false;
                    Description = "A strange /m capsule with an unknown function. It moves through space weightlessly, ten times slower.";
                    break;



                // healing
                case "salve":
                    Weight = 75;
                    IsGeneralGood = true;
                    IsWeapon = false;
                    IsConsumable = true;
                    Description = "A salve of /m that can be used to reduce pain.";
                    break;
                case "bandage":
                    Weight = 75;
                    IsGeneralGood = true;
                    IsWeapon = false;
                    IsConsumable = true;
                    Description = "A bandage made of /m that can be used to stop bleeding.";
                    break;
                case "vial":
                    Weight = 75;
                    IsGeneralGood = true;
                    IsWeapon = false;
                    IsContainer = true;
                    IsConsumable = true;
                    Description = "A vial of vitality, a fast acting energy source.";
                    break;



                case "urn":
                    Weight = 1500; // Example weight for an urn
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "An urn, often used for storing ashes or as a decorative piece.";
                    break;

                case "small chalice":
                    Weight = 200; // Example weight for a chalice
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A small chalice, often used for ceremonial purposes.";
                    break;

                case "big chalice":
                    Weight = 500; // Example weight for a chalice
                    IsContainer = true;
                    IsGeneralGood = true;
                    Description = "A big chalice, typically used in rituals or as a decorative item.";
                    break;



                //furniture

                // Inside the switch statement of your object constructor


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

                case "exit door":
                    Weight = 30000;
                    IsContainer = false;
                    Description = "A large, swinging barrier. You entered here to get in the structure.";
                    break;

                case "bed":
                    Weight = 20000;
                    IsContainer = true;
                    Description = "A piece of furniture for sleeping or resting.";
                    break;

                case "keg":
                    Weight = 10000;
                    IsContainer = true;
                    Description = "A barrel-like container for storing liquids.";
                    break;

                case "chest":
                    Weight = 8000;
                    IsContainer = true;
                    Description = "A storage box for valuables and other items.";
                    break;

                case "shadow fountain":
                    Weight = 10000000;
                    IsContainer = false;
                    Description = "A fountain that flows from a realm of pure shadow, commonly found at the center of a civilization. Items sent inside can be recalled from any fountain at will.";
                    break;

                case "chromaweaver":
                    Weight = 10000000;
                    IsContainer = false;
                    Description = "A mysterious device that bends light around it.";
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

                case "statue":
                    Weight = 5000;
                    IsContainer = true;
                    Description = "A base structure for displaying significant objects or statues.";
                    break;

                case "pylon":
                    Weight = 50000;
                    IsContainer = true;
                    Description = "An ancient device that seems to be linked to somoene.";
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

                case "tree":
                    Weight = 9000000;
                    IsContainer = true;
                    Description = "A huge membranous organism that grows out of the ground.";
                    break;
                case "bush":
                    Weight = 1000;
                    IsContainer = true;
                    Description = "A flowering, leafy, membranous organism that grows out of the ground.";
                    break;

               

                default:
                    int shibe = 1;
                    throw new Exception("Trying to create an unimplemented object!");

            }

            List<string> Mater = new List<string>();
            foreach (Material m in Materials)
            {
               Mater.Add(m.Name);
            }
            string MaterialList = Game1.FormatAndList(Mater);

            Description = Description.Replace("/m", MaterialList);

            //rarity

            if (IsWearable || IsWeapon)
            {
                int CoolioNumber = 500;
                if (Creator is Architect)
                {
                    CoolioNumber -= ((Architect)Creator).Creativity * 25;
                }



                int rarityInt = Game1.GameWorld.rnd.Next(1, CoolioNumber);

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

            if(Name != null)
            {
                IsGeneralGood = false;
            }

            ApplyImbuements(0);
            UpdateNames(false, null, Game1.LoadedArchitects.Count > 0);
        }

        public void ApplyImbuements(int Extra)
        {
            int Imbuements = Extra;

            if (Type == "dagger")
            {
                return;
            }

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
                "stagnant",        // when you have spent 7 seconds in a block/room
                "maxenergy"        // when energy is maxed
            };
                    List<string> PassiveEffects = new List<string>()
            {
                "+attack",         // +% attack power
                "+shield",         // +% shield effectiveness
                "+dodge",          // +% dodge chance
                "+weaponreaction",    // +% redirection chance
                "+bashpierce",     // +% bashing resistance
                "+slashthrash",    // +% slashing resistance
                "+stealth",        // become harder to see and target
                "+regen"           // +X energy regen/cycle
            };
                    List<string> TriggerConditions = new List<string>()
            {
                "onroll",         // when you roll
                "onduck",         // when you duck
                "onjump",         // when you jump
                "onblock",        // when you block
                "onparry",        // when you parry
                "onredirect",     // when you redirect
                "oncast",         // when you cast a spell
                "ondamage",       // when you take damage
                "onattack"        // when you successfully attack
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
                bool isTrigger = Game1.GameWorld.rnd.Next(2) == 0;
                string conditionOrTrigger = "";
                string buffOrEffect = "";
                int firstPower = 0;
                int secondPower = 0;

                if (isTrigger)
                {
                    int triggerIndex = Game1.GameWorld.rnd.Next(TriggerConditions.Count());
                    conditionOrTrigger = TriggerConditions[triggerIndex];
                    int effectIndex = Game1.GameWorld.rnd.Next(TriggerEffects.Count());
                    buffOrEffect = TriggerEffects[effectIndex];
                    // No power values assigned for triggers in this example
                }
                else
                {
                    int passiveIndex;
                    int buffIndex;

                    // Ensure "maxenergy" is never paired with "+regen"
                    do
                    {
                        passiveIndex = Game1.GameWorld.rnd.Next(PassiveConditions.Count());
                        buffIndex = Game1.GameWorld.rnd.Next(PassiveEffects.Count());
                        conditionOrTrigger = PassiveConditions[passiveIndex];
                        buffOrEffect = PassiveEffects[buffIndex];
                    } while (conditionOrTrigger == "maxenergy" && buffOrEffect == "+regen");

                    // === Assign power levels based on effect ===
                    if (buffOrEffect == "+regen")
                    {
                        secondPower = Game1.GameWorld.rnd.Next(1, 4);
                    }
                    else if (
                        buffOrEffect == "+bashpierce" ||
                        buffOrEffect == "+slashthrash"
                    )
                    {
                        secondPower = Game1.GameWorld.rnd.Next(10, 16); // Resistances: 8 to 12
                    }
                    else if (
                        buffOrEffect == "+shield" ||
                        buffOrEffect == "+dodge" ||
                        buffOrEffect == "+weaponreaction"
                    )
                    {
                        secondPower = Game1.GameWorld.rnd.Next(10, 16); // Reaction chances: 5 to 11
                    }
                    else if (
                        buffOrEffect == "+attack"
                    )
                    {
                        secondPower = Game1.GameWorld.rnd.Next(10, 16); // General power boosts
                    }

                    // === Optional: Assign firstPower for specific conditions ===
                    if (conditionOrTrigger == "diminished")
                    {
                        firstPower = Game1.GameWorld.rnd.Next(50, 71); // Threshold for low energy
                    }
                }

                this.Imbuements.Add(new Imbuement(isTrigger, conditionOrTrigger, buffOrEffect, firstPower, secondPower, this));
            }
        }

        public void Fractallize(int Cycles, Architect Executor)
        {
            FractalCycles = Cycles;
            RematerializeLocation = (Executor.Block.District.Location, Executor.Block.District, Executor.Block, Executor.Structure, Executor.Room);
            Game1.GameWorld.FractalObjects.Add(this);

            if (Executor.Room != null)
            {
                Executor.Room.Objects.Remove(this);
            }
            else if (Executor.Block != null)
            {
                Executor.Block.Objects.Remove(this);
            }
        }

        public Object()
        {

        }
    }
}