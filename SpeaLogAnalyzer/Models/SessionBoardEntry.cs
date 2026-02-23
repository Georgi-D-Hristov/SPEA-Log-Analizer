namespace SpeaLogAnalyzer.Models;

/// <summary>
/// Flattened view of a session + individual board result for display in the Session list.
/// </summary>
public class SessionBoardEntry
{
    public TestSession Session { get; set; } = null!;
    public BoardResult Board { get; set; } = null!;

    // Shortcut properties for binding
    public string SerialNumber => Board.SerialNumber;
    public int Channel => Board.Channel;
    public string PositionDisplay => Board.PositionDisplay;
    public TestResult Result => Board.Result;
    public string ResultDisplay => Board.ResultDisplay;
    public int RetestCount => Board.RetestCount;
    public string RetestDisplay => Board.RetestDisplay;
    public bool IsRetested => Board.IsRetested;
    public DateTime StartTime => Session.StartTime;
    public string FixtureId => Session.FixtureId;
    public string Operator => Session.Operator;
    public string FileName => Session.FileName;
    public string Program => Session.Program;
}
