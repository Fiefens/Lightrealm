using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Lightrealm
{
    public class WorldActionInitiator
    {

        public static Dictionary<string, int> ActionDifficulty = new Dictionary<string, int>
{
    // Stealth Actions
    { "spreaddisease", 100 }, // Challenging but feasible for a skilled individual.
    { "diplomance", 80 }, // Difficult but manageable for one person.
    { "corrupt", 70 }, // Corruption involves risk but is within reach for an individual.
    { "artifacttheft", 130 }, // Still very difficult, requiring high skill and planning.
    { "theft", 50 }, // Generic thievery, easier for a single person.
    { "embezzlement", 90 }, // Embezzlement is complex but achievable by a skilled person.

    // Direct Actions
    { "harvest", 110 }, // Harvesting energy is difficult but within reach.
    { "razebuilding", 70 }, // Destroying a building requires effort but is doable by a few people.
    { "rupture", 160 }, // A catastrophic rupture remains very difficult, nearly impossible for one person.

    // Actions Using Higher of Stealth or Direct
    { "takeover", 180 }, // Taking over a government is extremely challenging, requiring vast resources.
    { "killassorted", 80 }, // Killing various targets presents challenges but is achievable.
    { "kidnapassorted", 90 }, // Kidnapping multiple targets requires coordination but is feasible.
    { "kidnaptarget", 110 }, // Kidnapping a specific target is challenging.
    { "killtarget", 120 }, // Killing a specific target is difficult but possible with skill.
    { "incite", 140 }, // Inciting a war or revolution is very difficult.
    { "hunterfight", 90 }, // Fighting a dangerous creature is challenging but within reach.
    { "adventure", 60 }, // Adventuring requires effort but is achievable by individuals.

    // Simple Actions
    { "negotiate", 0 }, // Negotiating is straightforward and easy.
    { "visitation", 0 }, // Visiting someone is simple and requires minimal effort.
    { "craftsmanship", 0 }, // Crafting is easy and straightforward.
    { "marriage", 0 }, // Getting married is a simple and low-difficulty task.
    { "childbirth", 0 } // Childbirth is a natural process and easy in this context.
};

        public static Dictionary<string, string> ActionSkillType = new Dictionary<string, string>
{
    // Stealth Actions
    { "spreaddisease", "stealth" },
    { "diplomance", "stealth" },
    { "corrupt", "stealth" },
    { "artifacttheft", "stealth" },
    { "theft", "stealth" },
    { "embezzlement", "stealth" },

    // Direct Actions
    { "harvest", "direct" },
    { "razebuilding", "direct" },
    { "rupture", "direct" },

    // Actions Using Higher of Stealth or Direct
    { "takeover", "choose" },
    { "killassorted", "choose" },
    { "kidnapassorted", "choose" },
    { "kidnaptarget", "choose" },
    { "killtarget", "choose" },
    { "incite", "choose" },
    { "hunterfight", "choose" },
    { "adventure", "choose" },

    // Simple Actions
    { "negotiate", "simple" },
    { "visitation", "simple" },
    { "craftsmanship", "simple" },
    { "marriage", "simple" },
    { "childbirth", "simple" }
};



        public static void InitiateAction(World GameWorld, string Action, Entity Initiator, EntityList<Entity> RelatedEntities)
        {
            int StealthEffectiveness = 0;
            int DirectEffectiveness = 0;
            int FinalEffectiveness = 0;
            string ApproachUsed = ""; // To store whether stealth or direct was used for "choose" actions
            string Result = ""; // The result string: "MaV", "MiV", "MiD", or "MaD"

            int Month = ((int)Math.Round((decimal)(GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = "(" + Month + "/" + Year + ")";

            // Get the type of action (stealth, direct, choose, simple)
            string ActionType = ActionSkillType[Action];

            // Determine effectiveness based on action type
            if (ActionType == "stealth")
            {
                StealthEffectiveness = GetEntityEffectiveness("stealth", Initiator);
                FinalEffectiveness = StealthEffectiveness;
            }
            else if (ActionType == "direct")
            {
                DirectEffectiveness = GetEntityEffectiveness("direct", Initiator);
                FinalEffectiveness = DirectEffectiveness;
            }
            else if (ActionType == "choose")
            {
                StealthEffectiveness = GetEntityEffectiveness("stealth", Initiator);
                DirectEffectiveness = GetEntityEffectiveness("direct", Initiator);

                // Choose the higher of stealth or direct
                if (StealthEffectiveness > DirectEffectiveness)
                {
                    FinalEffectiveness = StealthEffectiveness;
                    ApproachUsed = "stealth";
                }
                else
                {
                    FinalEffectiveness = DirectEffectiveness;
                    ApproachUsed = "direct";
                }
            }
            else if (ActionType == "simple")
            {
                FinalEffectiveness = 1000; // Automatically successful for simple actions
            }

            // Add a random modifier between 1 and 100
            FinalEffectiveness += Game1.r.Next(0, 101);

            // Determine the difficulty of the action
            int Difficulty = ActionDifficulty[Action];

            // Calculate the result based on effectiveness and difficulty
            int Difference = FinalEffectiveness - Difficulty;

            if (Difference > 40)
            {
                Result = "MaV"; // Major Victory
            }
            else if (Difference > 0)
            {
                Result = "MiV"; // Minor Victory
            }
            else if (Difference > -40)
            {
                Result = "MiD"; // Minor Defeat
            }
            else
            {
                Result = "MaD"; // Major Defeat
            }


            bool Noticed = false;

            if (ApproachUsed == "direct" || Difference > 0)
            {
                Noticed = true;
            }




            void LogEvent(string data, Region r, EntityList<Entity> e)
            {
                string eventText = $"{Date} {data}";
                if (Initiator is Architect a && GameWorld.Calamity.Contains(a) && a.Level <= 4)
                {
                    GameWorld.HistoricalEvents.Add(new Event(eventText, r, e, true));
                }
                else
                {
                    GameWorld.HistoricalEvents.Add(new Event(eventText, r, e));
                }
            }

            if (Action == "spreaddisease")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                if (location.Region.Blight != ((Architect)Initiator).BlightManipulated)
                {
                    // Log different messages based on the result of the action
                    if (Result == "MaV")
                    {
                        LogEvent(Initiator.Name + " easily spread the " + ((Architect)Initiator).BlightManipulated.Name + " to " + location.Name + ".", location.Region, new EntityList<Entity>(){Initiator, ((Architect)Initiator).BlightManipulated, location});
                    }
                    else if (Result == "MiV")
                    {
                        LogEvent(Initiator.Name + " successfully spread the " + ((Architect)Initiator).BlightManipulated.Name + " to " + location.Name + " with some effort.", location.Region, new EntityList<Entity>() { Initiator, ((Architect)Initiator).BlightManipulated, location });
                    }
                    else if (Result == "MiD")
                    {
                        LogEvent(Initiator.Name + " encountered difficulty but managed to spread the " + ((Architect)Initiator).BlightManipulated.Name + " to " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, ((Architect)Initiator).BlightManipulated, location });
                    }
                    else // Result == "MaD"
                    {
                        LogEvent(Initiator.Name + " faced great hardship and struggled to spread the " + ((Architect)Initiator).BlightManipulated.Name + " effectively in " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, ((Architect)Initiator).BlightManipulated, location });
                    }

                    // Adjust the chance of success based on the result
                    int successChance;
                    if (Result == "MaV")
                    {
                        successChance = 5; // Higher chance for major victory
                    }
                    else if (Result == "MiV")
                    {
                        successChance = 30; // Standard chance for minor victory
                    }
                    else if (Result == "MiD")
                    {
                        successChance = 60; // Lower chance for minor defeat
                        Initiator.PissOffEntityOrPlace(location, false);
                    }
                    else // Result == "MaD"
                    {
                        successChance = 100; // Very low chance for major defeat
                        Initiator.PissOffEntityOrPlace(location, false);
                    }

                    if (Game1.r.Next(successChance) == 1)
                    {
                        location.Region.Blight = ((Architect)Initiator).BlightManipulated;
                        LogEvent(Initiator.Name + " fully established a terrible presence of " + ((Architect)Initiator).BlightManipulated.Name + " in " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, ((Architect)Initiator).BlightManipulated, location });
                    }
                }

                int GrievanceChance = 4;

                foreach (Architect a in district.Architects)
                {
                    if (Game1.r.Next(GrievanceChance) == 1 && a != Initiator)
                    {
                        a.Grievances.Add((Initiator.ID, " plagued " + a.PossessivePronoun + " town, " + a.Location.Name + "."));
                        location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                    }
                }
            }


            else if (Action == "takeover")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                if (location.Government == null)
                {
                    LogEvent(Initiator.Name + " has peacefully taken control of " + location.Name + ", as there was no governing body to oppose.", location.Region, new EntityList<Entity>(){Initiator, location});

                    (Initiator is Architect ? (Architect)Initiator : ((Group)Initiator).Leader).TakenLocations.Add(location);
                    location.Government = Initiator;

                    foreach (Architect a in district.Architects)
                    {
                        if (Game1.r.Next(4) == 1 && a != Initiator)
                        {
                            a.Grievances.Add((Initiator.ID, " unjustly took control of " + a.PossessivePronoun + " town, " + a.Location.Name + "."));
                            location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                        }
                    }
                }
                else if (Result == "MaV" || Result == "MiV")
                {
                    // If there is a minor or major victory, takeover succeeds
                    if (Result == "MaV")
                    {
                        LogEvent(Initiator.Name + " effortlessly took control of " + location.Name + " after overpowering " + location.Government.Name + ".", location.Region, new EntityList<Entity>(){Initiator, location, location.Government});
                    }
                    else if (Result == "MiV")
                    {
                        LogEvent(Initiator.Name + " successfully took control of " + location.Name + " after a struggle with " + location.Government.Name + ".", location.Region, new EntityList<Entity>(){Initiator, location, location.Government});
                    }

                    if (location.Government is Architect)
                    {
                        Architect govArchitect = (Architect)(location.Government);
                        govArchitect.District.Architects.Remove(govArchitect);
                        (Initiator is Architect ? (Architect)Initiator : ((Group)Initiator).Leader).KilledPeopleWhoActuallyMatter.Add(govArchitect);
                    }
                    else
                    {
                        foreach (Architect a in ((Group)(location.Government)).Architects)
                        {
                            a.District.Architects.Remove(a);
                            LogEvent(Initiator.Name + " killed " + a.Name + " as they were part of " + location.Government.Name + ".", location.Region, new EntityList<Entity>(){Initiator, a, location.Government});

                            (Initiator is Architect ?(Architect)Initiator : ((Group)Initiator).Leader).KilledPeopleWhoActuallyMatter.Add(a);
                        }
                    }

                    foreach (Architect a in district.Architects)
                    {
                        if (a != Initiator)
                        {
                            a.Grievances.Add((Initiator.ID, " unjustly took control of " + a.PossessivePronoun + " town, " + a.Location.Name + "."));
                            location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                        }
                    }

                    (Initiator is Architect ? (Architect)Initiator : ((Group)Initiator).Leader).TakenLocations.Add(location);
                    location.Government = Initiator;
                }
                else if (Result == "MiD")
                {
                    // If there is a minor defeat, the takeover fails but with some effort shown
                    LogEvent(Initiator.Name + " attempted to take control of " + location.Name + " but faced strong resistance from " + location.Government.Name + " and ultimately failed.", location.Region, new EntityList<Entity>(){Initiator, location, location.Government});
                    Initiator.PissOffEntityOrPlace(location, false);
                }
                else // Result == "MaD"
                {
                    // If there is a major defeat, the takeover fails miserably
                    LogEvent(Initiator.Name + " suffered a humiliating defeat while trying to take control of " + location.Name + " from " + location.Government.Name + ".", location.Region, new EntityList<Entity>(){Initiator, location, location.Government});
                    Initiator.PissOffEntityOrPlace(location, false);
                }
            }
            else if (Action == "killassorted")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                int peopleKilled = 0;
                int architectsAssassinated = 0;

                if (Result == "MaV")
                {
                    peopleKilled = Game1.r.Next(5, 11); // Major Victory: Kill 5 to 10 people
                    architectsAssassinated = Game1.r.Next(2, 4); // Major Victory: Assassinate 2 to 3 architects
                    LogEvent(Initiator.Name + " orchestrated a highly effective massacre in " + location.Name + ", leaving a trail of devastation.", location.Region, new EntityList<Entity>(){Initiator, location});
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiV")
                {
                    peopleKilled = Game1.r.Next(2, 5); // Minor Victory: Kill 2 to 4 people
                    architectsAssassinated = Game1.r.Next(1, 2); // Minor Victory: Assassinate 1 architect
                    LogEvent(Initiator.Name + " managed to kill several people in " + location.Name + ", but the impact was less significant.", location.Region, new EntityList<Entity>(){Initiator, location});
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiD")
                {
                    peopleKilled = Game1.r.Next(1, 3); // Minor Defeat: Kill 1 to 2 people
                    architectsAssassinated = 0; // Minor Defeat: No architects assassinated
                    LogEvent(Initiator.Name + " attempted to kill in " + location.Name + ", but the effort was largely thwarted.", location.Region, new EntityList<Entity>() { Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, false);
                }
                else if (Result == "MaD")
                {
                    peopleKilled = 0; // Major Defeat: No one killed
                    architectsAssassinated = 0; // Major Defeat: No architects assassinated
                    LogEvent(Initiator.Name + " faced great hardship while trying to kill in " + location.Name + ", failing entirely.", location.Region, new EntityList<Entity>() { Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, false);
                }

                // Apply the calculated effects based on the Result
                if (district.UnplacedPopulation > 0 && peopleKilled > 0)
                {
                    int InitialPop = district.UnplacedPopulation;
                    district.UnplacedPopulation = Math.Max(0, district.UnplacedPopulation - peopleKilled);
                    LogEvent(Initiator.Name + " killed " + (InitialPop - district.UnplacedPopulation).ToString() + " people in " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, location });

                    int DecideAge = Game1.r.Next(1, 6);
                    if (DecideAge == 1)
                    {
                        ((Architect)Initiator).KilledChildren += (InitialPop - district.UnplacedPopulation);
                    }
                    else if (DecideAge < 4)
                    {
                        ((Architect)Initiator).KilledMen -= (InitialPop - district.UnplacedPopulation);
                    }
                    else
                    {
                        ((Architect)Initiator).KilledWomen -= (InitialPop - district.UnplacedPopulation);
                    }
                }

                if (district.Architects.Count() > 0 && architectsAssassinated > 0)
                {
                    EntityList<Architect> architectsList = district.Architects.ToEntityList();
                    Game1.Shuffle(architectsList);

                    for (int i = 0; i < architectsAssassinated; i++)
                    {
                        Architect affectedArchitect = architectsList.First();

                        if (GameWorld.Calamity.Contains(affectedArchitect) || !affectedArchitect.IsAlive)
                        {
                            continue;
                        }

                        district.ArchitectsToRemove.Add(affectedArchitect);
                        LogEvent(Initiator.Name + " assassinated " + affectedArchitect.Name + " in " + location.Name + ".", location.Region, new EntityList<Entity>(){Initiator, affectedArchitect, location});

                        foreach (Architect a in district.Architects)
                        {
                            if (Game1.r.Next(4) == 1 && a != affectedArchitect)
                            {
                                a.Grievances.Add((Initiator.ID, " murdered a friend of " + a.Name + ", " + affectedArchitect.Name + "."));
                                location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                            }
                        }

                        ((Architect)Initiator).KilledPeopleWhoActuallyMatter.Add(affectedArchitect);
                    }
                }
            }

            else if (Action == "kidnapassorted")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                int peopleKidnapped = 0;
                int architectsKidnapped = 0;

                if (Result == "MaV")
                {
                    peopleKidnapped = Game1.r.Next(3, 6); // Major Victory: Kidnap 3 to 5 people
                    architectsKidnapped = Game1.r.Next(2, 4); // Major Victory: Kidnap 2 to 3 architects
                    LogEvent(Initiator.Name + " executed a highly effective kidnapping operation in " + location.Name + ", causing significant unrest.", location.Region, new EntityList<Entity>(){Initiator, location});
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiV")
                {
                    peopleKidnapped = Game1.r.Next(1, 3); // Minor Victory: Kidnap 1 to 2 people
                    architectsKidnapped = Game1.r.Next(1, 2); // Minor Victory: Kidnap 1 architect
                    LogEvent(Initiator.Name + " successfully kidnapped some people in " + location.Name + ", though the impact was moderate.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiD")
                {
                    peopleKidnapped = Game1.r.Next(0, 2); // Minor Defeat: Kidnap 0 to 1 person
                    architectsKidnapped = 0; // Minor Defeat: No architects kidnapped
                    LogEvent(Initiator.Name + " attempted a kidnapping in " + location.Name + ", but the operation was mostly foiled.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, false);
                }
                else if (Result == "MaD")
                {
                    peopleKidnapped = 0; // Major Defeat: No one kidnapped
                    architectsKidnapped = 0; // Major Defeat: No architects kidnapped
                    LogEvent(Initiator.Name + " faced significant resistance while attempting to kidnap in " + location.Name + ", failing entirely.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, false);
                }

                // Apply the calculated effects based on the Result
                if (district.UnplacedPopulation > 0 && peopleKidnapped > 0)
                {
                    int InitialPop = district.UnplacedPopulation;
                    district.UnplacedPopulation = Math.Max(0, district.UnplacedPopulation - peopleKidnapped);
                    LogEvent(Initiator.Name + " kidnapped " + (InitialPop - district.UnplacedPopulation).ToString() + " person(s) in " + location.Name + ".", location.Region, new EntityList<Entity>(){ Initiator, location });

                    int DecideAge = Game1.r.Next(1, 6);
                    if (DecideAge == 1)
                    {
                        ((Architect)Initiator).KidnappedChildren += (InitialPop - district.UnplacedPopulation);
                    }
                    else if (DecideAge < 4)
                    {
                        ((Architect)Initiator).KidnappedMen += (InitialPop - district.UnplacedPopulation);
                    }
                    else
                    {
                        ((Architect)Initiator).KidnappedWomen += (InitialPop - district.UnplacedPopulation);
                    }
                }

                if (district.Architects.Count() > 0 && architectsKidnapped > 0)
                {
                    EntityList<Architect> architectsList = district.Architects.ToEntityList();
                    Game1.Shuffle(architectsList);

                    for (int i = 0; i < architectsKidnapped; i++)
                    {
                        Architect affectedArchitect = architectsList.First();

                        district.ArchitectsToRemove.Add(affectedArchitect);
                        LogEvent(Initiator.Name + " kidnapped " + affectedArchitect.Name + " in " + location.Name + ".", location.Region, new EntityList<Entity>(){Initiator, affectedArchitect, location});

                        ((Architect)Initiator).KidnappedPeopleWhoActuallyMatter.Add(affectedArchitect);
                        foreach (Architect a in district.Architects)
                        {
                            if (Game1.r.Next(4) == 1 && a != affectedArchitect)
                            {
                                a.Grievances.Add((Initiator.ID, " kidnapped " + affectedArchitect.Name + ", a valued member of " + a.PossessivePronoun + " community"));
                                Initiator.PissOffEntityOrPlace(affectedArchitect, false);
                                Initiator.PissOffEntityOrPlace(a, true);
                                location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                            }
                        }
                    }
                }
            }

            else if (Action == "kidnaptarget")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];
                Architect targetArchitect = (Architect)RelatedEntities[2];

                if (targetArchitect != null && district.Architects.Contains(targetArchitect))
                {
                    if (Result == "MaV")
                    {
                        LogEvent(Initiator.Name + " flawlessly executed the kidnapping of " + targetArchitect.Name + " in " + location.Name + ", leaving no trace behind.", location.Region, new EntityList<Entity>(){Initiator, targetArchitect,location});
                    }
                    else if (Result == "MiV")
                    {
                        LogEvent(Initiator.Name + " successfully kidnapped " + targetArchitect.Name + " in " + location.Name + ", though there were some complications.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                    }
                    else if (Result == "MiD")
                    {
                        LogEvent(Initiator.Name + " attempted to kidnap " + targetArchitect.Name + " in " + location.Name + ", but faced resistance and barely managed to succeed.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                    }
                    else if (Result == "MaD")
                    {
                        LogEvent(Initiator.Name + " failed miserably in the attempt to kidnap " + targetArchitect.Name + " in " + location.Name + ". The operation was a disaster.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                        return; // Exit early if the operation was a major defeat, and no kidnapping occurred.
                    }

                    district.Architects.Remove(targetArchitect);
                    ((Architect)Initiator).KidnappedPeopleWhoActuallyMatter.Add(targetArchitect);

                    foreach (Architect a in district.Architects)
                    {
                        if (Game1.r.Next(4) == 1 && a != targetArchitect)
                        {
                            a.Grievances.Add((Initiator.ID, " kidnapped " + targetArchitect.Name + ", a valued member of " + a.PossessivePronoun + " community"));
                            Initiator.PissOffEntityOrPlace(targetArchitect, false);
                            Initiator.PissOffEntityOrPlace(a, true);
                            location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                        }
                    }
                }
            }
            else if (Action == "diplomance")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];
                Architect targetArchitect = (Architect)RelatedEntities[2];

                if (targetArchitect != null && district.Architects.Contains(targetArchitect))
                {
                    int moralChange = 0;

                    if (Result == "MaV")
                    {
                        moralChange = Game1.r.Next(30, 50); // Major Victory: Significant influence
                        LogEvent(Initiator.Name + " expertly corrupted " + targetArchitect.Name + "'s values in " + location.Name + ", deeply influencing them towards evil.", location.Region, new EntityList<Entity>(){Initiator, targetArchitect, location});
                    }
                    else if (Result == "MiV")
                    {
                        moralChange = Game1.r.Next(15, 30); // Minor Victory: Moderate influence
                        LogEvent(Initiator.Name + " successfully influenced " + targetArchitect.Name + "'s values towards evil in " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }
                    else if (Result == "MiD")
                    {
                        moralChange = Game1.r.Next(5, 15); // Minor Defeat: Slight influence
                        LogEvent(Initiator.Name + " attempted to influence " + targetArchitect.Name + " towards evil in " + location.Name + ", but only managed to make a slight impact.", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }
                    else if (Result == "MaD")
                    {
                        moralChange = Game1.r.Next(0, 5); // Major Defeat: Almost no influence
                        LogEvent(Initiator.Name + " failed to corrupt " + targetArchitect.Name + "'s values in " + location.Name + ". The attempt was almost entirely ineffective.", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }

                    targetArchitect.MoralCompass -= moralChange;

                    foreach (Architect a in district.Architects)
                    {
                        if (Game1.r.Next(4) == 1 && a != targetArchitect)
                        {
                            a.Grievances.Add((Initiator.ID, " was noticed by " + a.Name + ", who began to see a major change in " + targetArchitect.Name + " towards evil")); 
                            Initiator.PissOffEntityOrPlace(a, true);
                            location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                        }
                    }
                }
            }


            else if (Action == "corrupt")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];
                Architect targetArchitect = (Architect)RelatedEntities[2];

                if (targetArchitect != null && district.Architects.Contains(targetArchitect))
                {
                    int moralChange = 0;
                    int stabilityChange = 0;

                    if (Result == "MaV")
                    {
                        moralChange = Game1.r.Next(20, 40); // Major Victory: Significant corruption
                        stabilityChange = Game1.r.Next(20, 40);
                        LogEvent(Initiator.Name + " deeply corrupted " + targetArchitect.Name + "'s moral and stability values in " + location.Name + ", leaving a lasting impact.", location.Region, new EntityList<Entity>(){Initiator, targetArchitect, location});
                    }
                    else if (Result == "MiV")
                    {
                        moralChange = Game1.r.Next(10, 20); // Minor Victory: Moderate corruption
                        stabilityChange = Game1.r.Next(10, 20);
                        LogEvent(Initiator.Name + " successfully corrupted " + targetArchitect.Name + "'s moral and stability values in " + location.Name + ".", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }
                    else if (Result == "MiD")
                    {
                        moralChange = Game1.r.Next(5, 10); // Minor Defeat: Slight corruption
                        stabilityChange = Game1.r.Next(5, 10);
                        LogEvent(Initiator.Name + " attempted to corrupt " + targetArchitect.Name + " in " + location.Name + ", but only managed to cause minor changes.", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }
                    else if (Result == "MaD")
                    {
                        moralChange = Game1.r.Next(0, 5); // Major Defeat: Almost no corruption
                        stabilityChange = Game1.r.Next(0, 5);
                        LogEvent(Initiator.Name + " failed to corrupt " + targetArchitect.Name + " in " + location.Name + ". The attempt was largely ineffective.", location.Region, new EntityList<Entity>() { Initiator, targetArchitect, location });
                    }

                    targetArchitect.MoralCompass -= moralChange;
                    targetArchitect.StabilityCompass -= stabilityChange;

                    foreach (Architect a in district.Architects)
                    {
                        if (Game1.r.Next(4) == 1 && a != targetArchitect)
                        {
                            if (a == targetArchitect)
                            {
                                a.Grievances.Add((Initiator.ID, " was noticed by " + a.Name + ", who started to notice an evil difference in " + a.PossessivePronoun + " own psychology"));
                            }
                            else
                            {
                                a.Grievances.Add((Initiator.ID, " was noticed by " + a.Name + ", who began to notice an evil difference in " + targetArchitect.Name));
                                Initiator.PissOffEntityOrPlace(a, true);
                            }

                            location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                        }
                    }
                }
            }

            else if (Action == "killtarget")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];
                Architect targetArchitect = (Architect)RelatedEntities[2];

                if (targetArchitect != null && district.Architects.Contains(targetArchitect))
                {
                    string[] approaches = new string[]
                    {
            "cautiously stalked the target from a distance",
            "moved in silently for the kill",
            "set up a trap using the surrounding environment",
            "patiently observed the target, waiting for the perfect moment to strike"
                    };
                    string[] fightDetails = new string[]
                    {
            "The confrontation was intense and brutal",
            "The target fought back fiercely",
            "The kill was swift and efficient",
            "The struggle lasted for a long time"
                    };

                    string approach = approaches[Game1.r.Next(approaches.Length)];
                    string fightDetail = fightDetails[Game1.r.Next(fightDetails.Length)];
                    string outcome;

                    if (Result == "MaV" || Result == "MiV")
                    {
                        outcome = "In the end, the assassin completed their mission successfully.";
                        targetArchitect.IsAlive = false;
                        district.Architects.Remove(targetArchitect);
                        LogEvent($"{Initiator.Name} located {targetArchitect.Name}. {Initiator.Name} {approach}. {fightDetail}. {outcome} {Initiator.Name} assassinated {targetArchitect.Name} in {location.Name}.", location.Region, new EntityList<Entity>(){Initiator, targetArchitect, location});

                        foreach (Architect a in district.Architects)
                        {
                            if (Game1.r.Next(4) == 1 && a != targetArchitect)
                            {
                                Initiator.PissOffEntityOrPlace(a, true);
                                a.Grievances.Add((Initiator.ID, " murdered " + targetArchitect.Name + ", a friend of " + a.Name + "."));
                                location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                            }
                        }

                        ((Architect)Initiator).KilledPeopleWhoActuallyMatter.Add(targetArchitect);
                    }
                    else if (Result == "MiD")
                    {
                        outcome = "The assassin failed, but managed to escape alive.";
                        LogEvent($"{Initiator.Name} located {targetArchitect.Name}. {Initiator.Name} {approach}. {fightDetail}. {outcome} The attempt failed, but the assassin escaped.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                    }
                    else if (Result == "MaD")
                    {
                        if (Game1.r.Next(100) < 20 && Initiator is Architect a) // 20% chance of death
                        {
                            outcome = "Tragically, the assassin was killed.";
                            a.IsAlive = false;
                            LogEvent($"{Initiator.Name} located {targetArchitect.Name}. {Initiator.Name} {approach}. {fightDetail}. {outcome} Tragically, {a.Name} was killed in the attempt.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                        }
                        else
                        {
                            outcome = "The assassin failed miserably and was forced to retreat.";
                            LogEvent($"{Initiator.Name} located {targetArchitect.Name}. {Initiator.Name} {approach}. {fightDetail}. {outcome} The assassin managed to escape, but the mission failed.", location.Region, new EntityList<Entity>(){ Initiator, targetArchitect, location });
                        }
                    }

                }
            }

            else if (Action == "incite")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                LogEvent(Initiator.Name + " spread propaganda, attempting to influence " + location.HomeCivilization.Name + " into a conflict.", location.Region, new EntityList<Entity>(){Initiator, location});

                // Adjust WUACVP based on the result
                if (Result == "MaV")
                {
                    location.HomeCivilization.WakeUpAndChooseViolencePoints += Game1.r.Next(15, 21); // Major Victory, significant increase
                    LogEvent(Initiator.Name + " successfully incited " + location.HomeCivilization.Name + " into a state of unrest, significantly increasing their aggression.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiV")
                {
                    location.HomeCivilization.WakeUpAndChooseViolencePoints += Game1.r.Next(5, 15); // Minor Victory, moderate increase
                    LogEvent(Initiator.Name + " managed to stir some unrest in " + location.HomeCivilization.Name + ", leading to a moderate increase in aggression.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MiD")
                {
                    location.HomeCivilization.WakeUpAndChooseViolencePoints += Game1.r.Next(0, 5); // Minor Defeat, slight increase
                    LogEvent(Initiator.Name + " attempted to incite unrest in " + location.HomeCivilization.Name + ", but had only a minimal effect.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, true);
                }
                else if (Result == "MaD")
                {
                    LogEvent(Initiator.Name + " failed to incite unrest in " + location.HomeCivilization.Name + ".", location.Region, new EntityList<Entity>(){ Initiator, location });
                    Initiator.PissOffEntityOrPlace(location, true);
                }

                // Check if WUACVP exceeds 200 and trigger a war if so
                if (location.HomeCivilization.WakeUpAndChooseViolencePoints > 100)
                {
                    Civilization c = GameWorld.Civilizations[Game1.r.Next(GameWorld.Civilizations.Count())];

                    GameWorld.Wars.Add((c, location.HomeCivilization, 0, 0));

                    GameWorld.HistoricalEvents.Add(new Event($"{Date} {location.HomeCivilization.Name}, a {location.HomeCivilization.Type} society, declared war on {c.Name}, a {c.Type} society.", c.Capitol.Region, new EntityList<Entity>() { location.HomeCivilization, c }));

                    foreach (Architect a in GameWorld.AllArchitects)
                    {
                        if (a.HomeLocation != null && (a.HomeLocation.HomeCivilization == c || a.HomeLocation.HomeCivilization == location.HomeCivilization))
                        {
                            if (Game1.r.Next(3) != 1)
                            {
                                a.Grievances.Add((Initiator.ID, " caused a war that ruined the stability of " + a.Name + "'s life"));
                                location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                            }
                        }
                    }

                    location.HomeCivilization.WakeUpAndChooseViolencePoints = 0;
                }

                // Decrease MoralCompass and StabilityCompass for Architects in the district
                foreach (Architect a in district.Architects)
                {
                    a.MoralCompass -= Game1.r.Next(1, 3);
                    a.StabilityCompass -= Game1.r.Next(1, 3);
                }
            }

            else if (Action == "harvest")
            {
                Location location = (Location)RelatedEntities[0];
                District district = (District)RelatedEntities[1];

                // Adjust the number of people or architects harvested based on effectiveness
                if (Result == "MaV" || Result == "MiV")
                {
                    int numberOfVictims = Result == "MaV" ? Game1.r.Next(3, 6) : Game1.r.Next(1, 3); // Major Victory harvests more, Minor Victory less

                    // Harvest unplaced population
                    if (district.UnplacedPopulation > 0)
                    {
                        int InitialPop = district.UnplacedPopulation;
                        district.UnplacedPopulation = Math.Max(0, district.UnplacedPopulation - numberOfVictims);

                        LogEvent(Initiator.Name + " killed " + (InitialPop - district.UnplacedPopulation).ToString() + " people in " + location.Name + ", and harvested their energy.", location.Region, new EntityList<Entity>(){Initiator, location});
                        Initiator.PissOffEntityOrPlace(location, true);

                        int DecideAge = Game1.r.Next(1, 6);
                        if (DecideAge == 1)
                        {
                            ((Architect)Initiator).KilledChildren += (InitialPop - district.UnplacedPopulation);
                        }
                        else if (DecideAge < 4)
                        {
                            ((Architect)Initiator).KilledMen += (InitialPop - district.UnplacedPopulation);
                        }
                        else
                        {
                            ((Architect)Initiator).KilledWomen += (InitialPop - district.UnplacedPopulation);
                        }
                        foreach (Architect a in district.Architects)
                        {
                            if (Game1.r.Next(4) == 1 && a != Initiator)
                            {
                                a.Grievances.Add((Initiator.ID, " harvested energy, causing the death of many in " + a.PossessivePronoun + " town, " + location.Name + ""));
                                location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                            }
                        }
                    }

                    // Harvest from architects if population is unavailable
                    if (district.Architects.Count() > 0 && district.UnplacedPopulation == 0)
                    {
                        EntityList<Architect> architectsList = district.Architects.ToEntityList();
                        Game1.Shuffle(architectsList);

                        for (int i = 0; i < numberOfVictims && architectsList.Count > 0; i++)
                        {
                            Architect affectedArchitect = architectsList.First();
                            architectsList.Remove(affectedArchitect);

                            if (affectedArchitect == Initiator || !affectedArchitect.IsAlive)
                            {
                                continue;
                            }

                            district.ArchitectsToRemove.Add(affectedArchitect);

                            LogEvent(Initiator.Name + " assassinated " + affectedArchitect.Name + " in " + location.Name + ", and harvested their energy.", location.Region, new EntityList<Entity>(){Initiator, affectedArchitect, location});

                            ((Architect)Initiator).KilledPeopleWhoActuallyMatter.Add(affectedArchitect);
                            foreach (Architect a in district.Architects)
                            {
                                if (Game1.r.Next(4) == 1 && a != affectedArchitect)
                                {
                                    a.Grievances.Add((Initiator.ID, " murdered and harvested energy from " + affectedArchitect.Name + ", a good friend of theirs"));
                                    location.Region.TragedyPoints.Add((Game1.r.Next(-10, 11), Game1.r.Next(-10, 11)));
                                }
                            }
                        }
                    }
                }
                else if (Result == "MiD")
                {
                    LogEvent(Initiator.Name + " attempted to harvest energy in " + location.Name + ", but only managed to cause a small disturbance.", location.Region, new EntityList<Entity>(){Initiator, location});
                }
                else if (Result == "MaD")
                {
                    LogEvent(Initiator.Name + " failed miserably in the attempt to harvest energy in " + location.Name + ", and was unable to claim any victims.", location.Region, new EntityList<Entity>(){Initiator, location});
                }

                // Check if they have gained enough power to learn new spells
                if (((Architect)Initiator).KilledChildren + ((Architect)Initiator).KilledMen + ((Architect)Initiator).KilledWomen + ((Architect)Initiator).KilledPeopleWhoActuallyMatter.Count() > 100 && ((Architect)Initiator).SpellsKnown.Count() < 3)
                {
                    ((Architect)Initiator).SpellsKnown = new EntityList<Entity>(GameWorld.AllSpells.Union(GameWorld.AllLegendarySpells));
                    ((Architect)Initiator).Focus = 15;
                    LogEvent("After harvesting enough energy and renouncing the deities of the land, " + Initiator.Name + " became infused with unfathomable power from an unknown origin, but continued on to tempt the universe further.", location.Region, new EntityList<Entity>(){Initiator, location});
                }
            }

            else if (Action == "rupture")
            {
                Location location = (Location)RelatedEntities[0];
                Architect calamitizer = (Architect)Initiator;

                if (Result == "MaV")
                {
                    List<(int x, int z)> validRegions = new List<(int, int)>();

                    for (int x = 0; x < GameWorld.Width; x++)
                    {
                        for (int z = 0; z < GameWorld.Length; z++)
                        {
                            Region region = GameWorld.WorldMap[x + z * GameWorld.Width];
                            if (region.Biome == "void" || region.Biome == "ethereal")
                            {
                                validRegions.Add((x, z));
                            }
                        }
                    }

                    if (validRegions.Count() > 0)
                    {
                        var (ruptureX, ruptureZ) = validRegions[Game1.r.Next(validRegions.Count())];

                        GameWorld.TriggerRupture(ruptureX, ruptureZ, calamitizer, Game1.r.Next(1, 6));

                        int scanRadius = 8;
                        for (int x = Math.Max(0, ruptureX - scanRadius); x <= Math.Min(GameWorld.Width - 1, ruptureX + scanRadius); x++)
                        {
                            for (int z = Math.Max(0, ruptureZ - scanRadius); z <= Math.Min(GameWorld.Length - 1, ruptureZ + scanRadius); z++)
                            {
                                if (World.CalculateDistance(ruptureX, ruptureZ, x, z) > 3 && World.CalculateDistance(ruptureX, ruptureZ, x, z) <= scanRadius)
                                {
                                    Location nearbyLocation = GameWorld.WorldMap[x + z * GameWorld.Width].Location;
                                    if (nearbyLocation != null)
                                    {
                                        Initiator.PissOffEntityOrPlace(location, false);

                                        foreach (District district in nearbyLocation.Districts)
                                        {
                                            foreach (Architect architect in district.Architects)
                                            {
                                                if (Game1.r.Next(4) == 1 && architect != calamitizer)
                                                {
                                                    architect.Grievances.Add((calamitizer.ID, " caused a rupture near " + architect.Name + "'s district."));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        LogEvent(Initiator.Name + " successfully caused a catastrophic rupture, obliterating the area around " + location.Name + ".", location.Region, new EntityList<Entity>(){Initiator, location});
                    }
                }
                else if (Result == "MiV")
                {
                    LogEvent(Initiator.Name + " attempted to cause a rupture near " + location.Name + ", but the ritual was contained before it could cause significant damage.", location.Region, new EntityList<Entity>(){Initiator, location});
                }
                else if (Result == "MiD")
                {
                    LogEvent(Initiator.Name + " failed to cause a rupture near " + location.Name + ". The attempt caused a minor disturbance, but nothing catastrophic.", location.Region, new EntityList<Entity>(){ Initiator, location });
                }
                else if (Result == "MaD")
                {
                    LogEvent(Initiator.Name + " completely failed in an attempt to cause a rupture near " + location.Name + ".", location.Region, new EntityList<Entity>(){ Initiator, location });
                }
            }

            else if (Action == "artifacttheft")
            {
                Location location = (Location)RelatedEntities[0];
                Structure targetStructure = (Structure)RelatedEntities[2];
                Object targetObject = (Object)RelatedEntities[3];

                // Cast initiator as an Architect or Group
                Architect initiatorArchitect = Initiator as Architect;
                Group initiatorGroup = Initiator as Group;

                string initiatorName = initiatorArchitect?.Name ?? initiatorGroup?.Name;
                string rogueText = initiatorGroup != null ? "rogues" : "rogue";

                if (targetStructure.HistoricalObjects.Contains(targetObject))
                {
                    string[] approaches = new string[]
                    {
            "carefully planned the heist",
            "moved in quietly, avoiding detection",
            "used the cover of night to approach the target",
            "waited patiently for the right moment to strike"
                    };
                    string[] theftDetails = new string[]
                    {
            "The attempt was tense and nerve-wracking",
            "The target location was well-guarded",
            "The theft was executed swiftly",
            "It was a long and risky operation"
                    };
                    string[] successes = new string[]
                    {
            $"In the end, the {rogueText} successfully stole the item",
            $"The {rogueText} made off with the treasure without a hitch",
            $"The {rogueText} completed the heist with great skill",
            $"Ultimately, the {rogueText} managed to secure the prize"
                    };
                    string[] failures = new string[]
                    {
            $"The {rogueText} were caught but managed to escape",
            $"The target was too heavily guarded, and the {rogueText} fled",
            $"The {rogueText} were forced to abandon the mission",
            $"Despite their efforts, the {rogueText} failed to steal the item"
                    };

                    string[] deaths;

                    if (targetStructure.Block.District.Location.Government != null)
                    {
                        deaths = new string[]
                        {
                $"The {rogueText} were caught and executed by " + targetStructure.Block.District.Location.Government.Name,
                $"The guards overpowered and killed the {rogueText}",
                $"The {rogueText} met their demise during the attempt",
                $"Tragically, the {rogueText} did not survive the heist"
                        };
                    }
                    else
                    {
                        deaths = new string[]
                        {
                $"The {rogueText} were caught and executed by the general population",
                $"The guards overpowered and killed the {rogueText}",
                $"The {rogueText} met their demise during the attempt",
                $"Tragically, the {rogueText} did not survive the heist"
                        };
                    }

                    string approach = approaches[Game1.r.Next(approaches.Length)];
                    string theftDetail = theftDetails[Game1.r.Next(theftDetails.Length)];
                    string outcome = "";

                    if (Result == "MaV")
                    {
                        // Major Victory: Successful theft
                        outcome = successes[Game1.r.Next(successes.Length)];
                        if (initiatorArchitect != null)
                        {
                            initiatorArchitect.Inventory.Add(targetObject);
                        }
                        else if (initiatorGroup != null)
                        {
                            initiatorGroup.Leader.Inventory.Add(targetObject);
                        }
                        targetStructure.HistoricalObjects.Remove(targetObject);
                        LogEvent($"{initiatorName} found the treasure {targetObject.ReferredToNames[0]} at {location.Name}. {initiatorName} {approach}. {theftDetail}. {outcome}. The theft succeeded.", location.Region, new EntityList<Entity>() {Initiator, targetObject, location});
                    }
                    else if (Result == "MiV")
                    {
                        // Minor Victory: Partial success with escape
                        outcome = failures[Game1.r.Next(failures.Length)];
                        LogEvent($"{initiatorName} attempted to steal the treasure {targetObject.ReferredToNames[0]} from {location.Name}. {initiatorName} {approach}. {theftDetail}. {outcome}. The theft failed, but the {rogueText} escaped.", location.Region, new EntityList<Entity>() { Initiator, targetObject, location });
                    }
                    else if (Result == "MiD")
                    {
                        // Minor Defeat: Failure but with escape
                        outcome = failures[Game1.r.Next(failures.Length)];
                        LogEvent($"{initiatorName} attempted to steal the treasure {targetObject.ReferredToNames[0]} from {location.Name}. {initiatorName} {approach}. {theftDetail}. {outcome}. The {rogueText} failed to steal the item but managed to escape.", location.Region, new EntityList<Entity>() { Initiator, targetObject, location });
                    }
                    else if (Result == "MaD")
                    {
                        // Major Defeat: Failure with a 20% chance of death
                        if (Game1.r.Next(5) == 0)  // 20% chance of death
                        {
                            outcome = deaths[Game1.r.Next(deaths.Length)];
                            if (initiatorArchitect != null)
                            {
                                initiatorArchitect.IsAlive = false;
                            }
                            else if (initiatorGroup != null)
                            {
                                initiatorGroup.Leader.IsAlive = false;
                            }
                            LogEvent($"{initiatorName} attempted to steal the treasure {targetObject.ReferredToNames[0]} from {location.Name}. {initiatorName} {approach}. {theftDetail}. {outcome}. Tragically, the {rogueText} did not survive the heist.", location.Region, new EntityList<Entity>() { Initiator, targetObject, location });
                        }
                        else
                        {
                            outcome = failures[Game1.r.Next(failures.Length)];
                            LogEvent($"{initiatorName} attempted to steal the treasure {targetObject.ReferredToNames[0]} from {location.Name}. {initiatorName} {approach}. {theftDetail}. {outcome}. The {rogueText} failed and barely escaped with their life.", location.Region, new EntityList<Entity>() { Initiator, targetObject, location });
                        }
                    }

                    // Add grievances and reputation changes
                    if (location.Government is Group governmentGroup)
                    {
                        Initiator.PissOffEntityOrPlace(governmentGroup, true);
                        foreach (Architect a in governmentGroup.Architects)
                        {
                            a.Grievances.Add((initiatorArchitect?.ID ?? initiatorGroup.Leader.ID, $"{initiatorName} attempted to steal {targetObject.Name}, a treasured artifact, impacting the heritage and pride of {location.Name}."));
                        }
                    }
                    else if (location.Government is Architect governmentArchitect)
                    {
                        Initiator.PissOffEntityOrPlace(governmentArchitect, true);
                        governmentArchitect.Grievances.Add((initiatorArchitect?.ID ?? initiatorGroup.Leader.ID, $"{initiatorName} attempted to steal {targetObject.Name}, a treasured artifact, impacting the heritage and pride of {location.Name}."));
                    }

                    if (targetObject.Creator != null)
                    {
                        if (targetObject.Creator is Architect creatorArchitect)
                        {
                            Initiator.PissOffEntityOrPlace(creatorArchitect, true);
                            creatorArchitect.Grievances.Add((initiatorArchitect?.ID ?? initiatorGroup.Leader.ID, $"{initiatorName} stole {targetObject.Name}, a creation of great cultural significance."));
                        }
                        else if (targetObject.Creator is Group creatorGroup)
                        {
                            Initiator.PissOffEntityOrPlace(creatorGroup, true);
                            foreach (Architect member in creatorGroup.Architects)
                            {
                                member.Grievances.Add((initiatorArchitect?.ID ?? initiatorGroup.Leader.ID, $"{initiatorName} stole {targetObject.Name}, a creation of great cultural significance."));
                            }
                        }
                    }
                }
            }



            else if (Action == "hunterfight")
            {
                Architect targetArchitect = (Architect)RelatedEntities[2];
                Architect initiatorArchitect = (Architect)Initiator;

                string[] approaches = new string[]
                {
        "cautiously stalked the target from the shadows",
        "moved in, hoping for a swift kill",
        "set up an ambush using the nearby terrain",
        "patiently waited for the perfect moment to strike"
                };
                string[] fightDetails = new string[]
                {
        "The battle was fierce and relentless",
        "The target put up a strong resistance",
        "The fight was over quickly",
        "The fight was a struggle that lasted for hours"
                };
                string[] victories = new string[]
                {
        "In the end, the hunter emerged victorious",
        "The hunter triumphed with ease",
        "The hunter claimed victory after a hard-fought battle",
        "Ultimately, the hunter stood victorious"
                };
                string[] failures = new string[]
                {
        "Unfortunately, the hunter was defeated, though still alive",
        "The target managed to escape",
        "The hunter was forced to retreat",
        "Despite their efforts, the hunter failed"
                };
                string[] deaths = new string[]
                {
        "Tragically, the hunter was slain",
        "The target overpowered and killed the hunter",
        "The hunter met their demise in the encounter",
        "Sadly, the hunter did not survive the battle"
                };

                string approach = approaches[Game1.r.Next(approaches.Length)];
                string fightDetail = fightDetails[Game1.r.Next(fightDetails.Length)];
                string outcome = "";

                if (Result == "MaV")
                {
                    // Major Victory: Successful kill
                    outcome = victories[Game1.r.Next(victories.Length)];
                    targetArchitect.IsAlive = false;
                    LogEvent($"{initiatorArchitect.Name} located {targetArchitect.Name}. {initiatorArchitect.Name} {approach}. {fightDetail}. {outcome}. {initiatorArchitect.Name} assassinated {targetArchitect.Name} in {targetArchitect.Location.Name}.", targetArchitect.Location != null ? targetArchitect.Location.Region : Game1.GameWorld.WorldMap[Game1.r.Next(Game1.GameWorld.WorldMap.Count)], new EntityList<Entity>(){initiatorArchitect, targetArchitect});
                }
                else if (Result == "MiV")
                {
                    // Minor Victory: Target escapes, but initiator survives
                    outcome = failures[Game1.r.Next(failures.Length)];
                    LogEvent($"{initiatorArchitect.Name} engaged in combat with {targetArchitect.Name}. {initiatorArchitect.Name} {approach}. {fightDetail}. {outcome}. The target managed to escape, but the hunter survived.", targetArchitect.Location != null ? targetArchitect.Location.Region : Game1.GameWorld.WorldMap[Game1.r.Next(Game1.GameWorld.WorldMap.Count)], new EntityList<Entity>(){ initiatorArchitect, targetArchitect });
                }
                else if (Result == "MiD")
                {
                    // Minor Defeat: Initiator fails but escapes alive
                    outcome = failures[Game1.r.Next(failures.Length)];
                    LogEvent($"{initiatorArchitect.Name} attempted to take down {targetArchitect.Name}. {initiatorArchitect.Name} {approach}. {fightDetail}. {outcome}. The hunter failed but managed to escape with their life.", targetArchitect.Location != null ? targetArchitect.Location.Region : Game1.GameWorld.WorldMap[Game1.r.Next(Game1.GameWorld.WorldMap.Count)], new EntityList<Entity>(){ initiatorArchitect, targetArchitect });
                }
                else if (Result == "MaD")
                {
                    // Major Defeat: Initiator might die, 20% chance of death
                    if (Game1.r.Next(5) == 0) // 20% chance of death
                    {
                        outcome = deaths[Game1.r.Next(deaths.Length)];
                        initiatorArchitect.IsAlive = false;
                        LogEvent($"{initiatorArchitect.Name} hunted {targetArchitect.Name}. {initiatorArchitect.Name} {approach}. {fightDetail}. {outcome}. Tragically, {initiatorArchitect.Name} was killed in the attempt.", targetArchitect.Location != null ? targetArchitect.Location.Region : Game1.GameWorld.WorldMap[Game1.r.Next(Game1.GameWorld.WorldMap.Count)], new EntityList<Entity>(){ initiatorArchitect, targetArchitect });
                    }
                    else
                    {
                        outcome = failures[Game1.r.Next(failures.Length)];
                        LogEvent($"{initiatorArchitect.Name} attempted to kill {targetArchitect.Name}. {initiatorArchitect.Name} {approach}. {fightDetail}. {outcome}. The hunter barely escaped with their life.", targetArchitect.Location != null ? targetArchitect.Location.Region : Game1.GameWorld.WorldMap[Game1.r.Next(Game1.GameWorld.WorldMap.Count)], new EntityList<Entity>(){ initiatorArchitect, targetArchitect });
                    }
                }
            }


            else if (Action == "negotiate")
            {
                Architect initiatorArchitect = (Architect)Initiator;

                // Randomly select the negotiation topic
                string[] topics = { "peace", "trade", "threat", "relations" };
                string negotiationTopic = topics[Game1.r.Next(topics.Length)];

                string successMessage = "";
                string failureMessage = "";
                Entity threat = null;

                if (negotiationTopic == "peace")
                {
                    successMessage = $"{initiatorArchitect.Name} successfully negotiated with {initiatorArchitect.Location.Name} to promote peace. The negotiations were very productive.";
                    failureMessage = $"{initiatorArchitect.Name} attempted to negotiate peace with {initiatorArchitect.Location.Name}, but the efforts were not successful.";
                }
                else if (negotiationTopic == "trade")
                {
                    successMessage = $"{initiatorArchitect.Name} successfully negotiated with {initiatorArchitect.Location.Name} to promote trade. The negotiations were very productive.";
                    failureMessage = $"{initiatorArchitect.Name} attempted to promote trade with {initiatorArchitect.Location.Name}, but the negotiations were not successful.";
                }
                else if (negotiationTopic == "threat")
                {
                    string additionalInfo = "";

                    int threatType = Game1.r.Next(1, 4);
                    if (threatType == 1)
                    {
                        threat = GameWorld.Colossals[Game1.r.Next(GameWorld.Colossals.Count())];
                    }
                    else if (threatType == 2)
                    {
                        threat = GameWorld.Blights[Game1.r.Next(GameWorld.Blights.Count())];
                    }
                    else
                    {
                        threat = GameWorld.Calamity[0];
                        additionalInfo = $" and their gang of {Game1.CalamityIdeologicalObsessionMapping[GameWorld.CalamityIdeologicalObsession]}";
                    }

                    successMessage = $"{initiatorArchitect.Name} successfully negotiated with {initiatorArchitect.Location.Name} to deal with the threat of {threat.Name}{additionalInfo}. The negotiations were very productive.";
                    failureMessage = $"{initiatorArchitect.Name} attempted to negotiate a response to the threat of {threat.Name}{additionalInfo}, but the negotiations were not successful.";
                }
                else if (negotiationTopic == "relations")
                {
                    successMessage = $"{initiatorArchitect.Name} successfully negotiated with {initiatorArchitect.Location.Name} to establish better relations. The negotiations were very productive.";
                    failureMessage = $"{initiatorArchitect.Name} attempted to improve relations with {initiatorArchitect.Location.Name}, but the negotiations were not successful.";
                }

                if (Result == "MaV" || Result == "MiV")
                {
                    if (negotiationTopic == "threat")
                    {
                        LogEvent(successMessage, initiatorArchitect.Location.Region, new EntityList<Entity>() { initiatorArchitect, initiatorArchitect.Location, threat });
                    }
                    else
                    {
                        LogEvent(successMessage, initiatorArchitect.Location.Region, new EntityList<Entity>() { initiatorArchitect, initiatorArchitect.Location });
                    }
                }
                else
                {
                    if (negotiationTopic == "threat")
                    {
                        LogEvent(failureMessage, initiatorArchitect.Location.Region, new EntityList<Entity>() { initiatorArchitect, initiatorArchitect.Location, threat });
                    }
                    else
                    {
                        LogEvent(failureMessage, initiatorArchitect.Location.Region, new EntityList<Entity>() { initiatorArchitect, initiatorArchitect.Location });
                    }
                }
            }

            else if (Action == "visitation")
            {
                Architect initiatorArchitect = (Architect)Initiator;
                District selectedDistrict = (District)RelatedEntities[1];

                string[] shobeSubjects = new string[]
                {
        "relationships", "mining", "combat", "crafting", "trading", "stealth", "alchemy", "cooking", "fishing",
        "hunting", "quests", "gathering", "imbuement", "healing", "navigation", "tactics", "survival",
        "diplomacy", "lockpicking", "animal taming", "herbalism", "herbs", "blacksmithing", "tailoring",
        "carpentry", "architecture", "history", "sailing", "farming", "brewing", "divination", "spellcasting",
        "negotiation", "investigation", "potions", "archery", "swordsmanship", "armor crafting", "thievery",
        "mountaineering", "cartography", "astronomy", "necromancy", "spatiomancy", "conjuromancy", "fractalmancy",
        "perceptomancy", "beasts", "divinity", "illusion", "mechanics", "engineering"
                };

                bool pillagingStill = true;
                int diminishingReturnCounter = 0;

                while (pillagingStill)
                {
                    int decider = Game1.r.Next(100);

                    if (decider < 5)
                    {
                        if (selectedDistrict.Architects.Count() > 0)
                        {
                            Architect selectedArch = Game1.GetRandomItem<Architect>(selectedDistrict.Architects);
                            LogEvent($"{initiatorArchitect.Name} had a lovely chat about {shobeSubjects[Game1.r.Next(shobeSubjects.Length)]} in {initiatorArchitect.Location.Name} with {selectedArch.Name}.", initiatorArchitect.Location.Region, new EntityList<Entity>(){ initiatorArchitect, initiatorArchitect.Location, selectedArch});
                        }
                    }
                    else if (decider < 7)
                    {
                        EntityList<Object> o = GameWorld.LootTableMachine("general");
                        initiatorArchitect.Location.Wealth += Game1.r.Next(20, 50);
                        initiatorArchitect.Inventory.AddRange(o);
                        LogEvent($"{initiatorArchitect.Name} purchased some general items from {initiatorArchitect.Location.Name}.", initiatorArchitect.Location.Region, new EntityList<Entity>(){ initiatorArchitect, initiatorArchitect.Location });
                    }
                    else if (decider < 20)
                    {
                        pillagingStill = false;
                    }

                    if (diminishingReturnCounter > 1)
                    {
                        pillagingStill = false;
                    }

                    diminishingReturnCounter++;
                }

                initiatorArchitect.AdventureCooldown = Game1.r.Next(50, 100 + initiatorArchitect.Age);
            }

            else if (Action == "adventure")
            {
                Architect initiatorArchitect = (Architect)Initiator;

                bool pillagingStill = true;
                int diminishingReturnCounter = 0;

                if (WorldSubgroupManager.AdventuringLocations.Contains(initiatorArchitect.Location.Type))
                {
                    while (pillagingStill)
                    {
                        int decider = Game1.r.Next(100);

                        if (decider < 5)
                        {
                            pillagingStill = false;
                        }
                        else if (decider < 7)
                        {
                            EntityList<Structure> structures = Game1.ShuffleNewEL(initiatorArchitect.Location.AllStructures);
                            Structure selectedStructure = null;
                            Object selectedArtifact = null;

                            foreach (var structure in structures)
                            {
                                if (structure.HistoricalObjects != null && structure.HistoricalObjects.Count() > 0)
                                {
                                    selectedArtifact = structure.HistoricalObjects[Game1.r.Next(structure.HistoricalObjects.Count())];
                                    selectedStructure = structure;
                                    break;
                                }
                            }

                            if (selectedStructure != null && selectedArtifact != null)
                            {
                                LogEvent($"{initiatorArchitect.Name} took {selectedArtifact} from {selectedStructure.Name}.", initiatorArchitect.Location.Region, new EntityList<Entity>(){ initiatorArchitect, initiatorArchitect.Location });
                                initiatorArchitect.Inventory.Add(selectedArtifact);
                                selectedStructure.HistoricalObjects.Remove(selectedArtifact);
                            }
                        }
                        else if (decider < 20)
                        {
                            EntityList<Object> o = GameWorld.LootTableMachine("general");
                            initiatorArchitect.Location.Wealth -= Game1.r.Next(50, 100);
                            initiatorArchitect.Inventory.AddRange(o);
                            LogEvent($"{initiatorArchitect.Name} stole some general items from {initiatorArchitect.Location.Name}.", initiatorArchitect.Location.Region, new EntityList<Entity>(){initiatorArchitect, initiatorArchitect.Location});
                            diminishingReturnCounter++;
                            pillagingStill = false;
                        }

                        if (diminishingReturnCounter > 1)
                        {
                            pillagingStill = false;
                        }
                    }
                }
                else if (WorldSubgroupManager.VisitationLocations.Contains(initiatorArchitect.Location.Type))
                {
                    string[] shobeSubjects = new string[]
                    {
            "relationships", "mining", "combat", "crafting", "trading", "stealth", "alchemy", "cooking", "fishing",
            "hunting", "quests", "gathering", "imbuement", "healing", "navigation", "tactics", "survival",
            "diplomacy", "lockpicking", "animal taming", "herbalism", "herbs", "blacksmithing", "tailoring",
            "carpentry", "architecture", "history", "sailing", "farming", "brewing", "divination", "spellcasting",
            "negotiation", "investigation", "potions", "archery", "swordsmanship", "armor crafting", "thievery",
            "mountaineering", "cartography", "astronomy", "necromancy", "spatiomancy", "conjuromancy", "fractalmancy",
            "perceptomancy", "beasts", "divinity", "illusion", "mechanics", "engineering"
                    };

                    while (pillagingStill)
                    {
                        int decider = Game1.r.Next(100);

                        if (decider < 5)
                        {
                            District selectedDistrict = initiatorArchitect.Location.Districts[Game1.r.Next(initiatorArchitect.Location.Districts.Count())];

                            if (selectedDistrict.Architects.Count() > 0)
                            {
                                Architect selectedArch = Game1.GetRandomItem<Architect>(selectedDistrict.Architects);
                                LogEvent($"{initiatorArchitect.Name} had a lovely chat about {shobeSubjects[Game1.r.Next(shobeSubjects.Length)]} in {initiatorArchitect.Location.Name} with {selectedArch.Name}.", selectedArch.Location.Region, new EntityList<Entity>(){ Initiator, initiatorArchitect.Location, selectedArch});
                            }
                        }
                        else if (decider < 7)
                        {
                            EntityList<Object> o = GameWorld.LootTableMachine("general");
                            initiatorArchitect.Location.Wealth += Game1.r.Next(20, 50);
                            initiatorArchitect.Inventory.AddRange(o);
                            LogEvent($"{initiatorArchitect.Name} purchased some general items from {initiatorArchitect.Location.Name}.", initiatorArchitect.Location.Region, new EntityList<Entity>(){ Initiator, initiatorArchitect.Location });
                        }
                        else if (decider < 20)
                        {
                            pillagingStill = false;
                        }
                    }
                }
            }

            else if (Action == "theft")
            {
                Location location = (Location)RelatedEntities[0];
                int reputationChange = 0;

                string industry = location.Districts[Game1.r.Next(location.Districts.Count())].Industry;
                string profession = Game1.IndustryToProfession[industry];

                if (industry == "military" || industry == "waspkeeping")
                {
                    industry += " supplies";
                }

                if (Result == "MaV" || Result == "MiV")
                {
                    int stolenWealth = Game1.r.Next(100, 300);
                    location.Wealth -= stolenWealth;

                    LogEvent($"{Initiator.Name} successfully stole {industry} from {location.Name}. This has significantly impacted the area's wealth.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    reputationChange = -5;  // Higher penalty for successful thefts

                    foreach (District d in location.Districts)
                    {
                        foreach (Architect a in d.Architects)
                        {
                            if (a.Profession == profession)
                            {
                                a.Grievances.Add((Initiator.ID, $" stole {industry}, impacting {a.PossessivePronoun} livelihood."));
                            }
                        }
                    }
                }
                else if (Result == "MiD")
                {
                    LogEvent($"{Initiator.Name} attempted to steal {industry} from {location.Name} but was caught in the act. However, they managed to escape.", location.Region, new EntityList<Entity>(){ Initiator, location });
                    reputationChange = -3;

                    // Minor impact on wealth due to the failure
                    location.Wealth -= Game1.r.Next(20, 100);
                }
                else if (Result == "MaD")
                {
                    LogEvent($"{Initiator.Name} attempted to steal {industry} from {location.Name} but failed miserably and was caught.", location.Region, new EntityList<Entity>(){Initiator, location});
                    reputationChange = -10;  // Higher penalty for being caught in a major failure

                    // Very minor or no impact on wealth due to the failure
                    location.Wealth -= Game1.r.Next(0, 50);
                }

                // Apply the reputation change based on the initiator type
                if (Initiator is Architect architectInitiator)
                {
                    architectInitiator.Reputation += reputationChange;
                }
                else if (Initiator is Group groupInitiator)
                {
                    groupInitiator.Reputation += reputationChange;
                }

                Initiator.PissOffEntityOrPlace(location, true);
            }

            else if (Action == "embezzlement")
            {
                Location location = (Location)RelatedEntities[0];
                int reputationChange = 0;

                if (Result == "MaV" || Result == "MiV")
                {
                    LogEvent($"{Initiator.Name} successfully set up means to embezzle extra funding from {location.Name}'s governmental structure.", location.Region, new EntityList<Entity>(){Initiator, location});
                    reputationChange = -2;  // Slightly higher penalty for a successful embezzlement

                    location.Embezzlements.Add(Initiator);

                    if (location.Government is Group governmentGroup)
                    {
                        foreach (Architect a in governmentGroup.Architects)
                        {
                            a.Grievances.Add((Initiator.ID, $" embezzled funds from the government, undermining {a.PossessivePronoun} authority and trust."));
                        }
                    }
                    else if (location.Government is Architect governmentArchitect)
                    {
                        governmentArchitect.Grievances.Add((Initiator.ID, " embezzled funds, undermining the governance."));
                    }
                }
                else if (Result == "MiD")
                {
                    LogEvent($"{Initiator.Name} attempted to embezzle funds from {location.Name}, but the plan was discovered before it could succeed.", location.Region, new EntityList<Entity>(){Initiator, location});
                    reputationChange = -3;
                }
                else if (Result == "MaD")
                {
                    LogEvent($"{Initiator.Name} made a disastrous attempt to embezzle funds from {location.Name}, resulting in a complete failure and loss of reputation.", location.Region, new EntityList<Entity>(){Initiator, location});
                    reputationChange = -5;
                }

                // Apply the reputation change based on the initiator type
                if (Initiator is Architect architectInitiator)
                {
                    architectInitiator.Reputation += reputationChange;
                }
                else if (Initiator is Group groupInitiator)
                {
                    groupInitiator.Reputation += reputationChange;
                }

                Initiator.PissOffEntityOrPlace(location, true);
            }


            else if (Action == "craftsmanship")
            {
                Location location = (Location)RelatedEntities[0];
                int reputationChange = 0;

                Material Metal = GameWorld.Metals[Game1.r.Next(GameWorld.Metals.Count / 2)];

                List<string> metalObjects = new List<string>
    {
        "shortsword", "knife", "greatsword", "battle axe", "axe", "greataxe", "rapier", "spear", "pike",
        "pickaxe", "mace", "hammer", "shield", "whip", "scourge", "flail", "chain", "urn", "pot",
        "helmet", "forge", "jar", "bottle", "mug", "bowl", "cup", "keg", "chest", "barrel", "bin",
        "door", "small chalice", "big chalice", "altar", "table", "chair"
    };

                // Determine the creator: either an Architect or the Leader of a Group
                Architect creator = (Initiator is Architect architectInitiator) ? architectInitiator : ((Group)Initiator).Leader;

                // Create the object
                Object craftedObject = new Object("", metalObjects[Game1.r.Next(metalObjects.Count)], new EntityList<Material>() { Metal }, creator);
                craftedObject.Name = GameWorld.GenerateUniqueName("1W" + Game1.r.Next(4, 7) + "s", craftedObject);

                int decision = Game1.r.Next(1, 4);
                string initiatorName = Initiator.Name; // Use the name of the entire entity (either Architect or Group)

                if (decision == 1)
                {
                    location.Prism.HistoricalObjects.Add(craftedObject);
                    LogEvent($"{initiatorName} created the legendary {craftedObject.Type} {craftedObject.Name} in {location.Name}. It was stored in the prism of {location.Name} for safekeeping.", location.Region, new EntityList<Entity>(){Initiator, craftedObject, location});
                    reputationChange = 5;
                }
                else if (decision == 2 || location.Districts[0].Architects.Count == 0)
                {
                    creator.Inventory.Add(craftedObject);
                    LogEvent($"{initiatorName} created the legendary {craftedObject.Type} {craftedObject.Name} in {location.Name}. They held onto it for safekeeping.", location.Region, new EntityList<Entity>(){ Initiator, craftedObject, location });
                    reputationChange = 5;
                }
                else
                {
                    Architect buyer = Game1.GetRandomItem<Architect>(location.Districts[0].Architects);
                    int price = Game1.r.Next(0, buyer.Wealth);
                    buyer.Inventory.Add(craftedObject);
                    buyer.Wealth -= price;

                    creator.Wealth += price;
                    LogEvent($"{initiatorName} created the legendary {craftedObject.Type} {craftedObject.Name} in {location.Name}. They sold it to {buyer.Name} for {price}.", location.Region, new EntityList<Entity>(){ Initiator, craftedObject, location, buyer });
                    reputationChange = 5;
                }

                // Apply the reputation change to the creator (Leader if it's a Group, Architect otherwise)
                creator.Reputation += reputationChange;
            }
            else if (Action == "marriage")
            {
                Architect initiatorArchitect = (Architect)Initiator;
                Location location = (Location)RelatedEntities[0];

                bool CaresAboutMarriageRace = Game1.r.Next(15) != 1;

                foreach (Architect a in location.Districts[0].Architects)
                {
                    if (a.Spouse != null || a == initiatorArchitect || a.Sex == initiatorArchitect.Sex) continue;

                    bool similarCompasses = Math.Abs(a.MoralCompass - initiatorArchitect.MoralCompass) < 40 &&
                                            Math.Abs(a.StabilityCompass - initiatorArchitect.StabilityCompass) < 40;

                    int similarValuesCount = 0;
                    similarValuesCount += Math.Abs(a.PropertyValue - initiatorArchitect.PropertyValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.FamilyValue - initiatorArchitect.FamilyValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.PowerValue - initiatorArchitect.PowerValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.MoneyValue - initiatorArchitect.MoneyValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.KnowledgeValue - initiatorArchitect.KnowledgeValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.SpiritualityValue - initiatorArchitect.SpiritualityValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.ProwessValue - initiatorArchitect.ProwessValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.PatriotismValue - initiatorArchitect.PatriotismValue) < 4 ? 1 : 0;
                    similarValuesCount += Math.Abs(a.CourageValue - initiatorArchitect.CourageValue) < 4 ? 1 : 0;

                    bool raceConsideration = CaresAboutMarriageRace ? a.Race == initiatorArchitect.Race : true;

                    if (similarCompasses && similarValuesCount >= 3 && raceConsideration)
                    {
                        a.Spouse = initiatorArchitect;
                        initiatorArchitect.Spouse = a;

                        Structure shrine = location.AllStructures.FirstOrDefault(structure => structure.Type == "shrine");

                        if (shrine != null)
                        {
                            LogEvent($"{initiatorArchitect.Name} and {a.Name} got married at {shrine.Name} in {location.Name}.", location.Region, new EntityList<Entity>(){initiatorArchitect, a, shrine, location});
                        }
                        else
                        {
                            LogEvent($"{initiatorArchitect.Name} and {a.Name} got married in {location.Name}.", location.Region, new EntityList<Entity>(){initiatorArchitect, a, location});
                        }
                        break;
                    }
                }
            }
            else if (Action == "razebuilding")
            {
                Structure targetStructure = (Structure)RelatedEntities[1];

                if (Result == "MaV" || Result == "MiV")
                {
                    foreach (Room room in targetStructure.Rooms)
                    {
                        List<Object> objectsToRemove = room.Objects
                            .Where(obj => !obj.Type.Contains("door"))
                            .ToList();

                        foreach (Object obj in objectsToRemove)
                        {
                            room.Objects.Remove(obj);

                            if (obj.Name != null)
                            {
                                LogEvent($"{obj.Name} was destroyed in the razing of {targetStructure.Name}.", targetStructure.Block.District.Location.Region, new EntityList<Entity>(){obj, targetStructure});
                            }
                        }
                    }

                    LogEvent($"{Initiator.Name} successfully razed the building {targetStructure.Name}.", targetStructure.Block.District.Location.Region, new EntityList<Entity>(){Initiator, targetStructure});
                }
                else if (Result == "MiD")
                {
                    LogEvent($"{Initiator.Name} attempted to raze the building {targetStructure.Name}, but the effort was only partially successful.", targetStructure.Block.District.Location.Region, new EntityList<Entity>(){Initiator, targetStructure});
                }
                else if (Result == "MaD")
                {
                    LogEvent($"{Initiator.Name} failed miserably in the attempt to raze the building {targetStructure.Name}. The building remains entirely intact.", targetStructure.Block.District.Location.Region, new EntityList<Entity>(){Initiator, targetStructure});
                }

                if(targetStructure.Owner != null)
                {
                    Initiator.PissOffEntityOrPlace(targetStructure.Owner, false);
                }
                Initiator.PissOffEntityOrPlace(targetStructure.Block.District.Location, false);
            }

            else if (Action == "childbirth")
            {
                Architect initiatorArchitect = (Architect)Initiator;
                Location location = (Location)RelatedEntities[0];

                if (initiatorArchitect.Spouse != null && !initiatorArchitect.HadChildren)
                {
                    initiatorArchitect.HadChildren = true;
                    initiatorArchitect.Spouse.HadChildren = true;

                    static int GenerateChildrenNumber(Random r)
                    {
                        double lambda = 0.2; // Adjust this parameter to tweak the distribution
                        double uniformRandom = r.NextDouble();
                        double skewedRandom = -Math.Log(1 - uniformRandom) / lambda;

                        // Ensure the number is within our desired range but skewed towards lower numbers
                        int children = (int)Math.Floor(skewedRandom);

                        // Cap the result at 30 to allow for incredibly uncommon scenarios but not exceed it
                        if (children > 30) children = 30;

                        // Ensure at least 1 child
                        if (children < 1) children = 1;

                        return children;
                    }

                    int Children = GenerateChildrenNumber(Game1.r);
                    int ImportantChildren = Game1.r.Next(0, Children / 2);

                    if (ImportantChildren == 0)
                    {
                        LogEvent(initiatorArchitect.Name + " and " + initiatorArchitect.Spouse.Name + " had " + Children + " children, but none of them actually matter.", initiatorArchitect.Location.Region, new EntityList<Entity>(){initiatorArchitect, initiatorArchitect.Spouse});
                        location.Districts[Game1.r.Next(location.Districts.Count())].UnplacedPopulation += Children;
                    }
                    else
                    {
                        Race ChildRace = initiatorArchitect.Race == initiatorArchitect.Spouse.Race ? initiatorArchitect.Race : GameWorld.GetRace("archaix");

                        List<string> ImportantChildrenNames = new List<string>();
                        List<Architect> ImportantChildrenArchitects = new List<Architect>();

                        for (int i = ImportantChildren; i != 0; i--)
                        {
                            Architect a = new Architect("", Game1.Sexes[Game1.r.Next(2)], ChildRace, 0, "indolent", new EntityList<Object>(), initiatorArchitect.Location, initiatorArchitect.District, initiatorArchitect.Block, "", 0);
                            a.Name = GameWorld.GenerateUniqueArchitectName(a);
                            location.Districts[Game1.r.Next(location.Districts.Count())].Architects.Add(a);
                            ImportantChildrenNames.Add(a.Name);
                            ImportantChildrenArchitects.Add(a);
                        }
                        location.Districts[Game1.r.Next(location.Districts.Count())].UnplacedPopulation += (Children - ImportantChildren);

                        LogEvent(initiatorArchitect.Name + " and " + initiatorArchitect.Spouse.Name + " had " + Children + " children. The ones that actually matter are " + Game1.FormatList(ImportantChildrenNames) + ".", initiatorArchitect.Location.Region, new EntityList<Entity>(){initiatorArchitect, initiatorArchitect.Spouse}.Union(ImportantChildrenArchitects));
                    }
                }
            }
            return;
        }


        public static int GetEntityEffectiveness(string type, Entity initiator)
        {
            int effectiveness = 0;

            EntityList<Architect> architects = new EntityList<Architect>();

            if (initiator is Group g)
            {
                architects.AddRange(g.Architects);
            }
            else if (initiator is Architect a)
            {
                architects.Add(a);
            }
            else
            {
                return 0; // Return 0 if the initiator is neither a Group nor an Architect
            }

            foreach (Architect a in architects)
            {
                int statPoints = 0;
                int equipmentPoints = 0;

                // Calculate Stat Points
                if (type == "stealth")
                {
                    statPoints = (int)((a.Dexterity + a.Charisma + a.Creativity) * 4.0 / 3.0);
                }
                else if (type == "direct")
                {
                    statPoints = a.Strength + a.Agility + a.Focus + a.Endurance;
                }


                if (type != "stealth")
                { 
                    foreach (Object o in a.Clothing)
                    {
                        // No points for armor in stealth missions

                        equipmentPoints += o.Materials[0].Toughness / 5;
                    }
                }
               

                // Calculate Equipment Points for Weapons
                List<Object> weapons = new List<Object>();

                if (a.MainHeldObject != null && a.MainHeldObject.IsWeapon)
                {
                    weapons.Add(a.MainHeldObject);
                }
                if (a.OffHeldObject != null && a.OffHeldObject.IsWeapon)
                {
                    weapons.Add(a.OffHeldObject);
                }
                if (a.Inventory != null)
                {
                    weapons.AddRange(a.Inventory.Where(o => o.IsWeapon));
                }

                // Sort weapons by Toughness descending for the division calculation
                weapons = weapons.OrderByDescending(o => o.Materials[0].Toughness).ToList();

                for (int i = 0; i < weapons.Count; i++)
                {
                    int weaponToughness = weapons[i].Materials[0].Toughness;
                    int dividedToughness = weaponToughness / (i + 1); // i + 1 ensures no division by zero

                    if (type == "stealth")
                    {
                        dividedToughness *= 2; // Double points for stealth missions
                    }

                    equipmentPoints += dividedToughness;
                }

                // Combine Stat Points and Equipment Points to calculate effectiveness
                effectiveness += (statPoints + equipmentPoints);
            }

            return effectiveness;
        }
    }
}
