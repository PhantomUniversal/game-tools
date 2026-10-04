using System;
using System.Collections.Generic;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>Turns pasted text into roster characters, and supplies a sample roster to try the tool with.</summary>
public static class RosterService
{
    /// <summary>One character per line: player, name, class, combat power, item level, main (Y/N).
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

    public static List<CharacterData> CreateSample()
    {
        List<CharacterData> characters = new List<CharacterData>();
        AddSample(characters, "A", "철벽", "guardian", 36210, 3820, true);
        AddSample(characters, "A", "보조", "cleric", 28400, 3100, false);
        AddSample(characters, "A", "셋째", "ranger", 25300, 2900, false);
        AddSample(characters, "B", "그림자", "assassin", 38050, 4010, true);
        AddSample(characters, "B", "둘째", "chanter", 27100, 3050, false);
        AddSample(characters, "C", "새벽", "cleric", 32400, 3500, true);
        AddSample(characters, "C", "칼날", "gladiator", 29800, 3200, false);
        AddSample(characters, "C", "마나", "sorcerer", 26600, 2950, false);
        AddSample(characters, "D", "불꽃", "sorcerer", 35500, 3700, true);
        AddSample(characters, "E", "칼바람", "gladiator", 34800, 3650, true);
        AddSample(characters, "E", "방패", "guardian", 27900, 3000, false);
        AddSample(characters, "F", "주먹왕", "fighter", 33600, 3550, true);
        AddSample(characters, "F", "바람", "spiritmaster", 26000, 2800, false);
        AddSample(characters, "G", "바람결", "spiritmaster", 31900, 3400, true);
        AddSample(characters, "G", "은빛", "cleric", 27500, 2950, false);
        AddSample(characters, "H", "은하", "cleric", 30700, 3300, true);
        AddSample(characters, "H", "번개", "assassin", 28200, 3000, false);
        AddSample(characters, "I", "수호천사", "chanter", 31200, 3350, true);
        AddSample(characters, "I", "주문", "chanter", 26900, 2900, false);
        AddSample(characters, "J", "축복", "chanter", 30500, 3300, true);
        AddSample(characters, "K", "별빛", "ranger", 33000, 3500, true);
        AddSample(characters, "L", "초보", "ranger", 24000, 2500, true);
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

        int combatPower;
        int itemLevel;
        if (classId.Length == 0
            || !int.TryParse(cells[3].Trim().Replace(",", string.Empty), out combatPower)
            || !int.TryParse(cells[4].Trim(), out itemLevel))
        {
            return null;
        }

        CharacterData character = new CharacterData();
        character.Player = cells[0].Trim();
        character.Name = cells[1].Trim();
        character.ClassId = classId;
        character.CombatPower = combatPower;
        character.ItemLevel = itemLevel;
        character.IsMain = cells.Length > 5 && IsYes(cells[5]);
        return character;
    }

    private static bool IsYes(string cell)
    {
        string value = cell.Trim();
        return value.Equals("Y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "본캐"
            || value == "1";
    }

    private static void AddSample(List<CharacterData> characters, string player, string name, string classId, int combatPower, int itemLevel, bool isMain)
    {
        CharacterData character = new CharacterData();
        character.Player = player;
        character.Name = name;
        character.ClassId = classId;
        character.CombatPower = combatPower;
        character.ItemLevel = itemLevel;
        character.IsMain = isMain;
        characters.Add(character);
    }
}
