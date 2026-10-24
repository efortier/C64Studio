using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;



namespace TestProject
{
  /// <summary>
  /// The template lexer: delimiter recognition, tag tokens, string escapes,
  /// numbers, raw blocks, positions and newline normalization. Comments are
  /// gone from the token list; every list ends with EndOfFile.
  /// </summary>
  [TestClass]
  public class TestTextTemplateLexer
  {
    private static List<TemplateToken> Tokens( string Source, List<TemplateError> Errors = null )
    {
      return TemplateLexer.Tokenize( Source, new TemplateOptions(), Errors ?? new List<TemplateError>() );
    }



    private static TemplateTokenKind[] Kinds( string Source )
    {
      return Tokens( Source ).Select( t => t.Kind ).ToArray();
    }



    private static List<TemplateError> LexErrors( string Source )
    {
      var errors = new List<TemplateError>();
      Tokens( Source, errors );
      return errors;
    }



    [TestMethod]
    public void TestTextOnlyIsOneToken()
    {
      var tokens = Tokens( "abc {x} $( << >> } %} #}" );
      Assert.AreEqual( 2, tokens.Count );
      Assert.AreEqual( TemplateTokenKind.Text, tokens[0].Kind );
      Assert.AreEqual( "abc {x} $( << >> } %} #}", tokens[0].Text );
      Assert.AreEqual( TemplateTokenKind.EndOfFile, tokens[1].Kind );
    }



    [TestMethod]
    public void TestDelimitersAreRecognized()
    {
      CollectionAssert.AreEqual( new[]
      {
        TemplateTokenKind.Text, TemplateTokenKind.OutputOpen, TemplateTokenKind.Identifier, TemplateTokenKind.OutputClose,
        TemplateTokenKind.Text, TemplateTokenKind.StatementOpen, TemplateTokenKind.Identifier, TemplateTokenKind.Identifier, TemplateTokenKind.StatementClose,
        TemplateTokenKind.Text, TemplateTokenKind.Text, TemplateTokenKind.EndOfFile
      }, Kinds( "a{{ x }}b{% if x %}c{# k #}d" ) );
    }



    [TestMethod]
    public void TestTagTokens()
    {
      var tokens = Tokens( "{{ a.b ( ) | + - * == != < <= > >= }}" );
      CollectionAssert.AreEqual( new[]
      {
        TemplateTokenKind.OutputOpen, TemplateTokenKind.Identifier, TemplateTokenKind.Dot, TemplateTokenKind.Identifier,
        TemplateTokenKind.LParen, TemplateTokenKind.RParen, TemplateTokenKind.Pipe, TemplateTokenKind.Plus, TemplateTokenKind.Minus,
        TemplateTokenKind.Star, TemplateTokenKind.Equal, TemplateTokenKind.NotEqual, TemplateTokenKind.Less, TemplateTokenKind.LessEqual,
        TemplateTokenKind.Greater, TemplateTokenKind.GreaterEqual, TemplateTokenKind.OutputClose, TemplateTokenKind.EndOfFile
      }, tokens.Select( t => t.Kind ).ToArray() );
      Assert.AreEqual( "a", tokens[1].Text );
      Assert.AreEqual( "b", tokens[3].Text );
    }



    [TestMethod]
    public void TestStringLiteralMayContainClosers()
    {
      var tokens = Tokens( "{{ \"}} %} {{\" }}" );
      Assert.AreEqual( TemplateTokenKind.String, tokens[1].Kind );
      Assert.AreEqual( "}} %} {{", tokens[1].Text );
      Assert.AreEqual( TemplateTokenKind.OutputClose, tokens[2].Kind );
    }



    [TestMethod]
    public void TestStringEscapes()
    {
      var tokens = Tokens( "{{ \"a\\\"b\\\\c\\td\\ne\" }}" );
      Assert.AreEqual( "a\"b\\c\td\r\ne", tokens[1].Text, "\\n renders as the option's line ending" );
    }



    [TestMethod]
    public void TestUnknownEscapeIsReported()
    {
      var errors = LexErrors( "{{ \"a\\qb\" }}" );
      Assert.AreEqual( 1, errors.Count );
      Assert.AreEqual( "unknown escape '\\q' in string literal (use \\\" \\\\ \\n \\t)", errors[0].Message );
      Assert.AreEqual( 6, errors[0].Column );
    }



