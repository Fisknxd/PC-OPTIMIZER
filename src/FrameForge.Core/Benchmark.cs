using System.Globalization;

namespace FrameForge.Core;

public sealed record FrameTrace(FrameStats Stats, double[] Milliseconds,string Metric="FrameTimeMs")
{
    public double DurationSeconds => Milliseconds.Sum() / 1000;
    public double MedianMs => Benchmark.Median(Milliseconds);
    public int FramesOverBudget(double milliseconds) => Milliseconds.Count(x => x > milliseconds);
}

public sealed record BenchmarkRun(DateTimeOffset Time, string Source, FrameStats Stats, HardwareSnapshot Hardware,
    string Label = "", string Notes = "", double DurationSeconds = 0,string Metric="FrameTimeMs",string Application="");

public static class Benchmark
{
    public const int MaximumFrames = 1_000_000;
    public static FrameStats Parse(string csv) => Read(csv).Stats;

    // A deliberately narrow, documented format. Never guess units or silently drop invalid data.
    public static FrameTrace Read(string csv)
    {
        using var reader = new StringReader(csv);
        var header = reader.ReadLine()?.Trim().TrimStart('\uFEFF').Trim().Trim('"');
        if (header != "FrameTimeMs")
            throw new InvalidDataException("Expected a single column named FrameTimeMs, with frame durations in milliseconds.");
        var times = new List<double>();
        int lineNumber = 1;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (times.Count >= MaximumFrames)
                throw new InvalidDataException($"CSV exceeds the {MaximumFrames:N0} frame limit. Export a shorter capture.");
            var value = line.Trim();
            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2) value = value[1..^1];
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ||
                !double.IsFinite(duration) || duration <= 0 || duration > 60000)
                throw new InvalidDataException($"Line {lineNumber}: expected one finite duration greater than 0 and at most 60000 ms. Use a decimal point, without extra columns.");
            times.Add(duration);
        }
        return FromDurations(times);
    }
    public static FrameTrace FromDurations(IReadOnlyList<double> times,string metric="FrameTimeMs")
    {
        if(times.Count<100||times.Count>MaximumFrames||times.Any(x=>!double.IsFinite(x)||x<=0||x>60000))throw new InvalidDataException("A capture needs 100 to 1,000,000 finite frame durations greater than 0 and at most 60000 ms.");
        var sorted = times.OrderDescending().ToArray();
        var slowCount = Math.Max(1, (int)Math.Ceiling(times.Count * .01));
        var stats = new FrameStats(times.Count, 1000 / times.Average(),
            1000 / sorted.Take(slowCount).Average(), sorted[times.Count-(int)Math.Ceiling(times.Count*.99)],"Nearest rank");
        return new(stats, times.ToArray(),metric);
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) throw new ArgumentException("At least one value is required.",nameof(values));
        var ordered = values.Order().ToArray();
        int middle=ordered.Length/2;
        return ordered.Length%2==0?(ordered[middle-1]+ordered[middle])/2:ordered[middle];
    }

    // Keep the worst frame in every bucket: subsampling every nth frame hides stutters.
    public static double[] Peaks(IReadOnlyList<double> values, int buckets)
    {
        if (buckets <= 0) throw new ArgumentOutOfRangeException(nameof(buckets));
        if (values.Count <= buckets) return values.ToArray();
        var peaks = new double[buckets];
        for (int i = 0; i < values.Count; i++)
        {
            int bucket = (int)((long)i * buckets / values.Count);
            peaks[bucket] = Math.Max(peaks[bucket], values[i]);
        }
        return peaks;
    }
}
