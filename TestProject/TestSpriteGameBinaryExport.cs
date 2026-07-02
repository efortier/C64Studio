using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;



namespace TestProject
{
  [TestClass]
  public class TestSpriteGameBinaryExport
  {
    // Sprite anim-defs header offsets (must match SpriteProject / the .asm sidecar).
    const int HDR_ANIMATION_COUNT      = 0x00;
    const int HDR_SPRITE_COUNT         = 0x01;
    const int HDR_SPRITE_DATA_STRIDE   = 0x02;
    const int HDR_ANIM_FRAME_SIZE      = 0x03;
    const int HDR_OFFSET_ANIM_DEF_LO   = 0x04;
    const int HDR_OFFSET_ANIM_DEF_HI   = 0x06;
    const int HDR_OFFSET_ANIM_ID       = 0x08;

    /// <summary>
    /// A SpriteProject() already has 256 default sprites; give the first
    /// <paramref name="markedSprites"/> of them a unique marker in byte 0 so we
    /// can verify which sprite ends up in which exported block.
    /// </summary>
    private SpriteProject MakeProject( int markedSprites )
    {
      var proj = new SpriteProject();
      for ( int i = 0; i < markedSprites; ++i )
      {
        var d = new ByteBuffer();
        d.AppendU8( (byte)( 0xA0 + i ) );                // byte 0 = unique marker
        while ( d.Length < 63 ) d.AppendU8( 0 );
        proj.Sprites[i].Tile.Data = d;
      }
      return proj;
    }

    private SpriteProject.Overlay AddOverlay( SpriteProject proj, params int[] frameBank0 )
    {
      var ov = new SpriteProject.Overlay();
      ov.Name = "Walk";
      foreach ( var b in frameBank0 )
      {
        var f = new SpriteProject.OverlayFrame();
        f.BankIndex[0] = b;
        ov.Frames.Add( f );
      }
      proj.Overlays.Add( ov );
      return ov;
    }

    // ================================================================
    // 1. Header + def stream + frames
    // ================================================================
    [TestMethod]
    public void TestHeaderAndDefStream()
    {
      var proj = MakeProject( 8 );
      var ov = AddOverlay( proj, 5, 2, 5 );   // references sprites 5, 2 (repeat 5)
      ov.AnimationID = 7;
      ov.FrameDelay  = 4;
      ov.Loop        = true;
      ov.Slots[0].CustomColor = -1;          // "None": each frame exports its sprite's own colour
      ov.Slots[0].ExpandX = true;
      ov.Slots[0].ExpandY = false;
      proj.Sprites[5].Tile.CustomColor = 7;  // per-sprite bank colours - the export must
      proj.Sprites[2].Tile.CustomColor = 11; // take these, not the overlay slot colour

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      Assert.AreEqual( 1, animCount );
      Assert.AreEqual( 2, sprCount );     // distinct referenced sprites: 5, 2
      Assert.AreEqual( (byte)1,  anim.ByteAt( HDR_ANIMATION_COUNT ) );
      Assert.AreEqual( (byte)2,  anim.ByteAt( HDR_SPRITE_COUNT ) );
      Assert.AreEqual( (byte)64, anim.ByteAt( HDR_SPRITE_DATA_STRIDE ) );
      Assert.AreEqual( (byte)2,  anim.ByteAt( HDR_ANIM_FRAME_SIZE ) );

      int loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int idTable = anim.UInt16At( HDR_OFFSET_ANIM_ID );
      Assert.IsTrue( loTable >= 0x0A && loTable < anim.Length, "anim_def_lo points in-file" );
      Assert.AreEqual( (byte)7, anim.ByteAt( idTable ) );   // authored animation id

      int defOff = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.IsTrue( defOff >= 0x0A && defOff < anim.Length, "def offset is a real file offset" );

      Assert.AreEqual( (byte)7, anim.ByteAt( defOff + 0 ) );   // id
      Assert.AreEqual( (byte)4, anim.ByteAt( defOff + 1 ) );   // delay (1/50s)
      Assert.AreEqual( (byte)1, anim.ByteAt( defOff + 2 ) );   // expand x
      Assert.AreEqual( (byte)0, anim.ByteAt( defOff + 3 ) );   // expand y
      Assert.AreEqual( (byte)0, anim.ByteAt( defOff + 4 ) );   // multicolor (sprites are hires)
      Assert.AreEqual( (byte)3, anim.ByteAt( defOff + 5 ) );   // frame count
      Assert.AreEqual( (byte)1, anim.ByteAt( defOff + 6 ) );   // loop
      Assert.AreEqual( (byte)0, anim.ByteAt( defOff + 7 ) );   // start_random (default off)

      // Frames (2 bytes each) now start at +8: sprite pointer = compact block index,
      // color = the referenced bank sprite's own CustomColor (NOT the overlay slot
      // colour). Referenced order 5,2 -> 5=block0, 2=block1. Frames 5,2,5 -> ptr
      // 0,1,0, colours 7,11,7 (sprite 5=7, sprite 2=11) - never the overlay's 13.
      Assert.AreEqual( (byte)0,  anim.ByteAt( defOff + 8 ) );  // frame0 ptr
      Assert.AreEqual( (byte)7,  anim.ByteAt( defOff + 9 ) );  // frame0 color = sprite 5
      Assert.AreEqual( (byte)1,  anim.ByteAt( defOff + 10 ) ); // frame1 ptr
      Assert.AreEqual( (byte)11, anim.ByteAt( defOff + 11 ) ); // frame1 color = sprite 2
      Assert.AreEqual( (byte)0,  anim.ByteAt( defOff + 12 ) ); // frame2 ptr
      Assert.AreEqual( (byte)7,  anim.ByteAt( defOff + 13 ) ); // frame2 color = sprite 5
    }

