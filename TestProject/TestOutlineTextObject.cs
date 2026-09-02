extern alias studio;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OutlineTextObject = studio::RetroDevStudio.Controls.OutlineTextObject;
using OutlineTextAlignment = studio::RetroDevStudio.Controls.OutlineTextAlignment;
using OutlineTextLayout = studio::RetroDevStudio.Controls.OutlineTextLayout;



namespace TestProject
{
  /// <summary>
  /// Persistent outline text objects: blob (de)serialization + deep-copy
  /// semantics. The blob is what rides inside the .mapoutlines sidecar entry,
  /// so a dropped field here = silent data loss on reload.
  /// </summary>
  [TestClass]
  public class TestOutlineTextObject
  {
    private static OutlineTextObject MakeObject( string Text, float X, float Y )
    {
      return new OutlineTextObject()
      {
        Text       = Text,
        Position   = new System.Drawing.PointF( X, Y ),
        FontFamily = "Courier New",
        FontSize   = 24.5f,
        Bold       = true,
        Italic     = true,
        Color      = System.Drawing.Color.FromArgb( 200, 10, 20, 30 )
      };
    }



    [TestMethod]
    public void TestBlobRoundTripAllFields()
    {
      var objects = new List<OutlineTextObject>()
      {
        // Deliberately beyond U+00FF (em-dash, €, arrow, CJK, emoji): the blob
        // must be UTF-8 — a regression to byte-per-char AppendString silently
        // corrupts typed text and localized font names on reload.
        MakeObject( "Boss — 500€ → ダンジョン 🗝\nLine two", 12.5f, -3.25f ),
        MakeObject( "Second", 400f, 250f )
      };
      objects[0].FontFamily = "MS ゴシック";
      objects[0].WrapWidth = 220.5f;
      objects[0].CharSpacing = 2.5f;
      objects[0].LineSpacing = -3f;
      objects[0].AutoBreakWidth = 333.25f;
      objects[1].Bold = false;
      objects[1].Italic = false;

      var blob = OutlineTextObject.SaveListToBuffer( objects );
      Assert.IsNotNull( blob );
      var reloaded = OutlineTextObject.ReadListFromBuffer( blob );

      Assert.AreEqual( 2, reloaded.Count );
      for ( int i = 0; i < 2; ++i )
      {
        Assert.AreEqual( objects[i].Text, reloaded[i].Text, "Text " + i );
        Assert.AreEqual( objects[i].Position.X, reloaded[i].Position.X, 0.0001f, "X " + i );
        Assert.AreEqual( objects[i].Position.Y, reloaded[i].Position.Y, 0.0001f, "Y " + i );
        Assert.AreEqual( objects[i].FontFamily, reloaded[i].FontFamily, "FontFamily " + i );
        Assert.AreEqual( objects[i].FontSize, reloaded[i].FontSize, 0.0001f, "FontSize " + i );
        Assert.AreEqual( objects[i].Bold, reloaded[i].Bold, "Bold " + i );
        Assert.AreEqual( objects[i].Italic, reloaded[i].Italic, "Italic " + i );
        Assert.AreEqual( objects[i].Color.ToArgb(), reloaded[i].Color.ToArgb(), "Color " + i );
        Assert.AreEqual( objects[i].WrapWidth, reloaded[i].WrapWidth, 0.0001f, "WrapWidth " + i );
        Assert.AreEqual( objects[i].CharSpacing, reloaded[i].CharSpacing, 0.0001f, "CharSpacing " + i );
        Assert.AreEqual( objects[i].LineSpacing, reloaded[i].LineSpacing, 0.0001f, "LineSpacing " + i );
        Assert.AreEqual( objects[i].AutoBreakWidth, reloaded[i].AutoBreakWidth, 0.0001f, "AutoBreakWidth " + i );
      }
    }



