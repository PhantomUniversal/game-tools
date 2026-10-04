using System;
using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>Turns pasted text into roster characters.</summary>
public static class RosterService
{
    /// <summary>One character per line: number (1-10), name, class, combat power, item level, main (Y/N).
    /// Combat power and item level may be "-" or empty for not entered.
    /// Commas or tabs, so rows pasted from a spreadsheet work. Lines that do not parse are counted, not thrown.</summary>
    public static List<CharacterData> Parse(string text, GameDataTable data, out int skipped)
    {
        List<CharacterData> characters = new List<CharacterData>();
        skipped = 0;
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] cells = line.Split(new[] { ',', '\t' });
            CharacterData? characterOrNull = ParseCellsOrNull(cells, data);
            if (characterOrNull is null)
            {
                skipped++;
                continue;
            }

            characters.Add(characterOrNull);
        }

        return characters;
    }

    /// <summary>The class column takes the game data id or the Korean name.</summary>
    private static CharacterData? ParseCellsOrNull(string[] cells, GameDataTable data)
    {
        if (cells.Length < 5)
        {
            return null;
        }

        string classId = string.Empty;
        foreach (ClassRecord record in data.Classes)
        {
            if (record.Id == cells[2].Trim() || record.Name == cells[2].Trim())
            {
                classId = record.Id;
            }
        }

        int number;
        int? combatPower;
        int? itemLevel;
        if (classId.Length == 0
            || !int.TryParse(cells[0].Trim(), out number)
            || number < CharacterData.MIN_NUMBER
            || number > CharacterData.MAX_NUMBER
            || !TryParseOptional(cells[3], out combatPower)
            || !TryParseOptional(cells[4], out itemLevel))
        {
            return null;
        }

        CharacterData character = new CharacterData();
        character.Number = number;
        character.Name = cells[1].Trim();
        character.ClassId = classId;
        character.CombatPower = combatPower;
        character.ItemLevel = itemLevel;
        character.IsMain = cells.Length > 5 && IsYes(cells[5]);
        return character;
    }

    /// <summary>Parses the text a roster number box holds: "-" or blank is not entered (null), commas are allowed.</summary>
    public static bool TryParseOptional(string text, out int? value)
    {
        string trimmed = text.Trim().Replace(",", string.Empty);
        value = null;
        if (trimmed.Length == 0 || trimmed == "-")
        {
            return true;
        }

        int parsed;
        if (!int.TryParse(trimmed, out parsed) || parsed < 0)
        {
            return false;
        }

        value = parsed;
        return true;
    }

    /// <summary>Gives every number exactly one main: the first marked one, or else its first character.</summary>
    public static void NormalizeMains(IEnumerable<CharacterData> characters)
    {
        foreach (IGrouping<int, CharacterData> group in characters.GroupBy(character => character.Number))
        {
            CharacterData? markedOrNull = group.FirstOrDefault(character => character.IsMain);
            CharacterData main = markedOrNull is null ? group.First() : markedOrNull;
            foreach (CharacterData character in group)
            {
                character.IsMain = character == main;
            }
        }
    }

    private static bool IsYes(string cell)
    {
        string value = cell.Trim();
        return value.Equals("Y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "본캐"
            || value == "1";
    }
}
