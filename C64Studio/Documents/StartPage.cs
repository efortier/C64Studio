using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RetroDevStudio.Types;



namespace RetroDevStudio.Documents
{
  /// <summary>
  /// The Start Page: shown maximized in the document area at startup. Hosts
  /// the unlimited recent-files list (left side; pinned entries on top) fed
  /// by Settings.StartPageRecentFiles. Double-clicking an entry opens the
  /// file and closes the page; a missing file offers to remove its entry.
  /// Deliberately lightweight so startup stays fast: one small INI read and
  /// a handful of cached type icons — no per-entry file system access.
  /// </summary>
  public partial class StartPage : BaseDocument
  {
    private Settings.StartPageRecentFiles    m_RecentFiles;
    private Font                             m_TitleFont;
    private Dictionary<ProjectElement.ElementType, Bitmap> m_TypeIcons = new Dictionary<ProjectElement.ElementType, Bitmap>();



    public StartPage( StudioCore Core, Settings.StartPageRecentFiles RecentFiles )
    {
      this.Core     = Core;
      m_RecentFiles = RecentFiles;

      m_TitleFont = new Font( Font.Name, Font.Size + 2, FontStyle.Bold );
      BuildTypeIconCache();

      InitializeComponent();

      Icon = Properties.Resources.c64;

      // Cached GDI objects die with the page instance (the page is closed by
      // a double-click and lazily recreated by MainForm).
      Disposed += StartPage_Disposed;

      RebuildList();
    }



    private void StartPage_Disposed( object sender, EventArgs e )
    {
      m_TitleFont.Dispose();
      foreach ( var icon in m_TypeIcons.Values )
      {
        icon?.Dispose();
      }
      m_TypeIcons.Clear();
    }



    /// <summary>
    /// One badge bitmap per element type, drawn once at construction:
    /// a rounded square in a per-type accent color with a short type code.
    /// Mid-saturation fills stay readable on dark AND light themes (the
    /// legacy 16x16 explorer icons all but vanish on dark backgrounds),
    /// and the color itself distinguishes the types at a glance.
    /// </summary>
    private void BuildTypeIconCache()
    {
      m_TypeIcons[ProjectElement.ElementType.INVALID]          = CreateTypeBadge( Color.FromArgb( 0x9E, 0x9E, 0x9E ), "?" );
      m_TypeIcons[ProjectElement.ElementType.ASM_SOURCE]       = CreateTypeBadge( Color.FromArgb( 0x1E, 0x88, 0xE5 ), "ASM" );
      m_TypeIcons[ProjectElement.ElementType.SPRITE_SET]       = CreateTypeBadge( Color.FromArgb( 0xD8, 0x1B, 0x60 ), "SPR" );
      m_TypeIcons[ProjectElement.ElementType.CHARACTER_SET]    = CreateTypeBadge( Color.FromArgb( 0xFB, 0x8C, 0x00 ), "CHR" );
      m_TypeIcons[ProjectElement.ElementType.BASIC_SOURCE]     = CreateTypeBadge( Color.FromArgb( 0x00, 0xAC, 0xC1 ), "BAS" );
      m_TypeIcons[ProjectElement.ElementType.GRAPHIC_SCREEN]   = CreateTypeBadge( Color.FromArgb( 0x8E, 0x24, 0xAA ), "GFX" );
      m_TypeIcons[ProjectElement.ElementType.CHARACTER_SCREEN] = CreateTypeBadge( Color.FromArgb( 0x00, 0x89, 0x7B ), "SCR" );
      m_TypeIcons[ProjectElement.ElementType.MAP_EDITOR]       = CreateTypeBadge( Color.FromArgb( 0x43, 0xA0, 0x47 ), "MAP" );
      m_TypeIcons[ProjectElement.ElementType.SOLUTION]         = CreateTypeBadge( Color.FromArgb( 0xFF, 0xB3, 0x00 ), "S64" );
      m_TypeIcons[ProjectElement.ElementType.PROJECT]          = CreateTypeBadge( Color.FromArgb( 0x5C, 0x6B, 0xC0 ), "C64" );
      m_TypeIcons[ProjectElement.ElementType.DISASSEMBLER]     = CreateTypeBadge( Color.FromArgb( 0x54, 0x6E, 0x7A ), "DIS" );
      m_TypeIcons[ProjectElement.ElementType.BINARY_FILE]      = CreateTypeBadge( Color.FromArgb( 0x75, 0x75, 0x75 ), "BIN" );
      m_TypeIcons[ProjectElement.ElementType.MEDIA_MANAGER]    = CreateTypeBadge( Color.FromArgb( 0xE5, 0x39, 0x35 ), "DSK" );
      m_TypeIcons[ProjectElement.ElementType.VALUE_TABLE]      = CreateTypeBadge( Color.FromArgb( 0x7C, 0xB3, 0x42 ), "VAL" );
    }



