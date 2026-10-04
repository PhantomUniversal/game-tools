using System;
using Aion2Tools.Models;
using Avalonia.Media;

namespace Aion2Tools.ViewModels;

/// <summary>One line of a party card.</summary>
public class MemberRowViewModel : ViewModelBase
{
    private static readonly IBrush TANK_BRUSH = Brush.Parse("#63B3ED");
    private static readonly IBrush HEALER_BRUSH = Brush.Parse("#68D391");
    private static readonly IBrush SUPPORT_BRUSH = Brush.Parse("#F6E05E");
    private static readonly IBrush DEALER_BRUSH = Brush.Parse("#FC8181");

    public string RoleText { get; }

    public IBrush RoleBrush { get; }

    public string Name { get; }

    public string Detail { get; }

    public string CombatPowerText { get; }

    public MemberRowViewModel(CharacterData character, ClassRecord record, bool isBuffed)
    {
        RoleText = GetRoleText(record.Role);
        RoleBrush = GetRoleBrush(record.Role);
        Name = (character.IsMain ? "★ " : string.Empty) + character.Name + (isBuffed ? " ⚡" : string.Empty);
        Detail = $"{record.Name} · {character.Number}번";
        CombatPowerText = $"{character.CombatPower / 1000.0:0.0}k";
    }

    public static string GetRoleText(RoleKind role)
    {
        switch (role)
        {
            case RoleKind.Tank:
                return "탱";
            case RoleKind.Healer:
                return "힐";
            case RoleKind.Support:
                return "서폿";
            case RoleKind.Dealer:
                return "딜";
            case RoleKind.None:
                return "-";
            default:
                throw new NotImplementedException($"unhandled switch case: {role}");
        }
    }

    private static IBrush GetRoleBrush(RoleKind role)
    {
        switch (role)
        {
            case RoleKind.Tank:
                return TANK_BRUSH;
            case RoleKind.Healer:
                return HEALER_BRUSH;
            case RoleKind.Support:
                return SUPPORT_BRUSH;
            case RoleKind.Dealer:
                return DEALER_BRUSH;
            case RoleKind.None:
                return Brushes.Gray;
            default:
                throw new NotImplementedException($"unhandled switch case: {role}");
        }
    }
}
