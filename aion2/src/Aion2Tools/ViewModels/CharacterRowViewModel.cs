using Aion2Tools.Models;
using Aion2Tools.Services;

namespace Aion2Tools.ViewModels;

/// <summary>One character row inside a roster group. Combat power and item level are edited as text so "-" can mean not entered.
/// The setters do not echo back: a box cleared mid-edit must stay empty for the next digit, not turn into "-".</summary>
public class CharacterRowViewModel : ViewModelBase
{
    private const string NOT_ENTERED = "-";

    public CharacterData Character { get; }

    public string KindText => Character.IsMain ? "본캐" : "부캐";

    public string CombatPowerText
    {
        get => FormatOptional(Character.CombatPower);
        set
        {
            int? parsed;
            if (RosterService.TryParseOptional(value, out parsed))
            {
                Character.CombatPower = parsed;
            }
        }
    }

    public string ItemLevelText
    {
        get => FormatOptional(Character.ItemLevel);
        set
        {
            int? parsed;
            if (RosterService.TryParseOptional(value, out parsed))
            {
                Character.ItemLevel = parsed;
            }
        }
    }

    public CharacterRowViewModel(CharacterData character)
    {
        Character = character;
    }

    private static string FormatOptional(int? value)
    {
        if (!value.HasValue)
        {
            return NOT_ENTERED;
        }

        return value.Value.ToString();
    }
}
