// Street dice, from the web build (matpcul-tech/StreetEmpire src/state.js, DICE, diceRoll, diceAction, diceStep):
// craps against the corner's pot, on a block you hold that runs a dice game. You are the shooter: 7 or 11 on the
// first roll wins, 2, 3 or 12 loses, anything else is your point, and then you roll until the point comes back
// (win) or a 7 does (lose). A win pays even money out of the pot; a loss goes into it. The pot is the corner's own
// money, not your stash: it refills slowly, so a hot hand can clean it out and has to come back later.
using System;

namespace StreetEmpire.Sim
{
    /// <summary>A bet on the table: which block's game, how much, the point (0 before the first roll) and rolls so far.</summary>
    [Serializable]
    public class DiceBet { public int block = -1, bet, point, n; }

    /// <summary>One throw. result: "win", "lose", "point" (the first roll set the point) or "roll" (neither came).</summary>
    public struct DiceThrow { public int a, b, sum, point, bet, pay; public string result; }

    public static class Dice
    {
        public static readonly int[] Bets = { 5, 10, 25, 50 };
        /// <summary>How close you stand to play, and how far away walks off a live bet (meters).</summary>
        public const float Reach = 3.4f, Leave = 6.5f;
        /// <summary>Dollars a second a pot below its cap comes back.</summary>
        public const float Refill = 1f / 3f;

        /// <summary>sceneOf(b) === 'dice': every third block's corner runs the dice game (the others get cards and the stoop).</summary>
        public static bool Runs(TurfBlock b) => b != null && b.id % 3 == 0;

        public static float PotCap(Data.WorldFile w, TurfBlock b) => (40f + b.rate * 10f) * (b.built ? w.buildBoost : 1f);

        /// <summary>The pot, full the first time anyone looks.</summary>
        public static float Pot(Data.WorldFile w, TurfBlock b)
        {
            if (!b.hasPot) { b.hasPot = true; b.pot = PotCap(w, b); }
            return b.pot;
        }

        /// <summary>Why this bet cannot go down ("" when it can): not your dice corner, not a bet, no cash, or a pot too thin to cover it.</summary>
        public static string Refuse(TurfState s, Data.WorldFile w, TurfBlock b, int bet)
        {
            if (b == null || b.owner != Turf.You || !Runs(b)) return "No game here.";
            if (Array.IndexOf(Bets, bet) < 0) return "No such bet.";
            if (s.Crew(Turf.You).cash < bet) return $"You need ${bet} to get in.";
            if (Pot(w, b) < bet) return $"The pot at {b.hustle} cannot cover ${bet}.";
            return "";
        }

        /// <summary>Put the bet down (the cash leaves the crew now). Null when Refuse says no. The first roll is the caller's next step.</summary>
        public static DiceBet Open(TurfState s, Data.WorldFile w, TurfBlock b, int bet)
        {
            if (Refuse(s, w, b, bet).Length > 0) return null;
            s.Crew(Turf.You).cash -= bet;
            return new DiceBet { block = b.id, bet = bet };
        }

        /// <summary>Throw both dice from the game's own generator.</summary>
        public static DiceThrow Roll(TurfState s, Data.WorldFile w, DiceBet g)
        {
            int a = 1 + (int)Math.Floor(Turf.R(s) * 6), c = 1 + (int)Math.Floor(Turf.R(s) * 6);
            return Settle(s, w, g, a, c);
        }

        /// <summary>What a throw of a and c does to the bet. Tests call it with the faces they want.</summary>
        public static DiceThrow Settle(TurfState s, Data.WorldFile w, DiceBet g, int a, int c)
        {
            var b = s.Block(g.block); int sum = a + c; g.n++;
            string result = "roll";
            if (g.point == 0)
            {
                if (sum == 7 || sum == 11) result = "win";
                else if (sum == 2 || sum == 3 || sum == 12) result = "lose";
                else { g.point = sum; result = "point"; }
            }
            else if (sum == g.point) result = "win";
            else if (sum == 7) result = "lose";
            int pay = 0;
            if (result == "win") { pay = g.bet * 2; s.Crew(Turf.You).cash += pay; b.pot = Math.Max(0f, Pot(w, b) - g.bet); }
            else if (result == "lose") b.pot = Math.Min(PotCap(w, b) * 2f, Pot(w, b) + g.bet);
            return new DiceThrow { a = a, b = c, sum = sum, point = g.point, bet = g.bet, pay = pay, result = result };
        }

        /// <summary>The shooter walked off, got in a car, went down or lost the block with a bet live: the pot keeps it.</summary>
        public static void Forfeit(TurfState s, Data.WorldFile w, DiceBet g)
        {
            var b = s.Block(g.block); if (b == null) return;
            b.pot = Math.Min(PotCap(w, b) * 2f, Pot(w, b) + g.bet);
        }

        /// <summary>Pots that have been played come back up to their cap.</summary>
        public static void Step(TurfState s, Data.WorldFile w, float dt)
        {
            foreach (var b in s.blocks)
                if (b.hasPot) { float cap = PotCap(w, b); if (b.pot < cap) b.pot = Math.Min(cap, b.pot + dt * Refill); }
        }
    }
}
