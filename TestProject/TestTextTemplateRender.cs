using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;
using static TestProject.TextTemplateTestSupport;



namespace TestProject
{
  /// <summary>
  /// Rendering: output formatting, format specs and filters, loops and the
  /// loop object, conditionals and truthiness, null propagation, integer
  /// wraparound, determinism and speed.
  /// </summary>
  [TestClass]
  public class TestTextTemplateRender
  {
    [TestMethod]
    public void TestOutputsIntStringBool()
    {
      Assert.AreEqual( "3 Hello true", Render( "{{ count }} {{ title }} {{ ready }}" ) );
      Assert.AreEqual( "false", Render( "{{ ready }}", new RootData() ) );
    }



    [TestMethod]
    public void TestFormatSpecsD4X2x2Custom0000()
    {
      Assert.AreEqual( "0003 03 03 0003 3", Render( "{{ count:D4 }} {{ count:X2 }} {{ count:x2 }} {{ count:0000 }} {{ count:D }}" ) );
      Assert.AreEqual( "00FF ff", Render( "{{ count:X4 }} {{ count:x2 }}", new RootData() { Count = 255 } ) );
    }



    [TestMethod]
    public void TestFiltersUpperLowerChained()
    {
      Assert.AreEqual( "HELLO hello HELLO", Render( "{{ title|upper }} {{ title|upper|lower }} {{ title | upper }}" ) );
      Assert.AreEqual( "CAVES_1", Render( "{{ title|label }}", new RootData() { Title = "caves 1" } ) );
    }



    [TestMethod]
    public void TestFormatThenFilter()
    {
      Assert.AreEqual( "ff", Render( "{{ count:X2|lower }}", new RootData() { Count = 255 } ) );
    }



    [TestMethod]
    public void TestForLoopFields()
    {
      Assert.AreEqual( "0 1 true false 3;1 2 false false 3;2 3 false true 3;",
                       Render( "{% for i in items %}{{ loop.index }} {{ loop.index1 }} {{ loop.first }} {{ loop.last }} {{ loop.count }};{% endfor %}" ) );
    }



    [TestMethod]
    public void TestLastItemCommaIdiom()
    {
      Assert.AreEqual( "alpha, beta, gamma", Render( "{% for i in items %}{{ i.name }}{% if not loop.last %}, {% endif %}{% endfor %}" ) );
    }



    [TestMethod]
    public void TestNestedLoopsShadowAndParent()
    {
      Assert.AreEqual( "0.0:10 0.1:11 2.0:30 ",
                       Render( "{% for i in items %}{% for c in i.children %}{{ loop.parent.index }}.{{ loop.index }}:{{ c.id }} {% endfor %}{% endfor %}" ) );
      Assert.AreEqual( "alpha:2 beta:0 gamma:1 ", Render( "{% for i in items %}{{ i.name }}:{% for c in i.children %}{% if loop.last %}{{ loop.count }}{% endif %}{% endfor %}{% if i.children.count == 0 %}0{% endif %} {% endfor %}" ) );
    }



    [TestMethod]
    public void TestEmptyListSkipsBody()
    {
      Assert.AreEqual( "", Render( "{% for i in items %}x{% endfor %}", new RootData() ) );
      Assert.AreEqual( "", Render( "{% for i in nothing.children %}x{% endfor %}" ), "a null object's list is empty" );
    }



