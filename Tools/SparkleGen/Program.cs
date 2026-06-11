using System;
using System.Drawing;
using System.Drawing.Imaging;
using GR.Memory;
using RetroDevStudio;
using RetroDevStudio.Formats;
using RetroDevStudio.Types;

namespace SparkleGen
{
  /// <summary>
  /// Procedurally generates an 8-frame "twinkle" sparkle (an 8x8 pulsing star in
  /// the top-left of a 24x21 sprite), in both hi-res and multicolour, and writes:
  ///   sparkle_hires.spr / sparkle_mc.spr            - raw 8x63 sprite bytes
  ///   sparkle_hires.spriteproject / ..._mc.*        - loadable C64Studio projects
  ///                                                   (8 banks + a "Sparkle" overlay
  ///                                                    whose 8-frame animation cycles them)
  ///   sparkle_preview.png                           - scaled preview of every frame
  ///
  /// The .spriteproject is built with the REAL serializer (SpriteProject.SaveToBuffer),
  /// so it is byte-identical to what the editor writes — no hand-emitted chunks.
  /// </summary>
  internal static class Program
  {
    const int SPR_W       = 24;
    const int SPR_H       = 21;
    const int FRAME_BYTES = 63;       // 24x21 sprite, hires and MC alike
    const int FRAMES      = 8;
    const int GRID        = 8;        // the 8x8 logical sparkle area (top-left)

    // C64 palette indices used.
    const int COL_BLACK  = 0;
    const int COL_WHITE  = 1;
    const int COL_CYAN   = 3;
    const int COL_LTBLUE = 14;

    // Blood-trail charset.
    const int BLOOD_TILES = 16;
    const int BLOOD_SEED  = 1337;
    const int BLOOD_COLOR = 2;     // C64 red — the per-char colour; change in-game as you like

    [STAThread]
    static int Main( string[] args )
    {
      string outDir = ( args.Length > 0 )
                      ? args[0]
                      : System.IO.Path.Combine( AppContext.BaseDirectory, "out" );
      outDir = System.IO.Path.GetFullPath( outDir );
      System.IO.Directory.CreateDirectory( outDir );

      // 1. Geometry: 8 twinkle frames as 8x8 intensity-tier grids.
      int[][,] frames = BuildFrames();

      // 2. Pack each frame into a 63-byte sprite buffer, per mode.
      byte[][] hiresFrames = new byte[FRAMES][];
      byte[][] mcFrames    = new byte[FRAMES][];
      for ( int f = 0; f < FRAMES; ++f )
      {
        hiresFrames[f] = PackHires( frames[f] );
        mcFrames[f]    = PackMC( frames[f] );
      }

      // 3. Raw .spr (8 x 63 bytes, no header).
      WriteSpr( System.IO.Path.Combine( outDir, "sparkle_hires.spr" ), hiresFrames );
      WriteSpr( System.IO.Path.Combine( outDir, "sparkle_mc.spr" ),    mcFrames );

      // 4. Loadable .spriteproject (real serializer).
      string hiresProj = System.IO.Path.Combine( outDir, "sparkle_hires.spriteproject" );
      string mcProj    = System.IO.Path.Combine( outDir, "sparkle_mc.spriteproject" );
      BuildAndSaveProject( false, hiresFrames, "Sparkle HiRes", hiresProj );
      BuildAndSaveProject( true,  mcFrames,    "Sparkle MC",    mcProj );

      // 5. PNG preview (rendered from the tier grids in real C64 colours).
      WritePreview( System.IO.Path.Combine( outDir, "sparkle_preview.png" ), frames );

      // 6. Verify the projects load through the same reader the editor uses.
      bool ok = true;
      ok &= Verify( hiresProj, hiresFrames, GraphicTileMode.COMMODORE_HIRES );
      ok &= Verify( mcProj,    mcFrames,    GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES );
      ok &= ( new System.IO.FileInfo( System.IO.Path.Combine( outDir, "sparkle_hires.spr" ) ).Length == FRAMES * FRAME_BYTES );
      ok &= ( new System.IO.FileInfo( System.IO.Path.Combine( outDir, "sparkle_mc.spr" ) ).Length    == FRAMES * FRAME_BYTES );

      // ===== Second set: two sparkles per sprite, each a different sequence =====
      ok &= GenerateDualSet( outDir );

      // ===== Blood-trail charset: 16 hi-res 8x8 chars, heavy -> faint gradient =====
      ok &= GenerateBloodSet( outDir );

      Console.WriteLine();
      Console.WriteLine( ok ? "VERIFY: all checks passed." : "VERIFY: FAILED — see messages above." );
      Console.WriteLine( "Output folder: " + outDir );
      return ok ? 0 : 1;
    }


