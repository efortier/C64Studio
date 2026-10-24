using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RetroDevStudio.Formats.TextTemplate;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// The reference of a template's language and fields, built from the very
  /// registry the engine compiles against (so it can never drift): the
  /// syntax in short, then every root name with its fields as a tree. Insert
  /// (or a double-click) hands the selected field to the memo as a tag; a
  /// list field becomes a {% for %} skeleton. Modeless, kept beside the memo.
  /// </summary>
  public partial class DlgCustomExportFields : Form
  {
    /// <summary>What a tree node stands for.</summary>
    private class FieldNode
    {
      public string         Path = "";
      public TemplateField  Field = null;
      /// <summary>The loop variable the path starts with, when the node sits under a list; "" at the root.</summary>
      public string         LoopVariable = "";
      public string         LoopSource = "";
    }

    private const string SYNTAX =
      "{{ value }}                      prints a value: {{ map.id }}, {{ map.name }}\r\n" +
      "{{ number:D4 }}  {{ number:X2 }}   number formats (.NET): 0005, 1F; also :0000\r\n" +
      "{{ text | upper }}                filters: upper, lower, label (as an exported label)\r\n" +
      "{% for m in exported_maps %} ... {% endfor %}\r\n" +
      "                                  inside: loop.index (from 0), loop.index1, loop.first, loop.last, loop.count, loop.parent\r\n" +
      "{% if cond %} ... {% elif cond %} ... {% else %} ... {% endif %}\r\n" +
      "                                  == != < <= > >= and or not; an int is true when not 0, text and lists when not empty\r\n" +
      "{# comment #}                     left out of the output\r\n" +
      "{% raw %} ... {% endraw %}        literal text; a literal {{ elsewhere: {{ \"{{\" }}\r\n" +
      "\r\n" +
      "A line holding only {% %} tags (and comments) vanishes from the output, with its indentation.\r\n" +
      "Everything else is printed as typed - the indentation inside a block included.\r\n" +
      "Numbers: 12, $FF, 0x10.  Text: \"...\" with \\\" \\\\ \\n \\t.  Names are not case-sensitive.";

    private TemplateSchema          m_Schema = null;



    /// <summary>Raised with the text to put at the memo's caret.</summary>
    public event Action<string>     InsertRequested;



    public DlgCustomExportFields( StudioCore Core, TemplateSchema Schema )
    {
      InitializeComponent();
      m_Schema = Schema;
      editSyntax.Text = SYNTAX;
      editSyntax.Select( 0, 0 );
      BuildTree();
      Core.Theming.ApplyTheme( this );
    }



    private void BuildTree()
    {
      treeFields.BeginUpdate();
      treeFields.Nodes.Clear();
      foreach ( var field in m_Schema.RootType.Fields )
      {
        var node = MakeNode( field, field.Name, "", "" );
        treeFields.Nodes.Add( node );
        AddChildren( node, field.Type, field.Name, "", "", new List<TemplateType>() );
      }
      treeFields.EndUpdate();
    }



    private static TreeNode MakeNode( TemplateField Field, string Path, string LoopVariable, string LoopSource )
    {
      var node = new TreeNode( Field.Name + "  :  " + Field.Type.Name );
      node.Tag = new FieldNode() { Path = Path, Field = Field, LoopVariable = LoopVariable, LoopSource = LoopSource };
      return node;
    }



    /// <summary>
    /// The fields of Type under Parent. A list node's children are the
    /// element type's fields, addressed through the loop variable a {% for %}
    /// over the list would bind. A type already on the path is not expanded
    /// again (maps hold charsets hold maps ...).
    /// </summary>
    private void AddChildren( TreeNode Parent, TemplateType Type, string Path, string LoopVariable, string LoopSource, List<TemplateType> Stack )
    {
      if ( Type.Kind == TemplateTypeKind.List )
      {
        var element = Type.ElementType;
        if ( ( element.Kind != TemplateTypeKind.Object )
        ||   Stack.Contains( element ) )
        {
          return;
        }
        string variable = VariableNameFor( element );
        Stack.Add( element );
        foreach ( var field in element.Fields )
        {
          var node = MakeNode( field, variable + "." + field.Name, variable, Path );
          Parent.Nodes.Add( node );
          AddChildren( node, field.Type, variable + "." + field.Name, variable, Path, Stack );
        }
        Stack.RemoveAt( Stack.Count - 1 );
        return;
      }
      if ( ( Type.Kind != TemplateTypeKind.Object )
      ||   Stack.Contains( Type ) )
      {
        return;
      }
      Stack.Add( Type );
      foreach ( var field in Type.Fields )
      {
        var node = MakeNode( field, Path + "." + field.Name, LoopVariable, LoopSource );
        Parent.Nodes.Add( node );
        AddChildren( node, field.Type, Path + "." + field.Name, LoopVariable, LoopSource, Stack );
      }
      Stack.RemoveAt( Stack.Count - 1 );
    }



    private static string VariableNameFor( TemplateType Element )
    {
      string name = Element.Name;
      int underscore = name.LastIndexOf( '_' );
      if ( underscore >= 0 )
      {
        name = name.Substring( underscore + 1 );
      }
      return ( name.Length > 0 ) ? name : "item";
    }



    private static string FirstScalarField( TemplateType Type )
    {
      foreach ( var field in Type.Fields )
      {
        if ( field.Type.IsScalar )
        {
          return field.Name;
        }
      }
      return "count";
    }



    /// <summary>The text Insert produces for a node: a tag, or a loop skeleton for a list.</summary>
    private static string InsertTextOf( FieldNode Node )
    {
      var type = Node.Field.Type;
      if ( type.Kind == TemplateTypeKind.List )
      {
        string variable = VariableNameFor( type.ElementType );
        if ( type.ElementType.Kind == TemplateTypeKind.Object )
        {
          return "{% for " + variable + " in " + Node.Path + " %}\r\n\t{{ " + variable + "." + FirstScalarField( type.ElementType ) + " }}\r\n{% endfor %}\r\n";
        }
        return "{% for " + variable + " in " + Node.Path + " %}\r\n\t{{ " + variable + " }}\r\n{% endfor %}\r\n";
      }
      if ( type.Kind == TemplateTypeKind.Object )
      {
        return "{{ " + Node.Path + "." + FirstScalarField( type ) + " }}";
      }
      return "{{ " + Node.Path + " }}";
    }



    private void treeFields_AfterSelect( object sender, TreeViewEventArgs e )
    {
      var node = ( e.Node != null ) ? e.Node.Tag as FieldNode : null;
      if ( node == null )
      {
        labelDescription.Text = "";
        return;
      }
      string text = node.Field.Name + " (" + node.Field.Type.Name + "): " + node.Field.Description;
      if ( node.LoopVariable.Length > 0 )
      {
        text += "\r\nInside {% for " + node.LoopVariable + " in " + node.LoopSource + " %} ... {% endfor %}";
      }
      labelDescription.Text = text;
    }



    private void treeFields_NodeMouseDoubleClick( object sender, TreeNodeMouseClickEventArgs e )
    {
      Insert( e.Node );
    }



    private void btnInsert_Click( DecentForms.ControlBase Sender )
    {
      Insert( treeFields.SelectedNode );
    }



    private void Insert( TreeNode TreeNode )
    {
      var node = ( TreeNode != null ) ? TreeNode.Tag as FieldNode : null;
      if ( ( node == null )
      ||   ( InsertRequested == null ) )
      {
        return;
      }
      InsertRequested( InsertTextOf( node ) );
    }



    private void btnClose_Click( DecentForms.ControlBase Sender )
    {
      Close();
    }
  }
}
