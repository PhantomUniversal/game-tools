using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>Classes, presets and weights, loaded as one unit so a patch can change them without a rebuild.</summary>
public class GameDataTable
{
    public int Version { get; set; }

    public List<ClassRecord> Classes { get; set; } = new List<ClassRecord>();

    public List<PresetRecord> Presets { get; set; } = new List<PresetRecord>();

    public WeightsRecord Weights { get; set; } = new WeightsRecord();

    public GameDataTable()
    {
    }

    public ClassRecord? GetClassOrNull(string id)
    {
        foreach (ClassRecord record in Classes)
        {
            if (record.Id == id)
            {
                return record;
            }
        }

        return null;
    }
}
