using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.ViewModels;

public partial class SessionListViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboardVm;
    private List<SessionBoardEntry> _allEntries = [];

    [ObservableProperty]
    private List<SessionBoardEntry> _filteredEntries = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedResultFilter = "All";

    [ObservableProperty]
    private string _selectedPositionFilter = "All";

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _statusText = "Load data from Dashboard first.";

    [ObservableProperty]
    private ObservableCollection<string> _positionFilterOptions = ["All"];

    public List<string> ResultFilterOptions { get; } = ["All", "PASS", "FAIL"];

    public SessionListViewModel(DashboardViewModel dashboardVm)
    {
        _dashboardVm = dashboardVm;
    }

    [RelayCommand]
    private void Refresh()
    {
        var sessions = _dashboardVm.AllSessions;

        _allEntries = sessions
            .SelectMany(s => s.BoardResults
                .Where(b => b.Result != TestResult.None)
                .Select(b => new SessionBoardEntry { Session = s, Board = b }))
            .OrderByDescending(e => e.StartTime)
            .ToList();

        // Build position filter options
        var positions = _allEntries
            .Select(e => e.Channel)
            .Distinct()
            .OrderBy(c => c)
            .Select(c => $"Pos {c}")
            .ToList();

        PositionFilterOptions = new ObservableCollection<string>(["All", .. positions]);
        SelectedPositionFilter = "All";

        HasData = _allEntries.Count > 0;
        StatusText = HasData
            ? $"{_allEntries.Count} board results from {sessions.Count} records"
            : "Load data from Dashboard first.";
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedResultFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedPositionFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(e =>
                e.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.FixtureId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Operator.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedResultFilter == "PASS")
            query = query.Where(e => e.Result == TestResult.Pass);
        else if (SelectedResultFilter == "FAIL")
            query = query.Where(e => e.Result != TestResult.Pass && e.Result != TestResult.None);

        if (!string.IsNullOrEmpty(SelectedPositionFilter) && SelectedPositionFilter != "All")
        {
            if (int.TryParse(SelectedPositionFilter.Replace("Pos ", ""), out int pos))
                query = query.Where(e => e.Channel == pos);
        }

        FilteredEntries = query.ToList();
    }

    [RelayCommand]
    private async Task NavigateToDetail(SessionBoardEntry? entry)
    {
        if (entry?.Session is null) return;

        var navParams = new Dictionary<string, object>
        {
            { "Session", entry.Session }
        };

        await Shell.Current.GoToAsync(nameof(Views.SessionDetailPage), navParams);
    }
}
