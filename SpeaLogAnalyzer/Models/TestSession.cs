namespace SpeaLogAnalyzer.Models;

public class TestSession
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string FixtureId { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TestResult OverallResult { get; set; }
    public List<BoardResult> BoardResults { get; set; } = [];
    public List<TestMeasurement> Measurements { get; set; } = [];

    public int TotalBoards => BoardResults.Count(b => b.Result != TestResult.None);
    public int PassedBoards => BoardResults.Count(b => b.Result == TestResult.Pass);
    public int FailedBoards => BoardResults.Count(b => b.Result == TestResult.Fail);

    public double YieldPercent => TotalBoards > 0
        ? Math.Round(PassedBoards * 100.0 / TotalBoards, 1)
        : 0;

    public string OverallResultDisplay => OverallResult switch
    {
        TestResult.Pass => "PASS",
        TestResult.Fail => "FAIL",
        _ => "?"
    };

    public int TotalMeasurements => Measurements.Count;
    public int FailedMeasurements => Measurements.Count(m => m.Result != TestResult.Pass && m.Result != TestResult.None);
}
