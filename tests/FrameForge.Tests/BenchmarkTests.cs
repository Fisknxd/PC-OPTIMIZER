using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class BenchmarkTests
{
    static string Csv(IEnumerable<string> rows)=>"FrameTimeMs\n"+string.Join("\n",rows);
    [Fact] public void BomQuotedValuesAndWindowsLineEndingsAreSupported()
    {
        var trace=Benchmark.Read("\uFEFF\"FrameTimeMs\"\r\n"+string.Join("\r\n",Enumerable.Repeat("\"16.25\"",100))+"\r\n\r\n");
        Assert.Equal(100,trace.Stats.Frames); Assert.Equal(1000/16.25,trace.Stats.AverageFps,8);
        Assert.Equal(1.625,trace.DurationSeconds,8); Assert.Equal(16.25,trace.MedianMs);
    }
    [Theory] [InlineData("oops")] [InlineData("16,67")] [InlineData("10,20")] [InlineData("Infinity")] [InlineData("NaN")] [InlineData("-1")] [InlineData("0")] [InlineData("60001")] [InlineData("\"10")]
    public void InvalidFrameErrorsIdentifyTheOriginalLine(string value)
    {
        var error=Assert.Throws<InvalidDataException>(()=>Benchmark.Read(Csv(Enumerable.Repeat("10",100).Prepend(value)))); Assert.Contains("Line 2",error.Message);
    }
    [Fact] public void BlankLinesDoNotChangeErrorLineNumbers()
    {
        var error=Assert.Throws<InvalidDataException>(()=>Benchmark.Read("FrameTimeMs\n\n10\n\ninvalid")); Assert.Contains("Line 5",error.Message);
    }
    [Theory] [InlineData("FrameTimeUs")] [InlineData("FrameTimeMs,FPS")] [InlineData("FPS")] [InlineData("")]
    public void UnitsAndColumnsAreNeverGuessed(string header)=>Assert.Throws<InvalidDataException>(()=>Benchmark.Read(header+"\n"+string.Join("\n",Enumerable.Repeat("10",100))));
    [Fact] public void ChartPreservesIsolatedStuttersAndTheLastFrame()
    {
        var times=Enumerable.Repeat(10d,10001).ToArray(); times[121]=250; times[^1]=100;
        var peaks=Benchmark.Peaks(times,100); Assert.Equal(100,peaks.Length); Assert.Contains(250d,peaks); Assert.Equal(100d,peaks[^1]);
    }
    [Fact] public void SmallTracesKeepEveryFrame()=>Assert.Equal(new[]{10d,30d,20d},Benchmark.Peaks([10,30,20],100));
    [Fact] public void MedianAveragesTheTwoMiddleDurationsForEvenCaptures()
    {
        Assert.Equal(2,Benchmark.Median([3,1])); Assert.Equal(3,Benchmark.Median([9,1,3]));
    }
    [Fact] public void StatisticsAndBudgetsIncludeStutters()
    {
        var trace=Benchmark.Read(Csv(Enumerable.Repeat("10",198).Append("50").Append("100")));
        Assert.Equal(200,trace.Stats.Frames); Assert.Equal(1000/75d,trace.Stats.LowOnePercentFps,8);
        Assert.Equal(10,trace.Stats.P99FrameMs); Assert.Equal("Nearest rank",trace.Stats.PercentileMethod);Assert.Equal(2,trace.FramesOverBudget(16.667)); Assert.Equal(1,trace.FramesOverBudget(50));
        Assert.Equal(10,trace.MedianMs); Assert.Equal(2.13,trace.DurationSeconds,8);
    }
    [Fact] public void FrameLimitIsEnforced()=>Assert.Throws<InvalidDataException>(()=>Benchmark.Read(Csv(Enumerable.Repeat("10",Benchmark.MaximumFrames+1))));
    [Fact] public void OldBenchmarkHistoryIsCompatible()
    {
        var json="{\"Time\":\"2026-01-01T00:00:00Z\",\"Source\":\"old.csv\",\"Stats\":{\"Frames\":100,\"AverageFps\":100,\"LowOnePercentFps\":100,\"P99FrameMs\":10},\"Hardware\":{}}";
        var run=System.Text.Json.JsonSerializer.Deserialize<BenchmarkRun>(json)!; Assert.Equal("old.csv",run.Source); Assert.Equal("",run.Label); Assert.Equal(0,run.DurationSeconds);
    }
}
