using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Section : Entity
    {
        public int Length { get; set; }

        public EntityList<Entity> Domains { get; set; } = new EntityList<Entity>();
        public Composition Parent;

        public List<string> Perspectives { get; set; } = new List<string>();
        public string Tone { get; set; }
        public int Quality { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        public int Number { get; set; }

        public Section(string compositionType, Composition parent, int QualityMod, int number, string sectionType = "none")
        {
            this.Parent = parent;
            this.Number = number;
            this.Quality = Game1.GameWorld.rnd.Next(0, 8) + QualityMod;

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

            this.Tone = tones[Game1.GameWorld.rnd.Next(tones.Count())];
            this.Perspectives = new List<string> { perspectives[Game1.GameWorld.rnd.Next(perspectives.Count())] };
            this.Domains = GenerateDomains();

            this.Type = sectionType;

            if (compositionType == "song" && sectionType == "none")
            {
                this.Type = new List<string> { "verse", "chorus", "bridge" }[Game1.GameWorld.rnd.Next(3)];
            }
            else if (compositionType == "poem")
            {
                this.Length = Game1.GameWorld.rnd.Next(4, 10);
                this.Type = "Stanza";
            }
            else if (compositionType == "book")
            {
                this.Length = Game1.GameWorld.rnd.Next(5, 50);
                this.Type = "Chapter";
            }

            GenerateDescription();
        }

        public Section()
        {

        }

        public Section(string compositionType, Composition parent, int QualityMod, int number, string eventDescription = null, string sectionType = "none")
        {
            this.Parent = parent;
            this.Number = number;
            this.Quality = Game1.GameWorld.rnd.Next(0, 8) + QualityMod;

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

            this.Tone = tones[Game1.GameWorld.rnd.Next(tones.Count())];
            this.Perspectives = new List<string> { perspectives[Game1.GameWorld.rnd.Next(perspectives.Count())] };
            this.Domains = new EntityList<Entity> { Parent.Subject };

            this.Type = sectionType;

            if (compositionType == "song" && sectionType == "none")
            {
                this.Type = new List<string> { "verse", "chorus", "bridge" }[Game1.GameWorld.rnd.Next(3)];
            }
            else if (compositionType == "poem")
            {
                this.Length = Game1.GameWorld.rnd.Next(4, 10);
                this.Type = "Stanza";
            }
            else if (compositionType == "book")
            {
                this.Length = Game1.GameWorld.rnd.Next(5, 50);
                this.Type = "Chapter";
            }

            if (string.IsNullOrEmpty(eventDescription))
            {
                GenerateGenericDescription();
            }
            else
            {
                GenerateDescription(eventDescription);
            }
        }

        private void GenerateDescription()
        {
            List<string> formats = new List<string>
            {
                $"In {Type} {Number}, the {Tone} tone is evident as it explores {Game1.FormatList(Domains)} from a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} delves into {Game1.FormatList(Domains)} with a {Tone} tone, offering insights from a {Game1.FormatAndList(Perspectives)} viewpoint.",
                $"With a {Tone} tone, {Type} {Number} covers {Game1.FormatList(Domains)} and reflects on it with a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} sets a {Tone} mood while discussing {Game1.FormatList(Domains)} through a {Game1.FormatAndList(Perspectives)} lens.",
                $"The {Tone} tone of {Number} {Type} illuminates {Game1.FormatList(Domains)}, viewed from a {Game1.FormatAndList(Perspectives)} angle."
            };

            string format = formats[Game1.GameWorld.rnd.Next(formats.Count())];
            Description = format;
        }

        private void GenerateGenericDescription()
        {
            List<string> formats = new List<string>
            {
                $"In {Type} {Number}, the {Tone} tone is evident as it explores {Game1.FormatList(Domains)} from a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} delves into {Game1.FormatList(Domains)} with a {Tone} tone, offering insights from a {Game1.FormatAndList(Perspectives)} viewpoint.",
                $"With a {Tone} tone, {Type} {Number} covers {Game1.FormatList(Domains)} and reflects on it with a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} sets a {Tone} mood while discussing {Game1.FormatList(Domains)} through a {Game1.FormatAndList(Perspectives)} lens.",
                $"The {Tone} tone of {Number} {Type} illuminates {Game1.FormatList(Domains)}, viewed from a {Game1.FormatAndList(Perspectives)} angle."
            };

            string format = formats[Game1.GameWorld.rnd.Next(formats.Count())];
            Description = format;
        }

        private void GenerateDescription(string eventDescription)
        {
            List<string> formats = new List<string>
            {
                $"In {Type} {Number}, the {Tone} tone is evident as it recounts when {eventDescription}, from a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} delves into when {eventDescription} with a {Tone} tone, offering insights from a {Game1.FormatAndList(Perspectives)} viewpoint.",
                $"With a {Tone} tone, {Type} {Number} covers when {eventDescription} and reflects on it with a {Game1.FormatAndList(Perspectives)} perspective.",
                $"{Type} {Number} sets a {Tone} mood while discussing when {eventDescription} through a {Game1.FormatAndList(Perspectives)} lens.",
                $"The {Tone} tone of {Type} {Number} illuminates when {eventDescription}, viewed from a {Game1.FormatAndList(Perspectives)} angle."
            };

            string format = formats[Game1.GameWorld.rnd.Next(formats.Count())];
            Description = format;
        }

        private EntityList<Entity> GenerateDomains()
        {
            EntityList<Entity> generatedDomains = new EntityList<Entity>();

            if (Game1.GameWorld.rnd.NextDouble() < 0.8)
            {
                generatedDomains.Add(this.Parent.Subject);
            }

            double chanceToAddAdditionalDomain = 0.5;
            while (Game1.GameWorld.rnd.NextDouble() < chanceToAddAdditionalDomain)
            {
                Entity potentialDomain = Game1.GameWorld.Domains[Game1.GameWorld.rnd.Next(Game1.GameWorld.Domains.Count())];
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
