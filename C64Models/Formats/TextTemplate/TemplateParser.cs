using System;
using System.Collections.Generic;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>
  /// Builds the node tree from the token stream. Errors are collected, never
  /// thrown: inside a tag the first error wins and the rest of the tag is
  /// skipped, a mismatched closer closes the open block (so the structure
  /// after it stays sane), and an unclosed block is reported at its opener
  /// when the file ends. Nesting past Options.MaxNesting stops the parse.
  /// </summary>
  public class TemplateParser
  {
    private List<TemplateToken>     m_Tokens = null;
    private int                     m_Index = 0;
    private TemplateOptions         m_Options = null;
    private List<TemplateError>     m_Errors = null;
    private int                     m_Depth = 0;
    private bool                    m_Aborted = false;

    private static readonly string[]  s_ForStops = { "endfor", "endif", "elif", "else" };
    private static readonly string[]  s_IfStops  = { "endif", "elif", "else", "endfor" };
    private const string              STATEMENTS = "for, if, elif, else, endif, endfor, raw";



    /// <summary>The nodes of Tokens (which must end with EndOfFile); errors are appended to Errors.</summary>
    public static List<TemplateNode> Parse( List<TemplateToken> Tokens, TemplateOptions Options, List<TemplateError> Errors )
    {
      var parser = new TemplateParser( Tokens, Options ?? new TemplateOptions(), Errors );
      var nodes = new List<TemplateNode>();
      TemplateToken stop;
      parser.ParseNodes( nodes, null, out stop );
      return nodes;
    }



    private TemplateParser( List<TemplateToken> Tokens, TemplateOptions Options, List<TemplateError> Errors )
    {
      m_Tokens  = Tokens;
      m_Options = Options;
      m_Errors  = Errors ?? new List<TemplateError>();
      if ( ( m_Tokens.Count == 0 )
      ||   ( !m_Tokens[m_Tokens.Count - 1].Is( TemplateTokenKind.EndOfFile ) ) )
      {
        m_Tokens.Add( new TemplateToken( TemplateTokenKind.EndOfFile, "", 1, 1 ) );
      }
    }



    // ---------------------------------------------------------------- tokens

    private TemplateToken Current
    {
      get
      {
        return m_Tokens[m_Index];
      }
    }



    private TemplateToken Peek( int Offset )
    {
      return m_Tokens[Math.Min( m_Index + Offset, m_Tokens.Count - 1 )];
    }



    private void Advance()
    {
      if ( m_Index < m_Tokens.Count - 1 )
      {
        ++m_Index;
      }
    }



    private void Error( TemplateToken At, string Message )
    {
      m_Errors.Add( new TemplateError( At.Line, At.Column, Message ) );
    }



    /// <summary>The lower-case keyword Token spells, or null for anything else.</summary>
    private static string KeywordOf( TemplateToken Token )
    {
      if ( ( Token.Is( TemplateTokenKind.Identifier ) )
      &&   TemplateSchema.IsKeyword( Token.Text ) )
      {
        return Token.Text.ToLowerInvariant();
      }
      return null;
    }



    /// <summary>Skips to and past the next Close token (the tag's closer always exists, synthetic if need be).</summary>
    private void SkipTag( TemplateTokenKind Close )
    {
      while ( ( !Current.Is( Close ) )
      &&      ( !Current.Is( TemplateTokenKind.EndOfFile ) ) )
      {
        Advance();
      }
      Advance();
    }



    private bool ExpectClose( TemplateTokenKind Close )
    {
      if ( Current.Is( Close ) )
      {
        Advance();
        return true;
      }
      Error( Current, "expected '" + ( ( Close == TemplateTokenKind.OutputClose ) ? "}}" : "%}" ) + "' but found " + Current.Describe() );
      SkipTag( Close );
      return false;
    }



    /// <summary>Consumes "{% keyword %}" at Current, reporting anything between the keyword and the closer.</summary>
    private void ConsumeClosingTag()
    {
      Advance();
      Advance();
      ExpectClose( TemplateTokenKind.StatementClose );
    }



    private bool EnterBlock( TemplateToken Opener )
    {
      ++m_Depth;
      if ( m_Depth > m_Options.MaxNesting )
      {
        Error( Opener, "nesting deeper than " + m_Options.MaxNesting + " levels" );
        m_Aborted = true;
        m_Index = m_Tokens.Count - 1;
        return false;
      }
      return true;
    }



    private void LeaveBlock()
    {
      --m_Depth;
    }



    // ----------------------------------------------------------------- nodes

    /// <summary>
    /// Parses nodes into Into until the end of the file or a statement whose
    /// keyword is one of Stops; that statement is left unconsumed and handed
    /// back as StopTag (null at the end of the file).
    /// </summary>
    private void ParseNodes( List<TemplateNode> Into, string[] Stops, out TemplateToken StopTag )
    {
      StopTag = null;
      while ( !m_Aborted )
      {
        var token = Current;
        switch ( token.Kind )
        {
          case TemplateTokenKind.EndOfFile:
            return;
          case TemplateTokenKind.Text:
            Into.Add( new TextNode() { Text = token.Text, Line = token.Line, Column = token.Column } );
            Advance();
            break;
          case TemplateTokenKind.OutputOpen:
            {
              var node = ParseOutput();
              if ( node != null )
              {
                Into.Add( node );
              }
            }
            break;
          case TemplateTokenKind.StatementOpen:
            {
              string keyword = KeywordOf( Peek( 1 ) );
              if ( ( keyword != null )
              &&   ( Stops != null )
              &&   ( Array.IndexOf( Stops, keyword ) >= 0 ) )
              {
                StopTag = token;
                return;
              }
              ParseStatement( Into );
            }
            break;
          default:
            Error( token, "unexpected " + token.Describe() );
            Advance();
            break;
        }
      }
    }



    private void ParseStatement( List<TemplateNode> Into )
    {
      var open = Current;
      var first = Peek( 1 );
      string keyword = KeywordOf( first );
      switch ( keyword )
      {
        case "for":
          ParseFor( Into );
          return;
        case "if":
          ParseIf( Into );
          return;
        case "raw":
          ParseRaw( Into );
          return;
        case "endfor":
          Error( open, "'endfor' without a matching 'for'" );
          SkipTag( TemplateTokenKind.StatementClose );
          return;
        case "endif":
          Error( open, "'endif' without a matching 'if'" );
          SkipTag( TemplateTokenKind.StatementClose );
          return;
        case "elif":
          Error( open, "'elif' without a matching 'if'" );
          SkipTag( TemplateTokenKind.StatementClose );
          return;
        case "else":
          Error( open, "'else' without a matching 'if'" );
          SkipTag( TemplateTokenKind.StatementClose );
          return;
        case "endraw":
          Error( open, "'endraw' without a matching 'raw'" );
          SkipTag( TemplateTokenKind.StatementClose );
          return;
      }
      if ( first.Is( TemplateTokenKind.Identifier ) )
      {
        Error( first, "unknown statement '" + first.Text + "' (expected " + STATEMENTS + ")" );
      }
      else if ( first.Is( TemplateTokenKind.StatementClose ) )
      {
        Error( first, "empty statement (expected " + STATEMENTS + ")" );
      }
      else
      {
        string shown = ( first.Text.Length > 0 ) ? first.Text : first.Describe();
        Error( first, "unknown statement '" + shown + "' (expected " + STATEMENTS
                    + ") - to output a literal '{%' write {{ \"{%\" }} or wrap the text in {% raw %} ... {% endraw %}" );
      }
      SkipTag( TemplateTokenKind.StatementClose );
    }



    private void ParseFor( List<TemplateNode> Into )
    {
      var open = Current;
      Advance();
      Advance();
      var node = new ForNode() { Line = open.Line, Column = open.Column };
      bool headerOk = true;
      if ( !Current.Is( TemplateTokenKind.Identifier ) )
      {
        Error( Current, "'for' needs a loop variable: for <name> in <list>" );
        headerOk = false;
        SkipTag( TemplateTokenKind.StatementClose );
      }
      else
      {
        node.Variable       = Current.Text;
        node.VariableLine   = Current.Line;
        node.VariableColumn = Current.Column;
        Advance();
        if ( !Current.IsKeyword( "in" ) )
        {
          Error( Current, "expected 'in' after the loop variable '" + node.Variable + "'" );
          headerOk = false;
          SkipTag( TemplateTokenKind.StatementClose );
        }
        else
        {
          Advance();
          node.Source = ParseExpression();
          if ( node.Source == null )
          {
            headerOk = false;
            SkipTag( TemplateTokenKind.StatementClose );
          }
          else if ( !ExpectClose( TemplateTokenKind.StatementClose ) )
          {
            headerOk = false;
          }
        }
      }
      if ( !EnterBlock( open ) )
      {
        return;
      }
      TemplateToken stop;
      ParseNodes( node.Body, s_ForStops, out stop );
      LeaveBlock();
      if ( m_Aborted )
      {
        return;
      }
      if ( stop == null )
      {
        Error( open, "'{% for %}' is never closed with '{% endfor %}'" );
      }
      else
      {
        string keyword = KeywordOf( Peek( 1 ) );
        if ( keyword != "endfor" )
        {
          Error( stop, "'" + keyword + "' closes the 'for' block opened at line " + open.Line + ", col " + open.Column + "; expected 'endfor'" );
        }
        ConsumeClosingTag();
      }
      if ( headerOk )
      {
        Into.Add( node );
      }
    }



    private void ParseIf( List<TemplateNode> Into )
    {
      var open = Current;
      Advance();
      Advance();
      var node = new IfNode() { Line = open.Line, Column = open.Column };
      var branch = new IfBranch();
      node.Branches.Add( branch );
      bool headerOk = true;
      branch.Condition = ParseExpression();
      if ( branch.Condition == null )
      {
        headerOk = false;
        SkipTag( TemplateTokenKind.StatementClose );
      }
      else if ( !ExpectClose( TemplateTokenKind.StatementClose ) )
      {
        headerOk = false;
      }
      if ( !EnterBlock( open ) )
      {
        return;
      }
      var body = branch.Body;
      bool seenElse = false;
      while ( true )
      {
        TemplateToken stop;
        ParseNodes( body, s_IfStops, out stop );
        if ( m_Aborted )
        {
          LeaveBlock();
          return;
        }
        if ( stop == null )
        {
          Error( open, "'{% if %}' is never closed with '{% endif %}'" );
          break;
        }
        string keyword = KeywordOf( Peek( 1 ) );
        if ( keyword == "endif" )
        {
          ConsumeClosingTag();
          break;
        }
        if ( keyword == "elif" )
        {
          if ( seenElse )
          {
            Error( stop, "'elif' after 'else' in the 'if' block opened at line " + open.Line + ", col " + open.Column );
          }
          Advance();
          Advance();
          var next = new IfBranch();
          next.Condition = ParseExpression();
          if ( next.Condition == null )
          {
            headerOk = false;
            SkipTag( TemplateTokenKind.StatementClose );
          }
          else if ( !ExpectClose( TemplateTokenKind.StatementClose ) )
          {
            headerOk = false;
          }
          if ( !seenElse )
          {
            node.Branches.Add( next );
          }
          body = next.Body;
          continue;
        }
        if ( keyword == "else" )
        {
          if ( seenElse )
          {
            Error( stop, "'else' after 'else' in the 'if' block opened at line " + open.Line + ", col " + open.Column );
            ConsumeClosingTag();
            body = new List<TemplateNode>();
            continue;
          }
          ConsumeClosingTag();
          seenElse = true;
          node.Else = new List<TemplateNode>();
          body = node.Else;
          continue;
        }
        Error( stop, "'" + keyword + "' closes the 'if' block opened at line " + open.Line + ", col " + open.Column + "; expected 'endif'" );
        ConsumeClosingTag();
        break;
      }
      LeaveBlock();
      if ( headerOk )
      {
        Into.Add( node );
      }
    }



    private void ParseRaw( List<TemplateNode> Into )
    {
      Advance();
      Advance();
      if ( !Current.Is( TemplateTokenKind.StatementClose ) )
      {
        Error( Current, "'raw' takes no arguments" );
        SkipTag( TemplateTokenKind.StatementClose );
        return;
      }
      Advance();
      if ( Current.Is( TemplateTokenKind.Text ) )
      {
        Into.Add( new TextNode() { Text = Current.Text, Line = Current.Line, Column = Current.Column } );
        Advance();
      }
      // The lexer always follows a plain {% raw %} with {% endraw %}, a
      // synthetic one when the text never closed the block.
      if ( ( Current.Is( TemplateTokenKind.StatementOpen ) )
      &&   ( Peek( 1 ).IsKeyword( "endraw" ) ) )
      {
        ConsumeClosingTag();
      }
    }



    private OutputNode ParseOutput()
    {
      var open = Current;
      Advance();
      var node = new OutputNode() { Line = open.Line, Column = open.Column };
      node.Value = ParseExpression();
      if ( node.Value == null )
      {
        SkipTag( TemplateTokenKind.OutputClose );
        return null;
      }
      bool filtersSeen = false;
      while ( true )
      {
        if ( Current.Is( TemplateTokenKind.FormatSpec ) )
        {
          if ( filtersSeen )
          {
            Error( Current, "a format spec must come before filters: {{ value:D4|upper }}" );
            SkipTag( TemplateTokenKind.OutputClose );
            return null;
          }
          node.Format       = Current.Text;
          node.FormatLine   = Current.Line;
          node.FormatColumn = Current.Column;
          Advance();
          continue;
        }
        if ( Current.Is( TemplateTokenKind.Pipe ) )
        {
          Advance();
          if ( !Current.Is( TemplateTokenKind.Identifier ) )
          {
            Error( Current, "expected a filter name after '|' but found " + Current.Describe() );
            SkipTag( TemplateTokenKind.OutputClose );
            return null;
          }
          node.FilterReferences.Add( new FilterReference() { Name = Current.Text, Line = Current.Line, Column = Current.Column } );
          Advance();
          filtersSeen = true;
          continue;
        }
        break;
      }
      if ( !ExpectClose( TemplateTokenKind.OutputClose ) )
      {
        return null;
      }
      return node;
    }



    // ----------------------------------------------------------- expressions

    private Expression ParseExpression()
    {
      return ParseOr();
    }



    private Expression ParseOr()
    {
      var left = ParseAnd();
      if ( left == null )
      {
        return null;
      }
      while ( Current.IsKeyword( "or" ) )
      {
        var op = Current;
        Advance();
        var right = ParseAnd();
        if ( right == null )
        {
          return null;
        }
        left = Binary( BinaryOperator.Or, left, right, op );
      }
      return left;
    }



    private Expression ParseAnd()
    {
      var left = ParseNot();
      if ( left == null )
      {
        return null;
      }
      while ( Current.IsKeyword( "and" ) )
      {
        var op = Current;
        Advance();
        var right = ParseNot();
        if ( right == null )
        {
          return null;
        }
        left = Binary( BinaryOperator.And, left, right, op );
      }
      return left;
    }



    private Expression ParseNot()
    {
      if ( Current.IsKeyword( "not" ) )
      {
        var op = Current;
        Advance();
        var operand = ParseNot();
        if ( operand == null )
        {
          return null;
        }
        return new UnaryExpression() { Operator = UnaryOperator.Not, Operand = operand, Line = op.Line, Column = op.Column };
      }
      return ParseComparison();
    }



    private static bool TryComparisonOperator( TemplateToken Token, out BinaryOperator Operator )
    {
      Operator = BinaryOperator.Equal;
      switch ( Token.Kind )
      {
        case TemplateTokenKind.Equal:
          Operator = BinaryOperator.Equal;
          return true;
        case TemplateTokenKind.NotEqual:
          Operator = BinaryOperator.NotEqual;
          return true;
        case TemplateTokenKind.Less:
          Operator = BinaryOperator.Less;
          return true;
        case TemplateTokenKind.LessEqual:
          Operator = BinaryOperator.LessEqual;
          return true;
        case TemplateTokenKind.Greater:
          Operator = BinaryOperator.Greater;
          return true;
        case TemplateTokenKind.GreaterEqual:
          Operator = BinaryOperator.GreaterEqual;
          return true;
      }
      return false;
    }



    private Expression ParseComparison()
    {
      var left = ParseAdditive();
      if ( left == null )
      {
        return null;
      }
      BinaryOperator op;
      if ( !TryComparisonOperator( Current, out op ) )
      {
        return left;
      }
      var opToken = Current;
      Advance();
      var right = ParseAdditive();
      if ( right == null )
      {
        return null;
      }
      var result = Binary( op, left, right, opToken );
      BinaryOperator chained;
      if ( TryComparisonOperator( Current, out chained ) )
      {
        Error( Current, "a comparison cannot be chained (a < b < c)" );
        return null;
      }
      return result;
    }



    private Expression ParseAdditive()
    {
      var left = ParseTerm();
      if ( left == null )
      {
        return null;
      }
      while ( Current.Is( TemplateTokenKind.Plus ) || Current.Is( TemplateTokenKind.Minus ) )
      {
        var op = Current;
        Advance();
        var right = ParseTerm();
        if ( right == null )
        {
          return null;
        }
        left = Binary( op.Is( TemplateTokenKind.Plus ) ? BinaryOperator.Add : BinaryOperator.Subtract, left, right, op );
      }
      return left;
    }



    private Expression ParseTerm()
    {
      var left = ParseUnary();
      if ( left == null )
      {
        return null;
      }
      while ( Current.Is( TemplateTokenKind.Star ) )
      {
        var op = Current;
        Advance();
        var right = ParseUnary();
        if ( right == null )
        {
          return null;
        }
        left = Binary( BinaryOperator.Multiply, left, right, op );
      }
      return left;
    }



    private Expression ParseUnary()
    {
      if ( Current.Is( TemplateTokenKind.Minus ) )
      {
        var op = Current;
        Advance();
        var operand = ParseUnary();
        if ( operand == null )
        {
          return null;
        }
        return new UnaryExpression() { Operator = UnaryOperator.Negate, Operand = operand, Line = op.Line, Column = op.Column };
      }
      return ParsePrimary();
    }



    private Expression ParsePrimary()
    {
      var token = Current;
      switch ( token.Kind )
      {
        case TemplateTokenKind.Integer:
          Advance();
          return new LiteralExpression() { Value = token.IntValue, Line = token.Line, Column = token.Column };
        case TemplateTokenKind.String:
          Advance();
          return new LiteralExpression() { Value = token.Text, Line = token.Line, Column = token.Column };
        case TemplateTokenKind.LParen:
          {
            if ( !EnterBlock( token ) )
            {
              return null;
            }
            Advance();
            var inner = ParseExpression();
            LeaveBlock();
            if ( inner == null )
            {
              return null;
            }
            if ( !Current.Is( TemplateTokenKind.RParen ) )
            {
              Error( Current, "expected ')' but found " + Current.Describe() );
              return null;
            }
            Advance();
            return inner;
          }
        case TemplateTokenKind.Identifier:
          if ( token.IsKeyword( "true" ) )
          {
            Advance();
            return new LiteralExpression() { Value = true, Line = token.Line, Column = token.Column };
          }
          if ( token.IsKeyword( "false" ) )
          {
            Advance();
            return new LiteralExpression() { Value = false, Line = token.Line, Column = token.Column };
          }
          if ( KeywordOf( token ) != null )
          {
            Error( token, "unexpected keyword '" + token.Text + "'" );
            return null;
          }
          return ParsePath();
        default:
          Error( token, "expected a value but found " + token.Describe() );
          return null;
      }
    }



    private Expression ParsePath()
    {
      var path = new PathExpression() { Line = Current.Line, Column = Current.Column };
      path.Segments.Add( new PathSegment() { Name = Current.Text, Line = Current.Line, Column = Current.Column } );
      Advance();
      while ( Current.Is( TemplateTokenKind.Dot ) )
      {
        Advance();
        if ( !Current.Is( TemplateTokenKind.Identifier ) )
        {
          Error( Current, "expected a field name after '.' but found " + Current.Describe() );
          return null;
        }
        path.Segments.Add( new PathSegment() { Name = Current.Text, Line = Current.Line, Column = Current.Column } );
        Advance();
      }
      return path;
    }



    private static Expression Binary( BinaryOperator Operator, Expression Left, Expression Right, TemplateToken OperatorToken )
    {
      return new BinaryExpression() { Operator = Operator, Left = Left, Right = Right, Line = OperatorToken.Line, Column = OperatorToken.Column };
    }
  }
}
