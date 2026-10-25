using System.Globalization;
using System.Text;
namespace FrameForge.Core;

public sealed record BenchmarkCapture(string Application,string ProcessId,string SwapChain,string Metric,double[] Durations,int InitialRowsSkipped=0)
{
    public bool CanImport=>Durations.Length>=100;
    public string Label=>string.IsNullOrEmpty(Application)?$"Frame-time capture · {Durations.Length:N0} frames":$"{Application} · PID {ProcessId} · {Durations.Length:N0} frames · {SwapChain}";
    public string Explanation=>Metric=="MsBetweenPresents"?"Time between app presentations. Includes app frames that may not reach the screen; this is not displayed FPS or input latency.":Metric=="FrameTime"?"CPU frame-start intervals from PresentMon 2.x. This is not displayed FPS or input latency.":"Imported frame durations in milliseconds. The source tool determines what was measured.";
    public FrameTrace ToTrace()=>Benchmark.FromDurations(Durations,Metric);
}
public static class BenchmarkImport
{
    public const int MaximumCharacters=20*1024*1024;
    // Exact documented schemas only. Application/PID/swap-chain identity prevents merging
    // unrelated presentation streams into a fictitious high frame rate.
    public static IReadOnlyList<BenchmarkCapture> Read(string csv)
    {
        if(csv.Length>MaximumCharacters)throw new InvalidDataException("Capture exceeds the 20 MiB text limit.");
        using var reader=new StringReader(csv);var first=reader.ReadLine()??throw new InvalidDataException("The capture is empty.");
        var headers=Cells(first.Trim().TrimStart('\uFEFF').Trim(),1).Select(x=>x.Trim()).ToArray();
        if(headers.Length==1&&headers[0]=="FrameTimeMs")
        {
            var trace=Benchmark.Read(csv);return [new("","","","FrameTimeMs",trace.Milliseconds)];
        }
        if(headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=headers.Length)throw new InvalidDataException("The capture has duplicate column names.");
        int app=Array.IndexOf(headers,"Application"),pid=Array.IndexOf(headers,"ProcessID"),chain=Array.IndexOf(headers,"SwapChainAddress");
        var candidates=headers.Select((name,index)=>(name,index)).Where(x=>x.name is "MsBetweenPresents" or "msBetweenPresents" or "FrameTime").ToList();
        if(app<0||pid<0||chain<0||candidates.Count!=1)throw new InvalidDataException("Use FrameTimeMs, or a PresentMon CSV with Application, ProcessID, SwapChainAddress and exactly one supported timing column: MsBetweenPresents or FrameTime. Units are never guessed.");
        var timing=candidates[0];var metric=timing.name=="FrameTime"?"FrameTime":"MsBetweenPresents";
        var streams=new Dictionary<(string App,string Pid,string Chain),(List<double> Times,int Skipped)>();
        int lineNumber=1,rows=0;
        while(reader.ReadLine() is {} line)
        {
            lineNumber++;if(string.IsNullOrWhiteSpace(line))continue;
            if(++rows>Benchmark.MaximumFrames)throw new InvalidDataException("Capture has too many frame rows; export a shorter recording.");
            var cells=Cells(line,lineNumber);if(cells.Count!=headers.Length)throw new InvalidDataException($"Line {lineNumber}: column count does not match the header.");
            var identity=(App:cells[app],Pid:cells[pid],Chain:cells[chain]);
            if(string.IsNullOrWhiteSpace(identity.App)||identity.App.Length>512||identity.App.Any(char.IsControl)||!uint.TryParse(identity.Pid,NumberStyles.None,CultureInfo.InvariantCulture,out var process)||process==0||string.IsNullOrWhiteSpace(identity.Chain)||identity.Chain.Length>64||identity.Chain.Any(char.IsControl))throw new InvalidDataException($"Line {lineNumber}: missing or invalid application, process or presentation-stream identity.");
            if(!streams.TryGetValue(identity,out var stream))
            {
                if(streams.Count>=256)throw new InvalidDataException("Capture contains more than 256 presentation streams. Record only the game.");
                stream=([],0);streams.Add(identity,stream);
            }
            if(!double.TryParse(cells[timing.index],NumberStyles.Float,CultureInfo.InvariantCulture,out var duration)||!double.IsFinite(duration)||duration<0||duration>60000)throw new InvalidDataException($"Line {lineNumber}: invalid frame duration in milliseconds.");
            // PresentMon can have no previous interval on the first row. Nothing else is dropped.
            if(duration==0)
            {
                if(stream.Times.Count!=0||stream.Skipped!=0)throw new InvalidDataException($"Line {lineNumber}: zero duration is allowed only on the first row of a stream.");
                streams[identity]=(stream.Times,1);continue;
            }
            stream.Times.Add(duration);
        }
        if(streams.Count==0)throw new InvalidDataException("No presentation streams were found.");
        return streams.Select(x=>new BenchmarkCapture(x.Key.App,x.Key.Pid,x.Key.Chain,metric,x.Value.Times.ToArray(),x.Value.Skipped)).ToList();
    }
    static List<string> Cells(string line,int number)
    {
        if(line.Length>32768)throw new InvalidDataException($"Line {number}: CSV row is too long.");
        var cells=new List<string>();var value=new StringBuilder();bool quoted=false,closed=false;
        for(int i=0;i<line.Length;i++)
        {
            char c=line[i];
            if(quoted)
            {
                if(c=='"'){if(i+1<line.Length&&line[i+1]=='"'){value.Append('"');i++;}else{quoted=false;closed=true;}}
                else value.Append(c);
            }
            else if(c==','){cells.Add(value.ToString());value.Clear();closed=false;}
            else if(c=='"'&&value.Length==0&&!closed)quoted=true;
            else if(closed||c=='"')throw new InvalidDataException($"Line {number}: invalid CSV quoting. Multiline cells are not supported.");
            else value.Append(c);
            if(cells.Count>256)throw new InvalidDataException($"Line {number}: too many columns.");
        }
        if(quoted)throw new InvalidDataException($"Line {number}: an unterminated quoted cell was found.");
        cells.Add(value.ToString());if(cells.Count>256)throw new InvalidDataException($"Line {number}: too many columns.");return cells;
    }
}
