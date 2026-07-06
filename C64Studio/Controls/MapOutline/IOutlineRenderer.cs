using System;
using System.Collections.Generic;
using System.Drawing;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Drawing backend for the map outline canvas. Tools speak ONLY this
  /// vocabulary, so a future Direct2D/DirectWrite renderer can slot in by
  /// re-implementing it (plus the snapshot helpers on OutlineToolContext,
  /// which are the other GDI+ touchpoint). One renderer instance wraps one
  /// drawing session against the backing image — create, draw, dispose.
  ///
  /// All coordinates are IMAGE space; all colors carry alpha and composite
  /// over existing pixels (SourceOver), except RestoreRegion which is a
  /// pixel-exact overwrite.
  /// </summary>
  public interface IOutlineRenderer : IDisposable
  {
    /// <summary>
    /// Pixel-exact copy of Region from Source back into the target — the
    /// undo blit and the per-move stroke restore. NO alpha blending.
    /// </summary>
    void RestoreRegion( Bitmap Source, Rectangle Region );

    /// <summary>
    /// One antialiased round-capped, round-joined polyline — the whole
    /// accumulated brush stroke in a single pass, so semi-transparent ink
    /// never double-blends at segment joints.
    /// </summary>
    void DrawStrokePath( IReadOnlyList<PointF> Points, Color Color, float Width );

    void FillRectangle( RectangleF Rect, Color Color );

    void DrawRectangleOutline( RectangleF Rect, Color Color, float BorderWidth );

    void FillEllipse( RectangleF Rect, Color Color );

    void DrawEllipseOutline( RectangleF Rect, Color Color, float BorderWidth );

    /// <summary>
    /// Antialiased text at Position (top-left). Newlines break lines.
    /// </summary>
    void DrawText( string Text, PointF Position, string FontFamily, float FontSize,
                   bool Bold, bool Italic, Color Color );

    /// <summary>Pixel bounds the same DrawText call would cover.</summary>
    SizeF MeasureText( string Text, string FontFamily, float FontSize, bool Bold, bool Italic );

    /// <summary>
    /// Nearest-neighbor image stamp — chunky pixel-art scaling for tile
    /// stamps (bilinear would smear the C64 look).
    /// </summary>
    void DrawImageNearest( Bitmap Source, RectangleF Dest );
  }
}
