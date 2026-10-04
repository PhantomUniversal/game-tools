using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.ViewModels;

/// <summary>One party drawn as a card.</summary>
public class PartyCardViewModel : ViewModelBase
{
    public string Title { get; }

    public IReadOnlyList<MemberRowViewModel> Members { get; }

    public string Summary { get; }

    public PartyCardViewModel(PartyModel party, GameDataTable data)
    {
        Title = $"{party.Number}파티";
        bool hasSupport = party.Members.Any(member => data.GetClassOrNull(member.ClassId)!.Role == RoleKind.Support);
        Members = party.Members
            .Select(member => CreateRow(member, data, hasSupport))
            .ToList();
        Summary = $"{party.Members.Count}명 · 합계 {party.TotalCombatPower / 1000.0:0.0}k";
    }

    private static MemberRowViewModel CreateRow(CharacterData member, GameDataTable data, bool hasSupport)
    {
        ClassRecord record = data.GetClassOrNull(member.ClassId)!;
        bool isBuffed = hasSupport && record.Role == RoleKind.Dealer && record.BuffPriority > 0;
        return new MemberRowViewModel(member, record, isBuffed);
    }
}
