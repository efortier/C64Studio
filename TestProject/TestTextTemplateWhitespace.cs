using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;
using static TestProject.TextTemplateTestSupport;



namespace TestProject
{
  /// <summary>
  /// The tag-only-line rule: a line holding nothing but {% %} tags and
  /// comments vanishes from the output with its line break and indentation;
  /// an output tag or any other text anywhere on the line keeps the whole
  /// line verbatim. Indentation inside a block is literal.
  /// </summary>
  [TestClass]
  public class TestTextTemplateWhitespace
  {
    [TestMethod]
    public void TestTagOnlyLineRemovedCrlf()
    {
      Assert.AreEqual( "A\r\nB\r\nC", Render( "A\r\n{% if true %}\r\nB\r\n{% endif %}\r\nC" ) );
    }



    [TestMethod]
    public void TestTagOnlyLineRemovedLf()
    {
      Assert.AreEqual( "A\r\nB\r\nC", Render( "A\n{% if true %}\nB\n{% endif %}\nC" ), "an LF template renders the same CRLF bytes" );
    }



    [TestMethod]
    public void TestIndentationBeforeTagRemoved()
    {
      Assert.AreEqual( "A\r\n\tB\r\n", Render( "A\r\n   {% if true %}\r\n\tB\r\n  {% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestTrailingBlanksAfterTagStillTagOnly()
    {
      Assert.AreEqual( "B\r\n", Render( "{% if true %}  \t\r\nB\r\n{% endif %}   \r\n" ) );
    }



    [TestMethod]
    public void TestTagAtEofWithoutNewlineKeepsPreviousBreak()
    {
      Assert.AreEqual( "A\r\nB\r\n", Render( "A\r\n{% if true %}\r\nB\r\n{% endif %}" ) );
    }



    [TestMethod]
    public void TestForLoopOnOwnLines()
    {
      Assert.AreEqual( "A\r\n", Render( "{% for i in items %}\r\nA\r\n{% endfor %}", new RootData() { Items = { new Item() } } ) );
    }



    [TestMethod]
    public void TestInlineTagKeepsLine()
    {
      Assert.AreEqual( "xyz", Render( "x{% if true %}y{% endif %}z" ) );
    }



    [TestMethod]
    public void TestOutputOnTagLineKeepsLineAndIndent()
    {
      Assert.AreEqual( "Hello  \r\nB\r\n", Render( "{{ title }}  {% if true %}\r\nB\r\n{% endif %}\r\n" ), "output before the tag" );
      Assert.AreEqual( "    Hello\r\n", Render( "  {% if true %}  {{ title }}\r\n{% endif %}\r\n" ), "output after the tag: every blank stays" );
    }



    [TestMethod]
    public void TestNonBlankTextAfterTagKeepsLine()
    {
      Assert.AreEqual( " x\r\n", Render( "{% if true %} x\r\n{% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestTwoTagsOnOneLineRemoved()
    {
      Assert.AreEqual( "B\r\n", Render( "{% if true %}{% if true %}\r\nB\r\n{% endif %}  {% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestConsecutiveTagOnlyLines()
    {
      Assert.AreEqual( "B\r\n", Render( "{% if true %}\r\n{% if true %}\r\nB\r\n{% endif %}\r\n{% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestCommentAloneOnLineVanishes()
    {
      Assert.AreEqual( "A\r\nB", Render( "A\r\n{# c #}\r\nB" ) );
      Assert.AreEqual( "A\r\nB\r\n", Render( "A\r\n  {% if true %}  {# why #}\r\nB\r\n{% endif %}" ) );
    }



    [TestMethod]
    public void TestMultiLineCommentOnTagOnlyLines()
    {
      Assert.AreEqual( "A\r\nB", Render( "A\r\n  {# one\r\n two #}\r\nB" ) );
    }



    [TestMethod]
    public void TestMultiLineStatementTag()
    {
      Assert.AreEqual( "A\r\nB\r\n", Render( "A\r\n  {% if true\r\n and true %}\r\nB\r\n{% endif %}" ) );
    }



    [TestMethod]
    public void TestBlankLinesAreKept()
    {
      Assert.AreEqual( "A\r\n\r\n\r\nB\r\n", Render( "A\r\n\r\n{% if true %}\r\n\r\nB\r\n{% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestRawTagsOnOwnLinesVanishContentKept()
    {
      Assert.AreEqual( "{{ x }}\r\n{% if %}\r\nB", Render( "{% raw %}\r\n{{ x }}\r\n{% if %}\r\n{% endraw %}\r\nB" ) );
    }



    [TestMethod]
    public void TestRawTagInlineKeepsLine()
    {
      Assert.AreEqual( "a{{ x }}b", Render( "a{% raw %}{{ x }}{% endraw %}b" ) );
    }



    [TestMethod]
    public void TestTabIndentInsideBlockReachesOutput()
    {
      Assert.AreEqual( "\tX1\r\n\tX2\r\n\tX3\r\n", Render( "{% for i in items %}\r\n\tX{{ i.id }}\r\n{% endfor %}\r\n" ) );
    }



    [TestMethod]
    public void TestFirstLineTagOnly()
    {
      Assert.AreEqual( "B\r\n", Render( "{% if true %}\r\nB\r\n{% endif %}\r\n" ) );
    }



    [TestMethod]
    public void TestBlankTailAfterTagAtEofRemoved()
    {
      Assert.AreEqual( "B\r\n", Render( "B\r\n{% if true %}{% endif %}  " ) );
    }



    [TestMethod]
    public void TestTemplateWithoutTrailingNewlineMatchesOneWith()
    {
      string withNewline    = Render( "{% for i in items %}\r\n{{ i.name }}\r\n{% endfor %}\r\n" );
      string withoutNewline = Render( "{% for i in items %}\r\n{{ i.name }}\r\n{% endfor %}" );
      Assert.AreEqual( "alpha\r\nbeta\r\ngamma\r\n", withNewline );
      Assert.AreEqual( withNewline, withoutNewline );
    }



    [TestMethod]
    public void TestLfTemplateGivesTheSameBytes()
    {
      string crlf = Render( "{% for i in items %}\r\n\t{{ i.name }},\r\n{% endfor %}\r\n" );
      string lf   = Render( "{% for i in items %}\n\t{{ i.name }},\n{% endfor %}\n" );
      Assert.AreEqual( crlf, lf );
    }
  }
}
