using System;
using System.Collections.Generic;
using System.Drawing;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// One persistent object of the outline painter: selectable and movable
  /// until explicitly flattened to pixels. An object is EITHER text
  /// (editable, styleable) OR a pasted IMAGE (ImagePNGData != null — the
  /// text/font fields are ignored then); both kinds share the list, the
  /// selection, the move/delete/undo pipeline and the sidecar blob, so
  /// every list-level operation treats them uniformly. Lives in a per-map
  /// list owned by the editor (single-active-map lifecycle, like the outline
  /// bitmap) and persists as an opaque blob inside the map's sidecar entry.
  ///
  /// Deliberately in the Controls layer, not C64Models: measurement needs an
  /// IOutlineRenderer, and the sidecar container stays blob-dumb (it never
  /// decodes text objects, mirroring its undecoded-PNG philosophy).
  /// </summary>
  public class OutlineTextObject
  {
    public PointF     Position;               // image-space top-left anchor
    public string     Text = "";              // multi-line, \n separated
    public string     FontFamily = "Arial";
    public float      FontSize = 16.0f;
    public bool       Bold = false;
    public bool       Italic = false;
    public Color      Color = Color.White;
    /// <summary>
    /// Explicit word-wrap width (image space, set by dragging the right
    /// border): 0 = none; &gt; 0 = wrap at this width AND the frame follows it.
    /// When 0, AutoBreakWidth (below) governs the line breaks instead.
    /// </summary>
    public float      WrapWidth = 0f;
    /// <summary>
    /// FROZEN auto-wrap width (image space), captured when the edit box
    /// commits: while typing, lines break at the canvas right edge; the width
    /// that produced those breaks is stored here so the committed object keeps
    /// them VERBATIM — moving the object, resizing the canvas or reloading
    /// never reflows (the user froze the breaks by finishing the edit; only the
    /// next edit re-derives). 0 = no auto wrap; &lt; 0 = loaded from a blob
    /// predating this field (the load path migrates it once from the object's
    /// position, reproducing what that build displayed). The frame still hugs
    /// (Measure uses WrapWidth, not this), so auto-wrap never fattens the box
    /// or offsets centering.
    /// </summary>
    public float      AutoBreakWidth = 0f;
    /// <summary>Extra image-space pixels between characters (may be negative).</summary>
    public float      CharSpacing = 0f;
    /// <summary>Extra image-space pixels between lines (may be negative).</summary>
    public float      LineSpacing = 0f;

    /// <summary>
    /// Non-null = this object is a pasted IMAGE, not text: the PNG bytes are
    /// the authoritative payload (persisted undecoded, exactly like the
    /// sidecar's canvas PNGs); Text and every font/style/wrap field are
    /// ignored. Treated as IMMUTABLE once assigned — clones and undo
    /// snapshots share the reference, like the layout cache below.
    /// </summary>
    public byte[]     ImagePNGData = null;
    // Decoded lazily on first measure/draw, then shared by clones (same
    // immutable-share contract as m_CachedLayout — never disposed
    // explicitly; the GDI+ finalizer reclaims it).
    private Bitmap    m_CachedImage = null;

    // Layout cache (line breaks + per-char advances, image space); null =
    // stale. Invalidated on text/font/style/wrap/spacing changes; the cached
    // break width additionally re-lays-out when it changes, so a MOVE (which
    // shifts the auto-wrap boundary relative to the object) reflows without any
    // caller having to invalidate.
    private OutlineTextLayoutData m_CachedLayout = null;
    private float                 m_CachedBreakWidth = float.NaN;



    /// <summary>True for a pasted-image object; false for a text object.</summary>
    public bool IsImage
    {
      get
      {
        return ImagePNGData != null;
      }
    }



    /// <summary>
    /// The decoded image (image objects only) — decoded once from the PNG
    /// payload, cached and shared by clones. A corrupt payload yields a
    /// small magenta placeholder so bounds/draw/hit-test never see null.
    /// </summary>
    public Bitmap GetImage()
    {
      if ( m_CachedImage != null )
      {
        return m_CachedImage;
      }
      try
      {
        using ( var stream = new System.IO.MemoryStream( ImagePNGData ) )
        using ( var decoded = System.Drawing.Image.FromStream( stream ) )
        {
          m_CachedImage = new Bitmap( decoded );
        }
      }
      catch ( Exception )
      {
        var placeholder = new Bitmap( 16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
        using ( var g = Graphics.FromImage( placeholder ) )
        {
          g.Clear( System.Drawing.Color.Magenta );
        }
        m_CachedImage = placeholder;
      }
      return m_CachedImage;
    }



    public OutlineTextObject Clone()
    {
      return new OutlineTextObject()
      {
        Position       = Position,
        Text           = Text,
        FontFamily     = FontFamily,
        FontSize       = FontSize,
        Bold           = Bold,
        Italic         = Italic,
        Color          = Color,
        WrapWidth      = WrapWidth,
        AutoBreakWidth = AutoBreakWidth,
        CharSpacing    = CharSpacing,
        LineSpacing    = LineSpacing,
        ImagePNGData   = ImagePNGData,         // immutable payload — shared
        m_CachedImage  = m_CachedImage,        // decoded once, shared like the layout
        m_CachedLayout = m_CachedLayout,       // immutable once built — safe to share
        m_CachedBreakWidth = m_CachedBreakWidth
      };
    }



    /// <summary>Call after changing Text/FontFamily/FontSize/Bold/Italic/WrapWidth/CharSpacing.</summary>
    public void InvalidateMeasurement()
    {
      m_CachedLayout = null;
    }



    /// <summary>
    /// The width lines break at: the explicit WrapWidth if set, else the
    /// FROZEN auto-wrap width, else 0 = no wrapping. Deliberately independent
    /// of Position — line breaks never change outside the edit box. This is
    /// the LAYOUT (break) width only — the FRAME width stays WrapWidth (see
    /// MeasuredSize), so auto-wrap hugs.
    /// </summary>
    private float EffectiveBreakWidth()
    {
      if ( WrapWidth > 0f )
      {
        return WrapWidth;
      }
      // <= 0 covers both "no auto wrap" and the unmigrated-load sentinel (-1).
      return ( AutoBreakWidth > 0f ) ? AutoBreakWidth : 0f;
    }



    /// <summary>
    /// The object's laid-out lines (built lazily, cached until a text/style
    /// change OR a break-width change — the cache re-keys on the width, so
    /// width writes reflow without the writer needing to invalidate). Uses
    /// throwaway metrics — font metrics are identical on any 96-dpi Graphics,
    /// so drawing, bounds and the bake all agree.
    /// </summary>
    public OutlineTextLayoutData GetLayout()
    {
      float breakWidth = EffectiveBreakWidth();
      if ( ( m_CachedLayout == null )
      ||   ( m_CachedBreakWidth != breakWidth ) )
      {
        using ( var font = GdiPlusOutlineRenderer.CreateFont( FontFamily, FontSize, Bold, Italic ) )
        {
          string measured = string.IsNullOrEmpty( Text ) ? " " : Text;
          m_CachedLayout = OutlineTextLayout.LayoutWithTemporaryGraphics( font, measured, breakWidth, CharSpacing );
          m_CachedBreakWidth = breakWidth;
        }
      }
      return m_CachedLayout;
    }



    /// <summary>Exact image-space content size (typographic — no MeasureString
    /// padding; an image object's size is its decoded pixel size).</summary>
    public SizeF MeasuredSize()
    {
      if ( IsImage )
      {
        var image = GetImage();
        return new SizeF( image.Width, image.Height );
      }
      return OutlineTextLayout.Measure( GetLayout(), WrapWidth, LineSpacing );
    }



    /// <summary>
    /// The CONTENT's width — the widest laid-out line, ignoring any wider
    /// frame (an explicit WrapWidth widens MeasuredSize but not this); for an
    /// image object simply its pixel width. Centering aligns on this so a
    /// wide box never drags its text off-center.
    /// </summary>
    public float ContentWidth()
    {
      if ( IsImage )
      {
        return Math.Max( 1f, GetImage().Width );
      }
      float widest = 0f;
      foreach ( var line in GetLayout().Lines )
      {
        if ( line.Width > widest )
        {
          widest = line.Width;
        }
      }
      return Math.Max( 1f, widest );
    }



    /// <summary>
    /// Image-space bounds: anchor + exact measured size + a 4px AA/overhang
    /// margin, so hit-testing, invalidation, the flatten region and the
    /// selection frame all agree — and hug the text tightly (the old
    /// MeasureString padding made the right edge visibly fat).
    /// </summary>
    public Rectangle BoundsWithMargin()
    {
      var size = MeasuredSize();
      return Rectangle.FromLTRB(
        (int)Math.Floor( Position.X ) - 4, (int)Math.Floor( Position.Y ) - 4,
        (int)Math.Ceiling( Position.X + size.Width ) + 4,
        (int)Math.Ceiling( Position.Y + size.Height ) + 4 );
    }



    public static List<OutlineTextObject> CloneList( IEnumerable<OutlineTextObject> Source )
    {
      var clones = new List<OutlineTextObject>();
      if ( Source != null )
      {
        foreach ( var obj in Source )
        {
          clones.Add( obj.Clone() );
        }
      }
      return clones;
    }



    /// <summary>
    /// Serializes a list to the sidecar blob: a sequence of
    /// MAP_OUTLINE_TEXT_OBJECT FileChunks (append-tolerant per object).
    /// Null/empty list → null (the sidecar entry stores no blob at all).
    /// </summary>
    public static byte[] SaveListToBuffer( List<OutlineTextObject> Objects )
    {
      if ( ( Objects == null )
      ||   ( Objects.Count == 0 ) )
      {
        return null;
      }
      var buffer = new GR.Memory.ByteBuffer();
      foreach ( var obj in Objects )
      {
        if ( obj.IsImage )
        {
          // Image objects get their own chunk type in the SAME blob (list
          // order = z-order round-trips). Readers skip unknown chunk types,
          // so pre-feature builds load the texts and drop the images.
          var imageChunk = new GR.IO.FileChunk( RetroDevStudio.FileChunkConstants.MAP_OUTLINE_IMAGE_OBJECT );
          imageChunk.AppendF32( obj.Position.X );
          imageChunk.AppendF32( obj.Position.Y );
          imageChunk.AppendU32( (uint)obj.ImagePNGData.Length );
          if ( obj.ImagePNGData.Length > 0 )
          {
            imageChunk.Append( new GR.Memory.ByteBuffer( obj.ImagePNGData ) );
          }
          buffer.Append( imageChunk.ToBuffer() );
          continue;
        }
        var chunk = new GR.IO.FileChunk( RetroDevStudio.FileChunkConstants.MAP_OUTLINE_TEXT_OBJECT );
        // Text and FontFamily as length-prefixed UTF-8 — NOT AppendString,
        // which truncates every UTF-16 code unit above U+00FF to its low
        // byte (em-dashes, €, arrows, IME text, localized font names would
        // silently corrupt on the very next flush/reload).
        AppendUtf8( chunk, obj.Text ?? "" );
        AppendUtf8( chunk, obj.FontFamily ?? "Arial" );
        chunk.AppendF32( obj.FontSize );
        chunk.AppendU8( (byte)( ( obj.Bold ? 1 : 0 ) | ( obj.Italic ? 2 : 0 ) ) );
        chunk.AppendU32( (uint)obj.Color.ToArgb() );
        chunk.AppendF32( obj.Position.X );
        chunk.AppendF32( obj.Position.Y );
        // Appended later (guarded reads — older blobs fall back to 0).
        chunk.AppendF32( obj.WrapWidth );
        chunk.AppendF32( obj.CharSpacing );
        chunk.AppendF32( obj.LineSpacing );
        // Appended later still: the frozen auto-wrap width. Clamped — the -1
        // "unmigrated" sentinel is a LOAD-side state and must never persist.
        chunk.AppendF32( Math.Max( 0f, obj.AutoBreakWidth ) );
        buffer.Append( chunk.ToBuffer() );
      }
      return buffer.Data();
    }



    /// <summary>
    /// Deserializes the sidecar blob. Tolerant of null/garbage/unknown chunk
    /// types (skipped); never returns null. Values clamped to sane ranges so
    /// a corrupt blob can't produce a crashing object.
    /// </summary>
    public static List<OutlineTextObject> ReadListFromBuffer( byte[] Data )
    {
      var objects = new List<OutlineTextObject>();
      if ( ( Data == null )
      ||   ( Data.Length == 0 ) )
      {
        return objects;
      }
      var memReader = new GR.IO.MemoryReader( new GR.Memory.ByteBuffer( Data ) );
      var chunk = new GR.IO.FileChunk();
      while ( chunk.ReadFromStream( memReader ) )
      {
        if ( chunk.Type == RetroDevStudio.FileChunkConstants.MAP_OUTLINE_IMAGE_OBJECT )
        {
          var imageReader = chunk.MemoryReader();
          float imageX = imageReader.ReadF32();
          float imageY = imageReader.ReadF32();
          uint pngLength = imageReader.ReadUInt32();
          if ( ( pngLength == 0 )
          ||   ( imageReader.Size - imageReader.Position < pngLength ) )
          {
            continue;   // truncated/empty payload — drop, don't crash
          }
          var pngBlock = new GR.Memory.ByteBuffer();
          if ( imageReader.ReadBlock( pngBlock, pngLength ) != pngLength )
          {
            continue;
          }
          objects.Add( new OutlineTextObject()
          {
            Position = new PointF(
              float.IsFinite( imageX ) ? Math.Max( -65536f, Math.Min( 65536f, imageX ) ) : 0f,
              float.IsFinite( imageY ) ? Math.Max( -65536f, Math.Min( 65536f, imageY ) ) : 0f ),
            ImagePNGData = pngBlock.Data()
          } );
          continue;
        }
        if ( chunk.Type != RetroDevStudio.FileChunkConstants.MAP_OUTLINE_TEXT_OBJECT )
        {
          continue;
        }
        var reader = chunk.MemoryReader();
        var obj = new OutlineTextObject();
        obj.Text       = ReadUtf8( reader );
        obj.FontFamily = ReadUtf8( reader );
        if ( string.IsNullOrEmpty( obj.FontFamily ) )
        {
          obj.FontFamily = "Arial";
        }
        float size     = reader.ReadF32();
        // NaN survives Math.Max/Min (both return NaN) — a corrupt blob must
        // not poison every later measure/paint.
        obj.FontSize   = float.IsFinite( size ) ? Math.Max( 1.0f, Math.Min( 512.0f, size ) ) : 16.0f;
        byte style     = reader.ReadUInt8();
        obj.Bold       = ( ( style & 1 ) != 0 );
        obj.Italic     = ( ( style & 2 ) != 0 );
        obj.Color      = System.Drawing.Color.FromArgb( (int)reader.ReadUInt32() );
        float x        = reader.ReadF32();
        float y        = reader.ReadF32();
        // NaN/Inf from a corrupt blob must not poison the canvas math.
        obj.Position   = new PointF(
          float.IsFinite( x ) ? Math.Max( -65536f, Math.Min( 65536f, x ) ) : 0f,
          float.IsFinite( y ) ? Math.Max( -65536f, Math.Min( 65536f, y ) ) : 0f );
        // Wrap width + spacings, appended after the original fields — guarded
        // so pre-feature blobs fall back to 0 (auto width, default spacing).
        if ( reader.Size - reader.Position >= 4 )
        {
          float wrap = reader.ReadF32();
          obj.WrapWidth = float.IsFinite( wrap ) ? Math.Max( 0f, Math.Min( 65536f, wrap ) ) : 0f;
        }
        if ( reader.Size - reader.Position >= 4 )
        {
          float charSpacing = reader.ReadF32();
          obj.CharSpacing = float.IsFinite( charSpacing ) ? Math.Max( -32f, Math.Min( 256f, charSpacing ) ) : 0f;
        }
        if ( reader.Size - reader.Position >= 4 )
        {
          float lineSpacing = reader.ReadF32();
          obj.LineSpacing = float.IsFinite( lineSpacing ) ? Math.Max( -64f, Math.Min( 256f, lineSpacing ) ) : 0f;
        }
        // Frozen auto-wrap width. A blob predating the field gets the -1
        // sentinel: the load path migrates it ONCE from the object's position
        // (reproducing what the live-derive build displayed) — EffectiveBreakWidth
        // treats a leaked sentinel as "no wrap".
        obj.AutoBreakWidth = -1f;
        if ( reader.Size - reader.Position >= 4 )
        {
          float autoBreak = reader.ReadF32();
          obj.AutoBreakWidth = float.IsFinite( autoBreak ) ? Math.Max( 0f, Math.Min( 65536f, autoBreak ) ) : 0f;
        }
        objects.Add( obj );
      }
      memReader.Close();
      return objects;
    }



    private static void AppendUtf8( GR.IO.FileChunk Chunk, string Value )
    {
      var bytes = System.Text.Encoding.UTF8.GetBytes( Value );
      Chunk.AppendU32( (uint)bytes.Length );
      if ( bytes.Length > 0 )
      {
        Chunk.Append( new GR.Memory.ByteBuffer( bytes ) );
      }
    }



    private static string ReadUtf8( GR.IO.IReader Reader )
    {
      if ( Reader.Size - Reader.Position < 4 )
      {
        return "";
      }
      uint length = Reader.ReadUInt32();
      if ( ( length == 0 )
      ||   ( Reader.Size - Reader.Position < length ) )
      {
        return "";
      }
      var block = new GR.Memory.ByteBuffer();
      if ( Reader.ReadBlock( block, length ) != length )
      {
        return "";
      }
      return System.Text.Encoding.UTF8.GetString( block.Data() );
    }
  }
}
