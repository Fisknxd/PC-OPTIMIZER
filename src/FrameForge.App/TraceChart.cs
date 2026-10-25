using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FrameForge.Core;

namespace FrameForge.App;

/// <summary>Resize-aware chart with real units and peak-preserving downsampling.</summary>
public sealed class TraceChart : FrameworkElement
{
    double[] values=[],secondary=[];
    double? fixedMaximum;
    string unit="ms",caption="Frames →";
    public TraceChart() { Height=210; MinWidth=200; SizeChanged+=(_,_)=>InvalidateVisual(); }
    public void SetData(IEnumerable<double> primary,IEnumerable<double>? other=null,double? maximum=null,string units="ms",string xCaption="Frames →")
    {
        values=primary.ToArray(); secondary=other?.ToArray()??[]; fixedMaximum=maximum; unit=units; caption=xCaption; InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var background=new SolidColorBrush(Color.FromRgb(11,18,32)); var muted=new SolidColorBrush(Color.FromRgb(189,204,224));
        dc.DrawRoundedRectangle(background,null,new Rect(RenderSize),8,8);
        var plot=new Rect(62,18,Math.Max(1,ActualWidth-84),Math.Max(1,ActualHeight-56));
        var maximum=fixedMaximum??Math.Max(1,values.DefaultIfEmpty(0).Max()*1.1);
        void Label(string value,double x,double y)=>dc.DrawText(new FormattedText(value,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,muted,VisualTreeHelper.GetDpi(this).PixelsPerDip),new Point(x,y));
        for(int i=0;i<=4;i++)
        {
            var y=plot.Top+plot.Height*i/4;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(38,52,75)),1),new Point(plot.Left,y),new Point(plot.Right,y));
            Label($"{maximum*(4-i)/4:0.#}{unit}",8,y-7);
        }
        void Series(double[] data,string color)
        {
            if(data.Length==0) return;
            var points=Benchmark.Peaks(data,Math.Max(2,(int)plot.Width));
            var geometry=new StreamGeometry();
            using(var context=geometry.Open())
            {
                Point P(int i)=>new(plot.Left+i*plot.Width/Math.Max(1,points.Length-1),plot.Bottom-Math.Clamp(points[i]/maximum,0,1)*plot.Height);
                context.BeginFigure(P(0),false,false);
                for(int i=1;i<points.Length;i++) context.LineTo(P(i),true,false);
                if(points.Length==1) context.LineTo(new Point(plot.Right,P(0).Y),true,false);
            }
            geometry.Freeze(); dc.DrawGeometry(null,new Pen((SolidColorBrush)new BrushConverter().ConvertFromString(color)!,1.7),geometry);
        }
        Series(values,"#5EE0BA"); Series(secondary,"#99B7FF");
        Label(values.Length>0?caption:"No measurements yet",plot.Left,plot.Bottom+12);
        if(unit=="ms"&&maximum>16.667)
        {
            var pen=new Pen(muted,.7) { DashStyle=DashStyles.Dash };
            var y=plot.Bottom-16.667/maximum*plot.Height;
            dc.DrawLine(pen,new Point(plot.Left,y),new Point(plot.Right,y));
            Label("60 FPS budget · 16.67 ms",Math.Max(plot.Left,plot.Right-156),Math.Max(plot.Top,y-17));
        }
    }
}
