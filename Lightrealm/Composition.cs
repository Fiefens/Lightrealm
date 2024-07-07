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

        private int _subjectId;

        
        public Entity Subject
        {
            get => EntityGet<Entity>(_subjectId);
            set => _subjectId = value?.ID ?? 0;
        }

        private List<Entity> _subjects = new List<Entity>();

        public List<Section> Sections { get; set; }

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

            var descriptions = Sections.Select((s, i) => s.Description);
            string sectionType = Type switch
            {
                "book" => "Chapter",
                "poem" => "Stanza",
                _ => "Section"
            };
            return $"The work is a {qualityDescription} {Type}, primarily on {Subject.ReferredToNames[0]}, with {Sections.Count()} {sectionType.ToLower()}s. " +
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
                "refrain"
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

            string adjective = Game1.Capitalize(adjectives[Game1.r.Next(adjectives.Count())]);
            string noun = Game1.Capitalize(type switch
            {
                "song" => songNouns[Game1.r.Next(songNouns.Count())],
                "poem" => poemNouns[Game1.r.Next(poemNouns.Count())],
                "book" => bookNouns[Game1.r.Next(bookNouns.Count())],
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

            string format = formats[Game1.r.Next(formats.Count())];
            return format.Replace("{adjective}", adjective).Replace("{noun}", noun).Replace("{domain}", domain);
        }

        private List<Section> GenerateSectionsFromSubject(string type, Architect Author, Entity subject)
        {
            List<Section> sections = new List<Section>();
            var events = GetEventsForSubject(subject);

            int numberSections = Math.Min(type == "book" ? Game1.r.Next(5, 20) : Game1.r.Next(3, 12), events.Count());

            // If no events are found, create a generic section
            if (events.Count() == 0)
            {
                sections.Add(new Section(type, this, Author.Creativity, 1, "none"));
            }
            else
            {
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

            return subjects[random.Next(subjects.Count())];
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
                ;

            return events;
        }

        public Composition()
        {

        }
    }
}
