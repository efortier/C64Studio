extern alias studio;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using studio::RetroDevStudio.CustomRenderer.DisplayFilters;
using ByteBuffer = studio::GR.Memory.ByteBuffer;



namespace TestProject
{
  /// <summary>
  /// Locks in the display-filter persistence contract:
  /// - every filter's parameters round-trip through the pipeline's
  ///   SaveToBuffer / LoadFromBuffer (a filter added to the save path but
  ///   not the load path, or a field missing from Save/Load, fails here);
  /// - legacy parameter blobs from older builds still load (append-only
  ///   format with position-guarded reads);
  /// - unknown filter type names are skipped instead of breaking the file.
  /// </summary>
  [TestClass]
  public class TestDisplayFilters
  {
    private static FilterPipeline RoundTrip( FilterPipeline source )
    {
      var buf = source.SaveToBuffer();
      var loaded = new FilterPipeline();
      loaded.LoadFromBuffer( buf );
      return loaded;
    }



    [TestMethod]
    public void TestAllFiltersRoundTripWithNonDefaultParameters()
    {
      var pipeline = new FilterPipeline();
      pipeline.Filters.Add( new ScanlineFilter
      {
        Enabled = false,
        Intensity = 61,
        BeamWidthMin = 33,
        BeamWidthMax = 141,
        PreserveBrightness = 42,
      } );
      pipeline.Filters.Add( new PhosphorMaskFilter
      {
        RBoost = 11, GBoost = 12, BBoost = 13, Dim = 14,
        Pattern = PhosphorMaskFilter.PATTERN_SLOT_MASK,
        MaskScale = 3,
        PreserveLuma = 77,
      } );
      pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 66, Sigma = 23 } );
      pipeline.Filters.Add( new GammaAdjustFilter { Gamma = 220, Brightness = -30, Contrast = 40 } );
      pipeline.Filters.Add( new ColorTemperatureFilter { Temperature = -55, Tint = 21 } );
      pipeline.Filters.Add( new BarrelDistortionFilter { Curvature = 44, Vignette = 33, CornerRadius = 22 } );
      pipeline.Filters.Add( new PALCompositeFilter
      {
        Standard = PALCompositeFilter.STANDARD_NTSC,
        Connection = PALCompositeFilter.CONNECTION_SVIDEO,
        LumaBandwidth = 42,
        ChromaBandwidth = 9,
        LineBlend = 77,
        PhaseError = 11,
        EdgeRise = 12,
        EdgeFall = 3,
        Saturation = 130,
        GammaSim = 1,
      } );
      pipeline.Filters.Add( new BloomFilter { Threshold = 41, BloomStrength = 42, BloomRadius = 23, HalationStrength = 44, HalationRadius = 95 } );
      pipeline.Filters.Add( new ConvergenceFilter { RShiftX = -12, RShiftY = 3, BShiftX = 12, BShiftY = -3 } );
      pipeline.Filters.Add( new PhosphorPersistenceFilter { HalfLifeMs = 333, Strength = 88 } );
      pipeline.Filters.Add( new VICLumaNoiseFilter { Strength = 63, Coarseness = 2 } );

      var loaded = RoundTrip( pipeline );

      Assert.AreEqual( pipeline.Filters.Count, loaded.Filters.Count );

      var sl = (ScanlineFilter)loaded.Filters[0];
      Assert.IsFalse( sl.Enabled );
      Assert.AreEqual( 61, sl.Intensity );
      Assert.AreEqual( 33, sl.BeamWidthMin );
      Assert.AreEqual( 141, sl.BeamWidthMax );
      Assert.AreEqual( 42, sl.PreserveBrightness );

      var pm = (PhosphorMaskFilter)loaded.Filters[1];
      Assert.AreEqual( 11, pm.RBoost );
      Assert.AreEqual( 12, pm.GBoost );
      Assert.AreEqual( 13, pm.BBoost );
      Assert.AreEqual( 14, pm.Dim );
      Assert.AreEqual( PhosphorMaskFilter.PATTERN_SLOT_MASK, pm.Pattern );
      Assert.AreEqual( 3, pm.MaskScale );
      Assert.AreEqual( 77, pm.PreserveLuma );

