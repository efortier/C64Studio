using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats.TextTemplate;



namespace TestProject
{
  /// <summary>
  /// A tiny schema for the engine tests, independent of MapProject:
  /// item { id, name, flag, children, first_child }, roots items / title /
  /// count / ready / nothing, filters upper / lower / label.
  /// </summary>
  public static class TextTemplateTestSupport
  {
    public class Item
    {
      public int          Id = 0;
      public string       Name = "";
      public bool         Flag = false;
      public List<Item>   Children = new List<Item>();
    }



    public class RootData
    {
      public List<Item>   Items = new List<Item>();
      public string       Title = "";
      public int          Count = 0;
      public bool         Ready = false;
    }



    public static TemplateSchema BuildSchema()
    {
      var schema = new TemplateSchema();
      var item = schema.DefineObjectType( "item", "a test item" );
      item.AddField<Item>( "id", TemplateType.Int, "the id", i => i.Id );
      item.AddField<Item>( "name", TemplateType.String, "the name", i => i.Name );
      item.AddField<Item>( "flag", TemplateType.Bool, "the flag", i => i.Flag );
      item.AddField<Item>( "children", schema.ListOf( item ), "child items", i => i.Children );
      item.AddField<Item>( "first_child", item, "the first child, or nothing", i => ( i.Children.Count > 0 ) ? i.Children[0] : null );
      schema.RootType.AddField<RootData>( "items", schema.ListOf( item ), "all items", r => r.Items );
      schema.RootType.AddField<RootData>( "title", TemplateType.String, "the title", r => r.Title );
      schema.RootType.AddField<RootData>( "count", TemplateType.Int, "a number", r => r.Count );
      schema.RootType.AddField<RootData>( "ready", TemplateType.Bool, "a flag", r => r.Ready );
      schema.RootType.AddField<RootData>( "nothing", item, "an item that is always missing", r => null );
      schema.DefineFilter( "upper", "upper case", s => s.ToUpperInvariant() );
      schema.DefineFilter( "lower", "lower case", s => s.ToLowerInvariant() );
      schema.DefineFilter( "label", "spaces to underscores, upper case", s => s.Replace( ' ', '_' ).ToUpperInvariant() );
      return schema;
    }



    /// <summary>alpha (1, flag, children a1/a2), beta (2), gamma (3, flag, child g1); title "Hello", count 3, ready true.</summary>
    public static RootData SampleData()
    {
      var data = new RootData() { Title = "Hello", Count = 3, Ready = true };
      var alpha = new Item() { Id = 1, Name = "alpha", Flag = true };
      alpha.Children.Add( new Item() { Id = 10, Name = "a1" } );
      alpha.Children.Add( new Item() { Id = 11, Name = "a2" } );
      var beta = new Item() { Id = 2, Name = "beta" };
      var gamma = new Item() { Id = 3, Name = "gamma", Flag = true };
      gamma.Children.Add( new Item() { Id = 30, Name = "g1" } );
      data.Items.Add( alpha );
      data.Items.Add( beta );
      data.Items.Add( gamma );
      return data;
    }



    public static List<TemplateError> Errors( string Source, TemplateOptions Options = null )
    {
      List<TemplateError> errors;
      Template.Compile( Source, BuildSchema(), Options ?? new TemplateOptions(), out errors );
      return errors;
    }



    public static string Render( string Source, TemplateOptions Options = null )
    {
      return Render( Source, SampleData(), Options );
    }



    public static string Render( string Source, RootData Data, TemplateOptions Options = null )
    {
      List<TemplateError> errors;
      var template = Template.Compile( Source, BuildSchema(), Options ?? new TemplateOptions(), out errors );
      Assert.IsNotNull( template, "compile errors:\n" + Join( errors ) );
      return template.Render( Data );
    }



    public static string Join( List<TemplateError> Errors )
    {
      var lines = new List<string>();
      foreach ( var error in Errors )
      {
        lines.Add( error.ToString() );
      }
      return string.Join( "\n", lines.ToArray() );
    }



    /// <summary>Asserts exactly one error at Line/Column whose message equals Message.</summary>
    public static void AssertSingleError( string Source, int Line, int Column, string Message )
    {
      var errors = Errors( Source );
      Assert.AreEqual( 1, errors.Count, "expected one error, got:\n" + Join( errors ) );
      Assert.AreEqual( Line, errors[0].Line, "line of: " + errors[0] );
      Assert.AreEqual( Column, errors[0].Column, "column of: " + errors[0] );
      Assert.AreEqual( Message, errors[0].Message );
    }



    /// <summary>Asserts that some error's message contains Fragment.</summary>
    public static void AssertErrorContains( string Source, string Fragment )
    {
      var errors = Errors( Source );
      foreach ( var error in errors )
      {
        if ( error.Message.Contains( Fragment ) )
        {
          return;
        }
      }
      Assert.Fail( "no error contains '" + Fragment + "'; errors:\n" + Join( errors ) );
    }
  }
}
