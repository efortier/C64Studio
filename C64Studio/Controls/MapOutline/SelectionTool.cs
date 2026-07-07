using System;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Rectangular marquee selection for the outline canvas. The selection
  /// itself lives on the CANVAS (Context.SelectionRect) so the toolbar
  /// operations (crop, and the editor's Delete/Ctrl+C/X/V handling) can
  /// read it; this tool only drags the marquee out. Selection is
  /// operations-only by design — it never clips the drawing tools, and
  /// the canvas clears it when the user switches to another tool.
  ///
  /// Cancel is two-stage like the text tool: Escape during a drag drops
  /// just the drag; Escape with a committed selection clears it.
  /// </summary>
  public class SelectionTool : IOutlineTool
  {
    private PointF?     m_AnchorPos = null;
    private PointF      m_CurrentPos;



    public string Name
    {
      get
      {
        return "Select";
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
      // A fresh drag replaces the previous selection.
      Context.SetSelectionRect( null );
      m_AnchorPos = ImagePos;
      m_CurrentPos = ImagePos;
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      var previous = m_CurrentPos;
      m_CurrentPos = ImagePos;
      // Repaint the union of the old and new marquee extents (+ margin).
      var a = m_AnchorPos.Value;
      var invalid = Rectangle.FromLTRB(
        (int)Math.Floor( Math.Min( a.X, Math.Min( previous.X, ImagePos.X ) ) ) - 2,
        (int)Math.Floor( Math.Min( a.Y, Math.Min( previous.Y, ImagePos.Y ) ) ) - 2,
        (int)Math.Ceiling( Math.Max( a.X, Math.Max( previous.X, ImagePos.X ) ) ) + 2,
        (int)Math.Ceiling( Math.Max( a.Y, Math.Max( previous.Y, ImagePos.Y ) ) ) + 2 );
      Context.InvalidateImageRegion( invalid );
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      m_CurrentPos = ImagePos;
      var a = m_AnchorPos.Value;
      m_AnchorPos = null;
      var rect = Rectangle.FromLTRB(
        (int)Math.Floor( Math.Min( a.X, ImagePos.X ) ),
        (int)Math.Floor( Math.Min( a.Y, ImagePos.Y ) ),
        (int)Math.Ceiling( Math.Max( a.X, ImagePos.X ) ),
        (int)Math.Ceiling( Math.Max( a.Y, ImagePos.Y ) ) );
      // SetSelectionRect clamps to the image and nulls degenerate rects.
      Context.SetSelectionRect( rect );
    }



    public void Cancel( OutlineToolContext Context )
    {
      if ( m_AnchorPos.HasValue )
      {
        // First Escape mid-drag: drop only the drag.
        var a = m_AnchorPos.Value;
        m_AnchorPos = null;
        Context.InvalidateImageRegion( Rectangle.FromLTRB(
          (int)Math.Floor( Math.Min( a.X, m_CurrentPos.X ) ) - 2,
          (int)Math.Floor( Math.Min( a.Y, m_CurrentPos.Y ) ) - 2,
          (int)Math.Ceiling( Math.Max( a.X, m_CurrentPos.X ) ) + 2,
          (int)Math.Ceiling( Math.Max( a.Y, m_CurrentPos.Y ) ) + 2 ) );
        return;
      }
      Context.SetSelectionRect( null );
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      return false;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      // Drop an in-flight drag; the canvas clears the committed selection
      // itself on tool switch (operations-only contract).
      m_AnchorPos = null;
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
      // In-flight marquee: same black+white double-dash the committed
      // selection uses, so the look doesn't change on release.
      var a = ImageToView( m_AnchorPos.Value );
      var b = ImageToView( m_CurrentPos );
      var rect = RectangleF.FromLTRB( Math.Min( a.X, b.X ), Math.Min( a.Y, b.Y ),
                                      Math.Max( a.X, b.X ), Math.Max( a.Y, b.Y ) );
      MapOutlineCanvas.DrawMarquee( ViewGraphics, rect );
    }
  }
}
