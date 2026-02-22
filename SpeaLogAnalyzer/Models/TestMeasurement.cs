namespace SpeaLogAnalyzer.Models;

public class TestMeasurement
{
    public string RecordType { get; set; } = string.Empty; // ANL or FUNC
    public int Channel { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int SubStep { get; set; }
    public string Description { get; set; } = string.Empty;
    public TestResult Result { get; set; }
    public double MeasuredValue { get; set; }
    public double LowLimit { get; set; }
    public double HighLimit { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string TestPoints { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public TestCategory Category { get; set; }

    public bool HasNoLowLimit => LowLimit <= -1e9;
    public bool HasNoHighLimit => HighLimit >= 1e9;

    public string ResultDisplay => Result switch
    {
        TestResult.Pass => "PASS",
        TestResult.FailHigh => "FAIL(+)",
        TestResult.FailLow => "FAIL(-)",
        TestResult.Fail => "FAIL",
        TestResult.None => "NONE",
        _ => "?"
    };
}
