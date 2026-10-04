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

    public ResultRowViewModel(PartyResultViewModel result)
    {
        Title = result.Title;
        Result = result;
        int warnings = result.Checks.Count(check => check.StartsWith(WARNING_MARK));
        HasWarning = warnings > 0;
        CheckText = HasWarning ? $"⚠ {warnings}" : "✓ 이상 없음";
    }
}
