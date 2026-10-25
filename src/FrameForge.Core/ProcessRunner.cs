using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FrameForge.Core;

// A timeout covers the process and both redirected pipes. A descendant can keep a pipe
// open after its parent exits, so waiting only for the parent is insufficient.
public static class ProcessRunner
{
    public static async Task<string> Run(string file,IEnumerable<string> arguments,TimeSpan timeout,ActivityFeed? activity=null,int maximumOutputCharacters=4*1024*1024)
    {
        if(timeout<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(timeout));
        if(maximumOutputCharacters<=0)throw new ArgumentOutOfRangeException(nameof(maximumOutputCharacters));
        var start=new ProcessStartInfo(file){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var argument in arguments)start.ArgumentList.Add(argument);
        using var cancellation=new CancellationTokenSource(timeout);
        using var process=Process.Start(start)??throw new IOException("Could not start Windows utility.");
        async Task<string> Read(StreamReader reader,string prefix)
        {
            var result=new StringBuilder();var line=new StringBuilder();bool shortened=false;var buffer=new char[4096];
            void Report()
            {
                if(activity!=null)activity.Report(prefix+line+(shortened?" … [display shortened]":""));
                line.Clear();shortened=false;
            }
            while(await reader.ReadAsync(buffer.AsMemory(),cancellation.Token).ConfigureAwait(false) is var count&&count>0)
            {
                if(count>maximumOutputCharacters-result.Length)throw new IOException("Windows utility output exceeded the supported limit. No complete result was returned.");
                result.Append(buffer,0,count);
                if(activity==null)continue;
                for(int i=0;i<count;i++)
                {
                    if(buffer[i]=='\n')Report();
                    else if(buffer[i]!='\r'){if(line.Length<4096)line.Append(buffer[i]);else shortened=true;}
                }
            }
            if(line.Length>0||shortened)Report();
            return result.ToString();
        }
        var output=Read(process.StandardOutput,"");var error=Read(process.StandardError,"stderr: ");
        var pending=new List<Task>{process.WaitForExitAsync(cancellation.Token),output,error};
        void Stop()
        {
            cancellation.Cancel();
            try{if(!process.HasExited)process.Kill(entireProcessTree:true);}
            catch(Exception e)when(e is InvalidOperationException or Win32Exception or NotSupportedException){ }
        }
        try
        {
            while(pending.Count>0)
            {
                var completed=await Task.WhenAny(pending).WaitAsync(cancellation.Token).ConfigureAwait(false);
                await completed.ConfigureAwait(false);pending.Remove(completed);
            }
            var text=await output.ConfigureAwait(false);var errors=await error.ConfigureAwait(false);
            if(process.ExitCode!=0)throw new IOException($"{Path.GetFileName(file)} failed ({process.ExitCode}): {errors.Trim()} {text.Trim()}");
            return text.Trim();
        }
        catch(OperationCanceledException)when(cancellation.IsCancellationRequested)
        {
            Stop();throw new TimeoutException("Windows utility timed out; rescan before retrying.");
        }
        catch{Stop();throw;}
        finally
        {
            // Failed/cancelled pipe readers can complete after disposal; observe their errors.
            foreach(var task in pending)_=task.ContinueWith(t=>{_=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
        }
    }
}
