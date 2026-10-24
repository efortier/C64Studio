using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;
using static TestProject.TextTemplateTestSupport;



namespace TestProject
{
  /// <summary>
  /// The template parser: block structure, precedence, the exact structural
  /// error messages with their positions, recovery after a bad tag, and the
  /// nesting limit.
  /// </summary>
  [TestClass]
  public class TestTextTemplateParser
  {
    [TestMethod]
    public void TestForIfElifElseNesting()
    {
      string source = "{% for i in items %}{% if i.id == 1 %}one{% elif i.id == 2 %}{% for c in i.children %}c{% endfor %}none{% else %}other{% endif %};{% endfor %}";
      Assert.AreEqual( "one;none;other;", Render( source ) );
    }



    [TestMethod]
    public void TestStrayClosers()
    {
      AssertSingleError( "{% endfor %}", 1, 1, "'endfor' without a matching 'for'" );
      AssertSingleError( "{% endif %}", 1, 1, "'endif' without a matching 'if'" );
      AssertSingleError( "x\r\n {% else %}", 2, 2, "'else' without a matching 'if'" );
      AssertSingleError( "{% elif true %}", 1, 1, "'elif' without a matching 'if'" );
      AssertSingleError( "{% endraw %}", 1, 1, "'endraw' without a matching 'raw'" );
    }



    [TestMethod]
    public void TestMismatchedCloserClosesTheBlock()
    {
      AssertSingleError( "{% for i in items %}\r\n{% endif %}", 2, 1, "'endif' closes the 'for' block opened at line 1, col 1; expected 'endfor'" );
      AssertSingleError( "{% if true %}\r\n  {% endfor %}", 2, 3, "'endfor' closes the 'if' block opened at line 1, col 1; expected 'endif'" );
    }



    [TestMethod]
    public void TestUnclosedBlocksReportedAtOpener()
    {
      AssertSingleError( "x\r\n{% for i in items %}y", 2, 1, "'{% for %}' is never closed with '{% endfor %}'" );
      AssertSingleError( "x\r\n  {% if true %}y", 2, 3, "'{% if %}' is never closed with '{% endif %}'" );
    }



    [TestMethod]
    public void TestUnknownStatement()
    {
      AssertSingleError( "{% foo %}", 1, 4, "unknown statement 'foo' (expected for, if, elif, else, endif, endfor, raw)" );
    }



    [TestMethod]
    public void TestUnknownStatementHintsRawForLiteralBrace()
    {
      AssertSingleError( "{%0101 %}", 1, 3, "unknown statement '0101' (expected for, if, elif, else, endif, endfor, raw) - to output a literal '{%' write {{ \"{%\" }} or wrap the text in {% raw %} ... {% endraw %}" );
    }



    [TestMethod]
    public void TestEmptyStatement()
    {
      AssertSingleError( "{% %}", 1, 4, "empty statement (expected for, if, elif, else, endif, endfor, raw)" );
    }



    [TestMethod]
    public void TestChainedComparisonRefused()
    {
      AssertSingleError( "{{ 1 < 2 < 3 }}", 1, 10, "a comparison cannot be chained (a < b < c)" );
    }



    [TestMethod]
    public void TestPrecedenceNotBindsLooserThanComparison()
    {
      Assert.AreEqual( "true", Render( "{{ not 1 == 2 }}" ) );
      Assert.AreEqual( "false", Render( "{{ not 1 == 1 }}" ) );
    }



    [TestMethod]
    public void TestPrecedenceAndBeforeOr()
    {
      Assert.AreEqual( "false", Render( "{{ false or true and false }}" ) );
      Assert.AreEqual( "true", Render( "{{ true and false or true }}" ) );
      Assert.AreEqual( "false", Render( "{{ (true and false) or (true and false) }}" ) );
    }



    [TestMethod]
    public void TestArithmeticPrecedence()
    {
      Assert.AreEqual( "7", Render( "{{ 1 + 2 * 3 }}" ) );
      Assert.AreEqual( "9", Render( "{{ (1 + 2) * 3 }}" ) );
      Assert.AreEqual( "-6", Render( "{{ -2 * 3 }}" ) );
      Assert.AreEqual( "2", Render( "{{ 5 - 2 - 1 }}" ) );
      Assert.AreEqual( "4", Render( "{{ 5 - (2 - 1) }}" ) );
    }