    // ---- Geometry ----------------------------------------------------------

    /// <summary>
    /// Eight twinkle frames. Tiers: 0 empty, 1 dim (outer), 2 body, 3 bright (core).
    /// Authored in the top-left quadrant and mirrored 4-fold; each seed is also
    /// transposed so one axis seed paints both the vertical and horizontal arm,
    /// and diagonal seeds paint the 8-point burst. Ignite -> peak (F3) -> fade,
    /// looping seamlessly (F7 dim dot -> F0 dot).
    /// </summary>
    static int[][,] BuildFrames()
    {
      return new int[][,]
      {
        Frame( (3,3,2) ),                                                         // F0 seed dot
        Frame( (3,3,3),(3,2,1) ),                                                 // F1 ignite (+ stub)
        Frame( (3,3,3),(3,2,2),(3,1,1) ),                                         // F2 4-point star
        Frame( (3,3,3),(3,2,3),(3,1,2),(3,0,1),(2,2,2),(1,1,1) ),                 // F3 8-point burst (PEAK)
        Frame( (3,3,3),(3,2,2),(3,1,1),(2,2,1) ),                                 // F4 settling
        Frame( (3,3,2),(3,2,2),(3,1,1) ),                                         // F5 4-point (dimmer)
        Frame( (3,3,2),(3,2,1) ),                                                 // F6 remnant plus
        Frame( (3,3,1) ),                                                         // F7 fading dot
      };
    }

    /// <summary>Build one 8x8 tier grid from quadrant seeds, applying 4-fold mirror + transpose.</summary>
    static int[,] Frame( params (int x, int y, int t)[] seeds )
    {
      int[,] g = new int[GRID, GRID];

      void Plot( int x, int y, int t )
      {
        int[,] pts = { { x, y }, { GRID - 1 - x, y }, { x, GRID - 1 - y }, { GRID - 1 - x, GRID - 1 - y } };
        for ( int i = 0; i < 4; ++i )
        {
          int a = pts[i, 0];
          int b = pts[i, 1];
          if ( ( a >= 0 ) && ( a < GRID ) && ( b >= 0 ) && ( b < GRID ) && ( t > g[a, b] ) )
          {
            g[a, b] = t;
          }
        }
      }

      foreach ( var s in seeds )
      {
        Plot( s.x, s.y, s.t );   // axis / vertical
        Plot( s.y, s.x, s.t );   // transpose -> horizontal axis + diagonal arms (8-fold)
      }
      return g;
    }


    // ---- Packing (matches GraphicTile.SetPixel bit-for-bit) ----------------

    /// <summary>Hi-res: 1 bit/pixel, MSB = leftmost. Lit pixels show the sprite's CustomColor.</summary>
    static byte[] PackHires( int[,] g )
    {
      byte[] data = new byte[FRAME_BYTES];
      for ( int y = 0; y < GRID; ++y )
      {
        for ( int x = 0; x < GRID; ++x )
        {
          if ( g[x, y] >= 1 )
          {
            data[y * 3] |= (byte)( 0x80 >> x );   // x < 8 => byte 0 of the row (cols 0..7)
          }
        }
      }
      return data;
    }

