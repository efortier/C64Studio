using System;
using System.Collections.Generic;



namespace RetroDevStudio.Formats
{
  /// <summary>
  /// Sidecar container for per-map outline images (the map editor's paint
  /// mode). Lives next to the .mapproject as "&lt;projectfile&gt;.mapoutlines"
  /// and is written INDEPENDENTLY of the project file — drawing on an
  /// outline never dirties the map document; the editor flushes this
  /// container on outline-mode exit, map switch and document close.
  ///
  /// Images are keyed by Map.OutlineGuid (the only durable map identity;
  /// list indices shift on add/delete). PNG blobs are kept UNDECODED in
  /// memory: reading the file never decodes an image, and rewriting the
  /// file re-emits retained blobs verbatim — only the map actually being
  /// edited ever pays an encode/decode.
  ///
  /// Orphan policy: intermediate writes keep blobs whose GUID no longer
  /// matches a live map, because a map delete can be undone much later
  /// (the undo system re-inserts the same Map object, GUID intact). Only
  /// the final write on document close prunes to live GUIDs — that is the
  /// point where "deleting a map deletes its picture" becomes permanent,
  /// and it also self-heals files that kept orphans across a crash.
  /// </summary>
  public class MapOutlineContainer
  {
    public class OutlineImageEntry
    {
      public string     Guid = "";
      public int        Width = 0;
      public int        Height = 0;
      public byte[]     PNGData = null;
      // Re-association hints: the GUID's other half lives in the
      // .mapproject and only reaches disk when the USER saves — these
      // let a map with no (persisted) GUID re-adopt its image by name /
      // index, so drawings survive even when the project is never saved.
      public string     MapName = "";
      public int        MapIndex = -1;
    }



    private const uint CURRENT_VERSION = 1;

    private Dictionary<string, OutlineImageEntry>   m_Images = new Dictionary<string, OutlineImageEntry>();

