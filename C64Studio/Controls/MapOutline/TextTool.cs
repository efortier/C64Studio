using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// WYSIWYG text placement: click opens an in-canvas editing box at that
  /// spot; typing renders live in the actual font/style/color from the
  /// toolbar (changing them restyles the open box instantly); drag inside
  /// the box repositions it; Enter starts a new line. Click OUTSIDE
  /// commits the text as pixels and immediately opens a fresh box at the
  /// click point; switching tools commits too (OnDeactivate); Escape
  /// discards. There are no text objects — after commit it is paint.
  ///
  /// Nothing touches the image until commit; the whole edit session lives
  /// in the preview overlay. In-flight state is the edit position (null =
  /// idle). The steady (non-blinking) caret keeps the canvas free of a
  /// blink timer.
  /// </summary>
  public class TextTool : IOutlineTool
  {
    private PointF?       m_EditPos = null;
    private readonly StringBuilder m_Text = new StringBuilder();
    // Reposition drag: offset of the grab point from the text origin.
    private PointF        m_DragOffset;
    private bool          m_IsDraggingText = false;



    public string Name
    {
      get
      {
        return "Text";
      }
    }



    public Cursor Cursor
    {
      get
      {
        return Cursors.IBeam;
      }
    }



    public bool HasPendingEdit
    {
      get
      {
        return m_EditPos.HasValue;
      }
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      if ( !m_EditPos.HasValue )
      {
        m_EditPos = ImagePos;
        m_Text.Clear();
        Context.InvalidateImageRegion( EditRegion( Context ) );
        return;
      }
      if ( EditRegion( Context ).Contains( new Point( (int)ImagePos.X, (int)ImagePos.Y ) ) )
      {
        m_IsDraggingText = true;
        m_DragOffset = new PointF( ImagePos.X - m_EditPos.Value.X, ImagePos.Y - m_EditPos.Value.Y );
        return;
      }
      // Click-away: bake the current box, open a fresh one where clicked.
      Commit( Context );
      m_EditPos = ImagePos;
      m_Text.Clear();
      Context.InvalidateImageRegion( EditRegion( Context ) );
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( ( !m_IsDraggingText )
      ||   ( !m_EditPos.HasValue ) )
      {
        return;
      }
      var previous = EditRegion( Context );
      m_EditPos = new PointF( ImagePos.X - m_DragOffset.X, ImagePos.Y - m_DragOffset.Y );
      Context.InvalidateImageRegion( Rectangle.Union( previous, EditRegion( Context ) ) );
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      m_IsDraggingText = false;
    }



    public void Cancel( OutlineToolContext Context )
    {
      if ( !m_EditPos.HasValue )
      {
        return;
      }
      // Two-stage cancel: while repositioning, abort ONLY the drag (a
      // right-click or Escape mid-drag must not vaporize the typed text);
      // a second cancel with no drag in progress discards the box.
      if ( m_IsDraggingText )
      {
        m_IsDraggingText = false;
        return;
      }
      var region = EditRegion( Context );
      m_EditPos = null;
      m_Text.Clear();
      Context.InvalidateImageRegion( region );
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      if ( !m_EditPos.HasValue )
      {
        return false;
      }
      var previous = EditRegion( Context );
      if ( PressedChar == '\b' )
      {
        if ( m_Text.Length > 0 )
        {
          m_Text.Length -= 1;
        }
      }
      else if ( ( PressedChar == '\r' )
      ||        ( PressedChar == '\n' ) )
      {
        m_Text.Append( '\n' );
      }
      else if ( !char.IsControl( PressedChar ) )
      {
        m_Text.Append( PressedChar );
      }
      else
      {
        return false;
      }
      Context.InvalidateImageRegion( Rectangle.Union( previous, EditRegion( Context ) ) );
      return true;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      // Switching tools keeps what was typed — only Escape throws it away.
      Commit( Context );
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      return 0;
    }



    private void Commit( OutlineToolContext Context )
    {
      if ( !m_EditPos.HasValue )
      {
        return;
      }
      var editPos = m_EditPos.Value;
      string text = m_Text.ToString();
      var region = Rectangle.Intersect( EditRegion( Context ),
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      m_EditPos = null;
      m_Text.Clear();
      m_IsDraggingText = false;

      if ( ( text.Trim().Length == 0 )
      ||   ( region.Width < 1 )
      ||   ( region.Height < 1 ) )
      {
        Context.InvalidateImageRegion( region );
        return;
      }

      var beforeCrop = Context.Image.Clone( region, Context.Image.PixelFormat );
      using ( var renderer = Context.CreateRenderer() )
      {
        renderer.DrawText( text, editPos, Context.TextFontFamily, Context.TextFontSize,
                           Context.TextFontBold, Context.TextFontItalic, Context.PrimaryColor );
      }
      Context.InvalidateImageRegion( region );
      Context.CommitChange( region, beforeCrop, Name );
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( !m_EditPos.HasValue )
      {
        return;
      }
      string text = m_Text.ToString();
      var originView = ImageToView( m_EditPos.Value );

      // WYSIWYG at any zoom: render in view space with the font scaled by
      // the view factor — what commits into the image is the same text at
      // image scale.
      float viewFontSize = Math.Max( 1.0f, Context.TextFontSize * ViewZoom );
      var previousHint = ViewGraphics.TextRenderingHint;
      ViewGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
      using ( var font = GdiPlusOutlineRenderer.CreateFont( Context.TextFontFamily, viewFontSize,
                                                            Context.TextFontBold, Context.TextFontItalic ) )
      {
        SizeF textSize = ViewGraphics.MeasureString( text.Length > 0 ? text : " ", font );

        // Dashed frame marks the live box.
        using ( var framePen = new Pen( Color.FromArgb( 180, 255, 255, 255 ) ) )
        {
          framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
          ViewGraphics.DrawRectangle( framePen, originView.X - 2, originView.Y - 2,
                                      textSize.Width + 4, textSize.Height + 4 );
        }

        if ( text.Length > 0 )
        {
          using ( var brush = new SolidBrush( Context.PrimaryColor ) )
          {
            ViewGraphics.DrawString( text, font, brush, originView );
          }
        }

        // Steady caret after the last character of the last line.
        int lastBreak = text.LastIndexOf( '\n' );
        string lastLine = ( lastBreak < 0 ) ? text : text.Substring( lastBreak + 1 );
        int lineCount = 1;
        foreach ( char c in text )
        {
          if ( c == '\n' )
          {
            ++lineCount;
          }
        }
        float lineHeight = font.GetHeight( ViewGraphics );
        float caretX = originView.X + ( ( lastLine.Length > 0 )
          ? ViewGraphics.MeasureString( lastLine, font ).Width - 2
          : 2 );
        float caretY = originView.Y + ( lineCount - 1 ) * lineHeight;
        using ( var caretPen = new Pen( Context.PrimaryColor.A > 0
          ? Color.FromArgb( 255, Context.PrimaryColor ) : Color.White, Math.Max( 1.0f, ViewZoom ) ) )
        {
          ViewGraphics.DrawLine( caretPen, caretX, caretY + 2, caretX, caretY + lineHeight - 2 );
        }
      }
      ViewGraphics.TextRenderingHint = previousHint;
    }



    /// <summary>
    /// Image-space bounds of the current edit box (+AA margin). Empty when
    /// idle. Measured against the image via a throwaway renderer.
    /// </summary>
    private Rectangle EditRegion( OutlineToolContext Context )
    {
      if ( !m_EditPos.HasValue )
      {
        return Rectangle.Empty;
      }
      string text = m_Text.ToString();
      SizeF size;
      using ( var renderer = Context.CreateRenderer() )
      {
        size = renderer.MeasureText( text.Length > 0 ? text : "  ",
                                     Context.TextFontFamily, Context.TextFontSize,
                                     Context.TextFontBold, Context.TextFontItalic );
      }
      return Rectangle.FromLTRB(
        (int)Math.Floor( m_EditPos.Value.X ) - 4, (int)Math.Floor( m_EditPos.Value.Y ) - 4,
        (int)Math.Ceiling( m_EditPos.Value.X + size.Width ) + 4,
        (int)Math.Ceiling( m_EditPos.Value.Y + size.Height ) + 4 );
    }
  }
}
