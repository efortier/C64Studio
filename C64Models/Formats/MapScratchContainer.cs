using System;
using System.Collections.Generic;



namespace RetroDevStudio.Formats
{
  /// <summary>
  /// Sidecar container for per-map SCRATCH maps (the map editor's F12
  /// workspace: an independently-sized map for parking pieces of its owner
  /// while reorganizing). Lives next to the .mapproject as
  /// "&lt;projectfile&gt;.mapscratch" and is flushed together with the document
  /// (save / close) — scratch edits dirty the document like main-map edits.
  ///
  /// Entries are keyed by the OWNER map's OutlineGuid (the only durable
  /// map identity; list indices shift on add/delete — the same contract as
  /// the outline sidecar). Blobs are full nested MAP chunks (BuildMapChunk
  /// format) kept UNDECODED in memory: reading the file never deserializes
  /// a map, and rewriting re-emits retained blobs verbatim — only a scratch
  /// actually visited in the session ever pays a decode, keeping project
  /// load fast (lazy-load contract).
  ///
  /// Orphan policy: intermediate writes keep blobs whose GUID no longer
  /// matches a live map, because a map delete can be undone much later
  /// (the undo system re-inserts the same Map object, GUID intact). Only
  /// the final write on document close prunes to live GUIDs — that is the
  /// point where "deleting a map deletes its scratch" becomes permanent,
  /// and it also self-heals files that kept orphans across a crash.
  /// </summary>
  public class MapScratchContainer
  {
    public class ScratchEntry
    {
      public string     OwnerGuid = "";
      // Re-association hints: the GUID's other half lives in the
      // .mapproject and only reaches disk when the USER saves — these
      // let a map with no (persisted) GUID re-adopt its scratch by name /
      // index, so workspaces survive even when the project is never saved.
      public string     OwnerMapName = "";
      public int        OwnerMapIndex = -1;
      // The scratch map itself as an OPAQUE blob (one nested MAP chunk,
      // encoded/decoded by MapProject.BuildMapChunk / ReadMapFromBody in
      // the editor layer — the container never looks inside).
      public byte[]     MapData = null;
    }



    private const uint CURRENT_VERSION = 1;

    private Dictionary<string, ScratchEntry>    m_Entries = new Dictionary<string, ScratchEntry>();

    /// <summary>
    /// The last ReadFromFile found an EXISTING file it could not parse —
    /// intrinsic state of this container's contents ("incomplete view of
    /// what is on disk"). WriteToFile then preserves the unreadable
    /// original as "&lt;file&gt;.corrupt" before replacing it, so a transient
    /// read error can never silently destroy every stored scratch map.
    /// </summary>
    public bool LoadFailed
    {
      get;
      private set;
    } = false;



    public int Count
    {
      get
      {
        return m_Entries.Count;
      }
    }



    public IEnumerable<string> EntryGuids
    {
      get
      {
        return m_Entries.Keys;
      }
    }



    public ScratchEntry GetEntry( string OwnerGuid )
    {
      if ( string.IsNullOrEmpty( OwnerGuid ) )
      {
        return null;
      }
      ScratchEntry entry;
      if ( m_Entries.TryGetValue( OwnerGuid, out entry ) )
      {
        return entry;
      }
      return null;
    }



    public bool HasEntry( string OwnerGuid )
    {
      return GetEntry( OwnerGuid ) != null;
    }



    public void SetEntry( string OwnerGuid, byte[] MapData,
                          string OwnerMapName = "", int OwnerMapIndex = -1 )
    {
      if ( ( string.IsNullOrEmpty( OwnerGuid ) )
      ||   ( MapData == null )
      ||   ( MapData.Length == 0 ) )
      {
        return;
      }
      // SetEntry recreates the entry wholesale, so the hints MUST travel
      // through this signature — attaching them to the entry afterwards
      // would be silently dropped by the next flush.
      m_Entries[OwnerGuid] = new ScratchEntry()
      {
        OwnerGuid     = OwnerGuid,
        OwnerMapName  = OwnerMapName ?? "",
        OwnerMapIndex = OwnerMapIndex,
        MapData       = MapData
      };
    }



