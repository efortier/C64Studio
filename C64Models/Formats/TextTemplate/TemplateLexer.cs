using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>
  /// Turns template text into tokens. Text mode looks for the earliest "{{",
  /// "{%" or "{#"; everything else is literal (a lone "{", "$(", "&lt;&lt;" ...).
  /// Inside a tag the usual identifier / number / string / punctuation
  /// tokens apply, tags may span lines, and {% raw %} switches to raw mode
  /// until {% endraw %}. Line breaks in text become Options.NewLine, and the
  /// tag-only-line rule (ApplyWhitespaceRule) runs on the token stream.
  /// Positions are 1-based line/column on the original text, TAB = 1 column.
  /// </summary>
  public class TemplateLexer
  {
    private string                  m_Source = "";
    private TemplateOptions         m_Options = null;
    private List<TemplateError>     m_Errors = null;
    private List<TemplateToken>     m_Tokens = new List<TemplateToken>();
    private List<int>               m_LineStarts = new List<int>();
    private int                     m_Pos = 0;

    private static readonly Regex   s_EndRaw = new Regex( @"\{%\s*endraw\s*%\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant );
    private static readonly Regex   s_LineBreak = new Regex( "\r\n|\r|\n" );



    /// <summary>Tokens of Source, ending with an EndOfFile token; errors are appended to Errors.</summary>
    public static List<TemplateToken> Tokenize( string Source, TemplateOptions Options, List<TemplateError> Errors )
    {
      var lexer = new TemplateLexer( Source ?? "", Options ?? new TemplateOptions(), Errors );
      lexer.Run();
      ApplyWhitespaceRule( lexer.m_Tokens, lexer.m_Options.NewLine );
      lexer.m_Tokens.RemoveAll( t => ( t.Kind == TemplateTokenKind.Comment )
                                  || ( ( t.Kind == TemplateTokenKind.Text ) && ( t.Text.Length == 0 ) ) );
      lexer.m_Tokens.Add( lexer.MakeToken( TemplateTokenKind.EndOfFile, lexer.m_Source.Length ) );
      return lexer.m_Tokens;
    }



    private TemplateLexer( string Source, TemplateOptions Options, List<TemplateError> Errors )
    {
      if ( ( Source.Length > 0 )
      &&   ( Source[0] == '﻿' ) )
      {
        Source = Source.Substring( 1 );
      }
      m_Source  = Source;
      m_Options = Options;
      m_Errors  = Errors ?? new List<TemplateError>();
      ComputeLineStarts();
    }



    // ------------------------------------------------------------- positions

    private void ComputeLineStarts()
    {
      m_LineStarts.Add( 0 );
      for ( int i = 0; i < m_Source.Length; ++i )
      {
        char c = m_Source[i];
        if ( c == '\r' )
        {
          if ( ( i + 1 < m_Source.Length )
          &&   ( m_Source[i + 1] == '\n' ) )
          {
            ++i;
          }
          m_LineStarts.Add( i + 1 );
        }
        else if ( c == '\n' )
        {
          m_LineStarts.Add( i + 1 );
        }
      }
    }



    private int LineIndexOf( int Index )
    {
      int lo = 0;
      int hi = m_LineStarts.Count - 1;
      while ( lo < hi )
      {
        int mid = ( lo + hi + 1 ) / 2;
        if ( m_LineStarts[mid] <= Index )
        {
          lo = mid;
        }
        else
        {
          hi = mid - 1;
        }
      }
      return lo;
    }



    private TemplateToken MakeToken( TemplateTokenKind Kind, int Index )
    {
      return MakeToken( Kind, Index, "" );
    }



    private TemplateToken MakeToken( TemplateTokenKind Kind, int Index, string Text )
    {
      int line = LineIndexOf( Index );
      return new TemplateToken( Kind, Text, line + 1, Index - m_LineStarts[line] + 1 );
    }



    private void Error( int Index, string Message )
    {
      int line = LineIndexOf( Index );
      m_Errors.Add( new TemplateError( line + 1, Index - m_LineStarts[line] + 1, Message ) );
    }



    // --------------------------------------------------------------- text mode

    private void Run()
    {
      int textStart = 0;
      while ( true )
      {
        int tagIndex = FindNextTag( m_Pos );
        if ( tagIndex < 0 )
        {
          AddText( textStart, m_Source.Length );
          return;
        }
        AddText( textStart, tagIndex );
        char kind = m_Source[tagIndex + 1];
        m_Pos = tagIndex + 2;
        if ( kind == '#' )
        {
          int end = m_Source.IndexOf( "#}", m_Pos, StringComparison.Ordinal );
          if ( end < 0 )
          {
            Error( tagIndex, "'{#' is never closed with '#}'" );
            m_Tokens.Add( MakeToken( TemplateTokenKind.Comment, tagIndex ) );
            m_Pos = m_Source.Length;
          }
          else
          {
            m_Tokens.Add( MakeToken( TemplateTokenKind.Comment, tagIndex ) );
            m_Pos = end + 2;
          }
          textStart = m_Pos;
          continue;
        }
        bool output = ( kind == '{' );
        m_Tokens.Add( MakeToken( output ? TemplateTokenKind.OutputOpen : TemplateTokenKind.StatementOpen, tagIndex ) );
        int firstTagToken = m_Tokens.Count;
        LexTag( output, tagIndex );
        if ( ( !output )
        &&   IsPlainRawTag( firstTagToken ) )
        {
          LexRawBlock( tagIndex );
        }
        textStart = m_Pos;
      }
    }



    private int FindNextTag( int From )
    {
      for ( int i = From; i + 1 < m_Source.Length; ++i )
      {
        if ( m_Source[i] == '{' )
        {
          char next = m_Source[i + 1];
          if ( ( next == '{' )
          ||   ( next == '%' )
          ||   ( next == '#' ) )
          {
            return i;
          }
        }
      }
      return -1;
    }



    private void AddText( int From, int To )
    {
      if ( To <= From )
      {
        return;
      }
      string text = s_LineBreak.Replace( m_Source.Substring( From, To - From ), m_Options.NewLine );
      m_Tokens.Add( MakeToken( TemplateTokenKind.Text, From, text ) );
    }



    /// <summary>Exactly "{% raw %}": the tokens after the opener are the identifier raw and a real close.</summary>
    private bool IsPlainRawTag( int FirstTagToken )
    {
      return ( m_Tokens.Count == FirstTagToken + 2 )
          && m_Tokens[FirstTagToken].IsKeyword( "raw" )
          && m_Tokens[FirstTagToken + 1].Is( TemplateTokenKind.StatementClose )
          && ( !m_Tokens[FirstTagToken + 1].Synthetic );
    }



    private void LexRawBlock( int OpenerIndex )
    {
      var match = s_EndRaw.Match( m_Source, m_Pos );
      if ( !match.Success )
      {
        Error( OpenerIndex, "'{% raw %}' is never closed with '{% endraw %}'" );
        AddText( m_Pos, m_Source.Length );
        m_Pos = m_Source.Length;
        AddSyntheticTag( m_Pos, "endraw" );
        return;
      }
      AddText( m_Pos, match.Index );
      m_Tokens.Add( MakeToken( TemplateTokenKind.StatementOpen, match.Index ) );
      m_Tokens.Add( MakeToken( TemplateTokenKind.Identifier, match.Index + 2, "endraw" ) );
      m_Tokens.Add( MakeToken( TemplateTokenKind.StatementClose, match.Index + match.Length - 2 ) );
      m_Pos = match.Index + match.Length;
    }



    private void AddSyntheticTag( int Index, string Keyword )
    {
      var open = MakeToken( TemplateTokenKind.StatementOpen, Index );
      open.Synthetic = true;
      m_Tokens.Add( open );
      var keyword = MakeToken( TemplateTokenKind.Identifier, Index, Keyword );
      keyword.Synthetic = true;
      m_Tokens.Add( keyword );
      var close = MakeToken( TemplateTokenKind.StatementClose, Index );
      close.Synthetic = true;
      m_Tokens.Add( close );
    }



    // ---------------------------------------------------------------- tag mode

    private void LexTag( bool Output, int OpenerIndex )
    {
      while ( true )
      {
        SkipWhitespace();
        if ( m_Pos >= m_Source.Length )
        {
          Error( OpenerIndex, Output ? "'{{' is never closed with '}}'" : "'{%' is never closed with '%}'" );
          var close = MakeToken( Output ? TemplateTokenKind.OutputClose : TemplateTokenKind.StatementClose, m_Source.Length );
          close.Synthetic = true;
          m_Tokens.Add( close );
          return;
        }
        char c = m_Source[m_Pos];
        char n = ( m_Pos + 1 < m_Source.Length ) ? m_Source[m_Pos + 1] : '\0';
        if ( ( c == '}' )
        &&   ( n == '}' ) )
        {
          if ( !Output )
          {
            Error( m_Pos, "expected '%}' but found '}}'" );
          }
          m_Tokens.Add( MakeToken( Output ? TemplateTokenKind.OutputClose : TemplateTokenKind.StatementClose, m_Pos ) );
          m_Pos += 2;
          return;
        }
        if ( ( c == '%' )
        &&   ( n == '}' ) )
        {
          if ( Output )
          {
            Error( m_Pos, "expected '}}' but found '%}'" );
          }
          m_Tokens.Add( MakeToken( Output ? TemplateTokenKind.OutputClose : TemplateTokenKind.StatementClose, m_Pos ) );
          m_Pos += 2;
          return;
        }
        if ( IsIdentifierStart( c ) )
        {
          LexIdentifier();
          continue;
        }
        if ( char.IsDigit( c ) )
        {
          LexNumber();
          continue;
        }
        switch ( c )
        {
          case '$':
            LexDollarHex();
            break;
          case '"':
            LexString();
            break;
          case ':':
            if ( Output )
            {
              LexFormatSpec();
            }
            else
            {
              Error( m_Pos, "':' format specs are only allowed in {{ }} output tags" );
              m_Tokens.Add( MakeToken( TemplateTokenKind.Colon, m_Pos ) );
              ++m_Pos;
            }
            break;
          case '.':
            AddSingle( TemplateTokenKind.Dot );
            break;
          case '(':
            AddSingle( TemplateTokenKind.LParen );
            break;
          case ')':
            AddSingle( TemplateTokenKind.RParen );
            break;
          case '|':
            AddSingle( TemplateTokenKind.Pipe );
            break;
          case '+':
            AddSingle( TemplateTokenKind.Plus );
            break;
          case '-':
            AddSingle( TemplateTokenKind.Minus );
            break;
          case '*':
            AddSingle( TemplateTokenKind.Star );
            break;
          case '=':
            if ( n == '=' )
            {
              m_Tokens.Add( MakeToken( TemplateTokenKind.Equal, m_Pos ) );
              m_Pos += 2;
            }
            else
            {
              Error( m_Pos, "'=' is not an operator; use '==' to compare" );
              AddSingle( TemplateTokenKind.Equal );
            }
            break;
          case '!':
            if ( n == '=' )
            {
              m_Tokens.Add( MakeToken( TemplateTokenKind.NotEqual, m_Pos ) );
              m_Pos += 2;
            }
            else
            {
              Error( m_Pos, "unexpected character '!'" );
              ++m_Pos;
            }
            break;
          case '<':
            if ( n == '=' )
            {
              m_Tokens.Add( MakeToken( TemplateTokenKind.LessEqual, m_Pos ) );
              m_Pos += 2;
            }
            else
            {
              AddSingle( TemplateTokenKind.Less );
            }
            break;
          case '>':
            if ( n == '=' )
            {
              m_Tokens.Add( MakeToken( TemplateTokenKind.GreaterEqual, m_Pos ) );
              m_Pos += 2;
            }
            else
            {
              AddSingle( TemplateTokenKind.Greater );
            }
            break;
          default:
            Error( m_Pos, "unexpected character '" + c + "'" );
            ++m_Pos;
            break;
        }
      }
    }



    private void AddSingle( TemplateTokenKind Kind )
    {
      m_Tokens.Add( MakeToken( Kind, m_Pos ) );
      ++m_Pos;
    }



    private void SkipWhitespace()
    {
      while ( m_Pos < m_Source.Length )
      {
        char c = m_Source[m_Pos];
        if ( ( c == ' ' )
        ||   ( c == '\t' )
        ||   ( c == '\r' )
        ||   ( c == '\n' ) )
        {
          ++m_Pos;
        }
        else
        {
          return;
        }
      }
    }



    private static bool IsIdentifierStart( char c )
    {
      return ( ( c >= 'A' ) && ( c <= 'Z' ) )
          || ( ( c >= 'a' ) && ( c <= 'z' ) )
          || ( c == '_' );
    }



    private static bool IsIdentifierChar( char c )
    {
      return IsIdentifierStart( c ) || ( ( c >= '0' ) && ( c <= '9' ) );
    }



    private static bool IsHexDigit( char c )
    {
      return ( ( c >= '0' ) && ( c <= '9' ) )
          || ( ( c >= 'A' ) && ( c <= 'F' ) )
          || ( ( c >= 'a' ) && ( c <= 'f' ) );
    }



    private void LexIdentifier()
    {
      int start = m_Pos;
      while ( ( m_Pos < m_Source.Length )
      &&      IsIdentifierChar( m_Source[m_Pos] ) )
      {
        ++m_Pos;
      }
      m_Tokens.Add( MakeToken( TemplateTokenKind.Identifier, start, m_Source.Substring( start, m_Pos - start ) ) );
    }



    private void LexNumber()
    {
      int start = m_Pos;
      if ( ( m_Source[m_Pos] == '0' )
      &&   ( m_Pos + 1 < m_Source.Length )
      &&   ( ( m_Source[m_Pos + 1] == 'x' ) || ( m_Source[m_Pos + 1] == 'X' ) ) )
      {
        m_Pos += 2;
        LexHexDigits( start, "'0x' must be followed by hex digits" );
        return;
      }
      while ( ( m_Pos < m_Source.Length )
      &&      char.IsDigit( m_Source[m_Pos] ) )
      {
        ++m_Pos;
      }
      string digits = m_Source.Substring( start, m_Pos - start );
      var token = MakeToken( TemplateTokenKind.Integer, start, digits );
      long value;
      if ( ( !long.TryParse( digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value ) )
      ||   ( value > int.MaxValue ) )
      {
        Error( start, "number " + digits + " does not fit in 32 bits" );
        value = 0;
      }
      token.IntValue = (int)value;
      m_Tokens.Add( token );
    }



    private void LexDollarHex()
    {
      int start = m_Pos;
      ++m_Pos;
      LexHexDigits( start, "'$' must be followed by hex digits" );
    }



    /// <summary>The hex digits after a "$" or "0x" prefix at Start (m_Pos is past the prefix).</summary>
    private void LexHexDigits( int Start, string MissingDigitsMessage )
    {
      int digitsStart = m_Pos;
      while ( ( m_Pos < m_Source.Length )
      &&      IsHexDigit( m_Source[m_Pos] ) )
      {
        ++m_Pos;
      }
      string text = m_Source.Substring( Start, m_Pos - Start );
      var token = MakeToken( TemplateTokenKind.Integer, Start, text );
      if ( m_Pos == digitsStart )
      {
        Error( Start, MissingDigitsMessage );
      }
      else
      {
        string digits = m_Source.Substring( digitsStart, m_Pos - digitsStart );
        ulong value;
        if ( ( !ulong.TryParse( digits, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out value ) )
        ||   ( value > 0xFFFFFFFF ) )
        {
          Error( Start, "number " + text + " does not fit in 32 bits" );
          value = 0;
        }
        token.IntValue = unchecked( (int)(uint)value );
      }
      m_Tokens.Add( token );
    }



    private void LexString()
    {
      int start = m_Pos;
      ++m_Pos;
      var sb = new StringBuilder();
      while ( true )
      {
        if ( ( m_Pos >= m_Source.Length )
        ||   ( m_Source[m_Pos] == '\r' )
        ||   ( m_Source[m_Pos] == '\n' ) )
        {
          Error( start, "string literal is never closed" );
          break;
        }
        char c = m_Source[m_Pos];
        if ( c == '"' )
        {
          ++m_Pos;
          break;
        }
        if ( c == '\\' )
        {
          if ( m_Pos + 1 >= m_Source.Length )
          {
            Error( start, "string literal is never closed" );
            ++m_Pos;
            break;
          }
          char e = m_Source[m_Pos + 1];
          switch ( e )
          {
            case '"':
              sb.Append( '"' );
              break;
            case '\\':
              sb.Append( '\\' );
              break;
            case 'n':
              sb.Append( m_Options.NewLine );
              break;
            case 't':
              sb.Append( '\t' );
              break;
            default:
              Error( m_Pos, "unknown escape '\\" + e + "' in string literal (use \\\" \\\\ \\n \\t)" );
              sb.Append( e );
              break;
          }
          m_Pos += 2;
          continue;
        }
        sb.Append( c );
        ++m_Pos;
      }
      m_Tokens.Add( MakeToken( TemplateTokenKind.String, start, sb.ToString() ) );
    }



    /// <summary>":" and everything up to the next "|" or the tag's closer, trimmed.</summary>
    private void LexFormatSpec()
    {
      int colon = m_Pos;
      ++m_Pos;
      int start = m_Pos;
      while ( m_Pos < m_Source.Length )
      {
        char c = m_Source[m_Pos];
        char n = ( m_Pos + 1 < m_Source.Length ) ? m_Source[m_Pos + 1] : '\0';
        if ( ( c == '|' )
        ||   ( ( c == '}' ) && ( n == '}' ) )
        ||   ( ( c == '%' ) && ( n == '}' ) ) )
        {
          break;
        }
        ++m_Pos;
      }
      string spec = m_Source.Substring( start, m_Pos - start ).Trim();
      if ( spec.Length == 0 )
      {
        Error( colon, "empty number format after ':'" );
      }
      else if ( ( spec.IndexOf( '"' ) >= 0 )
      ||        ( spec.IndexOf( '\r' ) >= 0 )
      ||        ( spec.IndexOf( '\n' ) >= 0 ) )
      {
        Error( colon, "a number format may not contain quotes or line breaks" );
      }
      m_Tokens.Add( MakeToken( TemplateTokenKind.FormatSpec, colon, spec ) );
    }



    // ------------------------------------------------------ tag-only lines

    /// <summary>
    /// A line holding nothing but statement tags and comments (and blanks)
    /// vanishes from the output together with its line break: the blank
    /// indentation before the group and the line break after it are cut. An
    /// output tag or any other text on the line keeps the whole line as is.
    /// Works on the normalized token stream with a running "at line start"
    /// state, never a look-behind.
    /// </summary>
    public static void ApplyWhitespaceRule( List<TemplateToken> Tokens, string NewLine )
    {
      bool atLineStart = true;
      int i = 0;
      while ( i < Tokens.Count )
      {
        var token = Tokens[i];
        if ( token.Is( TemplateTokenKind.Text ) )
        {
          atLineStart = EndsAtLineStart( token.Text, atLineStart, NewLine );
          ++i;
        }
        else if ( token.Is( TemplateTokenKind.OutputOpen ) )
        {
          atLineStart = false;
          i = IndexAfterClose( Tokens, i, TemplateTokenKind.OutputClose );
        }
        else if ( IsTag( token ) )
        {
          int groupStart = i;
          var separators = new List<TemplateToken>();
          int j = i;
          while ( j < Tokens.Count )
          {
            if ( Tokens[j].Is( TemplateTokenKind.Comment ) )
            {
              ++j;
            }
            else if ( Tokens[j].Is( TemplateTokenKind.StatementOpen ) )
            {
              j = IndexAfterClose( Tokens, j, TemplateTokenKind.StatementClose );
            }
            else
            {
              break;
            }
            if ( ( j + 1 < Tokens.Count )
            &&   ( Tokens[j].Is( TemplateTokenKind.Text ) )
            &&   IsBlank( Tokens[j].Text )
            &&   IsTag( Tokens[j + 1] ) )
            {
              separators.Add( Tokens[j] );
              ++j;
            }
          }
          bool cleanBefore = atLineStart;
          bool cleanAfter = false;
          int cut = 0;
          if ( j >= Tokens.Count )
          {
            cleanAfter = true;
          }
          else if ( Tokens[j].Is( TemplateTokenKind.Text ) )
          {
            string after = Tokens[j].Text;
            int firstBreak = after.IndexOf( NewLine, StringComparison.Ordinal );
            if ( ( firstBreak >= 0 )
            &&   IsBlank( after.Substring( 0, firstBreak ) ) )
            {
              cleanAfter = true;
              cut = firstBreak + NewLine.Length;
            }
            else if ( ( firstBreak < 0 )
            &&        ( j == Tokens.Count - 1 )
            &&        IsBlank( after ) )
            {
              cleanAfter = true;
              cut = after.Length;
            }
          }
          if ( cleanBefore && cleanAfter )
          {
            if ( ( groupStart > 0 )
            &&   ( Tokens[groupStart - 1].Is( TemplateTokenKind.Text ) ) )
            {
              var previous = Tokens[groupStart - 1];
              int lastBreak = previous.Text.LastIndexOf( NewLine, StringComparison.Ordinal );
              previous.Text = ( lastBreak >= 0 ) ? previous.Text.Substring( 0, lastBreak + NewLine.Length ) : "";
            }
            foreach ( var separator in separators )
            {
              separator.Text = "";
            }
            if ( j < Tokens.Count )
            {
              Tokens[j].Text = Tokens[j].Text.Substring( cut );
            }
            atLineStart = true;
          }
          i = j;
        }
        else
        {
          ++i;
        }
      }
    }



    private static bool IsTag( TemplateToken Token )
    {
      return Token.Is( TemplateTokenKind.Comment ) || Token.Is( TemplateTokenKind.StatementOpen );
    }



    private static bool IsBlank( string Text )
    {
      foreach ( char c in Text )
      {
        if ( ( c != ' ' )
        &&   ( c != '\t' ) )
        {
          return false;
        }
      }
      return true;
    }



    private static bool EndsAtLineStart( string Text, bool AtLineStart, string NewLine )
    {
      int lastBreak = Text.LastIndexOf( NewLine, StringComparison.Ordinal );
      if ( lastBreak >= 0 )
      {
        return IsBlank( Text.Substring( lastBreak + NewLine.Length ) );
      }
      return AtLineStart && IsBlank( Text );
    }



    /// <summary>Index right after the first Close token following Open (Tokens.Count when there is none).</summary>
    private static int IndexAfterClose( List<TemplateToken> Tokens, int Open, TemplateTokenKind Close )
    {
      for ( int i = Open + 1; i < Tokens.Count; ++i )
      {
        if ( Tokens[i].Is( Close ) )
        {
          return i + 1;
        }
      }
      return Tokens.Count;
    }
  }
}
