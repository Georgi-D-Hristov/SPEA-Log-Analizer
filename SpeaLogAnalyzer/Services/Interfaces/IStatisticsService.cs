using SpeaLogAnalyzer.Models;

namespace SpeaLogAnalyzer.Services.Interfaces;

public interface IStatisticsService
{
    double CalculateYield(List<TestSession> sessions);
    int TotalBoards(List<TestSession> sessions);
    int PassedBoards(List<TestSession> sessions);
    int FailedBoards(List<TestSession> sessions);
    List<(string ComponentName, int FailCount)> GetTopFailingComponents(List<TestSession> sessions, int top = 10);
    List<(DateTime Date, double YieldPercent)> GetYieldTrend(List<TestSession> sessions);
    Dictionary<TestCategory, int> GetFailuresByCategory(List<TestSession> sessions);
    List<(string Operator, int SessionCount)> GetOperatorStats(List<TestSession> sessions);
    List<(string FixtureId, int SessionCount, double YieldPercent)> GetFixtureStats(List<TestSession> sessions);
}
