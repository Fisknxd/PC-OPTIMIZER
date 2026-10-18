using FrameForge.Core;
using Xunit;
namespace FrameForge.Tests;
public sealed class RecoveryReliabilityTests:IDisposable
{
    readonly string root=Path.Combine(Path.GetTempPath(),"ff-recovery-"+Guid.NewGuid().ToString("N"));
    static Optimization Op=>Catalog.All.Single(x=>x.Id=="game-recording");
    static SettingValue Group(params SettingValue[] values)=>GroupedSettings.Pack(values);
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    sealed class Settings:ISystemSettings
    {
        public bool IsAdministrator=>true;public SettingValue State=Group(new(true,"0"),new(false,null));public int Writes;public bool FailPartialRestore;
        public Task<SettingValue> Read(Optimization op)=>Task.FromResult(State);
        public Task Write(Optimization op,SettingValue value)
        {
            Writes++;if(FailPartialRestore){FailPartialRestore=false;State=Group(TweakDefinitions.ReadGroup(value,TweakDefinitions.Get(op))[0],TweakDefinitions.ReadGroup(State,TweakDefinitions.Get(op))[1]);throw new IOException("Interrupted grouped restore");}State=value;return Task.CompletedTask;
        }
    }
    [Fact]public async Task FailedSecondWriteRollsBackTheFirstIncludingOriginalAbsence()
    {
        var definition=TweakDefinitions.Get(Op);var original=new[]{new SettingValue(false,null),new SettingValue(true,"0")};var state=original.ToArray();
        await Assert.ThrowsAsync<IOException>(()=>GroupedSettings.Write(definition,FeatureToggles.Target(Op,true),i=>Task.FromResult(state[i]),(i,value)=>{if(i==1&&value.Value=="1")throw new IOException("Access denied");state[i]=value;return Task.CompletedTask;}));
        Assert.Equal(original,state);
    }
    [Fact]public async Task WriteThatMutatesThenThrowsIsAlsoRecovered()
    {
        var original=new[]{new SettingValue(true,"0"),new SettingValue(false,null)};var state=original.ToArray();
        await Assert.ThrowsAsync<IOException>(()=>GroupedSettings.Write(TweakDefinitions.Get(Op),FeatureToggles.Target(Op,true),i=>Task.FromResult(state[i]),(i,value)=>{state[i]=value;if(i==1&&value.Value=="1")throw new IOException("Flush failed");return Task.CompletedTask;}));
        Assert.Equal(original,state);
    }
    [Fact]public async Task CompensationPreservesUnexpectedExternalChangesAndStillRestoresOtherOwnedValues()
    {
        var state=new[]{new SettingValue(true,"0"),new SettingValue(true,"0")};
        var error=await Assert.ThrowsAsync<IOException>(()=>GroupedSettings.Write(TweakDefinitions.Get(Op),FeatureToggles.Target(Op,true),i=>Task.FromResult(state[i]),(i,value)=>{state[i]=value;if(i==1&&value.Value=="1"){state[0]=new(true,"7");throw new IOException("Concurrent edit");}return Task.CompletedTask;}));
        Assert.Contains("Recovery",error.Message);Assert.Equal("7",state[0].Value);Assert.Equal("0",state[1].Value);
    }
    [Theory][InlineData("Prepared")][InlineData("Failed")][InlineData("Restoring")]
    public async Task InterruptedMixedGroupCanRestoreWithoutReplacingUnknownValues(string status)
    {
        var settings=new Settings();var before=settings.State;var after=FeatureToggles.Target(Op,true);settings.State=Group(new(true,"1"),new(false,null));
        var store=new JsonStore(root);var record=new ChangeRecord{OperationId=Op.Id,Name=Op.Name,Before=before,After=after,Status=status};store.Save("history.json",new[]{record});
        var result=await new OperationEngine(settings,store).Undo(record.Id);Assert.Equal("Restored",result.Status);Assert.Equal(before,settings.State);Assert.Equal(1,settings.Writes);
    }
    [Fact]public async Task VerifiedGroupWithExternalMixedStateRemainsAConflictEvenAfterRepeatedUndo()
    {
        var settings=new Settings();var store=new JsonStore(root);var record=new ChangeRecord{OperationId=Op.Id,Before=settings.State,After=FeatureToggles.Target(Op,true),Status="Applied",WriteCompleted=true};store.Save("history.json",new[]{record});settings.State=Group(new(true,"1"),new(false,null));
        var engine=new OperationEngine(settings,store);Assert.Equal("Restore failed",(await engine.Undo(record.Id)).Status);Assert.Equal("Restore failed",(await engine.Undo(record.Id)).Status);Assert.Equal(0,settings.Writes);
    }
    [Fact]public async Task UnknownGroupMemberCannotBeOverwrittenByInterruptedRecovery()
    {
        var settings=new Settings();var store=new JsonStore(root);var record=new ChangeRecord{OperationId=Op.Id,Before=settings.State,After=FeatureToggles.Target(Op,true),Status="Prepared"};store.Save("history.json",new[]{record});settings.State=Group(new(true,"7"),new(false,null));
        Assert.Equal("Restore failed",(await new OperationEngine(settings,store).Undo(record.Id)).Status);Assert.Equal(0,settings.Writes);
    }
    [Fact]public async Task InterruptedUndoCanResumeFromADurableRestorePhase()
    {
        var settings=new Settings();var before=settings.State;var engine=new OperationEngine(settings,new(root));var h=new HardwareSnapshot{WindowsSupported=true};var record=await engine.Apply(await engine.PreviewToggle(Op,h,true),h,"group");
        settings.FailPartialRestore=true;var failed=await engine.Undo(record!.Id);Assert.Equal("Restore failed",failed.Status);Assert.True(failed.RestoreInProgress);
        var result=await new OperationEngine(settings,new(root)).Undo(record.Id);Assert.Equal("Restored",result.Status);Assert.False(result.RestoreInProgress);Assert.Equal(before,settings.State);
    }
    [Fact]public async Task SessionUndoStopsAtTheFirstConflictAndKeepsOlderOriginals()
    {
        var settings=new Settings{State=new(true,"0")};var engine=new OperationEngine(settings,new(root));var h=new HardwareSnapshot{WindowsSupported=true};var op=Catalog.All.Single(x=>x.Id=="dark");
        var first=await engine.Apply(await engine.PreviewToggle(op,h,false),h,"session");var second=await engine.Apply(await engine.PreviewToggle(op,h,true),h,"session");settings.State=new(true,"2");var writes=settings.Writes;
        var stopped=await RecoverySessions.Undo(engine,"session");Assert.Equal(0,stopped.Restored);Assert.Equal(2,stopped.Remaining);Assert.NotNull(stopped.Error);Assert.Equal(writes,settings.Writes);
        settings.State=second!.After;var complete=await RecoverySessions.Undo(engine,"session");Assert.Equal(2,complete.Restored);Assert.Equal(0,complete.Remaining);Assert.Equal(first!.Before,settings.State);
    }
}
