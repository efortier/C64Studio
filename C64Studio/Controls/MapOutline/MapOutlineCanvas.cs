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

    // Wacom pen input (reusable WinTab wrapper). Enabled only while the painter
    // is active; supplies pressure/eraser/proximity. Position ALWAYS comes from
    // the mouse (see OnMouseDown), so mouse and pen mix freely.
    private readonly RetroDevStudio.Tablet.WintabPenProvider m_Pen
      = new RetroDevStudio.Tablet.WintabPenProvider();
    // True while the in-flight stroke originated from the digitizer (pen), not
    // the mouse — intrinsic state of the current stroke (like m_ToolStrokeInFlight),
    // captured at pointer-down from the message source + proximity; drives
    // whether pressure is applied for this stroke.
    private bool          m_StrokeIsPen = false;
    // The tool handling the CURRENT stroke — differs from m_ActiveTool when the
    // pen is flipped to its eraser end (whole-stroke routing). null when idle.
    private IOutlineTool  m_StrokeTool = null;
    private readonly EraserTool m_PenEraser = new EraserTool();
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

    /// <summary>Raised when the rectangular selection changes (incl. cleared).</summary>
    public event EventHandler SelectionChanged;



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

    // ---- Pen (Wacom) settings. Defaulted here so pressure works immediately;
    // the editor overrides these from the project's saved tool settings. ----

    /// <summary>Master toggle: apply pen pressure to strokes.</summary>
    public bool PenPressureEnabled { get; set; } = true;

    /// <summary>0 = size, 1 = opacity, 2 = both.</summary>
    public int PenPressureTarget { get; set; } = 2;

    /// <summary>Pressure response exponent (1 = linear).</summary>
    public float PenPressureGamma { get; set; } = 1.0f;

    /// <summary>Lightest-touch stroke width, image pixels.</summary>
    public float PenMinWidth { get; set; } = 1.0f;

    /// <summary>Lightest-touch opacity factor, 0..1. A small floor (not 0)
    /// keeps a light touch faintly visible instead of fully transparent.</summary>
    public float PenMinAlpha { get; set; } = 0.2f;

    /// <summary>Flipping the pen to its eraser end erases instead of drawing.</summary>
    public bool PenFlipEraser { get; set; } = true;

    private int      m_PenCursorIndex = 0;
    private Cursor[] m_PenCursors = null;
    // Raw HCURSOR handles from CreateIconIndirect — the Cursor(IntPtr) wrapper
    // does NOT own them, so they must be destroyed explicitly in Dispose.
    private IntPtr[] m_PenCursorHandles = null;

    /// <summary>Names for the pen-cursor dropdown (index = PenCursorIndex).</summary>
    public static string[] PenCursorNames
    {
      get
      {
        return new string[] { "Crosshair", "Dot", "Ring", "Precision" };
      }
    }

    /// <summary>Which cursor to show while a pen is hovering (index into PenCursorNames).</summary>
    public int PenCursorIndex
    {
      get
      {
        return m_PenCursorIndex;
      }
      set
      {
        if ( value == m_PenCursorIndex )
        {
          return;
        }
        m_PenCursorIndex = value;
        UpdateCursor();
      }
    }

    /// <summary>True when a Wacom driver + tablet are present.</summary>
    public bool IsTabletPresent
    {
      get
      {
        return m_Pen.IsTabletPresent;
      }
    }

    /// <summary>The latest pen sample (pressure/eraser/proximity/buttons).</summary>
    public RetroDevStudio.Tablet.PenSample PenState
    {
      get
      {
        return m_Pen.Current;
      }
    }

    /// <summary>Raised on every pen packet — lets the editor drive button bindings.</summary>
    public event EventHandler<RetroDevStudio.Tablet.PenSample> PenSampleChanged
    {
      add
      {
        m_Pen.SampleChanged += value;
      }
      remove
      {
        m_Pen.SampleChanged -= value;
      }
    }

    /// <summary>
    /// Opens/closes the WinTab context. Called ONLY for the painter (outline
    /// mode active); the tablet does nothing anywhere else in the app.
    /// </summary>
    public void EnableTablet( bool Enable )
    {
      m_Pen.Enabled = ( Enable && m_Pen.IsTabletPresent );
    }

    /// <summary>
    /// Samples the pixel under the last pointer position and raises ColorPicked
    /// — the eyedropper as a pen-button action (the RMB eyedropper lives in
    /// OnMouseDown).
    /// </summary>
    public void EyedropAtPointer()
    {
      if ( ( m_Image == null )
      ||   ( float.IsNaN( m_PointerImagePos.X ) ) )
      {
        return;
      }
      int x = (int)Math.Floor( m_PointerImagePos.X );
      int y = (int)Math.Floor( m_PointerImagePos.Y );
      if ( ( x >= 0 )
      &&   ( y >= 0 )
      &&   ( x < m_Image.Width )
      &&   ( y < m_Image.Height ) )
      {
        ColorPicked?.Invoke( m_Image.GetPixel( x, y ) );
      }
    }

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
        // Orderly switch: the outgoing tool decides commit-vs-discard (text
        // commits what was typed; drags discard). Finalize whichever tool owns
        // an in-flight stroke — the pen-eraser during a flip-erase, otherwise
        // the active tool (also the path that commits an open text box).
        if ( m_StrokeTool != null )
        {
          if ( m_Image != null )
          {
            m_StrokeTool.OnDeactivate( RefreshToolContext() );
          }
          m_StrokeTool = null;
        }
        else if ( ( m_ActiveTool != null )
        &&        ( m_Image != null ) )
        {
          m_ActiveTool.OnDeactivate( RefreshToolContext() );
        }
        m_ToolStrokeInFlight = false;
        m_StrokeIsPen = false;
        ReleaseStrokeCapture();
        m_ActiveTool = value;
        // Selection is operations-only and belongs to the selection tool:
        // switching to any OTHER tool clears it.
        if ( !( value is SelectionTool ) )
        {
          SetSelectionRect( null );
        }
        UpdateCursor();
        Invalidate();
      }
    }



    private Rectangle? m_SelectionRect = null;

    /// <summary>
    /// The committed rectangular selection in image space (clamped to the
    /// image); null = none. Cleared on tool switch and whenever the
    /// backing image changes (map switch, crop, paste, extend).
    /// </summary>
    public Rectangle? SelectionRect
    {
      get
      {
        return m_SelectionRect;
      }
    }



    public void SetSelectionRect( Rectangle? Rect )
    {
      Rectangle? clamped = null;
      if ( ( Rect.HasValue )
      &&   ( m_Image != null ) )
      {
        var r = Rectangle.Intersect( Rect.Value, new Rectangle( 0, 0, m_Image.Width, m_Image.Height ) );
        if ( ( r.Width >= 1 )
        &&   ( r.Height >= 1 ) )
        {
          clamped = r;
        }
      }
      if ( clamped == m_SelectionRect )
      {
        return;
      }
      m_SelectionRect = clamped;
      SelectionChanged?.Invoke( this, EventArgs.Empty );
      Invalidate();
    }



    /// <summary>
    /// Erases the selection to opaque background — the outline "Delete"
    /// operation. Commits through the same pipeline the tools use, so the
    /// editor's undo stack and dirty tracking pick it up. False = no
    /// selection / no image (nothing happened).
    /// </summary>
    public bool EraseSelection()
    {
      if ( ( m_SelectionRect == null )
      ||   ( m_Image == null )
      // Never mid-stroke: erasing the selection region while a stroke's
      // compositor holds a pre-stroke snapshot interleaves the two edits —
      // the stroke's commit/cancel would resurrect the erased pixels and the
      // undo entries would disagree about "before".
      ||   ( m_ToolStrokeInFlight ) )
      {
        return false;
      }
      var region = m_SelectionRect.Value;
      var beforeCrop = m_Image.Clone( region, m_Image.PixelFormat );
      using ( var g = Graphics.FromImage( m_Image ) )
      using ( var fill = new SolidBrush( Color.FromArgb( 255, EraseColor ) ) )
      {
        g.FillRectangle( fill, region );
      }
      InvalidateImageRegion( region );
      RaiseChangeCommitted( region, beforeCrop, "Delete selection" );
      return true;
    }



    /// <summary>Copy of the selection's pixels; null if no selection.</summary>
    public Bitmap CopySelectionRegion()
    {
      if ( ( m_SelectionRect == null )
      ||   ( m_Image == null ) )
      {
        return null;
      }
      return m_Image.Clone( m_SelectionRect.Value, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
    }



    // Floating paste: the clipboard image rides centered under the cursor
    // until a left-click stamps it (one undoable commit) or Escape /
    // right-click / any flush point cancels it. Owned by the canvas.
    private Bitmap m_FloatingPasteImage = null;

    public bool HasFloatingPaste
    {
      get
      {
        return m_FloatingPasteImage != null;
      }
    }



    /// <summary>Takes OWNERSHIP of Image; disposes it on stamp or cancel.</summary>
    public void BeginFloatingPaste( Bitmap Image )
    {
      CancelFloatingPaste();
      if ( ( Image == null )
      ||   ( m_Image == null ) )
      {
        Image?.Dispose();
        return;
      }
      m_FloatingPasteImage = Image;
      UpdateCursor();
      Invalidate();
    }



    public void CancelFloatingPaste()
    {
      if ( m_FloatingPasteImage == null )
      {
        return;
      }
      m_FloatingPasteImage.Dispose();
      m_FloatingPasteImage = null;
      UpdateCursor();
      Invalidate();
    }



    /// <summary>Floating image's rect centered on Pos, whole pixels for crisp placement.</summary>
    private RectangleF FloatingPasteRect( PointF Pos )
    {
      return new RectangleF(
        (float)Math.Round( Pos.X - m_FloatingPasteImage.Width * 0.5f ),
        (float)Math.Round( Pos.Y - m_FloatingPasteImage.Height * 0.5f ),
        m_FloatingPasteImage.Width, m_FloatingPasteImage.Height );
    }



    private void StampFloatingPaste( PointF ImagePos )
    {
      var dest = FloatingPasteRect( ImagePos );
      var region = Rectangle.Intersect(
        Rectangle.FromLTRB( (int)dest.Left, (int)dest.Top, (int)Math.Ceiling( dest.Right ), (int)Math.Ceiling( dest.Bottom ) ),
        new Rectangle( 0, 0, m_Image.Width, m_Image.Height ) );
      if ( ( region.Width < 1 )
      ||   ( region.Height < 1 ) )
      {
        // Dropped entirely off the picture — nothing to stamp.
        CancelFloatingPaste();
        return;
      }
      var beforeCrop = m_Image.Clone( region, m_Image.PixelFormat );
      using ( var g = Graphics.FromImage( m_Image ) )
      {
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.DrawImage( m_FloatingPasteImage, dest.X, dest.Y,
                     m_FloatingPasteImage.Width, m_FloatingPasteImage.Height );
      }
      InvalidateImageRegion( region );
      RaiseChangeCommitted( region, beforeCrop, "Paste" );
      CancelFloatingPaste();
    }



    private void RaiseChangeCommitted( Rectangle Region, Bitmap BeforeCrop, string Description )
    {
      var handler = ChangeCommitted;
      if ( handler != null )
      {
        handler( Region, BeforeCrop, Description );
      }
      else
      {
        BeforeCrop.Dispose();
      }
    }



    /// <summary>
    /// Black+white double-dash marquee — visible against any content
    /// without a marching-ants animation timer.
    /// </summary>
    internal static void DrawMarquee( Graphics ViewGraphics, RectangleF Rect )
    {
      using ( var blackPen = new Pen( Color.Black ) )
      using ( var whitePen = new Pen( Color.White ) )
      {
        blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        whitePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        whitePen.DashOffset = 2;
        ViewGraphics.DrawRectangle( blackPen, Rect.X, Rect.Y, Rect.Width, Rect.Height );
        ViewGraphics.DrawRectangle( whitePen, Rect.X, Rect.Y, Rect.Width, Rect.Height );
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

      m_Pen.ProximityChanged += Pen_ProximityChanged;
    }




    protected override void OnHandleCreated( EventArgs e )
    {
      base.OnHandleCreated( e );
      // WinForms can recreate the handle; keep the WinTab context pointed at
      // the live HWND.
      m_Pen.Attach( Handle );
    }



    protected override void OnHandleDestroyed( EventArgs e )
    {
      m_Pen.Attach( IntPtr.Zero );
      base.OnHandleDestroyed( e );
    }



    protected override void WndProc( ref Message m )
    {
      // Forward every message to the pen provider first; it consumes WinTab
      // packet/proximity events and ignores the rest. Always call base so
      // normal input/paint messages proceed unchanged.
      m_Pen.ProcessMessage( ref m );
      base.WndProc( ref m );
    }



    private void Pen_ProximityChanged( object sender, EventArgs e )
    {
      // Pen entered/left hover range: swap the cursor and, if the pen was
      // yanked off while still drawing, land the stroke instead of losing it.
      if ( ( !m_Pen.Current.InProximity )
      &&   ( m_ToolStrokeInFlight )
      &&   ( m_StrokeIsPen )
      &&   ( m_StrokeTool != null )
      &&   ( m_Image != null )
      &&   ( !float.IsNaN( m_PointerImagePos.X ) ) )
      {
        // Only a PEN stroke gets landed on proximity loss — a mouse stroke that
        // happens to be in flight while a pen hovers must be unaffected.
        m_StrokeTool.OnPointerUp( RefreshToolContext(), m_PointerImagePos );
        m_StrokeTool = null;
        m_ToolStrokeInFlight = false;
        m_StrokeIsPen = false;
        ReleaseStrokeCapture();
      }
      UpdateCursor();
      Invalidate();
    }



    protected override void Dispose( bool disposing )
    {
      if ( disposing )
      {
        m_Pen.Dispose();
        if ( m_PenCursors != null )
        {
          foreach ( var cur in m_PenCursors )
          {
            // Index 0 is the shared built-in Cursors.Cross — never dispose it.
            if ( ( cur != null )
            &&   ( cur != Cursors.Cross ) )
            {
              cur.Dispose();
            }
          }
          m_PenCursors = null;
        }
        if ( m_PenCursorHandles != null )
        {
          // The Cursor(IntPtr) wrappers don't own these — destroy them here or
          // each open→hover→close cycle leaks 3 native cursor handles.
          foreach ( var handle in m_PenCursorHandles )
          {
            if ( handle != IntPtr.Zero )
            {
              DestroyIcon( handle );
            }
          }
          m_PenCursorHandles = null;
        }
      }
      base.Dispose( disposing );
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
          // Selection coordinates are meaningless against a different
          // image (map switch, crop, paste, extend) — clear, don't risk
          // stale bounds. Cleared BEFORE the swap so the clamp inside
          // SetSelectionRect runs against a consistent state.
          m_SelectionRect = null;
          SelectionChanged?.Invoke( this, EventArgs.Empty );
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
      // steals focus — and on the current focus NOT being a text/numeric
      // input or a combo (memo, font size, font list, extend step): merely
      // brushing the cursor across the canvas must not yank the caret
      // mid-typing or interrupt a font-list browse. Those users get
      // wheel-zoom back on their next canvas click. GetFocusedControl
      // (Win32 GetFocus) resolves the INNER control — a Krypton wrapper's
      // real RichTextBox is a TextBoxBase — so no wrapper-type checks.
      var focused = FocusSupport.GetFocusedControl();
      bool typingHasFocus = ( focused is TextBoxBase )
                         || ( focused is ComboBox );
      if ( ( Visible )
      &&   ( CanFocus )
      &&   ( Form.ActiveForm == FindForm() )
      &&   ( !typingHasFocus ) )
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
      // A hovering pen gets its own selectable cursor (cached — never rebuilt
      // per proximity flap, which would leak GDI handles).
      if ( m_Pen.Current.InProximity )
      {
        Cursor = CurrentPenCursor();
        return;
      }
      if ( m_ActiveTool != null )
      {
        Cursor = m_ActiveTool.Cursor;
        return;
      }
      Cursor = Cursors.Default;
    }



    private Cursor CurrentPenCursor()
    {
      if ( m_PenCursors == null )
      {
        IntPtr h1, h2, h3;
        var c1 = MakePenCursor( 1, out h1 );
        var c2 = MakePenCursor( 2, out h2 );
        var c3 = MakePenCursor( 3, out h3 );
        m_PenCursors = new Cursor[] { Cursors.Cross, c1, c2, c3 };
        m_PenCursorHandles = new IntPtr[] { IntPtr.Zero, h1, h2, h3 };
      }
      int idx = ( ( m_PenCursorIndex >= 0 ) && ( m_PenCursorIndex < m_PenCursors.Length ) )
        ? m_PenCursorIndex : 0;
      return m_PenCursors[idx] ?? Cursors.Cross;
    }



    [System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
    private struct ICONINFO
    {
      public bool   fIcon;
      public int    xHotspot;
      public int    yHotspot;
      public IntPtr hbmMask;
      public IntPtr hbmColor;
    }

    [System.Runtime.InteropServices.DllImport( "user32.dll" )]
    private static extern bool GetIconInfo( IntPtr hIcon, ref ICONINFO piconinfo );
    [System.Runtime.InteropServices.DllImport( "user32.dll" )]
    private static extern IntPtr CreateIconIndirect( ref ICONINFO iconinfo );
    [System.Runtime.InteropServices.DllImport( "user32.dll" )]
    private static extern bool DestroyIcon( IntPtr hIcon );
    [System.Runtime.InteropServices.DllImport( "gdi32.dll" )]
    private static extern bool DeleteObject( IntPtr hObject );

    /// <summary>
    /// Builds a 32×32 cursor with a CENTERED hotspot. Style: 1 = dot, 2 = ring,
    /// 3 = precision (long thin cross). Degrades to Cursors.Cross on any Win32
    /// failure. Caller caches the result.
    /// </summary>
    private static Cursor MakePenCursor( int Style, out IntPtr Handle )
    {
      Handle = IntPtr.Zero;
      const int SIZE = 32;
      const int C = SIZE / 2;
      using ( var bmp = new Bitmap( SIZE, SIZE, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) )
      {
        using ( var g = Graphics.FromImage( bmp ) )
        using ( var white = new Pen( Color.White, 3f ) )
        using ( var black = new Pen( Color.Black, 1f ) )
        using ( var fill = new SolidBrush( Color.White ) )
        {
          g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
          switch ( Style )
          {
            case 1:   // dot
              g.FillEllipse( fill, C - 2, C - 2, 4, 4 );
              g.DrawEllipse( black, C - 2, C - 2, 4, 4 );
              break;
            case 2:   // ring
              g.DrawEllipse( white, C - 6, C - 6, 12, 12 );
              g.DrawEllipse( black, C - 6, C - 6, 12, 12 );
              break;
            default:  // precision cross
              g.DrawLine( white, C, 2, C, SIZE - 3 );
              g.DrawLine( white, 2, C, SIZE - 3, C );
              g.DrawLine( black, C, 2, C, SIZE - 3 );
              g.DrawLine( black, 2, C, SIZE - 3, C );
              break;
          }
        }
        IntPtr hIcon = bmp.GetHicon();
        try
        {
          var info = new ICONINFO();
          if ( !GetIconInfo( hIcon, ref info ) )
          {
            return Cursors.Cross;
          }
          info.fIcon = false;
          info.xHotspot = C;
          info.yHotspot = C;
          IntPtr hCursor = CreateIconIndirect( ref info );
          DeleteObject( info.hbmColor );
          DeleteObject( info.hbmMask );
          if ( hCursor == IntPtr.Zero )
          {
            return Cursors.Cross;
          }
          Handle = hCursor;   // returned so Dispose can DestroyIcon it
          return new Cursor( hCursor );
        }
        finally
        {
          DestroyIcon( hIcon );
        }
      }
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
      m_ToolContext.PenActive = m_StrokeIsPen;
      m_ToolContext.PenPressure = m_Pen.Current.Pressure;
      m_ToolContext.PenPressureEnabled = PenPressureEnabled;
      m_ToolContext.PenPressureTarget = PenPressureTarget;
      m_ToolContext.PenPressureGamma = PenPressureGamma;
      m_ToolContext.PenMinWidth = PenMinWidth;
      m_ToolContext.PenMinAlpha = PenMinAlpha;
      m_ToolContext.SelectionRect = m_SelectionRect;
      m_ToolContext.SetSelectionRect = SetSelectionRect;
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
      // A floating paste is the most "in flight" state of all — Escape
      // (and every other cancel route) throws it away first.
      CancelFloatingPaste();
      var strokeTool = m_StrokeTool ?? m_ActiveTool;
      if ( ( strokeTool != null )
      &&   ( m_Image != null ) )
      {
        strokeTool.Cancel( RefreshToolContext() );
      }
      m_StrokeTool = null;
      m_ToolStrokeInFlight = false;
      m_StrokeIsPen = false;
      ReleaseStrokeCapture();
    }



    /// <summary>
    /// Releases an explicit mouse capture taken for a stroke/pan when that
    /// operation ends by any route OTHER than the normal mouse-up (RMB
    /// abort, tool switch mid-drag, flush point). Without this the canvas
    /// keeps Win32 capture with no button down, so the next click on a
    /// sibling control is delivered here instead — eating the click and
    /// possibly starting a phantom off-image stroke. Releasing capture
    /// re-enters OnMouseCaptureChanged synchronously, so callers clear
    /// m_ToolStrokeInFlight FIRST to keep that safety net a no-op instead
    /// of recursing; the Capture guard means we never steal a capture
    /// another control legitimately holds.
    /// </summary>
    private void ReleaseStrokeCapture()
    {
      if ( Capture )
      {
        Capture = false;
      }
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
        // Must include the STROKE tool: during a pen flip-erase the in-flight
        // tool is m_PenEraser (≠ m_ActiveTool), so checking only m_ActiveTool
        // would report "no pending edit" mid-erase and let Ctrl+Z pop the
        // committed undo stack into the live bitmap.
        return ( m_FloatingPasteImage != null )
            || ( ( m_StrokeTool != null )
            &&   ( m_StrokeTool.HasPendingEdit ) )
            || ( ( m_ActiveTool != null )
            &&   ( m_ActiveTool.HasPendingEdit ) );
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
      // A floating paste has no position until the user clicks — there is
      // nothing to commit at a flush point, so it discards.
      CancelFloatingPaste();
      var pendingTool = m_StrokeTool ?? m_ActiveTool;
      if ( ( pendingTool != null )
      &&   ( m_Image != null )
      &&   ( pendingTool.HasPendingEdit ) )
      {
        pendingTool.OnDeactivate( RefreshToolContext() );
      }
      m_StrokeTool = null;
      m_ToolStrokeInFlight = false;
      m_StrokeIsPen = false;
      ReleaseStrokeCapture();
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
      // moves keep panning/PAINTING.
      if ( !Capture )
      {
        if ( m_IsPanning )
        {
          m_IsPanning = false;
          UpdateCursor();
        }
        if ( m_ToolStrokeInFlight )
        {
          // LAND the stroke (as the proximity-out path does) rather than
          // discard it: the interruption isn't the user's choice, and
          // reverting minutes of careful freehand to a popup would read as
          // "my drawing vanished". Escape/RMB remain the deliberate cancels.
          if ( ( m_StrokeTool != null )
          &&   ( m_Image != null )
          &&   ( !float.IsNaN( m_PointerImagePos.X ) ) )
          {
            m_StrokeTool.OnPointerUp( RefreshToolContext(), m_PointerImagePos );
            m_StrokeTool = null;
            m_ToolStrokeInFlight = false;
            m_StrokeIsPen = false;
          }
          else
          {
            CancelActiveStroke();
          }
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
      if ( float.IsNaN( ImagePos.X ) )
      {
        return RectangleF.Empty;
      }
      float extent = 0;
      if ( m_FloatingPasteImage != null )
      {
        // The floating paste ghost outranks the tool's pointer ghost.
        extent = Math.Max( m_FloatingPasteImage.Width, m_FloatingPasteImage.Height ) * 0.5f + 2;
      }
      else
      {
        // During a stroke use the stroke tool; while merely hovering, preview
        // the eraser footprint if a flipped pen is in range, else the active
        // tool — mirroring OnPaint's selection exactly (incl. the pending-edit
        // exception) so the invalidated region always matches what is drawn.
        var ghostTool = m_StrokeTool;
        if ( ghostTool == null )
        {
          bool flipHover = ( m_Pen.Current.InProximity && m_Pen.Current.IsEraser && PenFlipEraser )
                        && ( ( m_ActiveTool == null )
                        ||   ( !m_ActiveTool.HasPendingEdit ) );
          ghostTool = flipHover ? (IOutlineTool)m_PenEraser : m_ActiveTool;
        }
        if ( ghostTool != null )
        {
          extent = ghostTool.PointerGhostExtent( RefreshToolContext() );
        }
      }
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
      if ( m_FloatingPasteImage != null )
      {
        // Floating paste owns the click: left stamps it centered on the
        // cursor, right throws it away.
        if ( e.Button == MouseButtons.Left )
        {
          StampFloatingPaste( ViewToImage( e.Location ) );
          return;
        }
        if ( e.Button == MouseButtons.Right )
        {
          CancelFloatingPaste();
          return;
        }
      }
      if ( e.Button == MouseButtons.Right )
      {
        if ( m_ToolStrokeInFlight )
        {
          // Standard paint-program move: right button aborts the stroke — but
          // ONLY from the mouse. A Wacom barrel button mapped to right-click in
          // the driver fires WM_RBUTTONDOWN mid-stroke; silently vaporizing the
          // stroke being drawn is the "it reverts while I draw" bug. Pen users
          // abort with Escape.
          if ( !m_Pen.Current.InProximity )
          {
            CancelActiveStroke();
          }
          return;
        }
        // Eyedropper — anywhere on the picture, any active tool (or none). But
        // NOT while a pen is in range: a Wacom barrel button is commonly mapped
        // to right-click in the driver, so a pen right-click here would silently
        // change the ink colour. The pen only changes colour via an explicit
        // sidebar button binding; a real MOUSE right-click still eyedrops.
        if ( ( m_Image != null )
        &&   ( !m_Pen.Current.InProximity ) )
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
        // Fix the stroke's input source at pointer-down: it is a PEN stroke iff
        // a pen is in range AND actually touching (tip bit / nonzero pressure in
        // the latest packet). We do NOT test the OS pen-message signature: WinTab
        // pressure requires the Wacom driver's "Windows Ink" to be OFF, and in
        // that mode the driver synthesizes the mouse events WITHOUT the OS
        // digitizer signature. The contact test keeps a pen merely PARKED in
        // hover range from hijacking a physical-mouse click into a zero-pressure
        // (near-invisible, or flip-erasing) pen stroke; if a genuine pen tap
        // ever races its contact packet, it degrades to a constant-width stroke.
        m_StrokeIsPen = ( m_Pen.Current.InProximity )
                     && ( ( ( m_Pen.Current.Buttons & 1 ) != 0 )
                     ||   ( m_Pen.Current.Pressure > 0f ) );
        // Pen flipped to its eraser end erases regardless of the selected tool
        // — route the WHOLE stroke through an internal eraser, leaving the
        // user's active tool untouched (restored automatically on flip-back).
        bool flipErase = ( m_StrokeIsPen && m_Pen.Current.IsEraser && PenFlipEraser );
        m_StrokeTool = flipErase ? (IOutlineTool)m_PenEraser : m_ActiveTool;
        m_ToolStrokeInFlight = true;
        Capture = true;
        m_PointerImagePos = ViewToImage( e.Location );
        m_StrokeTool.OnPointerDown( RefreshToolContext(), m_PointerImagePos );
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
        m_StrokeTool.OnPointerMove( RefreshToolContext(), m_PointerImagePos );
      }
      if ( ( m_ActiveTool != null )
      ||   ( m_FloatingPasteImage != null ) )
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
      if ( ( m_ActiveTool != null )
      ||   ( m_FloatingPasteImage != null ) )
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
        // PenActive must still be true through OnPointerUp so the pressure
        // path commits; clear it after.
        m_StrokeTool.OnPointerUp( RefreshToolContext(), ViewToImage( e.Location ) );
        m_StrokeTool = null;
        m_StrokeIsPen = false;
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

      // Tool overlay pass — in-flight shape previews, brush-size ghost. During
      // a flip-erase stroke this is the eraser; while merely HOVERING with a
      // flipped pen, preview the eraser too so the ghost matches what a press
      // will do (mirrors GhostViewRect's selection so the invalidation region
      // and the drawn ghost agree).
      var previewTool = m_StrokeTool;
      if ( previewTool == null )
      {
        // Hovering with a flipped pen previews the eraser footprint — but never
        // at the cost of hiding an active tool's LIVE pending edit (an open
        // text box must stay visible while the pen hovers).
        bool flipHover = ( m_Pen.Current.InProximity && m_Pen.Current.IsEraser && PenFlipEraser )
                      && ( ( m_ActiveTool == null )
                      ||   ( !m_ActiveTool.HasPendingEdit ) );
        previewTool = flipHover ? (IOutlineTool)m_PenEraser : m_ActiveTool;
      }
      if ( previewTool != null )
      {
        previewTool.OnPaintPreview( RefreshToolContext(), g, ImageToView, m_Zoom, m_PointerImagePos );
      }

      // Committed selection marquee (the in-flight drag is drawn by the
      // selection tool's preview pass above).
      if ( m_SelectionRect.HasValue )
      {
        var selTopLeft = ImageToView( new PointF( m_SelectionRect.Value.X, m_SelectionRect.Value.Y ) );
        DrawMarquee( g, new RectangleF( selTopLeft.X, selTopLeft.Y,
                                        m_SelectionRect.Value.Width * m_Zoom,
                                        m_SelectionRect.Value.Height * m_Zoom ) );
      }

      // Floating paste ghost, centered on the cursor: mostly-opaque
      // preview + marquee frame so it reads as "not stamped yet".
      if ( ( m_FloatingPasteImage != null )
      &&   ( !float.IsNaN( m_PointerImagePos.X ) ) )
      {
        var destImage = FloatingPasteRect( m_PointerImagePos );
        var ghostTopLeft = ImageToView( new PointF( destImage.X, destImage.Y ) );
        var destView = new RectangleF( ghostTopLeft.X, ghostTopLeft.Y,
                                       destImage.Width * m_Zoom, destImage.Height * m_Zoom );
        var previousInterpolation = g.InterpolationMode;
        var previousOffset = g.PixelOffsetMode;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        using ( var attributes = new System.Drawing.Imaging.ImageAttributes() )
        {
          var ghostAlpha = new System.Drawing.Imaging.ColorMatrix();
          ghostAlpha.Matrix33 = 0.7f;
          attributes.SetColorMatrix( ghostAlpha );
          g.DrawImage( m_FloatingPasteImage,
            new Rectangle( (int)destView.X, (int)destView.Y, (int)destView.Width, (int)destView.Height ),
            0, 0, m_FloatingPasteImage.Width, m_FloatingPasteImage.Height,
            GraphicsUnit.Pixel, attributes );
        }
        g.InterpolationMode = previousInterpolation;
        g.PixelOffsetMode = previousOffset;
        DrawMarquee( g, destView );
      }
    }
  }
}
