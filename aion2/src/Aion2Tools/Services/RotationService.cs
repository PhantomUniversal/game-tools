using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>A main/alt rotation over several runs of the same content. Each player brings one character per run;
/// each main plays a set number of runs, spread evenly so every run gets its share of mains, and the player's
/// alts fill the other runs. Which runs a main takes is then searched so each run composes as well as it can.</summary>
public static class RotationService
{
    private const int OUT = -1;
    private const int MAIN = 0;
    private const double MAIN_SPREAD_PENALTY = 5000;
    private const double PLAY_SPREAD_PENALTY = 200;
    private const double EPSILON = 0.0001;
    private const int TIME_BUDGET_MILLISECONDS = 3000;

    public static IReadOnlyList<PartyResultModel> Compose(
        IReadOnlyList<CharacterData> characters, PresetRecord preset, GameDataTable data, int runCount, int mainRunCount, int seed)
    {
        List<Player> players = characters
            .Where(character => PartyService.GetCutReason(character, preset, data).Length == 0)
            .GroupBy(character => character.Number)
            .OrderBy(group => group.Key)
            .Select(group => new Player(group.ToList()))
            .ToList();
        if (players.Count == 0 || runCount <= 0)
        {
            return Array.Empty<PartyResultModel>();
        }

        Plan plan = new Plan(players, preset, data, runCount, Math.Clamp(mainRunCount, 0, runCount), seed);
        plan.Fill();
        plan.Climb();
        return plan.BuildResults();
    }

    /// <summary>One number's characters that pass the cuts: index 0 is the main when it passes, the rest are alts.</summary>
    private sealed class Player
    {
        public List<CharacterData> Characters { get; }

        public bool HasMain { get; }

        public int AltCount => HasMain ? Characters.Count - 1 : Characters.Count;

        public Player(List<CharacterData> characters)
        {
            Characters = characters.OrderByDescending(character => character.IsMain).ToList();
            HasMain = Characters[0].IsMain;
        }

        public int FirstAlt => HasMain ? 1 : 0;
    }

    /// <summary>The schedule: for each run and player, the character index they bring, or OUT.</summary>
    private sealed class Plan
    {
        private readonly List<Player> _players;
        private readonly PresetRecord _preset;
        private readonly GameDataTable _data;
        private readonly int _runCount;
        private readonly int _mainRunCount;
        private readonly int _seatCount;
        private readonly int _seed;
        private readonly int[,] _picks;
        private readonly Dictionary<string, double> _runScores = new Dictionary<string, double>();

        public Plan(List<Player> players, PresetRecord preset, GameDataTable data, int runCount, int mainRunCount, int seed)
        {
            _players = players;
            _preset = preset;
            _data = data;
            _runCount = runCount;
            _mainRunCount = mainRunCount;
            _seatCount = preset.PartyCount * preset.PartySize;
            _seed = seed;
            _picks = new int[runCount, players.Count];
        }

        /// <summary>Mains go to the runs with the fewest mains so far; the free seats go to the players
        /// who have played least, each bringing their least used alt.</summary>
        public void Fill()
        {
            int[] mains = new int[_runCount];
            int[] plays = new int[_players.Count];
            int[,] altUses = new int[_players.Count, _players.Max(player => player.Characters.Count)];
            for (int run = 0; run < _runCount; run++)
            {
                for (int player = 0; player < _players.Count; player++)
                {
                    _picks[run, player] = OUT;
                }
            }

            for (int player = 0; player < _players.Count; player++)
            {
                if (!_players[player].HasMain)
                {
                    continue;
                }

                for (int count = 0; count < _mainRunCount; count++)
                {
                    int best = OUT;
                    for (int offset = 0; offset < _runCount; offset++)
                    {
                        int run = (player + offset) % _runCount;
                        if (_picks[run, player] == OUT && (best == OUT || mains[run] < mains[best]))
                        {
                            best = run;
                        }
                    }

                    _picks[best, player] = MAIN;
                    mains[best]++;
                    plays[player]++;
                }
            }

            for (int run = 0; run < _runCount; run++)
            {
                int seated = CountSeated(run);
                List<int> order = Enumerable.Range(0, _players.Count)
                    .Where(player => _picks[run, player] == OUT && _players[player].AltCount > 0)
                    .OrderBy(player => plays[player])
                    .ThenBy(player => (player + run) % _players.Count)
                    .ToList();
                foreach (int player in order)
                {
                    if (seated >= _seatCount)
                    {
                        break;
                    }

                    Player owner = _players[player];
                    int alt = Enumerable.Range(owner.FirstAlt, owner.AltCount).OrderBy(index => altUses[player, index]).First();
                    _picks[run, player] = alt;
                    altUses[player, alt]++;
                    plays[player]++;
                    seated++;
                }
            }
        }