    /// <summary>
    /// The last ReadFromFile found an EXISTING file it could not parse —
    /// intrinsic state of this container's contents ("incomplete view of
    /// what is on disk"). WriteToFile then preserves the unreadable
    /// original as "&lt;file&gt;.corrupt" before replacing it, so a transient
    /// read error can never silently destroy every stored outline.
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
        return m_Images.Count;
      }
    }



    public IEnumerable<string> ImageGuids
    {
      get
      {
        return m_Images.Keys;
      }
    }



    public OutlineImageEntry GetImage( string OutlineGuid )
    {
      if ( string.IsNullOrEmpty( OutlineGuid ) )
      {
        return null;
      }
      OutlineImageEntry entry;
      if ( m_Images.TryGetValue( OutlineGuid, out entry ) )
      {
        return entry;
      }
      return null;
    }



    public bool HasImage( string OutlineGuid )
    {
      return GetImage( OutlineGuid ) != null;
    }



    public void SetImage( string OutlineGuid, int Width, int Height, byte[] PNGData,
                          string MapName = "", int MapIndex = -1 )
    {
      if ( ( string.IsNullOrEmpty( OutlineGuid ) )
      ||   ( PNGData == null )
      ||   ( PNGData.Length == 0 )
      ||   ( Width <= 0 )
      ||   ( Height <= 0 ) )
      {
        return;
      }
      m_Images[OutlineGuid] = new OutlineImageEntry()
      {
        Guid     = OutlineGuid,
        Width    = Width,
        Height   = Height,
        PNGData  = PNGData,
        MapName  = MapName ?? "",
        MapIndex = MapIndex
      };
    }



    /// <summary>
    /// Finds an image a GUID-less map can re-ADOPT — the self-heal for
    /// the split-brain failure where the sidecar (auto-saved) has the
    /// picture but the .mapproject (user-saved) never persisted the
    /// GUID. Excludes GUIDs other maps already hold. Match order: exact
    /// name+index, then unique name, then unique index — hints are only
    /// trusted when they identify ONE candidate.
    /// </summary>
    public OutlineImageEntry FindAdoptableImage( string MapName, int MapIndex, ICollection<string> GuidsInUse )
    {
      var candidates = new List<OutlineImageEntry>();
      foreach ( var entry in m_Images.Values )
      {
        if ( ( GuidsInUse != null )
        &&   ( GuidsInUse.Contains( entry.Guid ) ) )
        {
          continue;
        }
        // Entries from files predating the hints carry ""/-1 — never
        // adoptable (no evidence which map they belonged to).
        if ( ( string.IsNullOrEmpty( entry.MapName ) )
        &&   ( entry.MapIndex < 0 ) )
        {
          continue;
        }
        candidates.Add( entry );
      }

      OutlineImageEntry match = null;
      foreach ( var entry in candidates )
      {
        if ( ( entry.MapName == ( MapName ?? "" ) )
        &&   ( entry.MapIndex == MapIndex ) )
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
        if ( ( !string.IsNullOrEmpty( entry.MapName ) )
        &&   ( entry.MapName == MapName ) )
        {
          if ( match != null )
          {
            return null;
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
        if ( ( entry.MapIndex >= 0 )
        &&   ( entry.MapIndex == MapIndex ) )
        {
          if ( match != null )
          {
            return null;
          }
          match = entry;
        }
      }
      return match;
    }



    public void RemoveImage( string OutlineGuid )
    {
      if ( !string.IsNullOrEmpty( OutlineGuid ) )
      {
        m_Images.Remove( OutlineGuid );
      }
    }



    /// <summary>
    /// Serializes the container. When LiveGuids is non-null, only images
    /// whose GUID it contains are written (the pruning final write); null
    /// keeps every retained image (intermediate writes — see orphan policy
    /// in the class comment).
    /// </summary>
    public GR.Memory.ByteBuffer SaveToBuffer( ICollection<string> LiveGuids )
    {
      var projectFile = new GR.Memory.ByteBuffer();

      var chunkInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_OUTLINE_INFO );
      chunkInfo.AppendU32( CURRENT_VERSION );
      projectFile.Append( chunkInfo.ToBuffer() );

      foreach ( var entry in m_Images.Values )
      {
        if ( ( LiveGuids != null )
        &&   ( !LiveGuids.Contains( entry.Guid ) ) )
        {
          continue;
        }
        var chunkImage = new GR.IO.FileChunk( FileChunkConstants.MAP_OUTLINE_IMAGE );
        chunkImage.AppendString( entry.Guid );
        chunkImage.AppendI32( entry.Width );
        chunkImage.AppendI32( entry.Height );
        chunkImage.AppendU32( (uint)entry.PNGData.Length );
        chunkImage.Append( new GR.Memory.ByteBuffer( entry.PNGData ) );
        // Appended re-association hints (see OutlineImageEntry) — older
        // readers stop after the blob and fall through to ""/-1.
        chunkImage.AppendString( entry.MapName ?? "" );
        chunkImage.AppendI32( entry.MapIndex );
        projectFile.Append( chunkImage.ToBuffer() );
      }
      return projectFile;
    }



    public bool ReadFromBuffer( GR.Memory.ByteBuffer Data )
    {
      m_Images.Clear();
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

      while ( chunk.ReadFromStream( memReader ) )
      {
        var chunkReader = chunk.MemoryReader();
        switch ( chunk.Type )
        {
          case FileChunkConstants.MAP_OUTLINE_INFO:
            // version currently unused (1); newer writers may append fields
            // after it, older readers simply ignore what they don't know.
            chunkReader.ReadUInt32();
            sawInfoChunk = true;
            break;
          case FileChunkConstants.MAP_OUTLINE_IMAGE:
            {
              string  guid   = chunkReader.ReadString();
              int     width  = chunkReader.ReadInt32();
              int     height = chunkReader.ReadInt32();
              uint    length = chunkReader.ReadUInt32();

              var pngData = new GR.Memory.ByteBuffer();
              if ( ( length > 0 )
              &&   ( chunkReader.ReadBlock( pngData, length ) == length ) )
              {
                string mapName = "";
                int mapIndex = -1;
                if ( chunkReader.Size - chunkReader.Position >= 4 )
                {
                  mapName = chunkReader.ReadString();
                }
                if ( chunkReader.Size - chunkReader.Position >= 4 )
                {
                  mapIndex = chunkReader.ReadInt32();
                }
                SetImage( guid, width, height, pngData.Data(), mapName, mapIndex );
              }
            }
            break;
        }
      }
      // Corruption detection: every writer emits MAP_OUTLINE_INFO first,
      // and a clean parse consumes the whole stream. Truncated garbage
      // must NOT masquerade as "valid but empty" — that would let the
      // next write clobber a possibly recoverable file (see LoadFailed).
      bool intact = ( sawInfoChunk )
                 && ( memReader.Position == Data.Length );
      memReader.Close();
      if ( !intact )
      {
        m_Images.Clear();
      }
      return intact;
    }



    /// <summary>
    /// Missing file = empty container and TRUE (a project simply has no
    /// outlines yet); unreadable/corrupt file = empty container and FALSE.
    /// </summary>
    public bool ReadFromFile( string Filename )
    {
      m_Images.Clear();
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
    /// Atomic write (tmp + delete + move, same pattern the studio settings
    /// use) so a crash mid-write can never corrupt existing outlines. When
    /// the container is empty the file is REMOVED instead — no point
    /// keeping a header-only sidecar around. LiveGuids: see SaveToBuffer.
    /// </summary>
    public bool WriteToFile( string Filename, ICollection<string> LiveGuids )
    {
      if ( string.IsNullOrEmpty( Filename ) )
      {
        return false;
      }

      bool hasAnyImage = false;
      foreach ( var entry in m_Images.Values )
      {
        if ( ( LiveGuids == null )
        ||   ( LiveGuids.Contains( entry.Guid ) ) )
        {
          hasAnyImage = true;
          break;
        }
      }

      try
      {
        // An unreadable original must never be silently clobbered — the
        // user's images are (possibly recoverably) inside it. Park a copy
        // next to it before any replace/delete below.
        if ( ( LoadFailed )
        &&   ( System.IO.File.Exists( Filename ) ) )
        {
          System.IO.File.Copy( Filename, Filename + ".corrupt", true );
        }

        if ( !hasAnyImage )
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
