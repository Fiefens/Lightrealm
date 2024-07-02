using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Race : Entity
    {
        public static T Entity<T>(int entityId) where T : Entity
        {
            if (Game1.GameWorld == null || Game1.GameWorld.AllEntities == null)
            {
                return (T)Convert.ChangeType(Game1.TemporaryEntities[entityId], typeof(T));
            }

            return (T)Convert.ChangeType(Game1.GameWorld.AllEntities[entityId], typeof(T));
        }

        public string Size { get; set; }

        private List<(string, int)> _bodyParts = new List<(string, int)>();
        public List<(string, Material)> BodyParts
        {
            get => _bodyParts.Select(tuple => (tuple.Item1, Entity<Material>(tuple.Item2))).ToList();
            set => _bodyParts = value.Select(tuple => (tuple.Item1, tuple.Item2.ID)).ToList();
        }

        public string Color { get; set; }
        public List<string> NecessaryBodyParts { get; set; } = new List<string>();
        public string RaceLetter { get; set; }
        public List<string> OppositionTags { get; set; } = new List<string>();
        public string Description { get; set; }
        public int NaturalArmor { get; set; } = 0;
        public List<string> Powers { get; set; } = new List<string>();
        public string MainInteractionAppendage { get; set; } = "";
        public string OffInteractionAppendage { get; set; } = "";

        public Race(string name, string size, List<(string, Material)> bodyParts, string color, List<string> necessaryBodyParts, List<string> oppositionTags, int naturalArmor, string mainInteractionAppendage, string offInteractionAppendage)
        {
            Name = name;
            Size = size;
            BodyParts = bodyParts;
            Color = color;
            NecessaryBodyParts = necessaryBodyParts;
            OppositionTags = oppositionTags;
            NaturalArmor = naturalArmor;

            MainInteractionAppendage = mainInteractionAppendage;
            OffInteractionAppendage = offInteractionAppendage;

            if (Name.EndsWith("guardian"))
            {
                List<string> PowerTypes = new List<string>() { "energybolts", "cloaking", "magneticfield", "shockwave", "slowray", "pulsebash", "harvest" };
                int numberOfPowers = Game1.r.Next(1, 4);
                Powers = PowerTypes.OrderBy(x => Game1.r.Next()).Take(numberOfPowers).ToList();
            }

            AddReferredToName(Name);

            Description = GenerateDescription();

            // Add body parts to the appropriate list
            foreach (var bodyPart in bodyParts.Select(bp => bp.Item1))
            {
                if (Game1.GameWorld != null)
                {
                    if (!Game1.GameWorld.AllEntities.Any(e => e.Value.Name == bodyPart) && !Game1.TemporaryEntities.Any(e => e.Value.Metadata == bodyPart))
                    {
                        Entity bodyPartEntity = new Entity(bodyPart);
                    }
                }
                else
                {
                    if (!Game1.TemporaryEntities.Any(e => e.Value.Metadata == bodyPart))
                    {
                        Entity bodyPartEntity = new Entity(bodyPart);
                    }
                }
            }
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
                baseDescription = "A guardian construct, built from pure " + BodyParts[0].Item2.Name + ", dedicated to an unknown purpose.";
            }

            string partsDescription = GenerateBodyPartsDescription();
            return baseDescription + " " + partsDescription;
        }

        private string GenerateBodyPartsDescription()
        {
            var commonParts = new HashSet<string> { "leg", "wing", "arm", "eye", "antenna", "tentacle", "tail", "hump", "fin", "tusk", "spike", "tooth", "hand", "foot", "shoulder" };
            var groupedParts = new Dictionary<string, int>();
            var irregularPlurals = new Dictionary<string, string>
            {
                {"tooth", "teeth"},
                {"foot", "feet"}
            };

            foreach (var bodyPart in BodyParts)
            {
                var partNameSplit = bodyPart.Item1.Split(' ');
                var lastWord = partNameSplit[partNameSplit.Length - 1];

                if (commonParts.Contains(lastWord))
                {
                    if (!groupedParts.ContainsKey(lastWord))
                    {
                        groupedParts[lastWord] = 0;
                    }
                    groupedParts[lastWord]++;
                }
                else
                {
                    if (!groupedParts.ContainsKey(bodyPart.Item1))
                    {
                        groupedParts[bodyPart.Item1] = 0;
                    }
                    groupedParts[bodyPart.Item1]++;
                }
            }

            var partsDescription = new List<string>();
            foreach (var part in groupedParts)
            {
                string partName;
                if (part.Value > 1)
                {
                    partName = irregularPlurals.ContainsKey(part.Key) ? irregularPlurals[part.Key] : part.Key + "s";
                }
                else
                {
                    partName = part.Key;
                }
                partsDescription.Add($"{part.Value} {partName}");
            }

            // Handling the formatting for different list lengths
            string description;
            if (partsDescription.Count > 1)
            {
                var allButLast = partsDescription.Take(partsDescription.Count - 1);
                var last = partsDescription.Last();
                description = string.Join(", ", allButLast) + ", and " + last;
            }
            else
            {
                description = partsDescription.FirstOrDefault() ?? "";
            }

            return "It has " + description + ".";
        }



        public static string GenerateUniqueAbbreviation(string raceName, EntityList<Race> existingRaces)
        {
            // Priority abbreviations (these races have seniority)
            Dictionary<string, string> priorityAbbreviations = new Dictionary<string, string>
            {
                {"nightfell", "N"}, {"luminarch", "L"}, {"archaix", "A"},
                {"isofractal", "I"}, {"photonexus", "P"}, {"shade", "S"},
                {"shadebeast", "SB"}, {"", "?"}, {"icosidodecahedron", "ID"},{"hypernexus", "HN"},{"shadeheart", "SH"},
                {"cassartrae", "CA"}, {"moari", "MO"}
            };

            if (priorityAbbreviations.TryGetValue(raceName.ToLower(), out string abbreviation))
            {
                return abbreviation;
            }

            // Generate a unique abbreviation for new race
            for (int i = 1; i < raceName.Length; i++)
            {
                string potentialAbbreviation = raceName.Substring(0, i + 1).ToUpper();

                if (!existingRaces.Any(r => r.RaceLetter == potentialAbbreviation))
                {
                    return potentialAbbreviation;
                }
            }

            // Fallback if no unique abbreviation found (very unlikely with long race names)
            return raceName.Substring(0, 2).ToUpper();
        }
    }
}
