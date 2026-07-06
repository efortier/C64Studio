using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// The drawing surface of the map editor's outline (paint) mode. Hosts a
  /// 32bpp ARGB backing bitmap owned by the editor and presents it under a
  /// float view transform: view = image * Zoom + Pan. Strokes are recorded
  /// in IMAGE space (Phase 2+), so drawing works at any zoom level — the
  /// transform only affects presentation.
  ///
  /// Composite custom control: its internals are code by design (the
  /// canvas has no static child controls); the control INSTANCE itself is
  /// Designer-placed on the map editor.
  ///
  /// Interaction handled here: Space+LMB panning (hand cursor), Ctrl+wheel
  /// zoom-to-cursor, plus zoom clamping. The background is painted in
  /// OnPaint on purpose — StudioTheme.RecolorControlsRecursive overwrites
  /// BackColor on every control, so a Designer-set BackColor cannot be
  /// relied on.
  /// </summary>
  public partial class MapOutlineCanvas : Control
  {
    /// <summary>Zoom bounds, as factors (10% .. 800%).</summary>
    public const float MIN_ZOOM = 0.10f;
    public const float MAX_ZOOM = 8.0f;

    // The letterbox around/behind the image. Deliberately distinct from
    // the canvas' black background color so the user can see where the
    // picture ends.
    private static readonly Color   BACKDROP_COLOR = Color.FromArgb( 62, 62, 66 );
    private static readonly Color   IMAGE_BORDER_COLOR = Color.FromArgb( 110, 110, 116 );

    // Backing image is OWNED BY THE EDITOR (single-decoded-canvas policy:
    // the editor encodes/disposes/decodes on map switches). The canvas
    // only presents and, in later phases, draws into it.
    private Bitmap      m_Image = null;

    private float       m_Zoom = 1.0f;
    private PointF      m_Pan = new PointF( 0, 0 );

    // Space+LMB panning. m_SpaceIsDown mirrors the physical key state —
    // intrinsic input state, not a behavior gate. A pan drag continues
    // until mouse-up even if Space is released mid-drag (standard paint
    // program behavior).
    private bool        m_SpaceIsDown = false;
    private bool        m_IsPanning = false;
    private Point       m_PanStartMouse = Point.Empty;
    private PointF      m_PanStartPan = new PointF( 0, 0 );

    // Tool dispatch. The context object is reused (refreshed before every
    // callback); m_ToolStrokeInFlight mirrors "the left button went down
    // over the canvas and was routed to the tool" — intrinsic input state,
    // cleared on the matching mouse-up or a cancel.
    private readonly OutlineToolContext   m_ToolContext = new OutlineToolContext();
    private IOutlineTool  m_ActiveTool = null;
    private bool          m_ToolStrokeInFlight = false;
    // Last pointer position in image space (NaN when outside the canvas) —
    // feeds tool previews like the brush-size ghost.
    private PointF        m_PointerImagePos = new PointF( float.NaN, float.NaN );
    private RectangleF    m_LastGhostViewRect = RectangleF.Empty;



    /// <summary>Raised whenever Zoom changes (toolbar label refresh).</summary>
    public event EventHandler ZoomChanged;

    /// <summary>
    /// A tool finished mutating the image: affected image rect + the
    /// region's PRE-change pixels (ownership transfers to the handler —
    /// push onto the undo stack or dispose) + undo description.
    /// </summary>
    public event Action<Rectangle, Bitmap, string> ChangeCommitted;

    /// <summary>Right-click eyedropper result.</summary>
    public event Action<Color> ColorPicked;



    /// <summary>Ink color for brush/outline/text (carries alpha).</summary>
    public Color PrimaryColor { get; set; } = Color.White;

    /// <summary>Shape fill color (carries alpha; A=0 = no fill).</summary>
    public Color SecondaryColor { get; set; } = Color.FromArgb( 255, 128, 128, 128 );

    /// <summary>What erasing paints — the canvas background.</summary>
    public Color EraseColor { get; set; } = Color.Black;

    public float BrushSize { get; set; } = 8.0f;

    public float EraserSize { get; set; } = 24.0f;

    public float ShapeBorderSize { get; set; } = 3.0f;

    public string TextFontFamily { get; set; } = "Arial";

    public float TextFontSize { get; set; } = 16.0f;

    public bool TextFontBold { get; set; } = false;

    public bool TextFontItalic { get; set; } = false;

    private Bitmap m_StampImage = null;

    /// <summary>
    /// Pre-rendered tile bitmap for the stamp tool. Owned by the EDITOR
    /// (it re-renders on tile/map changes and disposes the old one) —
    /// the canvas only references it.
    /// </summary>
    public Bitmap StampImage
    {
      get
      {
        return m_StampImage;
      }
      set
      {
        m_StampImage = value;
        Invalidate();
      }
    }

    /// <summary>Stamp magnification: 1 = native tile pixels, 2 = doubled, ...</summary>
    public float StampScale { get; set; } = 1.0f;



    /// <summary>
    /// The selected drawing tool; null = none (pan/zoom only). Switching
    /// away cancels an in-flight operation of the previous tool.
    /// </summary>
    public IOutlineTool ActiveTool
    {
      get
      {
        return m_ActiveTool;
      }
      set
      {
        if ( m_ActiveTool == value )
        {
          return;
        }
        // Orderly switch: the outgoing tool decides commit-vs-discard
        // (text commits what was typed; drags discard).
        if ( ( m_ActiveTool != null )
        &&   ( m_Image != null ) )
        {
          m_ActiveTool.OnDeactivate( RefreshToolContext() );
        }
        m_ToolStrokeInFlight = false;
        m_ActiveTool = value;
        UpdateCursor();
        Invalidate();
      }
    }



    public MapOutlineCanvas()
    {
      SetStyle(
        ControlStyles.ResizeRedraw |
        ControlStyles.OptimizedDoubleBuffer |
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.UserPaint |
        ControlStyles.Opaque |
        ControlStyles.Selectable, true );
    }



    /// <summary>
    /// The backing image. Not disposed here — ownership stays with the
    /// editor's outline lifecycle.
    /// </summary>
    public Bitmap Image
    {
      get
      {
        return m_Image;
      }
      set
      {
        if ( m_Image != value )
        {
          // A stroke can't survive its image being swapped out under it —
          // abort against the OLD image before the reference changes.
          CancelActiveStroke();
        }
        m_Image = value;
        Invalidate();
      }
    }



    public float Zoom
    {
      get
      {
        return m_Zoom;
      }
      set
      {
        SetZoomInternal( value );
      }
    }



    public int ZoomPercent
    {
      get
      {
        return (int)Math.Round( m_Zoom * 100.0f );
      }
    }



    public PointF Pan
    {
      get
      {
        return m_Pan;
      }
      set
      {
        m_Pan = value;
        Invalidate();
      }
    }



    public bool IsPanning
    {
      get
      {
        return m_IsPanning;
      }
    }



    public PointF ViewToImage( Point ViewPoint )
    {
      return new PointF( ( ViewPoint.X - m_Pan.X ) / m_Zoom,
                         ( ViewPoint.Y - m_Pan.Y ) / m_Zoom );
    }



    public PointF ImageToView( PointF ImagePoint )
    {
      return new PointF( ImagePoint.X * m_Zoom + m_Pan.X,
                         ImagePoint.Y * m_Zoom + m_Pan.Y );
    }



    /// <summary>
    /// 100% zoom with the image's top-left at the view's top-left (or
    /// centered on the axis where the image is smaller than the view).
    /// </summary>
    public void ResetView()
    {
      m_Zoom = 1.0f;
      m_Pan = new PointF( 0, 0 );
      if ( m_Image != null )
      {
        if ( m_Image.Width < ClientSize.Width )
        {
          m_Pan.X = ( ClientSize.Width - m_Image.Width ) / 2.0f;
        }
        if ( m_Image.Height < ClientSize.Height )
        {
          m_Pan.Y = ( ClientSize.Height - m_Image.Height ) / 2.0f;
        }
      }
      Invalidate();
      ZoomChanged?.Invoke( this, EventArgs.Empty );
    }



    /// <summary>
    /// Centers the picture in the view at the CURRENT zoom (ResetView by
    /// contrast also snaps back to 100%).
    /// </summary>
    public void CenterView()
    {
      if ( m_Image == null )
      {
        return;
      }
      m_Pan = new PointF( ( ClientSize.Width - m_Image.Width * m_Zoom ) * 0.5f,
                          ( ClientSize.Height - m_Image.Height * m_Zoom ) * 0.5f );
      Invalidate();
    }



    protected override void OnMouseEnter( EventArgs e )
    {
      // Windows routes WM_MOUSEWHEEL to the FOCUSED control — without
      // this, Ctrl+wheel zoom dies the moment a toolbar control was
      // touched. Hover-focus is standard paint-program behavior; gated on
      // the containing form being active so a background window never
      // steals focus.
      if ( ( Visible )
      &&   ( CanFocus )
      &&   ( Form.ActiveForm == FindForm() ) )
      {
        Focus();
      }
      base.OnMouseEnter( e );
    }



    /// <summary>
    /// Changes zoom keeping the image point under AnchorView stationary
    /// on screen — the mechanics of both Ctrl+wheel (anchor = cursor) and
    /// the toolbar buttons (anchor = view center).
    /// </summary>
    public void ZoomAt( Point AnchorView, float NewZoom )
    {
      float clamped = Math.Max( MIN_ZOOM, Math.Min( MAX_ZOOM, NewZoom ) );
      if ( clamped == m_Zoom )
      {
        return;
      }
      PointF anchorImage = ViewToImage( AnchorView );
      m_Zoom = clamped;
      m_Pan = new PointF( AnchorView.X - anchorImage.X * m_Zoom,
                          AnchorView.Y - anchorImage.Y * m_Zoom );
      Invalidate();
      ZoomChanged?.Invoke( this, EventArgs.Empty );
    }



    private void SetZoomInternal( float NewZoom )
    {
      ZoomAt( new Point( ClientSize.Width / 2, ClientSize.Height / 2 ), NewZoom );
    }



    /// <summary>
    /// One zoom "notch" (wheel click / toolbar button): multiplicative
    /// steps feel uniform across the whole 10%..800% range, with a snap
    /// to exactly 100% when a step crosses it (drawing quality is best
    /// at 1:1, so make it easy to land on).
    /// </summary>
    public void ZoomStep( int Direction, Point AnchorView )
    {
      float factor = ( Direction > 0 ) ? 1.25f : 1.0f / 1.25f;
      float target = m_Zoom * factor;
      // Snap only on a genuine CROSSING of 1.0 — starting exactly at 100%
      // must step away freely in both directions.
      if ( ( ( m_Zoom < 1.0f ) && ( target > 1.0f ) )
      ||   ( ( m_Zoom > 1.0f ) && ( target < 1.0f ) ) )
      {
        target = 1.0f;
      }
      ZoomAt( AnchorView, target );
    }



    protected override bool IsInputKey( Keys keyData )
    {
      // Space (pan chord) and the arrows must reach KeyDown instead of
      // being treated as dialog navigation keys.
      switch ( keyData & Keys.KeyCode )
      {
        case Keys.Space:
        case Keys.Up:
        case Keys.Down:
        case Keys.Left:
        case Keys.Right:
        // Enter/Back feed the text tool's WYSIWYG box (newline/delete)
        // instead of acting as dialog navigation.
        case Keys.Enter:
        case Keys.Back:
          return true;
      }
      return base.IsInputKey( keyData );
    }



    protected override void OnKeyPress( KeyPressEventArgs e )
    {
      if ( ( m_ActiveTool != null )
      &&   ( m_Image != null )
      &&   ( m_ActiveTool.OnKeyPress( RefreshToolContext(), e.KeyChar ) ) )
      {
        e.Handled = true;
        return;
      }
      base.OnKeyPress( e );
    }



    protected override void OnKeyDown( KeyEventArgs e )
    {
      if ( e.KeyCode == Keys.Space )
      {
        // With an open text box (or any pending edit), Space is INPUT for
        // the tool — typing a space must not arm the pan chord.
        if ( ( m_ActiveTool != null )
        &&   ( m_ActiveTool.HasPendingEdit ) )
        {
          base.OnKeyDown( e );
          return;
        }
        if ( !m_SpaceIsDown )
        {
          m_SpaceIsDown = true;
          UpdateCursor();
        }
        e.Handled = true;
        return;
      }
      base.OnKeyDown( e );
    }



    protected override void OnKeyUp( KeyEventArgs e )
    {
      if ( e.KeyCode == Keys.Space )
      {
        m_SpaceIsDown = false;
        UpdateCursor();
        e.Handled = true;
        return;
      }
      base.OnKeyUp( e );
    }



    protected override void OnLostFocus( EventArgs e )
    {
      // Key-up can be lost when focus moves away with Space held; resync
      // to the real key state instead of trusting the last event seen.
      m_SpaceIsDown = false;
      UpdateCursor();
      base.OnLostFocus( e );
    }



    private void UpdateCursor()
    {
      if ( ( m_IsPanning )
      ||   ( m_SpaceIsDown ) )
      {
        Cursor = Cursors.Hand;
        return;
      }
      if ( m_ActiveTool != null )
      {
        Cursor = m_ActiveTool.Cursor;
        return;
      }
      Cursor = Cursors.Default;
    }



    /// <summary>
    /// Refreshes the shared context before every tool callback — the
    /// image reference changes when the editor swaps maps, and colors and
    /// sizes track the toolbar live.
    /// </summary>
    private OutlineToolContext RefreshToolContext()
    {
      m_ToolContext.Image = m_Image;
      m_ToolContext.PrimaryColor = PrimaryColor;
      m_ToolContext.SecondaryColor = SecondaryColor;
      m_ToolContext.EraseColor = EraseColor;
      m_ToolContext.BrushSize = BrushSize;
      m_ToolContext.EraserSize = EraserSize;
      m_ToolContext.ShapeBorderSize = ShapeBorderSize;
      m_ToolContext.TextFontFamily = TextFontFamily;
      m_ToolContext.TextFontSize = TextFontSize;
      m_ToolContext.TextFontBold = TextFontBold;
      m_ToolContext.TextFontItalic = TextFontItalic;
      m_ToolContext.StampImage = m_StampImage;
      m_ToolContext.StampScale = StampScale;
      m_ToolContext.CreateRenderer = () => new GdiPlusOutlineRenderer( m_Image );
      m_ToolContext.InvalidateImageRegion = InvalidateImageRegion;
      m_ToolContext.CommitChange = ( region, beforeCrop, description ) =>
      {
        var handler = ChangeCommitted;
        if ( handler != null )
        {
          handler( region, beforeCrop, description );
        }
        else
        {
          beforeCrop.Dispose();
        }
      };
      return m_ToolContext;
    }



    /// <summary>
    /// Aborts an in-flight tool operation (Escape, tool switch, map swap).
    /// Safe to call any time.
    /// </summary>
    public void CancelActiveStroke()
    {
      if ( ( m_ActiveTool != null )
      &&   ( m_Image != null ) )
      {
        m_ActiveTool.Cancel( RefreshToolContext() );
      }
      m_ToolStrokeInFlight = false;
    }



    /// <summary>
    /// True while a tool has the pointer captured mid-operation — the
    /// editor's key handling uses this to route Escape to a cancel.
    /// </summary>
    public bool ToolStrokeInFlight
    {
      get
      {
        return m_ToolStrokeInFlight;
      }
    }



    /// <summary>
    /// True while the active tool holds uncommitted state (open text box,
    /// stroke, shape drag). Undo routing uses this: Ctrl+Z with a pending
    /// edit cancels only the edit instead of also popping the stack.
    /// </summary>
    public bool ToolHasPendingEdit
    {
      get
      {
        return ( m_ActiveTool != null )
            && ( m_ActiveTool.HasPendingEdit );
      }
    }



    /// <summary>
    /// Orderly wrap-up of any pending edit before a flush/exit: the tool
    /// decides commit-vs-discard (text COMMITS what was typed; strokes
    /// and shape drags discard). Without this, mode exit would encode a
    /// half-stroke into the sidecar and silently drop typed text.
    /// </summary>
    public void FinalizePendingEdit()
    {
      if ( ( m_ActiveTool != null )
      &&   ( m_Image != null )
      &&   ( m_ActiveTool.HasPendingEdit ) )
      {
        m_ActiveTool.OnDeactivate( RefreshToolContext() );
      }
      m_ToolStrokeInFlight = false;
    }



    /// <summary>
    /// Arms the Space-to-pan chord from OUTSIDE the canvas — the editor's
    /// ProcessCmdKey intercepts Space before a focused toolbar button can
    /// treat it as a click, then calls this (the intercepted key never
    /// reaches OnKeyDown). Idempotent under auto-repeat; the release is
    /// caught by OnKeyUp once focus is here, or by OnLostFocus.
    /// </summary>
    public void BeginSpacePan()
    {
      if ( !m_SpaceIsDown )
      {
        m_SpaceIsDown = true;
        UpdateCursor();
      }
    }



    protected override void OnMouseCaptureChanged( EventArgs e )
    {
      // Capture can be torn away without a matching mouse-up (Alt+Tab,
      // Win key, modal popup mid-drag). Left un-handled that strands
      // m_IsPanning/m_ToolStrokeInFlight as "true" and buttonless mouse
      // moves keep panning/PAINTING. Abort both cleanly.
      if ( !Capture )
      {
        if ( m_IsPanning )
        {
          m_IsPanning = false;
          UpdateCursor();
        }
        if ( m_ToolStrokeInFlight )
        {
          CancelActiveStroke();
        }
      }
      base.OnMouseCaptureChanged( e );
    }



    /// <summary>Maps an image-space region to view space and invalidates it.</summary>
    public void InvalidateImageRegion( Rectangle ImageRegion )
    {
      var topLeft = ImageToView( new PointF( ImageRegion.X, ImageRegion.Y ) );
      var viewRect = new RectangleF( topLeft.X, topLeft.Y,
                                     ImageRegion.Width * m_Zoom, ImageRegion.Height * m_Zoom );
      viewRect.Inflate( 3, 3 );
      Invalidate( Rectangle.Ceiling( viewRect ) );
    }



    /// <summary>
    /// View-space bounds of whatever ghost the active tool paints around
    /// the pointer (brush circle, stamp preview) — the tool itself knows
    /// its footprint via PointerGhostExtent.
    /// </summary>
    private RectangleF GhostViewRect( PointF ImagePos )
    {
      if ( ( float.IsNaN( ImagePos.X ) )
      ||   ( m_ActiveTool == null ) )
      {
        return RectangleF.Empty;
      }
      float extent = m_ActiveTool.PointerGhostExtent( RefreshToolContext() );
      if ( extent <= 0 )
      {
        return RectangleF.Empty;
      }
      var center = ImageToView( ImagePos );
      float radius = extent * m_Zoom + 4;
      return new RectangleF( center.X - radius, center.Y - radius, radius * 2, radius * 2 );
    }



    private void InvalidateGhost( RectangleF OldRect, RectangleF NewRect )
    {
      var union = OldRect;
      if ( union.IsEmpty )
      {
        union = NewRect;
      }
      else if ( !NewRect.IsEmpty )
      {
        union = RectangleF.Union( union, NewRect );
      }
      if ( !union.IsEmpty )
      {
        union.Inflate( 2, 2 );
        Invalidate( Rectangle.Ceiling( union ) );
      }
    }



    protected override void OnMouseDown( MouseEventArgs e )
    {
      Focus();
      if ( ( e.Button == MouseButtons.Left )
      &&   ( m_SpaceIsDown )
      &&   ( !m_ToolStrokeInFlight ) )
      {
        m_IsPanning = true;
        m_PanStartMouse = e.Location;
        m_PanStartPan = m_Pan;
        Capture = true;
        UpdateCursor();
        return;
      }
      if ( e.Button == MouseButtons.Right )
      {
        if ( m_ToolStrokeInFlight )
        {
          // Standard paint-program move: right button aborts the stroke.
          CancelActiveStroke();
          return;
        }
        // Eyedropper — anywhere on the picture, any active tool (or none).
        if ( m_Image != null )
        {
          var imagePos = ViewToImage( e.Location );
          int x = (int)Math.Floor( imagePos.X );
          int y = (int)Math.Floor( imagePos.Y );
          if ( ( x >= 0 )
          &&   ( y >= 0 )
          &&   ( x < m_Image.Width )
          &&   ( y < m_Image.Height ) )
          {
            ColorPicked?.Invoke( m_Image.GetPixel( x, y ) );
          }
        }
        return;
      }
      if ( ( e.Button == MouseButtons.Left )
      &&   ( !m_ToolStrokeInFlight )
      &&   ( m_ActiveTool != null )
      &&   ( m_Image != null ) )
      {
        m_ToolStrokeInFlight = true;
        Capture = true;
        m_PointerImagePos = ViewToImage( e.Location );
        m_ActiveTool.OnPointerDown( RefreshToolContext(), m_PointerImagePos );
        return;
      }
      base.OnMouseDown( e );
    }



    protected override void OnMouseMove( MouseEventArgs e )
    {
      if ( m_IsPanning )
      {
        m_Pan = new PointF( m_PanStartPan.X + ( e.X - m_PanStartMouse.X ),
                            m_PanStartPan.Y + ( e.Y - m_PanStartMouse.Y ) );
        Invalidate();
        return;
      }
      var oldGhost = m_LastGhostViewRect;
      m_PointerImagePos = ViewToImage( e.Location );
      m_LastGhostViewRect = GhostViewRect( m_PointerImagePos );

      if ( m_ToolStrokeInFlight )
      {
        m_ActiveTool.OnPointerMove( RefreshToolContext(), m_PointerImagePos );
      }
      if ( m_ActiveTool != null )
      {
        InvalidateGhost( oldGhost, m_LastGhostViewRect );
      }
      base.OnMouseMove( e );
    }



    protected override void OnMouseLeave( EventArgs e )
    {
      var oldGhost = m_LastGhostViewRect;
      m_PointerImagePos = new PointF( float.NaN, float.NaN );
      m_LastGhostViewRect = RectangleF.Empty;
      if ( m_ActiveTool != null )
      {
        InvalidateGhost( oldGhost, RectangleF.Empty );
      }
      base.OnMouseLeave( e );
    }



    protected override void OnMouseUp( MouseEventArgs e )
    {
      if ( ( m_IsPanning )
      &&   ( e.Button == MouseButtons.Left ) )
      {
        m_IsPanning = false;
        Capture = false;
        UpdateCursor();
        return;
      }
      if ( ( m_ToolStrokeInFlight )
      &&   ( e.Button == MouseButtons.Left ) )
      {
        m_ToolStrokeInFlight = false;
        Capture = false;
        m_ActiveTool.OnPointerUp( RefreshToolContext(), ViewToImage( e.Location ) );
        return;
      }
      base.OnMouseUp( e );
    }



    protected override void OnMouseWheel( MouseEventArgs e )
    {
      if ( ( ModifierKeys & Keys.Control ) == Keys.Control )
      {
        ZoomStep( e.Delta > 0 ? 1 : -1, e.Location );
        return;
      }
      base.OnMouseWheel( e );
    }



    protected override void OnPaint( PaintEventArgs e )
    {
      var g = e.Graphics;

      // Painted (not BackColor-based) — see class comment on theming.
      using ( var backdrop = new SolidBrush( BACKDROP_COLOR ) )
      {
        g.FillRectangle( backdrop, ClientRectangle );
      }

      if ( m_Image == null )
      {
        return;
      }

      // At exactly 100% use nearest neighbor: it is both the fastest and
      // the only pixel-exact presentation. Anywhere else use bilinear —
      // smooth ("not blocky") and much cheaper than bicubic while the
      // user zooms interactively.
      if ( m_Zoom == 1.0f )
      {
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
      }
      else
      {
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
      }
      // Half-pixel offset keeps the image aligned with the transform
      // instead of sampling half a texel off at non-integer zooms.
      g.PixelOffsetMode = PixelOffsetMode.Half;

      var destRect = new RectangleF( m_Pan.X, m_Pan.Y,
                                     m_Image.Width * m_Zoom,
                                     m_Image.Height * m_Zoom );
      g.DrawImage( m_Image, destRect );

      // Thin border marks the picture's edge against the backdrop.
      g.PixelOffsetMode = PixelOffsetMode.Default;
      using ( var borderPen = new Pen( IMAGE_BORDER_COLOR ) )
      {
        g.DrawRectangle( borderPen, destRect.X - 1, destRect.Y - 1,
                         destRect.Width + 1, destRect.Height + 1 );
      }

      // Tool overlay pass — in-flight shape previews, brush-size ghost.
      if ( m_ActiveTool != null )
      {
        m_ActiveTool.OnPaintPreview( RefreshToolContext(), g, ImageToView, m_Zoom, m_PointerImagePos );
      }
    }
  }
}