    // ================================================================
    // 2. Sprite data: only referenced sprites, correctly indexed
    // ================================================================
    [TestMethod]
    public void TestSpriteDataCompactionIndexing()
    {
      var proj = MakeProject( 8 );
      AddOverlay( proj, 5, 2, 5 );

      // sanity: the source sprite bytes are what we set.
      Assert.AreEqual( (uint)63, proj.Sprites[5].Tile.Data.Length );
      Assert.AreEqual( (byte)( 0xA0 + 5 ), proj.Sprites[5].Tile.Data.ByteAt( 0 ) );

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      Assert.AreEqual( 2, sprCount );
      Assert.AreEqual( (uint)( 2 * 64 ), sprdata.Length );

      // Block 0 must hold sprite 5's bytes; block 1 must hold sprite 2's bytes.
      Assert.AreEqual( (byte)( 0xA0 + 5 ), sprdata.ByteAt( 0 * 64 ) );
      Assert.AreEqual( (byte)( 0xA0 + 2 ), sprdata.ByteAt( 1 * 64 ) );
    }

    // ================================================================
    // 3. Multicolor flag from the first referenced sprite
    // ================================================================
    [TestMethod]
    public void TestMulticolorFlag()
    {
      var proj = MakeProject( 4 );
      proj.Sprites[1].Mode = SpriteMode.COMMODORE_24_X_21_MULTICOLOR;   // first referenced
      AddOverlay( proj, 1, 0 );

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      int loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int defOff  = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.AreEqual( (byte)1, anim.ByteAt( defOff + 4 ) );   // multicolor
    }

    // ================================================================
    // 4. Empty project -> valid header, zero counts
    // ================================================================
    [TestMethod]
    public void TestEmptyProject()
    {
      var proj = new SpriteProject();
      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      Assert.AreEqual( 0, animCount );
      Assert.AreEqual( 0, sprCount );
      Assert.AreEqual( (byte)0, anim.ByteAt( HDR_ANIMATION_COUNT ) );
      Assert.AreEqual( (byte)0, anim.ByteAt( HDR_SPRITE_COUNT ) );
      Assert.AreEqual( (uint)0, sprdata.Length );
      Assert.AreEqual( (ushort)0, anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO ) );   // section absent
    }

