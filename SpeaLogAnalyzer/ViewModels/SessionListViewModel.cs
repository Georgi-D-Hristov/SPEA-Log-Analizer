using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.ViewModels;

public partial class SessionListViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboardVm;
    private List<TestSession> _allSessions = [];

    [ObservableProperty]
    private List<TestSession> _filteredSessions = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedResultFilter = "All";

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _statusText = "Load data from Dashboard first.";

    public List<string> ResultFilterOptions { get; } = ["All", "PASS", "FAIL"];

    public SessionListViewModel(DashboardViewModel dashboardVm)
    {
        _dashboardVm = dashboardVm;
    }

    [RelayCommand]
    private void Refresh()
    {
        _allSessions = _dashboardVm.AllSessions;
        HasData = _allSessions.Count > 0;
        StatusText = HasData
            ? $"{_allSessions.Count} sessions loaded"
            : "Load data from Dashboard first.";
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedResultFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allSessions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToUpperInvariant();
            query = query.Where(s =>
                s.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.FixtureId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Operator.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.BoardResults.Any(b => b.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (SelectedResultFilter == "PASS")
            query = query.Where(s => s.OverallResult == TestResult.Pass);
        else if (SelectedResultFilter == "FAIL")
            query = query.Where(s => s.OverallResult == TestResult.Fail);

        FilteredSessions = query.OrderByDescending(s => s.StartTime).ToList();
    }

    [RelayCommand]
    private async Task NavigateToDetail(TestSession? session)
    {
        if (session is null) return;

        var navParams = new Dictionary<string, object>
        {
            { "Session", session }
        };

        await Shell.Current.GoToAsync(nameof(Views.SessionDetailPage), navParams);
    }
}