    /// <summary>
    /// Finds an entry a GUID-less map can re-ADOPT — the self-heal for
    /// the split-brain failure where the sidecar (auto-saved) has the
    /// scratch but the .mapproject (user-saved) never persisted the
    /// GUID. Excludes GUIDs other maps already hold. Match order: exact
    /// name+index, then unique name — hints are only trusted when they
    /// identify ONE candidate.
    /// </summary>
    public ScratchEntry FindAdoptableEntry( string MapName, int MapIndex, ICollection<string> GuidsInUse )
    {
      var candidates = new List<ScratchEntry>();
      foreach ( var entry in m_Entries.Values )
      {
        if ( ( GuidsInUse != null )
        &&   ( GuidsInUse.Contains( entry.OwnerGuid ) ) )
        {
          continue;
        }
        // Entries without hints carry ""/-1 — never adoptable (no
        // evidence which map they belonged to).
        if ( ( string.IsNullOrEmpty( entry.OwnerMapName ) )
        &&   ( entry.OwnerMapIndex < 0 ) )
        {
          continue;
        }
        candidates.Add( entry );
      }

      ScratchEntry match = null;
      foreach ( var entry in candidates )
      {
        if ( ( entry.OwnerMapName == ( MapName ?? "" ) )
        &&   ( entry.OwnerMapIndex == MapIndex ) )
        {
          if ( match != null )
          {
            return null;   // ambiguous even on the strongest key
          }
          match = entry;
        }
      }
      if ( match != null )
      {
        return match;
      }
      foreach ( var entry in candidates )
      {
        if ( ( !string.IsNullOrEmpty( entry.OwnerMapName ) )
        &&   ( entry.OwnerMapName == MapName ) )
        {
          if ( match != null )
          {
            return null;
          }
          match = entry;
        }
      }
      // Deliberately NO index-only tier: after a rename + reorder, a bare index
      // match can bind ANOTHER map's scratch, which that map then overwrites —
      // destructive. An unadopted orphan stays in the sidecar and remains
      // recoverable, so name evidence is required.
      return match;
    }



    public void RemoveEntry( string OwnerGuid )
    {
      if ( !string.IsNullOrEmpty( OwnerGuid ) )
      {
        m_Entries.Remove( OwnerGuid );
      }
    }



    /// <summary>
    /// Serializes the container. When LiveGuids is non-null, only entries
    /// whose GUID it contains are written (the pruning final write); null
    /// keeps every retained entry (intermediate writes — see orphan policy
    /// in the class comment).
    /// </summary>
    public GR.Memory.ByteBuffer SaveToBuffer( ICollection<string> LiveGuids )
    {
      var projectFile = new GR.Memory.ByteBuffer();

      var chunkInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_SCRATCH_INFO );
      chunkInfo.AppendU32( CURRENT_VERSION );
      projectFile.Append( chunkInfo.ToBuffer() );

      foreach ( var entry in m_Entries.Values )
      {
        if ( ( LiveGuids != null )
        &&   ( !LiveGuids.Contains( entry.OwnerGuid ) ) )
        {
          continue;
        }
        var chunkEntry = new GR.IO.FileChunk( FileChunkConstants.MAP_SCRATCH_ENTRY );
        chunkEntry.AppendString( entry.OwnerGuid );
        chunkEntry.AppendString( entry.OwnerMapName ?? "" );
        chunkEntry.AppendI32( entry.OwnerMapIndex );
        chunkEntry.AppendU32( (uint)entry.MapData.Length );
        chunkEntry.Append( new GR.Memory.ByteBuffer( entry.MapData ) );
        projectFile.Append( chunkEntry.ToBuffer() );
      }
      return projectFile;
    }



    public bool ReadFromBuffer( GR.Memory.ByteBuffer Data )
    {
      m_Entries.Clear();
      if ( Data == null )
      {
        return false;
      }
      if ( Data.Length == 0 )
      {
        return true;
      }

      var memReader = new GR.IO.MemoryReader( Data );
      var chunk = new GR.IO.FileChunk();
      bool sawInfoChunk = false;
      bool tailDamaged = false;

      while ( true )
      {
        var posBeforeChunk = memReader.Position;
        if ( !chunk.ReadFromStream( memReader ) )
        {
          // A clean end never consumes bytes (the too-short-for-a-header
          // pre-check fails with the position unmoved). Movement means a
          // chunk header promised more payload than the stream holds —
          // the partial read consumes the stream to its end, so WITHOUT
          // this flag a truncated tail would pass the position==length
          // check below and masquerade as "intact minus the last entry".
          tailDamaged = ( memReader.Position != posBeforeChunk );
          break;
        }
        var chunkReader = chunk.MemoryReader();
        switch ( chunk.Type )
        {
          case FileChunkConstants.MAP_SCRATCH_INFO:
            // version currently unused (1); newer writers may append fields
            // after it, older readers simply ignore what they don't know.
            chunkReader.ReadUInt32();
            sawInfoChunk = true;
            break;
          case FileChunkConstants.MAP_SCRATCH_ENTRY:
            {
              string  guid     = chunkReader.ReadString();
              string  mapName  = chunkReader.ReadString();
              int     mapIndex = chunkReader.ReadInt32();
              uint    length   = chunkReader.ReadUInt32();

              var mapData = new GR.Memory.ByteBuffer();
              if ( ( length > 0 )
              &&   ( chunkReader.ReadBlock( mapData, length ) == length ) )
              {
                // Fields appended after the blob by NEWER writers are
                // simply not read here — append-tolerant by construction.
                SetEntry( guid, mapData.Data(), mapName, mapIndex );
              }
            }
            break;
        }
      }
      // Corruption detection: every writer emits MAP_SCRATCH_INFO first,
      // and a clean parse consumes the whole stream. Truncated garbage
      // must NOT masquerade as "valid but empty" — that would let the
      // next write clobber a possibly recoverable file (see LoadFailed).
      bool intact = ( sawInfoChunk )
                 && ( !tailDamaged )
                 && ( memReader.Position == Data.Length );
      memReader.Close();
      if ( !intact )
      {
        m_Entries.Clear();
      }
      return intact;
    }



