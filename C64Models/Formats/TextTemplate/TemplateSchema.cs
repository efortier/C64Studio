using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>The kinds of value a template expression can have.</summary>
  public enum TemplateTypeKind
  {
    Int,
    String,
    Bool,
    List,
    Object
  }



  /// <summary>
  /// A type of a template schema: one of the three scalars, a list of a
  /// type (interned by TemplateSchema.ListOf, with the one pseudo-field
  /// "count"), or an object with named fields (TemplateSchema.DefineObjectType).
  /// </summary>
  public class TemplateType
  {
    public static readonly TemplateType   Int    = new TemplateType( "int", TemplateTypeKind.Int, "a whole number" );
    public static readonly TemplateType   String = new TemplateType( "string", TemplateTypeKind.String, "text" );
    public static readonly TemplateType   Bool   = new TemplateType( "bool", TemplateTypeKind.Bool, "true or false" );
    /// <summary>
    /// The type of an expression whose binding failed. Every check that meets
    /// it stays silent, so one mistake yields one error, never a cascade.
    /// </summary>
    public static readonly TemplateType   Error  = new TemplateType( "error", TemplateTypeKind.Object, "" );

    public string                         Name = "";
    public TemplateTypeKind               Kind = TemplateTypeKind.Object;
    /// <summary>The element type of a list type; null otherwise.</summary>
    public TemplateType                   ElementType = null;
    public string                         Description = "";
    private List<TemplateField>           m_Fields = new List<TemplateField>();



    public TemplateType( string Name, TemplateTypeKind Kind, string Description )
    {
      this.Name        = Name ?? "";
      this.Kind        = Kind;
      this.Description = Description ?? "";
    }



    /// <summary>The fields in declaration order - the order the Fields dialog shows.</summary>
    public IList<TemplateField> Fields
    {
      get
      {
        return m_Fields.AsReadOnly();
      }
    }



    public bool IsScalar
    {
      get
      {
        return ( Kind == TemplateTypeKind.Int )
            || ( Kind == TemplateTypeKind.String )
            || ( Kind == TemplateTypeKind.Bool );
      }
    }



    /// <summary>The field called Name (case-insensitive), or null.</summary>
    public TemplateField FindField( string Name )
    {
      foreach ( var field in m_Fields )
      {
        if ( string.Equals( field.Name, Name, StringComparison.OrdinalIgnoreCase ) )
        {
          return field;
        }
      }
      return null;
    }



    /// <summary>
    /// Adds a field. Getter receives an instance of this type and returns an
    /// int (any integer type), a string, a bool, an IList or an object (null
    /// is allowed for objects and renders as that type's zero values).
    /// </summary>
    public TemplateField AddField( string Name, TemplateType Type, string Description, Func<object, object> Getter )
    {
      var field = new TemplateField( Name, Type, Description, Getter );
      m_Fields.Add( field );
      return field;
    }



    /// <summary>"an int", "a string", "a bool", "a list", "an object of type map" - for messages.</summary>
    public string Described
    {
      get
      {
        switch ( Kind )
        {
          case TemplateTypeKind.Int:
            return "an int";
          case TemplateTypeKind.String:
            return "a string";
          case TemplateTypeKind.Bool:
            return "a bool";
          case TemplateTypeKind.List:
            return "a list";
          default:
            return "an object of type " + Name;
        }
      }
    }



    public override string ToString()
    {
      return Name;
    }
  }



  /// <summary>One named value of an object type.</summary>
  public class TemplateField
  {
    /// <summary>Canonical name, lower snake_case; lookups are case-insensitive.</summary>
    public string                         Name = "";
    public TemplateType                   Type = null;
    /// <summary>The text the Fields dialog shows; never empty.</summary>
    public string                         Description = "";
    public Func<object, object>           Getter = null;



    public TemplateField( string Name, TemplateType Type, string Description, Func<object, object> Getter )
    {
      this.Name        = Name ?? "";
      this.Type        = Type;
      this.Description = Description ?? "";
      this.Getter      = Getter;
    }



    public override string ToString()
    {
      return Name + " : " + ( ( Type != null ) ? Type.Name : "?" );
    }
  }



  /// <summary>A string-to-string transformation applied with "|": {{ map.name | upper }}.</summary>
  public class TemplateFilter
  {
    public string                         Name = "";
    public string                         Description = "";
    public Func<string, string>           Apply = null;



    public TemplateFilter( string Name, string Description, Func<string, string> Apply )
    {
      this.Name        = Name ?? "";
      this.Description = Description ?? "";
      this.Apply       = Apply;
    }
  }



  /// <summary>Typed convenience for schema builders: the getter takes the model instance directly.</summary>
  public static class TemplateTypeExtensions
  {
    public static TemplateField AddField<T>( this TemplateType Type, string Name, TemplateType FieldType, string Description, Func<T, object> Getter ) where T : class
    {
      return Type.AddField( Name, FieldType, Description, o => Getter( (T)o ) );
    }
  }



  /// <summary>
  /// Everything a template may refer to: the root names (the fields of
  /// RootType, whose getters take the root instance handed to Render), the
  /// object types, the built-in "loop" type and the filters. Compile needs
  /// only the schema, never data, and the Fields dialog lists ObjectTypes.
  /// </summary>
  public class TemplateSchema
  {
    /// <summary>Words the lexer treats specially; neither a name nor a field may be one.</summary>
    public static readonly string[]       Keywords = { "for", "in", "endfor", "if", "elif", "else", "endif", "raw", "endraw", "and", "or", "not", "true", "false" };

    private List<TemplateType>            m_ObjectTypes = new List<TemplateType>();
    private Dictionary<TemplateType, TemplateType> m_ListTypes = new Dictionary<TemplateType, TemplateType>();
    private List<TemplateFilter>          m_Filters = new List<TemplateFilter>();



    public TemplateSchema()
    {
      RootType = DefineObjectType( "root", "the names available at the top level of a template" );
      LoopType = DefineObjectType( "loop", "the innermost {% for %} loop (only inside one)" );
      LoopType.AddField<LoopState>( "index", TemplateType.Int, "position of the current item, counted from 0", l => l.Index );
      LoopType.AddField<LoopState>( "index1", TemplateType.Int, "position of the current item, counted from 1", l => l.Index + 1 );
      LoopType.AddField<LoopState>( "first", TemplateType.Bool, "true for the first item", l => l.Index == 0 );
      LoopType.AddField<LoopState>( "last", TemplateType.Bool, "true for the last item", l => l.Index == l.Count - 1 );
      LoopType.AddField<LoopState>( "count", TemplateType.Int, "number of items the loop runs over", l => l.Count );
      LoopType.AddField<LoopState>( "parent", LoopType, "the enclosing loop (loop.parent.index ...)", l => l.Parent );
    }



    /// <summary>The root names are its fields; its getters take the instance handed to Template.Render.</summary>
    public TemplateType RootType
    {
      get;
      private set;
    }



    /// <summary>The built-in type of "loop".</summary>
    public TemplateType LoopType
    {
      get;
      private set;
    }



    public static bool IsKeyword( string Name )
    {
      foreach ( var keyword in Keywords )
      {
        if ( string.Equals( keyword, Name, StringComparison.OrdinalIgnoreCase ) )
        {
          return true;
        }
      }
      return false;
    }



    public TemplateType DefineObjectType( string Name, string Description )
    {
      var type = new TemplateType( Name, TemplateTypeKind.Object, Description );
      m_ObjectTypes.Add( type );
      return type;
    }



    /// <summary>The list type of Element - one instance per element type, with the pseudo-field "count".</summary>
    public TemplateType ListOf( TemplateType Element )
    {
      TemplateType list;
      if ( m_ListTypes.TryGetValue( Element, out list ) )
      {
        return list;
      }
      list = new TemplateType( "list<" + Element.Name + ">", TemplateTypeKind.List, "a list of " + Element.Name );
      list.ElementType = Element;
      list.AddField( "count", TemplateType.Int, "number of items in the list", o => ( o is IList ) ? ( (IList)o ).Count : 0 );
      m_ListTypes[Element] = list;
      return list;
    }



    public TemplateFilter DefineFilter( string Name, string Description, Func<string, string> Apply )
    {
      var filter = new TemplateFilter( Name, Description, Apply );
      m_Filters.Add( filter );
      return filter;
    }



    /// <summary>The filter called Name (case-insensitive), or null.</summary>
    public TemplateFilter FindFilter( string Name )
    {
      foreach ( var filter in m_Filters )
      {
        if ( string.Equals( filter.Name, Name, StringComparison.OrdinalIgnoreCase ) )
        {
          return filter;
        }
      }
      return null;
    }



    /// <summary>RootType first, then loop, then the types in definition order.</summary>
    public IEnumerable<TemplateType> ObjectTypes
    {
      get
      {
        return m_ObjectTypes;
      }
    }



    public IEnumerable<TemplateFilter> Filters
    {
      get
      {
        return m_Filters;
      }
    }



    /// <summary>
    /// Self-check of a schema definition: names are lower snake_case and not
    /// keywords, unique per type and among the filters (case-insensitive),
    /// descriptions are filled in, every field has a type and a getter, and
    /// no root name is "loop". Empty when all is well.
    /// </summary>
    public List<string> Validate()
    {
      var problems = new List<string>();
      var namePattern = new Regex( "^[a-z_][a-z0-9_]*$" );
      var typeNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
      foreach ( var type in m_ObjectTypes )
      {
        if ( string.IsNullOrEmpty( type.Name ) )
        {
          problems.Add( "an object type has no name" );
        }
        else if ( !typeNames.Add( type.Name ) )
        {
          problems.Add( "type '" + type.Name + "' is defined twice" );
        }
        var fieldNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
        foreach ( var field in type.Fields )
        {
          string where = "type '" + type.Name + "': field '" + field.Name + "'";
          if ( !namePattern.IsMatch( field.Name ) )
          {
            problems.Add( where + " is not lower snake_case" );
          }
          if ( IsKeyword( field.Name ) )
          {
            problems.Add( where + " is a keyword" );
          }
          if ( ( type == RootType )
          &&   ( string.Equals( field.Name, "loop", StringComparison.OrdinalIgnoreCase ) ) )
          {
            problems.Add( where + " is reserved for the loop variable" );
          }
          if ( !fieldNames.Add( field.Name ) )
          {
            problems.Add( where + " is defined twice" );
          }
          if ( string.IsNullOrEmpty( field.Description ) )
          {
            problems.Add( where + " has no description" );
          }
          if ( field.Type == null )
          {
            problems.Add( where + " has no type" );
          }
          if ( field.Getter == null )
          {
            problems.Add( where + " has no getter" );
          }
        }
      }
      var filterNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
      foreach ( var filter in m_Filters )
      {
        string where = "filter '" + filter.Name + "'";
        if ( !namePattern.IsMatch( filter.Name ) )
        {
          problems.Add( where + " is not lower snake_case" );
        }
        if ( IsKeyword( filter.Name ) )
        {
          problems.Add( where + " is a keyword" );
        }
        if ( !filterNames.Add( filter.Name ) )
        {
          problems.Add( where + " is defined twice" );
        }
        if ( string.IsNullOrEmpty( filter.Description ) )
        {
          problems.Add( where + " has no description" );
        }
        if ( filter.Apply == null )
        {
          problems.Add( where + " has no implementation" );
        }
      }
      return problems;
    }
  }
}
