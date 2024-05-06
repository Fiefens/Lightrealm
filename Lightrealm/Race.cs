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
        public string Size;
        public List<(string, Material)> BodyParts;
        public string Color;
        public List<string> NecessaryBodyParts;
        public string RaceLetter;
        public List<string> OppositionTags = new List<string>();
        public string Description;
        public int NaturalArmor = 0;

        public Race(string name, string size, List<(string, Material)> bodyParts, string color, List<string> necessaryBodyParts, List<string> oppositionTags, int naturalArmor)
        {
            Name = name;
            Size = size;
            BodyParts = bodyParts;
            Color = color;
            NecessaryBodyParts = necessaryBodyParts;
            OppositionTags = oppositionTags;
            NaturalArmor = naturalArmor;

            ReferredToNames.Add(Name);

            Description = GenerateDescription();
        }

        private string GenerateDescription()
        {
            string baseDescription = Name.ToLower() switch
            {
                "luminarch" => "A white, glowing humanoid with a bright flame for a head bent, typically bent towards peace and stability.",
                "nightfell" => "A dark, shadowy humanoid with a dark flame for a head, typically bent toward individual liberation.",
                "archaix" => "A gray, swirling humanoid with a smoky flame for a head driven towards an unknown end.",
                "isofractal" => "A glass icosahedron surrounded by several glass shards, manipulating fractal energy to bring perfection to the universe.",
                "photonexus" => "A core surrounded by six spheres, driven to create a perfect society and destroy anyone who threatens it.",
                "shade" => "A small, chaotic creature made of shadowy substance manipulated to destroy the entire world.",
                "cnidriarch" => "A floating, colossal bell-shaped creature.",
                "wyrm" => "A colossally long snake that flies through the air.",
                "quetzal" => "A colossal bird whose flapping makes the earth shake.",
                "serpent" => "A colossal serpent with a hideous roar.",
                "shobe" => "A colossal, fluffy four-legged beast with a seemingly friendly complexion.",
                "shiba" => "A fluffy, four-legged creature with a friendly complexion and unfathomable charisma.",
                "debtshiba" => "A fluffy, four-legged creature with an unrivaled desire for capitalistic righteousness.",
                _ => "A " + Size + " wilderness creature of some sort."
            };

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



        public static string GenerateUniqueAbbreviation(string raceName, List<Race> existingRaces)
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