    /// <summary>
    /// Multicolour: 2 bits/pixel, 4 MC-pixels per byte, MSB-pair = leftmost.
    /// Bit-pairs: 01 = MC1, 10 = sprite CustomColor, 11 = MC2 (per SetBit in GraphicTile).
    /// Tier maps straight to the pair (1->MC1, 2->CustomColor, 3->MC2).
    /// </summary>
    static byte[] PackMC( int[,] g )
    {
      byte[] data = new byte[FRAME_BYTES];
      for ( int y = 0; y < GRID; ++y )
      {
        for ( int c = 0; c < GRID; ++c )   // c = MC-pixel column 0..7 (top-left 8 of 12)
        {
          int tier = g[c, y];
          if ( tier <= 0 )
          {
            continue;
          }
          int bytePos = y * 3 + ( c / 4 );
          int shift   = 2 * ( 3 - ( c % 4 ) );
          data[bytePos] |= (byte)( tier << shift );   // tier in {1,2,3} == the 2-bit pair
        }
      }
      return data;
    }

    static void WriteSpr( string path, byte[][] frames )
    {
      byte[] all = new byte[FRAMES * FRAME_BYTES];
      for ( int f = 0; f < FRAMES; ++f )
      {
        Array.Copy( frames[f], 0, all, f * FRAME_BYTES, FRAME_BYTES );
      }
      System.IO.File.WriteAllBytes( path, all );
    }


    // ---- Dual set: two sparkles per sprite, each a different sequence -------

    /// <summary>
    /// Build the "two sparkles" set. Both sparkles are the SAME "+" star twinkle, but
    /// sparkle B (bottom-right) runs PHASE_B frames out of phase with sparkle A
    /// (top-left), so they twinkle independently — one bursts while the other is small.
    /// Both are OR'd into each sprite's 63-byte frame at their own offset. Diagonal
    /// placement is the only layout that fits BOTH modes (a multicolour sprite is only
    /// 12 MC-pixels wide, so two 8-wide sparkles can't sit side-by-side).
    /// </summary>
    static bool GenerateDualSet( string outDir )
    {
      int[][,] star = BuildFrames();         // both sparkles use the same "+" star twinkle
      const int PHASE_B = 4;                  // sparkle B runs 4 frames out of phase with A

      byte[][] hires = new byte[FRAMES][];
      byte[][] mc    = new byte[FRAMES][];
      for ( int f = 0; f < FRAMES; ++f )
      {
        int[,] a = star[f];
        int[,] b = star[( f + PHASE_B ) % FRAMES];

        byte[] hbuf = new byte[FRAME_BYTES];
        PackHiresInto( hbuf, a, 0, 0 );     // A: top-left 8x8
        PackHiresInto( hbuf, b, 16, 13 );   // B: bottom-right 8x8
        hires[f] = hbuf;

        byte[] mbuf = new byte[FRAME_BYTES];
        PackMCInto( mbuf, a, 0, 0 );        // A: top-left (MC cols 0..7)
        PackMCInto( mbuf, b, 4, 13 );       // B: bottom-right (MC cols 4..11)
        mc[f] = mbuf;
      }

      WriteSpr( System.IO.Path.Combine( outDir, "sparkle_dual_hires.spr" ), hires );
      WriteSpr( System.IO.Path.Combine( outDir, "sparkle_dual_mc.spr" ),    mc );

      string hProj = System.IO.Path.Combine( outDir, "sparkle_dual_hires.spriteproject" );
      string mProj = System.IO.Path.Combine( outDir, "sparkle_dual_mc.spriteproject" );
      BuildAndSaveProject( false, hires, "Sparkle Dual HiRes", hProj );
      BuildAndSaveProject( true,  mc,    "Sparkle Dual MC",    mProj );

      WriteDualPreview( System.IO.Path.Combine( outDir, "sparkle_dual_preview.png" ), hires, mc );

      bool ok = true;
      ok &= Verify( hProj, hires, GraphicTileMode.COMMODORE_HIRES );
      ok &= Verify( mProj, mc,    GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES );
      ok &= ( new System.IO.FileInfo( System.IO.Path.Combine( outDir, "sparkle_dual_hires.spr" ) ).Length == FRAMES * FRAME_BYTES );
      ok &= ( new System.IO.FileInfo( System.IO.Path.Combine( outDir, "sparkle_dual_mc.spr" ) ).Length    == FRAMES * FRAME_BYTES );
      return ok;
    }

