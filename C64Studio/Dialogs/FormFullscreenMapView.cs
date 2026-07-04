using System;
using System.Windows.Forms;
using RetroDevStudio.CustomRenderer.DisplayFilters;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Borderless fullscreen, view-only presentation of one map: the map is
  /// integer-zoomed to the largest whole multiple that fits, centered, and
  /// drawn over a solid fill of the map's background color. A configurable
  /// number of C64 character rows ("reserved top lines" — the game's HUD
  /// area) is treated as sitting above the map, so the map lands exactly
  /// where it would on the real game screen.
  ///
  /// Sprites animate exactly as in the editor (the caller supplies compose
  /// and advance delegates over the SHARED instance state and pauses its
  /// own animation timer for the modal duration). The CRT display-filter
  /// pipeline is applied when the caller passes it (following the editor's
  /// "Filter enabled" master switch). Escape closes; there is no other UI —
  /// no chrome, no taskbar, no cursor.
  ///
  /// Built entirely in code — no Designer.cs — because the form has zero
  /// child controls; it is a single paint surface (same precedent as
  /// DlgDisplayFilters).
  /// </summary>
  public class FormFullscreenMapView : Form
  {
    private readonly GR.Image.FastImage   m_MapImage;         // full map 1:1, NOT owned
    private readonly GR.Image.FastImage   m_ComposeBuffer;    // map + sprites (owned, null when no sprites)
    private readonly GR.Image.FastImage   m_ScreenBuffer;     // scaled + filtered (owned)
    private readonly uint                 m_BackgroundARGB;
    private readonly Action<GR.Image.IImage>  m_ComposeSprites;   // null = static map
    private readonly Func<int, bool>      m_AdvanceFrames;        // ms → any frame changed
    private readonly FilterPipeline       m_Filters;              // null = no filtering

    private readonly int                  m_Scale;            // integer zoom, >= 1
    private readonly int                  m_MapDestX;         // may be negative (map larger than screen → clipped)
    private readonly int                  m_MapDestY;
    // Sprite overhang margins: sprites near the map's edges extend past the
    // map rect (on real hardware they'd overhang into the border). The
    // compose buffer is padded by these so the overhang isn't clipped; the
    // caller's compose delegate draws with map (0,0) at (PadLeft, PadTop).
    private readonly int                  m_ComposePadLeft;
    private readonly int                  m_ComposePadTop;

    private Timer                         m_AnimTimer;
    private bool                          m_FilterNeedsRepaint;



    public FormFullscreenMapView( Control ScreenAnchor,
                                  GR.Image.FastImage MapImage,
                                  uint BackgroundARGB,
                                  int ReservedTopLines,
                                  Action<GR.Image.IImage> ComposeSprites,
                                  Func<int, bool> AdvanceFrames,
                                  FilterPipeline Filters,
                                  int ComposePadLeft = 0,
                                  int ComposePadTop = 0,
                                  int ComposePadRight = 0,
                                  int ComposePadBottom = 0 )
    {
      m_MapImage        = MapImage;
      m_BackgroundARGB  = 0xff000000 | BackgroundARGB;
      m_ComposeSprites  = ComposeSprites;
      m_AdvanceFrames   = AdvanceFrames;
      m_Filters         = Filters;
      m_ComposePadLeft  = Math.Max( 0, ComposePadLeft );
      m_ComposePadTop   = Math.Max( 0, ComposePadTop );
      int composePadRight  = Math.Max( 0, ComposePadRight );
      int composePadBottom = Math.Max( 0, ComposePadBottom );

      FormBorderStyle = FormBorderStyle.None;
      StartPosition   = FormStartPosition.Manual;
      // The full monitor the editor lives on — bounds cover the taskbar,
      // TopMost keeps us above it.
      Bounds        = Screen.FromControl( ScreenAnchor ).Bounds;
      TopMost       = true;
      ShowInTaskbar = false;
      KeyPreview    = true;

      // We repaint the entire surface each frame via a raw blit — skip the
      // background erase pass entirely to avoid flicker.
      SetStyle( ControlStyles.AllPaintingInWmPaint
              | ControlStyles.UserPaint
              | ControlStyles.Opaque, true );

      // Layout: the reserved HUD rows are C64 character rows (8 source px)
      // AT MAP SCALE, so they participate in the fit — the integer zoom is
      // the largest s where (map + reserved band) still fits the screen.
      // Centering the (reserved + map) block vertically and centering the
      // map below a top band are mathematically the same position:
      //   mapY = (screenH − mapH·s + reservedPx·s) / 2.
      int reservedPx = Math.Max( 0, ReservedTopLines ) * 8;
      int screenW = Bounds.Width;
      int screenH = Bounds.Height;
      int mapW = Math.Max( 1, MapImage.Width );
      int mapH = Math.Max( 1, MapImage.Height );

      m_Scale = Math.Max( 1, Math.Min( screenW / mapW, screenH / ( mapH + reservedPx ) ) );
      m_MapDestX = ( screenW - mapW * m_Scale ) / 2;
      m_MapDestY = ( screenH - mapH * m_Scale + reservedPx * m_Scale ) / 2;

      m_ScreenBuffer = new GR.Image.FastImage( screenW, screenH, MapImage.PixelFormat );
      if ( m_ComposeSprites != null )
      {
        m_ComposeBuffer = new GR.Image.FastImage( MapImage.Width + m_ComposePadLeft + composePadRight,
                                                  MapImage.Height + m_ComposePadTop + composePadBottom,
                                                  MapImage.PixelFormat );
      }
    }



    protected override void OnShown( EventArgs e )
    {
      base.OnShown( e );
      // Pure view surface — no cursor. Balanced by the Show in OnFormClosed.
      Cursor.Hide();

      if ( ( m_AdvanceFrames != null )
      ||   ( m_Filters != null ) )
      {
        m_AnimTimer = new Timer
        {
          Interval = 30
        };
        m_AnimTimer.Tick += ( s, args ) =>
        {
          bool advanced = ( m_AdvanceFrames != null ) && m_AdvanceFrames( m_AnimTimer.Interval );
          // Also keep painting while a temporal filter (phosphor trail) is
          // still decaying, even when no sprite frame changed.
          if ( ( advanced )
          ||   ( m_FilterNeedsRepaint ) )
          {
            Invalidate();
          }
        };
        m_AnimTimer.Start();
      }
    }



    protected override void OnFormClosed( FormClosedEventArgs e )
    {
      if ( m_AnimTimer != null )
      {
        m_AnimTimer.Stop();
        m_AnimTimer.Dispose();
        m_AnimTimer = null;
      }
      Cursor.Show();
      m_ComposeBuffer?.Dispose();
      m_ScreenBuffer.Dispose();
      base.OnFormClosed( e );
    }



    protected override void OnKeyDown( KeyEventArgs e )
    {
      if ( e.KeyCode == Keys.Escape )
      {
        e.Handled = true;
        Close();
        return;
      }
      base.OnKeyDown( e );
    }



    protected override void OnPaintBackground( PaintEventArgs e )
    {
      // Everything is painted in OnPaint — a background erase would flicker.
    }



    protected override void OnPaint( PaintEventArgs e )
    {
      // 1. Compose: static map, plus the sprites at their current frames.
      // The compose buffer is padded by the sprite overhang; the map sits
      // at (PadLeft, PadTop) inside it, so the blit anchor shifts back by
      // the scaled padding and edge sprites keep their border overhang.
      GR.Image.FastImage source = m_MapImage;
      int blitDestX = m_MapDestX;
      int blitDestY = m_MapDestY;
      if ( m_ComposeSprites != null )
      {
        m_ComposeBuffer.Box( 0, 0, m_ComposeBuffer.Width, m_ComposeBuffer.Height, m_BackgroundARGB );
        m_MapImage.DrawTo( m_ComposeBuffer, m_ComposePadLeft, m_ComposePadTop );
        m_ComposeSprites( m_ComposeBuffer );
        source = m_ComposeBuffer;
        blitDestX -= m_ComposePadLeft * m_Scale;
        blitDestY -= m_ComposePadTop * m_Scale;
      }

      // 2. Background fill + integer upscale into the screen buffer.
      m_ScreenBuffer.Box( 0, 0, m_ScreenBuffer.Width, m_ScreenBuffer.Height, m_BackgroundARGB );
      BlitScaled( source, blitDestX, blitDestY );

      // 3. Optional CRT filter pass over the scaled result — same pipeline,
      // same coordinate contract as the editor's PostPaint (map rect +
      // matching source slice). StateKey is this form, so the phosphor
      // history is private to the fullscreen session.
      m_FilterNeedsRepaint = false;
      if ( ( m_Filters != null )
      &&   ( m_Filters.HasAnyEnabled ) )
      {
        // Filter rect = the MAP rect only (not the padded sprite-overhang
        // area) — matches the editor, where overhanging sprites also sit
        // outside the filtered map slice.
        int visDestX  = Math.Max( 0, m_MapDestX );
        int visDestY  = Math.Max( 0, m_MapDestY );
        int visDestX2 = Math.Min( m_ScreenBuffer.Width, m_MapDestX + m_MapImage.Width * m_Scale );
        int visDestY2 = Math.Min( m_ScreenBuffer.Height, m_MapDestY + m_MapImage.Height * m_Scale );
        var ctx = new FilterContext
        {
          RenderOffsetX  = visDestX,
          RenderOffsetY  = visDestY,
          MapPixelWidth  = Math.Max( 0, visDestX2 - visDestX ),
          MapPixelHeight = Math.Max( 0, visDestY2 - visDestY ),
          SourceWidth    = Math.Max( 0, ( visDestX2 - visDestX ) / m_Scale ),
          SourceHeight   = Math.Max( 0, ( visDestY2 - visDestY ) / m_Scale ),
          StateKey       = this,
        };
        m_Filters.Apply( m_ScreenBuffer, ctx );
        m_FilterNeedsRepaint = ctx.NeedsContinuousRepaint;
      }

      // 4. Present 1:1.
      IntPtr hdc = e.Graphics.GetHdc();
      try
      {
        m_ScreenBuffer.DrawToHDC( hdc, ClientRectangle );
      }
      finally
      {
        e.Graphics.ReleaseHdc( hdc );
      }
    }



    /// <summary>
    /// Nearest-neighbor integer upscale of the composed map into the screen
    /// buffer at the given anchor, clipped to the buffer (the map can be
    /// larger than the screen even at 1×, and the sprite-overhang padding
    /// shifts the anchor past the screen edges).
    /// </summary>
    private void BlitScaled( GR.Image.FastImage source, int DestX, int DestY )
    {
      int s = m_Scale;
      int destX0 = Math.Max( 0, DestX );
      int destY0 = Math.Max( 0, DestY );
      int destX1 = Math.Min( m_ScreenBuffer.Width, DestX + source.Width * s );
      int destY1 = Math.Min( m_ScreenBuffer.Height, DestY + source.Height * s );
      if ( ( destX1 <= destX0 )
      ||   ( destY1 <= destY0 ) )
      {
        return;
      }

      unsafe
      {
        byte* pSrc = (byte*)source.PinData();
        byte* pDst = (byte*)m_ScreenBuffer.PinData();
        int   srcStride = source.BytesPerLine;
        int   dstStride = m_ScreenBuffer.BytesPerLine;

        for ( int y = destY0; y < destY1; ++y )
        {
          uint* srcRow = (uint*)( pSrc + ( ( y - DestY ) / s ) * srcStride );
          uint* dstRow = (uint*)( pDst + y * dstStride );
          for ( int x = destX0; x < destX1; ++x )
          {
            dstRow[x] = srcRow[( x - DestX ) / s];
          }
        }

        source.UnpinData();
        m_ScreenBuffer.UnpinData();
      }
    }
  }
}
