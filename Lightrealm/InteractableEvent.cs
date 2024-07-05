using Lightrealm;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class InteractableEvent : Entity
    {
        private int _regionId;

        [JsonIgnore]
        public Region Region
        {
            get => EntityGet<Region>(_regionId);
            set => _regionId = value?.ID ?? 0;
        }

        public int MonthsBeforeDecay { get; set; }
        public string Type { get; set; }

        private int _homeCivilizationId;

        [JsonIgnore]
        public Civilization HomeCivilization
        {
            get => EntityGet<Civilization>(_homeCivilizationId);
            set => _homeCivilizationId = value?.ID ?? 0;
        }

        public string Info { get; set; }
        public string Intrigue { get; set; }

        public EntityList<Architect> GuaranteedArchitects { get; set; } = new EntityList<Architect>();

        public int Luminosity { get; set; } = 0;

        public InteractableEvent(Region region, int monthsBeforeDecay, string type, Civilization civ, EntityList<Architect> guaranteedArchitects)
        {
            Region = region;
            Name = Region.World.GenerateUniqueName("1S9s", this);
            MonthsBeforeDecay = monthsBeforeDecay;
            Type = type;
            HomeCivilization = civ;

            GuaranteedArchitects = guaranteedArchitects;

            Dictionary<string, List<string>> BiomeToIntrigue = new Dictionary<string, List<string>>
                            {
                                {"desert", new List<string>
                                    {
                                        "Shifting shadows catch your eye in the distance.",
                                        "Faint noises carry on the desert wind.",
                                        "An unseen presence appears across the dunes.",
                                        "Someone is following you, or could be heading the same way?",
                                        "You see something over a nearby hill."
                                    }
                                },
                                {"forest", new List<string>
                                    {
                                        "Something is stirring among the trees.",
                                        "Something is passing through the depths of the foliage.",
                                        "A rustle in the leaves hints at an unknown presence.",
                                        "You see something beyond the treeline.",
                                        "Something moves beyond the forest."
                                    }
                                },
                                {"lightforest", new List<string>
                                    {
                                        "Something is stirring among the trees.",
                                        "Something is passing through the depths of the foliage.",
                                        "A rustle in the leaves hints at an unknown presence.",
                                        "You see something beyond the treeline.",
                                        "Something moves beyond the forest."
                                    }
                                },
                                {"mountain", new List<string>
                                    {
                                        "Echoes of distant footsteps reverberate through the peaks.",
                                        "Silhouettes move along the mountainous horizon.",
                                        "You hear echoes over a nearby hill.",
                                        "You see the sun reflect off something nearby.",
                                        "You spy something from the craggy heights."
                                    }
                                },
                                {"plains", new List<string>
                                    {
                                        "The vast fields seem alive with hidden stirrings.",
                                        "Whispers of something unseen ride on the gentle breeze.",
                                        "A distant call echoes across the serene landscape.",
                                        "Something can be seen from afar.",
                                        "The horizon beckons, promising secrets yet unveiled."
                                    }
                                },
                                {"snowpeak", new List<string>
                                    {
                                        "Shadows dance in the snow-capped peaks.",
                                        "You see footprints. You might be able to follow them.",
                                        "Unnatural noises echo through the frozen silence.",
                                        "The icy heights conceal something from afar.",
                                        "The wind whispers with noise of activity from afar."
                                    }
                                },
                                {"taiga", new List<string>
                                    {
                                        "The piney air whispers of hidden creatures.",
                                        "Tracks lead into the depths of the mysterious forest.",
                                        "Distant calls hint at the unknown.",
                                        "The taiga conceals potential friend or foe.",
                                        "Something is behind you, breathing from the depths of the trees."
                                    }
                                },
                                {"tundra", new List<string>
                                    {
                                        "The frozen winds carry tales of unseen travelers.",
                                        "Footprints in the snow reveal a silent presence.",
                                        "The icy plains seem to conceal elusive figures.",
                                        "The silence of the tundra is broken by something distant.",
                                        "Something ahead breaks the flatness of the frozen landscape."
                                    }
                                },
                                {"ethereal", new List<string>
                                    {
                                        "Something is hiding in this disaster of unparalleled proportions."
                                    }
                                }
                            };

            switch (Type)
            {
                case "bandits":
                    Info = new List<string>() { "You happen upon a group of bandits.", "You spot a gang of bandits approaching the area.", "You hear some mumbling, and sight an outlaw gang travelling through the area." }[Game1.r.Next(0, 3)];
                    break;
                case "shadebeast":
                    Info = new List<string>() { "A corrupted hulk of rotting matter stands shaking before you. It hasn't seen you yet.", "As the sun sets, you spy a foul beast in the night, unaware of your arrival.", "Ahead of you stands a dark corrupted beast, oblivious to your presence." }[Game1.r.Next(0, 3)];
                    // Code for shadebeast
                    break;
                case "construct":
                    Info = new List<string>() { "Some sort of powerful mechanical creature stands before you. It seems to be guarding something.", "Some magical, powerful, mysterious construct marches through the area. It seems to have some kind of task.", "Before you is a mysterious construct, silently watching over its domain. It looks incredibly powerful." }[Game1.r.Next(0, 3)];
                    // Code for construct
                    break;
                case "wildcreatures":
                    Info = new List<string>() { "You hear some creatures rustling in the bushes.", "You hear a distant noise, sounds like some of the local fauna.", "You hear some kind of wild noise from afar." }[Game1.r.Next(0, 3)];
                    // Code for wildcreatures
                    break;
                case "traders":
                    Info = new List<string>() { "You see some traders, travelling a lesser travelled road to somewhere unknown. Could have useful items to purchase, or otherwise...", "Some sort of merchant, or maybe multiple for all you know, is headed " + Door.OrthogonalDoorDirections[Game1.r.Next(4)] + ".", "A group of traders emerges from an unfamiliar route, their purpose unclear." }[Game1.r.Next(0, 3)];
                    // Code for traders
                    break;
                case "vagabond":
                    Info = new List<string>() { "A lonely traveller seems to be headed somewhere. Perhaps they can help you somehow.", "A vagabond walks alone, their intentions unknown to you.", "Before you is a solitary wanderer. They at least appears harmless..." }[Game1.r.Next(0, 3)];
                    // Code for vagabond
                    break;
                case "adventurer":
                    Info = new List<string>() { "You cross paths with someone who appears equipped for adventure.", "A well prepared traveller stands before you, flaunting their satchel. Could be useful.", "You see a traveler who appears to be well equipped for adventure." }[Game1.r.Next(0, 3)];
                    // Code for adventurer
                    break;
                case "shiba":
                    Info = new List<string>() { "A fluffy, four legged creature is wandering the area. Perhaps you can attach it to your face.", "A beautiful creature wanders this area, searching for someone's face to meld with.", "You see both a lovely creature and a lovely piece of headwear." }[Game1.r.Next(0, 3)];
                    // Code for adventurer
                    break;
                case "priest":
                    Info = new List<string>() { "A lone traveller in religious clothing appears. They appear to be carrying something... shiny.", "A priestly figure approaches, a glint of something shiny in their possession.", "In religious attire, a lone priest walks your way, a mysterious gleam catching your eye." }[Game1.r.Next(0, 3)];
                    break;
                // Code for priest
                case "colossal":
                    Info = new List<string>() { "You had only heard of the legendary " + GuaranteedArchitects[0].Race.Name + " " + GuaranteedArchitects[0].Name + ", but it stands before you now...", "That, that is a big " + GuaranteedArchitects[0].Race.Name + "... you've heard of " + GuaranteedArchitects[0].Name + " before, but seeing it is different. It hasn't spotted you yet.", }[Game1.r.Next(0, 2)];
                    break;
                default:
                    // Code for the default case (if DecidedType doesn't match any of the specified cases)
                    break;
            }

            Intrigue = BiomeToIntrigue[region.Biome][Game1.r.Next(BiomeToIntrigue[region.Biome].Count)];
        }

        public InteractableEvent()
        {

        }
    }
}
