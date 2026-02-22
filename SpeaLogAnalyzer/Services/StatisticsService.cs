using SpeaLogAnalyzer.Models;
using SpeaLogAnalyzer.Services.Interfaces;

namespace SpeaLogAnalyzer.Services;

public class StatisticsService : IStatisticsService
{
    public double CalculateYield(List<TestSession> sessions)
    {
        int total = TotalBoards(sessions);
        int passed = PassedBoards(sessions);
        return total > 0 ? Math.Round(passed * 100.0 / total, 1) : 0;
    }

    public int TotalBoards(List<TestSession> sessions)
    {
        return sessions.SelectMany(s => s.BoardResults)
                       .Count(b => b.Result != TestResult.None);
    }

    public int PassedBoards(List<TestSession> sessions)
    {
        return sessions.SelectMany(s => s.BoardResults)
                       .Count(b => b.Result == TestResult.Pass);
    }

    public int FailedBoards(List<TestSession> sessions)
    {
        return sessions.SelectMany(s => s.BoardResults)
                       .Count(b => b.Result == TestResult.Fail);
    }

    public List<(string ComponentName, int FailCount)> GetTopFailingComponents(
        List<TestSession> sessions, int top = 10)
    {
        return sessions
            .SelectMany(s => s.Measurements)
            .Where(m => m.Result != TestResult.Pass && m.Result != TestResult.None)
            .GroupBy(m => m.ComponentName)
            .Select(g => (ComponentName: g.Key, FailCount: g.Count()))
            .OrderByDescending(x => x.FailCount)
            .Take(top)
            .ToList();
    }

    public List<(DateTime Date, double YieldPercent)> GetYieldTrend(List<TestSession> sessions)
    {
        return sessions
            .Where(s => s.StartTime != default)
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var boards = g.SelectMany(s => s.BoardResults)
                              .Where(b => b.Result != TestResult.None)
                              .ToList();
                int total = boards.Count;
                int passed = boards.Count(b => b.Result == TestResult.Pass);
                double yield = total > 0 ? Math.Round(passed * 100.0 / total, 1) : 0;
                return (Date: g.Key, YieldPercent: yield);
            })
            .ToList();
    }

    public Dictionary<TestCategory, int> GetFailuresByCategory(List<TestSession> sessions)
    {
        return sessions
            .SelectMany(s => s.Measurements)
            .Where(m => m.Result != TestResult.Pass && m.Result != TestResult.None)
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public List<(string Operator, int SessionCount)> GetOperatorStats(List<TestSession> sessions)
    {
        return sessions
            .GroupBy(s => s.Operator)
            .Select(g => (Operator: g.Key, SessionCount: g.Count()))
            .OrderByDescending(x => x.SessionCount)
            .ToList();
    }

    public List<(string FixtureId, int SessionCount, double YieldPercent)> GetFixtureStats(
        List<TestSession> sessions)
    {
        return sessions
            .GroupBy(s => s.FixtureId)
            .Select(g =>
            {
                var boards = g.SelectMany(s => s.BoardResults)
                              .Where(b => b.Result != TestResult.None)
                              .ToList();
                int total = boards.Count;
                int passed = boards.Count(b => b.Result == TestResult.Pass);
                double yield = total > 0 ? Math.Round(passed * 100.0 / total, 1) : 0;
                return (FixtureId: g.Key, SessionCount: g.Count(), YieldPercent: yield);
            })
            .OrderByDescending(x => x.SessionCount)
            .ToList();
    }
}
