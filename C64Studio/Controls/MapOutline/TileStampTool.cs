using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Stamps the armed tile image (Context.StampImage, pre-rendered by the
  /// editor from the selected map tile) onto the canvas at the selected
  /// magnification — the level-mockup tool. Click stamps once, centered
  /// under the cursor; keep the button down and drag to lay a trail (a
  /// new stamp drops whenever the pointer has travelled one stamp-size
  /// from the previous one). Every stamp is its own undo entry. Scaling
  /// is nearest-neighbor: 2× of a 16×16 tile is chunky 32×32 pixels, not
  /// a blur. Nothing is pending between clicks (stamps commit
  /// instantly), so Cancel/OnDeactivate have nothing to unwind.
  /// </summary>
  public class TileStampTool : IOutlineTool
  {
    // Position of the last stamp while the button is held (drag trail).
    private PointF?     m_LastStampPos = null;



    public string Name
    {
      get
      {
        return "Tile stamp";
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
        return false;
      }
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      if ( Context.StampImage == null )
      {
        return;
      }
      Stamp( Context, ImagePos );
      m_LastStampPos = ImagePos;
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( ( !m_LastStampPos.HasValue )
      ||   ( Context.StampImage == null ) )
      {
        return;
      }
      float stampWidth = Context.StampImage.Width * Context.StampScale;
      float stampHeight = Context.StampImage.Height * Context.StampScale;
      if ( ( Math.Abs( ImagePos.X - m_LastStampPos.Value.X ) >= stampWidth )
      ||   ( Math.Abs( ImagePos.Y - m_LastStampPos.Value.Y ) >= stampHeight ) )
      {
        Stamp( Context, ImagePos );
        m_LastStampPos = ImagePos;
      }
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      m_LastStampPos = null;
    }



    public void Cancel( OutlineToolContext Context )
    {
      m_LastStampPos = null;
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      return false;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      m_LastStampPos = null;
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      if ( Context.StampImage == null )
      {
        return 0;
      }
      return Math.Max( Context.StampImage.Width, Context.StampImage.Height )
           * Math.Max( 1.0f, Context.StampScale ) * 0.5f + 2;
    }



    /// <summary>Stamp rect centered on Pos, snapped to whole pixels for crisp NN edges.</summary>
    private static RectangleF StampRect( OutlineToolContext Context, PointF Pos )
    {
      float width = Context.StampImage.Width * Context.StampScale;
      float height = Context.StampImage.Height * Context.StampScale;
      return new RectangleF( (float)Math.Round( Pos.X - width * 0.5f ),
                             (float)Math.Round( Pos.Y - height * 0.5f ),
                             width, height );
    }



    private void Stamp( OutlineToolContext Context, PointF ImagePos )
    {
      var dest = StampRect( Context, ImagePos );
      var region = Rectangle.Intersect(
        Rectangle.FromLTRB( (int)Math.Floor( dest.Left ) - 1, (int)Math.Floor( dest.Top ) - 1,
                            (int)Math.Ceiling( dest.Right ) + 1, (int)Math.Ceiling( dest.Bottom ) + 1 ),
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      if ( ( region.Width < 1 )
      ||   ( region.Height < 1 ) )
      {
        return;
      }

      var beforeCrop = Context.Image.Clone( region, Context.Image.PixelFormat );
      using ( var renderer = Context.CreateRenderer() )
      {
        renderer.DrawImageNearest( Context.StampImage, dest );
      }
      Context.InvalidateImageRegion( region );
      Context.CommitChange( region, beforeCrop, Name );
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( ( Context.StampImage == null )
      ||   ( float.IsNaN( PointerImagePos.X ) ) )
      {
        return;
      }
      // Ghost of the stamp under the cursor: half transparent, NN-scaled,
      // with a thin frame so its footprint reads against busy content.
      var destImage = StampRect( Context, PointerImagePos );
      var topLeft = ImageToView( new PointF( destImage.X, destImage.Y ) );
      var destView = new RectangleF( topLeft.X, topLeft.Y,
                                     destImage.Width * ViewZoom, destImage.Height * ViewZoom );

      var previousInterpolation = ViewGraphics.InterpolationMode;
      var previousOffset = ViewGraphics.PixelOffsetMode;
      ViewGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
      ViewGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
      using ( var attributes = new ImageAttributes() )
      {
        var halfAlpha = new ColorMatrix();
        halfAlpha.Matrix33 = 0.55f;
        attributes.SetColorMatrix( halfAlpha );
        ViewGraphics.DrawImage( Context.StampImage,
          new Rectangle( (int)destView.X, (int)destView.Y, (int)destView.Width, (int)destView.Height ),
          0, 0, Context.StampImage.Width, Context.StampImage.Height,
          GraphicsUnit.Pixel, attributes );
      }
      ViewGraphics.InterpolationMode = previousInterpolation;
      ViewGraphics.PixelOffsetMode = previousOffset;
      using ( var framePen = new Pen( Color.FromArgb( 150, 255, 255, 255 ) ) )
      {
        framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        ViewGraphics.DrawRectangle( framePen, destView.X, destView.Y, destView.Width, destView.Height );
      }
    }
  }
}
