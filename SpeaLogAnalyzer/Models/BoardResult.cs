namespace SpeaLogAnalyzer.Models;

public class BoardResult
{
    public int Channel { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public TestResult Result { get; set; }
    public int RetestCount { get; set; }

    public string ResultDisplay => Result switch
    {
        TestResult.Pass => "PASS",
        TestResult.Fail => "FAIL",
        TestResult.None => "NONE",
        _ => "?"
    };

    public string PositionDisplay => $"Pos {Channel}";

    public string RetestDisplay => RetestCount > 0 ? $"×{RetestCount + 1}" : string.Empty;
    public bool IsRetested => RetestCount > 0;
}
