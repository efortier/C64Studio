using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;
using static TestProject.TextTemplateTestSupport;



namespace TestProject
{
  /// <summary>
  /// The template binder: names resolve against the schema at compile time
  /// with the exact messages (suggestions, available names), types of
  /// operators / format specs / filters / loops are checked, one mistake
  /// yields one error, and a compile needs no data.
  /// </summary>
  [TestClass]
  public class TestTextTemplateCompile
  {
    [TestMethod]
    public void TestUnknownRootWithSuggestion()
    {
      AssertSingleError( "{{ itemz.count }}", 1, 4, "unknown name 'itemz' - did you mean 'items'? Root names: items, title, count, ready, nothing, loop" );
      AssertSingleError( "{{ zzzzzz }}", 1, 4, "unknown name 'zzzzzz'. Root names: items, title, count, ready, nothing, loop" );
    }



    [TestMethod]
    public void TestUnknownFieldMessageListsFields()
    {
      AssertSingleError( "{% for i in items %}{{ i.idx }}{% endfor %}", 1, 26, "'i' has no field 'idx' - did you mean 'id'? Fields of item: id, name, flag, children, first_child" );
      AssertSingleError( "{{ nothing.first_child.zzz }}", 1, 24, "'nothing.first_child' has no field 'zzz'. Fields of item: id, name, flag, children, first_child" );
    }



    [TestMethod]
    public void TestLoopVariableHasElementType()
    {
      Assert.AreEqual( "alphabetagamma", Render( "{% for i in items %}{{ i.name }}{% endfor %}" ) );
      Assert.AreEqual( "a1a2g1", Render( "{% for i in items %}{% for c in i.children %}{{ c.name }}{% endfor %}{% endfor %}" ) );
    }



    [TestMethod]
    public void TestFieldOnScalar()
    {
      AssertSingleError( "{{ count.x }}", 1, 10, "'count' is an int and has no fields" );
      AssertSingleError( "{{ title.x }}", 1, 10, "'title' is a string and has no fields" );
    }



    [TestMethod]
    public void TestFieldOnListOnlyCount()
    {
      AssertSingleError( "{{ items.name }}", 1, 10, "'items' is a list; it has no field 'name' (only 'count') - loop over it with {% for x in items %}" );
      Assert.AreEqual( "3", Render( "{{ items.count }}" ) );
      Assert.AreEqual( "2", Render( "{% for i in items %}{% if loop.first %}{{ i.children.count }}{% endif %}{% endfor %}" ) );
    }



    [TestMethod]
    public void TestOutputListRefused()
    {
      AssertSingleError( "{{ items }}", 1, 4, "cannot output a list ('items'); use .count or loop over it with {% for %}" );
    }



    [TestMethod]
    public void TestOutputObjectRefused()
    {
      AssertSingleError( "{{ nothing }}", 1, 4, "cannot output an object of type item; pick a field: id, name, flag, children, first_child" );
      AssertSingleError( "{% for i in items %}{{ loop }}{% endfor %}", 1, 24, "cannot output an object of type loop; pick a field: index, index1, first, last, count, parent" );
    }



    [TestMethod]
    public void TestFormatOnStringRefused()
    {
      AssertSingleError( "{{ title:D4 }}", 1, 9, "format spec ':D4' needs an int, 'title' is a string" );
    }



    [TestMethod]
    public void TestInvalidFormatSpec()
    {
      AssertSingleError( "{{ count:Q4 }}", 1, 9, "invalid number format 'Q4'" );
    }



    [TestMethod]
    public void TestUnknownFilterWithSuggestion()
    {
      AssertSingleError( "{{ title|uppr }}", 1, 10, "unknown filter 'uppr' - did you mean 'upper'? Filters: label, lower, upper" );
    }



    [TestMethod]
    public void TestFilterOnIntSuggestsFormat()
    {
      AssertSingleError( "{{ count|upper }}", 1, 10, "filter 'upper' needs a string, 'count' is an int - format it first, e.g. count:D|upper" );
    }