    // ================================================================
    // 5. Loop / AnimationID round-trip through the project file
    // ================================================================
    [TestMethod]
    public void TestOverlayLoopAndIdRoundTrip()
    {
      var proj = MakeProject( 2 );
      var ov = AddOverlay( proj, 0, 1 );
      ov.Loop        = false;
      ov.AnimationID = 42;
      ov.FrameDelay  = 9;

      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      proj2.ReadFromBuffer( saved );

      Assert.AreEqual( 1, proj2.Overlays.Count );
      Assert.AreEqual( false, proj2.Overlays[0].Loop );
      Assert.AreEqual( 42, proj2.Overlays[0].AnimationID );
      Assert.AreEqual( 9, proj2.Overlays[0].FrameDelay );
    }

    // ================================================================
    // 7. Layout/offset dump decodes the anim-defs binary consistently
    // ================================================================
    [TestMethod]
    public void TestLayoutDumpMatchesBinary()
    {
      var proj = MakeProject( 8 );
      var ov = AddOverlay( proj, 5, 2, 5 );
      ov.AnimationID = 7;
      ov.FrameDelay  = 4;
      ov.Loop        = true;
      ov.Slots[0].CustomColor = -1;          // "None": per-sprite colours in the dump
      ov.Slots[0].ExpandX = true;
      proj.Sprites[5].Tile.CustomColor = 7;  // per-sprite bank colours the export uses
      proj.Sprites[2].Tile.CustomColor = 11;

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      string dump = proj.GenerateSpriteGameBinaryLayoutText( anim, sprdata );

      // Header decode.
      Assert.IsTrue( dump.Contains( "animation_count = 1" ), "animation_count line" );
      Assert.IsTrue( dump.Contains( "sprite_count = 2" ), "sprite_count line" );
      Assert.IsTrue( dump.Contains( "sprite_data_stride = 64" ), "stride line" );
      // Resolved def offset must match the actual offset in the binary.
      int loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int defOff  = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.IsTrue( dump.Contains( "ANIMATION 0 (id $07) @ $" + defOff.ToString( "X4" ) ), "animation header with resolved offset" );
      // Definition fields.
      Assert.IsTrue( dump.Contains( "delay = 4 (1/50s)" ), "delay line" );
      Assert.IsTrue( dump.Contains( "frame_count = 3" ), "frame_count line" );
      Assert.IsTrue( dump.Contains( "loop = 1" ), "loop line" );
      Assert.IsTrue( dump.Contains( "start_random = 0" ), "start_random line" );
      // Frames: ptr 0,1,0 with per-sprite colours 7,11,7 -> $07,$0B,$07.
      Assert.IsTrue( dump.Contains( "frame 0 -> sprite_ptr $00, color $07" ), "frame 0 decode" );
      Assert.IsTrue( dump.Contains( "frame 1 -> sprite_ptr $01, color $0B" ), "frame 1 decode" );
      Assert.IsTrue( dump.Contains( "frame 2 -> sprite_ptr $00, color $07" ), "frame 2 decode" );
      // Sprite-data summary.
      Assert.IsTrue( dump.Contains( "SPRITE-DATA FILE (2 blocks x 64 bytes = 128 bytes)" ), "sprite-data summary" );
    }

    // ================================================================
    // 6. Game-binary export settings round-trip
    // ================================================================
    [TestMethod]
    public void TestGameBinarySettingsRoundTrip()
    {
      var proj = MakeProject( 1 );
      proj.GameBinary.AnimFilename    = "anims.bin";
      proj.GameBinary.AnimAsm         = false;
      proj.GameBinary.SprFilename     = "sprites.bin";
      proj.GameBinary.SprCompress     = true;
      proj.GameBinary.SprCompressor   = "ZX0";
      proj.GameBinary.SprCompressFile = "sprites.zx0";
      proj.GameBinary.SprMaxSizeText  = "16384";
      proj.GameBinary.AnimCompressor  = "ZX0";
      proj.GameBinary.AnimAsmDirectory      = @"C:\out\asm";
      proj.GameBinary.AnimCompressDirectory = @"C:\out\animzx";
      proj.GameBinary.SprCompressDirectory  = @"C:\out\sprzx";
      proj.ExportMethodIndex = 6;

      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      proj2.ReadFromBuffer( saved );

      Assert.AreEqual( "anims.bin",   proj2.GameBinary.AnimFilename );
      Assert.AreEqual( false,         proj2.GameBinary.AnimAsm );
      Assert.AreEqual( "sprites.bin", proj2.GameBinary.SprFilename );
      Assert.AreEqual( true,          proj2.GameBinary.SprCompress );
      Assert.AreEqual( "sprites.zx0", proj2.GameBinary.SprCompressFile );
      Assert.AreEqual( "16384",       proj2.GameBinary.SprMaxSizeText );
      Assert.AreEqual( "ZX0",         proj2.GameBinary.AnimCompressor );
      Assert.AreEqual( "ZX0",         proj2.GameBinary.SprCompressor );
      Assert.AreEqual( @"C:\out\asm",    proj2.GameBinary.AnimAsmDirectory );
      Assert.AreEqual( @"C:\out\animzx", proj2.GameBinary.AnimCompressDirectory );
      Assert.AreEqual( @"C:\out\sprzx",  proj2.GameBinary.SprCompressDirectory );
      Assert.AreEqual( 6,             proj2.ExportMethodIndex );
    }

