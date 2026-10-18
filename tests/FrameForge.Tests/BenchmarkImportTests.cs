using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class BenchmarkImportTests
{
    static string Rows(string metric,string value="10",int count=100)=>"Application,ProcessID,SwapChainAddress,"+metric+"\n"+string.Join("\n",Enumerable.Repeat("game.exe,12,0x001,"+value,count));
    [Theory][InlineData("MsBetweenPresents","MsBetweenPresents")][InlineData("msBetweenPresents","MsBetweenPresents")][InlineData("FrameTime","FrameTime")]
    public void DocumentedSchemasKeepTheirMeasurementMeaning(string column,string metric)
    {
        var capture=Assert.Single(BenchmarkImport.Read(Rows(column)));Assert.Equal(metric,capture.Metric);Assert.Equal(100,capture.ToTrace().Stats.AverageFps);Assert.Equal(metric,capture.ToTrace().Metric);
    }
    [Fact]public void DifferentApplicationsProcessesAndSwapChainsStaySeparate()
    {
        var csv=Rows("MsBetweenPresents")+"\n"+string.Join("\n",Enumerable.Repeat("other.exe,13,0x002,20",100))+"\n"+string.Join("\n",Enumerable.Repeat("game.exe,12,0x003,5",100));
        var streams=BenchmarkImport.Read(csv);Assert.Equal(3,streams.Count);Assert.Equal(new[]{100d,50d,200d},streams.Select(x=>x.ToTrace().Stats.AverageFps));
    }
    [Fact]public void BomAndQuotedCommasAndEscapedQuotesAreParsed()
    {
        var csv="\uFEFF\"Application\",ProcessID,SwapChainAddress,MsBetweenPresents\r\n"+string.Join("\r\n",Enumerable.Repeat("\"Game, \"\"test\"\".exe\",12,0x1,16.25",100));
        var capture=Assert.Single(BenchmarkImport.Read(csv));Assert.Equal("Game, \"test\".exe",capture.Application);Assert.Equal(16.25,capture.ToTrace().MedianMs);
    }
    [Fact]public void OneInitialZeroIsReportedButNoLaterZeroIsDiscarded()
    {
        var csv=Rows("MsBetweenPresents","0",1)+"\n"+string.Join("\n",Enumerable.Repeat("game.exe,12,0x001,10",100));var capture=Assert.Single(BenchmarkImport.Read(csv));Assert.Equal(1,capture.InitialRowsSkipped);Assert.Equal(100,capture.Durations.Length);
        Assert.Throws<InvalidDataException>(()=>BenchmarkImport.Read(csv+"\ngame.exe,12,0x001,0"));
    }
    [Theory][InlineData("NaN")][InlineData("Infinity")][InlineData("-1")][InlineData("60001")][InlineData("NA")]
    public void InvalidDurationsAreNotSilentlyFiltered(string value)=>Assert.Throws<InvalidDataException>(()=>BenchmarkImport.Read(Rows("MsBetweenPresents",value)));
    [Theory][InlineData("Application,ProcessID,SwapChainAddress,FPS")][InlineData("FrameTime")][InlineData("Application,ProcessID,SwapChainAddress,FrameTime,MsBetweenPresents")][InlineData("Application,ProcessID,SwapChainAddress,MsBetweenPresents,msbetweenpresents")]
    public void UnknownUnitsAndAmbiguousColumnsFailClosed(string header)=>Assert.Throws<InvalidDataException>(()=>BenchmarkImport.Read(header+"\na,1,0x1,10,10"));
    [Fact]public void ShortStreamsStayVisibleButCannotBeImported()
    {
        var capture=Assert.Single(BenchmarkImport.Read(Rows("FrameTime",count:99)));Assert.False(capture.CanImport);Assert.Throws<InvalidDataException>(()=>capture.ToTrace());
    }
    [Fact]public void MissingCellsAndUnterminatedQuotesReportTheLine()
    {
        var error=Assert.Throws<InvalidDataException>(()=>BenchmarkImport.Read(Rows("FrameTime")+"\ngame.exe,12"));Assert.Contains("Line 102",error.Message);
        Assert.Throws<InvalidDataException>(()=>BenchmarkImport.Read(Rows("FrameTime")+"\n\"bad,12,0x1,10"));
    }
    [Fact]public void OriginalNormalizedCapturesStillWork()=>Assert.Equal("FrameTimeMs",Assert.Single(BenchmarkImport.Read("FrameTimeMs\n"+string.Join("\n",Enumerable.Repeat("10",100)))).ToTrace().Metric);
}
