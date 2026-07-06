using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Round antialiased freehand brush. Alpha-correct strokes: the image is
  /// snapshotted once at stroke start, and every pointer move restores the
  /// affected region from that snapshot and re-renders the WHOLE
  /// accumulated path in one pass — overlapping segments of a
  /// semi-transparent stroke therefore never double-blend at the joints.
  /// The in-flight state is the point list itself (null = no stroke).
  /// </summary>
  public class BrushTool : IOutlineTool
  {
    private List<PointF>    m_StrokePoints = null;
    private Bitmap          m_PreStrokeImage = null;
    private Rectangle       m_DirtyRegion = Rectangle.Empty;



    public virtual string Name
    {
      get
      {
        return "Brush stroke";
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
        return m_StrokePoints != null;
      }
    }



    protected virtual Color StrokeColor( OutlineToolContext Context )
    {
      return Context.PrimaryColor;
    }



    protected virtual float StrokeWidth( OutlineToolContext Context )
    {
      return Context.BrushSize;
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      // Defense against a torn-away mouse capture: a leftover snapshot
      // would leak (and its stroke was already baked in) — reset first.
      if ( m_PreStrokeImage != null )
      {
        EndStroke();
      }
      m_StrokePoints = new List<PointF>() { ImagePos };
      m_PreStrokeImage = (Bitmap)Context.Image.Clone();
      m_DirtyRegion = StrokeBounds( Context );
      RedrawStroke( Context, m_DirtyRegion );
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_StrokePoints == null )
      {
        return;
      }
      var last = m_StrokePoints[m_StrokePoints.Count - 1];
      float dx = ImagePos.X - last.X;
      float dy = ImagePos.Y - last.Y;
      if ( dx * dx + dy * dy < 0.25f )
      {
        // Sub-half-pixel jitter — no visual difference, keep the path lean.
        return;
      }
      m_StrokePoints.Add( ImagePos );

      // Restore + redraw over the UNION of old and new bounds so shrinking
      // AA fringes from the previous pass are cleaned up too.
      var newBounds = StrokeBounds( Context );
      var affected = Rectangle.Union( m_DirtyRegion, newBounds );
      RedrawStroke( Context, affected );
      m_DirtyRegion = newBounds;
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_StrokePoints == null )
      {
        return;
      }
      // The release position is part of the stroke — without this, a fast
      // flick loses its final segment (the last move event lags the up).
      OnPointerMove( Context, ImagePos );
      var region = Rectangle.Intersect( m_DirtyRegion,
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      if ( !region.IsEmpty )
      {
        // Undo unit: the region's pre-stroke pixels. Ownership of the crop
        // transfers to the commit callee.
        var beforeCrop = m_PreStrokeImage.Clone( region, m_PreStrokeImage.PixelFormat );
        Context.CommitChange( region, beforeCrop, Name );
      }
      EndStroke();
    }



    public void Cancel( OutlineToolContext Context )
    {
      if ( m_StrokePoints == null )
      {
        return;
      }
      var region = Rectangle.Intersect( m_DirtyRegion,
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      if ( !region.IsEmpty )
      {
        using ( var renderer = Context.CreateRenderer() )
        {
          renderer.RestoreRegion( m_PreStrokeImage, region );
        }
        Context.InvalidateImageRegion( region );
      }
      EndStroke();
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      return false;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      // A half-finished stroke has no meaningful commit — discard it.
      Cancel( Context );
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      return StrokeWidth( Context ) * 0.5f + 2;
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( float.IsNaN( PointerImagePos.X ) )
      {
        return;
      }
      // Brush-size ghost: a thin circle of the exact stroke footprint at
      // the pointer, scaled with the view.
      float radius = StrokeWidth( Context ) * 0.5f * ViewZoom;
      if ( radius < 1.0f )
      {
        return;
      }
      var center = ImageToView( PointerImagePos );
      using ( var pen = new Pen( Color.FromArgb( 160, 255, 255, 255 ) ) )
      {
        var previous = ViewGraphics.SmoothingMode;
        ViewGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        ViewGraphics.DrawEllipse( pen, center.X - radius, center.Y - radius, radius * 2, radius * 2 );
        ViewGraphics.SmoothingMode = previous;
      }
    }



    private void RedrawStroke( OutlineToolContext Context, Rectangle AffectedRegion )
    {
      var region = Rectangle.Intersect( AffectedRegion,
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      if ( region.IsEmpty )
      {
        return;
      }
      using ( var renderer = Context.CreateRenderer() )
      {
        renderer.RestoreRegion( m_PreStrokeImage, region );
        renderer.DrawStrokePath( m_StrokePoints, StrokeColor( Context ), StrokeWidth( Context ) );
      }
      Context.InvalidateImageRegion( region );
    }



    private Rectangle StrokeBounds( OutlineToolContext Context )
    {
      float minX = float.MaxValue, minY = float.MaxValue;
      float maxX = float.MinValue, maxY = float.MinValue;
      foreach ( var p in m_StrokePoints )
      {
        minX = Math.Min( minX, p.X );
        minY = Math.Min( minY, p.Y );
        maxX = Math.Max( maxX, p.X );
        maxY = Math.Max( maxY, p.Y );
      }
      // Half the pen width on every side, +2px antialiasing fringe.
      int margin = (int)Math.Ceiling( StrokeWidth( Context ) * 0.5f ) + 2;
      return Rectangle.FromLTRB(
        (int)Math.Floor( minX ) - margin, (int)Math.Floor( minY ) - margin,
        (int)Math.Ceiling( maxX ) + margin, (int)Math.Ceiling( maxY ) + margin );
    }



    private void EndStroke()
    {
      m_StrokePoints = null;
      if ( m_PreStrokeImage != null )
      {
        m_PreStrokeImage.Dispose();
        m_PreStrokeImage = null;
      }
      m_DirtyRegion = Rectangle.Empty;
    }
  }



  /// <summary>
  /// The eraser is a brush that paints the canvas background color at its
  /// own size — on the outline's opaque black canvas, erasing IS painting
  /// black.
  /// </summary>
  public class EraserTool : BrushTool
  {
    public override string Name
    {
      get
      {
        return "Erase";
      }
    }



    protected override Color StrokeColor( OutlineToolContext Context )
    {
      return Context.EraseColor;
    }



    protected override float StrokeWidth( OutlineToolContext Context )
    {
      return Context.EraserSize;
    }
  }
}
