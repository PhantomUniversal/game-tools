using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One run or alternative as a row of member chips; selecting it opens its details in the side panel.</summary>
public partial class ResultRowViewModel : ViewModelBase
{
    private const string WARNING_MARK = "⚠";

    public string Title { get; }

    public PartyResultViewModel Result { get; }

    public IReadOnlyList<PartyCardViewModel> Parties => Result.Parties;

    public string CheckText { get; }

    public bool HasWarning { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Empty seats show first: in a short rotation they say which run needs more mains.</summary>
    public ResultRowViewModel(PartyResultViewModel result, int seatCount)
    {
        Title = result.Title;
        Result = result;
        int warnings = result.Checks.Count(check => check.StartsWith(WARNING_MARK));
        int emptySeats = seatCount - result.Parties.Sum(party => party.Members.Count);
        HasWarning = warnings > 0 || emptySeats > 0;
        if (emptySeats > 0)
        {
            CheckText = $"빈자리 {emptySeats}";
        }
        else
        {
            CheckText = warnings > 0 ? $"⚠ {warnings}" : "✓ 이상 없음";
        }
    }
}