    /// <summary>
    /// Render one 32x32 type badge: rounded rect in the accent color, a
    /// slightly darker outline for definition on light backgrounds, and the
    /// type code centered — white or near-black by the fill's luminance so
    /// the text always has contrast.
    /// </summary>
    private static Bitmap CreateTypeBadge( Color BadgeColor, string Code )
    {
      var bmp = new Bitmap( 32, 32 );
      using ( var g = Graphics.FromImage( bmp ) )
      {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using ( var path = RoundedRectPath( new Rectangle( 0, 0, 31, 31 ), 7 ) )
        {
          using ( var fill = new SolidBrush( BadgeColor ) )
          {
            g.FillPath( fill, path );
          }
          using ( var outline = new Pen( ControlPaint.Dark( BadgeColor, 0.1f ) ) )
          {
            g.DrawPath( outline, path );
          }
        }

        float luminance = ( 0.299f * BadgeColor.R + 0.587f * BadgeColor.G + 0.114f * BadgeColor.B ) / 255f;
        Color textColor = ( luminance > 0.6f ) ? Color.FromArgb( 0x20, 0x20, 0x20 ) : Color.White;

        // ClearType needs an opaque background — AntiAlias renders cleanly
        // on the bitmap's transparent corners.
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using ( var font = new Font( "Segoe UI", ( Code.Length >= 3 ) ? 7.5f : 9f, FontStyle.Bold ) )
        using ( var text = new SolidBrush( textColor ) )
        using ( var sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap } )
        {
          g.DrawString( Code, font, text, new RectangleF( 0, 0, 32, 32 ), sf );
        }
      }
      return bmp;
    }



    private static System.Drawing.Drawing2D.GraphicsPath RoundedRectPath( Rectangle Bounds, int Radius )
    {
      int d = Radius * 2;
      var path = new System.Drawing.Drawing2D.GraphicsPath();
      path.AddArc( Bounds.Left, Bounds.Top, d, d, 180, 90 );
      path.AddArc( Bounds.Right - d, Bounds.Top, d, d, 270, 90 );
      path.AddArc( Bounds.Right - d, Bounds.Bottom - d, d, d, 0, 90 );
      path.AddArc( Bounds.Left, Bounds.Bottom - d, d, d, 90, 90 );
      path.CloseFigure();
      return path;
    }