    [TestMethod]
    public void TestMultipleErrorsSortedByPosition()
    {
      var errors = Errors( "{{ bad1 }}\r\n{% endfor %}\r\n{{ bad2 }}" );
      Assert.AreEqual( 3, errors.Count, Join( errors ) );
      Assert.AreEqual( 1, errors[0].Line );
      Assert.AreEqual( 2, errors[1].Line );
      Assert.AreEqual( 3, errors[2].Line );
    }



    [TestMethod]
    public void TestRecoveryContinuesAfterBadTag()
    {
      var errors = Errors( "{{ 1 + }}{{ nope }}" );
      Assert.AreEqual( 2, errors.Count, Join( errors ) );
      Assert.AreEqual( "expected a value but found '}}'", errors[0].Message );
      Assert.IsTrue( errors[1].Message.StartsWith( "unknown name 'nope'" ), errors[1].Message );
    }



    [TestMethod]
    public void TestRawTakesNoArguments()
    {
      AssertSingleError( "{% raw x %}", 1, 8, "'raw' takes no arguments" );
    }



    [TestMethod]
    public void TestElseAfterElse()
    {
      AssertSingleError( "{% if true %}{% else %}{% else %}{% endif %}", 1, 24, "'else' after 'else' in the 'if' block opened at line 1, col 1" );
    }



    [TestMethod]
    public void TestElifAfterElse()
    {
      AssertSingleError( "{% if true %}{% else %}{% elif true %}{% endif %}", 1, 24, "'elif' after 'else' in the 'if' block opened at line 1, col 1" );
    }



    [TestMethod]
    public void TestForNeedsVariable()
    {
      AssertSingleError( "{% for %}{% endfor %}", 1, 8, "'for' needs a loop variable: for <name> in <list>" );
    }



    [TestMethod]
    public void TestForNeedsIn()
    {
      AssertSingleError( "{% for i items %}{% endfor %}", 1, 10, "expected 'in' after the loop variable 'i'" );
    }



    [TestMethod]
    public void TestExpectedCloser()
    {
      AssertSingleError( "{% if true x %}{% endif %}", 1, 12, "expected '%}' but found 'x'" );
      AssertSingleError( "{{ title x }}", 1, 10, "expected '}}' but found 'x'" );
    }



    [TestMethod]
    public void TestFormatBeforeFilters()
    {
      AssertSingleError( "{{ title|upper:D4 }}", 1, 15, "a format spec must come before filters: {{ value:D4|upper }}" );
    }



    [TestMethod]
    public void TestUnclosedParenthesis()
    {
      AssertSingleError( "{{ (1 + 2 }}", 1, 11, "expected ')' but found '}}'" );
    }



    [TestMethod]
    public void TestExpectedValue()
    {
      AssertSingleError( "{{ }}", 1, 4, "expected a value but found '}}'" );
      AssertSingleError( "{{ in }}", 1, 4, "unexpected keyword 'in'" );
      AssertSingleError( "{{ title. }}", 1, 11, "expected a field name after '.' but found '}}'" );
      AssertSingleError( "{{ title| }}", 1, 11, "expected a filter name after '|' but found '}}'" );
    }



    [TestMethod]
    public void TestNestingLimit()
    {
      var sb = new StringBuilder();
      for ( int i = 0; i < 65; ++i )
      {
        sb.Append( "{% if true %}" );
      }
      var errors = Errors( sb.ToString() );
      Assert.AreEqual( 1, errors.Count, Join( errors ) );
      Assert.AreEqual( "nesting deeper than 64 levels", errors[0].Message );

      errors = Errors( "{% if true %}{% if true %}{% if true %}{% if true %}x{% endif %}{% endif %}{% endif %}{% endif %}", new TemplateOptions() { MaxNesting = 3 } );
      Assert.AreEqual( "nesting deeper than 3 levels", errors[0].Message );

      Assert.AreEqual( "x", Render( "{% if true %}{% if true %}{% if true %}x{% endif %}{% endif %}{% endif %}", new TemplateOptions() { MaxNesting = 3 } ) );
      Assert.AreEqual( "3", Render( "{{ (((3))) }}", new TemplateOptions() { MaxNesting = 3 } ), "parentheses count too" );
      Assert.AreEqual( "nesting deeper than 3 levels", Errors( "{{ ((((3)))) }}", new TemplateOptions() { MaxNesting = 3 } )[0].Message );
    }



    [TestMethod]
    public void TestEmptyTemplateRendersNothing()
    {
      Assert.AreEqual( "", Render( "" ) );
    }
  }
}
