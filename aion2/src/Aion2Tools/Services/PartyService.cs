using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>Splits characters into parties: random starts, then swaps and moves while the score rises.
/// The score is the weights of the game data added up, so rules change with the data, not the code.</summary>
public static class PartyService
{
    private const int BENCH = -1;
    private const int RESTART_COUNT = 40;
    private const double EPSILON = 1e-9;

    /// <summary>A character on the bench while a seat is free; below a duplicate player, so the same number still sits out.</summary>
    private const double BENCH_WITH_FREE_SEAT_PENALTY = 3000;


    // ─────────────────────────────────────────────────────────────────────────
    // << COMPOSE >>
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>The best distinct compositions, best first. Empty when nobody passes the cuts.</summary>
    public static IReadOnlyList<PartyResultModel> Compose(
        IReadOnlyList<CharacterData> characters, PresetRecord preset, GameDataTable data, int alternativeCount, int seed)
    {
        List<CharacterData> eligible = new List<CharacterData>();
        List<BenchModel> cut = new List<BenchModel>();
        foreach (CharacterData character in characters)
        {
            string reason = GetCutReason(character, preset, data);
            if (reason.Length > 0)
            {
                cut.Add(new BenchModel(character, reason));
                continue;
            }

            eligible.Add(character);
        }

        if (eligible.Count == 0)
        {
            return Array.Empty<PartyResultModel>();
        }

        double estimate = GetEstimatedCombatPower(eligible);
        Scorer scorer = new Scorer(eligible, preset, data, estimate);
        Random random = new Random(seed);
        Dictionary<string, int[]> found = new Dictionary<string, int[]>();
        for (int restart = 0; restart < RESTART_COUNT; restart++)
        {
            int[] groups = CreateStart(eligible.Count, preset, random);
            Climb(groups, scorer, preset);
            found.TryAdd(GetSignature(groups, preset), groups);
        }

        return found.Values
            .OrderByDescending(groups => scorer.Score(groups))
            .Take(alternativeCount)
            .Select(groups => BuildResult(groups, eligible, cut, scorer, preset, data, estimate))
            .ToList();
    }

    /// <summary>Why a character cannot join this content, or empty when it can.</summary>
    public static string GetCutReason(CharacterData character, PresetRecord preset, GameDataTable data)
    {
        if (data.GetClassOrNull(character.ClassId) is null)
        {
            return "클래스 미지정";
        }

        if (character.ItemLevel.HasValue && character.ItemLevel.Value < preset.MinItemLevel)
        {
            return $"아이템 레벨 미달 ({character.ItemLevel.Value:N0} < {preset.MinItemLevel:N0})";
        }

        if (character.CombatPower.HasValue && character.CombatPower.Value < preset.MinCombatPower)
        {
            return $"전투력 미달 ({character.CombatPower.Value:N0} < {preset.MinCombatPower:N0})";
        }

        return string.Empty;
    }

    /// <summary>What a character with no combat power entered counts as: the average of those entered, 0 if none.</summary>
    private static double GetEstimatedCombatPower(List<CharacterData> characters)
    {
        List<int> known = characters.Where(character => character.CombatPower.HasValue).Select(character => character.CombatPower!.Value).ToList();
        if (known.Count == 0)
        {
            return 0;
        }

        return known.Average();
    }

    private static double GetCombatPower(CharacterData character, double estimate)
    {
        return character.CombatPower.HasValue ? character.CombatPower.Value : estimate;
    }

    /// <summary>A shuffled order poured into the parties one seat at a time; whoever is left sits on the bench.</summary>
    private static int[] CreateStart(int count, PresetRecord preset, Random random)
    {
        int[] order = Enumerable.Range(0, count).ToArray();
        random.Shuffle(order);
        int[] groups = new int[count];
        int capacity = preset.PartyCount * preset.PartySize;
        for (int seat = 0; seat < count; seat++)
        {
            groups[order[seat]] = seat < capacity ? seat % preset.PartyCount : BENCH;
        }

        return groups;
    }