    /// <summary>
    /// Maps a file extension to the element type whose icon represents it —
    /// mirrors MainForm.OpenFile's extension dispatch.
    /// </summary>
    private static ProjectElement.ElementType ElementTypeFromExtension( string Extension )
    {
      string ext = ( Extension ?? "" ).ToUpper();
      switch ( ext )
      {
        case ".S64":
          return ProjectElement.ElementType.SOLUTION;
        case ".C64":
          return ProjectElement.ElementType.PROJECT;
        case ".DISASSEMBLY":
          return ProjectElement.ElementType.DISASSEMBLER;
        case ".SPRITEPROJECT":
        case ".SPR":
        case ".SPD":
          return ProjectElement.ElementType.SPRITE_SET;
        case ".VALUETABLEPROJECT":
          return ProjectElement.ElementType.VALUE_TABLE;
        case ".CHARSETPROJECT":
        case ".CHR":
          return ProjectElement.ElementType.CHARACTER_SET;
        case ".CHARSCREEN":
          return ProjectElement.ElementType.CHARACTER_SCREEN;
        case ".GRAPHICSCREEN":
        case ".IFF":
        case ".KOA":
        case ".KLA":
          return ProjectElement.ElementType.GRAPHIC_SCREEN;
        case ".BAS":
        case ".B":
          return ProjectElement.ElementType.BASIC_SOURCE;
        case ".MAPPROJECT":
        case ".CTM":
          return ProjectElement.ElementType.MAP_EDITOR;
        case ".BIN":
        case ".MAP":
          return ProjectElement.ElementType.BINARY_FILE;
      }
      if ( Lookup.MediaFormatFromExtension( ext ) != Formats.MediaFormatType.UNKNOWN )
      {
        return ProjectElement.ElementType.MEDIA_MANAGER;
      }
      return ProjectElement.ElementType.ASM_SOURCE;
    }



    /// <summary>
    /// Repopulate from the store: pinned entries first, most recent first
    /// within each group. No file system access — fast regardless of list
    /// size (the existence check happens only on double-click).
    /// </summary>
    public void RebuildList()
    {
      listRecentFiles.BeginUpdate();
      try
      {
        listRecentFiles.Items.Clear();
        foreach ( var entry in m_RecentFiles.SortedEntries() )
        {
          listRecentFiles.Items.Add( entry );
        }
      }
      finally
      {
        listRecentFiles.EndUpdate();
      }
      UpdateButtonStates();
    }



    private void UpdateButtonStates()
    {
      btnOpenFile.Enabled     = ( listRecentFiles.SelectedIndex >= 0 );
      btnRemoveEntry.Enabled  = ( listRecentFiles.SelectedIndex >= 0 );
      btnClearHistory.Enabled = ( listRecentFiles.Items.Count > 0 );
    }



    private void listRecentFiles_SelectedIndexChanged( object sender, EventArgs e )
    {
      UpdateButtonStates();
    }