        /// <summary>Hill climbing over three moves until nothing improves or the time is up:
        /// swap one player's picks between two runs, bring a different alt, or trade a seat with a player sitting out.</summary>
        public void Climb()
        {
            Stopwatch watch = Stopwatch.StartNew();
            double current = Score();
            bool isImproved = true;
            while (isImproved && watch.ElapsedMilliseconds < TIME_BUDGET_MILLISECONDS)
            {
                isImproved = false;
                for (int player = 0; player < _players.Count; player++)
                {
                    for (int first = 0; first < _runCount; first++)
                    {
                        for (int second = first + 1; second < _runCount; second++)
                        {
                            if (_picks[first, player] == _picks[second, player] || !CanSwapRuns(player, first, second))
                            {
                                continue;
                            }

                            SwapRuns(player, first, second);
                            if (TryKeep(ref current))
                            {
                                isImproved = true;
                            }
                            else
                            {
                                SwapRuns(player, first, second);
                            }
                        }
                    }

                    Player owner = _players[player];
                    for (int run = 0; run < _runCount; run++)
                    {
                        int from = _picks[run, player];
                        if (from == OUT || (owner.HasMain && from == MAIN))
                        {
                            continue;
                        }

                        for (int alt = owner.FirstAlt; alt < owner.Characters.Count; alt++)
                        {
                            if (alt == from)
                            {
                                continue;
                            }

                            _picks[run, player] = alt;
                            if (TryKeep(ref current))
                            {
                                from = alt;
                                isImproved = true;
                            }
                            else
                            {
                                _picks[run, player] = from;
                            }
                        }
                    }
                }

                for (int run = 0; run < _runCount; run++)
                {
                    for (int seated = 0; seated < _players.Count; seated++)
                    {
                        int pick = _picks[run, seated];
                        if (pick == OUT || (_players[seated].HasMain && pick == MAIN))
                        {
                            continue;
                        }

                        for (int waiting = 0; waiting < _players.Count; waiting++)
                        {
                            if (_picks[run, waiting] != OUT || _players[waiting].AltCount == 0)
                            {
                                continue;
                            }

                            _picks[run, seated] = OUT;
                            _picks[run, waiting] = _players[waiting].FirstAlt;
                            if (TryKeep(ref current))
                            {
                                isImproved = true;
                                break;
                            }

                            _picks[run, waiting] = OUT;
                            _picks[run, seated] = pick;
                        }
                    }
                }
            }
        }

        public IReadOnlyList<PartyResultModel> BuildResults()
        {
            List<PartyResultModel> results = new List<PartyResultModel>();
            for (int run = 0; run < _runCount; run++)
            {
                IReadOnlyList<PartyResultModel> composed = PartyService.Compose(GetRunCharacters(run), _preset, _data, 1, _seed + run);
                if (composed.Count == 0)
                {
                    break;
                }

                results.Add(composed[0]);
            }

            return results;
        }

        private bool TryKeep(ref double current)
        {
            double score = Score();
            if (score > current + EPSILON)
            {
                current = score;
                return true;
            }

            return false;
        }

        /// <summary>Each run's best composition, less the penalties for uneven mains per run and uneven play counts.</summary>
        private double Score()
        {
            double score = 0;
            int[] mains = new int[_runCount];
            int[] plays = new int[_players.Count];
            for (int run = 0; run < _runCount; run++)
            {
                score += GetRunScore(run);
                for (int player = 0; player < _players.Count; player++)
                {
                    int pick = _picks[run, player];
                    if (pick == OUT)
                    {
                        continue;
                    }

                    plays[player]++;
                    if (_players[player].HasMain && pick == MAIN)
                    {
                        mains[run]++;
                    }
                }
            }

            double mainAverage = mains.Average();
            double playAverage = plays.Average();
            score -= mains.Sum(count => Math.Abs(count - mainAverage)) * MAIN_SPREAD_PENALTY;
            score -= plays.Sum(count => Math.Abs(count - playAverage)) * PLAY_SPREAD_PENALTY;
            return score;
        }

        /// <summary>Cached by who plays, since most moves change only one or two runs.</summary>
        private double GetRunScore(int run)
        {
            List<CharacterData> characters = GetRunCharacters(run);
            string key = string.Join(",", Enumerable.Range(0, _players.Count).Select(player => _picks[run, player]));
            double score;
            if (_runScores.TryGetValue(key, out score))
            {
                return score;
            }

            IReadOnlyList<PartyResultModel> composed = PartyService.Compose(characters, _preset, _data, 1, _seed + run);
            score = composed.Count == 0 ? 0 : composed[0].Score;
            _runScores[key] = score;
            return score;
        }

        private List<CharacterData> GetRunCharacters(int run)
        {
            List<CharacterData> characters = new List<CharacterData>();
            for (int player = 0; player < _players.Count; player++)
            {
                int pick = _picks[run, player];
                if (pick != OUT)
                {
                    characters.Add(_players[player].Characters[pick]);
                }
            }

            return characters;
        }

        private int CountSeated(int run)
        {
            int seated = 0;
            for (int player = 0; player < _players.Count; player++)
            {
                if (_picks[run, player] != OUT)
                {
                    seated++;
                }
            }

            return seated;
        }

        /// <summary>Swapping a seat for a sit-out moves a seat between the runs; the gaining run must have room.</summary>
        private bool CanSwapRuns(int player, int first, int second)
        {
            if (_picks[first, player] == OUT)
            {
                return CountSeated(first) < _seatCount;
            }

            if (_picks[second, player] == OUT)
            {
                return CountSeated(second) < _seatCount;
            }

            return true;
        }

        private void SwapRuns(int player, int first, int second)
        {
            (_picks[first, player], _picks[second, player]) = (_picks[second, player], _picks[first, player]);
        }
    }
}
