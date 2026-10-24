using System;
using System.Collections.Generic;
using System.Linq;



namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>
  /// A compiled text template. Syntax: {{ value }} prints a value ({{ map.id:D4 }}
  /// formats an int, {{ map.name | upper }} applies filters); {% for x in list %}
  /// ... {% endfor %}, {% if cond %} ... {% elif cond %} ... {% else %} ... {% endif %};
  /// {# comment #}; {% raw %} ... {% endraw %} for literal text. A line holding
  /// only {% %} tags vanishes from the output; everything else, indentation
  /// included, is printed as typed. Names are case-insensitive. Compile needs
  /// only the schema: every mistake is reported with its line and column, and
  /// Render never fails for a template reason.
  /// </summary>
  public sealed class Template
  {
    private List<TemplateNode>    m_Nodes = null;
    private int                   m_SlotCount = 0;
    private int                   m_SourceLength = 0;
    private TemplateOptions       m_Options = null;



    private Template( List<TemplateNode> Nodes, int SlotCount, int SourceLength, TemplateSchema Schema, TemplateOptions Options )
    {
      m_Nodes        = Nodes;
      m_SlotCount    = SlotCount;
      m_SourceLength = SourceLength;
      m_Options      = Options;
      this.Schema    = Schema;
    }



    public TemplateSchema Schema
    {
      get;
      private set;
    }



    public static Template Compile( string Source, TemplateSchema Schema, out List<TemplateError> Errors )
    {
      return Compile( Source, Schema, new TemplateOptions(), out Errors );
    }



    /// <summary>The compiled template, or null when Errors (sorted by position) is not empty.</summary>
    public static Template Compile( string Source, TemplateSchema Schema, TemplateOptions Options, out List<TemplateError> Errors )
    {
      if ( Schema == null )
      {
        throw new ArgumentNullException( "Schema" );
      }
      var options = Options ?? new TemplateOptions();
      var errors = new List<TemplateError>();
      var tokens = TemplateLexer.Tokenize( Source ?? "", options, errors );
      var nodes = TemplateParser.Parse( tokens, options, errors );
      int slots = TemplateCompiler.Bind( nodes, Schema, options, errors );
      Errors = errors.OrderBy( e => e.Line ).ThenBy( e => e.Column ).ToList();
      if ( Errors.Count > 0 )
      {
        return null;
      }
      return new Template( nodes, slots, ( Source ?? "" ).Length, Schema, options );
    }



    /// <summary>Renders against Root, the instance the schema's root getters take.</summary>
    public string Render( object Root )
    {
      var scope = new RenderScope( Root, m_SlotCount, m_Options, m_SourceLength );
      foreach ( var node in m_Nodes )
      {
        node.Render( scope );
      }
      return scope.Output.ToString();
    }
  }
}