    /// <summary>
    /// Missing file = empty container and TRUE (a project simply has no
    /// scratch maps yet); unreadable/corrupt file = empty container and FALSE.
    /// </summary>
    public bool ReadFromFile( string Filename )
    {
      m_Entries.Clear();
      LoadFailed = false;
      if ( ( string.IsNullOrEmpty( Filename ) )
      ||   ( !System.IO.File.Exists( Filename ) ) )
      {
        return true;
      }
      var data = GR.IO.File.ReadAllBytes( Filename );
      if ( data == null )
      {
        LoadFailed = true;
        return false;
      }
      if ( !ReadFromBuffer( data ) )
      {
        LoadFailed = true;
        return false;
      }
      return true;
    }



    /// <summary>
    /// Call after renaming/moving a project file on disk: a .mapproject's
    /// ".mapscratch" sidecar must travel along, or every scratch map
    /// appears empty under the new name (the entries are keyed by the
    /// sidecar path). Best effort — the project rename itself already
    /// succeeded, and a stranded sidecar remains recoverable by renaming
    /// it manually.
    /// </summary>
    public static void AccompanyProjectFileRename( string OldPath, string NewPath )
    {
      try
      {
        if ( ( string.IsNullOrEmpty( OldPath ) )
        ||   ( string.IsNullOrEmpty( NewPath ) )
        ||   ( !string.Equals( System.IO.Path.GetExtension( OldPath ), ".mapproject",
                               StringComparison.OrdinalIgnoreCase ) ) )
        {
          return;
        }
        string oldSidecar = System.IO.Path.ChangeExtension( OldPath, ".mapscratch" );
        string newSidecar = System.IO.Path.ChangeExtension( NewPath, ".mapscratch" );
        if ( ( !System.IO.File.Exists( oldSidecar ) )
        ||   ( string.Equals( oldSidecar, newSidecar, StringComparison.OrdinalIgnoreCase ) ) )
        {
          return;
        }
        if ( System.IO.File.Exists( newSidecar ) )
        {
          // The project rename only succeeded because no .mapproject existed at
          // the target, so a sidecar there is an orphan of a deleted project —
          // replacing it is safe.
          System.IO.File.Delete( newSidecar );
        }
        System.IO.File.Move( oldSidecar, newSidecar );
      }
      catch ( Exception )
      {
      }
    }



    /// <summary>
    /// Atomic write (tmp + delete + move, same pattern the studio settings
    /// use) so a crash mid-write can never corrupt existing scratches. When
    /// the container is empty the file is REMOVED instead — no point
    /// keeping a header-only sidecar around. LiveGuids: see SaveToBuffer.
    /// </summary>
    public bool WriteToFile( string Filename, ICollection<string> LiveGuids )
    {
      if ( string.IsNullOrEmpty( Filename ) )
      {
        return false;
      }

      bool hasAnyEntry = false;
      foreach ( var entry in m_Entries.Values )
      {
        if ( ( LiveGuids == null )
        ||   ( LiveGuids.Contains( entry.OwnerGuid ) ) )
        {
          hasAnyEntry = true;
          break;
        }
      }

      try
      {
        // An unreadable original must never be silently clobbered — the
        // user's scratch maps are (possibly recoverably) inside it. Park a
        // copy next to it before any replace/delete below.
        //
        // ONE-TIME capture: LoadFailed stays true for the whole session, so
        // this guard runs on every write. Without the !Exists check the
        // SECOND write would copy the (now-good, already-replaced) Filename
        // over the backup and destroy the only recoverable copy of the
        // originally-corrupt entries. The !Exists guard makes the first
        // backup win permanently; overwrite:false is belt-and-suspenders.
        if ( ( LoadFailed )
        &&   ( System.IO.File.Exists( Filename ) )
        &&   ( !System.IO.File.Exists( Filename + ".corrupt" ) ) )
        {
          System.IO.File.Copy( Filename, Filename + ".corrupt", false );
        }

        if ( !hasAnyEntry )
        {
          if ( System.IO.File.Exists( Filename ) )
          {
            System.IO.File.Delete( Filename );
          }
          return true;
        }

        if ( !GR.IO.File.WriteAllBytes( Filename + ".tmp", SaveToBuffer( LiveGuids ) ) )
        {
          return false;
        }
        if ( System.IO.File.Exists( Filename ) )
        {
          // Atomic swap on the same volume — no delete-then-move window in
          // which a crash leaves NO file at all.
          System.IO.File.Replace( Filename + ".tmp", Filename, null );
        }
        else
        {
          System.IO.File.Move( Filename + ".tmp", Filename );
        }
        return true;
      }
      catch ( Exception )
      {
        // Never leave a stale .tmp behind to confuse the next write.
        try
        {
          System.IO.File.Delete( Filename + ".tmp" );
        }
        catch ( Exception )
        {
        }
        return false;
      }
    }
  }
}
