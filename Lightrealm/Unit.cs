using Lightrealm;
using Microsoft.Xna.Framework.Input;
using nFMOD;
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
    public class Unit : Entity
    {
        private int _regionId;

        public Region Region
        {
            get => EntityGet<Region>(_regionId);
            set => _regionId = value?.ID ?? 0;
        }

        public string Type { get; set; }

        public Location TargetLocation = null;
        public Region WanderRegion = null;

        public int TravelPoints = 0;

        public string Info { get; set; }
        public string Intrigue { get; set; }

        public EntityList<Architect> UnitArchitects { get; set; } = new EntityList<Architect>();

        public int Luminosity { get; set; } = 0;

        public Unit(Region region, string type, EntityList<Architect> Architects, Location targetLocation = null)
        {
            Region = region;
            Name = Game1.GameWorld.GenerateUniqueName("1S9s", this, Game1.GameWorld.rnd);
            Type = type;

            foreach(Architect a in Architects)
            {
                a.Unit = this;
            }

            TargetLocation = targetLocation;

            UnitArchitects = Architects;

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
                                {"ocean", new List<string>
                                    {
                                        "A dark ship looms on the horizon, its sails full despite the still air.",
                                        "The sound of creaking wood and flapping sails suggests you're not alone on these waters.",
                                        "A shadowy vessel cuts through the waves, heading in your direction.",
                                        "Distant noises echo across the open sea, sending a chill down your spine.",
                                        "A flag is spotted in the distance, drawing closer with each passing moment."
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
                    Info = new List<string>() { "You happen upon a group of bandits.", "You spot a gang of bandits approaching the area.", "You hear some mumbling, and sight an outlaw gang traveling through the area." }[Game1.GameWorld.rnd.Next(0, 3)];
                    break;
                case "shadebeast":
                    Info = new List<string>() { "A corrupted hulk of rotting matter stands shaking before you. It hasn't seen you yet.", "As the sun sets, you spy a foul beast in the night, unaware of your arrival.", "Ahead of you stands a dark corrupted beast, oblivious to your presence." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for shadebeast
                    break;
                case "construct":
                    Info = new List<string>() { "Some sort of powerful mechanical creature stands before you. It seems to be guarding something.", "Some magical, powerful, mysterious construct marches through the area. It seems to have some kind of task.", "Before you is a mysterious construct, silently watching over its domain. It looks incredibly powerful." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for construct
                    break;
                case "wildcreatures":
                    Info = new List<string>() { "You hear some creatures rustling in the bushes.", "You hear a distant noise, sounds like some of the local fauna.", "You hear some kind of wild noise from afar." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for wildcreatures
                    break;
                case "traders":
                    Info = new List<string>() { "You see some traders, traveling a lesser travelled road to somewhere unknown. Could have useful items to purchase, or otherwise...", "Some sort of merchant, or maybe multiple for all you know, is headed " + Door.OrthogonalDoorDirections[Game1.GameWorld.rnd.Next(4)] + ".", "A group of traders emerges from an unfamiliar route, their purpose unclear." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for traders
                    break;
                case "vagabond":
                    Info = new List<string>() { "A lonely traveler seems to be headed somewhere. Perhaps they can help you somehow.", "A vagabond walks alone, their intentions unknown to you.", "Before you is a solitary wanderer. They at least appears harmless..." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for vagabond
                    break;
                case "adventurer":
                    Info = new List<string>() { "You cross paths with someone who appears equipped for adventure.", "A well prepared traveler stands before you, flaunting their satchel. Could be useful.", "You see a traveler who appears to be well equipped for adventure." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for adventurer
                    break;
                case "shiba":
                    Info = new List<string>() { "A fluffy, four legged creature is wandering the area. Perhaps you can attach it to your face.", "A beautiful creature wanders this area, searching for someone's face to meld with.", "You see both a lovely creature and a lovely piece of headwear." }[Game1.GameWorld.rnd.Next(0, 3)];
                    // Code for adventurer
                    break;
                case "priest":
                    Info = new List<string>() { "A lone traveler in religious clothing appears. They appear to be carrying something... shiny.", "A priestly figure approaches, a glint of something shiny in their possession.", "In religious attire, a lone priest walks your way, a mysterious gleam catching your eye." }[Game1.GameWorld.rnd.Next(0, 3)];
                    break;
                // Code for priest
                case "colossal":
                    Info = new List<string>() { "You had only heard of the legendary " + UnitArchitects[0].Race.Name + " " + UnitArchitects[0].Name + ", but it stands before you now...", "That, that is a big " + UnitArchitects[0].Race.Name + "... you've heard of " + UnitArchitects[0].Name + " before, but seeing it is different. It hasn't spotted you yet.", }[Game1.GameWorld.rnd.Next(0, 2)];
                    break;
                default:
                    Info = "An unknown traveler stands before you. Their purpose is unclear.";
                    break;
            }

            Intrigue = BiomeToIntrigue[region.Biome][Game1.GameWorld.rnd.Next(BiomeToIntrigue[region.Biome].Count())];
        }

        public Unit()
        {

        }
    }
}
