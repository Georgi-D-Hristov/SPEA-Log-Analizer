using System.Globalization;
using SpeaLogAnalyzer.Models;
using SpeaLogAnalyzer.Services.Interfaces;

namespace SpeaLogAnalyzer.Services;

public class LogParserService : ILogParserService
{
    public async Task<TestSession> ParseFileAsync(string filePath, CancellationToken ct = default)
    {
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        var session = new TestSession
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath
        };

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "START":
                    ParseStartLine(parts, session);
                    break;
                case "ANL":
                    ParseMeasurementLine(parts, "ANL", session);
                    break;
                case "FUNC":
                    ParseMeasurementLine(parts, "FUNC", session);
                    break;
                case "SN":
                    ParseSerialNumbers(parts, session);
                    break;
                case "BOARDRESULT":
                    ParseBoardResults(parts, session);
                    break;
                case "END":
                    ParseEndLine(parts, session);
                    break;
            }
        }

        return session;
    }

    public async Task<List<TestSession>> ParseFolderAsync(
        string folderPath,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var files = Directory.GetFiles(folderPath, "CDCOLLK_*.txt")
                             .OrderBy(f => f)
                             .ToArray();

        var sessions = new List<TestSession>(files.Length);
        int processed = 0;

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var session = await ParseFileAsync(file, ct);
                sessions.Add(session);
            }
            catch (Exception)
            {
                // Skip files that can't be parsed
            }

            processed++;
            progress?.Report((int)(processed * 100.0 / files.Length));
        }

        return sessions;
    }

    private static void ParseStartLine(string[] parts, TestSession session)
    {
        // START;Site;Program;PanelSize;FixtureID;Operator;Date;Time
        if (parts.Length >= 8)
        {
            session.Site = parts[1];
            session.Program = parts[2];
            session.FixtureId = parts[4];
            session.Operator = parts[5];

            if (DateTime.TryParseExact(
                    $"{parts[6]} {parts[7]}",
                    "MM/dd/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var startTime))
            {
                session.StartTime = startTime;
            }
        }
    }

    private static void ParseMeasurementLine(string[] parts, string recordType, TestSession session)
    {
        // ANL/FUNC;Channel;ComponentName;StepNum;SubStep;Description;;Result;MeasuredValue;LowLimit;HighLimit;Unit;TestPoints;TestId
        if (parts.Length < 12)
            return;

        var measurement = new TestMeasurement
        {
            RecordType = recordType,
            Channel = ParseInt(parts[1]),
            ComponentName = parts[2],
            StepNumber = ParseInt(parts[3]),
            SubStep = ParseInt(parts[4]),
            Description = parts[5],
            Result = ParseResult(parts[7]),
            MeasuredValue = ParseDouble(parts[8]),
            LowLimit = ParseDouble(parts[9]),
            HighLimit = ParseDouble(parts[10]),
            Unit = parts[11],
            TestPoints = parts.Length > 12 ? parts[12] : string.Empty,
            TestId = parts.Length > 13 ? parts[13] : string.Empty
        };

        measurement.Category = DetermineCategory(measurement.Description, recordType);
        session.Measurements.Add(measurement);
    }

    private static void ParseSerialNumbers(string[] parts, TestSession session)
    {
        // SN;serial1;serial2;serial3;serial4
        for (int i = 1; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
            {
                var board = session.BoardResults.FirstOrDefault(b => b.Channel == i);
                if (board != null)
                {
                    board.SerialNumber = parts[i];
                }
                else
                {
                    session.BoardResults.Add(new BoardResult
                    {
                        Channel = i,
                        SerialNumber = parts[i],
                        Result = TestResult.None
                    });
                }
            }
        }
    }

    private static void ParseBoardResults(string[] parts, TestSession session)
    {
        // BOARDRESULT;PASS;PASS;FAIL;FAIL
        for (int i = 1; i < parts.Length; i++)
        {
            var result = parts[i].Trim().ToUpperInvariant() switch
            {
                "PASS" => TestResult.Pass,
                "FAIL" => TestResult.Fail,
                "NONE" => TestResult.None,
                _ => TestResult.None
            };

            var board = session.BoardResults.FirstOrDefault(b => b.Channel == i);
            if (board != null)
            {
                board.Result = result;
            }
            else
            {
                session.BoardResults.Add(new BoardResult
                {
                    Channel = i,
                    Result = result
                });
            }
        }
    }

    private static void ParseEndLine(string[] parts, TestSession session)
    {
        // END;OverallResult;Date;Time
        if (parts.Length >= 4)
        {
            session.OverallResult = parts[1].Trim().ToUpperInvariant() switch
            {
                "PASS" => TestResult.Pass,
                "FAIL" => TestResult.Fail,
                _ => TestResult.None
            };

            if (DateTime.TryParseExact(
                    $"{parts[2]} {parts[3]}",
                    "MM/dd/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var endTime))
            {
                session.EndTime = endTime;
            }
        }
    }

    private static TestResult ParseResult(string value)
    {
        return value.Trim() switch
        {
            "PASS" => TestResult.Pass,
            "FAIL(+)" => TestResult.FailHigh,
            "FAIL(-)" => TestResult.FailLow,
            "FAIL(+/-)" => TestResult.Fail,
            "FAIL" => TestResult.Fail,
            "NONE" => TestResult.None,
            _ => TestResult.None
        };
    }

    private static double ParseDouble(string value)
    {
        if (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value.Trim(), out var result))
            return result;
        return 0;
    }

    private static TestCategory DetermineCategory(string description, string recordType)
    {
        if (recordType == "FUNC")
            return TestCategory.Functional;

        var upper = description.ToUpperInvariant();

        if (upper.StartsWith("LNK"))
            return TestCategory.Link;
        if (upper.StartsWith("SHO"))
            return TestCategory.Short;
        if (upper.StartsWith("RES") || upper.StartsWith("RESR"))
            return TestCategory.Resistance;
        if (upper.StartsWith("CAP"))
            return TestCategory.Capacitance;
        if (upper.StartsWith("DIOD"))
            return TestCategory.Diode;
        if (upper.StartsWith("TRN"))
            return TestCategory.TransistorNPN;
        if (upper.StartsWith("TRP"))
            return TestCategory.TransistorPNP;
        if (upper.StartsWith("MOS"))
            return TestCategory.Mosfet;
        if (upper.StartsWith("TVS"))
            return TestCategory.TVS;
        if (upper.StartsWith("OPTC"))
            return TestCategory.Optocoupler;
        if (upper.StartsWith("JSCAN"))
            return TestCategory.JtagScan;
        if (upper.Contains("PASS-MARKING") || upper.Contains("PASSMARKING"))
            return TestCategory.PassMarking;

        return TestCategory.Unknown;
    }
}