      var hb = (HorizontalBlurFilter)loaded.Filters[2];
      Assert.AreEqual( 66, hb.Strength );
      Assert.AreEqual( 23, hb.Sigma );

      var ga = (GammaAdjustFilter)loaded.Filters[3];
      Assert.AreEqual( 220, ga.Gamma );
      Assert.AreEqual( -30, ga.Brightness );
      Assert.AreEqual( 40, ga.Contrast );

      var ct = (ColorTemperatureFilter)loaded.Filters[4];
      Assert.AreEqual( -55, ct.Temperature );
      Assert.AreEqual( 21, ct.Tint );

      var bd = (BarrelDistortionFilter)loaded.Filters[5];
      Assert.AreEqual( 44, bd.Curvature );
      Assert.AreEqual( 33, bd.Vignette );
      Assert.AreEqual( 22, bd.CornerRadius );

      var pal = (PALCompositeFilter)loaded.Filters[6];
      Assert.AreEqual( PALCompositeFilter.STANDARD_NTSC, pal.Standard );
      Assert.AreEqual( PALCompositeFilter.CONNECTION_SVIDEO, pal.Connection );
      Assert.AreEqual( 42, pal.LumaBandwidth );
      Assert.AreEqual( 9, pal.ChromaBandwidth );
      Assert.AreEqual( 77, pal.LineBlend );
      Assert.AreEqual( 11, pal.PhaseError );
      Assert.AreEqual( 12, pal.EdgeRise );
      Assert.AreEqual( 3, pal.EdgeFall );
      Assert.AreEqual( 130, pal.Saturation );
      Assert.AreEqual( 1, pal.GammaSim );

      var bl = (BloomFilter)loaded.Filters[7];
      Assert.AreEqual( 41, bl.Threshold );
      Assert.AreEqual( 42, bl.BloomStrength );
      Assert.AreEqual( 23, bl.BloomRadius );
      Assert.AreEqual( 44, bl.HalationStrength );
      Assert.AreEqual( 95, bl.HalationRadius );

      var cv = (ConvergenceFilter)loaded.Filters[8];
      Assert.AreEqual( -12, cv.RShiftX );
      Assert.AreEqual( 3, cv.RShiftY );
      Assert.AreEqual( 12, cv.BShiftX );
      Assert.AreEqual( -3, cv.BShiftY );

      var pp = (PhosphorPersistenceFilter)loaded.Filters[9];
      Assert.AreEqual( 333, pp.HalfLifeMs );
      Assert.AreEqual( 88, pp.Strength );

