using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Incremental variable-width stroke compositor for PEN (pressure) strokes.
  /// Mouse strokes deliberately stay on the old whole-path renderer
  /// (GdiPlusOutlineRenderer.DrawStrokePath) so their output is pixel-identical
  /// to before; this class is used only when a stroke carries per-point
  /// pressure, where re-rendering the whole path each move would "drag".
  ///
  /// Per move it rasterizes ONLY the new capsule (last→current point) and
  /// max-composites its coverage into a single-channel buffer, then recomposes
  /// the backing bitmap over just the capsule's bounding box. Cost is bounded
  /// by the brush footprint, not the stroke length. Max-of-coverage (rather
  /// than source-over stamping) is what keeps a semi-transparent stroke from
  /// double-blending where consecutive dabs overlap — GDI+ has no max-alpha
  /// compositing mode, so the max is done per-pixel here.
  ///
  /// The ink COLOUR is constant for the stroke; only per-point width and
  /// effective alpha vary. The backing canvas is opaque (fresh black), so the
  /// composite is a straight lerp( snapshot, ink, coverage ) with A = 255.
  /// The core math lives in <see cref="CompositeCapsulePixels"/> so it can be
  /// unit-tested without a live tablet.
  /// </summary>
  public sealed class PressureStroke : IDisposable
  {
    private Bitmap    m_Image;      // backing bitmap (destination) — owned by the editor
    private Bitmap    m_Snapshot;   // pre-stroke clone — owned by the CALLER, never disposed here
    private byte[]    m_Coverage;   // W*H effective ink alpha 0..255, max-accumulated
    private int       m_W, m_H;
    private int       m_InkR, m_InkG, m_InkB;
    private bool      m_HasLast;
    private PointF    m_Last;
    private Rectangle m_Union;      // union of all invalidated rects (image space)



    /// <summary>The union of every capsule bbox drawn so far (image space).</summary>
    public Rectangle UnionDirty
    {
      get
      {
        return m_Union;
      }
    }



    /// <summary>
    /// Begins a stroke against <paramref name="image"/>. <paramref name="snapshot"/>
    /// is the pre-stroke pixels (owned by the caller) used both to recompose over
    /// and to restore on cancel. Both must be 32bpp ARGB and the same size.
    /// </summary>
    public void Begin( Bitmap image, Bitmap snapshot, Color ink )
    {
      m_Image = image;
      m_Snapshot = snapshot;
      m_W = image.Width;
      m_H = image.Height;
      m_Coverage = new byte[m_W * m_H];
      m_InkR = ink.R;
      m_InkG = ink.G;
      m_InkB = ink.B;
      m_HasLast = false;
      m_Union = Rectangle.Empty;
    }



    /// <summary>
    /// Adds a point: draws a round-capped capsule from the previous point (or a
    /// dot for the first point) of the given width, contributing effective alpha
    /// <paramref name="alpha01"/> (0..1). Returns the invalidated image rect
    /// (Empty if fully off-image).
    /// </summary>
    public Rectangle AddPoint( PointF pt, float width, float alpha01 )
    {
      float w = Math.Max( 0.1f, width );
      PointF from = m_HasLast ? m_Last : pt;
      m_HasLast = true;
      m_Last = pt;

      int margin = (int)Math.Ceiling( w * 0.5f ) + 2;
      var bbox = Rectangle.FromLTRB(
        (int)Math.Floor( Math.Min( from.X, pt.X ) ) - margin,
        (int)Math.Floor( Math.Min( from.Y, pt.Y ) ) - margin,
        (int)Math.Ceiling( Math.Max( from.X, pt.X ) ) + margin,
        (int)Math.Ceiling( Math.Max( from.Y, pt.Y ) ) + margin );
      bbox = Rectangle.Intersect( bbox, new Rectangle( 0, 0, m_W, m_H ) );
      if ( ( bbox.Width < 1 )
      ||   ( bbox.Height < 1 ) )
      {
        return Rectangle.Empty;
      }

      // 1. Rasterize the new capsule's antialiased coverage into a temp tile.
      using ( var tile = new Bitmap( bbox.Width, bbox.Height, PixelFormat.Format32bppArgb ) )
      {
        using ( var g = Graphics.FromImage( tile ) )
        using ( var pen = new Pen( Color.White, w ) )
        {
          g.Clear( Color.Transparent );
          g.SmoothingMode = SmoothingMode.AntiAlias;
          pen.StartCap = LineCap.Round;
          pen.EndCap = LineCap.Round;
          pen.LineJoin = LineJoin.Round;
          float fx = from.X - bbox.X, fy = from.Y - bbox.Y;
          float tx = pt.X - bbox.X, ty = pt.Y - bbox.Y;
          if ( ( Math.Abs( tx - fx ) < 0.01f )
          &&   ( Math.Abs( ty - fy ) < 0.01f ) )
          {
            // Zero-length segment with round caps renders the brush dot.
            g.DrawLine( pen, fx, fy, fx + 0.01f, fy );
          }
          else
          {
            g.DrawLine( pen, fx, fy, tx, ty );
          }
        }

        // 2. Max-composite into the coverage buffer and recompose the bbox.
        CompositeTile( tile, bbox, alpha01 );
      }

      m_Union = m_Union.IsEmpty ? bbox : Rectangle.Union( m_Union, bbox );
      return bbox;
    }



    private void CompositeTile( Bitmap tile, Rectangle bbox, float alpha01 )
    {
      var tileRect = new Rectangle( 0, 0, bbox.Width, bbox.Height );
      // Acquire the three locks INSIDE the try so a throw on the 2nd/3rd never
      // strands an earlier lock (a leaked GDI+ lock would break the bitmap for
      // the rest of the session).
      BitmapData tData = null, sData = null, dData = null;
      try
      {
        tData = tile.LockBits( tileRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
        sData = m_Snapshot.LockBits( bbox, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
        dData = m_Image.LockBits( bbox, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
        int cols = bbox.Width;
        var tRow = new byte[tData.Stride];
        var sRow = new byte[sData.Stride];
        var dRow = new byte[dData.Stride];
        for ( int y = 0; y < bbox.Height; ++y )
        {
          Marshal.Copy( IntPtr.Add( tData.Scan0, y * tData.Stride ), tRow, 0, cols * 4 );
          Marshal.Copy( IntPtr.Add( sData.Scan0, y * sData.Stride ), sRow, 0, cols * 4 );
          int covRowBase = ( bbox.Y + y ) * m_W + bbox.X;
          CompositeCapsulePixels( tRow, sRow, dRow, cols, m_Coverage, covRowBase,
                                  alpha01, m_InkR, m_InkG, m_InkB );
          Marshal.Copy( dRow, 0, IntPtr.Add( dData.Scan0, y * dData.Stride ), cols * 4 );
        }
      }
      finally
      {
        if ( tData != null ) { tile.UnlockBits( tData ); }
        if ( sData != null ) { m_Snapshot.UnlockBits( sData ); }
        if ( dData != null ) { m_Image.UnlockBits( dData ); }
      }
    }



    /// <summary>
    /// Core per-row compositing (unit-testable, no GDI). For each pixel:
    /// coverage = max( coverage, tileAlpha × capsuleAlpha ), then
    /// dest = lerp( snapshot, ink, coverage/255 ), A = 255. Buffers are 32bpp
    /// little-endian BGRA rows (as GDI+ Format32bppArgb LockBits yields).
    /// </summary>
    public static void CompositeCapsulePixels(
      byte[] TileBgra, byte[] SnapshotBgra, byte[] DestBgra, int Cols,
      byte[] Coverage, int CovRowBase, float CapsuleAlpha01,
      int InkR, int InkG, int InkB )
    {
      float aMul = Math.Max( 0f, Math.Min( 1f, CapsuleAlpha01 ) );
      for ( int x = 0; x < Cols; ++x )
      {
        int bi = x * 4;
        int tileA = TileBgra[bi + 3];
        int newCov = (int)( tileA * aMul );
        int covIdx = CovRowBase + x;
        int cov = Coverage[covIdx];
        if ( newCov > cov )
        {
          cov = newCov;
          Coverage[covIdx] = (byte)cov;
        }
        int sB = SnapshotBgra[bi], sG = SnapshotBgra[bi + 1], sR = SnapshotBgra[bi + 2];
        int inv = 255 - cov;
        DestBgra[bi]     = (byte)( ( sB * inv + InkB * cov ) / 255 );
        DestBgra[bi + 1] = (byte)( ( sG * inv + InkG * cov ) / 255 );
        DestBgra[bi + 2] = (byte)( ( sR * inv + InkR * cov ) / 255 );
        DestBgra[bi + 3] = 255;
      }
    }



    /// <summary>Restores the backing bitmap from the snapshot over everything drawn.</summary>
    public void Cancel()
    {
      if ( ( m_Image == null )
      ||   ( m_Snapshot == null )
      ||   ( m_Union.IsEmpty ) )
      {
        return;
      }
      var region = Rectangle.Intersect( m_Union, new Rectangle( 0, 0, m_W, m_H ) );
      if ( ( region.Width <= 0 )
      ||   ( region.Height <= 0 ) )
      {
        return;
      }
      using ( var g = Graphics.FromImage( m_Image ) )
      {
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImage( m_Snapshot, region, region, GraphicsUnit.Pixel );
      }
    }



    public void Dispose()
    {
      // The snapshot is the caller's; do not dispose it here.
      m_Coverage = null;
      m_Image = null;
      m_Snapshot = null;
    }
  }
}
