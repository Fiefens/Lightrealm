using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Race : Entity
    {
        public string Size { get; set; }
        public List<string> BodyPartNames { get; set; } = new List<string>();
        public List<string> BodyPartMaterials { get; set; } = new List<string>();

        public string Color { get; set; }
        public List<string> NecessaryBodyParts { get; set; } = new List<string>();
        public string RaceLetter { get; set; }
        public List<string> OppositionTags { get; set; } = new List<string>();
        public string Description { get; set; }
        public int NaturalArmor { get; set; } = 0;
        public List<string> Powers { get; set; } = new List<string>();
        public string MainInteractionAppendage { get; set; } = "";
        public string OffInteractionAppendage { get; set; } = "";

        public Race(string name, string size, List<(string, Material)> bodyParts, string color, List<string> necessaryBodyParts, List<string> oppositionTags, int naturalArmor, string mainInteractionAppendage, string offInteractionAppendage, World GameWorld)
        {
            Name = name;
            Size = size;
            foreach (var bodyPart in bodyParts)
            {
                BodyPartNames.Add(bodyPart.Item1);
                BodyPartMaterials.Add(bodyPart.Item2.Name);
            }
            Color = color;
            NecessaryBodyParts = necessaryBodyParts;
            OppositionTags = oppositionTags;
            NaturalArmor = naturalArmor;
            MainInteractionAppendage = mainInteractionAppendage;
            OffInteractionAppendage = offInteractionAppendage;

            if (Name.EndsWith("guardian"))
            {
                List<string> PowerTypes = new List<string> { "energybolts", "cloaking", "magneticfield", "shockwave", "slowray", "pulsebash", "harvest" };
                int numberOfPowers = Game1.r.Next(1, 4);
                Powers = PowerTypes.OrderBy(x => Game1.r.Next()).Take(numberOfPowers);
            }

            AddReferredToName(Name);
            Description = GenerateDescription();

            foreach (var bodyPart in BodyPartNames)
            {
                if (!Game1.EntityLedger.Where(e => e.Value != null).Any(e => e.Value.Name == bodyPart) &&
                    !Game1.TemporaryEntities.Where(e => e.Value != null).Any(e => e.Value.Metadata == bodyPart))
                {
                    new Entity(bodyPart);
                }
            }

        }

        public Race()
        {

        }

        private string GenerateDescription()
        {
            string baseDescription = Name.ToLower() switch
            {
                "luminarch" => "A white, glowing humanoid with a bright flame for a head, typically bent towards peace and stability.",
                "nightfell" => "A dark, shadowy humanoid with a dark flame for a head, typically bent toward individual liberation.",
                "archaix" => "A gray, swirling humanoid with a smoky flame for a head driven towards an unknown end.",
                "isofractal" => "A glass icosahedron surrounded by several glass shards, manipulating fractal energy to bring perfection to the universe.",
                "photonexus" => "A core surrounded by six spheres, driven to create a perfect society and destroy anyone who threatens it.",
                "shade" => "A small, chaotic creature made of shadowy substance manipulated to destroy the entire world.",
                "cnidriarch" => "A floating, colossal bell-shaped creature with many tentacles.",
                "wyrm" => "An unfathomably long snake that flies through the air.",
                "quetzal" => "A colossal bird whose flapping makes the earth shake.",
                "serpent" => "A colossal dragon-like creature with a hideous roar.",
                "shobe" => "A colossal, fluffy four-legged beast with a seemingly friendly complexion.",
                "shiba" => "A fluffy, four-legged creature with a friendly complexion and unfathomable charisma.",
                "debtshiba" => "A fluffy, four-legged creature with an unrivaled desire for capitalistic righteousness.",
                "hypernexus" => "A very large photonexus bearing no imperfection.",
                "shadeheart" => "A colossal beating heart. It beats inconsistently, spreading a foul poison throughout its many veins.",
                "icosidodecahedron" => "An 80-sided rotating rainbow prism. A glorious expressive light accompanies its presence.",
                _ => "A " + Size + "-sized creature of some sort."
            };

            if (Name.EndsWith("guardian"))
            {
                baseDescription = "A guardian construct, built from pure " + BodyPartMaterials[0] + ", dedicated to an unknown purpose.";
            }

            return baseDescription + " " + GenerateBodyPartsDescription();
        }

        private string GenerateBodyPartsDescription()
        {
            var commonParts = new HashSet<string> { "leg", "wing", "arm", "eye", "antenna", "tentacle", "tail", "hump", "fin", "tusk", "spike", "tooth", "hand", "foot", "shoulder" };
            var groupedParts = BodyPartNames.GroupBy(bp => commonParts.Contains(bp.Split(' ').Last()) ? bp.Split(' ').Last() : bp)
                                            .ToDictionary(g => g.Key, g => g.Count()());

            if (!groupedParts.Any())
            {
                return "";
            }

            var irregularPlurals = new Dictionary<string, string>
            {
                { "tooth", "teeth" },
                { "foot", "feet" }
            };

            var partsDescription = groupedParts.Select(part =>
            {
                string partName = part.Value > 1 ? irregularPlurals.GetValueOrDefault(part.Key, part.Key + "s") : part.Key;
                return $"{part.Value} {partName}";
            });

            return "It has " + string.Join(", ", partsDescription.Take(partsDescription.Count() - 1)) + (partsDescription.Count() > 1 ? ", and " : "") + partsDescription.Last() + ".";
        }

        public static string GenerateUniqueAbbreviation(string raceName, List<Race> existingRaces)
        {
            var priorityAbbreviations = new Dictionary<string, string>
            {
                { "nightfell", "N" }, { "luminarch", "L" }, { "archaix", "A" },
                { "isofractal", "I" }, { "photonexus", "P" }, { "shade", "S" },
                { "shadebeast", "SB" }, { "", "?" }, { "icosidodecahedron", "ID" }, { "hypernexus", "HN" }, { "shadeheart", "SH" },
                { "cassartrae", "CA" }, { "moari", "MO" }
            };

            if (priorityAbbreviations.TryGetValue(raceName.ToLower(), out string abbreviation))
            {
                return abbreviation;
            }

            for (int i = 1; i < raceName.Length; i++)
            {
                string potentialAbbreviation = raceName.Substring(0, i + 1).ToUpper();

                if (!existingRaces.Any(r => r.RaceLetter == potentialAbbreviation))
                {
                    return potentialAbbreviation;
                }
            }

            return raceName.Substring(0, 2).ToUpper();
        }
    }
}
