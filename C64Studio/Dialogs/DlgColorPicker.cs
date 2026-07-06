using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Full Windows-RGB color picker with opacity for the map outline paint
  /// mode (nothing C64-palette-bound): hue/saturation wheel + value from
  /// the V slider, RGB and HSV slider/numeric pairs, hex entry, opacity,
  /// swatches, old/new preview. Master state is HSV + alpha — hue and
  /// saturation survive trips through black/grey where RGB alone would
  /// collapse them.
  ///
  /// All UI writes go through detached handlers (SyncUI detaches, writes,
  /// reattaches) — no suppression flags.
  /// </summary>
  public partial class DlgColorPicker : Form
  {
    private StudioCore    m_Core;

    // Master color state. m_Hue 0..360, m_Sat/m_Val 0..1, m_Alpha 0..255.
    private float         m_Hue = 0;
    private float         m_Sat = 0;
    private float         m_Val = 1;
    private int           m_Alpha = 255;

    private Color         m_OriginalColor;

    // Hue/sat disc for the CURRENT value (brightness); rebuilt when V
    // changes. m_WheelBitmapValue remembers which V the bitmap encodes.
    private Bitmap        m_WheelBitmap = null;
    private float         m_WheelBitmapValue = -1;

    private static readonly Color[] s_SwatchPalette = new Color[]
    {
      Color.FromArgb( 0x00, 0x00, 0x00 ), Color.FromArgb( 0xFF, 0xFF, 0xFF ),
      Color.FromArgb( 0x68, 0x37, 0x2B ), Color.FromArgb( 0x70, 0xA4, 0xB2 ),
      Color.FromArgb( 0x6F, 0x3D, 0x86 ), Color.FromArgb( 0x58, 0x8D, 0x43 ),
      Color.FromArgb( 0x35, 0x28, 0x79 ), Color.FromArgb( 0xB8, 0xC7, 0x6F ),
      Color.FromArgb( 0x6F, 0x4F, 0x25 ), Color.FromArgb( 0x43, 0x39, 0x00 ),
      Color.FromArgb( 0x9A, 0x67, 0x59 ), Color.FromArgb( 0x44, 0x44, 0x44 ),
      Color.FromArgb( 0x6C, 0x6C, 0x6C ), Color.FromArgb( 0x9A, 0xD2, 0x84 ),
      Color.FromArgb( 0x6C, 0x5E, 0xB5 ), Color.FromArgb( 0x95, 0x95, 0x95 ),
      Color.FromArgb( 0xFF, 0x00, 0x00 ), Color.FromArgb( 0xFF, 0xA5, 0x00 ),
      Color.FromArgb( 0xFF, 0xFF, 0x00 ), Color.FromArgb( 0x00, 0xFF, 0x00 ),
      Color.FromArgb( 0x00, 0xFF, 0xFF ), Color.FromArgb( 0xFF, 0x00, 0xFF )
    };



    public DlgColorPicker( Color InitialColor, StudioCore Core )
    {
      m_Core = Core;
      m_OriginalColor = InitialColor;

      InitializeComponent();

      PopulateSwatches();
      SetStateFromColor( InitialColor );
      SyncUI();

      if ( Core != null )
      {
        Core.Theming.ApplyTheme( this );
      }
    }



    /// <summary>The picked color including opacity.</summary>
    public Color SelectedColor
    {
      get
      {
        return Color.FromArgb( m_Alpha, CurrentRGB() );
      }
    }



    private Color CurrentRGB()
    {
      var rgb = ColorSystem.HSVToRGB( new ColorSystem.HSV()
      {
        H = m_Hue,
        S = m_Sat,
        V = m_Val
      } );
      return Color.FromArgb( rgb.R, rgb.G, rgb.B );
    }



    private void SetStateFromColor( Color NewColor )
    {
      var hsv = ColorSystem.RGBToHSV( new ColorSystem.RGB( NewColor.R, NewColor.G, NewColor.B ) );
      // Greys carry no hue and black no saturation — keep the previous
      // values there so slider trips through the achromatic axis don't
      // snap the wheel marker to 0°.
      if ( hsv.S > 0 )
      {
        m_Hue = hsv.H;
      }
      if ( hsv.V > 0 )
      {
        m_Sat = hsv.S;
      }
      m_Val = hsv.V;
      m_Alpha = NewColor.A;
    }



    // ---- UI synchronization ----------------------------------------------

    private void DetachValueHandlers()
    {
      trackR.ValueChanged -= channel_ValueChanged;
      trackG.ValueChanged -= channel_ValueChanged;
      trackB.ValueChanged -= channel_ValueChanged;
      editR.ValueChanged -= channel_ValueChanged;
      editG.ValueChanged -= channel_ValueChanged;
      editB.ValueChanged -= channel_ValueChanged;
      trackH.ValueChanged -= hsv_ValueChanged;
      trackS.ValueChanged -= hsv_ValueChanged;
      trackV.ValueChanged -= hsv_ValueChanged;
      editH.ValueChanged -= hsv_ValueChanged;
      editS.ValueChanged -= hsv_ValueChanged;
      editV.ValueChanged -= hsv_ValueChanged;
      trackA.ValueChanged -= alpha_ValueChanged;
      editA.ValueChanged -= alpha_ValueChanged;
      editHexColor.TextChanged -= editHexColor_TextChanged;
    }



    private void AttachValueHandlers()
    {
      trackR.ValueChanged += channel_ValueChanged;
      trackG.ValueChanged += channel_ValueChanged;
      trackB.ValueChanged += channel_ValueChanged;
      editR.ValueChanged += channel_ValueChanged;
      editG.ValueChanged += channel_ValueChanged;
      editB.ValueChanged += channel_ValueChanged;
      trackH.ValueChanged += hsv_ValueChanged;
      trackS.ValueChanged += hsv_ValueChanged;
      trackV.ValueChanged += hsv_ValueChanged;
      editH.ValueChanged += hsv_ValueChanged;
      editS.ValueChanged += hsv_ValueChanged;
      editV.ValueChanged += hsv_ValueChanged;
      trackA.ValueChanged += alpha_ValueChanged;
      editA.ValueChanged += alpha_ValueChanged;
      editHexColor.TextChanged += editHexColor_TextChanged;
    }



    /// <summary>Writes the master state into every control, handlers detached.</summary>
    private void SyncUI()
    {
      var rgb = CurrentRGB();

      DetachValueHandlers();
      try
      {
        trackR.Value = rgb.R;
        trackG.Value = rgb.G;
        trackB.Value = rgb.B;
        editR.Value = rgb.R;
        editG.Value = rgb.G;
        editB.Value = rgb.B;

        int hue = ( (int)Math.Round( m_Hue ) ) % 360;
        trackH.Value = Math.Min( 359, Math.Max( 0, hue ) );
        editH.Value = trackH.Value;
        trackS.Value = (int)Math.Round( m_Sat * 100 );
        editS.Value = trackS.Value;
        trackV.Value = (int)Math.Round( m_Val * 100 );
        editV.Value = trackV.Value;

        trackA.Value = m_Alpha;
        editA.Value = m_Alpha;

        editHexColor.Text = ( m_Alpha == 255 )
          ? $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}"
          : $"#{m_Alpha:X2}{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";
      }
      finally
      {
        AttachValueHandlers();
      }

      panelColorWheel.Invalidate();
      panelPreviewNew.Invalidate();
    }



    // ---- Control events ---------------------------------------------------

    private void channel_ValueChanged( object sender, EventArgs e )
    {
      // Track and numeric of the same channel mirror each other through
      // SyncUI; whichever fired carries the user's value.
      int r = ReferenceEquals( sender, editR ) ? (int)editR.Value : trackR.Value;
      int g = ReferenceEquals( sender, editG ) ? (int)editG.Value : trackG.Value;
      int b = ReferenceEquals( sender, editB ) ? (int)editB.Value : trackB.Value;
      SetStateFromColor( Color.FromArgb( m_Alpha, r, g, b ) );
      SyncUI();
    }



    private void hsv_ValueChanged( object sender, EventArgs e )
    {
      m_Hue = ReferenceEquals( sender, editH ) ? (float)editH.Value : trackH.Value;
      m_Sat = ( ReferenceEquals( sender, editS ) ? (float)editS.Value : trackS.Value ) / 100.0f;
      m_Val = ( ReferenceEquals( sender, editV ) ? (float)editV.Value : trackV.Value ) / 100.0f;
      SyncUI();
    }



    private void alpha_ValueChanged( object sender, EventArgs e )
    {
      m_Alpha = ReferenceEquals( sender, editA ) ? (int)editA.Value : trackA.Value;
      SyncUI();
    }



    private void editHexColor_TextChanged( object sender, EventArgs e )
    {
      Color parsed;
      if ( TryParseHexColor( editHexColor.Text, out parsed ) )
      {
        m_Alpha = parsed.A;
        SetStateFromColor( parsed );
        // Deliberately NOT SyncUI here — rewriting the textbox while the
        // user is still typing would fight the caret. Sliders/preview
        // still need the update though:
        DetachValueHandlers();
        try
        {
          var rgb = CurrentRGB();
          trackR.Value = rgb.R; editR.Value = rgb.R;
          trackG.Value = rgb.G; editG.Value = rgb.G;
          trackB.Value = rgb.B; editB.Value = rgb.B;
          int hue = ( (int)Math.Round( m_Hue ) ) % 360;
          trackH.Value = Math.Min( 359, Math.Max( 0, hue ) ); editH.Value = trackH.Value;
          trackS.Value = (int)Math.Round( m_Sat * 100 ); editS.Value = trackS.Value;
          trackV.Value = (int)Math.Round( m_Val * 100 ); editV.Value = trackV.Value;
          trackA.Value = m_Alpha; editA.Value = m_Alpha;
        }
        finally
        {
          AttachValueHandlers();
        }
        panelColorWheel.Invalidate();
        panelPreviewNew.Invalidate();
      }
    }



    private void editHexColor_Leave( object sender, EventArgs e )
    {
      // Normalize the text once typing is done.
      SyncUI();
    }



    private static bool TryParseHexColor( string Text, out Color ParsedColor )
    {
      ParsedColor = Color.Black;
      if ( string.IsNullOrEmpty( Text ) )
      {
        return false;
      }
      string hex = Text.Trim();
      if ( hex.StartsWith( "#" ) )
      {
        hex = hex.Substring( 1 );
      }
      if ( ( hex.Length != 6 )
      &&   ( hex.Length != 8 ) )
      {
        return false;
      }
      uint value;
      if ( !uint.TryParse( hex, System.Globalization.NumberStyles.HexNumber,
                           System.Globalization.CultureInfo.InvariantCulture, out value ) )
      {
        return false;
      }
      if ( hex.Length == 6 )
      {
        ParsedColor = Color.FromArgb( 255, (int)( ( value >> 16 ) & 0xFF ),
                                      (int)( ( value >> 8 ) & 0xFF ), (int)( value & 0xFF ) );
      }
      else
      {
        ParsedColor = Color.FromArgb( (int)( ( value >> 24 ) & 0xFF ), (int)( ( value >> 16 ) & 0xFF ),
                                      (int)( ( value >> 8 ) & 0xFF ), (int)( value & 0xFF ) );
      }
      return true;
    }



    // ---- Hue/saturation wheel ---------------------------------------------

    private void EnsureWheelBitmap()
    {
      if ( ( m_WheelBitmap != null )
      &&   ( m_WheelBitmapValue == m_Val ) )
      {
        return;
      }
      DisposeWheelBitmap();

      int size = Math.Min( panelColorWheel.ClientSize.Width, panelColorWheel.ClientSize.Height );
      if ( size < 8 )
      {
        return;
      }
      m_WheelBitmap = new Bitmap( size, size, PixelFormat.Format32bppArgb );
      m_WheelBitmapValue = m_Val;
      float radius = size * 0.5f;

      var data = m_WheelBitmap.LockBits( new Rectangle( 0, 0, size, size ),
                                         ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
      unsafe
      {
        byte* basePtr = (byte*)data.Scan0;
        for ( int y = 0; y < size; ++y )
        {
          byte* row = basePtr + y * data.Stride;
          for ( int x = 0; x < size; ++x )
          {
            float dx = ( x + 0.5f - radius ) / radius;
            float dy = ( y + 0.5f - radius ) / radius;
            float dist = (float)Math.Sqrt( dx * dx + dy * dy );
            byte alpha = 255;
            if ( dist > 1.0f )
            {
              // 1.5px feather so the disc edge is not a jagged staircase.
              float over = ( dist - 1.0f ) * radius / 1.5f;
              if ( over >= 1.0f )
              {
                row[x * 4 + 0] = 0;
                row[x * 4 + 1] = 0;
                row[x * 4 + 2] = 0;
                row[x * 4 + 3] = 0;
                continue;
              }
              alpha = (byte)( 255 * ( 1.0f - over ) );
              dist = 1.0f;
            }
            float hue = (float)( Math.Atan2( dy, dx ) * 180.0 / Math.PI );
            if ( hue < 0 )
            {
              hue += 360;
            }
            var rgb = ColorSystem.HSVToRGB( new ColorSystem.HSV()
            {
              H = hue,
              S = dist,
              V = m_Val
            } );
            row[x * 4 + 0] = rgb.B;
            row[x * 4 + 1] = rgb.G;
            row[x * 4 + 2] = rgb.R;
            row[x * 4 + 3] = alpha;
          }
        }
      }
      m_WheelBitmap.UnlockBits( data );
    }



    private void DisposeWheelBitmap()
    {
      if ( m_WheelBitmap != null )
      {
        m_WheelBitmap.Dispose();
        m_WheelBitmap = null;
      }
      m_WheelBitmapValue = -1;
    }



    private void panelColorWheel_Paint( object sender, PaintEventArgs e )
    {
      EnsureWheelBitmap();
      if ( m_WheelBitmap == null )
      {
        return;
      }
      e.Graphics.DrawImageUnscaled( m_WheelBitmap, 0, 0 );

      // Marker at the current hue/saturation.
      float radius = m_WheelBitmap.Width * 0.5f;
      double angle = m_Hue * Math.PI / 180.0;
      float markerX = radius + (float)Math.Cos( angle ) * m_Sat * radius;
      float markerY = radius + (float)Math.Sin( angle ) * m_Sat * radius;
      e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      using ( var outer = new Pen( Color.Black, 2 ) )
      using ( var inner = new Pen( Color.White, 2 ) )
      {
        e.Graphics.DrawEllipse( outer, markerX - 6, markerY - 6, 12, 12 );
        e.Graphics.DrawEllipse( inner, markerX - 5, markerY - 5, 10, 10 );
      }
    }



    private void PickWheelColorAt( Point MousePos )
    {
      if ( m_WheelBitmap == null )
      {
        return;
      }
      float radius = m_WheelBitmap.Width * 0.5f;
      float dx = ( MousePos.X - radius ) / radius;
      float dy = ( MousePos.Y - radius ) / radius;
      float dist = (float)Math.Sqrt( dx * dx + dy * dy );
      float hue = (float)( Math.Atan2( dy, dx ) * 180.0 / Math.PI );
      if ( hue < 0 )
      {
        hue += 360;
      }
      m_Hue = hue;
      m_Sat = Math.Min( 1.0f, dist );
      SyncUI();
    }



    private void panelColorWheel_MouseDown( object sender, MouseEventArgs e )
    {
      if ( e.Button == MouseButtons.Left )
      {
        PickWheelColorAt( e.Location );
      }
    }



    private void panelColorWheel_MouseMove( object sender, MouseEventArgs e )
    {
      if ( e.Button == MouseButtons.Left )
      {
        PickWheelColorAt( e.Location );
      }
    }



    // ---- Previews / swatches / buttons -------------------------------------

    private static void PaintSwatch( Graphics TargetGraphics, Rectangle Bounds, Color SwatchColor )
    {
      using ( var light = new SolidBrush( Color.FromArgb( 180, 180, 180 ) ) )
      using ( var dark = new SolidBrush( Color.FromArgb( 120, 120, 120 ) ) )
      {
        TargetGraphics.FillRectangle( light, Bounds );
        for ( int y = 0; y < ( Bounds.Height + 5 ) / 6; ++y )
        {
          for ( int x = 0; x < ( Bounds.Width + 5 ) / 6; ++x )
          {
            if ( ( ( x + y ) & 1 ) == 1 )
            {
              var cell = new Rectangle( Bounds.X + x * 6, Bounds.Y + y * 6, 6, 6 );
              cell.Intersect( Bounds );
              TargetGraphics.FillRectangle( dark, cell );
            }
          }
        }
      }
      using ( var brush = new SolidBrush( SwatchColor ) )
      {
        TargetGraphics.FillRectangle( brush, Bounds );
      }
    }



    private void panelPreviewOld_Paint( object sender, PaintEventArgs e )
    {
      PaintSwatch( e.Graphics, ( (Control)sender ).ClientRectangle, m_OriginalColor );
    }



    private void panelPreviewNew_Paint( object sender, PaintEventArgs e )
    {
      PaintSwatch( e.Graphics, ( (Control)sender ).ClientRectangle, SelectedColor );
    }



    private void panelPreviewOld_Click( object sender, EventArgs e )
    {
      // Clicking the "old" half reverts to the color the dialog opened with.
      m_Alpha = m_OriginalColor.A;
      SetStateFromColor( m_OriginalColor );
      SyncUI();
    }



    private void PopulateSwatches()
    {
      flowPickerSwatches.SuspendLayout();
      foreach ( var swatchColor in s_SwatchPalette )
      {
        var swatch = new Panel()
        {
          Size = new Size( 24, 24 ),
          Margin = new Padding( 3 ),
          BorderStyle = BorderStyle.FixedSingle,
          Tag = swatchColor
        };
        // Painted, NOT BackColor: Core.Theming.ApplyTheme (run in the
        // constructor right after this) recolors every control's
        // BackColor, which would blank the swatches.
        swatch.Paint += ( s, pe ) =>
        {
          using ( var brush = new SolidBrush( (Color)( (Control)s ).Tag ) )
          {
            pe.Graphics.FillRectangle( brush, ( (Control)s ).ClientRectangle );
          }
        };
        swatch.Click += swatch_Click;
        flowPickerSwatches.Controls.Add( swatch );
      }
      flowPickerSwatches.ResumeLayout();
    }



    private void swatch_Click( object sender, EventArgs e )
    {
      var swatchColor = (Color)( (Control)sender ).Tag;
      // Swatches pick the RGB; the opacity slider stays as set — matching
      // the reference dialog, where opacity is an independent axis.
      SetStateFromColor( Color.FromArgb( m_Alpha, swatchColor ) );
      SyncUI();
    }



    private void btnOK_Click( DecentForms.ControlBase Sender )
    {
      DialogResult = DialogResult.OK;
      Close();
    }



    private void btnCancel_Click( DecentForms.ControlBase Sender )
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }
  }
}
