using Lightrealm.GameEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    public class Composition : Entity
    {
        public string Type;
        public string PrimaryDomain;
        public List<Section> Sections;

        public Composition(string type, Architect Author, string Domain)
        {
            Type = type;
            if (Domain == "")
            {
                PrimaryDomain = Engine.Data.Domains[Game1.r.Next(Engine.Data.Domains.Count)];
            }
            else
            {
                PrimaryDomain = Domain;
            }
            Name = GenerateName(type, this.PrimaryDomain);
            Sections = GenerateSections(type, Author);
        }

        public string getDescription(int index)
        {
            if (index < 0 || index >= Sections.Count)
            {
                return "Invalid section index.";
            }
            var section = Sections[index];
            return $"Section {index + 1}: {section.Tone} tone, discussing {string.Join(", ", section.Domains)} with a {section.Perspectives} perspective.";
        }

        public string getCompleteWorkDescription()
        {
            var averageQuality = Sections.Average(s => s.Quality);
            string qualityDescription = GetQualityDescription(averageQuality);

            return $"Title: {Name}\n" +
                   $"Summary: This {Type} is considered {qualityDescription} with an average section quality of {averageQuality:N2}.\n" +
                   $"Sections:\n" + string.Join("\n", Sections.Select((s, i) => $"Section {i + 1}: {s.Tone} tone, discussing {string.Join(", ", s.Domains)} with a {s.Perspectives} perspective."));
        }

        private string GetQualityDescription(double quality)
        {
            int roundedQuality = (int)Math.Round(quality);
            switch (roundedQuality)
            {
                case 0: return "disastrous";
                case 1: return "unbearable";
                case 2: return "painful";
                case 3: return "troublesome";
                case 4: return "mediocre";
                case 5: return "ordinary";
                case 6: return "decent";
                case 7: return "good";
                case 8: return "excellent";
                case 9: return "remarkable";
                case 10: return "extraordinary";
                case 11: return "masterful";
                case 12: return "awe-inspiring";
                case 13: return "transcendent";
                case 14: return "ethereal";
                case 15: return "celestial";
                default: return "supernatural";
            }
        }

        private string GenerateName(string type, string domain)
        {
            // Random titles for each type
            List<string> songTitles = new List<string>
            {
                $"Echoes of {domain}",
                $"Ballad of the {domain}",
                $"Rhythms and {domain}",
                $"Harmonies in {domain}",
                $"Ode to {domain}",
                $"Serenade of {domain}",
                $"Whispers from {domain}",
                $"Crescendo on {domain}",
                $"Melody amidst {domain}",
                $"Chorus for {domain}"
            };

            List<string> poemTitles = new List<string>
            {
                $"A Quill on {domain}",
                $"Sonnets of {domain}",
                $"Glimpses into {domain}",
                $"Rhymes across {domain}",
                $"Verses beneath {domain}",
                $"Reflections on {domain}",
                $"Contours of {domain}",
                $"Echoes around {domain}",
                $"Whispers of {domain}",
                $"Stanzas for {domain}"
            };

            List<string> bookTitles = new List<string>
            {
                $"Tales from {domain}",
                $"Explorations in {domain}",
                $"Dialogues on {domain}",
                $"The {domain} Chronicles",
                $"Insights into {domain}",
                $"Journeys through {domain}",
                $"Narratives of {domain}",
                $"Pages from {domain}",
                $"Studies on {domain}",
                $"Mythos of {domain}"
            };

            switch (type)
            {
                case "song":
                    return songTitles[Game1.r.Next(songTitles.Count)];
                case "poem":
                    return poemTitles[Game1.r.Next(poemTitles.Count)];
                case "book":
                    return bookTitles[Game1.r.Next(bookTitles.Count)];
                default:
                    throw new ArgumentException("Invalid type specified");
            }
        }

        private List<Section> GenerateSections(string type, Architect Author)
        {
            List<Section> sections = new List<Section>();
            int numberSections = type == "book" ? Game1.r.Next(5, 20) : Game1.r.Next(3, 12);

            for (int i = 0; i < numberSections; i++)
            {
                sections.Add(new Section(type, this, Author.Creativity));
            }

            return sections;
        }
    }

    public class Section
    {
        public int Length;
        public List<string> Domains;
        public List<string> Perspectives;
        public string Tone;
        public int Quality;
        public string Type;
        public Composition Parent;  // Assuming this is properly initialized elsewhere

        public Section(string compositionType, Composition parent, int QualityMod)
        {
            this.Parent = parent;

            this.Quality = Game1.r.Next(0, 8) + QualityMod;

            // Expanded list of tones
            List<string> tones = new List<string>
            {
                "Passionate", "Depressed", "Indignant", "Joyful", "Sombre", "Optimistic", "Angry",
                "Serene", "Excited", "Pensive", "Skeptical", "Amused", "Anxious", "Hopeful"
            };

            // Expanded list of perspectives
            List<string> perspectives = new List<string>
            {
                "Admiration", "Criticism", "Neutral", "Nostalgia", "Skepticism", "Affection",
                "Disdain", "Curiosity", "Enthusiasm", "Contempt"
            };

            this.Tone = tones[Game1.r.Next(tones.Count)];
            this.Perspectives = new List<string> { perspectives[Game1.r.Next(perspectives.Count)] };

            // Initialize the Domains with the new domain generation method
            this.Domains = GenerateDomains();

            // Set Length and Type based on the composition type
            if (compositionType == "song")
            {
                this.Length = Game1.r.Next(1, 5);  // Lines in a verse
                this.Type = new List<string> { "verse", "chorus", "bridge" }[Game1.r.Next(3)];
            }
            else if (compositionType == "poem")
            {
                this.Length = Game1.r.Next(4, 10);  // Syllables in a line
                this.Type = "none";
            }
            else if (compositionType == "book")
            {
                this.Length = Game1.r.Next(5, 50);  // Pages in a chapter
                this.Type = "none";
            }
        }

        private List<string> GenerateDomains()
        {
            List<string> generatedDomains = new List<string>();

            // 80% chance to add the primary domain
            if (Game1.r.NextDouble() < 0.8)
            {
                generatedDomains.Add(this.Parent.PrimaryDomain);
            }

            // Additional domains with diminishing returns
            double chanceToAddAdditionalDomain = 0.5;  // Start with 50% chance for the first additional domain
            while (Game1.r.NextDouble() < chanceToAddAdditionalDomain)
            {
                string potentialDomain = Engine.Data.Domains[Game1.r.Next(Engine.Data.Domains.Count)];
                if (!generatedDomains.Contains(potentialDomain))  // Ensure no duplicates
                {
                    generatedDomains.Add(potentialDomain);
                    chanceToAddAdditionalDomain *= 0.5;  // Diminish the chance by half for the next potential domain
                }
            }

            return generatedDomains;
        }
    }
}