    /// <summary>OR an 8x8 tier grid into a 63-byte hi-res frame at a sprite-pixel offset (bounds-checked).</summary>
    static void PackHiresInto( byte[] data, int[,] g, int offX, int offY )
    {
      for ( int y = 0; y < GRID; ++y )
      {
        for ( int x = 0; x < GRID; ++x )
        {
          if ( g[x, y] < 1 ) continue;
          int sx = offX + x;
          int sy = offY + y;
          if ( ( sx < 0 ) || ( sx >= SPR_W ) || ( sy < 0 ) || ( sy >= SPR_H ) ) continue;
          data[sy * 3 + sx / 8] |= (byte)( 0x80 >> ( sx % 8 ) );
        }
      }
    }

    /// <summary>OR an 8x8 tier grid into a 63-byte multicolour frame at an MC-column offset (bounds-checked).</summary>
    static void PackMCInto( byte[] data, int[,] g, int offMc, int offY )
    {
      for ( int y = 0; y < GRID; ++y )
      {
        for ( int c = 0; c < GRID; ++c )
        {
          int tier = g[c, y];
          if ( tier <= 0 ) continue;
          int mc = offMc + c;
          int sy = offY + y;
          if ( ( mc < 0 ) || ( mc >= SPR_W / 2 ) || ( sy < 0 ) || ( sy >= SPR_H ) ) continue;
          data[sy * 3 + mc / 4] |= (byte)( tier << ( 2 * ( 3 - ( mc % 4 ) ) ) );
        }
      }
    }

    /// <summary>Preview: render the full 24x21 sprite for all 8 frames (both modes) by decoding the packed bytes.</summary>
    static void WriteDualPreview( string path, byte[][] hires, byte[][] mc )
    {
      Palette pal = ConstantData.DefaultPaletteC64();

      const int SC  = 6;     // screen pixels per hi-res sprite pixel
      const int PAD = 8;
      int frameW = SPR_W * SC;
      int frameH = SPR_H * SC;
      int totalW = PAD + FRAMES * ( frameW + PAD );
      int totalH = PAD + frameH + PAD + frameH + PAD;

      using ( Bitmap bmp = new Bitmap( totalW, totalH, PixelFormat.Format32bppArgb ) )
      using ( Graphics g = Graphics.FromImage( bmp ) )
      {
        g.Clear( Color.FromArgb( 0x10, 0x10, 0x10 ) );
        int yHires = PAD;
        int yMc    = PAD + frameH + PAD;
        for ( int f = 0; f < FRAMES; ++f )
        {
          int x = PAD + f * ( frameW + PAD );
          RenderSprite( g, hires[f], false, x, yHires, SC, pal );
          RenderSprite( g, mc[f],    true,  x, yMc,    SC, pal );
        }
        bmp.Save( path, ImageFormat.Png );
      }
    }

    /// <summary>Decode a 63-byte frame and draw the 24x21 sprite (transparent left as the dark background).</summary>
    static void RenderSprite( Graphics g, byte[] data, bool multicolor, int ox, int oy, int sc, Palette pal )
    {
      if ( !multicolor )
      {
        for ( int y = 0; y < SPR_H; ++y )
        {
          for ( int x = 0; x < SPR_W; ++x )
          {
            if ( ( data[y * 3 + x / 8] & ( 0x80 >> ( x % 8 ) ) ) == 0 ) continue;
            FillCell( g, pal.ColorValues[COL_WHITE], ox + x * sc, oy + y * sc, sc, sc );
          }
        }
      }
      else
      {
        int mcCount = SPR_W / 2;   // 12 MC-pixels wide
        for ( int y = 0; y < SPR_H; ++y )
        {
          for ( int c = 0; c < mcCount; ++c )
          {
            int pair = ( data[y * 3 + c / 4] >> ( 2 * ( 3 - ( c % 4 ) ) ) ) & 3;
            if ( pair == 0 ) continue;
            int idx = ( pair == 1 ) ? COL_LTBLUE : ( pair == 2 ) ? COL_CYAN : COL_WHITE;
            FillCell( g, pal.ColorValues[idx], ox + c * 2 * sc, oy + y * sc, 2 * sc, sc );   // MC pixel = 2 screen px wide
          }
        }
      }
    }

