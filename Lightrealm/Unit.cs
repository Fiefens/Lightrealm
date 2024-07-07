using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class Unit : Entity
    {
        private int _leaderId;
        public Architect Leader
        {
            get => EntityGet<Architect>(_leaderId);
            set => _leaderId = value?.ID ?? 0;
        }

        public List<Architect> Architects { get; set; } = new List<Architect>();

        public int OtherSoldiers { get; set; }

        private int _homeLocationId;
        public Location HomeLocation
        {
            get => EntityGet<Location>(_homeLocationId);
            set => _homeLocationId = value?.ID ?? 0;
        }

        public string Style { get; set; } = new List<string> { "aggressive", "defensive", "evasive", "balanced", "deceptive" }[Game1.r.Next(5)];

        public Unit(Architect leader, List<Architect> architects, int otherSoldiers, Location homeLocation)
        {
            Leader = leader;
            Architects = architects;
            OtherSoldiers = otherSoldiers;
            HomeLocation = homeLocation;

            Name = Game1.GameWorld.GenerateUniqueName("1W1w", this);
        }

        public Unit()
        {

        }

        public int CombatStrength()
        {
            int architectStrength = Architects.Sum(architect => architect.Strength);
            int otherSoldierStrength = OtherSoldiers * 3;
            return architectStrength + otherSoldierStrength;
        }

        public List<string> Fight(Unit u, Region r)
        {
            var results = new List<string>();

            // Detail the two fighting units
            results.Add($"{this.Name}: {this.Leader.Name}'s unit, Style: {this.Style}, Architects: {this.Architects.Count()}, Other Soldiers: {this.OtherSoldiers}");
            results.Add($"{u.Name}: {u.Leader.Name}'s unit, Style: {u.Style}, Architects: {u.Architects.Count()}, Other Soldiers: {u.OtherSoldiers}");

            // Define biome advantages and disadvantages
            var biomeAdvantages = new Dictionary<string, string>
            {
                { "aggressive", "plains" },
                { "defensive", "mountain" },
                { "balanced", "tundra" },
                { "deceptive", "taiga" },
                { "evasive", "forest" }
            };

            var biomeDisadvantages = new Dictionary<string, string>
            {
                { "aggressive", "forest" },
                { "defensive", "desert" },
                { "balanced", "ocean" },
                { "deceptive", "lightforest" },
                { "evasive", "snowpeak" }
            };

            // Determine combat strengths
            int thisStrength = this.CombatStrength();
            int opponentStrength = u.CombatStrength();

            // Modify strength based on biome for this unit
            if (biomeAdvantages[this.Style] == r.Biome)
            {
                thisStrength += 10;
                results.Add($"{this.Name}'s unit ({this.Style}) has an advantage in {r.Biome} biome due to favorable terrain for their tactics.");
            }
            else if (biomeDisadvantages[this.Style] == r.Biome)
            {
                thisStrength -= 10;
                results.Add($"{this.Name}'s unit ({this.Style}) has a disadvantage in {r.Biome} biome due to unfavorable terrain.");
            }

            // Modify strength based on biome for opponent unit
            if (biomeAdvantages[u.Style] == r.Biome)
            {
                opponentStrength += 10;
                results.Add($"{u.Name}'s unit ({u.Style}) has an advantage in {r.Biome} biome due to favorable terrain for their tactics.");
            }
            else if (biomeDisadvantages[u.Style] == r.Biome)
            {
                opponentStrength -= 10;
                results.Add($"{u.Name}'s unit ({u.Style}) has a disadvantage in {r.Biome} biome due to unfavorable terrain.");
            }

            // Adjust strength based on fighting style advantage
            if (this.Style == "Aggressive" && u.Style == "Defensive" ||
                this.Style == "Defensive" && u.Style == "Balanced" ||
                this.Style == "Balanced" && u.Style == "Deceptive" ||
                this.Style == "Deceptive" && u.Style == "Evasive" ||
                this.Style == "Evasive" && u.Style == "Aggressive")
            {
                thisStrength += 5;
                results.Add($"{this.Name}'s unit ({this.Style}) has a general fighting style advantage over {u.Name}'s unit ({u.Style}).");
            }
            else if (u.Style == "Aggressive" && this.Style == "Defensive" ||
                     u.Style == "Defensive" && this.Style == "Balanced" ||
                     u.Style == "Balanced" && this.Style == "Deceptive" ||
                     u.Style == "Deceptive" && this.Style == "Evasive" ||
                     u.Style == "Evasive" && this.Style == "Aggressive")
            {
                opponentStrength += 5;
                results.Add($"{u.Name}'s unit ({u.Style}) has a general fighting style advantage over {this.Name}'s unit ({this.Style}).");
            }

            // Determine the outcome and losses
            if (thisStrength > opponentStrength)
            {
                results.Add($"{this.Name} wins the fight with {thisStrength - opponentStrength} strength advantage.");
                DetermineVictoryType(thisStrength, opponentStrength, results, this.Name);
                CalculateLosses(opponentStrength, thisStrength, u, results);
            }
            else if (thisStrength < opponentStrength)
            {
                results.Add($"{u.Name} wins the fight with {opponentStrength - thisStrength} strength advantage.");
                DetermineVictoryType(opponentStrength, thisStrength, results, u.Name);
                CalculateLosses(thisStrength, opponentStrength, this, results);
            }
            else
            {
                results.Add("The fight is a draw.");
                results.Add("Both units took heavy losses, but neither could claim a clear victory.");
                CalculateLosses(thisStrength, opponentStrength, this, results);
                CalculateLosses(opponentStrength, thisStrength, u, results);
            }

            return results;
        }

        private void DetermineVictoryType(int winnerStrength, int loserStrength, List<string> results, string winnerName)
        {
            int strengthDifference = winnerStrength - loserStrength;

            if (strengthDifference > 20)
            {
                results.Add($"{winnerName} achieved a major victory.");
            }
            else if (strengthDifference > 10)
            {
                results.Add($"{winnerName} achieved a minor victory.");
            }
            else if (strengthDifference > 5)
            {
                results.Add($"{winnerName} achieved a relative draw, with a slight edge.");
            }
            else if (strengthDifference > 0)
            {
                results.Add($"{winnerName} achieved a minor loss but fought valiantly.");
            }
            else
            {
                results.Add($"{winnerName} achieved a major loss and was heavily outmatched.");
            }
        }

        private void CalculateLosses(int loserStrength, int winnerStrength, Unit loser, List<string> results)
        {
            int strengthDifference = winnerStrength - loserStrength;
            int architectLosses = Math.Min(strengthDifference / 10, loser.Architects.Count());
            int soldierLosses = Math.Min((strengthDifference - (architectLosses * 10)) / 3, loser.OtherSoldiers);

            if (architectLosses > 0)
            {
                List<string> architectNames = new List<string>();
                for (int i = 0; i < architectLosses; i++)
                {
                    architectNames.Add(loser.Architects[i].Name);
                }

                string formattedArchitectNames = Game1.FormatList(architectNames);
                results.Add($"{loser.Name} loses {architectLosses} Architects: {formattedArchitectNames}");

                loser.Architects.RemoveRange(0, architectLosses);
            }

            if (soldierLosses > 0)
            {
                results.Add($"{loser.Name} loses {soldierLosses} general soldiers.");
                loser.OtherSoldiers -= soldierLosses;
            }
        }
    }
}