    [TestMethod]
    public void TestFilterOnBool()
    {
      AssertSingleError( "{{ ready|upper }}", 1, 10, "filter 'upper' needs a string, 'ready' is a bool" );
    }



    [TestMethod]
    public void TestForOverNonList()
    {
      AssertSingleError( "{% for i in title %}{% endfor %}", 1, 13, "'for' needs a list, 'title' is a string" );
      AssertSingleError( "{% for i in nothing %}{% endfor %}", 1, 13, "'for' needs a list, 'nothing' is an object of type item" );
    }



    [TestMethod]
    public void TestLoopOutsideFor()
    {
      AssertSingleError( "{{ loop.index }}", 1, 4, "'loop' is only available inside a {% for %} block" );
    }



    [TestMethod]
    public void TestLoopParentWithoutNesting()
    {
      AssertSingleError( "{% for i in items %}{{ loop.parent.index }}{% endfor %}", 1, 29, "'loop.parent': there is no enclosing loop" );
      AssertSingleError( "{% for i in items %}{% for c in i.children %}{{ loop.parent.parent.index }}{% endfor %}{% endfor %}", 1, 61, "'loop.parent': there is no enclosing loop" );
    }



    [TestMethod]
    public void TestLoopVariableNameRules()
    {
      AssertSingleError( "{% for items in items %}{% endfor %}", 1, 8, "loop variable 'items' hides the root name 'items'; pick another name" );
      AssertSingleError( "{% for i in items %}{% for i in i.children %}{% endfor %}{% endfor %}", 1, 28, "loop variable 'i' is already used by the enclosing loop at line 1, col 8" );
      AssertSingleError( "{% for for in items %}{% endfor %}", 1, 8, "'for' is a keyword and cannot be a loop variable name" );
      AssertSingleError( "{% for loop in items %}{% endfor %}", 1, 8, "'loop' cannot be a loop variable name" );
    }



    [TestMethod]
    public void TestArithmeticTypes()
    {
      AssertSingleError( "{{ title + 1 }}", 1, 10, "operator '+' needs ints: 'title' is a string" );
      AssertSingleError( "{{ 1 * ready }}", 1, 6, "operator '*' needs ints: 'ready' is a bool" );
    }



    [TestMethod]
    public void TestComparisonTypes()
    {
      AssertSingleError( "{{ count == \"5\" }}", 1, 10, "cannot compare an int with a string (count == \"5\")" );
      AssertSingleError( "{{ ready != 1 }}", 1, 10, "cannot compare a bool with an int (ready != 1)" );
    }



    [TestMethod]
    public void TestObjectAndListComparisonRefused()
    {
      AssertSingleError( "{{ nothing == nothing }}", 1, 12, "cannot compare objects; compare a field instead (e.g. map.id == project.start_map.id)" );
      AssertSingleError( "{{ items == items }}", 1, 10, "cannot compare lists; compare their .count or loop over them" );
    }



    [TestMethod]
    public void TestOrderingNeedsInts()
    {
      AssertSingleError( "{{ title < \"b\" }}", 1, 10, "'<' needs ints: 'title' is a string" );
    }



    [TestMethod]
    public void TestUnaryMinusNeedsInt()
    {
      AssertSingleError( "{{ -title }}", 1, 4, "unary '-' needs an int, 'title' is a string" );
    }



    [TestMethod]
    public void TestNamesAreCaseInsensitive()
    {
      Assert.AreEqual( "Helloalphabetagamma", Render( "{{ TITLE }}{% FOR I IN ITEMS %}{{ I.NAME }}{% ENDFOR %}" ) );
      Assert.AreEqual( "HELLO", Render( "{{ title|UPPER }}" ) );
      Assert.AreEqual( "x", Render( "{% IF Ready AND NOT False %}x{% ENDIF %}" ) );
    }