    [TestMethod]
    public void TestNumbersDecimalDollarHexAndCHex()
    {
      var tokens = Tokens( "{{ 12 $FF 0x10 $ffffffff }}" );
      Assert.AreEqual( 12, tokens[1].IntValue );
      Assert.AreEqual( 255, tokens[2].IntValue );
      Assert.AreEqual( 16, tokens[3].IntValue );
      Assert.AreEqual( -1, tokens[4].IntValue, "eight hex digits wrap into an int" );
      Assert.AreEqual( "$FF", tokens[2].Text );
    }



    [TestMethod]
    public void TestNumberTooLargeIsReported()
    {
      var errors = LexErrors( "{{ 4294967296 }}" );
      Assert.AreEqual( "number 4294967296 does not fit in 32 bits", errors[0].Message );
      errors = LexErrors( "{{ $123456789 }}" );
      Assert.AreEqual( "number $123456789 does not fit in 32 bits", errors[0].Message );
    }



    [TestMethod]
    public void TestDollarAndHexPrefixNeedDigits()
    {
      Assert.AreEqual( "'$' must be followed by hex digits", LexErrors( "{{ $ }}" )[0].Message );
      Assert.AreEqual( "'0x' must be followed by hex digits", LexErrors( "{{ 0x }}" )[0].Message );
    }



    [TestMethod]
    public void TestFormatSpecLexedRaw()
    {
      var tokens = Tokens( "{{ x:0000 }}" );
      Assert.AreEqual( TemplateTokenKind.FormatSpec, tokens[2].Kind );
      Assert.AreEqual( "0000", tokens[2].Text );

      tokens = Tokens( "{{ x : D4 }}" );
      Assert.AreEqual( "D4", tokens[2].Text, "trimmed" );

      tokens = Tokens( "{{ x:X2|upper }}" );
      Assert.AreEqual( "X2", tokens[2].Text );
      Assert.AreEqual( TemplateTokenKind.Pipe, tokens[3].Kind );
      Assert.AreEqual( "upper", tokens[4].Text );
    }



    [TestMethod]
    public void TestEmptyFormatSpecIsReported()
    {
      Assert.AreEqual( "empty number format after ':'", LexErrors( "{{ a: }}" )[0].Message );
    }



    [TestMethod]
    public void TestFormatSpecOnlyInOutputTags()
    {
      Assert.AreEqual( "':' format specs are only allowed in {{ }} output tags", LexErrors( "{% if a:D4 %}" )[0].Message );
    }



    [TestMethod]
    public void TestCommentIsDropped()
    {
      var tokens = Tokens( "a{# anything {{ }} {% %} #}b" );
      Assert.AreEqual( 3, tokens.Count );
      Assert.AreEqual( "a", tokens[0].Text );
      Assert.AreEqual( "b", tokens[1].Text );
    }



    [TestMethod]
    public void TestRawBlockKeepsDelimitersLiteral()
    {
      var tokens = Tokens( "{% raw %}{{ x }} {% if %} {# c #}{% EndRaw %}z" );
      CollectionAssert.AreEqual( new[]
      {
        TemplateTokenKind.StatementOpen, TemplateTokenKind.Identifier, TemplateTokenKind.StatementClose,
        TemplateTokenKind.Text,
        TemplateTokenKind.StatementOpen, TemplateTokenKind.Identifier, TemplateTokenKind.StatementClose,
        TemplateTokenKind.Text, TemplateTokenKind.EndOfFile
      }, tokens.Select( t => t.Kind ).ToArray() );
      Assert.AreEqual( "{{ x }} {% if %} {# c #}", tokens[3].Text );
      Assert.AreEqual( "endraw", tokens[5].Text );
      Assert.AreEqual( "z", tokens[7].Text );
    }



    [TestMethod]
    public void TestUnterminatedOutputTagPosition()
    {
      var errors = new List<TemplateError>();
      var tokens = Tokens( "ab\n{{ x", errors );
      Assert.AreEqual( 1, errors.Count );
      Assert.AreEqual( "'{{' is never closed with '}}'", errors[0].Message );
      Assert.AreEqual( 2, errors[0].Line );
      Assert.AreEqual( 1, errors[0].Column );
      var close = tokens[tokens.Count - 2];
      Assert.AreEqual( TemplateTokenKind.OutputClose, close.Kind, "a synthetic closer lets the parser finish" );
      Assert.IsTrue( close.Synthetic );
    }



    [TestMethod]
    public void TestUnterminatedStatementTagPosition()
    {
      var errors = LexErrors( "{% if x" );
      Assert.AreEqual( "'{%' is never closed with '%}'", errors[0].Message );
      Assert.AreEqual( 1, errors[0].Line );
      Assert.AreEqual( 1, errors[0].Column );
    }



