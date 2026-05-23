using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class Composition : Entity
    {
        public string Type { get; set; }
        public Entity Subject;

        private EntityList<Entity> _subjects = new EntityList<Entity>();

        public EntityList<Section> Sections { get; set; }

        public Composition(string type, Architect Author, Entity ChosenEntity)
        {
            Type = type;
            Subject = ChosenEntity;

            if (Subject is Architect a)
            {
                a.UpdateNames();
            }
            else if (Subject is Object o)
            {
                o.UpdateNames(false, null, Game1.LoadedArchitects.Count > 0);
            }

            string domain = Subject.Name ?? Subject.ReferredToNames.FirstOrDefault() ?? "Something";
            Name = GenerateName(type, domain);

            Sections = GenerateSectionsFromSubject(type, Author, Subject);
        }


        public string GetCompleteWorkDescription()
        {
            var averageQuality = Sections.Average(s => s.Quality);
            string qualityDescription = GetQualityDescription(averageQuality);

            var descriptions = Sections.Select(s => s.Description);
            string sectionType = Type switch
            {
                "book" => "Chapter",
                "poem" => "Stanza",
                _ => "Section"
            };

            string FormattedName = Subject.Name != null ? Subject.Name : Subject.ReferredToNames[0];

            return $"The work is a {qualityDescription} {Type}, primarily on {FormattedName}, with {Sections.Count} {sectionType.ToLower()}s. " +
                   string.Join(" ", descriptions);
        }

        private string GetQualityDescription(double quality)
        {
            int roundedQuality = (int)Math.Round(quality);
            return roundedQuality switch
            {
                0 => "disastrous",
                1 => "unbearable",
                2 => "painful",
                3 => "troublesome",
                4 => "mediocre",
                5 => "ordinary",
                6 => "decent",
                7 => "good",
                8 => "excellent",
                9 => "remarkable",
                10 => "extraordinary",
                11 => "masterful",
                12 => "awe-inspiring",
                13 => "transcendent",
                14 => "ethereal",
                15 => "celestial",
                _ => "supernatural"
            };
        }
        public static readonly List<string> Adjectives = new()
        {
            "somber", "inquisitive", "mysterious", "joyful", "melancholic",
            "serene", "vibrant", "epic", "wistful", "enchanting"
        };

            public static readonly Dictionary<string, List<string>> NounsByType = new()
            {
                ["song"] = new List<string>
            {
                "melody", "harmony", "rhythm", "serenade", "ballad",
                "chorus", "lullaby", "symphony", "refrain"
            },
                ["poem"] = new List<string>
            {
                "sonnet", "verse", "stanza", "rhyme", "elegy",
                "couplet", "whisper"
            },
                ["book"] = new List<string>
            {
                "tale", "chronicle", "narrative", "story", "journey",
                "myth", "legend", "account", "fable", "saga"
            }
            };

            public static readonly List<string> NameFormats = new()
        {
            "[a] [n] of [d]",
            "[a] [d] [n]",
            "The [a] [n] in [d]",
            "[n] of the [a] [d]",
            "[d]: A [a] [n]",
            "The [n] of [a] [d]",
            "[a] [d]: [n]",
            "A [n] of [a] [d]",
            "[d]'s [a] [n]",
            "[n] from the [a] [d]"
        };
        private string GenerateName(string type, string domain)
        {
            var adjList = Adjectives;
            var nounList = NounsByType[type];

            string adjective = Game1.Capitalize(adjList[Game1.GameWorld.rnd.Next(adjList.Count)]);
            string noun = Game1.Capitalize(nounList[Game1.GameWorld.rnd.Next(nounList.Count)]);
            string dom = Game1.Capitalize(domain);

            string format = NameFormats[Game1.GameWorld.rnd.Next(NameFormats.Count)];

            return format
                .Replace("[a]", adjective)
                .Replace("[n]", noun)
                .Replace("[d]", dom);
        }


        private EntityList<Section> GenerateSectionsFromSubject(string type, Architect Author, Entity subject)
        {
            EntityList<Section> sections = new EntityList<Section>();
            var events = GetEventsForSubject(subject);

            int numberSections = Math.Min(type == "book" ? Game1.GameWorld.rnd.Next(2, 9) : Game1.GameWorld.rnd.Next(1, 6), events.Count);

            // If no events are found, create a generic section
            if (events.Count == 0)
            {
                sections.Add(new Section(type, this, Author.Creativity, 1, "none"));
            }
            else
            {
                if (type == "song")
                {
                    bool hasChorus = false;
                    bool hasBridge = false;

                    for (int i = 0; i < numberSections; i++)
                    {
                        string sectionType;
                        if (i == 0)
                        {
                            sectionType = "Verse";
                        }
                        else if (!hasChorus && Game1.GameWorld.rnd.NextDouble() < 0.5)
                        {
                            sectionType = "Chorus";
                            hasChorus = true;
                        }
                        else if (hasChorus && !hasBridge && i > 1 && Game1.GameWorld.rnd.NextDouble() < 0.3)
                        {
                            sectionType = "Bridge";
                            hasBridge = true;
                        }
                        else
                        {
                            sectionType = "Verse";
                        }
                        sections.Add(new Section(type, this, Author.Creativity, i + 1, sectionType));
                    }
                }
                else
                {
                    for (int i = 0; i < numberSections; i++)
                    {
                        sections.Add(new Section(type, this, Author.Creativity, i + 1, events[i]));
                    }
                }
            }

            return sections;
        }

        private List<string> GetEventsForSubject(Entity subject)
        {
            string subjectName = subject.Name ?? subject.ReferredToNames.FirstOrDefault();
            var result = new List<string>();

            foreach (var e in subject.AssociatedEvents)
            {
                string yearAndDay = "Unknown";
                string processedEvent = e.EventData;

                int yearStart = e.EventData.IndexOf('(');
                int yearEnd = e.EventData.IndexOf(')', yearStart + 1);
                if (yearStart != -1 && yearEnd != -1 && yearEnd > yearStart)
                {
                    yearAndDay = e.EventData.Substring(yearStart + 1, yearEnd - yearStart - 1);
                }

                int descriptionStart = e.EventData.IndexOf(") ") + 2;
                if (descriptionStart > 1 && descriptionStart < e.EventData.Length)
                {
                    processedEvent = e.EventData.Substring(descriptionStart);
                }

                result.Add($"In {yearAndDay}, {processedEvent}");
            }

            return result;
        }


        public Composition(Entity subject, string name)
        {
            Type = "book";
            Subject = subject;
            Name = name;

            Sections = new EntityList<Section>
            {
                new Section
                {
                    Parent = this,
                    Number = 1,
                    Type = "Chapter",
                    Length = 10,
                    Tone = "informative",
                    Perspectives = new List<string> { "neutral" },
                    Domains = new EntityList<Entity> { subject },
                    Quality = 5,
                    Description = "Section one covers " + subject.Name + " and how it affects shiba inus."
                }
            };
        }


        public Composition()
        {

        }
    }
}