      var vn = (VICLumaNoiseFilter)loaded.Filters[10];
      Assert.AreEqual( 63, vn.Strength );
      Assert.AreEqual( 2, vn.Coarseness );
    }



    [TestMethod]
    public void TestLegacyScanlineBlobStillLoads()
    {
      // Old builds saved (Intensity, Period, Offset). The rewrite ignores
      // Period/Offset and must fall back to defaults for the new fields.
      var legacyBlob = new ByteBuffer();
      legacyBlob.AppendI32( 37 );   // Intensity
      legacyBlob.AppendI32( 4 );    // Period (obsolete)
      legacyBlob.AppendI32( 2 );    // Offset (obsolete)

      var filter = new ScanlineFilter();
      int defaultMin      = filter.BeamWidthMin;
      int defaultMax      = filter.BeamWidthMax;
      int defaultPreserve = filter.PreserveBrightness;
      filter.LoadParameters( legacyBlob );

      Assert.AreEqual( 37, filter.Intensity );
      Assert.AreEqual( defaultMin, filter.BeamWidthMin );
      Assert.AreEqual( defaultMax, filter.BeamWidthMax );
      Assert.AreEqual( defaultPreserve, filter.PreserveBrightness );
    }



    [TestMethod]
    public void TestLegacyBlurBlobSeedsSigmaFromOldField()
    {
      // Old builds saved (Strength, Taps, SigmaTargetPx). Without the new
      // fourth field, the legacy sigma seeds the new source-px sigma.
      var legacyBlob = new ByteBuffer();
      legacyBlob.AppendI32( 55 );   // Strength
      legacyBlob.AppendI32( 11 );   // Taps (obsolete)
      legacyBlob.AppendI32( 17 );   // Sigma (legacy, target px)

      var filter = new HorizontalBlurFilter();
      filter.LoadParameters( legacyBlob );

      Assert.AreEqual( 55, filter.Strength );
      Assert.AreEqual( 17, filter.Sigma );
    }



    [TestMethod]
    public void TestLegacyPhosphorAndBarrelBlobsUseDefaultsForNewFields()
    {
      var phosphorBlob = new ByteBuffer();
      phosphorBlob.AppendI32( 21 );
      phosphorBlob.AppendI32( 22 );
      phosphorBlob.AppendI32( 23 );
      phosphorBlob.AppendI32( 24 );

      var pm = new PhosphorMaskFilter();
      int defaultPattern  = pm.Pattern;
      int defaultScale    = pm.MaskScale;
      int defaultPreserve = pm.PreserveLuma;
      pm.LoadParameters( phosphorBlob );
      Assert.AreEqual( 21, pm.RBoost );
      Assert.AreEqual( 24, pm.Dim );
      Assert.AreEqual( defaultPattern, pm.Pattern );
      Assert.AreEqual( defaultScale, pm.MaskScale );
      Assert.AreEqual( defaultPreserve, pm.PreserveLuma );

      var barrelBlob = new ByteBuffer();
      barrelBlob.AppendI32( 31 );
      barrelBlob.AppendI32( 32 );

      var bd = new BarrelDistortionFilter();
      int defaultCorner = bd.CornerRadius;
      bd.LoadParameters( barrelBlob );
      Assert.AreEqual( 31, bd.Curvature );
      Assert.AreEqual( 32, bd.Vignette );
      Assert.AreEqual( defaultCorner, bd.CornerRadius );
    }



    [TestMethod]
    public void TestUnknownFilterTypeIsSkippedOnLoad()
    {
      // Hand-build a pipeline blob (version 1) containing one unknown
      // filter followed by a known one — the loader must skip the unknown
      // entry (reading past its param blob by length) and still load the
      // rest.
      var buf = new ByteBuffer();
      buf.AppendU16( 1 );                       // envelope version
      buf.AppendI32( 2 );                       // filter count

      buf.AppendString( "FilterFromTheFuture" );
      buf.AppendU8( 1 );                        // enabled
      var unknownParams = new ByteBuffer();
      unknownParams.AppendI32( 12345 );
      buf.AppendI32( (int)unknownParams.Length );
      buf.Append( unknownParams );

      buf.AppendString( "ScanlineFilter" );
      buf.AppendU8( 1 );
      var scanParams = new ScanlineFilter { Intensity = 73 }.SaveParameters();
      buf.AppendI32( (int)scanParams.Length );
      buf.Append( scanParams );

      var pipeline = new FilterPipeline();
      pipeline.LoadFromBuffer( buf );

      Assert.AreEqual( 1, pipeline.Filters.Count );
      Assert.IsInstanceOfType( pipeline.Filters[0], typeof( ScanlineFilter ) );
      Assert.AreEqual( 73, ( (ScanlineFilter)pipeline.Filters[0] ).Intensity );
    }



    [TestMethod]
    public void TestLegacyPALBlobMapsOntoSignalEngine()
    {
      // v1 blobs carried (LumaBlur σ×10, ChromaBlur σ×10, LineBlend,
      // Saturation) for the old approximation engine. They must map onto
      // the signal engine's bandwidths per the documented formula, with
      // every NEW parameter neutral so old projects change minimally.
      var legacyBlob = new ByteBuffer();
      legacyBlob.AppendI32( 4 );     // LumaBlur
      legacyBlob.AppendI32( 30 );    // ChromaBlur
      legacyBlob.AppendI32( 65 );    // LineBlend
      legacyBlob.AppendI32( 100 );   // Saturation

      var f = new PALCompositeFilter();
      f.LoadParameters( legacyBlob );

      Assert.AreEqual( 56, f.LumaBandwidth );       // 60 - 4
      Assert.AreEqual( 14, f.ChromaBandwidth );     // 21 - 30/4
      Assert.AreEqual( 65, f.LineBlend );
      Assert.AreEqual( 100, f.Saturation );
      Assert.AreEqual( PALCompositeFilter.STANDARD_PAL, f.Standard );
      Assert.AreEqual( PALCompositeFilter.CONNECTION_COMPOSITE, f.Connection );
      Assert.AreEqual( 0, f.PhaseError );
      Assert.AreEqual( 0, f.EdgeRise );
      Assert.AreEqual( 0, f.EdgeFall );
      Assert.AreEqual( 0, f.GammaSim );
    }



    // ================================================================
    // Signal-engine sanity checks — run the actual Apply on small
    // buffers and assert the physics.
    // ================================================================

    private static studio::GR.Image.FastImage MakeImage( int w, int h, Func<int, int, uint> pattern )
    {
      var img = new studio::GR.Image.FastImage( w, h, studio::GR.Drawing.PixelFormat.Format32bppRgb );
      for ( int y = 0; y < h; ++y )
      {
        for ( int x = 0; x < w; ++x )
        {
          img.SetPixel( x, y, pattern( x, y ) );
        }
      }
      return img;
    }



    private static studio::GR.Image.FastImage ApplyFilter( PALCompositeFilter f, int w, int h, Func<int, int, uint> pattern )
    {
      var src = MakeImage( w, h, pattern );
      var dst = new studio::GR.Image.FastImage( w, h, studio::GR.Drawing.PixelFormat.Format32bppRgb );
      var ctx = new FilterContext
      {
        SourceBuffer   = src,
        TargetBuffer   = dst,
        RenderOffsetX  = 0,
        RenderOffsetY  = 0,
        MapPixelWidth  = w,
        MapPixelHeight = h,
        SourceWidth    = w,
        SourceHeight   = h,
      };
      f.Apply( ctx );
      src.Dispose();
      return dst;
    }



    [TestMethod]
    public void TestSignalEngineSolidGreyPassesThrough()
    {
      // A flat chroma-free input must come out (nearly) unchanged even
      // with the full authentic defaults active — any false color here
      // means luma is leaking into the chroma demodulator.
      var f = new PALCompositeFilter();
      var outImg = ApplyFilter( f, 32, 16, ( x, y ) => 0xff808080 );

      for ( int y = 0; y < 16; ++y )
      {
        for ( int x = 2; x < 30; ++x )
        {
          uint p = outImg.GetPixel( x, y );
          int r = (int)( ( p >> 16 ) & 0xff );
          int g = (int)( ( p >> 8 ) & 0xff );
          int b = (int)( p & 0xff );
          Assert.IsTrue( Math.Abs( r - 128 ) <= 5, $"R off at {x},{y}: {r}" );
          Assert.IsTrue( Math.Abs( g - 128 ) <= 5, $"G off at {x},{y}: {g}" );
          Assert.IsTrue( Math.Abs( b - 128 ) <= 5, $"B off at {x},{y}: {b}" );
        }
      }
      outImg.Dispose();
    }



    [TestMethod]
    public void TestPALDelayLineBlendsAlternatingLinesNTSCDoesNot()
    {
      // Alternating rows of two EQUAL-LUMA colors (green vs magenta, both
      // Y ≈ 70): the PAL delay line must average their chroma into one
      // solid color (the palstuff rule — "colors with matching brightness
      // blend"); NTSC has no delay line so the rows keep their distinct
      // colors. Equal luma matters: the delay line only averages CHROMA,
      // so a luma difference would survive under PAL too — authentically.
      Func<int, int, uint> pattern = ( x, y ) => ( ( y & 1 ) == 0 ) ? 0xff007800 : 0xffB40091;

      var palFilter = new PALCompositeFilter
      {
        Standard = PALCompositeFilter.STANDARD_PAL,
        LineBlend = 100,
        PhaseError = 0,
        EdgeRise = 0,
        EdgeFall = 0,
      };
      var ntscFilter = new PALCompositeFilter
      {
        Standard = PALCompositeFilter.STANDARD_NTSC,
        LineBlend = 100,
        PhaseError = 0,
        EdgeRise = 0,
        EdgeFall = 0,
      };

      var palOut = ApplyFilter( palFilter, 32, 16, pattern );
      var ntscOut = ApplyFilter( ntscFilter, 32, 16, pattern );

      double palDiff = AdjacentRowColorDiff( palOut, 7, 8 );
      double ntscDiff = AdjacentRowColorDiff( ntscOut, 7, 8 );

      Assert.IsTrue( ntscDiff > 40.0, $"NTSC rows should stay distinct, diff={ntscDiff:F1}" );
      Assert.IsTrue( palDiff < ntscDiff / 3.0, $"PAL rows should blend: pal={palDiff:F1} ntsc={ntscDiff:F1}" );

      palOut.Dispose();
      ntscOut.Dispose();
    }



    private static double AdjacentRowColorDiff( studio::GR.Image.FastImage img, int rowA, int rowB )
    {
      double sum = 0;
      int count = 0;
      for ( int x = 8; x < 24; ++x )
      {
        uint a = img.GetPixel( x, rowA );
        uint b = img.GetPixel( x, rowB );
        sum += Math.Abs( (int)( ( a >> 16 ) & 0xff ) - (int)( ( b >> 16 ) & 0xff ) );
        sum += Math.Abs( (int)( a & 0xff ) - (int)( b & 0xff ) );
        ++count;
      }
      return sum / count;
    }



    [TestMethod]
    public void TestCompositeSolidColorKeepsHueAndDesignLevelDotCrawl()
    {
      // Solid saturated red through PAL composite: the decoder must return
      // the SAME hue (a mis-phased demod reference rotates every color by
      // the high-pass's ~24° at the subcarrier) and the luma dot crawl
      // must stay at its designed (1 - NOTCH_DEPTH) residual — a
      // phase-misaligned notch ADDS ripple instead of cancelling (~5x).
      var f = new PALCompositeFilter { PhaseError = 0, EdgeRise = 0, EdgeFall = 0 };
      var outImg = ApplyFilter( f, 32, 16, ( x, y ) => 0xffC00000 );

      double sumR = 0, sumG = 0, sumB = 0;
      int count = 0;
      for ( int y = 4; y < 12; ++y )
      {
        for ( int x = 6; x < 26; ++x )
        {
          uint p = outImg.GetPixel( x, y );
          sumR += ( p >> 16 ) & 0xff;
          sumG += ( p >> 8 ) & 0xff;
          sumB += p & 0xff;
          ++count;
        }
      }
      double meanR = sumR / count;
      double meanG = sumG / count;
      double meanB = sumB / count;
      Assert.IsTrue( meanR > 150.0, $"red should stay saturated red, R={meanR:F1}" );
      Assert.IsTrue( meanG < 60.0, $"hue rotation leaks green, G={meanG:F1}" );
      Assert.IsTrue( meanB < 60.0, $"hue rotation leaks blue, B={meanB:F1}" );

      // Dot-crawl amplitude: per-row luma deviation stays near the design
      // residual (measured ≈±9 on this chroma; the broken notch gave ±57).
      double worst = 0;
      for ( int y = 4; y < 12; ++y )
      {
        double rowSum = 0;
        for ( int x = 6; x < 26; ++x )
        {
          uint p = outImg.GetPixel( x, y );
          rowSum += ( ( ( p >> 16 ) & 0xff ) + ( ( p >> 8 ) & 0xff ) + ( p & 0xff ) ) / 3.0;
        }
        double rowMean = rowSum / 20.0;
        for ( int x = 6; x < 26; ++x )
        {
          uint p = outImg.GetPixel( x, y );
          double luma = ( ( ( p >> 16 ) & 0xff ) + ( ( p >> 8 ) & 0xff ) + ( p & 0xff ) ) / 3.0;
          worst = Math.Max( worst, Math.Abs( luma - rowMean ) );
        }
      }
      Assert.IsTrue( worst < 25.0, $"dot crawl beyond design level: ±{worst:F1}" );

      outImg.Dispose();
    }



    [TestMethod]
    public void TestDefaultCompositeKeepsSingleBrightDotsVisible()
    {
      // Regression for the "can barely see anything" report: maps made of
      // 1-px dots and dithers on black must stay readable at the FRESH
      // DEFAULTS at a typical review zoom. Two causes were validated and
      // fixed: (1) the measured artifact is a ~2 px low→high TRANSITION
      // (τ ≈ 0.9 px, EdgeRise 9), not a 2 px TIME CONSTANT (EdgeRise 20 —
      // that crushes dots to ~40% before display); (2) decimating the
      // decoded signal to whole source pixels box-averaged the edge ramps
      // flat — the output now keeps subpixel resolution, so at zoom ≥ 2
      // the ramp's bright end stays visible like in the reference renders.
      // Measured at ZOOM 4 (128x64 target from a 32x16 source): a lone
      // white dot must keep a clearly visible core.
      Func<int, int, uint> dotSrc = ( x, y ) => ( ( x == 15 ) && ( y == 8 ) ) ? 0xffffffff : 0xff000000;

      var defaults = new PALCompositeFilter();      // fresh authentic defaults
      var crushed  = new PALCompositeFilter { EdgeRise = 20, EdgeFall = 5 };

      var defOut = ApplyZoomedFilter( defaults, 32, 16, 4, dotSrc );
      var crushOut = ApplyZoomedFilter( crushed, 32, 16, 4, dotSrc );

      int defPeak = 0, crushPeak = 0;
      for ( int y = 30; y < 38; ++y )
      {
        for ( int x = 52; x < 76; ++x )
        {
          uint dp = defOut.GetPixel( x, y );
          uint cp = crushOut.GetPixel( x, y );
          defPeak = Math.Max( defPeak, (int)( ( dp >> 8 ) & 0xff ) );
          crushPeak = Math.Max( crushPeak, (int)( ( cp >> 8 ) & 0xff ) );
        }
      }

      Assert.IsTrue( defPeak > 110, $"single dot too dark at defaults: {defPeak}" );
      Assert.IsTrue( defPeak > crushPeak + 25, $"defaults should be much brighter than the mis-calibrated 2px-τ: def={defPeak} crushed={crushPeak}" );

      defOut.Dispose();
      crushOut.Dispose();
    }



    private static studio::GR.Image.FastImage ApplyZoomedFilter( PALCompositeFilter f, int srcW, int srcH, int zoom, Func<int, int, uint> sourcePattern )
    {
      // The filter context maps a srcW x srcH source slice onto a zoomed
      // target rect — the source pattern is replicated into target space
      // the way the editor's DisplayPage upscale would.
      int w = srcW * zoom;
      int h = srcH * zoom;
      var src = MakeImage( w, h, ( x, y ) => sourcePattern( x / zoom, y / zoom ) );
      var dst = new studio::GR.Image.FastImage( w, h, studio::GR.Drawing.PixelFormat.Format32bppRgb );
      var ctx = new FilterContext
      {
        SourceBuffer   = src,
        TargetBuffer   = dst,
        RenderOffsetX  = 0,
        RenderOffsetY  = 0,
        MapPixelWidth  = w,
        MapPixelHeight = h,
        SourceWidth    = srcW,
        SourceHeight   = srcH,
      };
      f.Apply( ctx );
      src.Dispose();
      return dst;
    }



    [TestMethod]
    public void TestAsymmetricEdgesDarkenDenseHiresPatterns()
    {
      // The emusux0r mechanism: with slow rise / fast fall, a 1-px on/off
      // pattern never reaches full brightness, so its mean luma drops
      // measurably below the symmetric render of the same pattern.
      Func<int, int, uint> stripes = ( x, y ) => ( ( x & 1 ) == 0 ) ? 0xffffffff : 0xff000000;

      var asym = new PALCompositeFilter { PhaseError = 0, EdgeRise = 20, EdgeFall = 5 };
      var sym  = new PALCompositeFilter { PhaseError = 0, EdgeRise = 0, EdgeFall = 0 };

      var asymOut = ApplyFilter( asym, 32, 16, stripes );
      var symOut  = ApplyFilter( sym, 32, 16, stripes );

      double asymMean = MeanLuma( asymOut );
      double symMean  = MeanLuma( symOut );

      Assert.IsTrue( asymMean < symMean - 10.0,
                     $"dense pattern should darken: asym={asymMean:F1} sym={symMean:F1}" );

      asymOut.Dispose();
      symOut.Dispose();
    }



    private static double MeanLuma( studio::GR.Image.FastImage img )
    {
      double sum = 0;
      int count = 0;
      for ( int y = 0; y < img.Height; ++y )
      {
        for ( int x = 4; x < img.Width - 4; ++x )
        {
          uint p = img.GetPixel( x, y );
          sum += ( ( ( p >> 16 ) & 0xff ) + ( ( p >> 8 ) & 0xff ) + ( p & 0xff ) ) / 3.0;
          ++count;
        }
      }
      return sum / count;
    }



    [TestMethod]
    public void TestFutureEnvelopeVersionLoadsAsEmptyPipeline()
    {
      var buf = new ByteBuffer();
      buf.AppendU16( 999 );                     // envelope from the future
      buf.AppendI32( 1 );

      var pipeline = new FilterPipeline();
      pipeline.Filters.Add( new ScanlineFilter() );   // must be cleared
      pipeline.LoadFromBuffer( buf );

      Assert.AreEqual( 0, pipeline.Filters.Count );
    }



    [TestMethod]
    public void TestCloneCopiesAllParameters()
    {
      // Clone() backs the dialog's Revert — a parameter missing from Clone
      // silently survives "Revert changes". Round-trip each filter through
      // Clone and compare via the serialized parameter blob, which by the
      // round-trip test above covers every persisted field.
      var pipeline = new FilterPipeline();
      pipeline.Filters.Add( new ScanlineFilter { Intensity = 61, BeamWidthMin = 33, BeamWidthMax = 141, PreserveBrightness = 42 } );
      pipeline.Filters.Add( new PhosphorMaskFilter { RBoost = 11, Pattern = 2, MaskScale = 4, PreserveLuma = 66 } );
      pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 66, Sigma = 23 } );
      pipeline.Filters.Add( new BarrelDistortionFilter { Curvature = 44, Vignette = 33, CornerRadius = 22 } );
      pipeline.Filters.Add( new PALCompositeFilter { Standard = 1, Connection = 1, LumaBandwidth = 42, ChromaBandwidth = 9, LineBlend = 77, PhaseError = 11, EdgeRise = 12, EdgeFall = 3, Saturation = 130, GammaSim = 1 } );
      pipeline.Filters.Add( new BloomFilter { Threshold = 41, BloomStrength = 42, BloomRadius = 23, HalationStrength = 44, HalationRadius = 95 } );
      pipeline.Filters.Add( new ConvergenceFilter { RShiftX = -12, RShiftY = 3, BShiftX = 12, BShiftY = -3 } );
      pipeline.Filters.Add( new PhosphorPersistenceFilter { HalfLifeMs = 333, Strength = 88 } );
      pipeline.Filters.Add( new VICLumaNoiseFilter { Strength = 63, Coarseness = 2 } );

      var clone = pipeline.Clone();

      Assert.AreEqual( pipeline.Filters.Count, clone.Filters.Count );
      for ( int i = 0; i < pipeline.Filters.Count; ++i )
      {
        var expected = pipeline.Filters[i].SaveParameters();
        var actual   = clone.Filters[i].SaveParameters();
        Assert.AreEqual( expected.ToString(), actual.ToString(),
                         "Clone of " + pipeline.Filters[i].GetType().Name + " lost a parameter" );
      }
    }
  }
}
