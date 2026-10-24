using System;
using System.Collections.Generic;
using System.Text;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>
  /// Binds a parsed template to a schema: resolves every name to a root
  /// field, a loop variable or the loop state, types every expression, and
  /// checks operators, format specs, filters and loop variables. Everything
  /// a template can get wrong is found here, so rendering cannot fail. An
  /// expression that failed gets TemplateType.Error and causes no further
  /// messages.
  /// </summary>
  public class TemplateCompiler
  {
    private class ScopeVariable
    {
      public string         Name = "";
      public int            Slot = 0;
      public TemplateType   Type = null;
      public int            Line = 0;
      public int            Column = 0;
    }

    private const int                 MAX_LISTED_NAMES = 20;

    private TemplateSchema            m_Schema = null;
    private TemplateOptions           m_Options = null;
    private List<TemplateError>       m_Errors = null;
    private List<ScopeVariable>       m_Variables = new List<ScopeVariable>();
    private int                       m_LoopDepth = 0;
    private int                       m_SlotCount = 0;



    /// <summary>Binds Nodes in place and returns the number of loop variable slots a render needs.</summary>
    public static int Bind( List<TemplateNode> Nodes, TemplateSchema Schema, TemplateOptions Options, List<TemplateError> Errors )
    {
      var compiler = new TemplateCompiler( Schema, Options ?? new TemplateOptions(), Errors );
      compiler.BindNodes( Nodes );
      return compiler.m_SlotCount;
    }



    private TemplateCompiler( TemplateSchema Schema, TemplateOptions Options, List<TemplateError> Errors )
    {
      m_Schema  = Schema;
      m_Options = Options;
      m_Errors  = Errors ?? new List<TemplateError>();
    }



    private void Error( int Line, int Column, string Message )
    {
      m_Errors.Add( new TemplateError( Line, Column, Message ) );
    }



    // ----------------------------------------------------------------- nodes

    private void BindNodes( List<TemplateNode> Nodes )
    {
      foreach ( var node in Nodes )
      {
        if ( node is OutputNode )
        {
          BindOutput( (OutputNode)node );
        }
        else if ( node is ForNode )
        {
          BindFor( (ForNode)node );
        }
        else if ( node is IfNode )
        {
          BindIf( (IfNode)node );
        }
      }
    }



    private void BindOutput( OutputNode Node )
    {
      BindExpression( Node.Value );
      var type = Node.Value.Type;
      if ( type == TemplateType.Error )
      {
        return;
      }
      string display = Node.Value.Display;
      if ( type.Kind == TemplateTypeKind.List )
      {
        Error( Node.Value.Line, Node.Value.Column, "cannot output a list ('" + display + "'); use .count or loop over it with {% for %}" );
        return;
      }
      if ( type.Kind == TemplateTypeKind.Object )
      {
        Error( Node.Value.Line, Node.Value.Column, "cannot output an object of type " + type.Name + "; pick a field: " + FieldList( type ) );
        return;
      }
      var current = type;
      if ( Node.Format != null )
      {
        if ( current != TemplateType.Int )
        {
          Error( Node.FormatLine, Node.FormatColumn, "format spec ':" + Node.Format + "' needs an int, '" + display + "' is " + current.Described );
        }
        else if ( !TemplateValues.IsValidNumberFormat( Node.Format ) )
        {
          Error( Node.FormatLine, Node.FormatColumn, "invalid number format '" + Node.Format + "'" );
        }
        current = TemplateType.String;
      }
      foreach ( var reference in Node.FilterReferences )
      {
        var filter = m_Schema.FindFilter( reference.Name );
        if ( filter == null )
        {
          var names = new List<string>();
          foreach ( var f in m_Schema.Filters )
          {
            names.Add( f.Name );
          }
          names.Sort( StringComparer.OrdinalIgnoreCase );
          Error( reference.Line, reference.Column, "unknown filter '" + reference.Name + "'" + Suggestion( reference.Name, names ) + " Filters: " + Listed( names ) );
          return;
        }
        if ( current == TemplateType.Int )
        {
          Error( reference.Line, reference.Column, "filter '" + filter.Name + "' needs a string, '" + display + "' is an int - format it first, e.g. " + display + ":D|" + filter.Name );
          return;
        }
        if ( current != TemplateType.String )
        {
          Error( reference.Line, reference.Column, "filter '" + filter.Name + "' needs a string, '" + display + "' is " + current.Described );
          return;
        }
        Node.Filters.Add( filter );
        current = TemplateType.String;
      }
    }



    private void BindFor( ForNode Node )
    {
      BindExpression( Node.Source );
      var sourceType = Node.Source.Type;
      TemplateType elementType = TemplateType.Error;
      if ( sourceType != TemplateType.Error )
      {
        if ( sourceType.Kind != TemplateTypeKind.List )
        {
          Error( Node.Source.Line, Node.Source.Column, "'for' needs a list, '" + Node.Source.Display + "' is " + sourceType.Described );
        }
        else
        {
          elementType = sourceType.ElementType;
        }
      }
      string name = Node.Variable;
      if ( TemplateSchema.IsKeyword( name ) )
      {
        Error( Node.VariableLine, Node.VariableColumn, "'" + name + "' is a keyword and cannot be a loop variable name" );
      }
      else if ( string.Equals( name, "loop", StringComparison.OrdinalIgnoreCase ) )
      {
        Error( Node.VariableLine, Node.VariableColumn, "'loop' cannot be a loop variable name" );
      }
      else if ( m_Schema.RootType.FindField( name ) != null )
      {
        Error( Node.VariableLine, Node.VariableColumn, "loop variable '" + name + "' hides the root name '" + m_Schema.RootType.FindField( name ).Name + "'; pick another name" );
      }
      else
      {
        var enclosing = FindVariable( name );
        if ( enclosing != null )
        {
          Error( Node.VariableLine, Node.VariableColumn, "loop variable '" + name + "' is already used by the enclosing loop at line " + enclosing.Line + ", col " + enclosing.Column );
        }
      }
      var variable = new ScopeVariable() { Name = name, Slot = m_Variables.Count, Type = elementType, Line = Node.VariableLine, Column = Node.VariableColumn };
      Node.Slot = variable.Slot;
      if ( m_Variables.Count + 1 > m_SlotCount )
      {
        m_SlotCount = m_Variables.Count + 1;
      }
      m_Variables.Add( variable );
      ++m_LoopDepth;
      try
      {
        BindNodes( Node.Body );
      }
      finally
      {
        --m_LoopDepth;
        m_Variables.RemoveAt( m_Variables.Count - 1 );
      }
    }



    private void BindIf( IfNode Node )
    {
      foreach ( var branch in Node.Branches )
      {
        BindExpression( branch.Condition );
        BindNodes( branch.Body );
      }
      if ( Node.Else != null )
      {
        BindNodes( Node.Else );
      }
    }



    private ScopeVariable FindVariable( string Name )
    {
      for ( int i = m_Variables.Count - 1; i >= 0; --i )
      {
        if ( string.Equals( m_Variables[i].Name, Name, StringComparison.OrdinalIgnoreCase ) )
        {
          return m_Variables[i];
        }
      }
      return null;
    }



    // ----------------------------------------------------------- expressions

    private void BindExpression( Expression Expression )
    {
      if ( Expression is LiteralExpression )
      {
        var literal = (LiteralExpression)Expression;
        if ( literal.Value is string )
        {
          literal.Type = TemplateType.String;
        }
        else if ( literal.Value is bool )
        {
          literal.Type = TemplateType.Bool;
        }
        else
        {
          literal.Type = TemplateType.Int;
        }
      }
      else if ( Expression is PathExpression )
      {
        BindPath( (PathExpression)Expression );
      }
      else if ( Expression is UnaryExpression )
      {
        BindUnary( (UnaryExpression)Expression );
      }
      else if ( Expression is BinaryExpression )
      {
        BindBinary( (BinaryExpression)Expression );
      }
      else
      {
        Expression.Type = TemplateType.Error;
      }
    }



    private void BindUnary( UnaryExpression Expression )
    {
      BindExpression( Expression.Operand );
      var type = Expression.Operand.Type;
      if ( Expression.Operator == UnaryOperator.Not )
      {
        Expression.Type = TemplateType.Bool;
        return;
      }
      if ( type == TemplateType.Error )
      {
        Expression.Type = TemplateType.Error;
        return;
      }
      if ( type != TemplateType.Int )
      {
        Error( Expression.Line, Expression.Column, "unary '-' needs an int, '" + Expression.Operand.Display + "' is " + type.Described );
        Expression.Type = TemplateType.Error;
        return;
      }
      Expression.Type = TemplateType.Int;
    }



    private void BindBinary( BinaryExpression Expression )
    {
      BindExpression( Expression.Left );
      BindExpression( Expression.Right );
      var left  = Expression.Left.Type;
      var right = Expression.Right.Type;
      string symbol = BinaryExpression.Symbol( Expression.Operator );
      switch ( Expression.Operator )
      {
        case BinaryOperator.And:
        case BinaryOperator.Or:
          Expression.Type = TemplateType.Bool;
          return;
      }
      if ( ( left == TemplateType.Error )
      ||   ( right == TemplateType.Error ) )
      {
        Expression.Type = TemplateType.Error;
        return;
      }
      switch ( Expression.Operator )
      {
        case BinaryOperator.Equal:
        case BinaryOperator.NotEqual:
          if ( ( left.Kind == TemplateTypeKind.Object )
          ||   ( right.Kind == TemplateTypeKind.Object ) )
          {
            Error( Expression.Line, Expression.Column, "cannot compare objects; compare a field instead (e.g. map.id == project.start_map.id)" );
            Expression.Type = TemplateType.Error;
            return;
          }
          if ( ( left.Kind == TemplateTypeKind.List )
          ||   ( right.Kind == TemplateTypeKind.List ) )
          {
            Error( Expression.Line, Expression.Column, "cannot compare lists; compare their .count or loop over them" );
            Expression.Type = TemplateType.Error;
            return;
          }
          if ( left.Kind != right.Kind )
          {
            Error( Expression.Line, Expression.Column, "cannot compare " + left.Described + " with " + right.Described + " (" + Expression.Display + ")" );
            Expression.Type = TemplateType.Error;
            return;
          }
          Expression.Type = TemplateType.Bool;
          return;
        case BinaryOperator.Less:
        case BinaryOperator.LessEqual:
        case BinaryOperator.Greater:
        case BinaryOperator.GreaterEqual:
          if ( !RequireInts( Expression, "'" + symbol + "' needs ints: " ) )
          {
            return;
          }
          Expression.Type = TemplateType.Bool;
          return;
        default:
          if ( !RequireInts( Expression, "operator '" + symbol + "' needs ints: " ) )
          {
            return;
          }
          Expression.Type = TemplateType.Int;
          return;
      }
    }



    private bool RequireInts( BinaryExpression Expression, string Prefix )
    {
      Expression offending = null;
      if ( Expression.Left.Type != TemplateType.Int )
      {
        offending = Expression.Left;
      }
      else if ( Expression.Right.Type != TemplateType.Int )
      {
        offending = Expression.Right;
      }
      if ( offending == null )
      {
        return true;
      }
      Error( Expression.Line, Expression.Column, Prefix + "'" + offending.Display + "' is " + offending.Type.Described );
      Expression.Type = TemplateType.Error;
      return false;
    }



    private void BindPath( PathExpression Path )
    {
      var first = Path.Segments[0];
      TemplateType current;
      if ( string.Equals( first.Name, "loop", StringComparison.OrdinalIgnoreCase ) )
      {
        if ( m_LoopDepth == 0 )
        {
          Error( first.Line, first.Column, "'loop' is only available inside a {% for %} block" );
          Path.Type = TemplateType.Error;
          return;
        }
        Path.Root = PathRoot.Loop;
        current = m_Schema.LoopType;
        int parents = 0;
        for ( int i = 1; i < Path.Segments.Count; ++i )
        {
          if ( !string.Equals( Path.Segments[i].Name, "parent", StringComparison.OrdinalIgnoreCase ) )
          {
            break;
          }
          ++parents;
          if ( parents >= m_LoopDepth )
          {
            Error( Path.Segments[i].Line, Path.Segments[i].Column, "'loop.parent': there is no enclosing loop" );
            Path.Type = TemplateType.Error;
            return;
          }
        }
      }
      else
      {
        var variable = FindVariable( first.Name );
        if ( variable != null )
        {
          Path.Root = PathRoot.Variable;
          Path.Slot = variable.Slot;
          current = variable.Type;
        }
        else
        {
          var field = m_Schema.RootType.FindField( first.Name );
          if ( field == null )
          {
            var names = new List<string>();
            foreach ( var f in m_Schema.RootType.Fields )
            {
              names.Add( f.Name );
            }
            var candidates = new List<string>( names );
            foreach ( var v in m_Variables )
            {
              candidates.Add( v.Name );
            }
            if ( m_LoopDepth > 0 )
            {
              candidates.Add( "loop" );
            }
            names.Add( "loop" );
            Error( first.Line, first.Column, "unknown name '" + first.Name + "'" + Suggestion( first.Name, candidates ) + " Root names: " + Listed( names ) );
            Path.Type = TemplateType.Error;
            return;
          }
          Path.Root = PathRoot.RootField;
          Path.RootField = field;
          current = field.Type;
        }
      }
      string displaySoFar = first.Name;
      for ( int i = 1; i < Path.Segments.Count; ++i )
      {
        var segment = Path.Segments[i];
        if ( current == TemplateType.Error )
        {
          Path.Type = TemplateType.Error;
          return;
        }
        if ( current.Kind == TemplateTypeKind.List )
        {
          var field = current.FindField( segment.Name );
          if ( field == null )
          {
            Error( segment.Line, segment.Column, "'" + displaySoFar + "' is a list; it has no field '" + segment.Name
                                                + "' (only 'count') - loop over it with {% for x in " + displaySoFar + " %}" );
            Path.Type = TemplateType.Error;
            return;
          }
          Path.Fields.Add( field );
          current = field.Type;
        }
        else if ( current.Kind == TemplateTypeKind.Object )
        {
          var field = current.FindField( segment.Name );
          if ( field == null )
          {
            var names = new List<string>();
            foreach ( var f in current.Fields )
            {
              names.Add( f.Name );
            }
            Error( segment.Line, segment.Column, "'" + displaySoFar + "' has no field '" + segment.Name + "'" + Suggestion( segment.Name, names )
                                                + " Fields of " + current.Name + ": " + Listed( names ) );
            Path.Type = TemplateType.Error;
            return;
          }
          Path.Fields.Add( field );
          current = field.Type;
        }
        else
        {
          Error( segment.Line, segment.Column, "'" + displaySoFar + "' is " + current.Described + " and has no fields" );
          Path.Type = TemplateType.Error;
          return;
        }
        displaySoFar += "." + segment.Name;
      }
      Path.Type = current;
    }



    // -------------------------------------------------------------- messages

    private static string FieldList( TemplateType Type )
    {
      var names = new List<string>();
      foreach ( var field in Type.Fields )
      {
        names.Add( field.Name );
      }
      return Listed( names );
    }



    /// <summary>Names joined by ", ", at most MAX_LISTED_NAMES of them.</summary>
    private static string Listed( List<string> Names )
    {
      var sb = new StringBuilder();
      for ( int i = 0; i < Names.Count; ++i )
      {
        if ( i == MAX_LISTED_NAMES )
        {
          sb.Append( " ... (+" + ( Names.Count - MAX_LISTED_NAMES ) + " more, see Fields)" );
          break;
        }
        if ( i > 0 )
        {
          sb.Append( ", " );
        }
        sb.Append( Names[i] );
      }
      return sb.ToString();
    }



    /// <summary>" - did you mean 'x'?" for the closest candidate within two edits, else "." (the sentence then continues).</summary>
    private static string Suggestion( string Name, List<string> Candidates )
    {
      string best = null;
      int bestDistance = int.MaxValue;
      foreach ( var candidate in Candidates )
      {
        int distance = EditDistance( Name.ToLowerInvariant(), candidate.ToLowerInvariant() );
        if ( distance < bestDistance )
        {
          bestDistance = distance;
          best = candidate;
        }
      }
      if ( ( best != null )
      &&   ( bestDistance <= 2 ) )
      {
        return " - did you mean '" + best + "'?";
      }
      return ".";
    }



    /// <summary>Optimal string alignment (Damerau-Levenshtein with adjacent transpositions).</summary>
    public static int EditDistance( string A, string B )
    {
      int[,] d = new int[A.Length + 1, B.Length + 1];
      for ( int i = 0; i <= A.Length; ++i )
      {
        d[i, 0] = i;
      }
      for ( int j = 0; j <= B.Length; ++j )
      {
        d[0, j] = j;
      }
      for ( int i = 1; i <= A.Length; ++i )
      {
        for ( int j = 1; j <= B.Length; ++j )
        {
          int cost = ( A[i - 1] == B[j - 1] ) ? 0 : 1;
          int value = Math.Min( Math.Min( d[i - 1, j] + 1, d[i, j - 1] + 1 ), d[i - 1, j - 1] + cost );
          if ( ( i > 1 )
          &&   ( j > 1 )
          &&   ( A[i - 1] == B[j - 2] )
          &&   ( A[i - 2] == B[j - 1] ) )
          {
            value = Math.Min( value, d[i - 2, j - 2] + 1 );
          }
          d[i, j] = value;
        }
      }
      return d[A.Length, B.Length];
    }
  }
}