    // ─────────────────────────────────────────────────────────────────────────
    // << SEARCH >>
    // * Hill climbing over two moves: swap two characters, or move one into a free seat.
    // ─────────────────────────────────────────────────────────────────────────
    private static void Climb(int[] groups, Scorer scorer, PresetRecord preset)
    {
        double current = scorer.Score(groups);
        bool isImproved = true;
        while (isImproved)
        {
            isImproved = false;
            for (int first = 0; first < groups.Length; first++)
            {
                for (int second = first + 1; second < groups.Length; second++)
                {
                    if (groups[first] == groups[second])
                    {
                        continue;
                    }

                    Swap(groups, first, second);
                    double score = scorer.Score(groups);
                    if (score > current + EPSILON)
                    {
                        current = score;
                        isImproved = true;
                    }
                    else
                    {
                        Swap(groups, first, second);
                    }
                }
            }

            for (int index = 0; index < groups.Length; index++)
            {
                for (int target = BENCH; target < preset.PartyCount; target++)
                {
                    if (groups[index] == target || !HasFreeSeat(groups, target, preset))
                    {
                        continue;
                    }

                    int from = groups[index];
                    groups[index] = target;
                    double score = scorer.Score(groups);
                    if (score > current + EPSILON)
                    {
                        current = score;
                        isImproved = true;
                    }
                    else
                    {
                        groups[index] = from;
                    }
                }
            }
        }
    }

    private static void Swap(int[] groups, int first, int second)
    {
        (groups[first], groups[second]) = (groups[second], groups[first]);
    }

    private static bool HasFreeSeat(int[] groups, int target, PresetRecord preset)
    {
        if (target == BENCH)
        {
            return true;
        }

        int seated = 0;
        foreach (int group in groups)
        {
            if (group == target)
            {
                seated++;
            }
        }

        return seated < preset.PartySize;
    }

    /// <summary>Who sits in which party, so two starts that land on the same parties count once.</summary>
    private static string GetSignature(int[] groups, PresetRecord preset)
    {
        StringBuilder signature = new StringBuilder();
        for (int party = 0; party < preset.PartyCount; party++)
        {
            for (int index = 0; index < groups.Length; index++)
            {
                if (groups[index] == party)
                {
                    signature.Append(index).Append(',');
                }
            }

            signature.Append('|');
        }

        return signature.ToString();
    }


    // ─────────────────────────────────────────────────────────────────────────
    // << RESULT >>
    // ─────────────────────────────────────────────────────────────────────────
    private static PartyResultModel BuildResult(
        int[] groups, List<CharacterData> eligible, List<BenchModel> cut, Scorer scorer, PresetRecord preset, GameDataTable data, double estimate)
    {
        List<PartyModel> parties = new List<PartyModel>();
        for (int party = 0; party < preset.PartyCount; party++)
        {
            List<CharacterData> members = new List<CharacterData>();
            for (int index = 0; index < groups.Length; index++)
            {
                if (groups[index] == party)
                {
                    members.Add(eligible[index]);
                }
            }

            List<CharacterData> ordered = members
                .OrderBy(member => GetSeatOrder(data.GetClassOrNull(member.ClassId)))
                .ThenByDescending(member => GetCombatPower(member, estimate))
                .ToList();
            parties.Add(new PartyModel(party + 1, ordered));
        }

        List<BenchModel> bench = new List<BenchModel>();
        for (int index = 0; index < groups.Length; index++)
        {
            if (groups[index] == BENCH)
            {
                bench.Add(new BenchModel(eligible[index], "정원 초과"));
            }
        }

        bench.AddRange(cut);
        return new PartyResultModel(parties, bench, scorer.Score(groups), BuildChecks(parties, preset, data, estimate));
    }

    /// <summary>Tank first, healer last, as a party list is read out.</summary>
    private static int GetSeatOrder(ClassRecord? recordOrNull)
    {
        if (recordOrNull is null)
        {
            return 9;
        }

        switch (recordOrNull.Role)
        {
            case RoleKind.Tank:
                return 0;
            case RoleKind.Dealer:
                return 1;
            case RoleKind.Support:
                return 2;
            case RoleKind.Healer:
                return 3;
            case RoleKind.None:
                return 9;
            default:
                throw new NotImplementedException($"unhandled switch case: {recordOrNull.Role}");
        }
    }

