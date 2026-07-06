using System;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Shared drag-a-shape mechanics: anchor on pointer-down, live preview
  /// in the overlay pass (nothing touches the image until release), Shift
  /// constrains to square/circle. Outline uses the primary color at the
  /// shape border size, fill uses the secondary color — either can carry
  /// alpha, and alpha 0 (or border 0) simply omits that part. In-flight
  /// state is the anchor (null = idle).
  /// </summary>
  public abstract class ShapeToolBase : IOutlineTool
  {
    private PointF?     m_AnchorPos = null;
    private PointF      m_CurrentPos;



    public abstract string Name
    {
      get;
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



    /// <summary>Renders the final shape into the image.</summary>
    protected abstract void DrawShape( IOutlineRenderer Renderer, RectangleF ShapeRect,
                                       Color FillColor, Color OutlineColor, float BorderWidth );

    /// <summary>Draws the view-space preview of the same shape.</summary>
    protected abstract void DrawPreview( Graphics ViewGraphics, RectangleF ViewRect,
                                         Color FillColor, Color OutlineColor, float ViewBorderWidth );



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
      var previous = AffectedRegion( Context );
      m_CurrentPos = ImagePos;
      Context.InvalidateImageRegion( Rectangle.Union( previous, AffectedRegion( Context ) ) );
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return;
      }
      m_CurrentPos = ImagePos;
      var shapeRect = ShapeRect();
      var region = Rectangle.Intersect( AffectedRegion( Context ),
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      m_AnchorPos = null;
      if ( ( region.Width < 1 )
      ||   ( region.Height < 1 )
      ||   ( shapeRect.Width < 1 )
      ||   ( shapeRect.Height < 1 ) )
      {
        Context.InvalidateImageRegion( region );
        return;
      }

      var beforeCrop = Context.Image.Clone( region, Context.Image.PixelFormat );
      using ( var renderer = Context.CreateRenderer() )
      {
        DrawShape( renderer, shapeRect, Context.SecondaryColor, Context.PrimaryColor, Context.ShapeBorderSize );
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
      var region = AffectedRegion( Context );
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
      var shapeRect = ShapeRect();
      var topLeft = ImageToView( new PointF( shapeRect.X, shapeRect.Y ) );
      var viewRect = new RectangleF( topLeft.X, topLeft.Y,
                                     shapeRect.Width * ViewZoom, shapeRect.Height * ViewZoom );
      var previous = ViewGraphics.SmoothingMode;
      ViewGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      DrawPreview( ViewGraphics, viewRect, Context.SecondaryColor, Context.PrimaryColor,
                   Math.Max( 1.0f, Context.ShapeBorderSize * ViewZoom ) );
      ViewGraphics.SmoothingMode = previous;
    }



    /// <summary>
    /// Current shape rect in image space; Shift held = square/circle,
    /// growing toward the pointer's quadrant.
    /// </summary>
    private RectangleF ShapeRect()
    {
      var a = m_AnchorPos.Value;
      float dx = m_CurrentPos.X - a.X;
      float dy = m_CurrentPos.Y - a.Y;
      if ( ( Control.ModifierKeys & Keys.Shift ) == Keys.Shift )
      {
        float side = Math.Min( Math.Abs( dx ), Math.Abs( dy ) );
        dx = Math.Sign( dx ) * side;
        dy = Math.Sign( dy ) * side;
      }
      return RectangleF.FromLTRB( Math.Min( a.X, a.X + dx ), Math.Min( a.Y, a.Y + dy ),
                                  Math.Max( a.X, a.X + dx ), Math.Max( a.Y, a.Y + dy ) );
    }



    private Rectangle AffectedRegion( OutlineToolContext Context )
    {
      if ( !m_AnchorPos.HasValue )
      {
        return Rectangle.Empty;
      }
      var rect = ShapeRect();
      // Pens draw centered on the edge: half the border on each side, +2px AA.
      int margin = (int)Math.Ceiling( Context.ShapeBorderSize * 0.5f ) + 2;
      return Rectangle.FromLTRB(
        (int)Math.Floor( rect.Left ) - margin, (int)Math.Floor( rect.Top ) - margin,
        (int)Math.Ceiling( rect.Right ) + margin, (int)Math.Ceiling( rect.Bottom ) + margin );
    }
  }



  public class RectangleTool : ShapeToolBase
  {
    public override string Name
    {
      get
      {
        return "Rectangle";
      }
    }



    protected override void DrawShape( IOutlineRenderer Renderer, RectangleF ShapeRect,
                                       Color FillColor, Color OutlineColor, float BorderWidth )
    {
      Renderer.FillRectangle( ShapeRect, FillColor );
      Renderer.DrawRectangleOutline( ShapeRect, OutlineColor, BorderWidth );
    }



    protected override void DrawPreview( Graphics ViewGraphics, RectangleF ViewRect,
                                         Color FillColor, Color OutlineColor, float ViewBorderWidth )
    {
      if ( FillColor.A > 0 )
      {
        using ( var brush = new SolidBrush( FillColor ) )
        {
          ViewGraphics.FillRectangle( brush, ViewRect.X, ViewRect.Y, ViewRect.Width, ViewRect.Height );
        }
      }
      if ( OutlineColor.A > 0 )
      {
        using ( var pen = new Pen( OutlineColor, ViewBorderWidth ) )
        {
          ViewGraphics.DrawRectangle( pen, ViewRect.X, ViewRect.Y, ViewRect.Width, ViewRect.Height );
        }
      }
    }
  }



  public class EllipseTool : ShapeToolBase
  {
    public override string Name
    {
      get
      {
        return "Ellipse";
      }
    }



    protected override void DrawShape( IOutlineRenderer Renderer, RectangleF ShapeRect,
                                       Color FillColor, Color OutlineColor, float BorderWidth )
    {
      Renderer.FillEllipse( ShapeRect, FillColor );
      Renderer.DrawEllipseOutline( ShapeRect, OutlineColor, BorderWidth );
    }



    protected override void DrawPreview( Graphics ViewGraphics, RectangleF ViewRect,
                                         Color FillColor, Color OutlineColor, float ViewBorderWidth )
    {
      if ( FillColor.A > 0 )
      {
        using ( var brush = new SolidBrush( FillColor ) )
        {
          ViewGraphics.FillEllipse( brush, ViewRect );
        }
      }
      if ( OutlineColor.A > 0 )
      {
        using ( var pen = new Pen( OutlineColor, ViewBorderWidth ) )
        {
          ViewGraphics.DrawEllipse( pen, ViewRect );
        }
      }
    }
  }
}
