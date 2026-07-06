using System;
using System.Drawing;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Everything a tool needs to act on the outline canvas. Populated by
  /// MapOutlineCanvas before every tool callback; tools must not cache it
  /// across strokes (the Image reference changes when the editor swaps
  /// maps).
  /// </summary>
  public class OutlineToolContext
  {
    public Bitmap                       Image;
    /// <summary>Ink for brush/outline/text — carries alpha.</summary>
    public Color                        PrimaryColor;
    /// <summary>Shape fill — carries alpha; A=0 means no fill.</summary>
    public Color                        SecondaryColor;
    /// <summary>The canvas background — what erasing paints.</summary>
    public Color                        EraseColor;
    public float                        BrushSize;
    public float                        EraserSize;
    public float                        ShapeBorderSize;
    // Text tool styling — live from the toolbar, so an open WYSIWYG text
    // box restyles as the user changes them.
    public string                       TextFontFamily;
    public float                        TextFontSize;
    public bool                         TextFontBold;
    public bool                         TextFontItalic;
    // Tile stamp: the pre-rendered tile bitmap (owned by the editor; may
    // be null = nothing armed) and its integer magnification (1× = native
    // tile pixels, 2× doubles, ...).
    public Bitmap                       StampImage;
    public float                        StampScale;
    /// <summary>New drawing session against Image. Dispose after use.</summary>
    public Func<IOutlineRenderer>       CreateRenderer;
    /// <summary>Repaint request for an image-space region (view mapping is the canvas' job).</summary>
    public Action<Rectangle>            InvalidateImageRegion;
    /// <summary>
    /// Finished mutation: affected image rect + the region's PRE-change
    /// pixels (ownership transfers to the callee) + a human description
    /// for the undo menu ("Undo Brush stroke").
    /// </summary>
    public Action<Rectangle, Bitmap, string>  CommitChange;
  }



  /// <summary>
  /// A drawing tool of the outline canvas. Tools are registered in a list
  /// (MapEditor pairs them with their toolbar buttons) — adding a tool
  /// means adding a class + a button, not editing a switch. Pointer
  /// coordinates arrive in IMAGE space (float — drawing works at any
  /// zoom); a tool is "in flight" between OnPointerDown and
  /// OnPointerUp/Cancel and must tolerate Cancel at any time.
  /// </summary>
  public interface IOutlineTool
  {
    /// <summary>Display name (tooltips, undo descriptions).</summary>
    string Name
    {
      get;
    }

    Cursor Cursor
    {
      get;
    }

    /// <summary>
    /// True while the tool holds uncommitted in-flight state (an open
    /// text box, a drag preview, a stroke). The canvas yields chorded
    /// input (e.g. Space-to-pan) to the tool while this is set.
    /// </summary>
    bool HasPendingEdit
    {
      get;
    }

    /// <summary>
    /// IMAGE-space radius of whatever this tool paints around the bare
    /// pointer in its overlay pass (brush-size ghost, stamp ghost) — the
    /// canvas invalidates a matching region per mouse move. 0 = nothing
    /// follows the pointer.
    /// </summary>
    float PointerGhostExtent( OutlineToolContext Context );

    void OnPointerDown( OutlineToolContext Context, PointF ImagePos );

    void OnPointerMove( OutlineToolContext Context, PointF ImagePos );

    void OnPointerUp( OutlineToolContext Context, PointF ImagePos );

    /// <summary>Abort the in-flight operation, restoring the image.</summary>
    void Cancel( OutlineToolContext Context );

    /// <summary>
    /// A typed character reached the canvas (includes control chars like
    /// '\b' and '\r'). Return true when consumed — only the text tool
    /// wants these.
    /// </summary>
    bool OnKeyPress( OutlineToolContext Context, char PressedChar );

    /// <summary>
    /// Orderly switch-away (user picked another tool). Tools whose
    /// in-flight state is preview-only decide here whether to COMMIT
    /// (text keeps what was typed) or discard (drags). Forced teardowns
    /// (Escape, map swap) still go through Cancel.
    /// </summary>
    void OnDeactivate( OutlineToolContext Context );

    /// <summary>
    /// View-space overlay pass, drawn over the blitted image every paint:
    /// in-flight shape previews, brush-size ghost at the pointer, etc.
    /// PointerImagePos is the last known pointer position in image space
    /// (NaN/NaN when the pointer left the canvas).
    /// </summary>
    void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                         Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos );
  }
}
