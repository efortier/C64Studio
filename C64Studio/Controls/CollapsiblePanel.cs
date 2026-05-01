using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// A reusable panel that can be collapsed vertically via a clickable
  /// header row. Drop in like any other <see cref="Panel"/>:
  ///
  /// <code>
  ///   var cp = new CollapsiblePanel { Title = "Map tools", Dock = DockStyle.Top };
  ///   cp.Controls.Add( yourInnerStuff );
  ///   parent.Controls.Add( cp );
  /// </code>
  ///
  /// The header (<see cref="HeaderHeight"/> px tall) shows a triangle
  /// glyph (▼ when expanded, ▶ when collapsed) followed by the title.
  /// Clicking anywhere on the header toggles <see cref="Collapsed"/>.
  /// Children are placed inside the "body" area below the header — set
  /// <c>Padding.Top = HeaderHeight</c> automatically so children laid
  /// out from (0,0) inside the panel naturally avoid overlapping it.
  ///
  /// When collapsed, the panel shrinks to header-only height; the
  /// previous expanded height is remembered and restored on expand.
  /// Children stay in the control hierarchy but become invisible
  /// because they fall outside the visible bounds (we don't toggle
  /// each child's Visible — that would fight any external code that
  /// manages individual child visibility).
  ///
  /// Designer-safe: clicks at design time DO NOT toggle, so you can
  /// keep dropping controls into it without it folding up under you.
  /// </summary>
  [DesignerCategory( "code" )]
  public class CollapsiblePanel : Panel
  {
    private string  m_Title = "Panel";
    private bool    m_Collapsed = false;
    private int     m_HeaderHeight = 22;
    // The "open" height the panel returns to on expand. Set explicitly
    // via ExpandedHeight (Designer or code), else captured from
    // whatever Height was at OnHandleCreated time (which reflects the
    // Designer-applied size). Updated on user-driven resize while
    // expanded so a manual drag is remembered through collapse cycles.
    private int     m_ExpandedHeight = 100;
    private bool    m_ExpandedHeightCaptured = false;
    private bool    m_HoverHeader = false;



    /// <summary>Header label text.</summary>
    [Category( "Appearance" )]
    [DefaultValue( "Panel" )]
    public string Title
    {
      get { return m_Title; }
      set
      {
        if ( m_Title == value ) return;
        m_Title = value ?? "";
        Invalidate( HeaderRect );
      }
    }

    /// <summary>Pixel height of the clickable header row.</summary>
    [Category( "Appearance" )]
    [DefaultValue( 22 )]
    public int HeaderHeight
    {
      get { return m_HeaderHeight; }
      set
      {
        if ( value < 12 ) value = 12;
        if ( m_HeaderHeight == value ) return;
        m_HeaderHeight = value;
        // Padding pushes children below the header so they don't have
        // to manually offset their Y coords. Re-apply on size change.
        Padding = new Padding( Padding.Left, m_HeaderHeight, Padding.Right, Padding.Bottom );
        if ( m_Collapsed ) Height = m_HeaderHeight;
        Invalidate();
      }
    }

    /// <summary>True when the panel is folded down to header-only height.</summary>
    [Category( "Behavior" )]
    [DefaultValue( false )]
    public bool Collapsed
    {
      get { return m_Collapsed; }
      set
      {
        if ( m_Collapsed == value ) return;
        SetCollapsed( value );
      }
    }

    /// <summary>
    /// Height the panel returns to when expanded. Set in the Designer
    /// (or in code) to control how tall the open state is. Defaults
    /// to 100 px; if not explicitly set, the panel auto-captures
    /// whatever Designer Height was applied at <see cref="OnHandleCreated"/>
    /// time, so you can just drag the panel to size in the Designer
    /// and it'll restore to that size after a runtime expand.
    /// </summary>
    [Category( "Behavior" )]
    [DefaultValue( 100 )]
    public int ExpandedHeight
    {
      get { return m_ExpandedHeight; }
      set
      {
        if ( value < m_HeaderHeight + 1 ) value = m_HeaderHeight + 1;
        m_ExpandedHeight = value;
        // Mark as captured so OnHandleCreated doesn't overwrite an
        // explicit user setting with the Designer's auto-applied
        // Height (which may have been reduced if the panel was
        // collapsed at design time).
        m_ExpandedHeightCaptured = true;
        if ( !m_Collapsed && Height != m_ExpandedHeight )
        {
          Height = m_ExpandedHeight;
        }
      }
    }

    /// <summary>Raised after <see cref="Collapsed"/> changes.</summary>
    [Category( "Behavior" )]
    public event EventHandler CollapsedChanged;



    public CollapsiblePanel()
    {
      // Owner-drawn header needs DoubleBuffered + UserPaint for a
      // flicker-free repaint when the user mouses over it.
      SetStyle(
        ControlStyles.ResizeRedraw |
        ControlStyles.OptimizedDoubleBuffer |
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.UserPaint, true );

      // Reserve the header strip — children placed at y=0 land just
      // below it instead of behind it.
      Padding = new Padding( 0, m_HeaderHeight, 0, 0 );
      MinimumSize = new Size( 40, m_HeaderHeight );
    }



    /// <summary>
    /// Internal collapse-toggle that updates state, resizes, and
    /// fires the <see cref="CollapsedChanged"/> event. Splitting
    /// this out from the property setter lets the click handler
    /// remember the pre-collapse height.
    /// </summary>
    /// <summary>
    /// Effective collapsed-state height — at least the header, but
    /// bumped up to <see cref="MinimumSize"/>.Height when the user
    /// has set one larger. Honors the user's minimum so the panel
    /// can never silently shrink below it; the FlowLayoutPanel /
    /// TableLayoutPanel will then read the actual collapsed Height
    /// (via <see cref="GetPreferredSize"/>) and align siblings
    /// correctly below it.
    /// </summary>
    private int CollapsedTargetHeight
    {
      get
      {
        int h = m_HeaderHeight;
        if ( MinimumSize.Height > h ) h = MinimumSize.Height;
        return h;
      }
    }

    private void SetCollapsed( bool collapsed )
    {
      if ( !collapsed )
      {
        // Expanding — restore the explicit expanded height.
        m_Collapsed = false;
        int target = ( m_ExpandedHeight > CollapsedTargetHeight )
                     ? m_ExpandedHeight
                     : ( m_HeaderHeight + 60 );
        Height = target;
      }
      else
      {
        // Collapsing — snapshot the current height into ExpandedHeight
        // so expand restores exactly the same size. Skip the snapshot
        // when we're already at (or below) collapsed-target height —
        // means Designer placed us collapsed and Height isn't a
        // meaningful expanded size to remember.
        if ( Height > CollapsedTargetHeight )
        {
          m_ExpandedHeight = Height;
          m_ExpandedHeightCaptured = true;
        }
        m_Collapsed = true;
        Height = CollapsedTargetHeight;
      }
      Invalidate();
      var h = CollapsedChanged;
      if ( h != null ) h( this, EventArgs.Empty );
    }



    protected override void OnHandleCreated( EventArgs e )
    {
      base.OnHandleCreated( e );
      // Capture the Designer-applied Height as the expanded height,
      // unless the user explicitly set ExpandedHeight via the
      // property (in which case m_ExpandedHeightCaptured is already
      // true). Skip when collapsed at design time — Height there
      // reflects the collapsed state, not anything we want to remember.
      if ( !m_ExpandedHeightCaptured && !m_Collapsed && Height > m_HeaderHeight )
      {
        m_ExpandedHeight = Height;
        m_ExpandedHeightCaptured = true;
      }
    }



    /// <summary>
    /// Tell the parent layout engine (FlowLayoutPanel /
    /// TableLayoutPanel) what height we'd prefer. Without this
    /// override, the default <see cref="Control.GetPreferredSize"/>
    /// can hand back the unclamped requested size, which makes the
    /// next sibling in a FlowLayoutPanel align at the wrong y when
    /// our Height got clamped UP by MinimumSize. Returning the
    /// effective collapsed/expanded height here keeps siblings in
    /// step with what we actually paint.
    /// </summary>
    public override Size GetPreferredSize( Size proposedSize )
    {
      // Width: use the control's actual current Width. The default
      // base.GetPreferredSize for an empty Panel returns roughly
      // Padding.Horizontal — tiny — and FlowLayoutPanel honors that
      // and visibly squashes the panel. Returning the real Width
      // keeps the panel at whatever the user / parent layout sized it
      // to, clamped up to MinimumSize.Width.
      int targetW = Width;
      if ( targetW < MinimumSize.Width ) targetW = MinimumSize.Width;
      // Height: collapsed → CollapsedTargetHeight; expanded → current
      // Height. Either way clamped up to MinimumSize.Height so the
      // layout engine sees what we'll actually paint.
      int targetH = m_Collapsed ? CollapsedTargetHeight : Height;
      if ( targetH < MinimumSize.Height ) targetH = MinimumSize.Height;
      return new Size( targetW, targetH );
    }



    /// <summary>Header rectangle in client coords. Used for hit-test + invalidate.</summary>
    private Rectangle HeaderRect
    {
      get { return new Rectangle( 0, 0, Width, m_HeaderHeight ); }
    }



    protected override void OnMouseDown( MouseEventArgs e )
    {
      base.OnMouseDown( e );
      if ( DesignMode ) return;             // can't lose drop-target at design time
      if ( e.Button != MouseButtons.Left ) return;
      if ( !HeaderRect.Contains( e.Location ) ) return;
      SetCollapsed( !m_Collapsed );
    }



    protected override void OnMouseMove( MouseEventArgs e )
    {
      base.OnMouseMove( e );
      bool nowHover = HeaderRect.Contains( e.Location );
      if ( nowHover != m_HoverHeader )
      {
        m_HoverHeader = nowHover;
        Invalidate( HeaderRect );
        Cursor = nowHover && !DesignMode ? Cursors.Hand : Cursors.Default;
      }
    }



    protected override void OnMouseLeave( EventArgs e )
    {
      base.OnMouseLeave( e );
      if ( m_HoverHeader )
      {
        m_HoverHeader = false;
        Invalidate( HeaderRect );
        Cursor = Cursors.Default;
      }
    }



    protected override void OnResize( EventArgs e )
    {
      base.OnResize( e );
      // Track the user-driven expanded height so a subsequent collapse
      // and re-expand restore the user's chosen size, not whatever it
      // was at construction time. Only remember when expanded — the
      // collapsed-state height is computed from CollapsedTargetHeight
      // and shouldn't pollute the expanded-state memory.
      if ( !m_Collapsed && Height > m_HeaderHeight )
      {
        m_ExpandedHeight = Height;
        m_ExpandedHeightCaptured = true;
      }
    }



    protected override void OnPaint( PaintEventArgs e )
    {
      base.OnPaint( e );
      var g = e.Graphics;
      var hr = HeaderRect;

      // Header background — pick a tone slightly distinct from the
      // panel body so the clickable area is visually obvious. When
      // hovered, lift it a touch. We compute lighten/darken from
      // BackColor so this auto-adapts to whatever theme is applied.
      Color bg = BackColor;
      Color headerBg = ShiftBrightness( bg, m_HoverHeader ? +18 : +10 );
      using ( var b = new SolidBrush( headerBg ) )
      {
        g.FillRectangle( b, hr );
      }
      // 1px separator line below the header.
      using ( var p = new Pen( ShiftBrightness( bg, -20 ) ) )
      {
        g.DrawLine( p, 0, hr.Bottom - 1, Width, hr.Bottom - 1 );
      }

      // Triangle glyph — drawn as a filled triangle rather than a
      // Unicode arrow so it renders consistently regardless of the
      // installed font.
      Color glyphCol = ForeColor;
      const int glyphLeft = 6;
      int glyphMid = hr.Top + hr.Height / 2;
      int glyphSize = 5;
      Point[] tri;
      if ( m_Collapsed )
      {
        // ▶ pointing right.
        tri = new[]
        {
          new Point( glyphLeft,             glyphMid - glyphSize ),
          new Point( glyphLeft + glyphSize, glyphMid ),
          new Point( glyphLeft,             glyphMid + glyphSize ),
        };
      }
      else
      {
        // ▼ pointing down.
        tri = new[]
        {
          new Point( glyphLeft,                 glyphMid - glyphSize / 2 ),
          new Point( glyphLeft + glyphSize * 2, glyphMid - glyphSize / 2 ),
          new Point( glyphLeft + glyphSize,     glyphMid + glyphSize ),
        };
      }
      using ( var b = new SolidBrush( glyphCol ) )
      {
        g.FillPolygon( b, tri );
      }

      // Title text — left-aligned past the glyph, vertically centred.
      if ( !string.IsNullOrEmpty( m_Title ) )
      {
        const int titleLeft = 22;
        var titleRect = new Rectangle(
          titleLeft, hr.Top, Width - titleLeft - 4, hr.Height );
        using ( var b = new SolidBrush( glyphCol ) )
        {
          using ( var fmt = new StringFormat() { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter } )
          {
            g.DrawString( m_Title, Font, b, titleRect, fmt );
          }
        }
      }
    }



    /// <summary>
    /// Lighten or darken an RGB color by the given delta per channel
    /// (clamped to 0..255). Used to derive header / separator tones
    /// from the panel's BackColor so the look auto-adapts to whatever
    /// theme has been applied.
    /// </summary>
    private static Color ShiftBrightness( Color c, int delta )
    {
      int r = c.R + delta; if ( r < 0 ) r = 0; if ( r > 255 ) r = 255;
      int g = c.G + delta; if ( g < 0 ) g = 0; if ( g > 255 ) g = 255;
      int b = c.B + delta; if ( b < 0 ) b = 0; if ( b > 255 ) b = 255;
      return Color.FromArgb( c.A, r, g, b );
    }
  }
}
