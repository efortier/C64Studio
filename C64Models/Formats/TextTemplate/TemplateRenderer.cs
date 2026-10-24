using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>The "loop" object of a running {% for %}.</summary>
  public class LoopState
  {
    public int          Index = 0;
    public int          Count = 0;
    public LoopState    Parent = null;
  }



  /// <summary>The state of one Render call: the root instance, the loop variable slots, the innermost loop and the output.</summary>
  public class RenderScope
  {
    public object           Root = null;
    public object[]         Slots = null;
    public LoopState        Loop = null;
    public StringBuilder    Output = null;
    public TemplateOptions  Options = null;



    public RenderScope( object Root, int SlotCount, TemplateOptions Options, int Capacity )
    {
      this.Root    = Root;
      this.Slots   = new object[Math.Max( SlotCount, 1 )];
      this.Options = Options;
      this.Output  = new StringBuilder( Math.Max( Capacity, 16 ) );
    }
  }



  /// <summary>Value rules shared by the compiler and the renderer.</summary>
  public static class TemplateValues
  {
    private static readonly object[]  s_EmptyList = new object[0];



    public static bool IsInteger( object Value )
    {
      return ( Value is int ) || ( Value is byte ) || ( Value is sbyte ) || ( Value is short ) || ( Value is ushort )
          || ( Value is long ) || ( Value is ulong ) || ( Value is uint );
    }



    /// <summary>Any integer type as an Int32, wrapping silently; anything else is 0.</summary>
    public static int ToInt( object Value )
    {
      if ( Value is int )
      {
        return (int)Value;
      }
      if ( Value is byte )
      {
        return (byte)Value;
      }
      if ( Value is sbyte )
      {
        return (sbyte)Value;
      }
      if ( Value is short )
      {
        return (short)Value;
      }
      if ( Value is ushort )
      {
        return (ushort)Value;
      }
      if ( Value is long )
      {
        return unchecked( (int)(long)Value );
      }
      if ( Value is ulong )
      {
        return unchecked( (int)(ulong)Value );
      }
      if ( Value is uint )
      {
        return unchecked( (int)(uint)Value );
      }
      return 0;
    }



    /// <summary>bool as is; an int is true when not 0; a string or list when not empty; an object when not null.</summary>
    public static bool IsTruthy( object Value )
    {
      if ( Value == null )
      {
        return false;
      }
      if ( Value is bool )
      {
        return (bool)Value;
      }
      if ( Value is string )
      {
        return ( (string)Value ).Length > 0;
      }
      if ( Value is IList )
      {
        return ( (IList)Value ).Count > 0;
      }
      if ( IsInteger( Value ) )
      {
        return ToInt( Value ) != 0;
      }
      return true;
    }



    /// <summary>0, "", false, an empty list, or null for an object: what a field of a null object yields.</summary>
    public static object ZeroValueOf( TemplateType Type )
    {
      if ( Type == null )
      {
        return null;
      }
      switch ( Type.Kind )
      {
        case TemplateTypeKind.Int:
          return 0;
        case TemplateTypeKind.String:
          return "";
        case TemplateTypeKind.Bool:
          return false;
        case TemplateTypeKind.List:
          return s_EmptyList;
        default:
          return null;
      }
    }



    /// <summary>The text an output tag prints for a scalar value.</summary>
    public static string FormatOutput( object Value )
    {
      if ( Value == null )
      {
        return "";
      }
      if ( Value is string )
      {
        return (string)Value;
      }
      if ( Value is bool )
      {
        return ( (bool)Value ) ? "true" : "false";
      }
      if ( IsInteger( Value ) )
      {
        return ToInt( Value ).ToString( CultureInfo.InvariantCulture );
      }
      return Value.ToString();
    }



    public static bool ValuesEqual( object Left, object Right )
    {
      if ( IsInteger( Left ) && IsInteger( Right ) )
      {
        return ToInt( Left ) == ToInt( Right );
      }
      if ( ( Left is string ) && ( Right is string ) )
      {
        return string.Equals( (string)Left, (string)Right, StringComparison.Ordinal );
      }
      if ( ( Left is bool ) && ( Right is bool ) )
      {
        return (bool)Left == (bool)Right;
      }
      return object.Equals( Left, Right );
    }



    /// <summary>Whether .NET accepts Spec as a number format (tried on 0, -1 and the largest int).</summary>
    public static bool IsValidNumberFormat( string Spec )
    {
      try
      {
        0.ToString( Spec, CultureInfo.InvariantCulture );
        ( -1 ).ToString( Spec, CultureInfo.InvariantCulture );
        int.MaxValue.ToString( Spec, CultureInfo.InvariantCulture );
        return true;
      }
      catch ( FormatException )
      {
        return false;
      }
    }
  }
}
