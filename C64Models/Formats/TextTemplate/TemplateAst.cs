using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>A node of a parsed template; Render appends its output to the scope.</summary>
  public abstract class TemplateNode
  {
    public int      Line = 0;
    public int      Column = 0;



    public abstract void Render( RenderScope Scope );
  }



  public class TextNode : TemplateNode
  {
    public string   Text = "";



    public override void Render( RenderScope Scope )
    {
      Scope.Output.Append( Text );
    }
  }



  /// <summary>A filter name as written in an output tag, resolved by the compiler.</summary>
  public class FilterReference
  {
    public string   Name = "";
    public int      Line = 0;
    public int      Column = 0;
  }



  /// <summary>{{ value[:format][|filter...] }}</summary>
  public class OutputNode : TemplateNode
  {
    public Expression               Value = null;
    public string                   Format = null;
    public int                      FormatLine = 0;
    public int                      FormatColumn = 0;
    public List<FilterReference>    FilterReferences = new List<FilterReference>();
    /// <summary>The filters in application order, bound by the compiler.</summary>
    public List<TemplateFilter>     Filters = new List<TemplateFilter>();



    public override void Render( RenderScope Scope )
    {
      object value = Value.Evaluate( Scope );
      string text;
      if ( Format != null )
      {
        text = TemplateValues.ToInt( value ).ToString( Format, CultureInfo.InvariantCulture );
      }
      else
      {
        text = TemplateValues.FormatOutput( value );
      }
      foreach ( var filter in Filters )
      {
        text = filter.Apply( text ) ?? "";
      }
      Scope.Output.Append( text );
    }
  }



  /// <summary>{% for variable in source %} body {% endfor %}</summary>
  public class ForNode : TemplateNode
  {
    public string               Variable = "";
    public int                  VariableLine = 0;
    public int                  VariableColumn = 0;
    public Expression           Source = null;
    public List<TemplateNode>   Body = new List<TemplateNode>();
    /// <summary>The slot the variable lives in while the loop runs, assigned by the compiler.</summary>
    public int                  Slot = -1;



    public override void Render( RenderScope Scope )
    {
      var list = Source.Evaluate( Scope ) as IList;
      if ( list == null )
      {
        return;
      }
      var outer = Scope.Loop;
      var state = new LoopState() { Count = list.Count, Parent = outer };
      Scope.Loop = state;
      try
      {
        for ( int i = 0; i < list.Count; ++i )
        {
          state.Index = i;
          Scope.Slots[Slot] = list[i];
          foreach ( var node in Body )
          {
            node.Render( Scope );
          }
        }
      }
      finally
      {
        Scope.Loop = outer;
      }
    }
  }



  public class IfBranch
  {
    public Expression           Condition = null;
    public List<TemplateNode>   Body = new List<TemplateNode>();
  }



  /// <summary>{% if %} ... {% elif %} ... {% else %} ... {% endif %}</summary>
  public class IfNode : TemplateNode
  {
    public List<IfBranch>       Branches = new List<IfBranch>();
    public List<TemplateNode>   Else = null;



    public override void Render( RenderScope Scope )
    {
      foreach ( var branch in Branches )
      {
        if ( TemplateValues.IsTruthy( branch.Condition.Evaluate( Scope ) ) )
        {
          foreach ( var node in branch.Body )
          {
            node.Render( Scope );
          }
          return;
        }
      }
      if ( Else != null )
      {
        foreach ( var node in Else )
        {
          node.Render( Scope );
        }
      }
    }
  }



  // ------------------------------------------------------------ expressions

  /// <summary>An expression; Type is set by the compiler (TemplateType.Error when binding failed).</summary>
  public abstract class Expression
  {
    public int              Line = 0;
    public int              Column = 0;
    public TemplateType     Type = null;



    public abstract object Evaluate( RenderScope Scope );



    /// <summary>The expression as an error message shows it.</summary>
    public abstract string Display
    {
      get;
    }
  }



  public class LiteralExpression : Expression
  {
    public object   Value = null;



    public override object Evaluate( RenderScope Scope )
    {
      return Value;
    }



    public override string Display
    {
      get
      {
        if ( Value is string )
        {
          return "\"" + ( (string)Value ).Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ) + "\"";
        }
        return TemplateValues.FormatOutput( Value );
      }
    }
  }



  public class PathSegment
  {
    public string   Name = "";
    public int      Line = 0;
    public int      Column = 0;
  }



  public enum PathRoot
  {
    Unbound,
    RootField,
    Variable,
    Loop
  }



  /// <summary>a.b.c - a root name, a loop variable or "loop", then fields. Fields of a null object yield that type's zero value.</summary>
  public class PathExpression : Expression
  {
    public List<PathSegment>    Segments = new List<PathSegment>();
    public PathRoot             Root = PathRoot.Unbound;
    public TemplateField        RootField = null;
    public int                  Slot = -1;
    /// <summary>The fields after the first segment, bound by the compiler.</summary>
    public List<TemplateField>  Fields = new List<TemplateField>();



    public override object Evaluate( RenderScope Scope )
    {
      object current;
      switch ( Root )
      {
        case PathRoot.RootField:
          current = RootField.Getter( Scope.Root );
          break;
        case PathRoot.Variable:
          current = Scope.Slots[Slot];
          break;
        case PathRoot.Loop:
          current = Scope.Loop;
          break;
        default:
          return TemplateValues.ZeroValueOf( Type );
      }
      foreach ( var field in Fields )
      {
        current = ( current == null ) ? null : field.Getter( current );
      }
      return current ?? TemplateValues.ZeroValueOf( Type );
    }



    public override string Display
    {
      get
      {
        var names = new List<string>();
        foreach ( var segment in Segments )
        {
          names.Add( segment.Name );
        }
        return string.Join( ".", names.ToArray() );
      }
    }
  }



  public enum UnaryOperator
  {
    Not,
    Negate
  }



  public class UnaryExpression : Expression
  {
    public UnaryOperator    Operator = UnaryOperator.Not;
    public Expression       Operand = null;



    public override object Evaluate( RenderScope Scope )
    {
      object value = Operand.Evaluate( Scope );
      if ( Operator == UnaryOperator.Not )
      {
        return !TemplateValues.IsTruthy( value );
      }
      return unchecked( -TemplateValues.ToInt( value ) );
    }



    public override string Display
    {
      get
      {
        return ( ( Operator == UnaryOperator.Not ) ? "not " : "-" ) + Operand.Display;
      }
    }
  }



  public enum BinaryOperator
  {
    And,
    Or,
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    Add,
    Subtract,
    Multiply
  }



  public class BinaryExpression : Expression
  {
    public BinaryOperator   Operator = BinaryOperator.And;
    public Expression       Left = null;
    public Expression       Right = null;



    public static string Symbol( BinaryOperator Operator )
    {
      switch ( Operator )
      {
        case BinaryOperator.And:
          return "and";
        case BinaryOperator.Or:
          return "or";
        case BinaryOperator.Equal:
          return "==";
        case BinaryOperator.NotEqual:
          return "!=";
        case BinaryOperator.Less:
          return "<";
        case BinaryOperator.LessEqual:
          return "<=";
        case BinaryOperator.Greater:
          return ">";
        case BinaryOperator.GreaterEqual:
          return ">=";
        case BinaryOperator.Add:
          return "+";
        case BinaryOperator.Subtract:
          return "-";
        default:
          return "*";
      }
    }



    public override object Evaluate( RenderScope Scope )
    {
      switch ( Operator )
      {
        case BinaryOperator.And:
          return TemplateValues.IsTruthy( Left.Evaluate( Scope ) ) && TemplateValues.IsTruthy( Right.Evaluate( Scope ) );
        case BinaryOperator.Or:
          return TemplateValues.IsTruthy( Left.Evaluate( Scope ) ) || TemplateValues.IsTruthy( Right.Evaluate( Scope ) );
        case BinaryOperator.Equal:
          return TemplateValues.ValuesEqual( Left.Evaluate( Scope ), Right.Evaluate( Scope ) );
        case BinaryOperator.NotEqual:
          return !TemplateValues.ValuesEqual( Left.Evaluate( Scope ), Right.Evaluate( Scope ) );
      }
      int left  = TemplateValues.ToInt( Left.Evaluate( Scope ) );
      int right = TemplateValues.ToInt( Right.Evaluate( Scope ) );
      switch ( Operator )
      {
        case BinaryOperator.Less:
          return left < right;
        case BinaryOperator.LessEqual:
          return left <= right;
        case BinaryOperator.Greater:
          return left > right;
        case BinaryOperator.GreaterEqual:
          return left >= right;
        case BinaryOperator.Add:
          return unchecked( left + right );
        case BinaryOperator.Subtract:
          return unchecked( left - right );
        default:
          return unchecked( left * right );
      }
    }



    public override string Display
    {
      get
      {
        return Left.Display + " " + Symbol( Operator ) + " " + Right.Display;
      }
    }
  }
}