    // ================================================================
    // 9. Settings + export method survive a save/reload WITH an overlay
    //    present (the user's real scenario: GameBinary chunk is written
    //    after the overlay chunks).
    // ================================================================
    [TestMethod]
    public void TestSettingsSurviveWithOverlay()
    {
      var proj = MakeProject( 8 );
      var ov = AddOverlay( proj, 5, 2, 5 );
      ov.AnimationID = 7;
      ov.FrameDelay  = 4;
      ov.Loop        = true;
      proj.GameBinary.AnimFilename          = "anims.bin";
      proj.GameBinary.AnimCompress          = true;
      proj.GameBinary.AnimCompressDirectory = @"C:\zx";
      proj.GameBinary.SprFilename           = "spr.bin";
      proj.GameBinary.SprOverrideHex        = "$2000";
      proj.ExportMethodIndex                = 6;

      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      Assert.IsTrue( proj2.ReadFromBuffer( saved ) );

      Assert.AreEqual( 1, proj2.Overlays.Count );
      Assert.AreEqual( "anims.bin", proj2.GameBinary.AnimFilename );
      Assert.AreEqual( true, proj2.GameBinary.AnimCompress );
      Assert.AreEqual( @"C:\zx", proj2.GameBinary.AnimCompressDirectory );
      Assert.AreEqual( "spr.bin", proj2.GameBinary.SprFilename );
      Assert.AreEqual( "$2000", proj2.GameBinary.SprOverrideHex );
      Assert.AreEqual( 6, proj2.ExportMethodIndex );
    }

    // ================================================================
    // 8. Clipboard copy/paste covers EVERY settings field (auto-guard).
    //    Reflectively sets every public field, round-trips through the
    //    clipboard text, and fails by name if a field was dropped — so a
    //    field added to SpriteGameBinarySettings but not to ToClipboardString/
    //    FromClipboardString breaks this test immediately.
    // ================================================================
    [TestMethod]
    public void TestClipboardCoversEveryField()
    {
      var s = new SpriteProject.SpriteGameBinarySettings();
      var fields = typeof( SpriteProject.SpriteGameBinarySettings )
        .GetFields( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance );

      int n = 0;
      foreach ( var f in fields )
      {
        if ( f.FieldType == typeof( string ) )
        {
          f.SetValue( s, "val_" + n );           // distinctive, no '=' or newline
        }
        else if ( f.FieldType == typeof( bool ) )
        {
          f.SetValue( s, !(bool)f.GetValue( s ) ); // flip from its default
        }
        else
        {
          Assert.Fail( "SpriteGameBinarySettings field '" + f.Name + "' has type "
            + f.FieldType.Name + " which this test (and likely ToClipboardString/"
            + "FromClipboardString) does not handle. Update all three." );
        }
        ++n;
      }

      var s2 = SpriteProject.SpriteGameBinarySettings.FromClipboardString( s.ToClipboardString() );
      Assert.IsNotNull( s2, "round-trip returned null" );
      foreach ( var f in fields )
      {
        Assert.AreEqual( f.GetValue( s ), f.GetValue( s2 ),
          "Field '" + f.Name + "' did not survive the clipboard round-trip — add it to "
          + "ToClipboardString AND FromClipboardString." );
      }

      // Foreign / empty clipboard content must be rejected (paste shows a message).
      Assert.IsNull( SpriteProject.SpriteGameBinarySettings.FromClipboardString( "hello world" ) );
      Assert.IsNull( SpriteProject.SpriteGameBinarySettings.FromClipboardString( "" ) );
      Assert.IsNull( SpriteProject.SpriteGameBinarySettings.FromClipboardString( null ) );
    }

