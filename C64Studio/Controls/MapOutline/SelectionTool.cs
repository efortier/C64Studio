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

    // Move gesture: press-and-hold INSIDE a committed selection drags the
    // raster under it to a new position. The selected pixels are LIFTED (a
    // clone) at press; the backing image is not touched until release, so the
    // drag is a pure preview like the marquee. On release the source is
    // vacated to the background and the lifted pixels are stamped at the
    // destination in ONE undo step. Move affects ONLY the flattened raster —
    // persistent text objects float above it and are never touched.
    // m_MoveLifted != null == a move is in flight.
    private Bitmap      m_MoveLifted = null;
    private Rectangle   m_MoveSourceRect;
    private PointF      m_MoveGrabPos;
    private PointF      m_MoveCurrentPos;

    /// <summary>True while a selection-move drag is in flight.</summary>
    public bool IsMovingSelection
    {
      get
      {
        return m_MoveLifted != null;
      }
    }

    /// <summary>
    /// The destination rect (image space) the in-flight move currently points
    /// at; null when no move is active. The canvas reads this so the
    /// committed-selection marquee tracks the moved content during the drag.
    /// </summary>
    public Rectangle? SelectionMoveDestRect
    {
      get
      {
        if ( m_MoveLifted == null )
        {
          return null;
        }
        var offset = MoveOffset();
        return new Rectangle( m_MoveSourceRect.X + offset.X, m_MoveSourceRect.Y + offset.Y,
                              m_MoveSourceRect.Width, m_MoveSourceRect.Height );
      }
    }



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
        return ( m_AnchorPos.HasValue )
            || ( m_MoveLifted != null );
      }
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      // Press-and-hold INSIDE the committed selection grabs the raster under
      // it for a move; a press anywhere else starts a fresh marquee.
      if ( ( Context.SelectionRect.HasValue )
      &&   ( Context.Image != null )
      &&   ( ContainsFloat( Context.SelectionRect.Value, ImagePos ) ) )
      {
        m_MoveSourceRect = Context.SelectionRect.Value;
        m_MoveLifted     = Context.Image.Clone( m_MoveSourceRect, Context.Image.PixelFormat );
        m_MoveGrabPos    = ImagePos;
        m_MoveCurrentPos = ImagePos;
        Context.InvalidateImageRegion( m_MoveSourceRect );
        return;
      }
      // A fresh drag replaces the previous selection.
      Context.SetSelectionRect( null );
      m_AnchorPos = ImagePos;
      m_CurrentPos = ImagePos;
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_MoveLifted != null )
      {
        var oldDest = SelectionMoveDestRect.Value;
        m_MoveCurrentPos = ImagePos;
        var newDest = SelectionMoveDestRect.Value;
        // The vacated source stays filled with background throughout, so the
        // union of source + old dest + new dest covers everything that moved.
        var moveInvalid = Rectangle.Union( Rectangle.Union( m_MoveSourceRect, oldDest ), newDest );
        moveInvalid.Inflate( 2, 2 );
        Context.InvalidateImageRegion( moveInvalid );
        return;
      }
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
      if ( m_MoveLifted != null )
      {
        m_MoveCurrentPos = ImagePos;
        CommitMove( Context );
        return;
      }
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
      if ( m_MoveLifted != null )
      {
        // Escape / RMB during a move: drop it, leaving the selection and the
        // image exactly as they were (nothing was mutated yet).
        DropMove( Context );
        return;
      }
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
      // Drop an in-flight marquee drag or move (both are preview-only drags —
      // discard, don't commit); the canvas clears the committed selection
      // itself on tool switch (operations-only contract).
      DropMove( Context );
      m_AnchorPos = null;
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      return 0;
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( m_MoveLifted != null )
      {
        // Move preview: vacate the source to the opaque background, then float
        // the lifted pixels at the destination (nearest-neighbor, crisp at any
        // zoom). The marquee at the destination is drawn by the canvas, which
        // owns the committed-selection marquee.
        var srcTopLeft = ImageToView( new PointF( m_MoveSourceRect.X, m_MoveSourceRect.Y ) );
        var srcView = new RectangleF( srcTopLeft.X, srcTopLeft.Y,
                                      m_MoveSourceRect.Width * ViewZoom, m_MoveSourceRect.Height * ViewZoom );
        using ( var background = new SolidBrush( Color.FromArgb( 255, Context.EraseColor ) ) )
        {
          ViewGraphics.FillRectangle( background, srcView.X, srcView.Y, srcView.Width, srcView.Height );
        }
        var dest = SelectionMoveDestRect.Value;
        var destTopLeft = ImageToView( new PointF( dest.X, dest.Y ) );
        var destView = new RectangleF( destTopLeft.X, destTopLeft.Y,
                                       dest.Width * ViewZoom, dest.Height * ViewZoom );
        var previousInterpolation = ViewGraphics.InterpolationMode;
        var previousOffset = ViewGraphics.PixelOffsetMode;
        ViewGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        ViewGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        ViewGraphics.DrawImage( m_MoveLifted,
          new Rectangle( (int)destView.X, (int)destView.Y, (int)destView.Width, (int)destView.Height ),
          0, 0, m_MoveLifted.Width, m_MoveLifted.Height, GraphicsUnit.Pixel );
        ViewGraphics.InterpolationMode = previousInterpolation;
        ViewGraphics.PixelOffsetMode = previousOffset;
        return;
      }
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



    /// <summary>Whole-pixel drag offset from the grab point to the pointer.</summary>
    private Point MoveOffset()
    {
      return new Point( (int)Math.Round( m_MoveCurrentPos.X - m_MoveGrabPos.X ),
                        (int)Math.Round( m_MoveCurrentPos.Y - m_MoveGrabPos.Y ) );
    }



    /// <summary>Float-precise containment ( [X,Right) × [Y,Bottom) ).</summary>
    private static bool ContainsFloat( Rectangle Rect, PointF Pos )
    {
      return ( Pos.X >= Rect.X )
          && ( Pos.X <  Rect.Right )
          && ( Pos.Y >= Rect.Y )
          && ( Pos.Y <  Rect.Bottom );
    }



    /// <summary>
    /// Applies the finished move by delegating to the canvas, which vacates the
    /// source, stamps the lifted pixels at the destination, and records ONE
    /// undo entry (carrying the selection transition). A zero drag (dropped
    /// where it was grabbed) changes nothing. The preview is torn down BEFORE
    /// the canvas repaints so the stale float never draws over the result.
    /// </summary>
    private void CommitMove( OutlineToolContext Context )
    {
      var offset = MoveOffset();
      if ( ( Context.Image == null )
      ||   ( Context.MoveSelectionRegion == null )
      ||   ( ( offset.X == 0 ) && ( offset.Y == 0 ) ) )
      {
        // Dropped in place (or no image): keep the selection, just clear the
        // preview.
        DropMove( Context );
        return;
      }
      var source = m_MoveSourceRect;
      var dest   = new Rectangle( source.X + offset.X, source.Y + offset.Y, source.Width, source.Height );
      m_MoveLifted.Dispose();
      m_MoveLifted = null;
      Context.MoveSelectionRegion( source, dest );
    }



    /// <summary>Discards an in-flight move without touching the image.</summary>
    private void DropMove( OutlineToolContext Context )
    {
      if ( m_MoveLifted == null )
      {
        return;
      }
      var invalid = m_MoveSourceRect;
      var dest = SelectionMoveDestRect;
      m_MoveLifted.Dispose();
      m_MoveLifted = null;
      if ( dest.HasValue )
      {
        invalid = Rectangle.Union( invalid, dest.Value );
      }
      invalid.Inflate( 2, 2 );
      Context.InvalidateImageRegion( invalid );
    }
  }
}