    static void FillCell( Graphics g, uint argb, int x, int y, int w, int h )
    {
      using ( SolidBrush br = new SolidBrush( Color.FromArgb( unchecked( (int)argb ) ) ) )
      {
        g.FillRectangle( br, x, y, w, h );
      }
    }


    // ---- Blood-trail charset (16 hi-res 8x8 chars, heavy -> faint) ----------

    static bool GenerateBloodSet( string outDir )
    {
      byte[][] tiles = new byte[BLOOD_TILES][];
      for ( int i = 0; i < BLOOD_TILES; ++i )
      {
        tiles[i] = BuildBloodTile( i, BLOOD_TILES );
      }

      // Loadable .charsetproject (blood in chars 0..15, the rest left blank).
      CharsetProject cs = new CharsetProject();   // 256 hi-res chars + C64 palette by default
      cs.Colors.BackgroundColor = COL_BLACK;
      for ( int i = 0; i < BLOOD_TILES; ++i )
      {
        cs.Characters[i].Tile.Data        = new ByteBuffer( tiles[i] );
        cs.Characters[i].Tile.CustomColor = (byte)BLOOD_COLOR;
      }
      string projPath = System.IO.Path.Combine( outDir, "blood_trail.charsetproject" );
      GR.IO.File.WriteAllBytes( projPath, cs.SaveToBuffer() );

      // Raw binary of just the 16 blood chars (16 x 8 = 128 bytes).
      byte[] bin = new byte[BLOOD_TILES * 8];
      for ( int i = 0; i < BLOOD_TILES; ++i )
      {
        Array.Copy( tiles[i], 0, bin, i * 8, 8 );
      }
      System.IO.File.WriteAllBytes( System.IO.Path.Combine( outDir, "blood_trail.chr" ), bin );

      WriteBloodPreview( System.IO.Path.Combine( outDir, "blood_trail_preview.png" ), tiles );

      bool ok = VerifyCharset( projPath, tiles );
      ok &= ( new System.IO.FileInfo( System.IO.Path.Combine( outDir, "blood_trail.chr" ) ).Length == BLOOD_TILES * 8 );
      return ok;
    }

