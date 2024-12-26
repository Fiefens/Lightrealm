using System;
using System.Linq;

namespace Lightrealm
{
    public class MultiObject : Object
    {
        public EntityList<Object> BoundObjects = new EntityList<Object>();

        public MultiObject(Architect creator, EntityList<Object> objectsToCombine)
        {
            BoundObjects = objectsToCombine;

            // Type: Create a portmanteau of all object types
            Type = string.Join("-", objectsToCombine.Select(o => o.Type).Distinct());

            // Materials: Union of all materials without duplicates
            Materials = new EntityList<Material>(objectsToCombine.SelectMany(o => o.Materials).Distinct().ToList());

            // Description: Combine descriptions
            Description = string.Join(", bound with ", objectsToCombine.Select(o => o.Description.TrimEnd('.')));

            // IsContainer: If any object is a container, the MultiObject is
            IsContainer = objectsToCombine.Any(o => o.IsContainer);

            // ContainedObjects: Union with no duplicates
            ContainedObjects = new EntityList<Object>(objectsToCombine.SelectMany(o => o.ContainedObjects).Distinct().ToList());

            // IfTrueUseInIfFalseUseOn: Prioritize \"on"
            IfTrueUseInIfFalseUseOn = objectsToCombine.All(o => o.IfTrueUseInIfFalseUseOn);

            // LatestUpdateCycle: Use the highest cycle value
            LatestUpdateCycle = objectsToCombine.Max(o => o.LatestUpdateCycle);

            // Tags: Union of all tags
            Tags = objectsToCombine.SelectMany(o => o.Tags).Distinct().ToList();

            // DyedColor: Logic for "none" and other colors
            DyedColor = objectsToCombine.Select(o => o.DyedColor).Distinct().Count() == 1 ? objectsToCombine.First().DyedColor : objectsToCombine.FirstOrDefault(o => o.DyedColor != "none")?.DyedColor ?? "none";

            // IsMagical: If any object is magical, this is magical
            IsMagical = objectsToCombine.Any(o => o.IsMagical);

            // YLevelInFeet and YVelocity
            YLevelInFeet = 0;
            YVelocity = 0;

            // Weight: Combined weight
            Weight = objectsToCombine.Sum(o => o.Weight);

            // WeaponMaximumRange: Largest range
            WeaponMaximumRange = objectsToCombine.Max(o => o.WeaponMaximumRange);

            // RealityAugmented: If all are false, it's false. Otherwise, it's true.
            RealityAugmented = objectsToCombine.Any(o => o.RealityAugmented);

            // TemporaryStructureStorage: Set to null
            TemporaryStructureStorage = null;

            // AwareArchitects: Set to a new empty list
            AwareArchitects = new EntityList<Architect>();

            // CarvedSymbols: Union of all carved symbols without duplicates
            CarvedSymbols = new EntityList<Entity>(objectsToCombine.SelectMany(o => o.CarvedSymbols).Distinct().ToList());

            Block = null;
            Room = null;

            // Polished: Only polished if all objects are polished
            Polished = objectsToCombine.All(o => o.Polished);

            // Cleaned: Only cleaned if all objects are cleaned
            Cleaned = objectsToCombine.All(o => o.Cleaned);

            // HeatInCelsius: Average heat
            HeatInCelsius = (int)objectsToCombine.Average(o => o.HeatInCelsius);

            // IsConsumable: Only consumable if all objects are
            IsConsumable = objectsToCombine.All(o => o.IsConsumable);

            // Imbuements: Combine all imbuements
            Imbuements = new EntityList<Imbuement>(objectsToCombine.SelectMany(o => o.Imbuements).ToList());

            // IsWearable: If any object is wearable, the MultiObject is wearable
            IsWearable = objectsToCombine.Any(o => o.IsWearable);

            // Rarity: Set to the highest rarity
            Rarity = objectsToCombine.MaxBy(o => GetRarityRank(o.Rarity)).Rarity;

            // Exposure: Set to 0
            Exposure = 0;

            // IsBodyPart: Set to false
            IsBodyPart = false;

            // MajorArteryIsSevered: True if at least one object is true
            MajorArteryIsSevered = objectsToCombine.Any(o => o.MajorArteryIsSevered);

            // Airborne properties: Set to default
            AirborneTarget = null;
            AirbornePower = 0;
            AirborneCyclesToHitTarget = 0;

            // Thrower: Set to null
            Thrower = null;

            // Creator: Set to the passed Architect
            Creator = creator;

            // FireCycles, WetCycles, DestabilizedCycles: Combine values
            FireSeconds = objectsToCombine.Sum(o => o.FireSeconds);
            WetCycles = objectsToCombine.Sum(o => o.WetCycles);
            DestabilizedCycles = objectsToCombine.Sum(o => o.DestabilizedCycles);

            // FractalCycles: Set to 0
            FractalCycles = 0;

            RematerializeLocation = (null, null, null, null, null, null);

            // IsCoveredInPlants: True if any are
            PlantCycles = objectsToCombine.Sum(o => o.PlantCycles);

            // CoverageValues: Highest coverage values for each string
            CoverageValues = objectsToCombine
                .SelectMany(o => o.CoverageValues)
                .GroupBy(cv => cv.Item1)
                .Select(g => (g.Key, g.Max(cv => cv.Item2)))
                .ToList();

            // Coverage and CoverageName: Set to defaults
            Coverage = 0;
            CoverageName = "";

            // IsWeapon: If any object is a weapon, the MultiObject is
            IsWeapon = objectsToCombine.Any(o => o.IsWeapon);

            // DamageType: Pick the first damage type found in this order
            string[] damagePriority = { "piercing", "slashing", "scourging", "bashing" };
            DamageType = objectsToCombine
                .Where(o => o.IsWeapon)
                .Select(o => o.DamageType)
                .FirstOrDefault(dt => damagePriority.Contains(dt)) ?? "bashing";

            // ProjectileAerodynamic: Only true if all objects are
            ProjectileAerodynamic = objectsToCombine.All(o => o.ProjectileAerodynamic);

            // Strength: Take the highest strength
            Strength = objectsToCombine.Max(o => o.Strength);

            // Dissipating: Set to false
            Dissipating = false;

            // Integrity: Average integrity of all objects
            Integrity = (int)objectsToCombine.Average(o => o.Integrity);

            // IsWritable: Set to false
            IsWritable = false;

            // CompositionContent and SpecialKnowledge: Set to null
            CompositionContent = null;
            SpecialKnowledge = null;

            // IsGeneralGood: Set to false
            IsGeneralGood = false;

            // Owner: Set to null
            Owner = null;
        }