    // ================================================================
    // 10. Sprite-test playfield C64-magnification settings round-trip
    // ================================================================
    [TestMethod]
    public void TestPlayfieldMagnificationSettingsRoundTrip()
    {
      var proj = MakeProject( 1 );
      proj.TestUseC64Magnification = true;
      proj.TestTargetWidth  = 384;   // a C64 screen incl. borders
      proj.TestTargetHeight = 272;

      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      proj2.ReadFromBuffer( saved );

      Assert.AreEqual( true, proj2.TestUseC64Magnification );
      Assert.AreEqual( 384, proj2.TestTargetWidth );
      Assert.AreEqual( 272, proj2.TestTargetHeight );

      // A fresh project (and old files that lack these bytes) defaults to off,
      // with the C64 screen sized to the 320x200 visible area.
      var fresh = new SpriteProject();
      Assert.AreEqual( false, fresh.TestUseC64Magnification );
      Assert.AreEqual( 320, fresh.TestTargetWidth );
      Assert.AreEqual( 200, fresh.TestTargetHeight );
    }

    // ================================================================
    // 11. ALL animations are exported, sharing ONE compacted sprite bank.
    //     A sprite referenced by two animations is stored once; both refer
    //     to the same block.
    // ================================================================
    [TestMethod]
    public void TestMultipleAnimationsShareSpriteBank()
    {
      var proj = MakeProject( 8 );
      var a = AddOverlay( proj, 5, 2 );   // animation 0 references sprites 5, 2
      a.AnimationID = 1;
      var b = AddOverlay( proj, 2, 7 );   // animation 1 references sprites 2 (shared), 7
      b.AnimationID = 2;

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      // Both animations exported.
      Assert.AreEqual( 2, animCount );
      Assert.AreEqual( (byte)2, anim.ByteAt( HDR_ANIMATION_COUNT ) );

      // ONE shared bank: distinct sprites 5,2,7 -> 3 blocks, first-use order.
      Assert.AreEqual( 3, sprCount );
      Assert.AreEqual( (byte)3, anim.ByteAt( HDR_SPRITE_COUNT ) );
      Assert.AreEqual( (uint)( 3 * 64 ), sprdata.Length );
      Assert.AreEqual( (byte)( 0xA0 + 5 ), sprdata.ByteAt( 0 * 64 ) );   // block 0 = sprite 5
      Assert.AreEqual( (byte)( 0xA0 + 2 ), sprdata.ByteAt( 1 * 64 ) );   // block 1 = sprite 2 (shared)
      Assert.AreEqual( (byte)( 0xA0 + 7 ), sprdata.ByteAt( 2 * 64 ) );   // block 2 = sprite 7

      int offLo = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int offHi = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int offId = anim.UInt16At( HDR_OFFSET_ANIM_ID );

      // Per-animation id table preserves each authored id.
      Assert.AreEqual( (byte)1, anim.ByteAt( offId + 0 ) );
      Assert.AreEqual( (byte)2, anim.ByteAt( offId + 1 ) );

      // Animation 0: frames -> block 0 (sprite 5), block 1 (sprite 2).
      int def0 = anim.ByteAt( offLo + 0 ) | ( anim.ByteAt( offHi + 0 ) << 8 );
      Assert.IsTrue( def0 >= 0x0A && def0 < anim.Length, "def0 in-file" );
      Assert.AreEqual( (byte)1, anim.ByteAt( def0 + 0 ) );   // id
      Assert.AreEqual( (byte)2, anim.ByteAt( def0 + 5 ) );   // frame count
      Assert.AreEqual( (byte)0, anim.ByteAt( def0 + 8 ) );   // frame0 ptr -> block 0
      Assert.AreEqual( (byte)1, anim.ByteAt( def0 + 10 ) );  // frame1 ptr -> block 1

      // Animation 1: frames -> block 1 (shared sprite 2), block 2 (sprite 7).
      int def1 = anim.ByteAt( offLo + 1 ) | ( anim.ByteAt( offHi + 1 ) << 8 );
      Assert.IsTrue( def1 >= 0x0A && def1 < anim.Length, "def1 in-file" );
      Assert.AreEqual( (byte)2, anim.ByteAt( def1 + 0 ) );   // id
      Assert.AreEqual( (byte)2, anim.ByteAt( def1 + 5 ) );   // frame count
      Assert.AreEqual( (byte)1, anim.ByteAt( def1 + 8 ) );   // frame0 ptr -> block 1 (shared)
      Assert.AreEqual( (byte)2, anim.ByteAt( def1 + 10 ) );  // frame1 ptr -> block 2

      // The two definitions must occupy distinct, non-overlapping regions.
      Assert.AreNotEqual( def0, def1 );
    }