    /// <summary>
    /// One blood splat on an 8x8 hi-res grid. index 0 = heaviest (puddle + wide
    /// spatter), the last index = faintest (a couple of specks). A per-tile seeded
    /// RNG makes the splats look organic yet 100% reproducible.
    /// </summary>
    static byte[] BuildBloodTile( int index, int total )
    {
      Random rng = new Random( BLOOD_SEED + index * 7919 );
      bool[,] px = new bool[8, 8];

      double heavy = 1.0 - ( (double)index / ( total - 1 ) );   // 1 heavy .. 0 faint

      double cx    = 3.5 + ( rng.NextDouble() - 0.5 ) * 1.2;
      double cy    = 3.5 + ( rng.NextDouble() - 0.5 ) * 1.2;
      double rBlob = heavy * 2.6;

      // Central puddle (heavier tiles only); ragged edge via per-pixel noise.
      if ( rBlob > 0.7 )
      {
        for ( int y = 0; y < 8; ++y )
        {
          for ( int x = 0; x < 8; ++x )
          {
            double d     = Math.Sqrt( ( x - cx ) * ( x - cx ) + ( y - cy ) * ( y - cy ) );
            double noise = ( rng.NextDouble() - 0.5 ) * 1.8;
            if ( d <= rBlob + noise ) px[x, y] = true;
          }
        }
      }

      // Spatter droplets radiating outward from the centre.
      int drops = (int)Math.Round( 2 + heavy * 5 );
      for ( int k = 0; k < drops; ++k )
      {
        double ang = rng.NextDouble() * Math.PI * 2.0;
        double rho = rBlob * 0.6 + rng.NextDouble() * ( rBlob + 2.2 );
        int dx = (int)Math.Round( cx + Math.Cos( ang ) * rho );
        int dy = (int)Math.Round( cy + Math.Sin( ang ) * rho );
        if ( ( dx >= 0 ) && ( dx < 8 ) && ( dy >= 0 ) && ( dy < 8 ) ) px[dx, dy] = true;
        if ( rng.NextDouble() < 0.30 * heavy )   // occasional 2-px droplet on the heavier tiles
        {
          int ex = dx + ( ( rng.Next( 2 ) == 0 ) ? -1 : 1 );
          if ( ( ex >= 0 ) && ( ex < 8 ) && ( dy >= 0 ) && ( dy < 8 ) ) px[ex, dy] = true;
        }
      }

      // Never emit an empty tile.
      int lit = 0;
      foreach ( bool b in px ) { if ( b ) ++lit; }
      while ( lit < 2 )
      {
        int sx = 2 + rng.Next( 4 );
        int sy = 2 + rng.Next( 4 );
        if ( !px[sx, sy] ) { px[sx, sy] = true; ++lit; }
      }

      byte[] data = new byte[8];
      for ( int y = 0; y < 8; ++y )
      {
        for ( int x = 0; x < 8; ++x )
        {
          if ( px[x, y] ) data[y] |= (byte)( 0x80 >> x );
        }
      }
      return data;
    }

    /// <summary>Preview: a gallery row (each tile separated, heavy -> faint) and a trail row (tiles edge-to-edge).</summary>
    static void WriteBloodPreview( string path, byte[][] tiles )
    {
      Palette pal = ConstantData.DefaultPaletteC64();
      uint blood  = pal.ColorValues[BLOOD_COLOR];

      const int SC  = 14;    // screen px per char pixel
      const int PAD = 10;
      int charW  = 8 * SC;
      int charH  = 8 * SC;
      int totalW = PAD + BLOOD_TILES * ( charW + PAD );
      int totalH = PAD + charH + PAD + charH + PAD;

      using ( Bitmap bmp = new Bitmap( totalW, totalH, PixelFormat.Format32bppArgb ) )
      using ( Graphics g = Graphics.FromImage( bmp ) )
      {
        g.Clear( Color.FromArgb( 0x10, 0x10, 0x10 ) );

        int yGallery = PAD;
        int yTrail   = PAD + charH + PAD;
        for ( int i = 0; i < BLOOD_TILES; ++i )
        {
          RenderChar( g, tiles[i], PAD + i * ( charW + PAD ), yGallery, SC, blood );   // separated
          RenderChar( g, tiles[i], PAD + i * charW,           yTrail,   SC, blood );   // edge-to-edge
        }
        bmp.Save( path, ImageFormat.Png );
      }
    }

    static void RenderChar( Graphics g, byte[] data, int ox, int oy, int sc, uint colorArgb )
    {
      for ( int y = 0; y < 8; ++y )
      {
        for ( int x = 0; x < 8; ++x )
        {
          if ( ( data[y] & ( 0x80 >> x ) ) == 0 ) continue;
          FillCell( g, colorArgb, ox + x * sc, oy + y * sc, sc, sc );
        }
      }
    }

    static bool VerifyCharset( string path, byte[][] expected )
    {
      CharsetProject rt = new CharsetProject();
      if ( !rt.ReadFromBuffer( GR.IO.File.ReadAllBytes( path ) ) )
      {
        Console.WriteLine( "  FAIL: could not load " + System.IO.Path.GetFileName( path ) );
        return false;
      }
      bool ok = true;
      for ( int i = 0; i < expected.Length; ++i )
      {
        var d = rt.Characters[i].Tile.Data;
        if ( (int)d.Length < 8 )
        {
          ok = false;
          Console.WriteLine( "  FAIL: char " + i + " data length = " + d.Length );
          continue;
        }
        for ( int b = 0; b < 8; ++b )
        {
          if ( d.ByteAt( b ) != expected[i][b] )
          {
            ok = false;
            Console.WriteLine( "  FAIL: char " + i + " byte " + b + " mismatch" );
            break;
          }
        }
      }
      Console.WriteLine( ( ok ? "  OK:   " : "  FAIL: " ) + System.IO.Path.GetFileName( path ) );
      return ok;
    }


