using System;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Drag a rectangle, release to wipe it to the canvas background color.
  /// Nothing touches the image until release — the drag is pure view-space
  /// preview. In-flight state is the anchor point (null = idle).
  /// </summary>
  public class RectEraseTool : IOutlineTool
  {
    private PointF?     m_AnchorPos = null;
    private PointF      m_CurrentPos;



    public string Name
    {
      get
      {
        return "Rectangle erase";
      }
    }



    public Cursor Cursor
    {
      get
      {
        return Cursors.Cross;
      }
    }



    public bool HasPendingEdit
    {
      get
      {
        return m_AnchorPos.HasValue;
      }
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      m_AnchorPos = ImagePos;
      m_CurrentPos = ImagePos;
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      var previous = DragRegion();
      m_CurrentPos = ImagePos;
      // Only the preview overlay changes — invalidate old + new outline.
      Context.InvalidateImageRegion( Rectangle.Union( previous, DragRegion() ) );
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      m_CurrentPos = ImagePos;
      // The FILL is the exact dragged rect (matching the preview); only
      // the snapshot/invalidate region carries the safety padding.
      var a = m_AnchorPos.Value;
      var b = m_CurrentPos;
      var fillRect = RectangleF.FromLTRB( Math.Min( a.X, b.X ), Math.Min( a.Y, b.Y ),
                                          Math.Max( a.X, b.X ), Math.Max( a.Y, b.Y ) );
      var region = Rectangle.Intersect( DragRegion(),
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      m_AnchorPos = null;
      if ( ( region.Width < 1 )
      ||   ( region.Height < 1 )
      ||   ( fillRect.Width < 1 )
      ||   ( fillRect.Height < 1 ) )
      {
        Context.InvalidateImageRegion( region );
        return;
      }

      var beforeCrop = Context.Image.Clone( region, Context.Image.PixelFormat );
      using ( var renderer = Context.CreateRenderer() )
      {
        // Opaque background fill — alpha plays no role in erasing.
        renderer.FillRectangle( fillRect, Color.FromArgb( 255, Context.EraseColor ) );
      }
      Context.InvalidateImageRegion( region );
      Context.CommitChange( region, beforeCrop, Name );
    }



    public void Cancel( OutlineToolContext Context )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      var region = DragRegion();
      m_AnchorPos = null;
      Context.InvalidateImageRegion( region );
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      return false;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      Cancel( Context );
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      return 0;
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      var a = ImageToView( m_AnchorPos.Value );
      var b = ImageToView( m_CurrentPos );
      var rect = RectangleF.FromLTRB( Math.Min( a.X, b.X ), Math.Min( a.Y, b.Y ),
                                      Math.Max( a.X, b.X ), Math.Max( a.Y, b.Y ) );
      // Translucent wipe hint + dashed border.
      using ( var brush = new SolidBrush( Color.FromArgb( 70, 255, 80, 80 ) ) )
      {
        ViewGraphics.FillRectangle( brush, rect.X, rect.Y, rect.Width, rect.Height );
      }
      using ( var pen = new Pen( Color.FromArgb( 200, 255, 120, 120 ) ) )
      {
        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        ViewGraphics.DrawRectangle( pen, rect.X, rect.Y, rect.Width, rect.Height );
      }
    }



    /// <summary>Current drag rect in image space, pixel-snapped outward.</summary>
    private Rectangle DragRegion()
    {
      if ( !m_AnchorPos.HasValue )
      {
        return Rectangle.Empty;
      }
      var a = m_AnchorPos.Value;
      var b = m_CurrentPos;
      return Rectangle.FromLTRB(
        (int)Math.Floor( Math.Min( a.X, b.X ) ) - 1, (int)Math.Floor( Math.Min( a.Y, b.Y ) ) - 1,
        (int)Math.Ceiling( Math.Max( a.X, b.X ) ) + 1, (int)Math.Ceiling( Math.Max( a.Y, b.Y ) ) + 1 );
    }
  }
}