    private static List<string> BuildChecks(List<PartyModel> parties, PresetRecord preset, GameDataTable data, double estimate)
    {
        List<string> checks = new List<string>();
        Dictionary<int, int> players = new Dictionary<int, int>();
        Dictionary<string, int> debuffers = new Dictionary<string, int>();
        int buffDealers = 0;
        int buffedDealers = 0;
        bool hasSupport = false;
        List<string> misplaced = new List<string>();
        List<string> missingRoles = new List<string>();

        foreach (PartyModel party in parties)
        {
            int tanks = 0;
            int healers = 0;
            int supports = 0;
            int partyBuffDealers = 0;
            foreach (CharacterData member in party.Members)
            {
                ClassRecord record = data.GetClassOrNull(member.ClassId)!;
                switch (record.Role)
                {
                    case RoleKind.Tank:
                        tanks++;
                        break;
                    case RoleKind.Healer:
                        healers++;
                        break;
                    case RoleKind.Support:
                        supports++;
                        break;
                    case RoleKind.Dealer:
                        if (record.BuffPriority > 0)
                        {
                            partyBuffDealers++;
                        }

                        break;
                    case RoleKind.None:
                        break;
                    default:
                        throw new NotImplementedException($"unhandled switch case: {record.Role}");
                }

                if (record.IsRaidDebuff)
                {
                    debuffers[record.Name] = debuffers.GetValueOrDefault(record.Name) + 1;
                }

                if (record.PreferredParty > 0 && record.PreferredParty <= preset.PartyCount && record.PreferredParty != party.Number)
                {
                    misplaced.Add($"{member.Name}({record.Name})은 {record.PreferredParty}파티 권장");
                }

                players[member.Number] = players.GetValueOrDefault(member.Number) + 1;
            }

            buffDealers += partyBuffDealers;
            if (supports > 0)
            {
                hasSupport = true;
                buffedDealers += partyBuffDealers;
            }

            if (tanks == 0)
            {
                missingRoles.Add($"⚠ {party.Number}파티 탱커 없음");
            }

            if (healers == 0)
            {
                missingRoles.Add($"⚠ {party.Number}파티 힐러 없음");
            }
        }

        if (missingRoles.Count > 0)
        {
            checks.AddRange(missingRoles);
        }
        else
        {
            checks.Add("✅ 탱커 · 힐러 모두 있음");
        }

        List<string> twice = players.Where(pair => pair.Value > 1).Select(pair => $"{pair.Key}번").ToList();
        checks.Add(twice.Count > 0 ? "⚠ 같은 번호 중복: " + string.Join(", ", twice) : "✅ 같은 번호 중복 없음");

        if (hasSupport)
        {
            checks.Add($"⚡ 호법 파티에 들어간 호법 우선 딜러 {buffedDealers}/{buffDealers}");
        }

        List<string> duplicates = debuffers.Where(pair => pair.Value > 1).Select(pair => $"{pair.Key} {pair.Value}명").ToList();
        if (duplicates.Count > 0)
        {
            checks.Add("⚠ 공대 디버프 중복: " + string.Join(", ", duplicates));
        }
        else if (debuffers.Count > 0)
        {
            checks.Add("✅ 공대 디버프 중복 없음");
        }

        foreach (string line in misplaced)
        {
            checks.Add("⚠ " + line);
        }

        if (parties.Count > 1 && parties.All(party => party.Members.Count > 0))
        {
            List<double> totals = parties.Select(party => party.Members.Sum(member => GetCombatPower(member, estimate))).ToList();
            checks.Add($"전투력 차이 {GetSpreadPercent(totals):0.0}%");
        }

        int unknown = parties.Sum(party => party.UnknownCount);
        if (unknown > 0)
        {
            checks.Add($"전투력 미입력 {unknown}명은 평균 {estimate / 1000:0.0}k로 계산");
        }

        return checks;
    }

