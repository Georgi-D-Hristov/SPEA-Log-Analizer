using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.ViewModels;

[QueryProperty(nameof(Session), "Session")]
public partial class SessionDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private TestSession? _session;

    [ObservableProperty]
    private List<TestMeasurement> _filteredMeasurements = [];

    [ObservableProperty]
    private string _selectedChannelFilter = "All";

    [ObservableProperty]
    private string _selectedResultFilter = "All";

    [ObservableProperty]
    private string _selectedCategoryFilter = "All";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _totalMeasurements;

    [ObservableProperty]
    private int _failedMeasurements;

    public List<string> ChannelFilterOptions { get; } = ["All", "1", "2", "3", "4"];
    public List<string> ResultFilterOptions { get; } = ["All", "PASS", "FAIL"];
    public List<string> CategoryFilterOptions { get; } =
    [
        "All", "Link", "Short", "Resistance", "Capacitance", "Diode",
        "TransistorNPN", "TransistorPNP", "Mosfet", "TVS", "Optocoupler",
        "JtagScan", "Functional", "PassMarking"
    ];

    partial void OnSessionChanged(TestSession? value)
    {
        if (value is not null)
        {
            TotalMeasurements = value.TotalMeasurements;
            FailedMeasurements = value.FailedMeasurements;
            ApplyFilters();
        }
    }

    partial void OnSelectedChannelFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedResultFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryFilterChanged(string value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        if (Session is null) return;

        var query = Session.Measurements.AsEnumerable();

        if (SelectedChannelFilter != "All" && int.TryParse(SelectedChannelFilter, out int channel))
            query = query.Where(m => m.Channel == channel);

        if (SelectedResultFilter == "PASS")
            query = query.Where(m => m.Result == TestResult.Pass);
        else if (SelectedResultFilter == "FAIL")
            query = query.Where(m => m.Result != TestResult.Pass && m.Result != TestResult.None);

        if (SelectedCategoryFilter != "All" &&
            Enum.TryParse<TestCategory>(SelectedCategoryFilter, out var category))
            query = query.Where(m => m.Category == category);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(m =>
                m.ComponentName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredMeasurements = query.ToList();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
