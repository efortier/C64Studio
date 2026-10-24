namespace RetroDevStudio.Formats.TextTemplate
{
  public enum TemplateTokenKind
  {
    Text,
    Comment,
    OutputOpen,
    OutputClose,
    StatementOpen,
    StatementClose,
    Identifier,
    Integer,
    String,
    FormatSpec,
    Dot,
    LParen,
    RParen,
    Pipe,
    Colon,
    Plus,
    Minus,
    Star,
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    EndOfFile
  }



  /// <summary>One token of a template. Text tokens are mutable: the whitespace pass trims them.</summary>
  public class TemplateToken
  {
    public TemplateTokenKind    Kind = TemplateTokenKind.Text;
    /// <summary>Text / Comment: the (newline-normalized) content; Identifier: the name; String: the unescaped value; FormatSpec: the spec; Integer: the digits as typed.</summary>
    public string               Text = "";
    public int                  IntValue = 0;
    public int                  Line = 0;
    public int                  Column = 0;
    /// <summary>Made up by the lexer to close a tag that the text never closed (the error is already reported).</summary>
    public bool                 Synthetic = false;



    public TemplateToken( TemplateTokenKind Kind, string Text, int Line, int Column )
    {
      this.Kind   = Kind;
      this.Text   = Text ?? "";
      this.Line   = Line;
      this.Column = Column;
    }



    public bool Is( TemplateTokenKind Kind )
    {
      return this.Kind == Kind;
    }



    /// <summary>True for an identifier spelled like Keyword (case-insensitive).</summary>
    public bool IsKeyword( string Keyword )
    {
      return ( Kind == TemplateTokenKind.Identifier )
          && string.Equals( Text, Keyword, System.StringComparison.OrdinalIgnoreCase );
    }



    /// <summary>How an error message names this token.</summary>
    public string Describe()
    {
      switch ( Kind )
      {
        case TemplateTokenKind.Text:
          return "text";
        case TemplateTokenKind.Comment:
          return "a comment";
        case TemplateTokenKind.OutputOpen:
          return "'{{'";
        case TemplateTokenKind.OutputClose:
          return "'}}'";
        case TemplateTokenKind.StatementOpen:
          return "'{%'";
        case TemplateTokenKind.StatementClose:
          return "'%}'";
        case TemplateTokenKind.Identifier:
          return "'" + Text + "'";
        case TemplateTokenKind.Integer:
          return "number " + Text;
        case TemplateTokenKind.String:
          return "string literal";
        case TemplateTokenKind.FormatSpec:
          return "number format ':" + Text + "'";
        case TemplateTokenKind.Dot:
          return "'.'";
        case TemplateTokenKind.LParen:
          return "'('";
        case TemplateTokenKind.RParen:
          return "')'";
        case TemplateTokenKind.Pipe:
          return "'|'";
        case TemplateTokenKind.Colon:
          return "':'";
        case TemplateTokenKind.Plus:
          return "'+'";
        case TemplateTokenKind.Minus:
          return "'-'";
        case TemplateTokenKind.Star:
          return "'*'";
        case TemplateTokenKind.Equal:
          return "'=='";
        case TemplateTokenKind.NotEqual:
          return "'!='";
        case TemplateTokenKind.Less:
          return "'<'";
        case TemplateTokenKind.LessEqual:
          return "'<='";
        case TemplateTokenKind.Greater:
          return "'>'";
        case TemplateTokenKind.GreaterEqual:
          return "'>='";
        default:
          return "end of file";
      }
    }
  }
}