    [TestMethod]
    public void TestOldBlobWithoutAutoBreakWidthGetsMigrationSentinel()
    {
      // Hand-craft a chunk holding only the ORIGINAL seven fields (a blob
      // saved before WrapWidth/spacings/AutoBreakWidth existed). The read
      // must flag the missing frozen width with the -1 sentinel so the load
      // path migrates it once — NOT silently 0, which would un-wrap old text.
      var chunk = new GR.IO.FileChunk( studio::RetroDevStudio.FileChunkConstants.MAP_OUTLINE_TEXT_OBJECT );
      var textBytes = System.Text.Encoding.UTF8.GetBytes( "Old label" );
      chunk.AppendU32( (uint)textBytes.Length );
      chunk.Append( new GR.Memory.ByteBuffer( textBytes ) );
      var familyBytes = System.Text.Encoding.UTF8.GetBytes( "Arial" );
      chunk.AppendU32( (uint)familyBytes.Length );
      chunk.Append( new GR.Memory.ByteBuffer( familyBytes ) );
      chunk.AppendF32( 16f );                  // size
      chunk.AppendU8( 0 );                     // style bits
      chunk.AppendU32( 0xFFFFFFFF );           // color
      chunk.AppendF32( 10f );                  // x
      chunk.AppendF32( 20f );                  // y

      var reloaded = OutlineTextObject.ReadListFromBuffer( chunk.ToBuffer().Data() );
      Assert.AreEqual( 1, reloaded.Count );
      Assert.AreEqual( "Old label", reloaded[0].Text );
      Assert.AreEqual( 0f, reloaded[0].WrapWidth, 0.0001f, "missing WrapWidth defaults to 0" );
      Assert.AreEqual( -1f, reloaded[0].AutoBreakWidth, 0.0001f, "missing frozen width = migration sentinel" );

      // A leaked sentinel must behave as "no wrap", never poison the layout.
      Assert.AreEqual( 1, reloaded[0].GetLayout().Lines.Count );

      // And saving always persists a REAL value — the sentinel never round-trips.
      var resaved = OutlineTextObject.ReadListFromBuffer(
        OutlineTextObject.SaveListToBuffer( reloaded ) );
      Assert.AreEqual( 0f, resaved[0].AutoBreakWidth, 0.0001f, "sentinel must be clamped on save" );
    }



    [TestMethod]
    public void TestEmptyAndNullBlobs()
    {
      Assert.IsNull( OutlineTextObject.SaveListToBuffer( null ) );
      Assert.IsNull( OutlineTextObject.SaveListToBuffer( new List<OutlineTextObject>() ) );

      Assert.AreEqual( 0, OutlineTextObject.ReadListFromBuffer( null ).Count );
      Assert.AreEqual( 0, OutlineTextObject.ReadListFromBuffer( new byte[0] ).Count );
      // Garbage must not throw — tolerant read.
      Assert.AreEqual( 0, OutlineTextObject.ReadListFromBuffer( new byte[] { 0xDE, 0xAD, 0xBE } ).Count );
    }



    [TestMethod]
    public void TestAutoWrapFrozenWidthWrapsHugsAndNeverReflowsOnMove()
    {
      // A long single (no '\n') line that clearly overruns 120px.
      var obj = new OutlineTextObject()
      {
        Text          = "alpha bravo charlie delta echo foxtrot golf hotel india",
        Position      = new System.Drawing.PointF( 0f, 0f ),
        FontFamily    = "Arial",
        FontSize      = 16f,
        WrapWidth     = 0f          // no explicit width → the frozen width governs
      };

      // No frozen width → one line (never-edited / explicit no-wrap case).
      obj.AutoBreakWidth = 0f;
      obj.InvalidateMeasurement();
      Assert.AreEqual( 1, obj.GetLayout().Lines.Count, "no frozen width must not wrap" );
      Assert.IsTrue( obj.MeasuredSize().Width > 120f, "sanity: the text overruns 120px unwrapped" );

      // Frozen at 120 (what the edit box committed) → wraps, and the FRAME
      // hugs within it (no fat right edge / bad centering).
      obj.AutoBreakWidth = 120f;
      obj.InvalidateMeasurement();
      var wrapped = obj.GetLayout();
      Assert.IsTrue( wrapped.Lines.Count > 1, "must wrap at the frozen width" );
      Assert.IsTrue( obj.MeasuredSize().Width <= 120f + 1f, "frame must hug within the frozen width" );

      // THE user-reported bug: moving must NEVER reflow — the breaks are
      // frozen at edit-commit, independent of position.
      int frozenLineCount = wrapped.Lines.Count;
      float frozenWidth = obj.MeasuredSize().Width;
      foreach ( var moved in new[] { new System.Drawing.PointF( 500f, 40f ),
                                     new System.Drawing.PointF( -80f, 0f ),
                                     new System.Drawing.PointF( 60f, 300f ) } )
      {
        obj.Position = moved;
        Assert.AreEqual( frozenLineCount, obj.GetLayout().Lines.Count,
                         "moving must not change the line breaks" );
        Assert.AreEqual( frozenWidth, obj.MeasuredSize().Width, 0.001f,
                         "moving must not change the measured size" );
      }
    }



