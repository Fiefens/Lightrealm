using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Ruleset : Entity
    {
        public string Rules { get; private set; }
        public Dictionary<string, List<string>> Script { get; private set; }

        private List<string> selectedPieces;
        public string ObjectivePiece { get; private set; }

        public Ruleset()
        {
            Name = Game1.GameWorld.GenerateUniqueName("1S" + Game1.GameWorld.rnd.Next(2, 6) + "s", this, Game1.GameWorld.rnd);

            selectedPieces = new List<string>();

            string[] objectives = { "capture your opponent's pieces", "arrange your pieces in a specific pattern", "remove all of your opponent's pieces", "roll the highest number", "build the tallest structure", "control the board", "assemble a specific combination of pieces", "be the last player with pieces remaining", "dominate the most territories", "collect the most resources" };
            string[] setupModes = { "place all pieces on the board", "distribute pieces among players", "give each player an equal number of pieces", "arrange pieces in a circle", "scatter pieces randomly", "place pieces in a grid", "create a central pool of pieces", "divide the pieces into territories", "place pieces in alternating turns", "assign pieces based on a roll", "shuffle and deal pieces randomly", "line up pieces in rows", "assign pieces according to color", "group pieces by type", "set pieces at random positions", "arrange pieces based on player choice", "place pieces in secret locations", "build initial structures with pieces", "place pieces in a spiral pattern" };
            string[] turnOrders = { "goes clockwise based on the player with the lowest roll", "goes counterclockwise based on the player with the lowest roll", "goes clockwise based on the player with the highest roll", "is decided randomly", "starts with the player with the fewest pieces and escalates", "starts with the player with the most pieces and de-escalates", "is clockwise starting with the youngest", "is clockwise starting with the oldest", "is based on the opinion of the most experienced player" };
            string[] masterActions = { "place a piece", "move a piece", "remove a piece", "trade a piece", "roll a die", "flip a piece", "exchange a piece", "hide a piece", "steal a piece", "gain a piece" };
            string[] reactions = { "must skip their next turn", "lose a piece", "swap pieces with an opponent", "remove an opponent's piece", "draw a new piece", "forfeit their next move" };
            string[] pieceTypes = { "Stone", "Crystal", "2-sided die", "4-sided die", "6-sided die", "8-sided die", "10-sided die", "12-sided die", "20-sided die" };

            string CreateNewPiece()
            {
                string newPiece = $"{Game1.Capitalize(Game1.Colors[Game1.GameWorld.rnd.Next(Game1.Colors.Count)])} {Game1.Capitalize(pieceTypes[Game1.GameWorld.rnd.Next(pieceTypes.Length)])}";
                selectedPieces.Add(newPiece);
                return newPiece;
            }

            string GetOrCreatePiece()
            {
                if (selectedPieces.Count == 0 || Game1.GameWorld.rnd.NextDouble() < 0.5)
                {
                    return CreateNewPiece();
                }
                else
                {
                    return selectedPieces[Game1.GameWorld.rnd.Next(selectedPieces.Count)];
                }
            }

            string selectedObjective = objectives[Game1.GameWorld.rnd.Next(objectives.Length)];
            ObjectivePiece = CreateNewPiece();

            List<string> objectiveActions = new List<string>();
            switch (selectedObjective)
            {
                case "capture your opponent's pieces":
                    objectiveActions.Add($"capture an opponent's {ObjectivePiece}");
                    objectiveActions.Add($"defend their {ObjectivePiece}");
                    break;
                case "arrange your pieces in a specific pattern":
                    objectiveActions.Add($"place a {ObjectivePiece} in the pattern");
                    objectiveActions.Add($"move a {ObjectivePiece} to form the pattern");
                    break;
                case "remove all of your opponent's pieces":
                    objectiveActions.Add($"remove an opponent's {ObjectivePiece}");
                    objectiveActions.Add($"replace an opponent's {ObjectivePiece} with their {GetOrCreatePiece()}");
                    break;
                case "roll the highest number":
                    objectiveActions.Add($"roll a {ObjectivePiece}");
                    objectiveActions.Add($"reroll a {ObjectivePiece}");
                    break;
                case "build the tallest structure":
                    objectiveActions.Add($"stack a {ObjectivePiece}");
                    objectiveActions.Add($"stabilize a {ObjectivePiece} in the structure");
                    break;
                case "control the board":
                    objectiveActions.Add($"place a {ObjectivePiece} to control an area");
                    objectiveActions.Add($"move a {ObjectivePiece} to gain control");
                    break;
                case "assemble a specific combination of pieces":
                    objectiveActions.Add($"collect a {ObjectivePiece}");
                    objectiveActions.Add($"combine a {ObjectivePiece} with a {GetOrCreatePiece()}");
                    break;
                case "be the last player with pieces remaining":
                    objectiveActions.Add($"eliminate an opponent's {ObjectivePiece}");
                    objectiveActions.Add($"protect their {ObjectivePiece}");
                    break;
                case "dominate the most territories":
                    objectiveActions.Add($"claim a territory with a {ObjectivePiece}");
                    objectiveActions.Add($"fortify a territory with a {ObjectivePiece}");
                    break;
                case "collect the most resources":
                    objectiveActions.Add($"harvest resources based on the represented number of your {ObjectivePiece}s");
                    objectiveActions.Add($"trade resources based on the represented number of your {ObjectivePiece}s");
                    break;
            }

            if (Game1.GameWorld.rnd.NextDouble() < 0.8)
            {
                objectiveActions.Add("roll a die");
            }

            List<string> selectedActions = objectiveActions
                .Concat(masterActions.OrderBy(x => Game1.GameWorld.rnd.Next()).Take(Game1.GameWorld.rnd.Next(3, 7 - objectiveActions.Count)))
                .ToList();

            // Ensure no duplicates in selectedActions
            selectedActions = selectedActions.Distinct().ToList();

            List<string> selectedSetup = new List<string>();
            int setupSteps = Game1.GameWorld.rnd.Next(2, 5);
            for (int i = 0; i < setupSteps; i++)
            {
                string setupStep = setupModes[Game1.GameWorld.rnd.Next(setupModes.Length)];
                selectedSetup.Add(setupStep.Replace("pieces", Game1.Capitalize(GetOrCreatePiece() + "s")));
            }

            string selectedTurnOrder = turnOrders[Game1.GameWorld.rnd.Next(turnOrders.Length)];

            int numReactions = Game1.GameWorld.rnd.Next(2, 5);
            Dictionary<string, string> selectedReactions = new Dictionary<string, string>();
            int maxAttempts = 10; // To avoid infinite loops, limit the number of attempts to find a unique pair

            for (int i = 0; i < numReactions; i++)
            {
                string selectedReaction;
                do
                {
                    selectedReaction = reactions[Game1.GameWorld.rnd.Next(reactions.Length)];
                } while (selectedReaction.Contains("gain a bonus action") || selectedReaction.Contains("gain an extra turn"));

                string triggerAction;
                int attempts = 0;

                do
                {
                    triggerAction = selectedActions[Game1.GameWorld.rnd.Next(selectedActions.Count)];
                    attempts++;
                }
                while (selectedReactions.ContainsKey(triggerAction) && attempts < maxAttempts);

                // Add the unique pair to the dictionary if it's unique and the limit of attempts hasn't been reached
                if (!selectedReactions.ContainsKey(triggerAction))
                {
                    selectedReactions.Add(triggerAction, $"they {selectedReaction}");
                }
            }

            Script = new Dictionary<string, List<string>>
        {
            { "Objective", new List<string> { selectedObjective.Replace("pieces", Game1.Capitalize(ObjectivePiece + "s")) } },
            { "Setup", selectedSetup.Select(s => s.Replace("pieces", ObjectivePiece + "s")).ToList() },
            { "TurnOrder", new List<string> { selectedTurnOrder } },
            { "Actions", selectedActions.Select(a => $"they {a.Replace("piece", ObjectivePiece)}").ToList() },
            { "Reactions", selectedReactions.Select(kvp => $"When they {kvp.Key}, {kvp.Value}").ToList() }
        };

            Rules = $"The objective of {Name} is to {Script["Objective"].First()}. " +
                    $"To set up the game, {Game1.FormatAndList(Script["Setup"])}. " +
                    $"Turn order {Script["TurnOrder"].First()}. " +
                    $"On their turn, a player can {Game1.FormatOrList(Script["Actions"].Select(a => a.Replace("they ", "")).ToList())}. " +
                    $"{Game1.FormatAndList(Script["Reactions"])}.";
        }
    }


    [Serializable]
    public class Boardgame : Entity
    {
        public Ruleset GameRules { get; private set; }
        public int CurrentTurn { get; private set; }
        public int MaxTurns { get; private set; }
        public List<string> playerNames;
        public List<string> randomizedPlayerOrder;
        public Dictionary<string, int> playerPoints;

        public EntityList<Architect> Players = new EntityList<Architect>();

        public bool SetupComplete = false;

        public Boardgame(Ruleset rules, EntityList<Architect> players, int rounds)
        {
            GameRules = rules;
            playerNames = players.Select(p => p.Name).ToList();
            CurrentTurn = 0;
            MaxTurns = players.Count() * rounds;
            playerPoints = playerNames.ToDictionary(name => name, name => 0);
            randomizedPlayerOrder = playerNames.OrderBy(x => Guid.NewGuid()).ToList();

            Name = rules.Name;

            Players.AddRange(players);
        }

        public string SimulateGame()
        {
            StringBuilder gameNarrative = new StringBuilder();

            if (CurrentTurn == 0)
            {
                Setup(gameNarrative);
            }

            while (CurrentTurn < MaxTurns)
            {
                SimulateTurn(gameNarrative);
            }

            // Replace "dies" with "dice" in the entire narrative before returning
            gameNarrative.Replace("dies", "dice");
            return gameNarrative.ToString();
        }

        public string SimulateTurn(StringBuilder narrative)
        {
            int playerIndex = CurrentTurn % randomizedPlayerOrder.Count;
            string player = randomizedPlayerOrder[playerIndex];

            StringBuilder turnNarrative = new StringBuilder();

            var selectedAction = GameRules.Script["Actions"][Game1.GameWorld.rnd.Next(GameRules.Script["Actions"].Count)];
            turnNarrative.Append($"On {player}'s turn, {selectedAction}. ");

            // Handle Reactions
            HandleReactions(turnNarrative, selectedAction);

            // Handle scoring or other game logic
            if (selectedAction.Contains(GameRules.ObjectivePiece))
            {
                playerPoints[player]++;
            }

            CurrentTurn++;

            if (CurrentTurn >= MaxTurns)
            {
                var winner = playerPoints.OrderByDescending(kvp => kvp.Value).First().Key;
                turnNarrative.Append($"The game has concluded. The winner is {winner}!");
            }

            // Append the turn's narrative to the overall narrative
            narrative.Append(turnNarrative.ToString());

            // Return the turn's narrative as a string
            turnNarrative.Replace("dies", "dice");
            return turnNarrative.ToString();
        }


        private void HandleReactions(StringBuilder narrative, string action)
        {
            foreach (var reaction in GameRules.Script["Reactions"])
            {
                var triggerAction = reaction.Split(',')[0].Replace("When they ", "").Trim();
                if (action.Contains(triggerAction))
                {
                    string reactionEffect = reaction.Split(',')[1].Trim();
                    narrative.Append($"As a result, {reactionEffect}. ");
                }
            }
        }

        private string Setup(StringBuilder setupLog)
        {
            StringBuilder setupNarrative = new StringBuilder();

            string challenger = playerNames[0];
            string gameName = this.GameRules.Name;
            setupNarrative.Append($"{challenger} challenges {Game1.FormatAndList(playerNames.Skip(1).ToList())} to a game of {gameName}. ");

            setupNarrative.Append($"{string.Join(" and ", playerNames)} ");
            setupNarrative.Append($"{Game1.FormatAndList(GameRules.Script["Setup"])}. ");

            // Append the setup's narrative to the overall log
            setupLog.Append(setupNarrative.ToString());

            SetupComplete = true;

            // Return the setup's narrative as a string
            return setupNarrative.ToString();
        }
    }
}