    // ---- SpriteProject construction (real serializer) ----------------------

    static void BuildAndSaveProject( bool multicolor, byte[][] frameBytes, string name, string path )
    {
      // A fresh project already has 256 banks + the default C64 palette; we
      // overwrite banks 0..7 and add one animated overlay.
      SpriteProject project = new SpriteProject();
      project.Name = name;
      project.Colors.BackgroundColor = COL_BLACK;
      project.Colors.MultiColor1     = COL_LTBLUE;   // dim outer (MC bit-pair 01)
      project.Colors.MultiColor2     = COL_WHITE;    // bright core (MC bit-pair 11)
      project.Colors.ActivePalette   = 0;
      project.Colors.PaletteOffset   = 0;
      project.TestLoop                = true;        // overlay test panel loops by default
      project.TestBackgroundColorIndex = COL_BLACK;

      SpriteMode      spriteMode  = multicolor ? SpriteMode.COMMODORE_24_X_21_MULTICOLOR : SpriteMode.COMMODORE_24_X_21_HIRES;
      GraphicTileMode tileMode    = multicolor ? GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES : GraphicTileMode.COMMODORE_HIRES;
      byte            customColor = (byte)( multicolor ? COL_CYAN : COL_WHITE );   // MC body / hires glint

      for ( int f = 0; f < FRAMES; ++f )
      {
        SpriteProject.SpriteData s = project.Sprites[f];
        s.Mode             = spriteMode;
        s.Tile.Mode        = tileMode;
        s.Tile.Width       = SPR_W;
        s.Tile.Height      = SPR_H;
        s.Tile.CustomColor = customColor;
        s.Tile.Data        = new ByteBuffer( frameBytes[f] );
      }

      SpriteProject.Overlay ov = new SpriteProject.Overlay();
      ov.Name                  = "Sparkle";
      ov.Slots[0].Enabled         = true;
      ov.Slots[0].X               = 0;
      ov.Slots[0].Y               = 0;
      ov.Slots[0].CustomColor     = customColor;
      ov.Slots[0].BackgroundColor = COL_BLACK;
      ov.Slots[0].MultiColor1     = COL_LTBLUE;
      ov.Slots[0].MultiColor2     = COL_WHITE;
      for ( int f = 0; f < FRAMES; ++f )
      {
        SpriteProject.OverlayFrame fr = new SpriteProject.OverlayFrame();
        fr.DelayMS      = 100;
        fr.BankIndex[0] = f;       // slot 0 shows bank f at frame f
        ov.Frames.Add( fr );
      }
      project.Overlays.Add( ov );

      GR.IO.File.WriteAllBytes( path, project.SaveToBuffer() );
    }


    // ---- PNG preview -------------------------------------------------------

    static void WritePreview( string path, int[][,] frames )
    {
      Palette pal = ConstantData.DefaultPaletteC64();

      const int CELL = 16;
      const int PAD  = 8;
      int hiresFrameW = GRID * CELL;
      int mcFrameW    = GRID * CELL * 2;   // MC pixels are 2 screen-pixels wide
      int frameH      = GRID * CELL;

      int rowHiresW = PAD + FRAMES * ( hiresFrameW + PAD );
      int rowMcW    = PAD + FRAMES * ( mcFrameW + PAD );
      int totalW    = Math.Max( rowHiresW, rowMcW );
      int totalH    = PAD + frameH + PAD + frameH + PAD;

      using ( Bitmap bmp = new Bitmap( totalW, totalH, PixelFormat.Format32bppArgb ) )
      using ( Graphics g = Graphics.FromImage( bmp ) )
      {
        g.Clear( Color.FromArgb( 0x10, 0x10, 0x10 ) );

        int yHires = PAD;
        int yMc    = PAD + frameH + PAD;
        for ( int f = 0; f < FRAMES; ++f )
        {
          DrawFrame( g, frames[f], PAD + f * ( hiresFrameW + PAD ), yHires, CELL, 1, false, pal );
          DrawFrame( g, frames[f], PAD + f * ( mcFrameW + PAD ),    yMc,    CELL, 2, true,  pal );
        }
        bmp.Save( path, ImageFormat.Png );
      }
    }

