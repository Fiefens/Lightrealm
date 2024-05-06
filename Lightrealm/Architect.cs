using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lightrealm
{
    [Serializable]

    public class Architect : Entity
    {
        public string Sex { get; set; }
        public string Pronoun { get; set; }
        public string PossessivePronoun { get; set; }
        public string ObjectivePronoun { get; set; }

        public bool RuptureMode = false;

        public (Location, District, Block, Structure, Room) SavePoint = (null,null,null,null,null);
        public int SavePointTicks = 0;

        public List<string> AlignedDomains = new List<string>();

        public List<Composition> CultureBank = new List<Composition>();

        public Location InteractionLocation = null;

        public string Prompt = "";
        public string LastPrompt = "";

        // Function to get distance to another architect
        public int GetDistance(object entity)
        {
            // Check if the entity is an Architect
            if (entity is Architect otherArchitect)
            {
                // Find the distance record for the architect
                var distanceRecord = Distances.FirstOrDefault(d => d.Item1 == otherArchitect);

                // Return the distance if found, otherwise return -1
                return distanceRecord.Item1 != null ? distanceRecord.Item2 : -1;
            }

            // If the entity is not an Architect, return 0 to automatically succeed
            return 0;
        }

        public int BleedingCycle = 0;

        public List<(Architect, int)> Distances = new List<(Architect, int)>();

        public int DivineProtection = 0;
        public int DivineMight = 0;

        public List<(Entity, string)> Grievances = new List<(Entity, string)>();

        public int CooldownCycles = 0;

        public bool IsCalamity = false;

        public void DistanceFromArchitect(Architect otherArchitect, int distanceModifier)
        {
            // Check and update existing distance for this architect
            var existingDistance = Distances.FindIndex(d => d.Item1 == otherArchitect);
            if (existingDistance != -1)
            {
                // Modify the existing distance by the distanceModifier and ensure it's within the 0-5 range
                int newDistance = Distances[existingDistance].Item2 + distanceModifier;
                Distances[existingDistance] = (otherArchitect, Math.Clamp(newDistance, 0, 5));
            }
            else
            {
                // Initialize new distance if not exist, capped at 5
                Distances.Add((otherArchitect, Math.Clamp(distanceModifier, 0, 5)));
            }

            // Ensure the other architect also updates its distance list symmetrically
            var otherArchitectDistance = otherArchitect.Distances.FindIndex(d => d.Item1 == this);
            if (otherArchitectDistance != -1)
            {
                int newDistance = otherArchitect.Distances[otherArchitectDistance].Item2 + distanceModifier;
                otherArchitect.Distances[otherArchitectDistance] = (this, Math.Clamp(newDistance, 0, 5));
            }
            else
            {
                otherArchitect.Distances.Add((this, Math.Clamp(distanceModifier, 0, 5)));
            }
        }


        public double Speed()
        {
            int numberOfLegs = BodyParts.Count(part => part.Type.Contains("leg"));
            double baseSpeed = 1.0; // Normalized average speed
            double legSpeedModifier = CalculateLegSpeedModifier(numberOfLegs);
            double agilityModifier = CalculateAgilityModifier(Agility); // Adjusted to keep speed not too high
            double totalWeight = Inventory.Concat(Clothing).Sum(item => item.Weight);
            double weightPenaltyModifier = CalculateWeightPenaltyModifier(totalWeight, Endurance);

            double rawSpeed = baseSpeed * legSpeedModifier * agilityModifier * weightPenaltyModifier;
            return Math.Round(rawSpeed, 2); // Rounding to the nearest two decimal places

            double CalculateLegSpeedModifier(int numberOfLegs)
            {
                // Adjusting for more nuanced effects of additional legs
                if (numberOfLegs == 0) return 0.05; // halved
                else if (numberOfLegs == 1) return 0.25; // halved
                else if (numberOfLegs == 2) return 0.5; // halved
                else // Diminishing returns for each leg above 2
                {
                    double extraLegsModifier = 0.05 * (numberOfLegs - 2); // Halved boost per additional leg
                    return 0.5 + Math.Min(extraLegsModifier, 0.25); // Halved cap
                }
            }

            double CalculateAgilityModifier(int agility)
            {
                // Adjusting the agility effect to prevent speeds too high
                double baselineAgility = 4;
                double agilityEffect = (agility - baselineAgility) * 0.025; // Halved effect per point
                return 1 + agilityEffect;
            }

            double CalculateWeightPenaltyModifier(double totalWeight, int endurance)
            {
                double excessWeight = Math.Max(0, totalWeight - 1000); // Considering weight above 1000g
                double penalty = excessWeight / 10000; // Halved penalty for weight
                double enduranceModifier = 1 - (0.025 * (endurance - 1)); // Halved impact of endurance
                return Math.Max(0.75, 1 - (penalty * enduranceModifier)); // Ensuring a minimum 75% speed
            }
        }


        public int NaturalArmor = 0; //number from 1-100, dependent on race, chance of blocking an attack simply with natural ability

        public int Level;
        public int SpendableLevels;

        public List<Object> Sparks = new List<Object>();

        public Architect UndeadCreator = null;

        public bool Invisible = false;

        public int EscapeChance()
        {
            return Math.Min(Agility * 4 + PathOfShadowLevel * 3 + (CyclesSinceMoved/50) * 2, 100);
        }

        public int CombatCycles = 0;

        public int PathOfShadowLevel = 0;
        public int PathOfLifeLevel = 0;
        public int PathOfDeathLevel = 0;
        public int PathOfTimeLevel = 0;
        public int PathOfStarsLevel = 0;
        public int PathOfHeatLevel = 0;
        public int PathOfIllusionsLevel = 0;
        public int PathOfEtherealityLevel = 0;
        public int PathOfVoidLevel = 0;
        public int PathOfStormsLevel = 0;
        public int PathOfForgeLevel = 0;
        public int PathOfLoreLevel = 0;
        public int PathOfMindLevel = 0;
        public int PathOfSoulLevel = 0;
        public int PathOfBodyLevel = 0;
        public int PathOfSpaceLevel = 0;
        public int PathOfRealityLevel = 0;
        public int PathOfLightLevel = 0;

        public Architect Master = null;
        public string MasterRelation = "";

        public Architect Spouse = null;

        public bool Augumented = false;

        public List<Object> ShadowStorage = new List<Object>();

        public int Wealth = 0;

        public double AdversaryAge = 0;
        public int AdversarySpawnTime = Game1.r.Next(25, 36);

        public bool TriggeredLock = false;
        public int CyclesSinceMoved = 0;

        public string GetNonZeroPathLevels()
        {
            var paths = new List<string>();

            if (PathOfShadowLevel > 0) paths.Add($"Shadow LVL {PathOfShadowLevel}");
            if (PathOfLifeLevel > 0) paths.Add($"Life LVL {PathOfLifeLevel}");
            if (PathOfDeathLevel > 0) paths.Add($"Death LVL {PathOfDeathLevel}");
            if (PathOfTimeLevel > 0) paths.Add($"Time LVL {PathOfTimeLevel}");
            if (PathOfStarsLevel > 0) paths.Add($"Stars LVL {PathOfStarsLevel}");
            if (PathOfHeatLevel > 0) paths.Add($"Heat LVL {PathOfHeatLevel}");
            if (PathOfIllusionsLevel > 0) paths.Add($"Illusions LVL {PathOfIllusionsLevel}");
            if (PathOfEtherealityLevel > 0) paths.Add($"Ethereality LVL {PathOfEtherealityLevel}");
            if (PathOfVoidLevel > 0) paths.Add($"Void LVL {PathOfVoidLevel}");
            if (PathOfStormsLevel > 0) paths.Add($"Storms LVL {PathOfStormsLevel}");
            if (PathOfForgeLevel > 0) paths.Add($"Forge LVL {PathOfForgeLevel}");
            if (PathOfLoreLevel > 0) paths.Add($"Lore LVL {PathOfLoreLevel}");
            if (PathOfMindLevel > 0) paths.Add($"Mind LVL {PathOfMindLevel}");
            if (PathOfSoulLevel > 0) paths.Add($"Soul LVL {PathOfSoulLevel}");
            if (PathOfBodyLevel > 0) paths.Add($"Body LVL {PathOfBodyLevel}");
            if (PathOfSpaceLevel > 0) paths.Add($"Space LVL {PathOfSpaceLevel}");
            if (PathOfRealityLevel > 0) paths.Add($"Reality LVL {PathOfRealityLevel}");
            if (PathOfLightLevel > 0) paths.Add($"Light LVL {PathOfLightLevel}");

            return paths.Count > 0 ? $"PATHS: {string.Join(", ", paths)}" : "No paths have levels above 0.";
        }


        public Block BlockLastCycle = null;

        public int BarrierStacks = 0;

        public string GetDescription(int value)
        {
            switch (value)
            {
                case 0: return "non-existent";
                case 1: return "terrible";
                case 2: return "poor";
                case 3: return "average";
                case 4: return "good";
                case 5: return "excellent";
                case 6: return "superior";
                case 7: return "exceptional";
                case 8: return "unbelievable";
                case 9: return "unfathomable";
                case 10: return "mythical";
                default: return "archancient"; // In case of greater than 10 value
            }
        }

        public (Region, Location, District, Block, Structure, Room) RematerializeLocation = (null, null, null, null, null, null);

        public Race Race { get; set; }
        public string Profession { get; set; }
        public int Age { get; set; }

        public List<Architect> MeldedShibas = new List<Architect>();

        public bool IsAlive { get; set; } = true;

        public int MoralCompass = 0;
        public int StabilityCompass = 0;
        public int PropertyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int FamilyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int PowerValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int MoneyValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int KnowledgeValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int SpiritualityValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int ProwessValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int PatriotismValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int CourageValue = Math.Max(0, Game1.r.Next(-4, 6));
        public int CreativityValue = Math.Max(0, Game1.r.Next(-4, 6));

        public Location NextMigrationLocation { get; set; }

        public List<Imbuement> CurrentlyActiveImbuements = new List<Imbuement>();

        public bool RecievedImmortalityBuff { get; set; }

        public int DistrictPoints = 0;

        public Structure StudyBuilding { get; set; }

        public bool HasMadeALegendaryArtifact = false;

        public string CheckEnergyLevel()
        {
            double ratio = Energy / MaxEnergy();
            if (ratio == 0)
            {
                return(Pronoun + " is dead.");
            }
            if (ratio < 0.2)
            {
                return (Pronoun + " appears on the verge of collapse.");
            }
            else if (ratio < 0.4)
            {
                return (Pronoun + " is not looking very healthy.");
            }
            else if (ratio < 0.6)
            {
                return (Pronoun + " is looking somewhat dim.");
            }
            else if (ratio < 0.8)
            {
                return (Pronoun + " is looking just fine.");
            }
            else
            {
                return (Pronoun + " looks perfectly healthy.");
            }
        }
        public string DescribeArchitectInventory()
        {
            var itemCounts = new Dictionary<string, int>();

            // Function to add an item to the dictionary
            void AddItem(Object item)
            {
                if (item != null && item.ReferredToNames.Any())
                {
                    string itemName = item.ReferredToNames[0];
                    if (!itemCounts.ContainsKey(itemName))
                        itemCounts[itemName] = 0;
                    itemCounts[itemName]++;
                }
            }

            // Add right and left hand objects
            AddItem(RightHandObject);
            AddItem(LeftHandObject);

            // Add inventory items
            foreach (Object o in Inventory)
            {
                AddItem(o);
            }

            // Add clothing items
            foreach (Object c in Clothing)
            {
                AddItem(c);
            }

            // Generate description
            StringBuilder description = new StringBuilder();

            // Describe hand objects
            string DescribeHandObject(Object handObject, string hand)
            {
                if (handObject != null && handObject.ReferredToNames.Any())
                {
                    string article = "aeiouAEIOU".Contains(handObject.ReferredToNames[0][0]) ? "an " : "a ";
                    return $"{Pronoun} has {article}{handObject.ReferredToNames[0]} in {PossessivePronoun} {hand} hand";
                }
                else
                {
                    return $"{Pronoun} {hand} hand is empty";
                }
            }

            description.Append(DescribeHandObject(LeftHandObject, "left"));
            description.Append(", and ");
            description.Append(DescribeHandObject(RightHandObject, "right"));
            description.AppendLine(".");

            // Describe inventory
            if (itemCounts.Count > 0)
            {
                description.Append($"{Pronoun} is carrying ");
                var inventoryDescriptions = itemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"a {itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (inventoryDescriptions.Count > 1)
                {
                    var lastItem = inventoryDescriptions.Last();
                    inventoryDescriptions.RemoveAt(inventoryDescriptions.Count - 1);
                    description.Append(string.Join(", ", inventoryDescriptions));
                    description.Append(", and " + lastItem);
                }
                else
                {
                    description.Append(inventoryDescriptions[0]);
                }

                // Adding a sentence for clothing items
                var clothingDescriptions = itemCounts.Select(kvp =>
                {
                    string itemName = kvp.Key;
                    int count = kvp.Value;
                    string pluralForm = itemName.EndsWith("s") ? itemName + "es" : itemName + "s";
                    return count == 1 ? $"a {itemName}" : $"{count} {pluralForm}";
                }).ToList();

                if (clothingDescriptions.Any())
                {
                    description.Append(". ");
                    description.Append($"{Pronoun} is wearing ");

                    if (clothingDescriptions.Count > 1)
                    {
                        var lastClothingItem = clothingDescriptions.Last();
                        clothingDescriptions.RemoveAt(clothingDescriptions.Count - 1);
                        description.Append(string.Join(", ", clothingDescriptions));
                        description.Append(", and " + lastClothingItem);
                    }
                    else
                    {
                        description.Append(clothingDescriptions[0]);
                    }
                }
            }

            return description.ToString();
        }

        public List<Object> Inventory { get; set; }
        public Object LeftHandObject { get; set; }
        public Object RightHandObject { get; set; }

        public bool RightHanded { get; set; } = true;
        public bool HadChildren = false;

        public string FavoriteColor { get; set; }
        public Material FavoriteGemstone { get; set; }
        public Material FavoriteStone { get; set; }
        public Material FavoriteWood { get; set; }
        public Material FavoriteMetal { get; set; }
        public Material FavoriteCloth { get; set; }
        public Object FavoriteBook { get; set; }


        public Group Group { get; set; } = null;
        public int GroupLoyalty { get; set; } = -1;
        public int TerminalAge { get; set; } = 0;
        public bool DoIDieOfOldAge { get; set; } = true;

        public bool IsLoadedTrader = false;

        public int Reputation = 0;

        public int PurifiedBurnedCities = 0;
        public Blight BlightManipulated = null;
        public int KilledPeopleWithBlight = 0;
        public List<Location> TakenLocations = new List<Location>();
        public int KilledWomen = 0;
        public int KilledMen = 0;
        public int KilledChildren = 0;
        public int KidnappedWomen = 0;
        public int KidnappedMen = 0;
        public int KidnappedChildren = 0;

        public List<Architect> KilledPeopleWhoActuallyMatter = new List<Architect>();
        public List<Architect> KidnappedPeopleWhoActuallyMatter = new List<Architect>();

        public int CorruptedCities = 0;
        public int Decieved = 0;
        public string PowerType = "";

        public int Strength;
        public int Dexterity;
        public int Agility;
        public int Endurance;
        public int Creativity;
        public int Charisma;
        public int Focus;

        public Object FindBodyPart(string objectType)
        {
            foreach (var bodyPart in BodyParts)
            {
                if (bodyPart.Type == objectType)
                {
                    return bodyPart;
                }
            }

            return null;
        }

        private int ApplyRandomMultiplier(int baseChance, int reactionModifierInt, int actionIndex)
        {
            // Convert the reactionModifierInt to a unique percentage for each action within the range of -10% to +10%
            // The actionIndex parameter ensures each action gets a different part of the reactionModifierInt
            double multiplier = (((reactionModifierInt / (actionIndex + 1)) % 21) - 10) / 100.0; // This will give a value between -0.10 and 0.10

            // Apply this multiplier to the base chance
            int adjustedChance = (int)(baseChance + baseChance * multiplier);
            return Math.Min(Math.Max(adjustedChance, 0), 100); // Ensuring the chance is between 0 and 100
        }

        public (int sustain, int parry, int block, int duck, int jump, int roll, int disarm, int redirect) CalculateSuccessChances(Attack attack, int reactionModifierInt, Architect Attacker, int attackersProficiency)
        {
            // Assuming you have methods to get these values from imbuements
            int extraDodgeChance = ExtraDodgeChance;
            int extraParryingProficiency = ExtraRedirectionChance;
            int extraShieldingProficiency = ExtraShieldEffectiveness;

            int GetResistance(string DamageType)
            {
                if (DamageType == "scourging")
                {
                    return ExtraScourgingResistance;
                }
                else if (DamageType == "piercing")
                {
                    return ExtraPiercingResistance;
                }
                else if (DamageType == "slashing")
                {
                    return ExtraSlashingResistance;
                }
                else
                {
                    return ExtraBashingResistance;
                }
            }

            // Resistance calculation

            float resistanceMultiplier;

            if (attack.Weapon != null)
            {
                resistanceMultiplier = 1 + (GetResistance(attack.Weapon.DamageType) / 100f);
            }
            else
            {
                resistanceMultiplier = 1 + (GetResistance("bashing") / 100f);
            }

            int CalculateChance(int proficiency, int baseChance, int multiplier)
            {
                int chance = baseChance + (proficiency * multiplier);
                chance = (int)(chance * resistanceMultiplier); // Apply resistance
                return Math.Max(0, Math.Min(chance, 100)); // Clamp the chance between 0 and 100
            }

            int sustainChance = 0; // Sustain means taking the hit, so 0% chance to evade

            // Target-based modifications
            bool targetAffectsJump = attack.Target.Contains("head") || attack.Target.Contains("neck") || attack.Target.Contains("eye") || attack.Target.Contains("tooth") || attack.Target.Contains("core") || attack.Target.Contains("tooth");
            bool targetAffectsDuck = attack.Target.Contains("shard") || attack.Target.Contains("leg") || attack.Target.Contains("foot");

            int ModifyChanceBasedOnTarget(int chance, bool increase)
            {
                return (int)(chance * (increase ? 1.2 : 0.8));
            }

            // Calculate chances with uniqueness
            int parryChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("parrying") + extraParryingProficiency, 50, 4), reactionModifierInt, 1); 
            int blockChance = (this.LeftHandObject != null && this.LeftHandObject.Type == "shield" || this.RightHandObject != null && this.RightHandObject.Type == "shield") ? ApplyRandomMultiplier(CalculateChance(GetProficiency("blocking") + extraShieldingProficiency, 50, 4), reactionModifierInt, 2) : 0;

            int baseDuckChance = CalculateChance(GetProficiency("dodging") + extraDodgeChance, 50, 4);
            int baseJumpChance = CalculateChance(GetProficiency("dodging") + extraDodgeChance, 50, 4);

            // Adjust jump and duck chances based on the attack target
            int duckChance = targetAffectsJump ? ModifyChanceBasedOnTarget(baseDuckChance, true) : (targetAffectsDuck ? ModifyChanceBasedOnTarget(baseDuckChance, false) : baseDuckChance);
            int jumpChance = targetAffectsJump ? ModifyChanceBasedOnTarget(baseJumpChance, false) : (targetAffectsDuck ? ModifyChanceBasedOnTarget(baseJumpChance, true) : baseJumpChance);

            int rollChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("dodging") + extraDodgeChance, 35, 4), reactionModifierInt, 5);
            int disarmChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("disarming"), 0, 6), reactionModifierInt, 6);
            int redirectChance = ApplyRandomMultiplier(CalculateChance(GetProficiency("redirection"), 50, 4), reactionModifierInt, 7);

            // Existing multiplier logic
            float multiplier = 1.0f;
            if (DestabilizedCycles > 0)
            {
                multiplier *= 0.5f; // Reduce chances by half if destabilized
            }
            if (Attacker.IsCoveredInPlants)
            {
                multiplier *= 1.5f; // Increase chances by 1.5x if attacker is covered in plants
            }


            foreach(Object o in Attacker.Clothing)
            {
                if(!o.IsWearable)
                {
                    multiplier += 0.2f;
                }
            }

            float attackersProficiencyImpact = 1.0f - (attackersProficiency * 0.03f); // Each proficiency level reduces defender's success chance by 3%

            // Apply the multiplier and ensuring the value stays within 0-100
            sustainChance = (int)Math.Round(sustainChance * multiplier);
            parryChance = (int)Math.Round(parryChance * multiplier);
            blockChance = (int)Math.Round(blockChance * multiplier);
            duckChance = (int)Math.Round(duckChance * multiplier);
            jumpChance = (int)Math.Round(jumpChance * multiplier);
            rollChance = (int)Math.Round(rollChance * multiplier);
            disarmChance = (int)Math.Round(disarmChance * multiplier);
            redirectChance = (int)Math.Round(redirectChance * multiplier);


            //Apply Weight Reduction

            int CalculateTotalWeight(IEnumerable<Object> objects)
            {
                int totalWeight = 0;
                foreach (var obj in objects)
                {
                    totalWeight += (int)(obj.Weight);
                    if (obj.ContainedObjects != null && obj.ContainedObjects.Any())
                    {
                        totalWeight += CalculateTotalWeight(obj.ContainedObjects); // Recursively add the weight of contained objects
                    }
                }
                return totalWeight;
            }

            int totalWeight = CalculateTotalWeight(this.Inventory)
                              + CalculateTotalWeight(this.Clothing)
                              + (this.LeftHandObject != null ? CalculateTotalWeight(new[] { this.LeftHandObject }) : 0)
                              + (this.RightHandObject != null ? CalculateTotalWeight(new[] { this.RightHandObject }) : 0);

            float weightImpact = 1.0f - (totalWeight / 1000 * 0.02f); 
            multiplier *= weightImpact; 

            // Ensuring values are clamped between 0 and 100 after final adjustments
            return (
                Math.Max(0, Math.Min(sustainChance, 100)), // Sustain chance isn't affected by proficiency as it's always 0
                Math.Max(0, Math.Min((int)(parryChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(blockChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(duckChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(jumpChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(rollChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(disarmChance * attackersProficiencyImpact), 100)),
                Math.Max(0, Math.Min((int)(redirectChance * attackersProficiencyImpact), 100))
            );
        }






        private int ApplyRandomMultiplier(int baseChance, int reactionModifierInt)
        {
            // Convert the reactionModifierInt to a percentage within the range of -10% to +10%
            double multiplier = ((reactionModifierInt % 21) - 10) / 100.0; // This will give a value between -0.10 and 0.10

            // Apply this multiplier to the base chance
            int adjustedChance = (int)(baseChance + baseChance * multiplier);
            return Math.Min(Math.Max(adjustedChance, 0), 100); // Ensuring the chance is between 0 and 100
        }


        private int CalculateProficiencyModifier(Architect architect, Object weapon)
        {
            if (weapon != null && weapon.IsWeapon)
            {
                switch (weapon.DamageType)
                {
                    case "slashing":
                        return architect.GetProficiency("slashing");
                    case "piercing":
                        return architect.GetProficiency("piercing");
                    case "bashing":
                        return architect.GetProficiency("bashing");
                    case "scourging":
                        return architect.GetProficiency("scourging");
                    default:
                        return 0;
                }
            }
            return 0;
        }

        public Location Location { get; set; }
        public District District { get; set; }
        public Block Block { get; set; }
        public Structure Structure { get; set; }
        public Room Room { get; set; }

        public string Size { get; set; } = "average";

        public string CurrentlyMovingPlace { get; set; } = "none";
        public bool InTheProcessOfLeaving { get; set; } = false;

        public Location HomeLocation { get; set; }
        public District HomeDistrict { get; set; }
        public Structure HomeStructure { get; set; }

        public string Destiny { get; set; } = "none";
        public int DestinyArrivalYear { get; set; } = 999;

        public List<(Architect, int)> KnownArchitectsAndOpinions { get; set; } = new List<(Architect, int)>();

        public bool IsStudying { get; set; }
        public bool Loaded { get; set; }

        public List<Object> Clothing = new List<Object>();

        public string ScholarType { get; set; } = "";
        public string FavoriteScienceField { get; set; } = "";
        public string FavoriteCultureField { get; set; } = "";
        public string FavoriteMagicField { get; set; } = "";

        public int MagicStudyPoints { get; set; } = 0;
        public int CultureStudyPoints { get; set; } = 0;
        public int ScienceStudyPoints { get; set; } = 0;

        public List<string> SpellsKnown { get; set; } = new List<string>();
        public bool DiscoveredASpell { get; set; } = false;

        public int FireCycles { get; set; } = 0;
        public int WetCycles { get; set; } = 0;
        public int BlindCycles { get; set; } = 0;
        public int DestabilizedCycles { get; set; } = 0;
        public int ConcussionCycles { get; set; } = 0;

        public bool OnGround { get; set; } = false;

        public int FractalCycles = 0;

        public bool IsImmortal { get; set; } = false;
        public bool IsCoveredInPlants { get; set; } = false;

        public int YLevelInFeet { get; set; } = 0;
        public int YVelocity { get; set; } = 0;
        public int SpellHoldCycles { get; set; } = 0;

        public bool Focused { get; set; } = false;

        public int DaysSinceFood { get; set; } = 0;
        public int DaysSinceLiquid { get; set; } = 0;
        public int NightsSinceSleep { get; set; } = 0;
        public int DaysSinceCoffeeOrTea { get; set; } = 0;
        public int DaysSinceSocialized { get; set; } = 0;
        public int DaysSincePerforming { get; set; } = 0;
        public int DaysSincePlayingGame { get; set; } = 0;

        public Architect TargetArchitect { get; set; }
        public Object TargetObject { get; set; }
        public (Region, Location, District, Block, Structure, string) Target { get; set; }

        public string Task { get; set; } = "";
        public int CyclesLeftInTask { get; set; } = 0;

        public double Energy;
        public int MaxEnergyMod = 0;
        public int MaxEnergy()
        {
            int Max = (Endurance * 10) + 100;

            if(Race == Game1.GameWorld.GetRace("luminarch"))
            {
                Max += Max * (1 / 10);
            }
            else if (Race == Game1.GameWorld.GetRace("nightfell"))
            {
                Max -= Max * (1 / 10);
            }

            Max += MaxEnergyMod;

            return Max;
        }


        public double Bleeding = 0;

        public List<Object> BodyParts { get; set; } = new List<Object>();

        public bool PrefersCoffeeIfTrue { get; set; } = false;

        public bool IsColossal { get; set; } = false;

        public int ColossalMinefieldX { get; set; }
        public int ColossalMinefieldZ { get; set; }

        public int ExtraShieldEffectiveness = 0;
        public int ExtraAttackPower = 0;
        public int ExtraDodgeChance = 0;
        public int ExtraRedirectionChance = 0;
        public int ExtraBashingResistance = 0;
        public int ExtraSlashingResistance = 0;
        public int ExtraPiercingResistance = 0;
        public int ExtraScourgingResistance = 0;
        public int ExtraStealth = 0;
        public int ExtraHealingCapability = 0;
        public int ExtraEnergyRegen = 0;

        public int ArmorProficiency { get; set; } = 0;

        public List<string> OppositionTags { get; set; } = new List<string>() { };
        public List<Architect> SuperTrustedArchitects { get; set; } = new List<Architect>();

        public List<(string, int)> XPValues { get; set; } = new List<(string, int)>
        {
            ("slashing", 0),
            ("piercing", 0),
            ("bashing", 0),
            ("scourging", 0),
            ("dodging", 0),
            ("blocking", 0),
            ("disarming", 0),
            ("redirection", 0),
            ("parrying", 0)
        };

        public void ChangeXP(string proficiencyName, int xpChange)
        {
            var proficiencyIndex = XPValues.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiencyIndex != -1)
            {
                // If found, update the XP
                XPValues[proficiencyIndex] = (XPValues[proficiencyIndex].Item1, XPValues[proficiencyIndex].Item2 + xpChange);
            }
        }

        public int GetProficiency(string proficiencyName)
        {
            // Find the proficiency in the list
            var proficiency = XPValues.FirstOrDefault(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (proficiency.Equals(default((string, int))))
            {
                return -1; // Return -1 if not found
            }
            else
            {
                // Return the level based on the XP
                return CalculateLevel(proficiency.Item2);
            }
        }
        private int CalculateLevel(int xp)
        {
            int level = 1;
            int currentThreshold = 100;
            while (xp >= currentThreshold)
            {
                level++;

                // Determine the multiplier based on the cycle for this level
                double multiplier;
                if ((level - 2) % 3 == 0) multiplier = 2.5; // Every 3rd level starting from level 2
                else multiplier = 2.0; // For the other levels

                currentThreshold = (int)(currentThreshold * multiplier);
            }
            return level;
        }
 

        public void SetProficiency(string proficiencyName, int value)
        {
            // Check if the proficiency already exists
            int index = XPValues.FindIndex(p => p.Item1.Equals(proficiencyName, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                // Update existing proficiency
                XPValues[index] = (proficiencyName, value);
            }
        }
        public Object MainHandObject()
        {
            if (RightHanded == true)
            {
                return RightHandObject;
            }
            else
            {
                return LeftHandObject;
            }
        }
        public Object OffHandObject()
        {
            if (RightHanded == false)
            {
                return RightHandObject;
            }
            else
            {
                return LeftHandObject;
            }
        }

        public void ChangeOpinion(Architect a, int Change)
        {
            int NewOpinion = 0;
            foreach ((Architect, int) Arch in KnownArchitectsAndOpinions)
            {
                if (Arch.Item1 == a)
                {
                    NewOpinion = Arch.Item2 + Change;
                    KnownArchitectsAndOpinions.Remove(Arch);
                    KnownArchitectsAndOpinions.Add((Arch.Item1, NewOpinion));
                    return;
                }
            }
            //this only runs if we didnt find the architect
            KnownArchitectsAndOpinions.Add((a, Change));
        }
        public int GetOpinion(Architect a)
        {
            foreach ((Architect, int) Arch in KnownArchitectsAndOpinions)
            {
                if (Arch.Item1 == a)
                {
                    return (Arch.Item2);
                }
            }
            return (0);
        }


        public void KitOutArchitect(string Type)
        {
            // Adjusted for warrior power
            if (Type == "warriorpower0")
            {
                // Create a weapon
                Material weaponMaterial = Location.HomeCivilization.CulturalMetal;
                string weaponType = Game1.AllWeapons[Game1.r.Next(Game1.AllWeapons.Count())];
                Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);

                // Assign the weapon to the appropriate hand
                if (RightHanded)
                {
                    RightHandObject = weapon;
                }
                else
                {
                    LeftHandObject = weapon;
                }

                // List of possible armor types to create
                List<string> armorTypes = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings", "left boot", "right boot" };
                Material armorMaterial = Location.HomeCivilization.CulturalMetal; // Material for the armor

                // Random chance generator
                Random r = new Random();

                foreach (string armorType in armorTypes)
                {
                    // 75% chance to create and equip each armor piece
                    if (r.Next(100) < 75)
                    {
                        // Create armor object
                        Object armor = new Object(null, armorType, new List<Material>() { armorMaterial }, null);

                        // Equip the armor to the architect
                        // This method needs to be implemented according to your game's logic
                        Clothing.Add(armor);
                    }
                }
            }
            // Priest outfitting
            else if (Type == "priest")
            {
                // Randomize material between Shadesteel or Prismite

                Material m = null;

                if (Race == Game1.GameWorld.GetRace("luminarch"))
                {
                    m = Game1.GameWorld.Prismite;
                }
                else if (Race == Game1.GameWorld.GetRace("nightfell"))
                {
                    m = Game1.GameWorld.Shadesteel;
                }

                List<Object> o = Game1.GameWorld.LootTableMachine("magicaltreasure12");

                if (m != null)
                {
                    foreach (Object O in o)
                    {
                        O.Materials.Add(m);
                    }
                }

                Inventory.AddRange(o);
            }
            else if (Type == "adventurer")
            {
                // Equip with a weapon
                Material weaponMaterial = FavoriteMetal;
                string weaponType = Game1.AllWeapons[Game1.r.Next(Game1.AllWeapons.Count())];
                Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);
                if (RightHanded)
                {
                    RightHandObject = weapon;
                }
                else
                {
                    LeftHandObject = weapon;
                }

                // Inside the 'else if (Type == "adventurer")' block, when equipping with armor
                List<string> armorTypes = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings", "left boot", "right boot" };
                foreach (string armorType in armorTypes)
                {
                    if (Game1.r.Next(100) < 75) // 75% chance to equip each armor piece
                    {
                        Material armorMaterial = FavoriteMetal; // Assuming FavoriteMetal is defined elsewhere

                        if (armorType == "gauntlet" || armorType == "boot")
                        {
                            // Create left and right variants for gauntlets or boots
                            Object leftArmor = new Object(null, "left " + armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(leftArmor);

                            Object rightArmor = new Object(null, "right " + armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(rightArmor);
                        }
                        else
                        {
                            // For other armor types, just create one object
                            Object armor = new Object(null, armorType, new List<Material>() { armorMaterial }, null);
                            Clothing.Add(armor);
                        }
                    }
                }



                // Add general adventuring supplies
                // Inside the 'else if (Type == "adventurer")' block, after equipping with armor
                Random random = new Random();

                // Add a cup filled with a drink (50% chance)
                if (random.Next(2) == 0) // 50% chance
                {
                    Material cupMaterial = FavoriteStone; // Material for the cup
                    Material drinkMaterial = random.Next(2) == 0 ? Game1.GameWorld.Coffee : Game1.GameWorld.Tea; // Randomly choose between coffee or tea
                    Object cup = new Object(null, "small cup", new List<Material>() { cupMaterial }, null);
                    cup.ContainedObjects.Add(new Object(null, "drink", new List<Material>() { drinkMaterial }, null));
                    Inventory.Add(cup);
                }

                // Add cut gems (50% chance)
                if (random.Next(2) == 0)
                {
                    Object gem = new Object(null, "cut gem", new List<Material>() { /* Specify gem material */ }, null);
                    Inventory.Add(gem);
                }

                // Add extra weapons (50% chance)
                if (random.Next(2) == 0)
                {
                    string WeaponType = Game1.AllWeapons[random.Next(Game1.AllWeapons.Count())];
                    Object extraWeapon = new Object(null, WeaponType, new List<Material>() { FavoriteMetal }, null);
                    Inventory.Add(extraWeapon);
                }

                // Add a wax tablet (50% chance)
                if (random.Next(2) == 0)
                {
                    Object waxTablet = new Object(null, "waxtablet", new List<Material>() { FavoriteWood }, null);
                    Inventory.Add(waxTablet);
                }

                // Add jars (50% chance)
                if (random.Next(2) == 0)
                {
                    Object jar = new Object(null, "jar", new List<Material>() { Game1.GameWorld.Glass }, null);
                    Inventory.Add(jar);
                }

                // Add fragments made of Game1.Vitalium (50% chance)
                if (random.Next(2) == 0)
                {
                    Object fragment = new Object(null, "fragment", new List<Material>() { Game1.GameWorld.Vitalium }, null);
                    Inventory.Add(fragment);
                }

                // Note: For material specifics (/* Specify material */), you should define or select the appropriate materials based on your game's world and logic.


            }
            else if (Type == "archmage")
            {
                // Assigning a staff as a weapon
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new List<Material>() { staffMaterial }, null);
                staff.Rarity = "rare";
                staff.ApplyImbuements(1); // Assuming 1 indicates a minor magical enhancement
                RightHandObject = staff;

                // Wearing a robe made from cultural cloth
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                Clothing.Add(robe);

                // Carrying a magical tome as an inventory item
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                book.ApplyImbuements(2); // Assuming 2 indicates magical texts or spells
                Inventory.Add(book);
            }
            else if (Type == "beastmaster")
            {

                // Assigning a whip as a weapon, indicating control over beasts
                Material whipMaterial = Location.HomeCivilization.CulturalCloth; // Assuming there's a CulturalLeather not listed but logically present
                Object whip = new Object(null, "whip", new List<Material>() { whipMaterial }, null);
                RightHandObject = whip;

                // Equipping boots for traversing wild terrains
                Material bootsMaterial = Location.HomeCivilization.CulturalCloth; // Assuming again the presence of CulturalLeather
                Object boots = new Object(null, "left boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
                // Repeating for right boot
                boots = new Object(null, "right boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
            }
            else if (Type == "warlock")
            {
                // Assigning a dark tome as a weapon/source of power
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Assuming 3 indicates dark or forbidden knowledge
                RightHandObject = tome;

                // Carrying a potion indicative of dark arts
                Material potionMaterial = Location.HomeCivilization.CulturalGemstone; // Using gemstone as a placeholder for magical essence
                Object potion = new Object(null, "lesser energy potion", new List<Material>() { potionMaterial }, null);
                potion.Rarity = "uncommon";
                Inventory.Add(potion);

                // Equipping an amulet as a source of magical protection or power
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(1); // Minor magical protection
                Clothing.Add(amulet);
            }
            else if (Type.ToLower() == "duelist")
            {
                // Set Duelist's agility
                Agility = 10;

                // List of possible weapon types for a Duelist from the provided object constructor
                List<string> possibleWeapons = new List<string> { "sword", "dagger", "rapier", "knife", "sword" };

                // Determine the number of weapons to assign (between 2 and 4)
                int numberOfWeapons = Game1.r.Next(2, 5); // Assuming Game1.r is a Random instance

                // Selecting random weapons
                for (int i = 0; i < numberOfWeapons; i++)
                {
                    string weaponType = possibleWeapons[Game1.r.Next(possibleWeapons.Count)];
                    Material weaponMaterial = Location.HomeCivilization.CulturalMetal; // Assuming all weapons are made of the civilization's cultural metal
                    Object weapon = new Object(null, weaponType, new List<Material>() { weaponMaterial }, null);

                    // For the first weapon, assign it to the right hand if right-handed, otherwise add to inventory
                    if (i == 0 && RightHanded)
                    {
                        RightHandObject = weapon;
                    }
                    else
                    {
                        Inventory.Add(weapon);
                    }
                }

                // Cool items addition - A lightweight cloak for agility and some mystique
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);

                // Adding a small, easily concealable magical item for surprise or utility
                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object magicalAmulet = new Object(null, "amulet", new List<Material>() { gemMaterial }, null); // A magical amulet for an unexpected advantage
                magicalAmulet.Rarity = "rare";
                magicalAmulet.ApplyImbuements(1); // Imbuement for agility or other duelist-favorable attributes
                Inventory.Add(magicalAmulet);
            }

            else if (Type.ToLower() == "mage")
            {
                // Assigning a wand as a magical implement
                Material orbmat = Location.HomeCivilization.CulturalGemstone;
                Object orb = new Object(null, "orb", new List<Material>() { orbmat }, null);
                orb.Rarity = "rare";
                orb.ApplyImbuements(2); // Assuming this imbues the wand with specific magical properties
                RightHandObject = orb;

                // Wearing a robe made from cultural cloth, indicative of a mage's status
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                robe.Rarity = "uncommon";
                robe.ApplyImbuements(1); // Possibly for protection or augmentation of magical abilities
                Clothing.Add(robe);

                // Carrying a magical tome for spells
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "rare";
                book.ApplyImbuements(3); // Indicating a book of powerful spells or arcane knowledge
                Inventory.Add(book);
            }
            else if (Type.ToLower() == "sorcerer")
            {
                // Equipping with a staff as a primary magical conduit
                Material staffMaterial = Location.HomeCivilization.CulturalWood;
                Object staff = new Object(null, "staff", new List<Material>() { staffMaterial }, null);
                staff.Rarity = "uncommon";
                staff.ApplyImbuements(2); // Assume this enhances magical power
                RightHandObject = staff;

                // Carrying a tome of spells
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(0); // Assuming this contains high-level spells
                Inventory.Add(tome);

                // A magical amulet for defense or power boost
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "rare";
                amulet.ApplyImbuements(0); // Providing magical protection or power boost
                Inventory.Add(amulet);
            }
            else if (Type.ToLower() == "elemental")
            {
                // Carrying a gemstone that resonates with their elemental nature
                Material gemMaterial = Location.HomeCivilization.CulturalGemstone;
                Object elementalGem = new Object(null, "gem", new List<Material>() { gemMaterial }, null);
                elementalGem.Rarity = "rare"; // Elemental gems are precious and powerful
                elementalGem.ApplyImbuements(2); // Assuming this enhances their control over their element
                Inventory.Add(elementalGem);
            }
            else if (Type.ToLower() == "spy")
            {
                // A dagger for close, silent attacks or utility
                Material daggerMaterial = Location.HomeCivilization.CulturalMetal;
                Object dagger = new Object(null, "dagger", new List<Material>() { daggerMaterial }, null);
                RightHandObject = dagger;

                // A cloak for blending into shadows
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "artificer")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Location.HomeCivilization.CulturalMetal;
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                RightHandObject = multiTool;

                // Carrying a blueprint or schematic for a masterpiece
                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new List<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }
            else if (Type.ToLower() == "archartificer")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Location.HomeCivilization.CulturalMetal;
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                RightHandObject = multiTool;

                // Carrying a blueprint or schematic for a masterpiece
                Material schematicMaterial = Location.HomeCivilization.CulturalSheet;
                Object schematic = new Object(null, "sheet", new List<Material>() { schematicMaterial }, null);
                schematic.Rarity = "uncommon";
                Inventory.Add(schematic);
            }
            else if (Type.ToLower() == "archbard")
            {
                // Equipping with a cloak that enhances their charismatic presence
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "archduelist")
            {
                // Set Archduelist's agility and skill
                Agility = 12; // Assuming modification of stats is permissible here for emphasis

                // Equipping with a masterfully crafted rapier or sword
                Material weaponMaterial = Location.HomeCivilization.CulturalMetal;
                Object weapon = new Object(null, "rapier", new List<Material>() { weaponMaterial }, null);
                weapon.Rarity = "epic";
                weapon.ApplyImbuements(3); // For superior finesse and damage
                RightHandObject = weapon;

                // Carrying a talisman for luck or protection in duels
                Material talismanMaterial = Location.HomeCivilization.CulturalGemstone;
                Object talisman = new Object(null, "amulet", new List<Material>() { talismanMaterial }, null);
                talisman.Rarity = "uncommon";
                Inventory.Add(talisman);
            }
            else if (Type.ToLower() == "archluminary")
            {
                // Bearing a symbol of office or authority
                Material symbolMaterial = Location.HomeCivilization.CulturalGemstone;
                Object symbol = new Object(null, "amulet", new List<Material>() { symbolMaterial }, null);
                symbol.Rarity = "epic";
                symbol.ApplyImbuements(2); // Signifying power or protection
                RightHandObject = symbol;

                // Wearing a robe that denotes their high rank
                Material robeMaterial = Location.HomeCivilization.CulturalCloth;
                Object robe = new Object(null, "robe", new List<Material>() { robeMaterial }, null);
                robe.Rarity = "rare";
                Clothing.Add(robe);

                // Carrying a tome of ancient wisdom or law
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Containing knowledge or spells of governance and influence
                Inventory.Add(tome);
            }
            else if (Type.ToLower() == "bard")
            {
                // Wearing a garment that enhances their allure
                Material garmentMaterial = Location.HomeCivilization.CulturalCloth;
                Object garment = new Object(null, "cape", new List<Material>() { garmentMaterial }, null);
                garment.Rarity = "uncommon";
                Clothing.Add(garment);
            }
            else if (Type.ToLower() == "conjumancer")
            {
                // Holding a summoning crystal
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object crystal = new Object(null, "orb", new List<Material>() { crystalMaterial }, null); // "Orb" used here to represent a crystal with summoning power
                crystal.Rarity = "rare";
                crystal.ApplyImbuements(2); // Assume this enhances summoning power
                RightHandObject = crystal;

                // Wearing a cloak that conceals their presence, aiding in summoning rituals
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "diplomancer")
            {
                // Bearing a medallion that signifies their diplomatic status
                Material medallionMaterial = Location.HomeCivilization.CulturalMetal;
                Object medallion = new Object(null, "amulet", new List<Material>() { medallionMaterial }, null);
                medallion.Rarity = "uncommon";
                Inventory.Add(medallion);

                // Carrying a scroll of truce or treaties
                Material scrollMaterial = Location.HomeCivilization.CulturalSheet;
                Object scroll = new Object(null, "scroll", new List<Material>() { scrollMaterial }, null);
                scroll.Rarity = "common";
                Inventory.Add(scroll);

                // Wearing attire that reflects their diplomatic role
                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new List<Material>() { attireMaterial }, null); // "Robe" here symbolizes formal diplomatic wear
                attire.Rarity = "uncommon";
                Clothing.Add(attire);
            }
            else if (Type.ToLower() == "fractalmancer")
            {
                // Holding a fractal orb that embodies their control over patterns
                Material fractalOrbMaterial = Location.HomeCivilization.CulturalGemstone;
                Object fractalOrb = new Object(null, "orb", new List<Material>() { fractalOrbMaterial }, null);
                fractalOrb.Rarity = "rare";
                fractalOrb.ApplyImbuements(3); // Enhancing their reality-manipulating powers
                RightHandObject = fractalOrb;

                // Wearing attire that seems to shift and change patterns
                Material attireMaterial = Location.HomeCivilization.CulturalCloth;
                Object attire = new Object(null, "robe", new List<Material>() { attireMaterial }, null);
                attire.Rarity = "uncommon";
                Clothing.Add(attire);
            }
            else if (Type.ToLower() == "hunter")
            {
                // Equipped with a knife for skinning and close combat
                Material knifeMaterial = Location.HomeCivilization.CulturalMetal;
                Object knife = new Object(null, "knife", new List<Material>() { knifeMaterial }, null);
                knife.Rarity = "common";
                Inventory.Add(knife);

                // Wearing boots suitable for tracking through various terrains
                Material bootsMaterial = Location.HomeCivilization.CulturalCloth; // Assuming for thematic consistency
                Object boots = new Object(null, "left boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
                boots = new Object(null, "right boot", new List<Material>() { bootsMaterial }, null);
                Clothing.Add(boots);
            }
            else if (Type.ToLower() == "knight")
            {
                // Equipped with a sword, the knight's primary weapon
                Material swordMaterial = Location.HomeCivilization.CulturalMetal;
                Object sword = new Object(null, "sword", new List<Material>() { swordMaterial }, null);
                sword.Rarity = "common";
                RightHandObject = sword;

                // Wearing a full set of armor for protection
                Material armorMaterial = Location.HomeCivilization.CulturalMetal;
                List<string> armorPieces = new List<string> { "helmet", "chestplate", "left gauntlet", "right gauntlet", "leggings" };
                foreach (var piece in armorPieces)
                {
                    Object armor = new Object(null, piece, new List<Material>() { armorMaterial }, null);
                    armor.Rarity = "uncommon";
                    Clothing.Add(armor);
                }
            }
            else if (Type.ToLower() == "magician")
            {
                // Holding a summoning crystal
                Material crystalMaterial = Location.HomeCivilization.CulturalGemstone;
                Object crystal = new Object(null, "orb", new List<Material>() { crystalMaterial }, null); // "Orb" used here to represent a crystal with summoning power
                crystal.Rarity = "uncommon";
                crystal.ApplyImbuements(0); // Assume this enhances summoning power
                RightHandObject = crystal;

                // Wearing a cloak that provides some magical protection
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "mercenary")
            {
                // Equipped with a versatile weapon, such as a sword or axe
                Material weaponMaterial = Location.HomeCivilization.CulturalMetal;
                Object weapon = new Object(null, "sword", new List<Material>() { weaponMaterial }, null); // "Sword" can be replaced with "axe" or another weapon based on preference
                weapon.Rarity = "common";
                RightHandObject = weapon;

                // Wearing durable leather armor for protection and mobility
                Material armorMaterial = Location.HomeCivilization.CulturalCloth; // Assuming leather is represented by "Cloth" for this context
                Object armor = new Object(null, "chestplate", new List<Material>() { armorMaterial }, null);
                armor.Rarity = "common";
                Clothing.Add(armor);
            }
            else if (Type.ToLower() == "necromancer")
            {
                // Holding a dark tome containing necromantic spells
                Material tomeMaterial = Location.HomeCivilization.CulturalSheet;
                Object tome = new Object(null, "book", new List<Material>() { tomeMaterial }, null);
                tome.Rarity = "rare";
                tome.ApplyImbuements(3); // Enhances dark magic abilities
                RightHandObject = tome;

                // Wearing a cloak that signifies their macabre affinity
                Material cloakMaterial = Location.HomeCivilization.CulturalCloth;
                Object cloak = new Object(null, "cape", new List<Material>() { cloakMaterial }, null);
                cloak.Rarity = "uncommon";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "perceptomancer")
            {
                // Equipped with an amulet that enhances sensory perception
                Material amuletMaterial = Location.HomeCivilization.CulturalGemstone;
                Object amulet = new Object(null, "amulet", new List<Material>() { amuletMaterial }, null);
                amulet.Rarity = "uncommon";
                amulet.ApplyImbuements(2); // Boosts perceptual abilities
                Inventory.Add(amulet);
            }
            else if (Type.ToLower() == "scout")
            {
                // Carrying a short sword or dagger for self-defense
                Material weaponMaterial = Location.HomeCivilization.CulturalMetal;
                Object weapon = new Object(null, "dagger", new List<Material>() { weaponMaterial }, null);
                weapon.Rarity = "common";
                RightHandObject = weapon;

                // Equipped with light armor for mobility
                Material armorMaterial = Location.HomeCivilization.CulturalCloth;
                Object armor = new Object(null, "chestplate", new List<Material>() { armorMaterial }, null);
                armor.Rarity = "common";
                Clothing.Add(armor);

                // Using a cloak for camouflage
                Object cloak = new Object(null, "cape", new List<Material>() { armorMaterial }, null);
                cloak.Rarity = "common";
                Clothing.Add(cloak);
            }
            else if (Type.ToLower() == "shadeheart")
            {
                //nothing
            }
            else if (Type.ToLower() == "spatiomancer")
            {
                // Equipped with a spatial ring
                Material ringMaterial = Location.HomeCivilization.CulturalGemstone;
                Object ring = new Object(null, "amulet", new List<Material>() { ringMaterial }, null); // Assuming "amulet" can represent a ring in this context
                ring.Rarity = "epic";
                ring.ApplyImbuements(4); // For spatial manipulation and teleportation
                Inventory.Add(ring);

                // Carrying a tome on spatial theories
                Material bookMaterial = Location.HomeCivilization.CulturalSheet;
                Object book = new Object(null, "book", new List<Material>() { bookMaterial }, null);
                book.Rarity = "uncommon";
                Inventory.Add(book);
            }
            else if (Type.ToLower() == "thief")
            {
                // Equipping with a multi-tool device
                Material toolMaterial = Location.HomeCivilization.CulturalMetal;
                Object multiTool = new Object(null, "hammer", new List<Material>() { toolMaterial }, null); // Assuming "small tool" represents a versatile device
                multiTool.Rarity = "rare";
                multiTool.ApplyImbuements(0); // Enhancements for crafting or magical tinkering
                RightHandObject = multiTool;

                // Equipped with a small dagger for self-defense and utility
                Material daggerMaterial = Location.HomeCivilization.CulturalMetal;
                Object dagger = new Object(null, "dagger", new List<Material>() { daggerMaterial }, null);
                dagger.Rarity = "common";
                RightHandObject = dagger;

                // Wearing dark clothing for stealth
                Material clothingMaterial = Location.HomeCivilization.CulturalCloth;
                Object clothing = new Object(null, "robe", new List<Material>() { clothingMaterial }, null);
                clothing.Rarity = "common";
                Clothing.Add(clothing);
            }
        }



        public Architect(string name, string sex, Race race, int age, string role, List<Object> inventory, Location location, District district, Block block, string destiny, int Level)
        {
            Location = location;
            Block = block;
            District = district;

            Game1.GameWorld.AllArchitects.Add(this);

            MoralCompass = Game1.r.Next(-100, 101); //more is good, less is evil
            StabilityCompass = Game1.r.Next(-100, 101); //more is lawful, less is chaotic

            Name = name;
            Sex = sex;

            if (Location != null && Location.Region.World.HumanoidRaces.Contains(race))
            {
                AddCulturalClothing(Location.HomeCivilization.CulturalHeadwear, Location.HomeCivilization.CulturalCloth);
                AddCulturalClothing(Location.HomeCivilization.CulturalNeckwear, Location.HomeCivilization.CulturalCloth);
                AddCulturalClothing(Location.HomeCivilization.CulturalBodywear, Location.HomeCivilization.CulturalCloth);
                AddCulturalClothing(Location.HomeCivilization.CulturalLegwear, Location.HomeCivilization.CulturalCloth);
                AddCulturalClothing(Location.HomeCivilization.CulturalHandwear, Location.HomeCivilization.CulturalCloth);
                AddCulturalClothing(Location.HomeCivilization.CulturalFootwear, Location.HomeCivilization.CulturalCloth);
            }

            List<int> SkillValues = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
            // Shuffle the SkillValues list
            List<int> shuffledSkills = SkillValues.OrderBy(x => Game1.r.Next()).ToList();

            // Assign values to each skill
            Strength = shuffledSkills[0];
            Agility = shuffledSkills[1];
            Dexterity = shuffledSkills[2];
            Endurance = shuffledSkills[3];
            Creativity = shuffledSkills[4];
            Charisma = shuffledSkills[5];
            Focus = shuffledSkills[6];

            //give him a ton of alligned domains.
            AlignedDomains = Game1.Domains.OrderBy(x => Guid.NewGuid()).Take(Game1.r.Next(1, 8)).ToList();

            // Function to add cultural clothing items
            void AddCulturalClothing(string culturalItems, Material material)
            {
                if (culturalItems != "none")
                {
                    string[] items = culturalItems.Split('/');

                    foreach (string item in items)
                    {
                        Clothing.Add(new Object(null, item.Trim(), new List<Material>() { material }, null));
                    }
                }
            }

            if (Sex == "male")
            {
                Pronoun = "he";
                PossessivePronoun = "his";
                ObjectivePronoun = "him";
            }
            else if (Sex == "female")
            {
                Pronoun = "she";
                PossessivePronoun = "her";
                ObjectivePronoun = "her";
            }

            Race = race;
            NaturalArmor = race.NaturalArmor;
            OppositionTags.AddRange(Race.OppositionTags);

            Age = age;

            if(inventory == null)
            {
                Inventory = new List<Object>();
            }
            else
            {
                Inventory = inventory;
            }


            if(role != null && role != "")
            {
                Profession = role;
            }
            else
            {
                Profession = "no profession";
            }


            Destiny = destiny;
            Loaded = false;


            FavoriteCultureField = Game1.CultureSchools[Game1.r.Next(Game1.CultureSchools.Count)];
            FavoriteScienceField = Game1.ScienceSchools[Game1.r.Next(Game1.ScienceSchools.Count)];
            FavoriteMagicField = Game1.MagicSchools[Game1.r.Next(Game1.MagicSchools.Count)];

            FavoriteColor = Game1.Colors[Game1.r.Next(Game1.Colors.Count)];
            FavoriteGemstone = Game1.GameWorld.Gemstones[Game1.r.Next(Game1.GameWorld.Gemstones.Count)];
            FavoriteStone = Game1.GameWorld.Stones[Game1.r.Next(Game1.GameWorld.Stones.Count)];
            FavoriteWood = Game1.GameWorld.Woods[Game1.r.Next(Game1.GameWorld.Woods.Count)];
            FavoriteMetal = Game1.GameWorld.Metals[Game1.r.Next(Game1.GameWorld.Metals.Count)];
            FavoriteCloth = Game1.GameWorld.Cloths[Game1.r.Next(Game1.GameWorld.Cloths.Count)];

            HomeDistrict = district;
            HomeLocation = location;

            DestinyArrivalYear = Game1.r.Next(18, 45);

            if (Game1.GameWorld.HumanoidRaces.Contains(Race))
            {
                if (Game1.r.Next(1, 4) == 1)
                {
                    DoIDieOfOldAge = false;
                    TerminalAge = Game1.r.Next(Age, 120);
                }
                else
                {
                    DoIDieOfOldAge = true;
                    TerminalAge = Game1.r.Next(Age, 120);
                }
            }
            else
            {
                DoIDieOfOldAge = false;
                TerminalAge = 13371337;
            }


            if (Profession == "scholar")
            {
                int ScholarDecider = Game1.r.Next(1, 16);

                if (ScholarDecider < 3)
                {
                    ScholarType = "mage";
                }
                else if (ScholarDecider < 6)
                {
                    ScholarType = "engineer";
                }
                else if (ScholarDecider < 9)
                {
                    ScholarType = "entertainer";
                }
                else if (ScholarDecider < 11)
                {
                    ScholarType = "artificer";
                }
                else if (ScholarDecider < 13)
                {
                    ScholarType = "bard";
                }
                else if (ScholarDecider < 15)
                {
                    ScholarType = "sage";
                }
                else
                {
                    ScholarType = "luminary";
                }
            }

            ReferredToNames = new List<string>() { "Placeholder" };

            AddBodyParts();

            foreach (Object o in BodyParts)
            {
                o.IsBodyPart = true;
            }

            Energy = MaxEnergy();
        }

        public void AddBodyParts()
        {
            if (Race != null)
            {
                foreach ((string, Material) o in Race.BodyParts)
                {
                    BodyParts.Add(new Object(Name + "'s " + o.Item1, o.Item1, new List<Material> { o.Item2 }, false, false, null, this, 5, false, null, null, null, false));
                }
            }
        }

        public void DropInventory()
        {
            if(Room != null)
            {
                Room.Objects.AddRange(Inventory);
                Room.Objects.AddRange(Clothing);
                Room.Objects.Add(RightHandObject);
                Room.Objects.Add(LeftHandObject);
            }
            else if (Block != null)
            {
                Block.Objects.AddRange(Inventory);
                Block.Objects.AddRange(Clothing);
                Block.Objects.Add(RightHandObject);
                Block.Objects.Add(LeftHandObject);
            }
        }

        public void UpdateNames()
        {
            //update referred to names
            bool PlayerKnowsArch = false;
            ReferredToNames = new List<string>();

            if (IsAlive)
            {
                foreach ((Architect, int) a in Game1.MostRecentPartyTurnArchitect.KnownArchitectsAndOpinions)
                {
                    if (a.Item1 == this)
                    {
                        PlayerKnowsArch = true;
                    }
                }


                // Determine if the player knows the architect or if the architect is part of the party.
                if (PlayerKnowsArch == true || Game1.GamePlayerParty.Architects.Contains(this))
                {
                    ReferredToNames = new List<string>() { Name };

                    // Only add the prefix if Task is not empty.
                    if (!string.IsNullOrEmpty(Task))
                    {
                        ReferredToNames.Add(Game1.ConvertArchitectToDescription(this));
                    }
                }
                else
                {
                    // Handle unknown profession or description scenario

                    ReferredToNames = new List<string>();

                    if (!string.IsNullOrEmpty(Profession))
                    {
                        ReferredToNames.Add("the unknown " + Profession);
                        ReferredToNames.Add("unknown " + Profession);
                        ReferredToNames.Add(Profession);
                    }
                    else
                    {
                        ReferredToNames.Add("indolent");
                        ReferredToNames.Add("unknown indolent");
                        ReferredToNames.Add("the unknown indolent");
                    }

                    // Only add if Task is empty
                    if (string.IsNullOrEmpty(Task))
                    {
                        ReferredToNames.Add(Game1.ConvertArchitectToDescription(this));
                    }
                }

                // Add formatted names for each body part
                foreach (Object o in BodyParts)
                {
                    o.ReferredToNames = new List<string>();

                    foreach (string s in ReferredToNames)
                    {
                        o.ReferredToNames.Add(s + "'s " + o.Type);
                        o.ReferredToNames.Add(s + "' " + o.Type);
                    }
                }


                //this is breaking a lot of stuff, i think most commands will work though without it.

                /*
                List<string> modifiedNames = new List<string>();

                foreach (string s in ReferredToNames)
                {
                    if (s != "" && s[s.Length - 1] == 's')
                    {
                        modifiedNames.Add(s + "'");
                    }
                    else
                    {
                        modifiedNames.Add(s + "'s");
                    }
                }

                ReferredToNames.AddRange(modifiedNames);
                */

                ReferredToNames.RemoveAll(s => string.IsNullOrEmpty(s));
            }
            else
            {
                foreach ((Architect, int) a in Game1.LoadedArchitects[Game1.ArchitectIndex].KnownArchitectsAndOpinions)
                {
                    if (a.Item1 == this)
                    {
                        PlayerKnowsArch = true;
                    }
                }

                if (PlayerKnowsArch)
                {
                    ReferredToNames.Add(Name + ", dead.");
                    ReferredToNames.Add("dead " + Profession);
                }
                else
                {
                    ReferredToNames.Add("dead " + Profession);
                }
            }

            foreach (Object o in Inventory)
            {
                o.UpdateNames();
            }
            if (LeftHandObject != null)
            {
                LeftHandObject.UpdateNames();
            }
            if (RightHandObject != null)
            {
                RightHandObject.UpdateNames();
            }
        }






































        public List<Attack> UpdateSelfActionsAndSuch()
        {
            //cycle hunger, health, etc.
            //update general information

          
            List<Attack> Attacks = new List<Attack>();

            if (Room == null && Block == null)
            {
                return Attacks;
            }

            if (Energy > MaxEnergy())
            {
                Energy = MaxEnergy();
            }

            if(CombatCycles == 0 && Energy < MaxEnergy())
            {
                Energy++;
            }

            void AnnounceToParty(string announcement, Color color)
            {
                if (Game1.LoadedArchitects[Game1.ArchitectIndex].Room == Room || Game1.LoadedArchitects[Game1.ArchitectIndex].Block == Block)
                {
                    Game1.MakeObservation(announcement, color);
                    if (Game1.LoadedArchitects[Game1.ArchitectIndex].Level < Level)
                    {
                        Game1.LoadedArchitects[Game1.ArchitectIndex].Level++;
                        Game1.LoadedArchitects[Game1.ArchitectIndex].SpendableLevels++;
                    }
                }
            }

            //distancing


            //first clear old ones
            List<(Architect, int)> DistancesToRemove = new List<(Architect, int)>();

            foreach ((Architect, int) a in Distances)
            {
                if(a.Item1.Room != Room || a.Item1.Block != Block)
                {
                    DistancesToRemove.Add(a);
                }
            }

            // Finally, remove the old distances that are not relevant anymore
            foreach ((Architect, int) a in DistancesToRemove)
            {
                Distances.Remove(a);
            }

            // Add new ones, or update based on existing ones
            List<Architect> ArchitectsToUse = (Room != null) ? Room.Architects : Block.Architects;

            foreach (Architect a in ArchitectsToUse)
            {
                // Check if the architect is already in the distances list
                if (!Distances.Any(d => d.Item1 == a))
                {
                    // Check if there is a reciprocal distance already determined by another architect
                    var existingDistance = Distances.FirstOrDefault(d => d.Item1 == a && ArchitectsToUse.Contains(d.Item1));
                    int distance;

                    if (existingDistance.Item1 != null)
                    {
                        // If a reciprocal distance exists, use the same distance
                        distance = existingDistance.Item2;
                    }
                    else
                    {
                        // Generate a random distance between 0 and 5
                        distance = Game1.r.Next(0, 6);
                    }

                    // Add the new architect and distance to the list
                    Distances.Add((a, distance));
                }
            }



            //reset imbuements

            ExtraShieldEffectiveness = 0;
            ExtraAttackPower = 0;
            ExtraDodgeChance = 0;
            ExtraRedirectionChance = 0;
            ExtraBashingResistance = 0;
            ExtraPiercingResistance = 0;
            ExtraSlashingResistance = 0;
            ExtraScourgingResistance = 0;
            ExtraStealth = 0;
            ExtraHealingCapability = 0;
            ExtraEnergyRegen = 0;

            //racial benefits

            if(Race == Game1.GameWorld.GetRace("nightfell"))
            {
                ExtraAttackPower += 10;
            }

            //path of shadow

            if(PathOfShadowLevel >= 2)
            {
                ExtraStealth += 10;
            }
            if(Invisible && PathOfShadowLevel < 8)
            {
                Energy -= 3;
            }

            //test hand items to see if they will burn you
            bool isSameRoomOrBlockMatch = Game1.GamePlayerParty.Architects.Any(architect => architect.Room == this.Room)
                              || Game1.GamePlayerParty.Architects.Any(architect => architect.Block == this.Block);

            

            //drop items that are too hot

            if(PathOfHeatLevel < 4)
            {
                if (LeftHandObject != null && (LeftHandObject.FireCycles > 0 || LeftHandObject.HeatInCelsius >= 30 + (Focus * 5)))
                {
                    if (isSameRoomOrBlockMatch)
                    {
                        Game1.MakeObservation("The " + LeftHandObject.ReferredToNames[0] + " in " + ReferredToNames[0] + "'s left hand is too hot! " + ReferredToNames[0] + " drops it in pain.", Color.Orange);
                    }
                    if (Room != null)
                    {
                        Room.Objects.Add(LeftHandObject);
                        LeftHandObject = null;
                    }
                    else
                    {
                        Block.Objects.Add(LeftHandObject);
                        LeftHandObject = null;
                    }

                }
                if (RightHandObject != null && (RightHandObject.FireCycles > 0 || RightHandObject.HeatInCelsius >= 30 + (Focus * 5)))
                {
                    if (isSameRoomOrBlockMatch)
                    {
                        Game1.MakeObservation("The " + RightHandObject.ReferredToNames[0] + " in " + ReferredToNames[0] + "'s right hand is too hot! " + ReferredToNames[0] + " drops it in pain.", Color.Orange);
                    }
                    if (Room != null)
                    {
                        Room.Objects.Add(RightHandObject); 
                        RightHandObject = null;
                    }
                    else
                    {
                        Block.Objects.Add(RightHandObject);
                        RightHandObject = null;
                    }
                }

                foreach (Object clothingItem in Clothing) 
                {
                    if (clothingItem.FireCycles > 0 || clothingItem.HeatInCelsius >= 30 + (Focus * 5))
                    {
                        Energy -= 1;

                        if (isSameRoomOrBlockMatch) 
                        {
                            Game1.MakeObservation("The " + clothingItem.ReferredToNames[0] + " on " + ReferredToNames[0] + " is too hot, and is singing their skin!", Color.Orange);
                        }
                    }
                }
            }


            double GetMaterialCoverageMultiplier(Material material)
            {
                double baseMultiplier = 1.0; // Default multiplier
                switch (material.Type)
                {
                    case "wood":
                        baseMultiplier = 1.1;
                        break;
                    case "cloth":
                        baseMultiplier = 1.0; // Assuming cloth is the baseline
                        break;
                    case "stone":
                        baseMultiplier = 1.3;
                        break;
                    case "metal":
                        baseMultiplier = 1.5;
                        break;
                    case "glass":
                        baseMultiplier = 0.8; // Glass might be fragile
                        break;
                    default:
                        break;
                }
                return baseMultiplier * (1 + (0.1 * material.Toughness)); // Example formula
            }

            // Assuming each BodyPart object has a CoverageName property to store the name of the clothing item providing the most coverage

            // Modify the system to also track the clothing item that provides the highest coverage for each body part
            Dictionary<string, (int coverage, string itemName)> highestCoverageByItem = new Dictionary<string, (int coverage, string itemName)>();

            foreach (var clothingItem in this.Clothing)
            {
                // Determine the strongest material for this clothing item
                Material strongestMaterial = clothingItem.Materials.OrderByDescending(m => m.Toughness).First();

                // Calculate coverage values taking material into account
                foreach (var (bodyPart, coverage) in clothingItem.CoverageValues)
                {
                    double materialMultiplier = GetMaterialCoverageMultiplier(strongestMaterial);
                    int adjustedCoverage = (int)(coverage * materialMultiplier);

                    if (highestCoverageByItem.ContainsKey(bodyPart))
                    {
                        if (highestCoverageByItem[bodyPart].coverage < adjustedCoverage)
                        {
                            highestCoverageByItem[bodyPart] = (adjustedCoverage, clothingItem.ReferredToNames[0]); // Update with higher coverage and item name
                        }
                    }
                    else
                    {
                        highestCoverageByItem.Add(bodyPart, (adjustedCoverage, clothingItem.ReferredToNames[0])); // Add new entry with coverage and item name
                    }
                }
            }

            foreach (var bodyPart in this.BodyParts)
            {
                if (highestCoverageByItem.TryGetValue(bodyPart.Type, out var highestCoverage))
                {
                    bodyPart.Coverage = highestCoverage.coverage;
                    bodyPart.CoverageName = highestCoverage.itemName; // Assign the name of the item providing the highest coverage
                }
            }

            if (CombatCycles > 0)
            {
                CombatCycles--;  // Regular decrement for being in combat

                bool closeEnemies = false;  // Flag to check for close enemies

                foreach (Architect a in ArchitectsToUse)
                {
                    // Check if the Architect is targeting this and within a distance of 5
                    if (a.TargetArchitect == this && (a.Task == "killtarget" || a.Task == "disabletarget") && a.GetDistance(this) <= 5)
                    {
                        closeEnemies = true;
                        break;
                    }
                }

                // If there are no close enemies, decrement CombatCycles again
                if (!closeEnemies && CombatCycles > 0)
                {
                    CombatCycles--;
                }
            }


            CurrentlyActiveImbuements = new List<Imbuement>();

            foreach (Object o in Clothing)
            {
                CurrentlyActiveImbuements.AddRange(o.Imbuements);
            }

            foreach(Object o in Inventory)
            {
                if(!o.IsWearable)
                {
                    CurrentlyActiveImbuements.AddRange(o.Imbuements);
                }
            }

            if (LeftHandObject != null)
            {
                CurrentlyActiveImbuements.AddRange(LeftHandObject.Imbuements);
            }
            if (RightHandObject != null)
            {
                CurrentlyActiveImbuements.AddRange(RightHandObject.Imbuements);
            }

            foreach(Architect a in MeldedShibas)
            {
                int attributeToIncrease = Game1.r.Next(0, 9); 
                switch (attributeToIncrease)
                {
                    case 0:
                        ExtraShieldEffectiveness += 2;
                        break;
                    case 1:
                        ExtraAttackPower += 2;
                        break;
                    case 2:
                        ExtraDodgeChance += 2;
                        break;
                    case 3:
                        ExtraRedirectionChance += 2;
                        break;
                    case 4:
                        ExtraBashingResistance += 2;
                        break;
                    case 5:
                        ExtraPiercingResistance += 2;
                        break;
                    case 6:
                        ExtraSlashingResistance += 2;
                        break;
                    case 7:
                        ExtraScourgingResistance += 2;
                        break;
                    case 8:
                        ExtraStealth += 2;
                        break;
                    default:
                        // Just in case of an unexpected value
                        break;
                }
            }



            //go ahead and apply the imbuement effect if its a passive imbuement, but if it isnt then wait for later
            foreach (Imbuement i in CurrentlyActiveImbuements)
            {
                if (!i.IsTrigger)
                {
                    bool MetCondition = false;

                    if (i.ConditionOrTrigger == "multipleenemies")
                    {
                        int Quota = 0;
                        if (Room != null)
                        {
                            foreach (Architect e in Room.Architects)
                            {
                                if (e.Task == "fighting")
                                {
                                    Quota++;
                                }
                            }
                        }
                        else
                        {
                            foreach (Architect e in Block.Architects)
                            {
                                if (e.Task == "fighting")
                                {
                                    Quota++;
                                }
                            }
                        }

                        if (Quota > 1)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "grounded")
                    {
                        if (OnGround)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "diminished")
                    {
                        if (Energy < i.FirstPower)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "lowlight")
                    {
                        if (Room != null)
                        {
                            if (Structure.LightLevelOf5 < 3)
                            {
                                MetCondition = true;
                            }
                        }
                        else
                        {
                            if (Location.Region.World.IsNightTime())
                            {
                                MetCondition = true;
                            }
                        }
                    }
                    else if (i.ConditionOrTrigger == "stagnant")
                    {
                        if (CyclesSinceMoved > 200)
                        {
                            MetCondition = true;
                        }
                    }
                    else if (i.ConditionOrTrigger == "maxenergy")
                    {
                        if (Energy == MaxEnergy())
                        {
                            MetCondition = true;
                        }
                    }

                    if (MetCondition)
                    {
                        switch (i.BuffOrResult)
                        {
                            case "+attack":
                                ExtraAttackPower += i.FirstPower; // Assuming FirstPower is the percentage increase
                                break;
                            case "+shield":
                                ExtraShieldEffectiveness += i.FirstPower;
                                break;
                            case "+dodge":
                                ExtraDodgeChance += i.FirstPower;
                                break;
                            case "+redirection":
                                ExtraRedirectionChance += i.FirstPower;
                                break;
                            case "+bash":
                                ExtraBashingResistance += i.FirstPower;
                                break;
                            case "+pierce":
                                ExtraPiercingResistance += i.FirstPower; // Assuming you have a field for this
                                break;
                            case "+slash":
                                ExtraSlashingResistance += i.FirstPower;
                                break;
                            case "+scourge":
                                ExtraScourgingResistance += i.FirstPower;
                                break;
                            case "+stealth":
                                ExtraStealth += i.FirstPower; // Or some other logic for stealth
                                break;
                            case "+heal":
                                ExtraHealingCapability += i.FirstPower;
                                break;
                            case "+regen":
                                ExtraEnergyRegen += i.FirstPower; // Assuming SecondPower is the flat regen amount
                                break;
                            default:
                                // Handle unknown BuffOrResult
                                break;
                        }
                    }
                }
            }


            //regenerate


            Energy = Math.Min(MaxEnergy(), Energy + ExtraEnergyRegen);

            //Handle A variety of On Tick Effects

            if (WetCycles > 0)
            {
                FireCycles = 0;
                WetCycles--;
            }
            if (FireCycles > 0)
            {
                if(PathOfHeatLevel < 8)
                {
                    Energy -= (0.3 * FireCycles);

                    FireCycles--;
                }
            }
            if (BlindCycles > 0)
            {
                BlindCycles--;
            }
            if (DestabilizedCycles > 0)
            {
                DestabilizedCycles--;
            }
            if (ConcussionCycles > 0)
            {
                ConcussionCycles--;
            }

            if (YLevelInFeet > 0)
            {
                // Adjusting the velocity based on gravity
                YVelocity += (int)(Math.Round(Math.Min(0, (YLevelInFeet - 300) * 0.02f))); 

                YLevelInFeet = Math.Min(0, YLevelInFeet - 300);

                if (YLevelInFeet == 0)
                {
                    //impact
                    int DamageInstances = (int)(YVelocity / (0.8));
                    YVelocity = 0;

                    for (int i = 0; i < DamageInstances; i++)
                    {
                        Object o = this.BodyParts[Game1.r.Next(BodyParts.Count)];

                        Game1.MakeObservation(o.ReferredToNames[0] + " is crushed by the impact!", Color.Orange);
                        o.Integrity = Math.Max(0, o.Integrity - 25); // Ensure integrity doesn't go below zero
                    }
                }
            }



            //have an opinion with everyone in the room

            IEnumerable<Architect> architects = (Room == null) ? Block.Architects : Room.Architects;





            //living statuses








            //actions

            if (!Game1.GamePlayerParty.Architects.Contains(this) && ConcussionCycles == 0 && SpellHoldCycles == 0 && CooldownCycles == 0 && Race != Game1.GameWorld.GetRace("moari"))
            {
                foreach (Architect a in architects)
                {
                    if (GetOpinion(a) == 0 && a != this)
                    {
                        int FinalOpinion = 0;

                        if (OppositionTags.Contains("humanoids") && Location.Region.World.HumanoidRaces.Contains(a.Race) ||
                            OppositionTags.Contains("alllife") && a.Race.Name.Contains("shade") ||
                            OppositionTags.Contains("allsentient") ||
                            OppositionTags.Contains("allunalike") && a.Race != Race ||
                            OppositionTags.Contains("allevil") && (a.Race.Name.Contains("shade") || (a.Group != null && a.Group.Reputation < -50) || a.Reputation < -50) ||
                            OppositionTags.Contains("intruders") && a.HomeLocation != Location)
                        {
                            FinalOpinion = Math.Min(FinalOpinion, -247);
                        }
                        else
                        {
                            FinalOpinion = Math.Min(FinalOpinion, 1);
                        }

                        ChangeOpinion(a, FinalOpinion);

                    }

                    if (OppositionTags.Contains("indebted"))
                    {
                        if (HomeStructure.MarketDebt <= -1 && Game1.GamePlayerParty.Architects.Contains(a) && a.Structure == null)
                        {
                            ChangeOpinion(a, -247);
                        }
                    }
                }


                //first see if you want to kill someone lmao
                Architect KillTarget = null;
                Architect DisableTarget = null;

                //stop tryin to kill iftheyre allready dead lmao

                if ((Task == "killtarget" || Task == "disabletarget") && TargetArchitect.IsAlive == false)
                {
                    Task = "";
                    TargetArchitect = null;
                }

                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if (GetOpinion(a) < -100)
                        {
                            KillTarget = a;
                        }
                        else if (GetOpinion(a) < -50)
                        {
                            DisableTarget = a;

                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if (GetOpinion(a) < -100)
                        {
                            KillTarget = a;
                        }
                        else if (GetOpinion(a) < -50)
                        {
                            DisableTarget = a;
                        }
                    }
                }

                int ChangeInX = 0;
                int ChangeInZ = 0;
                string AlternateMove = "";

                if (KillTarget != null)
                {
                    Task = "killtarget";
                    CyclesLeftInTask = 500;
                    TargetArchitect = KillTarget;
                }
                else if (DisableTarget != null)
                {
                    Task = "disabletarget";
                    CyclesLeftInTask = 500;
                    TargetArchitect = DisableTarget;
                }

                if (Task == "killtarget" && KillTarget != null)
                {
                    //update the targetting system so you go to where they are naturally
                    Target = (TargetArchitect.Location.Region, TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Structure, "");
                }
                if (Task == "disabletarget" && DisableTarget != null)
                {
                    Target = (TargetArchitect.Location.Region, TargetArchitect.Location, TargetArchitect.District, TargetArchitect.Block, TargetArchitect.Structure, "");
                }



                if (Group != null && Group.Leader.Loaded)
                {

                }
                else if (Task == "" && BlindCycles == 0 && Profession != "warlock" && Profession != "sorcerer" && Race != Game1.GameWorld.GetRace("debtshiba") /*cant make judgements if ur blind lol, and cant if you already have a basic job.*/)
                {
                    if (IsLoadedTrader && DaysSinceLiquid < 2 && DaysSinceFood < 2)
                    {
                        Task = "vacanttfortrade";
                        CyclesLeftInTask = 500;
                        Target = Block.FindNearestThing("market");
                    }
                    else
                    {
                        if (DaysSinceLiquid > 0 && Task != "drinking")
                        {
                            Task = "drinking";
                            CyclesLeftInTask = 500;

                            if (Game1.r.Next(1, 3) == 1)
                            {
                                Target = Block.FindNearestThing("well");
                            }
                            else
                            {
                                Target = Block.FindNearestThing("tavern");
                            }
                        }
                        if (DaysSinceCoffeeOrTea > 0 && Task != "drinkingcaffeine")
                        {
                            Task = "drinkingcaffeine";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if (DaysSinceFood > 0 && Task != "eating")
                        {
                            Task = "eating";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if (NightsSinceSleep > 0 && Task != "sleeping")
                        {
                            Task = "sleeping";
                            CyclesLeftInTask = 350000;
                            Target = Block.FindNearestThing("house");
                        }
                        else if (Group == Location.Government || this == Location.Government || this == Location.HomeCivilization.Alpha)
                        {
                            Task = "discussion";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("prism");
                        }
                        else if (Profession == "scholar")
                        {
                            Task = "study";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("library");
                        }
                        else if (DaysSinceSocialized > 0 && Task != "socializing")
                        {
                            Task = "socializing";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("tavern");
                        }
                        else if ((DaysSincePerforming > 5 || (Profession == "musician" && DaysSincePerforming > 0)) && Task != "performmusic" && Task != "performdance" && Task != "performtheater" && Task != "performpoetry")
                        {
                            Task = new List<string>() { "performmusic", "performdance", "performtheater", "performpoetry" }[Game1.r.Next(3)];
                            CyclesLeftInTask = 500;
                            if (Game1.r.Next(1, 3) == 1)
                            {
                                Target = Block.FindNearestThing("tavern");
                            }
                            else
                            {
                                Target = Block.FindNearestThing("well");
                            }
                        }
                        else if (Game1.r.Next(1, 3) == 1)
                        {
                            Task = "industry";
                            CyclesLeftInTask = 500;
                            Target = Block.FindNearestThing("house");
                        }
                        else if (Block.FindNearestThing("tavern").Item2 != Location) //pls dont go to a random tavern if you live at something dumb like a spire
                        {
                            //pick random task for the fun of it
                            int TaskDecider = Game1.r.Next(0, 30);

                            if (TaskDecider == 0)
                            {
                                Task = new List<string>() { "performmusic", "performdance", "performtheater", "performpoetry" }[Game1.r.Next(3)];
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 7)
                            {
                                Task = "socializing";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 12)
                            {
                                Task = "drinking";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 15)
                            {
                                Task = "drinkingcaffeine";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else if (TaskDecider < 25)
                            {
                                Task = "eating";
                                CyclesLeftInTask = 500;
                                Target = Block.FindNearestThing("tavern");
                            }
                            else
                            {
                                Task = "contemplate";
                                Target = Block.FindNearestThing("well");
                                Target = (Target.Item1, Target.Item2, Target.Item3, Target.Item3.DistrictMap[Game1.r.Next(0, 49)], null, Target.Item6);
                                CyclesLeftInTask = 500;
                            }
                        }
                    }
                }

                double angleDegrees = 99991;

                if (CyclesLeftInTask <= 0 && (Location.Region, Location, District, Block, Structure, "") == Target)
                {
                    //CONGRAGULATIONS! youre done with your task lool, you can reap the benefits.

                    //tasks not listed here don't have nebefits
                    switch (Task)
                    {
                        case "drinking":
                            DaysSinceLiquid = 0;
                            break;
                        case "drinkingcaffeine":
                            DaysSinceCoffeeOrTea = 0;
                            DaysSinceLiquid = 0;
                            break;
                        case "sleeping":
                            NightsSinceSleep = 0;
                            break;
                        case "discussion":
                            DaysSinceSocialized = 0;
                            break;
                        case "socializing":
                            DaysSinceSocialized = 0;
                            break;
                        case "performmusic":
                            DaysSincePerforming = 0;
                            break;
                        case "performpoetry":
                            DaysSincePerforming = 0;
                            break;
                        case "performdance":
                            DaysSincePerforming = 0;
                            break;
                        case "performtheater":
                            DaysSincePerforming = 0;
                            break;
                        case "cook":
                            //add cooking stuff <3
                            break;
                        case "industry":
                            if (Room != null)
                            {
                                Room.Objects.AddRange(District.GenerateItems(District.Industry, 1));
                            }
                            else
                            {
                                Block.Objects.AddRange(District.GenerateItems(District.Industry, 1));
                            }
                            break;
                        default:
                            break;
                    }

                    Task = "";
                }
                else if (Task != "" && (Location.Region, Location, District, Block, Structure, "") == Target)
                {
                    string DetermineAttackVerb(string weaponType)
                    {

                        var verbMappings = new Dictionary<string, List<string>>
                        {
                            {"slashing", new List<string> {"slash", "slice"}},
                            {"piercing", new List<string> {"pierce", "stab"}},
                            {"bashing", new List<string> {"bash", "crush"}},
                            {"scourging", new List<string> {"scourge", "lash", "strike"}}
                        };

                        if (verbMappings.ContainsKey(weaponType))
                        {
                            var verbs = verbMappings[weaponType];
                            return verbs[Game1.r.Next(verbs.Count)]; // Randomly select a verb
                        }

                        return "attack"; // Default verb if weapon type is not found
                    }

                    if (Task == "killtarget" || Task == "disabletarget")
                    {
                        bool CastedASpell = false;
                        int SpellCastingDistance = 3; 

                        // Now, you can use ArchitectsToUse to check for proximity, interactions, etc.

                        if (ArchitectsToUse.Contains(TargetArchitect)) // Check if the target is in the same area
                        {
                            List<string> OffensiveSpells = new List<string> { "Expel", "Water Bolt", "Chaos Flare", "Concentrated Ignition", "Tremor" };

                            if ((SpellsKnown.Count > 0 && Game1.r.Next(2) == 0) || Profession == "sorcerer" || Profession == "warlock" || Race == Game1.GameWorld.GetRace("debtshiba"))
                            {
                                List<string> offensiveSpellsInKit;

                                if (Race == Game1.GameWorld.GetRace("debtshiba"))
                                {
                                    offensiveSpellsInKit = Game1.AllSpells.Where(spell => OffensiveSpells.Contains(spell)).ToList();
                                }
                                else
                                {
                                    offensiveSpellsInKit = SpellsKnown.Where(spell => OffensiveSpells.Contains(spell)).ToList();
                                }

                                if (offensiveSpellsInKit.Any() && GetDistance(TargetArchitect) <= SpellCastingDistance)
                                {
                                    int Spells = 1;

                                    if (Race == Game1.GameWorld.GetRace("debtshiba"))
                                    {
                                        Spells = 50;
                                    }
                                    else if (Profession == "sorcerer" || Profession == "warlock")
                                    {
                                        Spells = Game1.r.Next(3, 8);
                                    }

                                    for (int i = Spells; i != 0; i--)
                                    {
                                        string spellToCast = offensiveSpellsInKit[Game1.r.Next(offensiveSpellsInKit.Count)];
                                        CastedASpell = true;
                                        Game1.Announcements.AddRange(CastSpell(spellToCast, new List<Entity>() { TargetArchitect }));
                                    }
                                }
                            }

                            if (!CastedASpell) // If no spell was cast, try a melee attack
                            {
                                Object Weapon;
                                Object mainHand = RightHanded ? MainHandObject() : OffHandObject();
                                Object offHand = RightHanded ? OffHandObject() : MainHandObject();

                                if (mainHand != null && mainHand.IsWeapon)
                                {
                                    Weapon = mainHand;
                                }
                                else if (offHand != null && offHand.IsWeapon)
                                {
                                    Weapon = offHand;
                                }
                                else
                                {
                                    Weapon = BodyParts[Game1.r.Next(BodyParts.Count)]; // Assuming unarmed combat uses body parts as weapons
                                }

                                if (Weapon.WeaponMaximumRange >= GetDistance(TargetArchitect))
                                {
                                    string attackVerb = DetermineAttackVerb(Weapon.DamageType);
                                    Attacks.Add(new Attack(attackVerb, this, TargetArchitect.Name, Weapon));
                                }
                                else // If neither spell nor weapon attack is possible, approach the target
                                {
                                    DistanceFromArchitect(TargetArchitect, -2); // Decrease distance by 2
                                    CooldownCycles += (int)(4 * Math.Round(Speed()));
                                }
                            }
                        }
                    }

                    CyclesLeftInTask--;
                }

                // Method to determine the attack verb based on weapon type


                else if (Task != "" && Target != (Location.Region, Location, District, Block, Structure, "") && Target != (null, null, null, null, null, null) && BlindCycles == 0 /*cant make judgements if ur blind lol*/)
                {
                    //you can't complete your task because you arent at your target yet.

                    if (Target == (null, null, null, null, null, ""))
                    {
                        //just do nothing so we can update before you freak out
                    }
                    else if (Location.Region != Target.Item1 || Location != Target.Item2 || District != Target.Item3)
                    {
                        //your location or district is incorrect, and you have to travle to a new one.

                        string Edge = "";

                        if (Block.X > Block.Z)
                        {
                            //either north or east
                            if (Block.X + Block.Z > 7)
                            {
                                Edge = "east";
                            }
                            else
                            {
                                Edge = "north";
                            }
                        }
                        else
                        {
                            if (Block.X + Block.Z > 7)
                            {
                                Edge = "south";
                            }
                            else
                            {
                                Edge = "west";
                            }
                        }

                        if (Edge == "west")
                        {
                            ChangeInX = -1;
                        }
                        else if (Edge == "east")
                        {
                            ChangeInX = 1;
                        }
                        else if (Edge == "north")
                        {
                            ChangeInZ = -1;
                        }
                        else if (Edge == "south")
                        {
                            ChangeInZ = 1;
                        }
                    }
                    else if (Block != null && Structure != null && Target.Item5 == null)
                    {
                        //leave the structure
                        AlternateMove = "leavebuilding";
                    }
                    else if (Block != null && Target.Item4 != null && Block != Target.Item4)
                    {
                        // Calculate differences in X and Z, and the normalized angle in degrees

                        int deltaX = Target.Item4.X - Block.X;
                        int deltaZ = Target.Item4.Z - Block.Z;

                        // Calculate angle in radians
                        double angleRadians = Math.Atan2(deltaX, -deltaZ);

                        // Convert angle to degrees
                        angleDegrees = angleRadians * (180.0 / Math.PI);

                        if (angleDegrees < 0)
                        {
                            angleDegrees += 360;
                        }


                        // Determine direction based on the angle
                        if (angleDegrees >= 337.5 || angleDegrees < 22.5)
                        {
                            // North
                            ChangeInX = 0;
                            ChangeInZ = -1;
                        }
                        else if (angleDegrees >= 22.5 && angleDegrees < 67.5)
                        {
                            // Northeast
                            ChangeInX = 1;
                            ChangeInZ = -1;
                        }
                        else if (angleDegrees >= 67.5 && angleDegrees < 112.5)
                        {
                            // East
                            ChangeInX = 1;
                            ChangeInZ = 0;
                        }
                        else if (angleDegrees >= 112.5 && angleDegrees < 157.5)
                        {
                            // Southeast
                            ChangeInX = 1;
                            ChangeInZ = 1;
                        }
                        else if (angleDegrees >= 157.5 && angleDegrees < 202.5)
                        {
                            // South
                            ChangeInX = 0;
                            ChangeInZ = 1;
                        }
                        else if (angleDegrees >= 202.5 && angleDegrees < 247.5)
                        {
                            // Southwest
                            ChangeInX = -1;
                            ChangeInZ = 1;
                        }
                        else if (angleDegrees >= 247.5 && angleDegrees < 292.5)
                        {
                            // West
                            ChangeInX = -1;
                            ChangeInZ = 0;
                        }
                        else // angleDegrees >= 292.5 && angleDegrees < 337.5
                        {
                            // Northwest
                            ChangeInX = -1;
                            ChangeInZ = -1;
                        }
                        // The program will handle the rest of the movement
                        // ...
                    }
                    else if (Block != null && Structure == null && Target.Item5 != null)
                    {
                        //by this point, were in the right block. we just need to enter the structure.
                        AlternateMove = Target.Item5.Name;
                    }
                    else
                    {
                        //you left the district? Get out?
                        DistrictPoints++;
                    }
                }


                Dictionary<(int, int), string> CoordsToDirection = new Dictionary<(int, int), string>()
                {
                    {(1, 1), "southeast"},
                    {(1, 0), "east"},
                    {(1, -1), "northeast"},
                    {(0, 1), "south"},
                    {(0, -1), "north"},
                    {(-1, 1), "southwest"},
                    {(-1, 0), "west"},
                    {(-1, -1), "northwest"}
                };


                if (ChangeInX != 0 || ChangeInZ != 0)
                {
                    if (CurrentlyMovingPlace == CoordsToDirection[(ChangeInX, ChangeInZ)])
                    {
                        // This check is now streamlined to occur only once.
                        if (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance())
                        {
                            int NewX = Block.X + ChangeInX;
                            int NewZ = Block.Z + ChangeInZ;

                            if (NewX < 0 || NewX > 6 || NewZ < 0 || NewZ > 6)
                            {
                                if ((Target.Item2 != null && Target.Item2 != Location) || (Target.Item3 != null && Target.Item3 != District))
                                {
                                    TriggeredLock = true;
                                    Block.Architects.Remove(this);
                                    Target.Item3.Architects.Add(this);
                                    Block = null;
                                    Location = Target.Item2;
                                    District = Target.Item3;

                                    if (Game1.LoadedArchitects.Contains(this))
                                    {
                                        Game1.LoadedArchitects.Remove(this);
                                    }

                                    // Assuming Attacks is a collection that's being returned
                                    return Attacks;
                                }
                                else
                                {
                                    throw new Exception("Attempting to leave the district without a specified target.");
                                }
                            }
                            else
                            {
                                Block.Architects.Remove(this);
                                CooldownCycles += (int)(Math.Round(35 * Speed()));
                                Block = District.DistrictMap[NewX + NewZ * 7];
                                Block.Architects.Add(this);
                            }

                            CurrentlyMovingPlace = "";
                        }
                        else
                        {
                            // Handle escape failure

                            CooldownCycles += (int)(Math.Round(35 * Speed()));
                            AnnounceToParty(ReferredToNames[0] + " failed to escape!", Color.LimeGreen);
                        }
                    }
                    else
                    {
                        // Setting up for a new move attempt in the next cycle or action.
                        if (CombatCycles == 0 || Game1.r.Next(100) <= EscapeChance())
                        {
                            CurrentlyMovingPlace = CoordsToDirection[(ChangeInX, ChangeInZ)];
                            CooldownCycles += (int)(Math.Round(35 * Speed()));
                            // Optionally, make an observation if it's not the first move attempt.
                            if (CombatCycles != 0)
                            {
                                AnnounceToParty(ReferredToNames[0] + " is preparing to move...", Color.Red);
                            }
                        }
                        else
                        {
                            CooldownCycles += (int)(Math.Round(35 * Speed()));
                            AnnounceToParty(ReferredToNames[0] + " failed to escape!", Color.Red);
                        }
                    }
                }
                else if (AlternateMove != "")
                {
                    if (Target.Item5 != null && Target.Item5.Name == AlternateMove)
                    {
                        Block.Architects.Remove(this);
                        Structure = Target.Item5;
                        CooldownCycles += (int)(Math.Round(35 * Speed()));
                        Room = Target.Item5.Rooms[0];
                        Target.Item5.Rooms[0].Architects.Add(this);
                    }
                }
            }
            else if (CooldownCycles > 0)
            {
                CooldownCycles--;
            }



            if (Bleeding > 0)
            {
                BleedingCycle += 1;

                if(Bleeding == 5)
                {
                    BleedingCycle = 0;
                    Energy -= Bleeding;
                    Bleeding -= 1;
                }
            }

            if (BlockLastCycle != Block)
            {
                CyclesSinceMoved = 0;
            }
            else
            {
                CyclesSinceMoved++;
            }

            BlockLastCycle = Block;


            //lose track of invisible targets

            if (TargetArchitect != null && TargetArchitect.Invisible)
            {
                if(TargetArchitect.PathOfShadowLevel >= 6 || (TargetArchitect.Inventory.Count == 0 && TargetArchitect.Clothing.Count == 0))
                {
                    TargetArchitect = null;
                    Task = "";
                }
            }

            return Attacks;
        }



























































        public Architect()
        {
            //for serialization
        }

        public List<TextStorage> CastSpell(string Spell, List<Entity> Targets) //the list of strings are the announcements
        {
            List<TextStorage> Announcements = new List<TextStorage>();

            string casterName = this.ReferredToNames[0];

            foreach(Entity e in Targets)
            {
                if(e is Architect)
                {
                    ((Architect)e).CombatCycles = 250;
                    CombatCycles = 250;
                }
            }


            List<Entity> TargetsToPurge = new List<Entity>();
            foreach(Entity e in Targets)
            {
                if(GetDistance(e) >= 4)
                {
                    Announcements.Add(new TextStorage($"{e.ReferredToNames[0]} is outside of casting range.", Color.Yellow));
                    TargetsToPurge.Add(e);
                }
            }
            foreach(Entity e in TargetsToPurge)
            {
                Targets.Remove(e);
            }



            foreach (Entity CurrentTarget in Targets)
            {
                if (Spell == "water bolt")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());

                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} curves their hand inwards, accumulating vapor. They hurl the concentrated sphere...", Color.Purple));
                            Announcements.Add(new TextStorage($"It crashes into {CurrentTarget.ReferredToNames[0]}, splashing into them!", Color.Purple));
                            break;
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        if (((Object)CurrentTarget).FireCycles > 0)
                        {
                            ((Object)CurrentTarget).FireCycles = 0;
                        }
                        ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 2;
                    }
                    else
                    {
                        if (((Architect)CurrentTarget).FireCycles > 0)
                        {
                            ((Architect)CurrentTarget).FireCycles = 0;
                        }
                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            o.Integrity = o.Integrity - 2;
                        }
                    }
                }
                else if (Spell == "chaos flare")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());

                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} makes a fist and jerks their arm inwards, conjuring two spheres of light and dark rotating it. They throw them...", Color.Purple));
                            Announcements.Add(new TextStorage($"They crash into {CurrentTarget.ReferredToNames[0]}, and react explosively!", Color.Purple));
                            break;
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).FireCycles += Game1.r.Next(0, 100);
                        ((Object)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 50);
                        ((Object)CurrentTarget).Integrity = ((Object)CurrentTarget).Integrity - 50;
                    }
                    else
                    {
                        ((Architect)CurrentTarget).FireCycles += Game1.r.Next(0, 50);
                        ((Architect)CurrentTarget).DestabilizedCycles += Game1.r.Next(0, 50);
                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            if (Game1.r.Next(0, 2) == 0)
                            {
                                o.Integrity = o.Integrity - 25;
                            }
                        }
                    }
                }
                else if (Spell == "concentrated ignition")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());
                    foreach (Architect a in Game1.GamePlayerParty.Architects)
                    {
                        if (a.Room == this.Room && a.Block == this.Block)
                        {
                            Announcements.Add(new TextStorage($"{casterName} holds their palms one over the other facing each other, and gathers heat energy...", Color.Purple));
                            Announcements.Add(new TextStorage($"It quickly dissipates, reassembling itself at {CurrentTarget.ReferredToNames[0]}!", Color.Purple));
                        }
                    }

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).FireCycles += Game1.r.Next(20, 50);
                    }
                    else
                    {
                        ((Architect)CurrentTarget).FireCycles += Game1.r.Next(50, 100);
                    }
                }
                else if (Spell == "tremor")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());

                    Announcements.Add(new TextStorage($"{casterName} holds out their hands palms down and shoves into the ground...", Color.Purple));
                    Announcements.Add(new TextStorage($"A massive tremor shakes the ground, but {CurrentTarget.ReferredToNames[0]} is unshaken!", Color.Purple));

                    foreach (Object o in Block.Objects)
                    {
                        if (o != CurrentTarget)
                        {
                            o.DestabilizedCycles = Game1.r.Next(100, 200);
                        }
                    }
                    foreach (Architect a in Block.Architects)
                    {
                        if (a != CurrentTarget)
                        {
                            a.DestabilizedCycles = Game1.r.Next(100, 200);
                        }
                    }
                }
                else if (Spell == "immobile illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple));
                }
                else if (Spell == "shadow veil")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple));
                }
                else if (Spell == "mobile illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple));
                }
                else if (Spell == "reactive illusion")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple));
                }
                else if (Spell == "truthfulness")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage("You have only decieved yourself.", Color.Purple));
                }
                else if (Spell == "rise")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} gestures their hand towards the sky...", Color.Purple));
                    Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " flies into the air!", Color.Purple));

                    if (CurrentTarget is Object)
                    {
                        ((Object)CurrentTarget).YLevelInFeet += Game1.r.Next(30, 50);
                    }
                    else
                    {
                        ((Architect)CurrentTarget).YLevelInFeet += Game1.r.Next(30, 50);
                    }
                }
                else if (Spell == "hold")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} clenches their fist violently...", Color.Purple));
                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " stagnates.", Color.Purple));
                        ((Object)CurrentTarget).AirborneTarget = null;
                        ((Object)CurrentTarget).AirborneCyclesToHitTarget = 0;
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " freezes in time!", Color.Purple));
                        ((Architect)CurrentTarget).SpellHoldCycles = 8;
                    }
                }
                else if (Spell == "forcethrow")
                {
                    CooldownCycles += (int)Math.Round(15 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} clenches their fist at " + CurrentTarget.ReferredToNames[0] + ", gathering material. They thrust it at " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple));

                    Entity MainTarget = CurrentTarget;
                    Targets.RemoveAt(0);

                    foreach (Entity o in Targets)
                    {
                        if (o == MainTarget)
                        {

                        }
                        else
                        {
                            if (o is Object)
                            {
                                Announcements.Add(new TextStorage(o.ReferredToNames[0] + " flies at " + CurrentTarget.ReferredToNames[0] + "!", Color.Purple));
                                ((Object)o).AirborneTarget = MainTarget;
                                ((Object)CurrentTarget).AirborneCyclesToHitTarget = 15 - Focus;
                            }
                            else
                            {
                                Announcements.Add(new TextStorage(o.ReferredToNames[0] + " stumbles, but isnt yielding to your force!", Color.Purple));
                                ((Architect)o).DestabilizedCycles += 50;
                            }
                        }
                    }
                }
                else if (Spell == "shatter")
                {
                    CooldownCycles += (int)Math.Round(30 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} brings his arms inward and swings them outward violently...", Color.Purple));

                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissipates across the area!", Color.Purple));
                        Block.Objects.Remove((Object)CurrentTarget);
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " pulls himself together violently...", Color.Purple));
                        ((Architect)CurrentTarget).DestabilizedCycles += 100;
                    }
                }
                else if (Spell == "intercept")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} reaches their hand towards " + CurrentTarget.ReferredToNames[0] + " and grasps...", Color.Purple));

                    if (CurrentTarget is Object && ((Object)CurrentTarget).AirborneTarget != null)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget + " dissapears in a web of fractals!", Color.Purple));
                        ((Object)CurrentTarget).Fractallize(9999999);
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("Nothing seems to happen...?", Color.Purple));
                    }
                }
                else if (Spell == "expel")
                {
                    CooldownCycles += (int)Math.Round(20 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} reaches their hand towards " + CurrentTarget.ReferredToNames[0] + " and grasps...", Color.Purple));

                    if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissapears in a web of fractals!", Color.Purple));
                        ((Object)CurrentTarget).Fractallize(9999999);
                    }
                    else if (CurrentTarget is Architect)
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " dissapears in a web of fractals!", Color.Purple));
                        ((Architect)CurrentTarget).Fractallize(9999999);
                    }
                    else
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " is enveloped in fractals, but does not fade.", Color.Purple));
                    }
                }
                else if (Spell == "extract")
                {
                    CooldownCycles += (int)Math.Round(20 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple));

                    if (CurrentTarget is Object && Game1.GameWorld.FractalObjects.Contains((Object)CurrentTarget))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " reappears in a web of fractals!", Color.Purple));
                        ((Object)CurrentTarget).RematerializeLocation = (Location.Region, Location, District, Block, Structure, Room);
                        ((Object)CurrentTarget).FractalCycles = 0;

                    }
                    else if (CurrentTarget is Architect && Game1.GameWorld.FractalArchitects.Contains((Architect)CurrentTarget))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " reappears in a web of fractals!", Color.Purple));
                        ((Architect)CurrentTarget).RematerializeLocation = (Location.Region, Location, District, Block, Structure, Room);
                        ((Architect)CurrentTarget).FractalCycles = 0;
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                }
                else if (Spell == "raise")
                {
                    CooldownCycles += (int)Math.Round(100 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple));

                    if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " rises from the dead!", Color.Purple));
                        ((Architect)CurrentTarget).IsAlive = true;
                        ((Architect)CurrentTarget).IsImmortal = true;
                        ((Architect)CurrentTarget).Energy = 50;
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                }
                else if (Spell == "ressurect")
                {
                    CooldownCycles += (int)Math.Round(500 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} speaks the name of " + CurrentTarget.ReferredToNames[0] + " and meditates...", Color.Purple));

                    if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == this.Room))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " is surrounded in crystals and returns from the dead!", Color.Purple));
                        ((Architect)CurrentTarget).IsAlive = true;
                        ((Architect)CurrentTarget).IsImmortal = true;
                        ((Architect)CurrentTarget).Energy = 100;

                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            o.Integrity = 100;
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                }
                else if (Spell == "animate")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} conjures a spark of necromantic energy and passes it to " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple));

                    if (CurrentTarget is Architect && ((Architect)CurrentTarget).IsAlive == false && (((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == Room))
                    {
                        Announcements.Add(new TextStorage(CurrentTarget.ReferredToNames[0] + " rises with a putrid, dark energy!", Color.Purple));
                        ((Architect)CurrentTarget).IsAlive = true;
                        ((Architect)CurrentTarget).IsImmortal = true;
                        ((Architect)CurrentTarget).Race = Game1.GameWorld.GetRace("shade");

                        ((Architect)CurrentTarget).OppositionTags.Add("alllife");
                        ((Architect)CurrentTarget).Energy = 100;


                        foreach (Object o in ((Architect)CurrentTarget).BodyParts)
                        {
                            o.Integrity = 100;
                        }
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                }
                else if (Spell == "ethereal rupture")
                {
                    RuptureMode = true;
                }
                else if (Spell == "emergence")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} holds out a hand and speaks the name of " + CurrentTarget.ReferredToNames[0] + "...", Color.Purple));

                    if (!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == true)
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                    else
                    {
                        Architect target = (Architect)CurrentTarget;

                        Announcements.Add(new TextStorage(CurrentTarget.Name + " appears in front of you!", Color.Purple));

                        target.Block = Block;

                        target.BodyParts.Clear();
                        target.AddBodyParts();

                        target.Inventory = new List<Object>();
                        target.Clothing = new List<Object>();
                        target.Energy = target.MaxEnergy();

                        if (Room != null)
                        {
                            Room.Architects.Add(target);
                            target.Room = Room;
                        }
                        else
                        {
                            Block.Architects.Add(target);
                        }
                    }
                }
                else if (Spell == "eternal bind")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} stares deeply into " + CurrentTarget.ReferredToNames[0] + "'s eyes...", Color.Purple));

                    if ((!(CurrentTarget is Architect) || ((Architect)CurrentTarget).IsAlive == false) && ((Architect)CurrentTarget).Block == Block && ((Architect)CurrentTarget).Room == Room)
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                    else
                    {
                        Architect target = (Architect)CurrentTarget;

                        Announcements.Add(new TextStorage(CurrentTarget.Name + " stares back in awe...", Color.Purple));

                        target.ChangeOpinion(this, 999999);
                        
                        if(target.TargetArchitect == this && (target.Task == "killtarget" || target.Task == "disabletarget"))
                        {
                            target.Task = "";
                            target.TargetArchitect = null;
                        }

                        if(!Game1.GamePlayerParty.Architects.Contains(target))
                        {
                            Game1.GamePlayerParty.Architects.Add(target);
                        }
                    }
                }
                else if (Spell == "expunge")
                {
                    CooldownCycles += (int)Math.Round(5 / Speed());
                    Announcements.Add(new TextStorage($"{casterName} gestures agressively...", Color.Purple));


                    if (CurrentTarget is Civilization)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} and its legacy have fallen...", Color.Purple));

                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (l.HomeCivilization == CurrentTarget)
                            {
                                l.Region.MyLocation = null;
                            }
                        }
                    }
                    else if (Game1.AllSpells.Contains(CurrentTarget.Metadata))
                    {
                        Announcements.Add(new TextStorage($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple));
                        Game1.GameWorld.DeletedSpells.Add(CurrentTarget.Metadata);

                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            a.SpellsKnown.Remove(CurrentTarget.Metadata);
                        }
                    }
                    else if (Game1.AllLegendarySpells.Contains(CurrentTarget.Metadata))
                    {
                        Announcements.Add(new TextStorage($"An accursed relic locks this spell away. Perhaps you can find and banish this artifact instead.", Color.Purple));
                    }
                    else if (CurrentTarget is Blight)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} has been entirely purified...", Color.Purple));

                        for (int x = 0; x < Game1.GameWorld.Width; x++)
                        {
                            for (int z = 0; z < Game1.GameWorld.Width; z++)
                            {
                                if (Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width].Blight == CurrentTarget)
                                {
                                    Game1.GameWorld.WorldMap[x, z * Game1.GameWorld.Width].Blight = Game1.GameWorld.Purity;
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Composition)
                    {
                        Announcements.Add(new TextStorage($"The knowledge of {CurrentTarget.Name} has been erased from the land...", Color.Purple));

                        foreach (Architect a in Game1.GameWorld.AllArchitects)
                        {
                            a.CultureBank.Remove((Composition)CurrentTarget);
                        }

                        Game1.GameWorld.DeletedCompositions.Add((Composition)CurrentTarget);
                    }
                    else if (CurrentTarget is Deity)
                    {
                        Announcements.Add(new TextStorage($"You feel an intense pain...", Color.Purple));
                        Energy = 1;
                    }
                    else if (CurrentTarget is District)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple));

                        ((District)CurrentTarget).Location.Districts.Remove(((District)CurrentTarget));

                        if (Game1.GamePlayerParty.Architects.Contains(this))
                        {
                            foreach (Architect a in Game1.GamePlayerParty.Architects)
                            {
                                if (a.District == CurrentTarget)
                                {
                                    Game1.MakeObservation(a.Name + " was successfully launched into oblivion. How embarrassing...", Color.Magenta);
                                    a.IsAlive = false;
                                    Game1.GamePlayerParty.Architects.Remove(a);

                                    if (Game1.GamePlayerParty.Architects.Count == 0)
                                    {
                                        Game1.GameState = "dead";

                                        if (Game1.GamePlayerParty.Architects.Count == 0)
                                        {
                                            Game1.GameState = "dead";
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Location)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} detaches from the ground and levitates into infinite nothing.", Color.Purple));

                        ((Location)CurrentTarget).Region.MyLocation = null;

                        if (Game1.GamePlayerParty.Architects.Contains(this))
                        {
                            foreach (Architect a in Game1.GamePlayerParty.Architects)
                            {
                                if (a.Location == (Location)CurrentTarget)
                                {
                                    Game1.MakeObservation(LoadedArchitects[ArchitectIndex].Name + " was successfully launched into oblivion. How embarrassing...", Color.Magenta);
                                    Game1.LoadedArchitects[ArchitectIndex].IsAlive = false;
                                    if (GamePlayerParty.Architects.Contains(LoadedArchitects[ArchitectIndex]))
                                    {
                                        Game1.GamePlayerParty.Architects.Remove(LoadedArchitects[ArchitectIndex]);

                                        if (GamePlayerParty.Architects.Count == 0)
                                        {
                                            Game1.GameState = "dead";

                                            if (GamePlayerParty.Architects.Count == 0)
                                            {
                                                Game1.GameState = "dead";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentTarget is Party)
                    {
                        Announcements.Add(new TextStorage($"Your party has disbanded.", Color.Purple));

                        List<Architect> ArchitectsToBanish = new List<Architect>();
                        foreach(Architect a in Game1.GamePlayerParty.Architects)
                        {
                            if(a != this)
                            {
                                ArchitectsToBanish.Add(a);
                            }
                        }

                        foreach(Architect a in ArchitectsToBanish)
                        {
                            Game1.GamePlayerParty.Architects.Remove(a);
                        }
                    }
                    else if (CurrentTarget is Group)
                    {
                        //disbands the group, removes it from any power, does not kill the members
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name}'s relationship has fractured.", Color.Purple));

                        Game1.GameWorld.Groups.Remove((Group)CurrentTarget);
                        Game1.GameWorld.TradingGroups.Remove((Group)CurrentTarget);

                        foreach(Location l in Game1.GameWorld.AllLocations)
                        {
                            if(l.Government == CurrentTarget)
                            {
                                l.Government = null;
                            }
                        }

                        foreach(Architect a in ((Group)CurrentTarget).Architects)
                        {
                            a.Group == null;
                        }
                    }
                    else if (CurrentTarget is Architect)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} is banished and forgotten.", Color.Purple));


                        Architect a = (Architect)CurrentTarget;

                        if(District.Architects.Contains(a))
                        {
                            District.Architects.Remove(a);
                        }

                        Game1.GameWorld.AllArchitects.Remove(a);

                        foreach (Architect A in Game1.GameWorld.AllArchitects)
                        {
                            A.KnownArchitectsAndOpinions.RemoveAll(opinion => opinion.Item1 == a);
                        }

                        if (Game1.GameWorld.Colossals.Contains(a))
                        {
                            Game1.GameWorld.Colossals.Remove(a);
                        }

                        if (Game1.LoadedArchitects.Contains(a))
                        {
                            Game1.LoadedArchitects.Remove(a);
                        }

                        foreach (Location l in Game1.GameWorld.AllLocations)
                        {
                            if (l.Government == a)
                            {
                                l.Government = null;
                            }
                        }

                        for (int x = 0; x < Game1.GameWorld.Width; x++)
                        {
                            for (int z = 0; z < Game1.GameWorld.Width; z++)
                            {
                                foreach(InteractableEvent e in Game1.GameWorld.WorldMap[x,z].Events)
                                {
                                    if(e.GuarranteedArchitects.Contains(a))
                                    {
                                        e.GuarranteedArchitects.Remove(a);
                                        break;
                                    }
                                }
                            }
                        }

                        a.IsAlive = false;
                        a.Location = null;
                        a.District = null;

                        if(a.Room != null)
                        {
                            a.Room.Architects.Remove(a);
                            a.DropInventory();
                            a.Room = null;
                        }
                        else if (a.Block != null)
                        {
                            a.Block.Architects.Remove(a);
                            a.DropInventory();
                            a.Block = null;
                        }

                        if(Game1.GamePlayerParty.Architects.Contains(a))
                        {
                            Game1.GamePlayerParty.Architects.Remove(a);
                        }

                        if (a.Group != null)
                        {
                            if(a.Group.Leader == a)
                            {
                                //disbands the group, removes it from any power, does not kill the members
                                Announcements.Add(new TextStorage($"{CurrentTarget.Name}'s relationship has fractured.", Color.Purple));

                                Game1.GameWorld.Groups.Remove((Group)CurrentTarget);
                                Game1.GameWorld.TradingGroups.Remove((Group)CurrentTarget);

                                foreach (Location l in Game1.GameWorld.AllLocations)
                                {
                                    if (l.Government == CurrentTarget)
                                    {
                                        l.Government = null;
                                    }
                                }

                                foreach (Architect A in ((Group)CurrentTarget).Architects)
                                {
                                    A.Group == null;
                                }
                            }
                            else
                            {
                                a.Group.Architects.Remove(a);
                            }
                        }

                        if (Game1.GameWorld.Hypernexus == a)
                        {
                            Game1.GameWorld.Hypernexus = null;
                        }
                        if (Game1.GameWorld.Icosidodecahedron == a)
                        {
                            Game1.GameWorld.Icosidodecahedron = null;
                        }
                        if (Game1.GameWorld.Shadeheart == a)
                        {
                            Game1.GameWorld.Shadeheart = null;
                        }

                    }
                    else if (CurrentTarget is Object)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} collapses into a singularity. (UNIMPLEMENTED PLS)", Color.Purple));

                        //unimplemented fsr
                    }
                    else if (CurrentTarget is Material)
                    {
                        //add the material to a list of banished materials. Replace objects iwth these materials with Void Energy, a new material when they update. You cannot cast this spell on Void Material.
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name}'s properties have been reduced away.", Color.Purple));

                        Game1.GameWorld.DeletedMaterials.Add((Material)CurrentTarget);
                    }
                    else if (CurrentTarget is Race)
                    {
                        Announcements.Add(new TextStorage($"The members of {CurrentTarget.Name} have been reduced to indistinguishable lifeforms.", Color.Purple));
                        //a banished race makes all members of that race shades. this is stored in a static list.

                        Game1.GameWorld.Races
                    }
                    else if (CurrentTarget is Structure)
                    {
                        Announcements.Add(new TextStorage($"{CurrentTarget.Name} vanishes.", Color.Purple));
                    }
                    else if (CurrentTarget is World)
                    {
                        Game1.GameState = "mainscreen";
                    }
                    else
                    {
                        Announcements.Add(new TextStorage("...but nothing happens.", Color.Purple));
                    }
                }
            }


            foreach (Imbuement i in CurrentlyActiveImbuements)
            {
                if (i.IsTrigger && i.ConditionOrTrigger == "oncast")
                {
                    ActivatePower(i.BuffOrResult);
                }
            }

            return Announcements;
        }

        public TextStorage ActivatePower(string Power)
        {
            if (Power == "barrier")
            {
                BarrierStacks++;
                return new TextStorage(ReferredToNames[0] + " generates a barrier stack!", Color.Magenta);
            }
            else if (Power == "projectile")
            {
                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            Object o = new Object(null, "bolt", new List<Material>() { new Material("energy", "energy", 3, 0) }, this);
                            Room.Objects.Add(o);
                            o.AirborneTarget = a;
                            o.AirborneCyclesToHitTarget = 15 - Focus;
                            return new TextStorage(ReferredToNames[0] + " fires a bolt at " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            Object o = new Object(null, "bolt", new List<Material>() { new Material("energy", "energy", 3, 0) }, this);
                            Block.Objects.Add(o);
                            o.AirborneTarget = a;
                            o.AirborneCyclesToHitTarget = 15 - Focus;
                            return new TextStorage(ReferredToNames[0] + " fires a bolt at " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
            }
            else if (Power == "ignite")
            {
                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            a.FireCycles += 40;
                            return new TextStorage(ReferredToNames[0] + " ignites " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            a.FireCycles += 40;
                            return new TextStorage(ReferredToNames[0] + " ignites " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
            }
            else if (Power == "destabilize")
            {
                if (Room != null)
                {
                    foreach (Architect a in Room.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            a.DestabilizedCycles += 20;
                            return new TextStorage(ReferredToNames[0] + " destabilizes " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
                else
                {
                    foreach (Architect a in Block.Architects)
                    {
                        if ((a.Task == "fighting" && a.TargetArchitect == this) || (Task == "fighting" && this.TargetArchitect == a))
                        {
                            a.DestabilizedCycles += 20;
                            return new TextStorage(ReferredToNames[0] + " destabilizes " + a.ReferredToNames[0] + "!", Color.Magenta);
                        }
                    }
                }
            }
            else if (Power == "dismiss")
            {
                ExtraDodgeChance += 10;
                return new TextStorage(ReferredToNames[0] + " becomes partially intangible!", Color.Magenta);
            }
            return new TextStorage("An unknown power triggered!", Color.Magenta);
        }

        public void Fractallize(int Cycles)
        {
            FractalCycles = Cycles;
            RematerializeLocation = (this.Location.Region, this.Location, this.District, this.Block, this.Structure, this.Room);
            Location.Region.World.FractalArchitects.Add(this);

            if (Room != null)
            {
                Room.Architects.Remove(this);
            }
            else if (Block != null)
            {
                Block.Architects.Remove(this);
            }
        }
    }
}
