using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using RetroDevStudio.Types;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// The memo a custom export's template is typed in. Hosts the source
  /// editor control (line numbers, real tabs, multi-level undo, the template
  /// tags highlighted) and a plain text box behind one small API, so the
  /// editor can be swapped with UsePlainTextBox should the source editor
  /// misbehave here. Text set from code raises TextChanged like typing does
  /// and clears the undo history (a populated memo must never "undo" into
  /// another definition's text); a repeated TextChanged with unchanged text
  /// (the source editor raises one on highlight refreshes) is swallowed.
  /// </summary>
  public partial class TemplateMemo : UserControl
  {
    private bool                  m_UsePlainTextBox = false;
    private string                m_LastText = "";
    private TextStyle             m_OutputStyle    = new TextStyle( Brushes.DarkCyan, null, FontStyle.Regular );
    private TextStyle             m_StatementStyle = new TextStyle( Brushes.DarkOrange, null, FontStyle.Bold );
    private TextStyle             m_CommentStyle   = new TextStyle( Brushes.Gray, null, FontStyle.Italic );

    private static readonly Regex s_CommentPattern   = new Regex( @"\{#.*?#\}", RegexOptions.Singleline | RegexOptions.Compiled );
    private static readonly Regex s_StatementPattern = new Regex( @"\{%.*?%\}", RegexOptions.Singleline | RegexOptions.Compiled );
    private static readonly Regex s_OutputPattern    = new Regex( @"\{\{.*?\}\}", RegexOptions.Singleline | RegexOptions.Compiled );



    /// <summary>Raised when the text differs from the last raised/set text - by typing or by setting Text.</summary>
    public new event EventHandler TextChanged;



    public TemplateMemo()
    {
      InitializeComponent();
      editFast.TextChanged  += editFast_TextChanged;
      editPlain.TextChanged += editPlain_TextChanged;
    }



    /// <summary>Which editor shows: false = the source editor control, true = a plain text box. Switching keeps the text.</summary>
    [Browsable( true )]
    [DefaultValue( false )]
    public bool UsePlainTextBox
    {
      get
      {
        return m_UsePlainTextBox;
      }
      set
      {
        if ( value == m_UsePlainTextBox )
        {
          return;
        }
        string text = Text;
        m_UsePlainTextBox = value;
        if ( m_UsePlainTextBox )
        {
          editPlain.Text = text;
        }
        else
        {
          editFast.Text = text;
        }
        editFast.Visible  = !m_UsePlainTextBox;
        editPlain.Visible = m_UsePlainTextBox;
        ClearUndo();
      }
    }



    [Browsable( false )]
    [DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
    public override string Text
    {
      get
      {
        return m_UsePlainTextBox ? editPlain.Text : editFast.Text;
      }
      set
      {
        string text = value ?? "";
        if ( m_UsePlainTextBox )
        {
          editPlain.Text = text;
        }
        else
        {
          editFast.Text = text;
        }
        ClearUndo();
        RaiseTextChangedIfDifferent();
      }
    }



    [Browsable( false )]
    public bool CanUndo
    {
      get
      {
        return m_UsePlainTextBox ? editPlain.CanUndo : editFast.UndoEnabled;
      }
    }



    [Browsable( false )]
    public bool CanRedo
    {
      get
      {
        return m_UsePlainTextBox ? false : editFast.RedoEnabled;
      }
    }



    /// <summary>True while the keyboard belongs to this memo.</summary>
    [Browsable( false )]
    public bool EditorFocused
    {
      get
      {
        return ContainsFocus;
      }
    }



    public void Undo()
    {
      if ( m_UsePlainTextBox )
      {
        editPlain.Undo();
      }
      else
      {
        editFast.Undo();
      }
    }



    public void Redo()
    {
      if ( !m_UsePlainTextBox )
      {
        editFast.Redo();
      }
    }



    public void ClearUndo()
    {
      if ( m_UsePlainTextBox )
      {
        editPlain.ClearUndo();
      }
      else
      {
        editFast.ClearUndo();
      }
    }



    /// <summary>Inserts Text at the caret (replacing a selection), as typing it would.</summary>
    public void InsertAtCaret( string Text )
    {
      if ( string.IsNullOrEmpty( Text ) )
      {
        return;
      }
      if ( m_UsePlainTextBox )
      {
        editPlain.SelectedText = Text;
        editPlain.Focus();
      }
      else
      {
        editFast.InsertText( Text );
        editFast.Focus();
      }
    }



    /// <summary>Puts the caret on the 1-based Line and scrolls it into view.</summary>
    public void GoToLine( int Line )
    {
      if ( Line < 1 )
      {
        return;
      }
      if ( m_UsePlainTextBox )
      {
        int index = editPlain.GetFirstCharIndexFromLine( Line - 1 );
        if ( index < 0 )
        {
          return;
        }
        editPlain.Select( index, 0 );
        editPlain.ScrollToCaret();
        editPlain.Focus();
      }
      else
      {
        if ( Line > editFast.LinesCount )
        {
          return;
        }
        editFast.Navigate( Line - 1 );
        editFast.Focus();
      }
    }



    /// <summary>Fonts and colours from the settings: the source editor's font, the code colours, the template tags in their own colours.</summary>
    public void ApplyTheme( StudioCore Core )
    {
      if ( Core == null )
      {
        return;
      }
      var font = new Font( Core.Settings.SourceFontFamily, Core.Settings.SourceFontSize, Core.Settings.SourceFontStyle );
      editFast.Font  = font;
      editPlain.Font = font;

      Color back = GR.Color.Helper.FromARGB( Core.Settings.BGColor( ColorableElement.EMPTY_SPACE ) );
      Color fore = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.CODE ) );
      editFast.BackColor         = back;
      editFast.ForeColor         = fore;
      editFast.IndentBackColor   = back;
      editFast.PaddingBackColor  = back;
      editFast.LineNumberColor   = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.COMMENT ) );
      editPlain.BackColor        = back;
      editPlain.ForeColor        = fore;
      // A caret that shows on the editor background (same rule as the source editor).
      editFast.CaretColor = ( ( 0.2126 * back.R + 0.7152 * back.G + 0.0722 * back.B ) < 127.5 ) ? Color.White : Color.Black;
      Color selection = GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.SELECTED_TEXT ) );
      editFast.SelectionColor = Color.FromArgb( 60, selection.R, selection.G, selection.B );

      m_OutputStyle    = new TextStyle( new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.LITERAL_STRING ) ) ), null, FontStyle.Regular );
      m_StatementStyle = new TextStyle( new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.PSEUDO_OP ) ) ), null, FontStyle.Bold );
      m_CommentStyle   = new TextStyle( new SolidBrush( GR.Color.Helper.FromARGB( Core.Settings.FGColor( ColorableElement.COMMENT ) ) ), null, FontStyle.Italic );
      editFast.ClearStylesBuffer();
      Highlight( editFast.Range );
    }



    private void editFast_TextChanged( object sender, TextChangedEventArgs e )
    {
      Highlight( e.ChangedRange );
      RaiseTextChangedIfDifferent();
    }



    private void editPlain_TextChanged( object sender, EventArgs e )
    {
      RaiseTextChangedIfDifferent();
    }



    private void Highlight( FastColoredTextBoxNS.Range Range )
    {
      Range.ClearStyle( m_CommentStyle, m_StatementStyle, m_OutputStyle );
      Range.SetStyle( m_CommentStyle, s_CommentPattern );
      Range.SetStyle( m_StatementStyle, s_StatementPattern );
      Range.SetStyle( m_OutputStyle, s_OutputPattern );
    }



    private void RaiseTextChangedIfDifferent()
    {
      string text = Text;
      if ( text == m_LastText )
      {
        return;
      }
      m_LastText = text;
      if ( TextChanged != null )
      {
        TextChanged( this, EventArgs.Empty );
      }
    }



    protected override void OnGotFocus( EventArgs e )
    {
      base.OnGotFocus( e );
      if ( m_UsePlainTextBox )
      {
        editPlain.Focus();
      }
      else
      {
        editFast.Focus();
      }
    }
  }
}
