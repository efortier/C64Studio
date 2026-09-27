using System.Collections.Generic;
using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Undo entry for adding / duplicating / removing a character set of a map
  /// project. Identified by the MapCharset OBJECT, not only by its index: an
  /// undo of an add removes that object wherever it is, and re-inserting is
  /// skipped when the object is still in the list. The undo manager creates
  /// the complementary task BEFORE Apply runs, so a refused removal (a map or
  /// scratch still bound to the charset — entity-type edits have no undo, so
  /// LIFO alone doesn't guarantee it) leaves both stacks consistent: the
  /// redo then finds the charset present and does nothing.
  /// The model pair Remove/Insert is an exact inverse for live maps; a
  /// revision snapshot that pointed at a removed charset falls to 0 and
  /// stays there.
  /// </summary>
  public class UndoMapCharsetListChange : UndoTask
  {
    public enum Kind
    {
      Added,      // Apply = remove the charset again
      Removed     // Apply = put the charset back at Index
    }

    private MapEditor              _Editor = null;
    private MapProject             _Project = null;
    private Kind                   _Kind;
    private int                    _Index = -1;
    private MapProject.MapCharset  _Charset = null;
    // The Character Set tab selection at construction (restored by Removed).
    private int                    _CurrentCharsetIndex = 0;



    public UndoMapCharsetListChange( MapEditor Editor, MapProject Project, Kind Kind, int Index, MapProject.MapCharset Charset )
    {
      _Editor   = Editor;
      _Project  = Project;
      _Kind     = Kind;
      _Index    = Index;
      _Charset  = Charset;
      _CurrentCharsetIndex = Project.CurrentCharsetIndex;
    }



    public override string Description
    {
      get
      {
        return ( _Kind == Kind.Added ) ? "Add Character Set" : "Remove Character Set";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapCharsetListChange( _Editor, _Project,
                                           ( _Kind == Kind.Added ) ? Kind.Removed : Kind.Added,
                                           _Index, _Charset );
    }



    public override void Apply()
    {
      if ( _Kind == Kind.Added )
      {
        int index = _Project.Charsets.IndexOf( _Charset );
        if ( index < 0 )
        {
          return;
        }
        if ( !_Project.RemoveCharset( index, _Editor.ScratchWorkspaces() ) )
        {
          List<string> users;
          _Project.CanRemoveCharset( index, out users );
          System.Windows.Forms.MessageBox.Show(
            "Character set '" + _Project.CharsetDisplayNameOf( _Charset ) + "' cannot be removed - it is still used by: "
            + string.Join( ", ", users ) + ".",
            "Undo refused",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning );
          return;
        }
      }
      else
      {
        if ( !_Project.Charsets.Contains( _Charset ) )
        {
          _Project.InsertCharset( _Index, _Charset, _Editor.ScratchWorkspaces() );
        }
        if ( ( _CurrentCharsetIndex >= 0 )
        &&   ( _CurrentCharsetIndex < _Project.Charsets.Count ) )
        {
          _Project.CurrentCharsetIndex = _CurrentCharsetIndex;
        }
      }
      _Editor.CharsetListChanged();
      _Editor.SetModified();
    }
  }
}