    [TestMethod]
    public void TestUnterminatedCommentPosition()
    {
      var errors = LexErrors( "x\n  {# c" );
      Assert.AreEqual( "'{#' is never closed with '#}'", errors[0].Message );
      Assert.AreEqual( 2, errors[0].Line );
      Assert.AreEqual( 3, errors[0].Column );
    }



    [TestMethod]
    public void TestUnterminatedRawPosition()
    {
      var errors = new List<TemplateError>();
      var tokens = Tokens( "{% raw %}abc", errors );
      Assert.AreEqual( "'{% raw %}' is never closed with '{% endraw %}'", errors[0].Message );
      Assert.AreEqual( 1, errors[0].Column );
      Assert.AreEqual( "abc", tokens[3].Text, "the rest is literal text" );
      Assert.AreEqual( "endraw", tokens[5].Text );
      Assert.IsTrue( tokens[5].Synthetic );
    }



    [TestMethod]
    public void TestUnterminatedStringPosition()
    {
      var errors = LexErrors( "{{ \"abc }}" );
      Assert.AreEqual( "string literal is never closed", errors[0].Message );
      Assert.AreEqual( 4, errors[0].Column );
      errors = LexErrors( "{{ \"abc\ndef\" }}" );
      Assert.AreEqual( "string literal is never closed", errors[0].Message, "a line break ends the literal" );
    }



    [TestMethod]
    public void TestLineColumnCountingCrlfLfAndTab()
    {
      var tokens = Tokens( "a\r\nb\n\tc{{ x }}" );
      var open = tokens.First( t => t.Kind == TemplateTokenKind.OutputOpen );
      Assert.AreEqual( 3, open.Line );
      Assert.AreEqual( 3, open.Column, "a TAB counts as one column" );
      var x = tokens.First( t => t.Kind == TemplateTokenKind.Identifier );
      Assert.AreEqual( 6, x.Column );
    }



    [TestMethod]
    public void TestTagSpanningLinesPositions()
    {
      var tokens = Tokens( "{{\n  x\r\n}}" );
      var x = tokens.First( t => t.Kind == TemplateTokenKind.Identifier );
      Assert.AreEqual( 2, x.Line );
      Assert.AreEqual( 3, x.Column );
      Assert.AreEqual( 3, tokens.First( t => t.Kind == TemplateTokenKind.OutputClose ).Line );
    }



    [TestMethod]
    public void TestLfIsNormalizedToCrlf()
    {
      Assert.AreEqual( "a\r\nb\r\n", Tokens( "a\nb\n" )[0].Text );
    }



    [TestMethod]
    public void TestCrlfIsNotDoubled()
    {
      Assert.AreEqual( "a\r\nb", Tokens( "a\r\nb" )[0].Text );
    }



    [TestMethod]
    public void TestLoneCrIsNormalized()
    {
      Assert.AreEqual( "a\r\nb", Tokens( "a\rb" )[0].Text );
    }



    [TestMethod]
    public void TestNewLineOptionIsUsed()
    {
      var tokens = TemplateLexer.Tokenize( "a\r\nb\nc", new TemplateOptions() { NewLine = "\n" }, new List<TemplateError>() );
      Assert.AreEqual( "a\nb\nc", tokens[0].Text );
    }



    [TestMethod]
    public void TestBomIsDropped()
    {
      var tokens = Tokens( "﻿abc{{ x }}" );
      Assert.AreEqual( "abc", tokens[0].Text );
      Assert.AreEqual( 4, tokens[1].Column, "columns count from the first real character" );
    }



    [TestMethod]
    public void TestEqualsHint()
    {
      Assert.AreEqual( "'=' is not an operator; use '==' to compare", LexErrors( "{{ a = b }}" )[0].Message );
    }



    [TestMethod]
    public void TestUnexpectedCharacter()
    {
      var errors = LexErrors( "{{ a & b }}" );
      Assert.AreEqual( "unexpected character '&'", errors[0].Message );
      Assert.AreEqual( 6, errors[0].Column );
    }



    [TestMethod]
    public void TestWrongCloserIsReportedAndStillCloses()
    {
      var errors = new List<TemplateError>();
      var tokens = Tokens( "{{ a %}b", errors );
      Assert.AreEqual( "expected '}}' but found '%}'", errors[0].Message );
      Assert.AreEqual( TemplateTokenKind.OutputClose, tokens[2].Kind );
      Assert.AreEqual( "b", tokens[3].Text );
    }



    [TestMethod]
    public void TestEmptySourceIsJustEndOfFile()
    {
      var tokens = Tokens( "" );
      Assert.AreEqual( 1, tokens.Count );
      Assert.AreEqual( TemplateTokenKind.EndOfFile, tokens[0].Kind );
    }
  }
}
