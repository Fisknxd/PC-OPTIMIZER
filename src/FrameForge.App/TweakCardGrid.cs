using System.Windows;
using System.Windows.Controls;
namespace FrameForge.App;

// Reflows on layout changes without rebuilding cards or rereading Windows settings.
internal sealed class TweakCardGrid:Panel
{
    static int Columns(double width)=>width>=900?2:1;
    protected override Size MeasureOverride(Size availableSize)
    {
        var width=double.IsFinite(availableSize.Width)?availableSize.Width:650;
        var columns=Columns(width);double height=0;
        for(int i=0;i<InternalChildren.Count;i+=columns)
        {
            double rowHeight=0;
            for(int c=0;c<columns&&i+c<InternalChildren.Count;c++)
            {
                var child=InternalChildren[i+c];child.Measure(new(width/columns,double.PositiveInfinity));rowHeight=Math.Max(rowHeight,child.DesiredSize.Height);
            }
            height+=rowHeight;
        }
        return new(width,height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns=Columns(finalSize.Width);double top=0;
        for(int i=0;i<InternalChildren.Count;i+=columns)
        {
            double rowHeight=0;for(int c=0;c<columns&&i+c<InternalChildren.Count;c++)rowHeight=Math.Max(rowHeight,InternalChildren[i+c].DesiredSize.Height);
            for(int c=0;c<columns&&i+c<InternalChildren.Count;c++)InternalChildren[i+c].Arrange(new Rect(c*finalSize.Width/columns,top,finalSize.Width/columns,rowHeight));
            top+=rowHeight;
        }
        return finalSize;
    }
}