    [TestMethod]
    public void TestCompileNeedsNoData()
    {
      List<TemplateError> errors;
      var template = Template.Compile( "{{ title }}:{{ items.count }}", BuildSchema(), out errors );
      Assert.IsNotNull( template, Join( errors ) );
      Assert.AreEqual( "Hello:3", template.Render( SampleData() ) );
      Assert.AreEqual( ":0", template.Render( new RootData() ) );
    }



    [TestMethod]
    public void TestErrorsDontCascade()
    {
      var errors = Errors( "{{ nope.x + 1 }}{{ (nope + 1) * 2 }}{% for i in nope %}{{ i.x }}{% endfor %}" );
      Assert.AreEqual( 3, errors.Count, Join( errors ) );
      foreach ( var error in errors )
      {
        Assert.IsTrue( error.Message.StartsWith( "unknown name 'nope'" ), error.Message );
      }
    }



    [TestMethod]
    public void TestCompileReturnsNullOnErrors()
    {
      List<TemplateError> errors;
      Assert.IsNull( Template.Compile( "{{ nope }}", BuildSchema(), out errors ) );
      Assert.AreEqual( 1, errors.Count );
    }



    [TestMethod]
    public void TestSchemaValidates()
    {
      Assert.AreEqual( 0, BuildSchema().Validate().Count, JoinProblems( BuildSchema().Validate() ) );

      var bad = new TemplateSchema();
      var thing = bad.DefineObjectType( "thing", "" );
      thing.AddField( "BadName", TemplateType.Int, "x", o => 0 );
      thing.AddField( "for", TemplateType.Int, "x", o => 0 );
      thing.AddField( "twice", TemplateType.Int, "x", o => 0 );
      thing.AddField( "TWICE", TemplateType.Int, "x", o => 0 );
      thing.AddField( "silent", TemplateType.Int, "", o => 0 );
      thing.AddField( "lazy", TemplateType.Int, "x", null );
      bad.RootType.AddField( "loop", thing, "x", o => null );
      bad.DefineFilter( "Upper", "", null );
      var problems = bad.Validate();
      CollectionAssert.Contains( problems, "type 'thing': field 'BadName' is not lower snake_case" );
      CollectionAssert.Contains( problems, "type 'thing': field 'for' is a keyword" );
      CollectionAssert.Contains( problems, "type 'thing': field 'TWICE' is defined twice" );
      CollectionAssert.Contains( problems, "type 'thing': field 'silent' has no description" );
      CollectionAssert.Contains( problems, "type 'thing': field 'lazy' has no getter" );
      CollectionAssert.Contains( problems, "type 'root': field 'loop' is reserved for the loop variable" );
      CollectionAssert.Contains( problems, "filter 'Upper' is not lower snake_case" );
      CollectionAssert.Contains( problems, "filter 'Upper' has no description" );
      CollectionAssert.Contains( problems, "filter 'Upper' has no implementation" );
    }



    private static string JoinProblems( List<string> Lines )
    {
      return string.Join( "\n", Lines.ToArray() );
    }



    [TestMethod]
    public void TestObjectTypesListRootAndLoopFirst()
    {
      var types = new List<TemplateType>( BuildSchema().ObjectTypes );
      Assert.AreEqual( "root", types[0].Name );
      Assert.AreEqual( "loop", types[1].Name );
      Assert.AreEqual( "item", types[2].Name );
      Assert.AreEqual( "list<item>", BuildSchema().ListOf( types[2] ).Name );
    }



    [TestMethod]
    public void TestEditDistance()
    {
      Assert.AreEqual( 0, TemplateCompiler.EditDistance( "maps", "maps" ) );
      Assert.AreEqual( 1, TemplateCompiler.EditDistance( "mapz", "maps" ) );
      Assert.AreEqual( 1, TemplateCompiler.EditDistance( "mpas", "maps" ), "a transposition is one edit" );
      Assert.AreEqual( 2, TemplateCompiler.EditDistance( "mp", "maps" ) );
      Assert.AreEqual( 4, TemplateCompiler.EditDistance( "", "maps" ) );
    }
  }
}
