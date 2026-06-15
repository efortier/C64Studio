using System;
using System.Collections.Generic;



namespace RetroDevStudio.Settings
{
  /// <summary>
  /// The Start Page's own recent-files store: every file the user opened,
  /// kept WITHOUT a count limit (deliberately independent of the MRU
  /// preference's MRUMaxCount), with a per-entry "pinned" flag. Persisted as
  /// a single INI file (startpage.ini) in the application main directory,
  /// written atomically (tmp + move, mirroring MainForm.SaveSettings).
  /// Loading is lazy and tolerant — a missing or damaged file simply yields
  /// an empty list, and unknown sections are ignored so future Start Page
  /// settings can share the same file.
  /// </summary>
  public class StartPageRecentFiles
  {
    public class Entry
    {
      public string   Path = "";
      public bool     Pinned = false;
    }

    // Most-recently-opened first. Pin state does not affect the stored
    // order; SortedEntries() lifts pinned entries to the top for display.
    private List<Entry>     m_Entries = null;   // null = not loaded yet

    // Editor option: keep the Start Page open after launching a file from it
    // (default true). Persisted in the [Settings] section of the same ini.
    private bool            m_KeepStartPageOpen = true;



    public string IniFilename
    {
      get
      {
        return GR.Path.Append( System.Windows.Forms.Application.StartupPath, "startpage.ini" );
      }
    }



    /// <summary>
    /// When true (default), opening a file from the Start Page leaves the page
    /// open; when false, the page closes after launching the file. Persisted
    /// in the [Settings] section of startpage.ini.
    /// </summary>
    public bool KeepStartPageOpen
    {
      get
      {
        EnsureLoaded();
        return m_KeepStartPageOpen;
      }
      set
      {
        EnsureLoaded();
        if ( m_KeepStartPageOpen == value )
        {
          return;
        }
        m_KeepStartPageOpen = value;
        Save();
      }
    }



    private void EnsureLoaded()
    {
      if ( m_Entries != null )
      {
        return;
      }
      m_Entries = new List<Entry>();
      m_KeepStartPageOpen = true;   // default when the file omits the setting

      // Missing file (first run) yields "" — empty list, defaults kept.
      string content = GR.IO.File.ReadAllText( IniFilename );
      if ( string.IsNullOrEmpty( content ) )
      {
        return;
      }

      string section = "";
      foreach ( var rawLine in content.Split( '\n' ) )
      {
        string line = rawLine.Trim();
        if ( ( line.Length == 0 )
        ||   ( line.StartsWith( ";" ) ) )
        {
          continue;
        }
        if ( line.StartsWith( "[" ) )
        {
          section = line;
          continue;
        }
        if ( string.Compare( section, "[Settings]", true ) == 0 )
        {
          // key=value settings (currently just KeepOpen=0|1).
          int eq = line.IndexOf( '=' );
          if ( eq > 0 )
          {
            string key = line.Substring( 0, eq ).Trim();
            string val = line.Substring( eq + 1 ).Trim();
            if ( string.Compare( key, "KeepOpen", true ) == 0 )
            {
              m_KeepStartPageOpen = ( val != "0" );
            }
          }
          continue;
        }
        if ( string.Compare( section, "[Files]", true ) != 0 )
        {
          continue;
        }
        // Entry line: "<0|1>|<path>" — pipe separator because '=' is a
        // legal character in Windows file names.
        int sep = line.IndexOf( '|' );
        if ( sep < 1 )
        {
          continue;
        }
        string  path   = line.Substring( sep + 1 ).Trim();
        bool    pinned = ( line.Substring( 0, sep ).Trim() == "1" );
        if ( ( path.Length == 0 )
        ||   ( FindEntry( path ) != null ) )
        {
          continue;
        }
        m_Entries.Add( new Entry() { Path = path, Pinned = pinned } );
      }
    }



    private void Save()
    {
      var sb = new System.Text.StringBuilder();
      sb.AppendLine( "; C64Studio Start Page settings" );
      sb.AppendLine( "[Settings]" );
      sb.AppendLine( "KeepOpen=" + ( m_KeepStartPageOpen ? "1" : "0" ) );
      sb.AppendLine();
      sb.AppendLine( "[Files]" );
      foreach ( var entry in m_Entries )
      {
        sb.Append( entry.Pinned ? "1|" : "0|" );
        sb.AppendLine( entry.Path );
      }

      // Atomic-ish write: tmp file first, then swap — a crash mid-write
      // can't destroy the existing list (same pattern as SaveSettings).
      string filename = IniFilename;
      try
      {
        System.IO.File.WriteAllText( filename + ".tmp", sb.ToString() );
        if ( System.IO.File.Exists( filename ) )
        {
          System.IO.File.Delete( filename );
        }
        System.IO.File.Move( filename + ".tmp", filename );
      }
      catch ( Exception )
      {
        // The application directory may not be writable (non-portable
        // install) — losing recent-list persistence must never break the
        // app or the open action that triggered the save.
      }
    }



    private Entry FindEntry( string Filename )
    {
      foreach ( var entry in m_Entries )
      {
        if ( string.Compare( entry.Path, Filename, true ) == 0 )
        {
          return entry;
        }
      }
      return null;
    }



    private static string NormalizePath( string Filename )
    {
      try
      {
        return System.IO.Path.GetFullPath( Filename );
      }
      catch ( Exception )
      {
        return Filename;
      }
    }



    /// <summary>
    /// Record a user-initiated file open: new entries go to the front,
    /// existing entries move to the front keeping their pin state. The
    /// list is never trimmed.
    /// </summary>
    public void RecordOpen( string Filename )
    {
      if ( string.IsNullOrEmpty( Filename ) )
      {
        return;
      }
      EnsureLoaded();

      string path = NormalizePath( Filename );
      var entry = FindEntry( path );
      if ( entry != null )
      {
        m_Entries.Remove( entry );
        entry.Path = path;
      }
      else
      {
        entry = new Entry() { Path = path };
      }
      m_Entries.Insert( 0, entry );
      Save();
    }



    public void TogglePin( Entry ToToggle )
    {
      EnsureLoaded();
      ToToggle.Pinned = !ToToggle.Pinned;
      Save();
    }



    public void Remove( Entry ToRemove )
    {
      EnsureLoaded();
      m_Entries.Remove( ToRemove );
      Save();
    }



    /// <summary>
    /// Clear the history — removes every entry that is NOT pinned. Pinned
    /// entries are the user's favorites and survive a clear.
    /// </summary>
    public void ClearHistory()
    {
      EnsureLoaded();
      m_Entries.RemoveAll( entry => !entry.Pinned );
      Save();
    }



    /// <summary>
    /// Display order: pinned entries first, then the rest — most recently
    /// opened first within each group.
    /// </summary>
    public List<Entry> SortedEntries()
    {
      EnsureLoaded();

      var sorted = new List<Entry>( m_Entries.Count );
      foreach ( var entry in m_Entries )
      {
        if ( entry.Pinned )
        {
          sorted.Add( entry );
        }
      }
      foreach ( var entry in m_Entries )
      {
        if ( !entry.Pinned )
        {
          sorted.Add( entry );
        }
      }
      return sorted;
    }
  }
}
