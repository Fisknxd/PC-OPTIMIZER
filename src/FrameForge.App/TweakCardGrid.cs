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
        var children=InternalChildren.Cast<UIElement>().Where(c=>c.Visibility!=Visibility.Collapsed).ToArray();
        var columns=Columns(width);double height=0;
        for(int i=0;i<children.Length;i+=columns)
        {
            double rowHeight=0;
            for(int c=0;c<columns&&i+c<children.Length;c++)
            {
                var child=children[i+c];child.Measure(new(width/columns,double.PositiveInfinity));rowHeight=Math.Max(rowHeight,child.DesiredSize.Height);
            }
            height+=rowHeight;
        }
        return new(width,height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var children=InternalChildren.Cast<UIElement>().Where(c=>c.Visibility!=Visibility.Collapsed).ToArray();
        var columns=Columns(finalSize.Width);double top=0;
        for(int i=0;i<children.Length;i+=columns)
        {
            double rowHeight=0;for(int c=0;c<columns&&i+c<children.Length;c++)rowHeight=Math.Max(rowHeight,children[i+c].DesiredSize.Height);
            for(int c=0;c<columns&&i+c<children.Length;c++)children[i+c].Arrange(new Rect(c*finalSize.Width/columns,top,finalSize.Width/columns,rowHeight));
            top+=rowHeight;
        }
        return finalSize;
    }
}
