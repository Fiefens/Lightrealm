using System;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    [Serializable]
    public class Composition : Entity
    {
        public string Type;
        public Entity Subject;
        public List<Section> Sections;

        public Composition(string type, Architect Author, Entity ChosenEntity)
        {
            Type = type;
            Subject = ChosenEntity;
            Name = GenerateName(type, Subject.ReferredToNames[0]);
            Sections = GenerateSectionsFromSubject(type, Author, Subject);
        }

        public string GetCompleteWorkDescription()
        {
            var averageQuality = Sections.Average(s => s.Quality);
            string qualityDescription = GetQualityDescription(averageQuality);

            var descriptions = Sections.Select((s, i) => s.Description).ToList();
            string sectionType = Type switch
            {
                "book" => "Chapter",
                "poem" => "Stanza",
                _ => "Section"
            };
            return $"The work is a {qualityDescription} {Type}, primarily on {Subject.ReferredToNames[0]}, with {Sections.Count} {sectionType.ToLower()}s. " +
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

        private string GenerateName(string type, string domain)
        {
            List<string> adjectives = new List<string>
            {
                "somber",
                "inquisitive",
                "mysterious",
                "joyful",
                "melancholic",
                "serene",
                "vibrant",
                "epic",
                "wistful",
                "enchanting"
            };

            List<string> songNouns = new List<string>
            {
                "melody",
                "harmony",
                "rhythm",
                "serenade",
                "ballad",
                "chorus",
                "lullaby",
                "symphony",
                "refrain",
                "cantata"
            };

            List<string> poemNouns = new List<string>
            {
                "sonnet",
                "verse",
                "stanza",
                "rhyme",
                "elegy",
                "couplet",
                "whisper"
            };

            List<string> bookNouns = new List<string>
            {
                "tale",
                "chronicle",
                "narrative",
                "story",
                "journey",
                "myth",
                "legend",
                "account",
                "fable",
                "saga"
            };

            string adjective = Game1.Capitalize(adjectives[Game1.r.Next(adjectives.Count)]);
            string noun = Game1.Capitalize(type switch
            {
                "song" => songNouns[Game1.r.Next(songNouns.Count)],
                "poem" => poemNouns[Game1.r.Next(poemNouns.Count)],
                "book" => bookNouns[Game1.r.Next(bookNouns.Count)],
                _ => throw new ArgumentException("Invalid type specified")
            });

            domain = Game1.Capitalize(domain);

            List<string> formats = new List<string>
            {
                $"{adjective} {noun} of {domain}",
                $"{adjective} {domain} {noun}",
                $"The {adjective} {noun} in {domain}",
                $"{noun} of the {adjective} {domain}",
                $"{domain}: A {adjective} {noun}",
                $"The {noun} of {adjective} {domain}",
                $"{adjective} {domain}: {noun}",
                $"A {noun} of {adjective} {domain}",
                $"{domain}'s {adjective} {noun}",
                $"{noun} from the {adjective} {domain}"
            };

            string format = formats[Game1.r.Next(formats.Count)];
            return format.Replace("{adjective}", adjective).Replace("{noun}", noun).Replace("{domain}", domain);
        }

        private List<Section> GenerateSectionsFromSubject(string type, Architect Author, Entity subject)
        {
            List<Section> sections = new List<Section>();
            var events = GetEventsForSubject(subject);
            int numberSections = Math.Min(type == "book" ? Game1.r.Next(5, 20) : Game1.r.Next(3, 12), events.Count);

            if (type == "song")
            {
                List<string> sectionTypes = new List<string> { "Verse" };
                bool hasChorus = false;
                bool hasBridge = false;

                for (int i = 0; i < numberSections; i++)
                {
                    string sectionType;
                    if (i == 0)
                    {
                        sectionType = "Verse";
                    }
                    else if (!hasChorus && Game1.r.NextDouble() < 0.5)
                    {
                        sectionType = "Chorus";
                        hasChorus = true;
                    }
                    else if (hasChorus && !hasBridge && i > 1 && Game1.r.NextDouble() < 0.3)
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

            return sections;
        }

        private Entity GenerateRandomSubject()
        {
            var random = new Random();
            List<Entity> subjects = new List<Entity>();

            subjects.AddRange(Game1.GameWorld.AllArchitects);
            subjects.AddRange(Game1.GameWorld.AllLocations);
            subjects.AddRange(Game1.GameWorld.AllLocations.SelectMany(loc => loc.AllStructures));
            subjects.AddRange(Game1.GameWorld.AllLocations.SelectMany(loc => loc.AllStructures.SelectMany(structure => structure.HistoricalObjects)));

            return subjects[random.Next(subjects.Count)];
        }

        private List<string> GetEventsForSubject(Entity subject)
        {
            string subjectName = subject.ReferredToNames[0];

            var events = Game1.GameWorld.HistoricalEvents
                .Where(e => e.Contains(subjectName))
                .Select(e =>
                {
                    // Extract the year and day from the event string
                    int yearStart = e.IndexOf("(") + 1;
                    int yearEnd = e.IndexOf(")", yearStart);
                    string yearAndDay = e.Substring(yearStart, yearEnd - yearStart);

                    // Extract the event description after the year and day part
                    int descriptionStart = e.IndexOf(") ") + 2;
                    string processedEvent = e.Substring(descriptionStart);

                    return $"In {yearAndDay}, {processedEvent}";
                })
                .ToList();

            return events;
        }
    }

    [Serializable]
    public class Section
    {
        public int Length;
        public List<string> Domains;
        public List<string> Perspectives;
        public string Tone;
        public int Quality;
        public string Type;
        public string Description;
        public Composition Parent;
        public int Number;

        public Section(string compositionType, Composition parent, int QualityMod, int number, string sectionType = "none")
        {
            this.Parent = parent;
            this.Number = number;
            this.Quality = Game1.r.Next(0, 8) + QualityMod;

            List<string> tones = new List<string>
            {
                "passionate", "depressed", "indignant", "joyful", "sombre", "optimistic", "angry",
                "serene", "excited", "pensive", "skeptical", "amused", "anxious", "hopeful"
            };

            List<string> perspectives = new List<string>
            {
                "admirable", "criticizing", "neutral", "nostalgic", "skeptical", "affectionate",
                "disdainful", "curious", "enthusiastic", "contemptuous"
            };

            this.Tone = tones[Game1.r.Next(tones.Count)];
            this.Perspectives = new List<string> { perspectives[Game1.r.Next(perspectives.Count)] };
            this.Domains = GenerateDomains();

            this.Type = sectionType;

            if (compositionType == "song" && sectionType == "none")
            {
                this.Type = new List<string> { "verse", "chorus", "bridge" }[Game1.r.Next(3)];
            }
            else if (compositionType == "poem")
            {
                this.Length = Game1.r.Next(4, 10);
                this.Type = "Stanza";
            }
            else if (compositionType == "book")
            {
                this.Length = Game1.r.Next(5, 50);
                this.Type = "Chapter";
            }

            GenerateDescription();
        }

        public Section(string compositionType, Composition parent, int QualityMod, int number, string eventDescription, string sectionType = "none")
        {
            this.Parent = parent;
            this.Number = number;
            this.Quality = Game1.r.Next(0, 8) + QualityMod;

            List<string> tones = new List<string>
            {
                "passionate", "depressed", "indignant", "joyful", "sombre", "optimistic", "angry",
                "serene", "excited", "pensive", "skeptical", "amused", "anxious", "hopeful"
            };

            List<string> perspectives = new List<string>
            {
                "admirable", "criticizing", "neutral", "nostalgic", "skeptical", "affectionate",
                "disdainful", "curious", "enthusiastic", "contemptuous"
            };

            this.Tone = tones[Game1.r.Next(tones.Count)];
            this.Perspectives = new List<string> { perspectives[Game1.r.Next(perspectives.Count)] };
            this.Domains = new List<string> { Parent.Subject.ReferredToNames[0] };

            this.Type = sectionType;

            if (compositionType == "song" && sectionType == "none")
            {
                this.Type = new List<string> { "verse", "chorus", "bridge" }[Game1.r.Next(3)];
            }
            else if (compositionType == "poem")
            {
                this.Length = Game1.r.Next(4, 10);
                this.Type = "Stanza";
            }
            else if (compositionType == "book")
            {
                this.Length = Game1.r.Next(5, 50);
                this.Type = "Chapter";
            }

            GenerateDescription(eventDescription);
        }

        private void GenerateDescription()
        {
            List<string> formats = new List<string>
            {
                $"In {Type} {Number}, the {Tone} tone is evident as it explores {Game1.FormatList(Domains)} from a {Game1.FormatList(Perspectives)} perspective.",
                $"{Type} {Number} delves into {Game1.FormatList(Domains)} with a {Tone} tone, offering insights from a {Game1.FormatList(Perspectives)} viewpoint.",
                $"With a {Tone} tone, {Type} {Number} covers {Game1.FormatList(Domains)} and reflects on it with a {Game1.FormatList(Perspectives)} perspective.",
                $"{Type} {Number} sets a {Tone} mood while discussing {Game1.FormatList(Domains)} through a {Game1.FormatList(Perspectives)} lens.",
                $"The {Tone} tone of {Number} {Type} illuminates {Game1.FormatList(Domains)}, viewed from a {Game1.FormatList(Perspectives)} angle."
            };

            string format = formats[Game1.r.Next(formats.Count)];
            Description = format;
        }

        private void GenerateDescription(string eventDescription)
        {
            List<string> formats = new List<string>
            {
                $"In {Type} {Number}, the {Tone} tone is evident as it recounts when {eventDescription}, from a {Game1.FormatList(Perspectives)} perspective.",
                $"{Type} {Number} delves into when {eventDescription} with a {Tone} tone, offering insights from a {Game1.FormatList(Perspectives)} viewpoint.",
                $"With a {Tone} tone, {Type} {Number} covers when {eventDescription} and reflects on it with a {Game1.FormatList(Perspectives)} perspective.",
                $"{Type} {Number} sets a {Tone} mood while discussing when {eventDescription} through a {Game1.FormatList(Perspectives)} lens.",
                $"The {Tone} tone of {Type} {Number} illuminates when {eventDescription}, viewed from a {Game1.FormatList(Perspectives)} angle."
            };

            string format = formats[Game1.r.Next(formats.Count)];
            Description = format;
        }

        private List<string> GenerateDomains()
        {
            List<string> generatedDomains = new List<string>();

            if (Game1.r.NextDouble() < 0.8)
            {
                generatedDomains.Add(this.Parent.Subject.ReferredToNames[0]);
            }

            double chanceToAddAdditionalDomain = 0.5;
            while (Game1.r.NextDouble() < chanceToAddAdditionalDomain)
            {
                string potentialDomain = Game1.Domains[Game1.r.Next(Game1.Domains.Count)];
                if (!generatedDomains.Contains(potentialDomain))
                {
                    generatedDomains.Add(potentialDomain);
                    chanceToAddAdditionalDomain *= 0.5;
                }
            }

            return generatedDomains;
        }
    }
}