    static void DrawFrame( Graphics g, int[,] grid, int ox, int oy, int cell, int xScale, bool multicolor, Palette pal )
    {
      for ( int y = 0; y < GRID; ++y )
      {
        for ( int x = 0; x < GRID; ++x )
        {
          int  tier = grid[x, y];
          uint argb;
          if ( tier <= 0 )
          {
            argb = ( ( ( x + y ) & 1 ) == 0 ) ? 0xff202020u : 0xff2c2c2cu;   // transparent checker
          }
          else if ( !multicolor )
          {
            argb = pal.ColorValues[COL_WHITE];
          }
          else
          {
            int idx = ( tier == 1 ) ? COL_LTBLUE : ( tier == 2 ) ? COL_CYAN : COL_WHITE;
            argb = pal.ColorValues[idx];
          }

          using ( SolidBrush br = new SolidBrush( Color.FromArgb( unchecked( (int)argb ) ) ) )
          {
            g.FillRectangle( br, ox + x * cell * xScale, oy + y * cell, cell * xScale, cell );
          }
        }
      }
    }


    // ---- Verification ------------------------------------------------------

    static bool Verify( string path, byte[][] expectedFrames, GraphicTileMode expectedTileMode )
    {
      SpriteProject rt = new SpriteProject();
      if ( !rt.ReadFromBuffer( GR.IO.File.ReadAllBytes( path ) ) )
      {
        Console.WriteLine( "  FAIL: could not load " + System.IO.Path.GetFileName( path ) );
        return false;
      }

      bool ok = true;

      if ( rt.Overlays.Count != 1 )
      {
        ok = false;
        Console.WriteLine( "  FAIL: overlay count = " + rt.Overlays.Count );
      }
      else
      {
        SpriteProject.Overlay ov = rt.Overlays[0];
        if ( ov.Frames.Count != FRAMES ) { ok = false; Console.WriteLine( "  FAIL: frame count = " + ov.Frames.Count ); }
        if ( !ov.Slots[0].Enabled )      { ok = false; Console.WriteLine( "  FAIL: slot 0 not enabled" ); }
        for ( int f = 0; ( f < FRAMES ) && ( f < ov.Frames.Count ); ++f )
        {
          if ( ov.Frames[f].BankIndex[0] != f )
          {
            ok = false;
            Console.WriteLine( "  FAIL: frame " + f + " bank index = " + ov.Frames[f].BankIndex[0] );
          }
        }
      }

      for ( int f = 0; f < FRAMES; ++f )
      {
        SpriteProject.SpriteData s = rt.Sprites[f];
        if ( s.Tile.Mode != expectedTileMode )
        {
          ok = false;
          Console.WriteLine( "  FAIL: sprite " + f + " tile mode = " + s.Tile.Mode );
        }
        if ( (int)s.Tile.Data.Length != FRAME_BYTES )
        {
          ok = false;
          Console.WriteLine( "  FAIL: sprite " + f + " data length = " + s.Tile.Data.Length );
        }
        else
        {
          for ( int b = 0; b < FRAME_BYTES; ++b )
          {
            if ( s.Tile.Data.ByteAt( b ) != expectedFrames[f][b] )
            {
              ok = false;
              Console.WriteLine( "  FAIL: sprite " + f + " byte " + b + " mismatch" );
              break;
            }
          }
        }
      }

      Console.WriteLine( ( ok ? "  OK:   " : "  FAIL: " ) + System.IO.Path.GetFileName( path ) );
      return ok;
    }
  }
}
