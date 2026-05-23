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

        public bool Accursed = false;

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
                int numberOfPowers = Game1.TemporaryRand.Next(1, 4);
                Powers = PowerTypes.OrderBy(x => Game1.TemporaryRand.Next()).Take(numberOfPowers).ToList();
            }

            AddReferredToName(Name);
            Description = GenerateDescription();

            foreach (var bodyPart in BodyPartNames)
            {
                bool entityExists = false;

                if (Game1.GameWorld != null && Game1.GameWorld.EntityLedger != null)
                {
                    entityExists = Game1.GameWorld.EntityLedger.Values.Any(e => e != null && e.Name == bodyPart);
                }

                if (!entityExists)
                {
                    entityExists = Game1.TemporaryEntityLedger.Values.Any(e => e != null && e.Metadata == bodyPart);
                }

                {
                    new Entity(bodyPart);
                }
            }


            if (MainInteractionAppendage == "findforme" || OffInteractionAppendage == "findforme")
            {
                string main = null;
                string off = null;

                // Look for matching pairs: e.g., "left hand" + "right hand", or "hand 1" + "hand 2"
                var candidates = BodyPartNames.ToList();

                // 1. Try left/right pattern
                foreach (var baseName in new[] { "hand", "foot", "leg", "arm", "wing", "tentacle", "fin", "tail", "antenna" })
                {
                    string left = candidates.FirstOrDefault(p => p.ToLower().StartsWith("left " + baseName));
                    string right = candidates.FirstOrDefault(p => p.ToLower().StartsWith("right " + baseName));

                    if (left != null && right != null)
                    {
                        main = right;
                        off = left;
                        break;
                    }
                }

                // 2. Try patterns like "front left leg" and "front right leg"
                if (main == null || off == null)
                {
                    foreach (var baseName in new[] { "leg", "arm", "wing", "tentacle", "fin", "tail", "antenna" })
                    {
                        var groupings = candidates
                            .Where(p => p.ToLower().Contains(baseName))
                            .GroupBy(p => p.ToLower().Replace("left", "").Replace("right", "").Replace("front", "").Replace("back", "").Trim())
                            .Where(g => g.Count() > 1)
                            .ToList();

                        if (groupings.Any())
                        {
                            var pair = groupings.First().Take(2).ToList();
                            main = pair[0];
                            off = pair[1];
                            break;
                        }
                    }
                }

                // 3. Try patterns like "leg 1", "leg 2"
                if (main == null || off == null)
                {
                    var numberPattern = new System.Text.RegularExpressions.Regex(@"^(?<base>\D+)\s*(?<num>\d+)$");
                    var groups = candidates
                        .Select(p => (Match: numberPattern.Match(p), Name: p))
                        .Where(x => x.Match.Success)
                        .GroupBy(x => x.Match.Groups["base"].Value.Trim())
                        .Where(g => g.Count() >= 2)
                        .ToList();

                    if (groups.Any())
                    {
                        var pair = groups.First().Take(2).Select(x => x.Name).ToList();
                        main = pair[0];
                        off = pair[1];
                    }
                }

                // 4. Fall back to any two distinct parts
                if (main == null || off == null)
                {
                    var randomParts = BodyPartNames.OrderBy(x => Game1.TemporaryRand.Next()).Distinct().Take(2).ToList();
                    if (randomParts.Count == 2)
                    {
                        main ??= randomParts[0];
                        off ??= randomParts[1];
                    }
                    else if (randomParts.Count == 1)
                    {
                        main ??= randomParts[0];
                        off ??= randomParts[0];
                    }
                }

                if (MainInteractionAppendage == "findforme")
                    MainInteractionAppendage = main;
                if (OffInteractionAppendage == "findforme")
                    OffInteractionAppendage = off;

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
                "shade" => "A mysterious creature comprised of pure void energy.",
                "cnidriarch" => "A floating, colossal bell-shaped creature with many tentacles.",
                "wyrm" => "An unfathomably long snake that flies through the air.",
                "quetzal" => "A colossal bird whose flapping makes the earth shake.",
                "serpent" => "A colossal dragon-like creature with a hideous roar.",
                "shobe" => "A colossal, fluffy four-legged beast with a seemingly friendly complexion.",
                "shiba" => "A fluffy, four-legged creature with a friendly complexion and unfathomable charisma.",
                "debtshiba" => "A fluffy, four-legged creature with unfathomable charisma and an unrivaled desire for capitalistic righteousness.",
                "hypernexus" => "A very large photonexus bearing no imperfection.",
                "shadeheart" => "A colossal beating heart. It beats inconsistently, spreading a foul poison throughout its many veins.",
                "icosidodecahedron" => "A 32-sided rotating rainbow prism. A glorious expressive light accompanies its presence.",
                _ => "A " + Size + "-sized creature."
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

            // Group body parts properly
            var groupedParts = BodyPartNames
                .Select(bp =>
                {
                    var parts = bp.Split(' ');

                    // If the last part is a number, remove it to group correctly
                    if (int.TryParse(parts.Last(), out _))
                    {
                        return string.Join(" ", parts.Take(parts.Length - 1)); // Removes the last numeric part
                    }

                    // If it starts with left/right/front/back, take the last meaningful word
                    if (parts.Length > 1 && (parts[0] == "left" || parts[0] == "right" || parts[0] == "front" || parts[0] == "back"))
                    {
                        return parts.Last(); // Get the main body part name
                    }

                    return bp; // Otherwise, return as-is
                })
                .GroupBy(bp => bp)
                .ToDictionary(g => g.Key, g => g.Count());

            if (!groupedParts.Any())
            {
                return "";
            }

            var irregularPlurals = new Dictionary<string, string>
    {
        { "tooth", "teeth" },
        { "foot", "feet" }
    };

            // Build the parts description
            var partsDescription = groupedParts.Select(part =>
            {
                string partName = part.Value > 1 ? irregularPlurals.GetValueOrDefault(part.Key, part.Key + "s") : part.Key;
                return $"{part.Value} {partName}";
            });

            // Combine into the final string
            return "It has " + string.Join(", ", partsDescription.Take(partsDescription.Count() - 1)) +
                   (partsDescription.Count() > 1 ? ", and " : "") + partsDescription.Last() + ".";
        }



        public static string GenerateUniqueAbbreviation(string raceName, EntityList<Race> existingRaces)
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
