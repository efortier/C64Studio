extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// The Custom Exports tab in a REAL MapEditor: loading fills the list and
  /// the fields without dirtying, every field edit lands in the model and
  /// dirties once, Add / Delete (multi-select) / Clear / reorder / Active are
  /// undoable whole-list steps, a renamed definition keeps its identity, and
  /// a refresh re-selects by ID.
  /// </summary>
  [TestClass]
  public class TestCustomExportEditor
  {
    /// <summary>A MapEditor whose confirmations are answered by the test.</summary>
    private class AnsweringMapEditor : MapEditor
    {
      public int      DeleteAsked = 0;
      public int      ClearAsked = 0;
      public bool     Answer = true;

      public AnsweringMapEditor( StudioCore Core ) :
        base( Core )
      {
      }

      protected override bool AskDeleteCustomExports( List<string> Names )
      {
        ++DeleteAsked;
        return Answer;
      }

      protected override bool AskClearCustomExports( int Count )
      {
        ++ClearAsked;
        return Answer;
      }
    }



    // ---------------------------------------------------------------- harness

    private static void CreateHandles( Control Control )
    {
      var handle = Control.Handle;
      foreach ( Control child in Control.Controls )
      {
        CreateHandles( child );
      }
    }



    private static object Field( object Owner, string Name )
    {
      var field = typeof( MapEditor ).GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, "MapEditor field '" + Name + "' not found" );
      return field.GetValue( Owner );
    }



    private static object Invoke( MapEditor Editor, string Method, params object[] Arguments )
    {
      var method = typeof( MapEditor ).GetMethod( Method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( method, "MapEditor method '" + Method + "' not found" );
      object result = method.Invoke( Editor, Arguments );
      Application.DoEvents();
      return result;
    }



    private static void ClickButton( MapEditor Editor, string Handler )
    {
      Invoke( Editor, Handler, new object[] { null } );
    }



    private static ListView List( MapEditor Editor )
    {
      return (ListView)Field( Editor, "listCustomExports" );
    }



    private static string GetText( object Control )
    {
      return (string)Control.GetType().GetProperty( "Text" ).GetValue( Control );
    }



    private static void SetText( object Control, string Text )
    {
      Control.GetType().GetProperty( "Text" ).SetValue( Control, Text );
      Application.DoEvents();
    }



    private static void SelectRows( ListView List, params int[] Rows )
    {
      List.SelectedItems.Clear();
      foreach ( int row in Rows )
      {
        List.Items[row].Selected = true;
      }
      Application.DoEvents();
    }



    private static int[] IDs( StudioMapProject Project )
    {
      return Project.CustomExports.Select( e => e.ID ).ToArray();
    }



    private const string CONTENT_FILES = "{% for m in exported_maps %}\r\n\tRefAddFile(\"MAP-{{ m.id:D4 }}\")\r\n{% endfor %}\r\n";



    /// <summary>One map; definitions Files (ID 1), Mixed (ID 2, inactive, LF + non-ASCII content) and Third (ID 3).</summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      var tile = new MapProject.Tile() { Name = "T" };
      tile.Chars.Resize( 1, 1 );
      project.Charsets[0].Tiles.Add( tile );
      project.Charsets[0].ReindexTiles();
      var map = new MapProject.Map() { Name = "Alpha", MapID = 1, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      project.Maps.Add( map );

      var files = project.AddCustomExport( "Files" );
      files.FileName = "map_files.asm";
      files.Content  = CONTENT_FILES;
      var mixed = project.AddCustomExport( "Mixed" );
      mixed.Active      = false;
      mixed.Directory   = "..\\src";
      mixed.FileName    = "names.inc";
      mixed.Description = "lf\nonly";
      mixed.Content     = "a\nb é";
      project.AddCustomExport( "Third" ).FileName = "third.txt";
      return project;
    }



    private static void WithEditor( MapProject Project, Action<AnsweringMapEditor, StudioMapProject> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioCustomExportEditor_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, Project.SaveToBuffer().Data() );
        try
        {
          using ( var editor = new AnsweringMapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();
            Body( editor, (StudioMapProject)Field( editor, "m_MapProject" ) );
          }
        }
        finally
        {
          foreach ( var file in new[] { path, Path.ChangeExtension( path, ".mapscratch" ), Path.ChangeExtension( path, ".mapoutlines" ) } )
          {
            try
            {
              if ( File.Exists( file ) )
              {
                File.Delete( file );
              }
            }
            catch ( IOException )
            {
            }
          }
        }
      } );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestLoadFillsTheListAndFieldsWithoutDirtying()
    {
      WithEditor( BuildProject(), ( editor, project ) =>
      {
        var list = List( editor );
        Assert.AreEqual( 3, list.Items.Count );
        Assert.AreEqual( 1, list.SelectedIndices.Count );
        Assert.AreEqual( 0, list.SelectedIndices[0], "row 0 is selected on load" );
        Assert.AreEqual( "Files", list.Items[0].Text );
        Assert.AreEqual( "yes", list.Items[0].SubItems[1].Text );
        Assert.AreEqual( "map_files.asm", list.Items[0].SubItems[2].Text );
        Assert.AreEqual( "no", list.Items[1].SubItems[1].Text );
        Assert.AreEqual( "Files", GetText( Field( editor, "editCustomExportName" ) ) );
        Assert.AreEqual( CONTENT_FILES, GetText( Field( editor, "memoCustomExportContent" ) ), "the memo shows the template verbatim" );
        Assert.IsTrue( (bool)Field( editor, "checkCustomExportActive" ).GetType().GetProperty( "Checked" ).GetValue( Field( editor, "checkCustomExportActive" ) ) );
        Assert.IsFalse( editor.Modified, "loading" );

        SelectRows( list, 1 );
        Assert.AreEqual( "Mixed", GetText( Field( editor, "editCustomExportName" ) ) );
        Assert.AreEqual( "..\\src", GetText( Field( editor, "editCustomExportDirectory" ) ) );
        Assert.AreEqual( "lf\r\nonly", GetText( Field( editor, "editCustomExportDescription" ) ), "normalized on load" );
        Assert.AreEqual( "a\r\nb é", GetText( Field( editor, "memoCustomExportContent" ) ) );
        Assert.IsFalse( (bool)Field( editor, "checkCustomExportActive" ).GetType().GetProperty( "Checked" ).GetValue( Field( editor, "checkCustomExportActive" ) ) );
        Assert.IsFalse( editor.Modified, "selecting another definition" );

        SelectRows( list, 0, 2 );
        Assert.IsFalse( (bool)Field( editor, "editCustomExportName" ).GetType().GetProperty( "Enabled" ).GetValue( Field( editor, "editCustomExportName" ) ), "fields are off with several rows selected" );
        Assert.IsFalse( editor.Modified, "multi-selecting" );

        editor.RefreshDisplayOptions();
        Application.DoEvents();
        Assert.IsFalse( editor.Modified, "a display refresh" );
      } );
    }



    [TestMethod]
    public void TestFieldEditsLandInTheModelAndDirty()
    {
      WithEditor( BuildProject(), ( editor, project ) =>
      {
        var list = List( editor );
        SetText( Field( editor, "editCustomExportName" ), "Renamed" );
        Assert.AreEqual( "Renamed", project.CustomExports[0].Name );
        Assert.AreEqual( 1, project.CustomExports[0].ID, "the ID is the identity, the name is free" );
        Assert.AreEqual( "Renamed", list.Items[0].Text, "the row follows" );
        Assert.IsTrue( editor.Modified );

        SetText( Field( editor, "editCustomExportFileName" ), "other.asm" );
        Assert.AreEqual( "other.asm", project.CustomExports[0].FileName );
        Assert.AreEqual( "other.asm", list.Items[0].SubItems[2].Text );

        SetText( Field( editor, "editCustomExportDirectory" ), "gen" );
        Assert.AreEqual( "gen", project.CustomExports[0].Directory );

        SetText( Field( editor, "editCustomExportDescription" ), "one\r\ntwo" );
        Assert.AreEqual( "one\r\ntwo", project.CustomExports[0].Description );

        SetText( Field( editor, "memoCustomExportContent" ), "{{ project.name }}\n\t{{ exported_maps.count }}" );
        Assert.AreEqual( "{{ project.name }}\r\n\t{{ exported_maps.count }}", project.CustomExports[0].Content, "CRLF canonical, tab kept" );

        var check = Field( editor, "checkCustomExportActive" );
        check.GetType().GetProperty( "Checked" ).SetValue( check, false );
        Application.DoEvents();
        Assert.IsFalse( project.CustomExports[0].Active );
        Assert.AreEqual( "no", list.Items[0].SubItems[1].Text );
        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        Assert.IsTrue( project.CustomExports[0].Active, "the Active toggle is a project undo step" );
        Assert.AreEqual( "yes", list.Items[0].SubItems[1].Text );
        Assert.AreEqual( "Renamed", project.CustomExports[0].Name, "undoing Active leaves the typed name" );
      } );
    }



    [TestMethod]
    public void TestAddDeleteClearWithUndo()
    {
      WithEditor( BuildProject(), ( editor, project ) =>
      {
        var list = List( editor );
        ClickButton( editor, "btnCustomExportAdd_Click" );
        Assert.AreEqual( 4, project.CustomExports.Count );
        Assert.AreEqual( 4, project.CustomExports[3].ID );
        Assert.AreEqual( "Export 4", project.CustomExports[3].Name );
        Assert.IsTrue( project.CustomExports[3].Active );
        Assert.AreEqual( 3, list.SelectedIndices[0], "the new definition is selected" );
        Assert.AreEqual( "Export 4", GetText( Field( editor, "editCustomExportName" ) ) );
        Assert.IsTrue( editor.Modified );

        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        CollectionAssert.AreEqual( new[] { 1, 2, 3 }, IDs( project ) );
        Assert.AreEqual( 3, list.Items.Count );
        editor.DocumentInfo.UndoManager.Redo();
        Application.DoEvents();
        CollectionAssert.AreEqual( new[] { 1, 2, 3, 4 }, IDs( project ) );
        Assert.AreEqual( 3, list.SelectedIndices[0], "redo re-selects the added definition" );

        SelectRows( list, 0, 2 );
        ClickButton( editor, "btnCustomExportDelete_Click" );
        Assert.AreEqual( 1, editor.DeleteAsked );
        CollectionAssert.AreEqual( new[] { 2, 4 }, IDs( project ), "both selected rows are gone" );
        Assert.AreEqual( 0, list.SelectedIndices[0], "the row that moved up is selected" );
        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        CollectionAssert.AreEqual( new[] { 1, 2, 3, 4 }, IDs( project ), "undo restores the order" );

        editor.Answer = false;
        SelectRows( list, 1 );
        ClickButton( editor, "btnCustomExportDelete_Click" );
        Assert.AreEqual( 2, editor.DeleteAsked );
        Assert.AreEqual( 4, project.CustomExports.Count, "Cancel deletes nothing" );
        editor.Answer = true;

        ClickButton( editor, "btnCustomExportClear_Click" );
        Assert.AreEqual( 1, editor.ClearAsked );
        Assert.AreEqual( 0, project.CustomExports.Count );
        Assert.AreEqual( 0, list.Items.Count );
        Assert.IsFalse( (bool)Field( editor, "editCustomExportName" ).GetType().GetProperty( "Enabled" ).GetValue( Field( editor, "editCustomExportName" ) ) );
        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        CollectionAssert.AreEqual( new[] { 1, 2, 3, 4 }, IDs( project ) );
        Assert.AreEqual( "Mixed", project.CustomExports[1].Name );
        Assert.AreEqual( "a\r\nb é", project.CustomExports[1].Content, "the snapshot keeps every field" );
      } );
    }



    [TestMethod]
    public void TestReorderWithUndo()
    {
      WithEditor( BuildProject(), ( editor, project ) =>
      {
        var list = List( editor );
        int newFirst = (int)Invoke( editor, "ReorderCustomExports", new List<int>() { 2 }, 0 );
        Assert.AreEqual( 0, newFirst );
        CollectionAssert.AreEqual( new[] { 3, 1, 2 }, IDs( project ) );
        Assert.AreEqual( "Third", list.Items[0].Text );
        Assert.IsTrue( editor.Modified );

        Assert.AreEqual( -1, (int)Invoke( editor, "ReorderCustomExports", new List<int>() { 0 }, 0 ), "dropping in place changes nothing" );
        CollectionAssert.AreEqual( new[] { 3, 1, 2 }, IDs( project ) );

        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        CollectionAssert.AreEqual( new[] { 1, 2, 3 }, IDs( project ), "the in-place drop pushed no undo step" );
        Assert.AreEqual( "Files", list.Items[0].Text );

        newFirst = (int)Invoke( editor, "ReorderCustomExports", new List<int>() { 0, 1 }, 3 );
        Assert.AreEqual( 1, newFirst );
        CollectionAssert.AreEqual( new[] { 3, 1, 2 }, IDs( project ), "a block moves as one" );
      } );
    }



    [TestMethod]
    public void TestRefreshReselectsByID()
    {
      WithEditor( BuildProject(), ( editor, project ) =>
      {
        var list = List( editor );
        SelectRows( list, 2 );
        Assert.AreEqual( 3, editor.SelectedCustomExportID );
        editor.RefreshCustomExportList( -1 );
        Application.DoEvents();
        Assert.AreEqual( 2, list.SelectedIndices[0], "the selection survives a rebuild" );
        editor.RefreshCustomExportList( 1 );
        Application.DoEvents();
        Assert.AreEqual( 0, list.SelectedIndices[0] );
        Assert.AreEqual( "Files", GetText( Field( editor, "editCustomExportName" ) ) );
        Assert.IsFalse( editor.Modified, "refreshing never dirties" );
      } );
    }
  }
}
