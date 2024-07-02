using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Imbuement : Entity
    {
        public bool IsTrigger { get; set; }
        public string ConditionOrTrigger { get; set; }
        public string BuffOrResult { get; set; }
        public int FirstPower { get; set; }
        public int SecondPower { get; set; }

        public bool IsSatisfied { get; set; }

        public Imbuement(bool isTrigger, string conditionOrTrigger, string buffOrResult, int firstPower, int secondPower)
        {
            IsTrigger = isTrigger;
            ConditionOrTrigger = conditionOrTrigger;
            BuffOrResult = buffOrResult;
            FirstPower = firstPower;
            SecondPower = secondPower;
        }

        public string GetDescription()
        {
            string conditionDescription = GetConditionDescription();
            string buffDescription = GetBuffDescription();

            return conditionDescription + buffDescription;
        }

        private string GetConditionDescription()
        {
            string description = "";
            if (IsTrigger)
            {
                // Logic for triggers
                if (IsTrigger)
                {
                    // Logic for triggers
                    switch (ConditionOrTrigger)
                    {
                        case "onjump":
                            description = "When you jump over an attack, ";
                            break;
                        case "onduck":
                            description = "When you duck under an attack, ";
                            break;
                        case "onroll":
                            description = "When you roll away from an attack, ";
                            break;
                        case "onblock":
                            description = "When you block an attack with a shield, ";
                            break;
                        case "onparry":
                            description = "When you parry an attack, ";
                            break;
                        case "onredirect":
                            description = "When you redirect an attack, ";
                            break;
                        case "oncast":
                            description = "When you cast a spell, ";
                            break;
                        case "ondamage":
                            description = "When you are successfully attacked, ";
                            break;
                        case "onattack":
                            description = "When you successfully attack, ";
                            break;
                    }
                }
            }
            else
            {
                // Logic for conditions
                switch (ConditionOrTrigger)
                {
                    case "multipleenemies":
                        description = "When fighting multiple enemies, ";
                        break;
                    case "grounded":
                        description = "When on the ground, ";
                        break;
                    case "diminished":
                        description = $"When energy is below {FirstPower}, ";
                        break;
                    case "lowlight":
                        description = "When in low light, ";
                        break;
                    case "stagnant":
                        description = "When you have spent 7 seconds in one block or room, ";
                        break;
                    case "maxenergy":
                        description = "When energy is maxed, ";
                        break;
                }
            }
            return description;
        }

        private string GetBuffDescription()
        {
            string description = "";
            if (IsTrigger)
            {
                switch (BuffOrResult)
                {
                    case "barrier":
                        description = $"generate a barrier stack.";
                        break;
                    case "projectile":
                        description = $"fire a bolt at the nearest hostile.";
                        break;
                    case "ignite":
                        description = $"ignite the nearest hostile.";
                        break;
                    case "destabilize":
                        description = $"destabilize the nearest hostile.";
                        break;
                    case "dismiss":
                        description = $"gain dismissal for three seconds.";
                        break;
                }
            }
            else
            {
                switch (BuffOrResult)
                {
                    case "+attack":
                        description = $"increase attack power by {SecondPower}%.";
                        break;
                    case "+shield":
                        description = $"increase shield effectiveness by {SecondPower}%.";
                        break;
                    case "+dodge":
                        description = $"increase roll chance by {SecondPower}%.";
                        break;
                    case "+redirection":
                        description = $"increase redirection chance by {SecondPower}%.";
                        break;
                    case "+bash":
                        description = $"increase bashing resistance by {SecondPower}%.";
                        break;
                    case "+pierce":
                        description = $"increase piercing resistance by {SecondPower}%.";
                        break;
                    case "+slash":
                        description = $"increase slashing resistance by {SecondPower}%.";
                        break;
                    case "+scourge":
                        description = $"increase scourging resistance by {SecondPower}%.";
                        break;
                    case "+stealth":
                        description = "become slightly harder to see and target.";
                        break;
                    case "+heal":
                        description = $"enhance healing capability by {SecondPower}%.";
                        break;
                    case "+regen":
                        description = $"regenerate {SecondPower} energy per second.";
                        break;
                }
            }
            return description;
        }

    }
}
