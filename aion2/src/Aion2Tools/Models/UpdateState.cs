namespace Aion2Tools.Models;

/// <summary>Where the app's own update stands this session. Never saved.</summary>
public enum UpdateState
{
    None = 0,
    Available = 1,
    Building = 2,
    Ready = 3,
    UpToDate = 4,
    Failed = 5,
}