    private void listRecentFiles_DrawItem( object sender, DrawItemEventArgs e )
    {
      if ( ( e.Index < 0 )
      ||   ( e.Index >= listRecentFiles.Items.Count ) )
      {
        using ( var backBrush = new SolidBrush( listRecentFiles.BackColor ) )
        {
          e.Graphics.FillRectangle( backBrush, e.Bounds );
        }
        return;
      }
      Core.Theming.DrawThemedBackground( e, listRecentFiles );

      var entry = (Settings.StartPageRecentFiles.Entry)listRecentFiles.Items[e.Index];
      var g     = e.Graphics;

      // Per-type color badge, rendered once at native 32x32 — no scaling.
      var type = ElementTypeFromExtension( GR.Path.GetExtension( entry.Path ) );
      if ( ( m_TypeIcons.TryGetValue( type, out Bitmap icon ) )
      &&   ( icon != null ) )
      {
        g.DrawImage( icon, new Rectangle( e.Bounds.Left + 4, e.Bounds.Top + 4, 32, 32 ) );
      }

      // Two-line entry, Delphi-start-page style: file name on top, its
      // folder below in a dimmed tone (50/50 blend of fore/back stays
      // readable on both dark and light themes).
      Color fore = listRecentFiles.ForeColor;
      Color back = listRecentFiles.BackColor;
      Color gray = Color.FromArgb( ( fore.R + back.R ) / 2, ( fore.G + back.G ) / 2, ( fore.B + back.B ) / 2 );

      const int starZone = 30;
      var   titleRect = new Rectangle( e.Bounds.Left + 42, e.Bounds.Top + 3,  e.Bounds.Width - 42 - starZone, 19 );
      var   pathRect  = new Rectangle( e.Bounds.Left + 42, e.Bounds.Top + 22, e.Bounds.Width - 42 - starZone, 16 );

      using ( var sf = new StringFormat() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap } )
      {
        using ( var titleBrush = new SolidBrush( fore ) )
        {
          g.DrawString( GR.Path.GetFileName( entry.Path ), m_TitleFont, titleBrush, titleRect, sf );
        }
        using ( var pathBrush = new SolidBrush( gray ) )
        {
          g.DrawString( GR.Path.GetDirectoryName( entry.Path ), Font, pathBrush, pathRect, sf );
        }

        // Pin star — clickable (see MouseDown): filled amber when pinned,
        // a dimmed outline star when not. Amber and the fore/back blend
        // both read clearly on dark AND light themes.
        var starRect = PinStarRect( e.Bounds );
        using ( var starFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center } )
        {
          if ( entry.Pinned )
          {
            using ( var pinBrush = new SolidBrush( Color.FromArgb( 0xFF, 0xC1, 0x07 ) ) )
            {
              g.DrawString( "★", m_TitleFont, pinBrush, starRect, starFormat );
            }
          }
          else
          {
            using ( var pinBrush = new SolidBrush( gray ) )
            {
              g.DrawString( "☆", m_TitleFont, pinBrush, starRect, starFormat );
            }
          }
        }
      }
      e.DrawFocusRectangle();
    }



    /// <summary>
    /// The clickable pin-star zone at the right edge of a row.
    /// </summary>
    private static Rectangle PinStarRect( Rectangle ItemBounds )
    {
      return new Rectangle( ItemBounds.Right - 28, ItemBounds.Top + 2, 26, 24 );
    }



    private void listRecentFiles_MouseDown( object sender, MouseEventArgs e )
    {
      int idx = listRecentFiles.IndexFromPoint( e.Location );

      // Right-click selects the row under the cursor so the context menu
      // targets what the user clicked, not a stale selection.
      if ( e.Button == MouseButtons.Right )
      {
        if ( idx >= 0 )
        {
          listRecentFiles.SelectedIndex = idx;
        }
        return;
      }

      // Left click on the star toggles the pin directly.
      if ( ( e.Button == MouseButtons.Left )
      &&   ( idx >= 0 )
      &&   ( PinStarRect( listRecentFiles.GetItemRectangle( idx ) ).Contains( e.Location ) ) )
      {
        TogglePinOf( (Settings.StartPageRecentFiles.Entry)listRecentFiles.Items[idx] );
      }
    }



    private void TogglePinOf( Settings.StartPageRecentFiles.Entry Entry )
    {
      m_RecentFiles.TogglePin( Entry );
      RebuildList();
      // Keep the toggled entry selected at its new position.
      int newIndex = listRecentFiles.Items.IndexOf( Entry );
      if ( newIndex >= 0 )
      {
        listRecentFiles.SelectedIndex = newIndex;
      }
    }



    private Settings.StartPageRecentFiles.Entry SelectedEntry()
    {
      if ( listRecentFiles.SelectedIndex < 0 )
      {
        return null;
      }
      return (Settings.StartPageRecentFiles.Entry)listRecentFiles.Items[listRecentFiles.SelectedIndex];
    }



    private void contextRecent_Opening( object sender, System.ComponentModel.CancelEventArgs e )
    {
      var entry = SelectedEntry();
      if ( entry == null )
      {
        e.Cancel = true;
        return;
      }
      menuItemPinRecent.Text = entry.Pinned ? "Unpin" : "Pin to top";
    }



    private void menuItemPinRecent_Click( object sender, EventArgs e )
    {
      var entry = SelectedEntry();
      if ( entry == null )
      {
        return;
      }
      TogglePinOf( entry );
    }



    private void menuItemRemoveRecent_Click( object sender, EventArgs e )
    {
      RemoveSelectedEntry();
    }



    private void RemoveSelectedEntry()
    {
      var entry = SelectedEntry();
      if ( entry == null )
      {
        return;
      }
      m_RecentFiles.Remove( entry );
      RebuildList();
    }



    private void btnOpenFile_Click( DecentForms.ControlBase Sender )
    {
      var entry = SelectedEntry();
      if ( entry == null )
      {
        return;
      }
      OpenEntry( entry );
    }



    private void btnRemoveEntry_Click( DecentForms.ControlBase Sender )
    {
      RemoveSelectedEntry();
    }



    private void btnClearHistory_Click( DecentForms.ControlBase Sender )
    {
      if ( listRecentFiles.Items.Count == 0 )
      {
        return;
      }
      var result = MessageBox.Show( this,
        "Clear the recent files history?" + Environment.NewLine + Environment.NewLine
          + "Pinned entries are kept.",
        "Clear history",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning );
      if ( result != DialogResult.Yes )
      {
        return;
      }
      m_RecentFiles.ClearHistory();
      RebuildList();
    }



    private void listRecentFiles_MouseDoubleClick( object sender, MouseEventArgs e )
    {
      if ( e.Button != MouseButtons.Left )
      {
        return;
      }
      int idx = listRecentFiles.IndexFromPoint( e.Location );
      if ( idx < 0 )
      {
        return;
      }
      // Double-clicking the pin star is two pin toggles, not an open.
      if ( PinStarRect( listRecentFiles.GetItemRectangle( idx ) ).Contains( e.Location ) )
      {
        return;
      }
      OpenEntry( (Settings.StartPageRecentFiles.Entry)listRecentFiles.Items[idx] );
    }



    /// <summary>
    /// Open a recent entry (double-click or the Open File button): a file
    /// that no longer exists offers to drop its entry; an existing one is
    /// opened and this page closes itself.
    /// </summary>
    private void OpenEntry( Settings.StartPageRecentFiles.Entry Entry )
    {
      if ( !System.IO.File.Exists( Entry.Path ) )
      {
        var result = MessageBox.Show( this,
          "The file" + Environment.NewLine + Entry.Path + Environment.NewLine
            + "no longer exists." + Environment.NewLine + Environment.NewLine
            + "Remove it from the recent files list?",
          "File not found",
          MessageBoxButtons.YesNo,
          MessageBoxIcon.Warning );
        if ( result == DialogResult.Yes )
        {
          m_RecentFiles.Remove( Entry );
          RebuildList();
        }
        return;
      }

      // Show a wait cursor while the editor loads — projects can take a
      // moment and the click otherwise gives no feedback. UseWaitCursor on
      // the MAIN form (not this page, which closes mid-flow) keeps the
      // cursor up across the message pumping that happens while the new
      // document tab is shown and docked; Cursor.Current makes it appear
      // immediately without waiting for a mouse move.
      Core.MainForm.UseWaitCursor = true;
      Cursor.Current = Cursors.WaitCursor;
      try
      {
        // Opening re-records the file via MainForm's hooks, moving it to the
        // top of the unpinned group for next time.
        Core.MainForm.OpenFile( Entry.Path );
      }
      finally
      {
        Core.MainForm.UseWaitCursor = false;
        Cursor.Current = Cursors.Default;
      }
      // Options → "Keep Start Page open after opening a file" (default on)
      // leaves the page as a background tab; otherwise close it.
      if ( !m_RecentFiles.KeepStartPageOpen )
      {
        Close();
      }
    }



    public override void RefreshDisplayOptions()
    {
      base.RefreshDisplayOptions();
      // Theme colors are read live in DrawItem — a repaint is all a theme
      // change needs.
      listRecentFiles.Invalidate();
    }
  }
}
