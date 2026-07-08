using System;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Modeless popup editor for a map's free-form notes (the "Map memo").
  /// Deliberately a SEPARATE window: a RichTextBox in its own Form gets
  /// normal keyboard handling — no document-level ProcessCmdKey racing the
  /// control for every keystroke, which is what made the old sidebar memo
  /// misbehave. The dialog holds the authoritative text while open and
  /// writes it back to the map at sync points (map switch, focus loss,
  /// close, project save) via FlushToMap — so .Rtf is read ONCE per sync,
  /// never per keystroke, and the selection is never disturbed.
  /// </summary>
  public partial class DlgMapMemo : Form
  {
    private Formats.MapProject.Map      m_Map = null;    // null = read-only / no target
    private bool                        m_MemoDirty = false;
    private bool                        m_Populating = false;

    /// <summary>Raised on the first edit since the last flush — dirty the document.</summary>
    public event Action                 MemoModified;

    /// <summary>Raised when the user picks a font family/size — persist project-wide.</summary>
    public event Action<string, int>    FontPickChanged;



    public DlgMapMemo( StudioCore Core )
    {
      InitializeComponent();

      foreach ( var family in FontFamily.Families )
      {
        comboFont.Items.Add( family.Name );
      }

      Core.Theming.ApplyTheme( this );

      richMemo.TextChanged += richMemo_TextChanged;
      comboFont.SelectedIndexChanged += fontPick_Changed;
      editFontSize.ValueChanged += fontPick_Changed;
      btnBold.Click += ( s, e ) => ToggleStyle( FontStyle.Bold );
      btnItalic.Click += ( s, e ) => ToggleStyle( FontStyle.Italic );
      btnUnderline.Click += ( s, e ) => ToggleStyle( FontStyle.Underline );
      btnColor.Click += btnColor_Click;
    }



    /// <summary>
    /// Point the memo at a map (or a read-only revision snapshot). Flushes
    /// the previous map's pending edits first, so switching maps in the
    /// editor never loses notes.
    /// </summary>
    public void SetMap( Formats.MapProject.Map Map, string MapName, bool ReadOnly )
    {
      FlushToMap();

      m_Populating = true;
      try
      {
        if ( Map == null )
        {
          richMemo.Clear();
          m_Map = null;
          Text = "Map memo";
        }
        else
        {
          if ( string.IsNullOrEmpty( Map.MemoRTF ) )
          {
            richMemo.Clear();
          }
          else
          {
            try
            {
              richMemo.Rtf = Map.MemoRTF;
            }
            catch ( ArgumentException )
            {
              // Corrupt/hand-edited RTF — show it as raw text rather than throw.
              richMemo.Text = Map.MemoRTF;
            }
          }
          // A revision snapshot is read-only — show it but never write back.
          m_Map = ReadOnly ? null : Map;
          Text = "Map memo — " + MapName + ( ReadOnly ? "   (revision, read-only)" : "" );
        }
        richMemo.ReadOnly = ReadOnly || ( Map == null );
        SetToolbarEnabled( !richMemo.ReadOnly );
        m_MemoDirty = false;
      }
      finally
      {
        m_Populating = false;
      }
    }



    /// <summary>Applies the project's saved font pick as the memo's base font + toolbar state.</summary>
    public void SetFontPick( string Family, int Size )
    {
      m_Populating = true;
      try
      {
        int idx = string.IsNullOrEmpty( Family ) ? -1 : comboFont.Items.IndexOf( Family );
        if ( idx >= 0 )
        {
          comboFont.SelectedIndex = idx;
        }
        if ( ( Size >= editFontSize.Minimum )
        &&   ( Size <= editFontSize.Maximum ) )
        {
          editFontSize.Value = Size;
        }
        if ( !string.IsNullOrEmpty( Family ) )
        {
          try
          {
            richMemo.Font = new Font( Family, (float)editFontSize.Value );
          }
          catch ( ArgumentException )
          {
            // Font no longer installed — keep the control default.
          }
        }
      }
      finally
      {
        m_Populating = false;
      }
    }



    public void SetPlacement( string Geometry )
    {
      if ( !string.IsNullOrEmpty( Geometry ) )
      {
        GR.Forms.WindowStateManager.GeometryFromString( Geometry, this );
      }
    }



    public string CurrentPlacement
    {
      get
      {
        return GR.Forms.WindowStateManager.GeometryToString( this );
      }
    }



    /// <summary>
    /// Writes the box's current text back into the target map — reads .Rtf
    /// ONCE, only if something actually changed. Called at every sync
    /// point (map switch, deactivate, close, project save).
    /// </summary>
    public void FlushToMap()
    {
      if ( ( m_Map == null )
      ||   ( !m_MemoDirty ) )
      {
        return;
      }
      string rtf = ( richMemo.TextLength == 0 ) ? "" : richMemo.Rtf;
      if ( m_Map.MemoRTF != rtf )
      {
        m_Map.MemoRTF = rtf;
      }
      m_MemoDirty = false;
    }



    private void richMemo_TextChanged( object sender, EventArgs e )
    {
      if ( ( m_Populating )
      ||   ( m_Map == null ) )
      {
        return;
      }
      m_MemoDirty = true;
      // Dirty the document immediately (idempotent) — but do NOT read .Rtf
      // here; that happens once per sync in FlushToMap.
      MemoModified?.Invoke();
    }



    private void ToggleStyle( FontStyle Style )
    {
      if ( richMemo.ReadOnly )
      {
        return;
      }
      var current = richMemo.SelectionFont ?? richMemo.Font;
      richMemo.SelectionFont = new Font( current, current.Style ^ Style );
      // No .Rtf read here → the selection is untouched; TextChanged fired
      // by SelectionFont marks the memo dirty.
      richMemo.Focus();
    }



    private void fontPick_Changed( object sender, EventArgs e )
    {
      if ( m_Populating )
      {
        return;
      }
      if ( comboFont.SelectedIndex < 0 )
      {
        return;
      }
      string family = (string)comboFont.SelectedItem;
      int size = (int)editFontSize.Value;

      if ( !richMemo.ReadOnly )
      {
        var styleSource = richMemo.SelectionFont ?? richMemo.Font;
        try
        {
          if ( richMemo.SelectionLength > 0 )
          {
            richMemo.SelectionFont = new Font( family, size, styleSource.Style );
          }
          else
          {
            // No selection: change the base font so new typing uses it.
            richMemo.Font = new Font( family, size, richMemo.Font.Style );
          }
        }
        catch ( ArgumentException )
        {
          // Family can't render at this size/style — ignore the pick.
          return;
        }
      }
      FontPickChanged?.Invoke( family, size );
    }



    private void btnColor_Click( object sender, EventArgs e )
    {
      if ( richMemo.ReadOnly )
      {
        return;
      }
      using ( var dlg = new ColorDialog() )
      {
        dlg.FullOpen = true;
        dlg.Color = richMemo.SelectionColor.IsEmpty ? richMemo.ForeColor : richMemo.SelectionColor;
        if ( dlg.ShowDialog( this ) == DialogResult.OK )
        {
          richMemo.SelectionColor = dlg.Color;
          richMemo.Focus();
        }
      }
    }



    private void SetToolbarEnabled( bool Enabled )
    {
      comboFont.Enabled = Enabled;
      editFontSize.Enabled = Enabled;
      btnBold.Enabled = Enabled;
      btnItalic.Enabled = Enabled;
      btnUnderline.Enabled = Enabled;
      btnColor.Enabled = Enabled;
    }



    protected override void OnDeactivate( EventArgs e )
    {
      // Clicking back into the editor is a sync point — land pending text.
      FlushToMap();
      base.OnDeactivate( e );
    }



    protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
    {
      // Self-contained: this Form owns its keyboard, so the style chords
      // are safe here — no document accelerators to collide with.
      if ( ( richMemo.Focused )
      &&   ( !richMemo.ReadOnly ) )
      {
        switch ( keyData )
        {
          case Keys.Control | Keys.B:
            ToggleStyle( FontStyle.Bold );
            return true;
          case Keys.Control | Keys.I:
            ToggleStyle( FontStyle.Italic );
            return true;
          case Keys.Control | Keys.U:
            ToggleStyle( FontStyle.Underline );
            return true;
        }
      }
      return base.ProcessCmdKey( ref msg, keyData );
    }
  }
}