    [TestMethod]
    public void TestAutoWrapHeightCountsLineSpacingPerWrappedLine()
    {
      var obj = new OutlineTextObject()
      {
        Text          = "alpha bravo charlie delta echo foxtrot golf hotel india",
        Position      = new System.Drawing.PointF( 0f, 0f ),
        FontFamily    = "Arial",
        FontSize      = 16f,
        WrapWidth     = 0f,
        AutoBreakWidth = 120f
      };

      obj.LineSpacing = 0f;
      obj.InvalidateMeasurement();
      int lineCount = obj.GetLayout().Lines.Count;
      Assert.IsTrue( lineCount > 1, "precondition: the text wrapped" );
      float heightNoSpacing = obj.MeasuredSize().Height;

      obj.LineSpacing = 10f;
      obj.InvalidateMeasurement();
      float heightWithSpacing = obj.MeasuredSize().Height;

      // Line spacing applies BETWEEN every visual line, wrapped ones included.
      Assert.AreEqual( ( lineCount - 1 ) * 10f, heightWithSpacing - heightNoSpacing, 0.5f,
                       "line spacing must count each wrapped line gap" );
    }



    [TestMethod]
    public void TestCloneIsDeepAndIndependent()
    {
      var original = MakeObject( "Original", 10f, 20f );
      var clone = original.Clone();

      clone.Text = "Changed";
      clone.Position = new System.Drawing.PointF( 99f, 99f );
      clone.Bold = false;

      Assert.AreEqual( "Original", original.Text );
      Assert.AreEqual( 10f, original.Position.X );
      Assert.IsTrue( original.Bold );

      var list = new List<OutlineTextObject>() { original };
      var cloned = OutlineTextObject.CloneList( list );
      cloned[0].Text = "ListChanged";
      Assert.AreEqual( "Original", original.Text, "CloneList must deep-copy" );
    }



    // ================================================================
    // Pasted-image objects — they share the list/blob with text objects;
    // kind and z-order must round-trip, payload byte-exact.
    // ================================================================

    private static byte[] MakeFakePng( byte Seed, int Length )
    {
      // The blob layer never decodes — any payload exercises the round-trip.
      var data = new byte[Length];
      for ( int i = 0; i < Length; ++i )
      {
        data[i] = (byte)( Seed + i );
      }
      return data;
    }



    [TestMethod]
    public void TestImageObjectBlobRoundTripMixedList()
    {
      var payload = MakeFakePng( 7, 777 );
      var objects = new List<OutlineTextObject>()
      {
        MakeObject( "Below", 5f, 6f ),
        new OutlineTextObject()
        {
          Position     = new System.Drawing.PointF( 100.5f, -20.25f ),
          ImagePNGData = payload
        },
        MakeObject( "Above", 300f, 40f )
      };

      var blob = OutlineTextObject.SaveListToBuffer( objects );
      Assert.IsNotNull( blob );
      var reloaded = OutlineTextObject.ReadListFromBuffer( blob );

      // Kind AND z-order (list order) must survive — the image sits between
      // the two texts exactly as saved.
      Assert.AreEqual( 3, reloaded.Count );
      Assert.IsFalse( reloaded[0].IsImage );
      Assert.IsTrue( reloaded[1].IsImage );
      Assert.IsFalse( reloaded[2].IsImage );
      Assert.AreEqual( "Below", reloaded[0].Text );
      Assert.AreEqual( "Above", reloaded[2].Text );
      Assert.AreEqual( 100.5f, reloaded[1].Position.X, 0.0001f );
      Assert.AreEqual( -20.25f, reloaded[1].Position.Y, 0.0001f );
      CollectionAssert.AreEqual( payload, reloaded[1].ImagePNGData );
    }



    [TestMethod]
    public void TestImageObjectCloneSharesImmutablePayload()
    {
      var payload = MakeFakePng( 3, 64 );
      var original = new OutlineTextObject()
      {
        Position     = new System.Drawing.PointF( 10f, 20f ),
        ImagePNGData = payload
      };
      var clone = original.Clone();

      Assert.IsTrue( clone.IsImage );
      // The payload is immutable-by-contract — clones SHARE the reference
      // (undo snapshots would otherwise duplicate every pasted screenshot).
      Assert.AreSame( original.ImagePNGData, clone.ImagePNGData );
      clone.Position = new System.Drawing.PointF( 99f, 99f );
      Assert.AreEqual( 10f, original.Position.X, "position must be per-clone" );
    }