    [TestMethod]
    public void TestIfElifElse()
    {
      Assert.AreEqual( "onetwoother", Render( "{% for i in items %}{% if i.id == 1 %}one{% elif i.id == 2 %}two{% else %}other{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "b", Render( "{% if false %}a{% elif false %}x{% else %}b{% endif %}" ) );
      Assert.AreEqual( "", Render( "{% if false %}a{% endif %}" ) );
    }



    [TestMethod]
    public void TestTruthiness()
    {
      Assert.AreEqual( "b", Render( "{% if 0 %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "a", Render( "{% if 7 %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "b", Render( "{% if \"\" %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "a", Render( "{% if \"x\" %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "a", Render( "{% if items %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "b", Render( "{% if items %}a{% else %}b{% endif %}", new RootData() ) );
      Assert.AreEqual( "b", Render( "{% if nothing %}a{% else %}b{% endif %}" ) );
      Assert.AreEqual( "a", Render( "{% for i in items %}{% if i.first_child %}a{% else %}b{% endif %}{% endfor %}", new RootData() { Items = { SampleData().Items[0] } } ) );
      Assert.AreEqual( "a", Render( "{% if ready %}a{% else %}b{% endif %}" ) );
    }



    [TestMethod]
    public void TestLogicAndComparisons()
    {
      Assert.AreEqual( "false false true true", Render( "{{ not true }} {{ true and false }} {{ false or true }} {{ not false and true }}" ) );
      Assert.AreEqual( "true false true true false", Render( "{{ 1 < 2 }} {{ 2 <= 1 }} {{ \"a\" == \"a\" }} {{ true != false }} {{ \"a\" == \"A\" }}" ) );
      Assert.AreEqual( "true true", Render( "{{ 3 >= 3 }} {{ 4 > 3 }}" ) );
    }



    [TestMethod]
    public void TestNullObjectPropagatesZeroValues()
    {
      Assert.AreEqual( "0||false|0|", Render( "{{ nothing.id }}|{{ nothing.name }}|{{ nothing.flag }}|{{ nothing.children.count }}|{{ nothing.first_child.name }}" ) );
      Assert.AreEqual( "0000", Render( "{{ nothing.first_child.id:D4 }}" ) );
    }



    [TestMethod]
    public void TestIntegerWrapIsSilent()
    {
      Assert.AreEqual( "-2147483648", Render( "{{ 2147483647 + 1 }}" ) );
      Assert.AreEqual( "-2147483648", Render( "{{ -2147483647 - 1 }}" ) );
    }



    [TestMethod]
    public void TestNegativeHexFormat()
    {
      Assert.AreEqual( "FFFFFFFF", Render( "{{ -1:X2 }}" ) );
      Assert.AreEqual( "-1", Render( "{{ -1 }}" ) );
    }



    [TestMethod]
    public void TestStringNewlineEscapeUsesOption()
    {
      Assert.AreEqual( "a\r\nb", Render( "{{ \"a\\nb\" }}" ) );
      Assert.AreEqual( "a\nb", Render( "{{ \"a\\nb\" }}", new TemplateOptions() { NewLine = "\n" } ) );
    }



    [TestMethod]
    public void TestHexLiterals()
    {
      Assert.AreEqual( "256 $FF", Render( "{{ $FF + 0x01 }} ${{ $ff:X2 }}" ) );
    }



    [TestMethod]
    public void TestParentheses()
    {
      Assert.AreEqual( "9 6", Render( "{{ (1 + 2) * 3 }} {{ (count) * 2 }}" ) );
    }



    [TestMethod]
    public void TestRenderTwiceIdentical()
    {
      List<TemplateError> errors;
      var template = Template.Compile( "{% for i in items %}{{ loop.index }}{{ i.name|upper }}{% endfor %}{{ count:D3 }}", BuildSchema(), out errors );
      Assert.IsNotNull( template, Join( errors ) );
      string first = template.Render( SampleData() );
      string second = template.Render( SampleData() );
      Assert.AreEqual( "0ALPHA1BETA2GAMMA003", first );
      Assert.AreEqual( first, second );
    }



    [TestMethod]
    public void TestLiteralTextIsVerbatim()
    {
      Assert.AreEqual( "\t.const X = $FF // { } % # $( << >>\r\n", Render( "\t.const X = $FF // { } % # $( << >>\r\n" ) );
    }



    [TestMethod]
    public void TestLargeLoopPerformance()
    {
      var data = new RootData();
      for ( int i = 0; i < 10000; ++i )
      {
        var item = new Item() { Id = i, Name = "item" + i, Flag = ( i % 2 == 0 ) };
        item.Children.Add( new Item() { Id = i * 10, Name = "child" } );
        data.Items.Add( item );
      }
      List<TemplateError> errors;
      var template = Template.Compile( "{% for i in items %}{{ loop.index1:D5 }} {{ i.name|upper }}{% for c in i.children %} {{ c.id:X4 }}{% endfor %}{% if i.flag %} even{% endif %}\r\n{% endfor %}", BuildSchema(), out errors );
      Assert.IsNotNull( template, Join( errors ) );
      var watch = Stopwatch.StartNew();
      string output = template.Render( data );
      watch.Stop();
      Assert.IsTrue( output.StartsWith( "00001 ITEM0 0000 even\r\n00002 ITEM1 000A\r\n" ), output.Substring( 0, 60 ) );
      Assert.IsTrue( watch.ElapsedMilliseconds < 3000, "10000 items took " + watch.ElapsedMilliseconds + " ms" );
    }
  }
}
