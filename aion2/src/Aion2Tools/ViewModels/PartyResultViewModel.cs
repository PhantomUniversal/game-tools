using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aion2Tools.Models;

namespace Aion2Tools.ViewModels;

/// <summary>One composition as shown: party cards, the rule checks, and who sat out.</summary>
public class PartyResultViewModel : ViewModelBase
{
    private readonly PartyResultModel _result;
    private readonly GameDataTable _data;

    public string Title { get; }

    public IReadOnlyList<PartyCardViewModel> Parties { get; }

    public IReadOnlyList<string> Checks { get; }

    public IReadOnlyList<string> Bench { get; }

    public bool HasBench => Bench.Count > 0;

    public PartyResultViewModel(string title, PartyResultModel result, GameDataTable data)
    {
        _result = result;
        _data = data;
        Title = title;
        Parties = result.Parties.Select(party => new PartyCardViewModel(party, data)).ToList();
        Checks = result.Checks;
        Bench = result.Bench.Select(bench => $"{bench.Character.Name} ({GetClassName(bench.Character)}) — {bench.Reason}").ToList();
    }

    /// <summary>Plain text to paste into a chat.</summary>
    public string ToText()
    {
        StringBuilder text = new StringBuilder();
        foreach (PartyModel party in _result.Parties)
        {
            text.AppendLine($"[{party.Number}파티]");
            foreach (CharacterData member in party.Members)
            {
                ClassRecord record = _data.GetClassOrNull(member.ClassId)!;
                text.AppendLine($"- {MemberRowViewModel.GetRoleText(record.Role)} {member.Name} ({record.Name}) {member.CombatPower:N0}");
            }
        }

        if (_result.Bench.Count > 0)
        {
            text.AppendLine("[대기]");
            foreach (BenchModel bench in _result.Bench)
            {
                text.AppendLine($"- {bench.Character.Name} ({GetClassName(bench.Character)}) {bench.Reason}");
            }
        }

        return text.ToString();
    }

    private string GetClassName(CharacterData character)
    {
        ClassRecord? recordOrNull = _data.GetClassOrNull(character.ClassId);
        if (recordOrNull is null)
        {
            return "?";
        }

        return recordOrNull.Name;
    }
}
