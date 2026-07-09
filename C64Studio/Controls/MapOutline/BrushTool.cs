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

    // Pressure (pen) stroke compositor for THIS stroke; null = a mouse stroke
    // (constant width, rendered by the classic whole-path path below). Its
    // existence IS the intrinsic "this is a pressure stroke" state — not a
    // behavior-gating flag.
    private PressureStroke  m_Pressure = null;
    private PointF          m_LastPressurePos;



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



    /// <summary>Whether pressure may modulate opacity (brush: yes; eraser: no).</summary>
    protected virtual bool PressureAffectsAlpha( OutlineToolContext Context )
    {
      return true;
    }



    /// <summary>
    /// How pressure maps to width. Brush (false): the configured size is the
    /// CEILING — a light touch thins toward PenMinWidth, full pressure reaches
    /// the configured size. Eraser (true): the configured size is the FLOOR —
    /// pressure only ENLARGES it (never smaller), and since opacity is
    /// meaningless for erasing, pressure always drives size regardless of the
    /// Size/Opacity target.
    /// </summary>
    protected virtual bool PressureGrowsFromBase( OutlineToolContext Context )
    {
      return false;
    }



    /// <summary>Maps the live pen pressure to this point's width and effective alpha.</summary>
    private void PressureWidthAlpha( OutlineToolContext Context, out float Width, out float Alpha )
    {
      float p = Math.Max( 0f, Math.Min( 1f, Context.PenPressure ) );
      float gamma = Math.Max( 0.05f, Context.PenPressureGamma );
      float pg = (float)Math.Pow( p, gamma );

      bool affectsSize  = ( Context.PenPressureTarget == 0 ) || ( Context.PenPressureTarget == 2 );
      bool affectsAlpha = ( ( Context.PenPressureTarget == 1 ) || ( Context.PenPressureTarget == 2 ) )
                       && PressureAffectsAlpha( Context );

      float baseWidth = StrokeWidth( Context );
      if ( PressureGrowsFromBase( Context ) )
      {
        // Eraser: configured size is the floor; harder press only enlarges it
        // (up to 2× at full pressure). Sensitivity (gamma) shapes the curve.
        Width = Math.Min( 512f, baseWidth * ( 1.0f + pg ) );
      }
      else
      {
        float minW = Math.Min( baseWidth, Math.Max( 0.5f, Context.PenMinWidth ) );
        Width = affectsSize ? ( minW + ( baseWidth - minW ) * pg ) : baseWidth;
      }

      float inkA = StrokeColor( Context ).A / 255.0f;
      float minA = Math.Max( 0f, Math.Min( 1f, Context.PenMinAlpha ) );
      float alphaFactor = affectsAlpha ? ( minA + ( 1.0f - minA ) * pg ) : 1.0f;
      Alpha = inkA * alphaFactor;
    }



    /// <summary>
    /// The width the CURRENT pressure would actually paint/erase — so the ghost
    /// ring matches the real footprint. Base size while hovering (pressure 0);
    /// grows with pressure during a pen stroke (e.g. the eraser ring expands as
    /// you press). Falls back to the plain configured width when pressure is off.
    /// </summary>
    protected float EffectiveWidth( OutlineToolContext Context )
    {
      if ( ( Context.PenActive )
      &&   ( Context.PenPressureEnabled ) )
      {
        float w, a;
        PressureWidthAlpha( Context, out w, out a );
        return w;
      }
      return StrokeWidth( Context );
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

      if ( ( Context.PenActive )
      &&   ( Context.PenPressureEnabled ) )
      {
        // Pressure stroke: incremental variable-width compositing (fast, and
        // the pre-stroke clone above is its restore/undo source).
        m_Pressure = new PressureStroke();
        m_Pressure.Begin( Context.Image, m_PreStrokeImage, StrokeColor( Context ) );
        m_LastPressurePos = ImagePos;
        float w, a;
        PressureWidthAlpha( Context, out w, out a );
        var r = m_Pressure.AddPoint( ImagePos, w, a );
        if ( !r.IsEmpty )
        {
          Context.InvalidateImageRegion( r );
        }
        return;
      }

      m_DirtyRegion = StrokeBounds( Context );
      RedrawStroke( Context, m_DirtyRegion );
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_Pressure != null )
      {
        float pdx = ImagePos.X - m_LastPressurePos.X;
        float pdy = ImagePos.Y - m_LastPressurePos.Y;
        if ( pdx * pdx + pdy * pdy < 0.25f )
        {
          // Sub-half-pixel jitter — keep the trail lean.
          return;
        }
        m_LastPressurePos = ImagePos;
        float w, a;
        PressureWidthAlpha( Context, out w, out a );
        var r = m_Pressure.AddPoint( ImagePos, w, a );
        if ( !r.IsEmpty )
        {
          Context.InvalidateImageRegion( r );
        }
        return;
      }
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
      if ( m_Pressure != null )
      {
        // The release position is part of the stroke — and its capsule must be
        // invalidated (a fast flick's mouse-up lags the last move, so this final
        // dab is new painted area the per-move path never invalidated).
        float w, a;
        PressureWidthAlpha( Context, out w, out a );
        var lastDab = m_Pressure.AddPoint( ImagePos, w, a );
        if ( !lastDab.IsEmpty )
        {
          Context.InvalidateImageRegion( lastDab );
        }
        var pregion = Rectangle.Intersect( m_Pressure.UnionDirty,
          new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
        if ( !pregion.IsEmpty )
        {
          // The image already holds the composited stroke; undo unit is the
          // pre-stroke pixels of the affected region.
          var pbefore = m_PreStrokeImage.Clone( pregion, m_PreStrokeImage.PixelFormat );
          Context.CommitChange( pregion, pbefore, Name );
        }
        EndStroke();
        return;
      }
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
      if ( m_Pressure != null )
      {
        m_Pressure.Cancel();
        Context.InvalidateImageRegion( m_Pressure.UnionDirty );
        EndStroke();
        return;
      }
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
      // Pressure-adjusted so the invalidation region grows with the ring and
      // no trail is left when a hard press enlarges the footprint.
      return EffectiveWidth( Context ) * 0.5f + 2;
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( float.IsNaN( PointerImagePos.X ) )
      {
        return;
      }
      // Ghost: a thin circle of the exact CURRENT stroke footprint at the
      // pointer (pressure-adjusted), scaled with the view.
      float radius = EffectiveWidth( Context ) * 0.5f * ViewZoom;
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
      // Dispose the compositor first (it only nulls its refs — the snapshot it
      // borrowed is m_PreStrokeImage, disposed just below).
      if ( m_Pressure != null )
      {
        m_Pressure.Dispose();
        m_Pressure = null;
      }
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



    // Erasing is opaque (paints the canvas background) — pressure scales the
    // eraser WIDTH only, never its opacity.
    protected override bool PressureAffectsAlpha( OutlineToolContext Context )
    {
      return false;
    }



    // The configured Eraser size is the FLOOR — pressure only makes it bigger.
    protected override bool PressureGrowsFromBase( OutlineToolContext Context )
    {
      return true;
    }
  }
}
