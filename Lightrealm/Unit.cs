using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Lightrealm
{
    [Serializable]
    public class Unit : Group
    {
        public int OtherSoldiers { get; set; }

        private int _homeLocationId;
        public Location HomeLocation
        {
            get => EntityGet<Location>(_homeLocationId);
            set => _homeLocationId = value?.ID ?? 0;
        }

        public string Style { get; set; } = new List<string> { "aggressive", "defensive", "evasive", "balanced", "deceptive" }[Game1.r.Next(5)];

        public Unit(Architect leader, EntityList<Architect> architects, int otherSoldiers, Location homeLocation)
        {
            Leader = leader;
            Architects = architects;
            OtherSoldiers = otherSoldiers;
            HomeLocation = homeLocation;

            foreach(Architect a in architects)
            {
                a.Unit = this;
            }

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

            // Detail the two fighting units in a single narrative sentence
            results.Add($"{this.Name}, the {this.Style.ToLower()} squad led by {this.Leader.Name} with {this.Architects.Count() + this.OtherSoldiers} soldiers, fought {u.Name}, {u.Leader.Name}'s {u.Style.ToLower()} squad of {u.Architects.Count() + u.OtherSoldiers} soldiers.");

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
            }
            else if (biomeDisadvantages[this.Style] == r.Biome)
            {
                thisStrength -= 10;
            }

            // Modify strength based on biome for opponent unit
            if (biomeAdvantages[u.Style] == r.Biome)
            {
                opponentStrength += 10;
            }
            else if (biomeDisadvantages[u.Style] == r.Biome)
            {
                opponentStrength -= 10;
            }

            // Adjust strength based on fighting style advantage
            if (this.Style == "Aggressive" && u.Style == "Defensive" ||
                this.Style == "Defensive" && u.Style == "Balanced" ||
                this.Style == "Balanced" && u.Style == "Deceptive" ||
                this.Style == "Deceptive" && u.Style == "Evasive" ||
                this.Style == "Evasive" && u.Style == "Aggressive")
            {
                thisStrength += 5;
            }
            else if (u.Style == "Aggressive" && this.Style == "Defensive" ||
                     u.Style == "Defensive" && this.Style == "Balanced" ||
                     u.Style == "Balanced" && this.Style == "Deceptive" ||
                     u.Style == "Deceptive" && this.Style == "Evasive" ||
                     u.Style == "Evasive" && this.Style == "Aggressive")
            {
                opponentStrength += 5;
            }

            // Determine the outcome and losses in a single narrative sentence
            if (thisStrength > opponentStrength)
            {
                results.Add($"{this.Name} won the fight with a historically calculated advantage of {thisStrength - opponentStrength}. {this.Name} achieved a {DetermineVictoryType(thisStrength, opponentStrength)}, while {u.Name} suffered a {DetermineLossType(thisStrength, opponentStrength)}.");
                CalculateLosses(opponentStrength, thisStrength, u, results);
            }
            else if (thisStrength < opponentStrength)
            {
                results.Add($"{u.Name} won the fight with a historically calculated advantage of {opponentStrength - thisStrength}. {u.Name} achieved a {DetermineVictoryType(opponentStrength, thisStrength)}, while {this.Name} suffered a {DetermineLossType(opponentStrength, thisStrength)}.");
                CalculateLosses(thisStrength, opponentStrength, this, results);
            }
            else
            {
                results.Add("The fight ended in a draw, with both sides incurring heavy losses but unable to secure a clear victory.");
                CalculateLosses(thisStrength, opponentStrength, this, results);
                CalculateLosses(opponentStrength, thisStrength, u, results);
            }

            return results;
        }

        private string DetermineVictoryType(int winnerStrength, int loserStrength)
        {
            int strengthDifference = winnerStrength - loserStrength;

            if (strengthDifference > 20)
            {
                return "major victory";
            }
            else if (strengthDifference > 10)
            {
                return "minor victory";
            }
            else if (strengthDifference > 5)
            {
                return "relative draw with a slight edge";
            }
            else if (strengthDifference > 0)
            {
                return "minor loss but fought valiantly";
            }
            else
            {
                return "major loss and was heavily outmatched";
            }
        }

        private string DetermineLossType(int winnerStrength, int loserStrength)
        {
            int strengthDifference = loserStrength - winnerStrength;

            if (strengthDifference > 20)
            {
                return "major loss and was heavily outmatched";
            }
            else if (strengthDifference > 10)
            {
                return "minor loss";
            }
            else if (strengthDifference > 5)
            {
                return "relative draw with a slight edge";
            }
            else if (strengthDifference > 0)
            {
                return "minor victory but struggled";
            }
            else
            {
                return "major victory";
            }
        }

        private void CalculateLosses(int loserStrength, int winnerStrength, Unit loser, List<string> results)
        {
            int strengthDifference = winnerStrength - loserStrength;
            int architectLosses = Math.Min(strengthDifference / 10, loser.Architects.Count());
            int soldierLosses = Math.Min((strengthDifference - (architectLosses * 10)) / 3, loser.OtherSoldiers);

            if (architectLosses > 0)
            {
                loser.Architects.RemoveRange(0, architectLosses);
            }

            if (soldierLosses > 0)
            {
                loser.OtherSoldiers -= soldierLosses;
            }
        }

    }
}
