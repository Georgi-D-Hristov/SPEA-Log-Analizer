namespace SpeaLogAnalyzer.Models;

public class BoardResult
{
    public int Channel { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public TestResult Result { get; set; }

    public string ResultDisplay => Result switch
    {
        TestResult.Pass => "PASS",
        TestResult.Fail => "FAIL",
        TestResult.None => "NONE",
        _ => "?"
    };
}
