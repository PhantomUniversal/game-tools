namespace Aion2Tools.Models;

/// <summary>What one self-update attempt came to.</summary>
public enum UpdateResult
{
    None = 0,
    Built = 1,
    UpToDate = 2,
    Failed = 3,
}
