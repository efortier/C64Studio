extern alias studio;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PressureStroke = studio::RetroDevStudio.Controls.PressureStroke;



namespace TestProject
{
  /// <summary>
  /// Pixel-math verification for the pen pressure stroke compositor. The core
  /// routine (PressureStroke.CompositeCapsulePixels) is pure array math with no
  /// GDI dependency, so the max-coverage + lerp behaviour that the running app
  /// can't easily be asserted on is nailed down here instead.
  ///
  /// Buffers are 32bpp little-endian BGRA rows (what GDI+ Format32bppArgb
  /// LockBits yields): index 0=B, 1=G, 2=R, 3=A.
  /// </summary>
  [TestClass]
  public class TestPressureStroke
  {
    // Black opaque snapshot pixel (BGRA).
    private static byte[] BlackRow( int cols )
    {
      var row = new byte[cols * 4];
      for ( int x = 0; x < cols; ++x )
      {
        row[x * 4 + 3] = 255;   // A only; RGB = 0
      }
      return row;
    }



    // Tile row with a uniform coverage alpha across all pixels (BGRA, white).
    private static byte[] TileRow( int cols, byte alpha )
    {
      var row = new byte[cols * 4];
      for ( int x = 0; x < cols; ++x )
      {
        row[x * 4 + 0] = 255;
        row[x * 4 + 1] = 255;
        row[x * 4 + 2] = 255;
        row[x * 4 + 3] = alpha;
      }
      return row;
    }



    [TestMethod]
    public void TestOpaqueFullCoverageWritesInk()
    {
      // Fully-covered capsule, full alpha, red ink over black → pixel = red.
      int cols = 4;
      var cov = new byte[cols];
      var dest = new byte[cols * 4];
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), BlackRow( cols ), dest, cols, cov, 0, 1.0f,
        /*InkR*/ 255, /*InkG*/ 0, /*InkB*/ 0 );

      for ( int x = 0; x < cols; ++x )
      {
        Assert.AreEqual( 255, dest[x * 4 + 2], "R" );  // R
        Assert.AreEqual( 0, dest[x * 4 + 1], "G" );    // G
        Assert.AreEqual( 0, dest[x * 4 + 0], "B" );    // B
        Assert.AreEqual( 255, dest[x * 4 + 3], "A" );  // opaque
        Assert.AreEqual( 255, cov[x], "coverage" );
      }
    }



    [TestMethod]
    public void TestHalfCoverageLerps()
    {
      // Half effective alpha over black → halfway to ink.
      int cols = 2;
      var cov = new byte[cols];
      var dest = new byte[cols * 4];
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), BlackRow( cols ), dest, cols, cov, 0, 0.5f,
        200, 100, 40 );

      // cov = 255 * 0.5 = 127; dest = 0 + ink*127/255.
      Assert.AreEqual( 127, cov[0] );
      Assert.AreEqual( 200 * 127 / 255, dest[2], "R" );
      Assert.AreEqual( 100 * 127 / 255, dest[1], "G" );
      Assert.AreEqual( 40 * 127 / 255, dest[0], "B" );
    }



    [TestMethod]
    public void TestMaxCoverageDoesNotDoubleBlend()
    {
      // Two overlapping semi-transparent passes on the SAME coverage buffer must
      // take the MAX (0.5), not accumulate to ~0.75 — that is the whole reason
      // for the coverage buffer instead of source-over dab stamping.
      int cols = 2;
      var cov = new byte[cols];
      var dest = new byte[cols * 4];
      var snap = BlackRow( cols );

      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), snap, dest, cols, cov, 0, 0.5f, 255, 0, 0 );
      int afterFirst = dest[2];   // R
      Assert.AreEqual( 127, cov[0] );

      // Second identical pass over the same coverage — must not raise it.
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), snap, dest, cols, cov, 0, 0.5f, 255, 0, 0 );
      Assert.AreEqual( 127, cov[0], "coverage stayed at the max, not summed" );
      Assert.AreEqual( afterFirst, dest[2], "no double-blend darkening" );
    }



    [TestMethod]
    public void TestCoverageKeepsTheStrongerPress()
    {
      // A lighter pass after a heavier one must NOT reduce coverage.
      int cols = 1;
      var cov = new byte[cols];
      var dest = new byte[cols * 4];
      var snap = BlackRow( cols );

      // Heavy press first (0.8 → cov 204).
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), snap, dest, cols, cov, 0, 0.8f, 255, 255, 255 );
      Assert.AreEqual( 204, cov[0] );

      // Light press over the same pixel (0.3 → 76) must be ignored (max wins).
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 255 ), snap, dest, cols, cov, 0, 0.3f, 255, 255, 255 );
      Assert.AreEqual( 204, cov[0], "max coverage retained" );
      Assert.AreEqual( 204, dest[2], "R reflects the stronger press" );
    }



    [TestMethod]
    public void TestPartialTileCoverageAttenuates()
    {
      // Anti-aliased fringe: tile alpha 128 at full capsule alpha → cov 128.
      int cols = 1;
      var cov = new byte[cols];
      var dest = new byte[cols * 4];
      PressureStroke.CompositeCapsulePixels(
        TileRow( cols, 128 ), BlackRow( cols ), dest, cols, cov, 0, 1.0f, 255, 255, 255 );
      Assert.AreEqual( 128, cov[0] );
      Assert.AreEqual( 128, dest[2] );
    }
  }
}