    [TestMethod]
    public void TestImageObjectTruncatedBlobKeepsPriorObjects()
    {
      var objects = new List<OutlineTextObject>()
      {
        MakeObject( "Survivor", 1f, 2f ),
        new OutlineTextObject()
        {
          Position     = new System.Drawing.PointF( 50f, 60f ),
          ImagePNGData = MakeFakePng( 9, 256 )
        }
      };
      var blob = OutlineTextObject.SaveListToBuffer( objects );

      // Clip into the image chunk's payload: the damaged trailing chunk is
      // dropped, everything before it still loads (tolerant-reader contract).
      var truncated = new byte[blob.Length - 5];
      System.Array.Copy( blob, truncated, truncated.Length );
      var reloaded = OutlineTextObject.ReadListFromBuffer( truncated );

      Assert.AreEqual( 1, reloaded.Count );
      Assert.IsFalse( reloaded[0].IsImage );
      Assert.AreEqual( "Survivor", reloaded[0].Text );
    }



    // ================================================================
    // Line justification — a per-object LAYOUT attribute: persisted as an
    // appended byte (older blobs read as Left), never changes the frame.
    // ================================================================

    [TestMethod]
    public void TestAlignmentBlobRoundTripAndDefault()
    {
      var objects = new List<OutlineTextObject>()
      {
        MakeObject( "left", 0f, 0f ),
        MakeObject( "center", 10f, 10f ),
        MakeObject( "right", 20f, 20f )
      };
      Assert.AreEqual( OutlineTextAlignment.Left, objects[0].Alignment, "new objects default to Left" );
      objects[1].Alignment = OutlineTextAlignment.Center;
      objects[2].Alignment = OutlineTextAlignment.Right;

      var reloaded = OutlineTextObject.ReadListFromBuffer( OutlineTextObject.SaveListToBuffer( objects ) );
      Assert.AreEqual( 3, reloaded.Count );
      Assert.AreEqual( OutlineTextAlignment.Left, reloaded[0].Alignment );
      Assert.AreEqual( OutlineTextAlignment.Center, reloaded[1].Alignment );
      Assert.AreEqual( OutlineTextAlignment.Right, reloaded[2].Alignment );

      // Clones carry it (undo snapshots would otherwise silently reset it).
      Assert.AreEqual( OutlineTextAlignment.Right, objects[2].Clone().Alignment );
    }



    [TestMethod]
    public void TestAlignmentOffsetsAndFrameIndependence()
    {
      // Two lines of clearly different widths; the frame is the widest line.
      var obj = MakeObject( "WWWWWWWW\nW", 0f, 0f );
      obj.Bold = false;
      obj.Italic = false;
      var frame = obj.MeasuredSize();
      var layout = obj.GetLayout();
      Assert.AreEqual( 2, layout.Lines.Count );
      var shortLine = layout.Lines[1];
      float slack = frame.Width - shortLine.Width;
      Assert.IsTrue( slack > 4f, "test needs a visibly shorter second line" );

      Assert.AreEqual( 0f, OutlineTextLayout.LineOffsetX( shortLine, frame.Width, OutlineTextAlignment.Left ), 0.0001f );
      Assert.AreEqual( System.Math.Round( slack * 0.5f ),
                       OutlineTextLayout.LineOffsetX( shortLine, frame.Width, OutlineTextAlignment.Center ), 0.0001f );
      Assert.AreEqual( System.Math.Round( slack ),
                       OutlineTextLayout.LineOffsetX( shortLine, frame.Width, OutlineTextAlignment.Right ), 0.0001f );
      // The widest line never shifts, whatever the alignment.
      Assert.AreEqual( 0f, OutlineTextLayout.LineOffsetX( layout.Lines[0], frame.Width, OutlineTextAlignment.Right ), 0.0001f );

      // Alignment is layout, not size: the frame and bounds are unchanged.
      obj.Alignment = OutlineTextAlignment.Right;
      Assert.AreEqual( frame.Width, obj.MeasuredSize().Width, 0.0001f );
      Assert.AreEqual( frame.Height, obj.MeasuredSize().Height, 0.0001f );
    }



    [TestMethod]
    public void TestImageObjectCorruptPayloadYieldsPlaceholder()
    {
      // Garbage bytes that aren't a decodable PNG: measurement/draw must get
      // a placeholder (16x16), never a null or an exception.
      var broken = new OutlineTextObject()
      {
        Position     = new System.Drawing.PointF( 0f, 0f ),
        ImagePNGData = MakeFakePng( 1, 32 )
      };
      var image = broken.GetImage();
      Assert.IsNotNull( image );
      Assert.AreEqual( 16, image.Width );
      Assert.AreEqual( 16, image.Height );
      var size = broken.MeasuredSize();
      Assert.AreEqual( 16f, size.Width, 0.0001f );
      Assert.AreEqual( 16f, size.Height, 0.0001f );
    }
  }
}
