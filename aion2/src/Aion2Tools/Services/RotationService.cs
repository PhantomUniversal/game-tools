using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>A main/alt rotation over several runs of the same content. Each player brings one character per run.
/// Each main plays a set number of runs, back to back where it can, and the players' alts fill the other seats.
/// When characters may repeat (원정) the mains are spread evenly over the runs; under an entry limit (성역, once
/// each) filling the runs comes first, the early runs before the late ones. The schedule is then searched so each
/// run composes as well as it can.</summary>
public static class RotationService
{
    private const int OUT = -1;
    private const int MAIN = 0;
    private const double MAIN_SPREAD_PENALTY = 5000;
    private const double SPLIT_TURN_PENALTY = 400;
    private const double PLAY_SPREAD_PENALTY = 200;

    /// <summary>Per empty seat, times how many runs come after it: when characters run short, the gaps go to the last runs.</summary>
    private const double EARLY_EMPTY_SEAT_PENALTY = 1500;
    private const double EPSILON = 0.0001;
    private const int TIME_BUDGET_MILLISECONDS = 3000;

    public static RotationResult Compose(
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
            return new RotationResult(Array.Empty<PartyResultModel>(), Array.Empty<IReadOnlyList<MainTurnModel>>());
        }

        bool isLimited = preset.EntryLimit > 0 && preset.EntryLimit < runCount;
        int entryLimit = isLimited ? preset.EntryLimit : runCount;
        int mainLimit = Math.Clamp(mainRunCount, 0, entryLimit);
        Plan plan = new Plan(players, preset, data, runCount, mainLimit, entryLimit, isLimited, seed);
        plan.Climb();
        return new RotationResult(plan.BuildResults(), plan.GetMainTurns());
    }

    /// <summary>One number's characters that pass the cuts: index 0 is the main when it passes, the rest are alts.</summary>
    private sealed class Player
    {
        public List<CharacterData> Characters { get; }

        public bool HasMain { get; }

        public int FirstAlt => HasMain ? 1 : 0;

        public Player(List<CharacterData> characters)
        {
            Characters = characters.OrderByDescending(character => character.IsMain).ToList();
            HasMain = Characters[0].IsMain;
        }
    }

    /// <summary>The schedule: for each run and player, the character index they bring, or OUT.</summary>
    private sealed class Plan
    {
        private readonly List<Player> _players;
        private readonly PresetRecord _preset;
        private readonly GameDataTable _data;
        private readonly int _runCount;
        private readonly int _mainLimit;
        private readonly int _entryLimit;
        private readonly bool _isLimited;
        private readonly int _seatCount;
        private readonly int _seed;
        private readonly int[,] _picks;
        private readonly bool[,] _mains;
        private readonly Dictionary<string, double> _runScores = new Dictionary<string, double>();

        /// <summary>The players with a main, in the order their mains first take their turns.</summary>
        private readonly int[] _mainOrder;

        /// <summary>How many main turns fit one after another in the runs; more mains than that play side by side.</summary>
        private readonly int _turnsPerLane;

        public Plan(
            List<Player> players, PresetRecord preset, GameDataTable data, int runCount, int mainLimit, int entryLimit, bool isLimited, int seed)
        {
            _players = players;
            _preset = preset;
            _data = data;
            _runCount = runCount;
            _mainLimit = mainLimit;
            _entryLimit = entryLimit;
            _isLimited = isLimited;
            _seatCount = preset.PartyCount * preset.PartySize;
            _seed = seed;
            _picks = new int[runCount, players.Count];
            _mains = new bool[runCount, players.Count];
            _mainOrder = Enumerable.Range(0, players.Count).Where(player => players[player].HasMain).ToArray();
            _turnsPerLane = mainLimit == 0 ? 1 : Math.Max(1, runCount / mainLimit);
        }

        /// <summary>Three stages, each until nothing improves or the time is up: the order of the main turns as
        /// back-to-back blocks; then single main runs moved, or traded between two mains, which may split a turn
        /// at a cost; then the alts (swap a player's picks between two runs, bring a different alt, or trade a seat
        /// with a player sitting out).</summary>
        public void Climb()
        {
            Stopwatch watch = Stopwatch.StartNew();
            PlaceMainsInOrder();
            FillAlts();
            double current = Score();

            bool isImproved = true;
            while (isImproved && watch.ElapsedMilliseconds < TIME_BUDGET_MILLISECONDS)
            {
                isImproved = false;
                for (int first = 0; first < _mainOrder.Length; first++)
                {
                    for (int second = first + 1; second < _mainOrder.Length; second++)
                    {
                        SwapTurns(first, second);
                        PlaceMainsInOrder();
                        FillAlts();
                        if (TryKeep(ref current))
                        {
                            isImproved = true;
                            continue;
                        }

                        SwapTurns(first, second);
                    }
                }
            }

            PlaceMainsInOrder();
            FillAlts();
            current = Score();
            isImproved = true;
            while (isImproved && watch.ElapsedMilliseconds < TIME_BUDGET_MILLISECONDS)
            {
                isImproved = false;
                foreach (int player in _mainOrder)
                {
                    for (int from = 0; from < _runCount; from++)
                    {
                        for (int to = 0; to < _runCount; to++)
                        {
                            if (!_mains[from, player] || _mains[to, player])
                            {
                                continue;
                            }

                            isImproved |= TryMainMove(player, from, to, OUT, ref current);
                            foreach (int other in _mainOrder)
                            {
                                if (_mains[from, player] && other != player && _mains[to, other] && !_mains[from, other])
                                {
                                    isImproved |= TryMainMove(player, from, to, other, ref current);
                                }
                            }
                        }
                    }
                }
            }

            isImproved = true;
            while (isImproved && watch.ElapsedMilliseconds < TIME_BUDGET_MILLISECONDS)
            {
                isImproved = false;
                for (int player = 0; player < _players.Count; player++)
                {
                    isImproved |= ClimbAlts(player, ref current);
                }

                for (int run = 0; run < _runCount; run++)
                {
                    isImproved |= ClimbSeats(run, ref current);
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

        /// <summary>The mains by the run they first play, those starting in the same run on one line.</summary>
        public IReadOnlyList<IReadOnlyList<MainTurnModel>> GetMainTurns()
        {
            List<MainTurnModel> turns = new List<MainTurnModel>();
            foreach (int player in _mainOrder)
            {
                List<int> runs = Enumerable.Range(0, _runCount).Where(run => _mains[run, player]).Select(run => run + 1).ToList();
                if (runs.Count > 0)
                {
                    turns.Add(new MainTurnModel(_players[player].Characters[MAIN], runs, runs.Count == _mainLimit && CountSegments(player) == 1));
                }
            }

            return turns
                .GroupBy(turn => turn.Runs[0])
                .OrderBy(group => group.Key)
                .Select(group => (IReadOnlyList<MainTurnModel>)group.ToList())
                .ToList();
        }

        /// <summary>Each main turn is a block of runs back to back, the blocks laid out in turn order; more mains
        /// than fit one after another start again from the first run, side by side.</summary>
        private void PlaceMainsInOrder()
        {
            Array.Clear(_mains);
            for (int position = 0; position < _mainOrder.Length && _mainLimit > 0; position++)
            {
                int start = position % _turnsPerLane * _mainLimit;
                for (int run = start; run < Math.Min(start + _mainLimit, _runCount); run++)
                {
                    _mains[run, _mainOrder[position]] = true;
                }
            }
        }

        /// <summary>Moves one main run of a player elsewhere; with <paramref name="other"/>, that main takes the run
        /// given up in exchange, so both keep their count. Rebuilds the alts and keeps the move only if it scores better.</summary>
        private bool TryMainMove(int player, int from, int to, int other, ref double current)
        {
            bool[,] mains = (bool[,])_mains.Clone();
            int[,] picks = (int[,])_picks.Clone();
            _mains[from, player] = false;
            _mains[to, player] = true;
            if (other != OUT)
            {
                _mains[to, other] = false;
                _mains[from, other] = true;
            }

            FillAlts();
            if (TryKeep(ref current))
            {
                return true;
            }

            Array.Copy(mains, _mains, mains.Length);
            Array.Copy(picks, _picks, picks.Length);
            return false;
        }

        /// <summary>Mains where placed; the free seats go to the players who have played least, each bringing
        /// their least used alt that still has an entry left.</summary>
        private void FillAlts()
        {
            int[] plays = new int[_players.Count];
            int[,] uses = new int[_players.Count, _players.Max(player => player.Characters.Count)];
            for (int run = 0; run < _runCount; run++)
            {
                for (int player = 0; player < _players.Count; player++)
                {
                    _picks[run, player] = _mains[run, player] ? MAIN : OUT;
                    if (_mains[run, player])
                    {
                        plays[player]++;
                    }
                }
            }

            for (int run = 0; run < _runCount; run++)
            {
                int seated = CountSeated(run);
                List<int> order = Enumerable.Range(0, _players.Count)
                    .Where(player => _picks[run, player] == OUT)
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
                    int alt = OUT;
                    for (int index = owner.FirstAlt; index < owner.Characters.Count; index++)
                    {
                        if (uses[player, index] < _entryLimit && (alt == OUT || uses[player, index] < uses[player, alt]))
                        {
                            alt = index;
                        }
                    }

                    if (alt == OUT)
                    {
                        continue;
                    }

                    _picks[run, player] = alt;
                    uses[player, alt]++;
                    plays[player]++;
                    seated++;
                }
            }
        }

        /// <summary>A player's alt picks: swap two runs, or bring a different alt with an entry left.</summary>
        private bool ClimbAlts(int player, ref double current)
        {
            bool isImproved = false;
            for (int first = 0; first < _runCount; first++)
            {
                for (int second = first + 1; second < _runCount; second++)
                {
                    if (_picks[first, player] == _picks[second, player] || _mains[first, player] || _mains[second, player]
                        || !CanSwapRuns(player, first, second))
                    {
                        continue;
                    }

                    SwapRuns(player, first, second);
                    if (TryKeep(ref current))
                    {
                        isImproved = true;
                        continue;
                    }

                    SwapRuns(player, first, second);
                }
            }

            Player owner = _players[player];
            for (int run = 0; run < _runCount; run++)
            {
                if (_picks[run, player] == OUT || _mains[run, player])
                {
                    continue;
                }

                for (int alt = owner.FirstAlt; alt < owner.Characters.Count; alt++)
                {
                    if (alt == _picks[run, player] || CountUses(player, alt) >= _entryLimit)
                    {
                        continue;
                    }

                    int kept = _picks[run, player];
                    _picks[run, player] = alt;
                    if (TryKeep(ref current))
                    {
                        isImproved = true;
                        continue;
                    }

                    _picks[run, player] = kept;
                }
            }

            return isImproved;
        }

        /// <summary>A seated alt makes room for a player sitting out who still has an alt with an entry left.</summary>
        private bool ClimbSeats(int run, ref double current)
        {
            bool isImproved = false;
            for (int seated = 0; seated < _players.Count; seated++)
            {
                int pick = _picks[run, seated];
                if (pick == OUT || _mains[run, seated])
                {
                    continue;
                }

                for (int waiting = 0; waiting < _players.Count; waiting++)
                {
                    int alt = _picks[run, waiting] == OUT ? FindFreeAlt(waiting) : OUT;
                    if (alt == OUT)
                    {
                        continue;
                    }

                    _picks[run, seated] = OUT;
                    _picks[run, waiting] = alt;
                    if (TryKeep(ref current))
                    {
                        isImproved = true;
                        break;
                    }

                    _picks[run, waiting] = OUT;
                    _picks[run, seated] = pick;
                }
            }

            return isImproved;
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

        /// <summary>Each run's best composition, less the penalties for empty seats before the last runs, main
        /// turns split into pieces, uneven play counts, and, when characters may repeat, uneven mains per run.</summary>
        private double Score()
        {
            double score = 0;
            int[] mains = new int[_runCount];
            int[] plays = new int[_players.Count];
            for (int run = 0; run < _runCount; run++)
            {
                score += GetRunScore(run);
                score -= (_seatCount - CountSeated(run)) * (_runCount - 1 - run) * EARLY_EMPTY_SEAT_PENALTY;
                for (int player = 0; player < _players.Count; player++)
                {
                    if (_picks[run, player] == OUT)
                    {
                        continue;
                    }

                    plays[player]++;
                    if (_mains[run, player])
                    {
                        mains[run]++;
                    }
                }
            }

            foreach (int player in _mainOrder)
            {
                score -= Math.Max(0, CountSegments(player) - 1) * SPLIT_TURN_PENALTY;
            }

            double playAverage = plays.Average();
            score -= plays.Sum(count => Math.Abs(count - playAverage)) * PLAY_SPREAD_PENALTY;
            if (!_isLimited)
            {
                double mainAverage = mains.Average();
                score -= mains.Sum(count => Math.Abs(count - mainAverage)) * MAIN_SPREAD_PENALTY;
            }

            return score;
        }

        /// <summary>Cached by who plays, since most moves change only one or two runs.</summary>
        private double GetRunScore(int run)
        {
            string key = string.Join(",", Enumerable.Range(0, _players.Count).Select(player => _picks[run, player]));
            double score;
            if (_runScores.TryGetValue(key, out score))
            {
                return score;
            }

            IReadOnlyList<PartyResultModel> composed = PartyService.Compose(GetRunCharacters(run), _preset, _data, 1, _seed + run);
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

        /// <summary>How many separate stretches of back-to-back runs a player's main plays.</summary>
        private int CountSegments(int player)
        {
            int segments = 0;
            for (int run = 0; run < _runCount; run++)
            {
                if (_mains[run, player] && (run == 0 || !_mains[run - 1, player]))
                {
                    segments++;
                }
            }

            return segments;
        }

        private int CountUses(int player, int index)
        {
            int uses = 0;
            for (int run = 0; run < _runCount; run++)
            {
                if (_picks[run, player] == index)
                {
                    uses++;
                }
            }

            return uses;
        }

        private int FindFreeAlt(int player)
        {
            Player owner = _players[player];
            for (int index = owner.FirstAlt; index < owner.Characters.Count; index++)
            {
                if (CountUses(player, index) < _entryLimit)
                {
                    return index;
                }
            }

            return OUT;
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

        private void SwapTurns(int first, int second)
        {
            (_mainOrder[first], _mainOrder[second]) = (_mainOrder[second], _mainOrder[first]);
        }

        private void SwapRuns(int player, int first, int second)
        {
            (_picks[first, player], _picks[second, player]) = (_picks[second, player], _picks[first, player]);
        }
    }
}