        // Override Value function to combine values of all objects
        public override int Value()
        {
            return BoundObjects.Sum(o => o.Value());
        }

        // Override FindObjectGenericStrength to use the highest strength
        public override int FindObjectGenericStrength()
        {
            return BoundObjects.Max(o => o.FindObjectGenericStrength());
        }

        // Helper function to rank rarity
        private int GetRarityRank(string rarity)
        {
            switch (rarity.ToLower())
            {
                case "common": return 1;
                case "uncommon": return 2;
                case "rare": return 3;
                case "epic": return 4;
                case "mythical": return 5;
                case "legendary": return 6;
                case "exalted": return 7;
                default: return 0;
            }
        }

        // Override TakeDamageFromObject to handle multi-object damage
        public override EntityList<TextStorage> TakeDamageFromObject(Object o, int WielderProficiency, Architect MeleeAttacker, string DescriptiveVerb)
        {
            var announcements = new EntityList<TextStorage>();

            foreach (var boundObject in BoundObjects.ToList()) // Use ToList to avoid modification during iteration
            {
                var damageResults = boundObject.TakeDamageFromObject(o, WielderProficiency, MeleeAttacker, DescriptiveVerb);
                announcements.AddRange(damageResults);

                if (boundObject.Integrity <= 0)
                {
                    BoundObjects.Remove(boundObject);
                }
            }

            return announcements;
        }

        // Override UpdateNames to handle multi-object names
        public void UpdateNames()
        {
            // Use the MultiObject type name
            string combinedName = string.Join("-", BoundObjects.Select(o => o.Type).Distinct());
            ClearReferredToNames();
            AddReferredToName(combinedName);

            // Additional logic to handle named multiobjects if necessary
            base.UpdateNames(false, null);
        }
    }
}