    /// <summary>(max - min) / average, in percent. 0 when there is nothing to compare.</summary>
    public static double GetSpreadPercent(IReadOnlyList<double> totals)
    {
        double average = totals.Average();
        if (average <= 0)
        {
            return 0;
        }

        return (totals.Max() - totals.Min()) / average * 100;
    }


    // ─────────────────────────────────────────────────────────────────────────
    // << SCORER >>
    // * Precomputes each character's class and player once; Score runs thousands of times per compose.
    // ─────────────────────────────────────────────────────────────────────────
    private sealed class Scorer
    {
        private readonly ClassRecord[] _classes;
        private readonly int[] _players;
        private readonly double[] _combatPowers;
        private readonly PresetRecord _preset;
        private readonly WeightsRecord _weights;

        public Scorer(List<CharacterData> eligible, PresetRecord preset, GameDataTable data, double estimate)
        {
            _classes = eligible.Select(character => data.GetClassOrNull(character.ClassId)!).ToArray();
            _players = eligible.Select(character => character.Number).ToArray();
            _combatPowers = eligible.Select(character => GetCombatPower(character, estimate)).ToArray();
            _preset = preset;
            _weights = data.Weights;
        }

        public double Score(int[] groups)
        {
            double score = 0;
            double[] totals = new double[_preset.PartyCount];
            Dictionary<int, int> players = new Dictionary<int, int>();
            Dictionary<string, int> debuffers = new Dictionary<string, int>();

            for (int party = 0; party < _preset.PartyCount; party++)
            {
                int tanks = 0;
                int healers = 0;
                int supports = 0;
                int buffSum = 0;
                for (int index = 0; index < groups.Length; index++)
                {
                    if (groups[index] != party)
                    {
                        continue;
                    }

                    ClassRecord record = _classes[index];
                    totals[party] += _combatPowers[index];
                    players[_players[index]] = players.GetValueOrDefault(_players[index]) + 1;
                    if (record.IsRaidDebuff)
                    {
                        debuffers[record.Id] = debuffers.GetValueOrDefault(record.Id) + 1;
                    }

                    if (_preset.PartyCount > 1 && record.PreferredParty == party + 1)
                    {
                        score += _weights.PreferredParty;
                    }

                    switch (record.Role)
                    {
                        case RoleKind.Tank:
                            tanks++;
                            break;
                        case RoleKind.Healer:
                            healers++;
                            break;
                        case RoleKind.Support:
                            supports++;
                            break;
                        case RoleKind.Dealer:
                            buffSum += record.BuffPriority;
                            break;
                        case RoleKind.None:
                            break;
                        default:
                            throw new NotImplementedException($"unhandled switch case: {record.Role}");
                    }
                }

                score -= tanks == 0 ? _weights.MissingTank : (tanks - 1) * _weights.ExtraSameRole;
                score -= healers == 0 ? _weights.MissingHealer : (healers - 1) * _weights.ExtraSameRole;
                if (supports > 0)
                {
                    score += _weights.SupportInParty + buffSum * _weights.BuffPriority;
                    score -= (supports - 1) * _weights.ExtraSameRole;
                }
            }

            foreach (int count in players.Values)
            {
                score -= (count - 1) * _weights.DuplicatePlayer;
            }

            foreach (int count in debuffers.Values)
            {
                score -= (count - 1) * _weights.RaidDebuffDuplicate;
            }

            int benched = groups.Count(group => group == BENCH);
            int freeSeats = _preset.PartyCount * _preset.PartySize - (groups.Length - benched);
            score -= Math.Min(benched, freeSeats) * BENCH_WITH_FREE_SEAT_PENALTY;

            score += totals.Sum() / 1000 * _weights.CombatPowerPer1000;
            if (_preset.PartyCount > 1)
            {
                score -= GetSpreadPercent(totals) * _weights.BalancePercent;
            }

            return score;
        }
    }
}
