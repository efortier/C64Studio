using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Undo entry for a character set's editor-side metadata: display name,
  /// export name and the "Export character set" flag.
  /// </summary>
  public class UndoMapCharsetMetaChange : UndoTask
  {
    private MapEditor              _Editor = null;
    private MapProject.MapCharset  _Charset = null;
    private string                 _DisplayName = "";
    private string                 _ExportName = "";
    private bool                   _ExportEnabled = true;



    public UndoMapCharsetMetaChange( MapEditor Editor, MapProject.MapCharset Charset )
    {
      _Editor        = Editor;
      _Charset       = Charset;
      _DisplayName   = Charset.DisplayName;
      _ExportName    = Charset.ExportName;
      _ExportEnabled = Charset.ExportEnabled;
    }



    public override string Description
    {
      get
      {
        return "Character Set Settings Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapCharsetMetaChange( _Editor, _Charset );
    }



    public override void Apply()
    {
      _Charset.DisplayName   = _DisplayName;
      _Charset.ExportName    = _ExportName;
      _Charset.ExportEnabled = _ExportEnabled;
      _Editor.CharsetMetaChanged( _Charset );
      _Editor.SetModified();
    }
  }
}
