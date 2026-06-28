using RetroDevStudio.Controls;
using RetroDevStudio.Types;
using RetroDevStudio;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace RetroDevStudio.CustomRenderer
{
  public class ToolStripSeparatorRenderer : ToolStripProfessionalRenderer
  {
    private StudioCore      Core;

    public ToolStripSeparatorRenderer( StudioCore Core )
    {
      this.Core = Core;
    }



    protected override void OnRenderSeparator( ToolStripSeparatorRenderEventArgs e )
    {
      var bounds = new Rectangle( Point.Empty, e.Item.Size );

      e.Graphics.FillRectangle( new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) ) ), bounds );

      int     lineY = bounds.Bottom - ( bounds.Height / 2 ) - 1;
      int     lineLeft = bounds.Left + 3;
      int     lineRight = bounds.Right;

      e.Graphics.DrawLine( new System.Drawing.Pen( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.BACKGROUND_CONTROL ) ) ), lineLeft, lineY, lineRight, lineY );
      //base.OnRenderSeparator( e );
    }
  }



  public class StudioTheme : ToolStripProfessionalRenderer
  {
    [DllImport( "dwmapi.dll", PreserveSig = true )]
    private static extern int DwmSetWindowAttribute( IntPtr hwnd, int attr, ref int attrValue, int attrSize );

    [DllImport( "uxtheme.dll", CharSet = CharSet.Unicode )]
    private static extern int SetWindowTheme( IntPtr hwnd, string pszSubAppName, string pszSubIdList );

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private StudioCore      Core;


    public SolidBrush       BrushBackgroundControl = null;
    public Pen              PenBackgroundControl = null;


    public StudioTheme( StudioCore Core )
    {
      this.Core = Core;
    }



    protected override void OnRenderMenuItemBackground( ToolStripItemRenderEventArgs e )
    {
      UInt32      color = Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL );
      if ( ( e.Item.Pressed )
      ||   ( e.Item.Selected ) )
      {
        e.Graphics.FillRectangle( new SolidBrush( GR.Color.Helper.FromARGB( color ) ), 0, 0, e.Item.Width, e.Item.Height );

        color = Core.Settings.FGColor( ColorableElement.SELECTED_TEXT );

        // make transparent
        if ( ( color & 0xff000000 ) == 0xff000000 )
        {
          color = ( color & 0x00ffffff ) | 0x40000000;
        }
      }
      e.Graphics.FillRectangle( new SolidBrush( GR.Color.Helper.FromARGB( color ) ), 0, 0, e.Item.Width, e.Item.Height );
    }



    protected override void OnRenderSeparator( ToolStripSeparatorRenderEventArgs e )
    {
      var bounds = new Rectangle( Point.Empty, e.Item.Size );
      bounds = new Rectangle( Point.Empty, e.Item.Bounds.Size );
      e.Graphics.FillRectangle( new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) ) ), bounds );

      int     lineY = bounds.Bottom - ( bounds.Height / 2 ) - 1;
      int     lineLeft = bounds.Left + 3;
      int     lineRight = bounds.Right;

      e.Graphics.DrawLine( new System.Drawing.Pen( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.BACKGROUND_CONTROL ) ) ), lineLeft, lineY, lineRight, lineY );
      //base.OnRenderSeparator( e );
    }



    public void ApplyThemeToToolStripItems( ToolStrip Strip, ToolStripItemCollection Items )
    {
      if ( Strip.Renderer != this )
      {
        Strip.Renderer = this;
      }

      foreach ( ToolStripItem item in Items )
      {
        item.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
        item.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        if ( item is ToolStripSeparator )
        {
          var tsItem = item as ToolStripSeparator;

          tsItem.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_BUTTON ) );
          tsItem.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        }
        if ( item is ToolStripDropDownItem )
        {
          var tsItem = item as ToolStripDropDownItem;
          ApplyThemeToToolStripItems( Strip, tsItem.DropDownItems );
        }
      }
    }



    public void RecolorControlsRecursive( Control.ControlCollection Controls )
    {
      foreach ( Control control in Controls )
      {
        control.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );

        if ( control is ThemedButton )
        {
          var button = control as ThemedButton;

          button.DisabledTextColor = DarkenColor( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.CONTROL_TEXT ) ) );
        }

        if ( control is ThemedCheckBox )
        {
          var themedCheck = control as ThemedCheckBox;

          // Dimmed-but-readable disabled text: darken the (light) themed
          // foreground rather than fall through to SystemColors.GrayText.
          themedCheck.DisabledTextColor = DarkenColor( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) ) );
        }

        control.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );

        if ( control is ComboBox )
        {
          var combo = control as ComboBox;

          combo.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          combo.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
          if ( ( combo.DropDownStyle == ComboBoxStyle.DropDownList )
          &&   ( combo.DrawMode == DrawMode.Normal ) )
          {
            // DropDownList ignores BackColor with visual styles unless owner-drawn
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DrawItem -= ComboBoxDropDownList_DrawItem;
            combo.DrawItem += ComboBoxDropDownList_DrawItem;
          }
        }
        if ( control is TextBox )
        {
          var edit = control as TextBox;

          edit.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
        }
        if ( control is Button )
        {
          var button = control as Button;

          button.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_BUTTON ) );
        }
        if ( control is RadioButton )
        {
          var button = control as RadioButton;
          if ( button.Appearance == Appearance.Button )
          {
            button.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_BUTTON ) );
          }
        }
        if ( control is CheckBox )
        {
          var button = control as CheckBox;

          if ( button.Appearance == Appearance.Button )
          {
            button.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_BUTTON ) );
          }
        }
        if ( control is GroupBox )
        {
          var gb = control as GroupBox;

          gb.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
          gb.FlatStyle = FlatStyle.Flat;
        }
        if ( control is NumericUpDown )
        {
          var nud = control as NumericUpDown;

          nud.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          nud.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        }
        if ( control is ListBox )
        {
          var lb = control as ListBox;

          lb.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          lb.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        }
        if ( control is RichTextBox )
        {
          var rtb = control as RichTextBox;

          rtb.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          rtb.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        }
        if ( control is DataGridView )
        {
          var dgv = control as DataGridView;

          dgv.BackgroundColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          dgv.DefaultCellStyle.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          dgv.DefaultCellStyle.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
          dgv.ColumnHeadersDefaultCellStyle.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          dgv.ColumnHeadersDefaultCellStyle.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
          dgv.EnableHeadersVisualStyles = false;
          dgv.GridColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.BACKGROUND_CONTROL ) );
        }
        if ( control is TreeView )
        {
          var tv = control as TreeView;

          tv.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          tv.ForeColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
        }
        if ( control is ToolStrip )
        {
          var toolStrip = control as ToolStrip;

          ApplyThemeToToolStripItems( toolStrip, toolStrip.Items );
        }
        if ( control is TabPage )
        {
          var tabPage = control as TabPage;

          // UseVisualStyleBackColor ignores BackColor and paints
          // the OS visual styles background (white) instead
          tabPage.UseVisualStyleBackColor = false;
        }
        if ( control is TabControl )
        {
          var tabControl = control as TabControl;

          // Strip visual styles from the TabControl so BackColor is respected
          // and the OS doesn't paint its own white border/background
          if ( tabControl.IsHandleCreated )
          {
            SetWindowTheme( tabControl.Handle, "", "" );
          }
          else
          {
            tabControl.HandleCreated -= TabControl_HandleCreated;
            tabControl.HandleCreated += TabControl_HandleCreated;
          }

          if ( tabControl.DrawMode != TabDrawMode.OwnerDrawFixed )
          {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
          }
        }
        if ( control is CSListView )
        {
          var lv = control as CSListView;

          // Selected-text foreground: use the regular CONTROL_TEXT color
          // (light on dark, dark on light) so the text is readable against
          // the highlight strip. Previously this was set to the SELECTED_
          // TEXT color too, which on the dark theme is dark blue — same
          // hue as the highlight tint, leaving the text nearly invisible
          // against a 25%-transparent dark-blue strip on a dark form bg.
          //
          // Selection BG: pull from the SAME color the scrollbar slider
          // uses (DecentForms.ControlRenderer.ColorScrollSlider — themed
          // as a brightened version of BACKGROUND_BUTTON). That color is
          // explicitly chosen to stand out against the dark theme, so
          // the row highlight is now visibly distinct from the row body.
          // Made 25% transparent so the underlying row colors aren't
          // completely overpainted.
          lv.SelectedTextColor    = Core.Settings.FGColor( ColorableElement.CONTROL_TEXT );
          lv.SelectedTextBGColor  = DecentForms.ControlRenderer.ColorScrollSlider;
          // make transparent
          if ( ( lv.SelectedTextBGColor & 0xff000000 ) == 0xff000000 )
          {
            lv.SelectedTextBGColor = ( lv.SelectedTextBGColor & 0x00ffffff ) | 0x40000000;
          }
          lv.ForeColor            = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) );
          lv.BackColor            = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
        }
        else if ( control is ListView )
        {
          var lv = control as ListView;

          if ( !lv.OwnerDraw )
          {
            lv.OwnerDraw = true;
            lv.DrawColumnHeader += ListView_DrawColumnHeader;
            lv.DrawItem += Lv_DrawItem;
            lv.DrawSubItem += Lv_DrawSubItem;
          }
        }
        RecolorControlsRecursive( control.Controls );
      }
    }



    private void Lv_DrawSubItem( object sender, DrawListViewSubItemEventArgs e )
    {
      e.DrawDefault = true;
    }



    private void Lv_DrawItem( object sender, DrawListViewItemEventArgs e )
    {
      e.DrawDefault = true;
    }



    private void ComboBoxDropDownList_DrawItem( object sender, DrawItemEventArgs e )
    {
      var combo = (ComboBox)sender;

      DrawThemedBackground( e, combo );

      if ( e.Index >= 0 )
      {
        e.Graphics.DrawString( combo.Items[e.Index].ToString(), combo.Font,
          new SolidBrush( combo.ForeColor ), e.Bounds.Left + 2, e.Bounds.Top + 1 );
      }
    }



    private void TabControl_DrawItem( object sender, DrawItemEventArgs e )
    {
      var control = (TabControl)sender;

      TabPage page = control.TabPages[e.Index];

      var bgColorUnselected = ShiftColor( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) ) );
      var bgColorSelected = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );

      if ( e.State == DrawItemState.Selected )
      {
        page.BackColor = bgColorSelected;
      }
      else
      {
        page.BackColor = bgColorUnselected;
      }
      e.Graphics.FillRectangle( new SolidBrush( page.BackColor ), e.Bounds );

      Rectangle paddedBounds = e.Bounds;
      int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
      paddedBounds.Offset( 1, yOffset );
      TextRenderer.DrawText( e.Graphics, page.Text, e.Font, paddedBounds, page.ForeColor );
    }



    private void TabControl_HandleCreated( object sender, EventArgs e )
    {
      var tabControl = (TabControl)sender;
      SetWindowTheme( tabControl.Handle, "", "" );
      tabControl.HandleCreated -= TabControl_HandleCreated;
    }



    private void ListView_DrawColumnHeader( object sender, DrawListViewColumnHeaderEventArgs e )
    {
      e.DrawBackground();

      e.Graphics.FillRectangle( new SolidBrush( ShiftColor( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) ) ) ), e.Bounds );
      e.Graphics.FillRectangle( new SolidBrush( ShiftColor( ShiftColor( GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.CONTROL_TEXT ) ) ) ) ),
        new Rectangle( e.Bounds.Right - 1, e.Bounds.Top, 1, e.Bounds.Height ) );

      var stringFormat = new StringFormat();
      stringFormat.LineAlignment = StringAlignment.Center;

      if ( e.Header.TextAlign == HorizontalAlignment.Center )
      {
        stringFormat.Alignment |= StringAlignment.Center;
      }
      else if ( e.Header.TextAlign == HorizontalAlignment.Right )
      {
        stringFormat.Alignment |= StringAlignment.Far;
      }

      e.Graphics.DrawString( e.Header.Text, e.Header.ListView.Font, 
        new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CONTROL_TEXT ) ) ),
        e.Bounds, stringFormat );
    }



    public void ApplyTheme( Form Form )
    {
      Form.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );

      RecolorControlsRecursive( Form.Controls );

      SetDarkTitleBar( Form );
    }



    /// <summary>
    /// Theme a non-Form container (e.g. a UserControl that is created at runtime
    /// and added to an already-themed document, like the lazily-built export
    /// forms). Mirrors ApplyTheme(Form) but skips the title-bar handling, which
    /// only applies to top-level windows.
    /// </summary>
    public void ApplyTheme( Control Control )
    {
      Control.BackColor = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );

      RecolorControlsRecursive( Control.Controls );
    }



    public void SetDarkTitleBar( Form Form )
    {
      if ( !Form.IsHandleCreated )
      {
        return;
      }

      try
      {
        if ( Environment.OSVersion.Version.Build >= 17763 )
        {
          var bg = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.BACKGROUND_CONTROL ) );
          bool isDark = ( 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B ) < 128;
          int value = isDark ? 1 : 0;
          DwmSetWindowAttribute( Form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof( int ) );
        }
      }
      catch
      {
        // DwmSetWindowAttribute may not be available on all systems
      }
    }



    internal Color DarkenColor( Color OrigColor )
    {
      float   darkFactor = 0.85f;
      return System.Drawing.Color.FromArgb( OrigColor.A, (int)( OrigColor.R * darkFactor ), (int)( OrigColor.G * darkFactor ), (int)( OrigColor.B * darkFactor ) );
    }



    internal Color LightenColor( Color OrigColor )
    {
      float   factor = 1.3f;
      int     offset = 15;
      return System.Drawing.Color.FromArgb( OrigColor.A,
        Math.Min( 255, (int)( OrigColor.R * factor ) + offset ),
        Math.Min( 255, (int)( OrigColor.G * factor ) + offset ),
        Math.Min( 255, (int)( OrigColor.B * factor ) + offset ) );
    }



    internal Color ShiftColor( Color OrigColor )
    {
      float luminance = ( 0.299f * OrigColor.R + 0.587f * OrigColor.G + 0.114f * OrigColor.B ) / 255f;
      if ( luminance < 0.5f )
      {
        return LightenColor( OrigColor );
      }
      return DarkenColor( OrigColor );
    }



    public void DrawThemedBackground( DrawItemEventArgs e, Control Owner )
    {
      if ( ( e.State & DrawItemState.Selected ) != 0 )
      {
        var selColor = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.SELECTED_TEXT ) );
        e.Graphics.FillRectangle( new SolidBrush( Color.FromArgb( 100, selColor.R, selColor.G, selColor.B ) ), e.Bounds );
      }
      else
      {
        e.Graphics.FillRectangle( new SolidBrush( Owner.BackColor ), e.Bounds );
      }
    }



    public void DrawSingleColorComboBox( ComboBox Combo, DrawItemEventArgs e, Palette Pal )
    {
      DrawSingleColorComboBox( Combo, e, Pal, 0 );
    }



    public void DrawSingleColorComboBox( ComboBox Combo, DrawItemEventArgs e, Palette Pal, int PaletteOffset )
    {
      DrawThemedBackground( e, Combo );
      if ( e.Index == -1 )
      {
        return;
      }

      int   indexToDraw = e.Index;
      int   indexInPalette = e.Index + PaletteOffset;

      int offset = (int)e.Graphics.MeasureString( "22", Combo.Font ).Width + 5 + 3;

      System.Drawing.Rectangle itemRect = new System.Drawing.Rectangle( e.Bounds.Left + offset, e.Bounds.Top, e.Bounds.Width - offset, e.Bounds.Height );
      if ( ( e.State & DrawItemState.Disabled ) != 0 )
      {
        e.Graphics.FillRectangle( System.Drawing.SystemBrushes.GrayText, itemRect );
        e.Graphics.DrawString( Combo.Items[indexToDraw].ToString(), Combo.Font, new System.Drawing.SolidBrush( System.Drawing.Color.Gray ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else if ( ( e.State & DrawItemState.Selected ) != 0 )
      {
        if ( indexInPalette < Pal.ColorBrushes.Length )
        {
          e.Graphics.FillRectangle( Pal.ColorBrushes[indexInPalette], itemRect );
        }
        e.Graphics.DrawString( Combo.Items[indexToDraw].ToString(), Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else
      {
        if ( indexInPalette < Pal.ColorBrushes.Length )
        {
          e.Graphics.FillRectangle( Pal.ColorBrushes[indexInPalette], itemRect );
        }
        e.Graphics.DrawString( Combo.Items[indexToDraw].ToString(), Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
    }



    public void DrawMultiColorComboBox( ComboBox Combo, DrawItemEventArgs e, Palette Pal )
    {
      DrawThemedBackground( e, Combo );
      if ( e.Index == -1 )
      {
        return;
      }

      int offset = (int)e.Graphics.MeasureString( "22", Combo.Font ).Width + 5 + 3;
      System.Drawing.Rectangle itemRect = new System.Drawing.Rectangle( e.Bounds.Left + offset, e.Bounds.Top, e.Bounds.Width - offset, e.Bounds.Height );


      if ( e.Index >= 8 )
      {
        itemRect = new System.Drawing.Rectangle( e.Bounds.Left + offset, e.Bounds.Top, ( e.Bounds.Width - offset ) / 2, e.Bounds.Height );
        if ( e.Index < Pal.ColorBrushes.Length )
        {
          e.Graphics.FillRectangle( Pal.ColorBrushes[e.Index], itemRect );
        }
        itemRect = new System.Drawing.Rectangle( e.Bounds.Left + offset + ( e.Bounds.Width - offset ) / 2, e.Bounds.Top, e.Bounds.Width - ( e.Bounds.Width - offset ) / 2, e.Bounds.Height );
        if ( e.Index < Pal.ColorBrushes.Length )
        {
          e.Graphics.FillRectangle( Pal.ColorBrushes[e.Index - 8], itemRect );
        }
      }
      else
      {
        if ( e.Index < Pal.ColorBrushes.Length )
        {
          e.Graphics.FillRectangle( Pal.ColorBrushes[e.Index], itemRect );
        }
      }
      if ( ( e.State & DrawItemState.Disabled ) != 0 )
      {
        e.Graphics.DrawString( Combo.Items[e.Index].ToString(), Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else if ( ( e.State & DrawItemState.Selected ) != 0 )
      {
        e.Graphics.DrawString( Combo.Items[e.Index].ToString(), Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else
      {
        e.Graphics.DrawString( Combo.Items[e.Index].ToString(), Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
    }



    public void DrawVC20ColorComboBox( ComboBox Combo, DrawItemEventArgs e, Palette Pal )
    {
      DrawThemedBackground( e, Combo );
      if ( e.Index == -1 )
      {
        return;
      }

      int offset = (int)e.Graphics.MeasureString( "22 MC", Combo.Font ).Width + 3;
      System.Drawing.Rectangle itemRect = new System.Drawing.Rectangle( e.Bounds.Left + offset, e.Bounds.Top, e.Bounds.Width - offset, e.Bounds.Height );


      if ( e.Index < Pal.ColorBrushes.Length )
      {
        e.Graphics.FillRectangle( Pal.ColorBrushes[e.Index % 8], itemRect );
      }
      string  text = ( e.Index % 8 ).ToString( "D2" );
      if ( e.Index >= 8 )
      {
        text += " MC";
      }
      if ( ( e.State & DrawItemState.Disabled ) != 0 )
      {
        e.Graphics.DrawString( text, Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else if ( ( e.State & DrawItemState.Selected ) != 0 )
      {
        e.Graphics.DrawString( text, Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else
      {
        e.Graphics.DrawString( text, Combo.Font, new System.Drawing.SolidBrush( Combo.ForeColor ), 3.0f, e.Bounds.Top + 1.0f );
      }
    }

  }
}
