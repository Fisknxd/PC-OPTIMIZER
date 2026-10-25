using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class ProfileSelectionTests
{
    static HashSet<string> Available()=>LowEndPlans.Choices.Select(x=>x.OperationId).ToHashSet();
    [Fact]public void StrongBrowserChoiceIncludesItsPrerequisite()=>Assert.Equal(new[]{"chrome-memory","chrome-memory-strong"},ProfileSelection.Resolve(["chrome-memory-strong"],Available(),new HashSet<string>()).Order());
    [Fact]public void TurningOffTheBaseAlsoUnchecksItsDependent()
    {
        var result=ProfileSelection.Toggle(["edge-sleep","edge-sleep-sooner"],"edge-sleep",false,Available(),new HashSet<string>());Assert.Empty(result);
    }
    [Fact]public void AlreadyConfiguredPrerequisiteDoesNotNeedAnotherWrite()
    {
        var available=Available();available.Remove("chrome-memory");var result=ProfileSelection.Resolve(["chrome-memory-strong"],available,new HashSet<string>{"chrome-memory"});Assert.Equal(new[]{"chrome-memory-strong"},result);
    }
    [Fact]public void MissingPrerequisiteMakesTheDependentUnavailable()
    {
        var available=Available();available.Remove("edge-sleep");Assert.Empty(ProfileSelection.Resolve(["edge-sleep-sooner"],available,new HashSet<string>()));
    }
    [Fact]public void PresetsNeverIncludeUnavailableOrUnknownOperations()
    {
        var result=ProfileSelection.Resolve(ProfileSelection.Preset("Browser memory").Append("unknown"),new HashSet<string>{"chrome-background"},new HashSet<string>());Assert.Equal(new[]{"chrome-background"},result);
    }
    [Fact]public void EmptySavedSelectionRemainsDistinctFromFreshDefaults()
    {
        var old=System.Text.Json.JsonSerializer.Deserialize<Preferences>("{}");Assert.Null(old!.LowEndSelection);
        var cleared=System.Text.Json.JsonSerializer.Deserialize<Preferences>("{\"LowEndSelection\":[]}");Assert.Empty(cleared!.LowEndSelection!);Assert.NotEmpty(ProfileSelection.Preset("Recommended"));
    }
    [Fact]public void OldStatisticsKeepTheirHistoricalMethod()
    {
        var stats=System.Text.Json.JsonSerializer.Deserialize<FrameStats>("{\"Frames\":100,\"AverageFps\":100,\"LowOnePercentFps\":100,\"P99FrameMs\":10}");Assert.Equal("Legacy tail",stats!.PercentileMethod);
        var trace=Benchmark.FromDurations(Enumerable.Repeat(10d,99).Append(100).ToArray());Assert.Equal(10,trace.Stats.P99FrameMs);Assert.Equal("Nearest rank",trace.Stats.PercentileMethod);
    }
}