    // ================================================================
    // 12. "Start at random frame" per-animation flag: exported at +$07,
    //     survives a project save/reload, defaults to false.
    // ================================================================
    [TestMethod]
    public void TestStartAtRandomFrameExportAndRoundTrip()
    {
      var proj = MakeProject( 4 );
      var ov = AddOverlay( proj, 1, 2 );
      ov.StartAtRandomFrame = true;

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      int loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int defOff  = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.AreEqual( (byte)1, anim.ByteAt( defOff + 7 ) );   // start_random exported at +$07

      // Survives a project save/reload.
      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      proj2.ReadFromBuffer( saved );
      Assert.AreEqual( 1, proj2.Overlays.Count );
      Assert.AreEqual( true, proj2.Overlays[0].StartAtRandomFrame );

      // Fresh overlay defaults to false.
      Assert.AreEqual( false, new SpriteProject.Overlay().StartAtRandomFrame );
    }

    // ================================================================
    // 13. Slot-0 colour override: "None" (-1, the default) exports each
    //     frame in its bank sprite's own colour; 0..15 forces that one
    //     colour into EVERY frame. Also round-trips through save/load.
    // ================================================================
    [TestMethod]
    public void TestSlotColorOverrideExportAndRoundTrip()
    {
      // Default is "None".
      Assert.AreEqual( -1, new SpriteProject.OverlaySlot().CustomColor );

      var proj = MakeProject( 8 );
      var ov = AddOverlay( proj, 5, 2 );
      proj.Sprites[5].Tile.CustomColor = 7;
      proj.Sprites[2].Tile.CustomColor = 11;
      ov.Slots[0].CustomColor = 13;          // override: all frames become colour 13

      ByteBuffer anim, sprdata;
      int animCount, sprCount;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );

      int loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      int hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      int defOff  = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.AreEqual( (byte)13, anim.ByteAt( defOff + 9 ) );   // frame0 colour = override
      Assert.AreEqual( (byte)13, anim.ByteAt( defOff + 11 ) );  // frame1 colour = override

      // Override (13) and "None" (-1) both survive a project save/reload.
      ov.Slots[1].CustomColor = -1;
      var saved = proj.SaveToBuffer();
      var proj2 = new SpriteProject();
      proj2.ReadFromBuffer( saved );
      Assert.AreEqual( 13, proj2.Overlays[0].Slots[0].CustomColor );
      Assert.AreEqual( -1, proj2.Overlays[0].Slots[1].CustomColor );

      // Back to "None" -> per-sprite colours again.
      proj.Overlays[0].Slots[0].CustomColor = -1;
      proj.ExportAsGameBinarySprites( out anim, out sprdata, out animCount, out sprCount );
      loTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_LO );
      hiTable = anim.UInt16At( HDR_OFFSET_ANIM_DEF_HI );
      defOff  = anim.ByteAt( loTable ) | ( anim.ByteAt( hiTable ) << 8 );
      Assert.AreEqual( (byte)7,  anim.ByteAt( defOff + 9 ) );   // frame0 = sprite 5's colour
      Assert.AreEqual( (byte)11, anim.ByteAt( defOff + 11 ) );  // frame1 = sprite 2's colour
    }
  }
}
