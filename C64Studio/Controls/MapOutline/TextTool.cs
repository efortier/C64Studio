using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Text as PERSISTENT OBJECTS: committing the WYSIWYG box creates (or
  /// updates) an OutlineTextObject that stays selectable, editable and
  /// movable until explicitly flattened to pixels — this tool never bakes
  /// (only the editor's Flatten action does).
  ///
  /// Interaction: click empty ground opens a new edit box (type; drag inside
  /// repositions; Enter = newline); click AWAY commits the box into an
  /// object. Click an existing object to SELECT it (marquee frame); drag a
  /// selected object to MOVE it; click a selected object again (or a natural
  /// double-click) to EDIT it — its box opens seeded with its text and
  /// style, and the object itself is hidden while the box replaces it
  /// visually. Escape is staged: abort drag → discard box → deselect.
  ///
  /// States: S0 idle · S1 selected · S2 editing-new · S3 editing-existing ·
  /// S4 box-drag · S5 press-armed-on-object · S6 dragging-object. Selection
  /// itself lives on the CANVAS (SelectionRect pattern) so the editor's
  /// Delete arbitration and toolbar binding can read it.
  /// </summary>
  public class TextTool : IOutlineTool
  {
    // ---- Edit box (S2/S3/S4) ----
    private PointF?       m_EditPos = null;
    private readonly StringBuilder m_Text = new StringBuilder();
    // Caret position INSIDE the buffer (0..Length) — typing/Backspace act
    // here, and Left/Right/Up/Down/Home/End move it, so a typo mid-text is
    // fixable without erasing everything after it.
    private int           m_CaretIndex = 0;
    private PointF        m_DragOffset;
    private bool          m_IsDraggingText = false;
    // Non-null: the box is editing THIS object (S3); its style snapshot at
    // open time detects no-change commits.
    private OutlineTextObject m_EditingObject = null;

    // ---- Object press/drag (S5/S6) ----
    private OutlineTextObject m_PressedObject = null;
    private PointF        m_PressPos;
    private bool          m_IsDraggingObject = false;
    private List<OutlineTextObject> m_ObjectsBeforeDrag = null;
    // The GROUP being dragged (the whole selection at press time) and each
    // member's original position — Ctrl+click multi-selections move as one.
    private List<OutlineTextObject> m_DraggedObjects = null;
    private List<PointF>  m_DragOriginals = null;

    // ---- Right-border resize (sets WrapWidth, text reflows) ----
    private OutlineTextObject m_ResizeObject = null;
    private float         m_ResizeOriginalWrap = 0f;
    private Rectangle     m_ResizeStartBounds = Rectangle.Empty;
    private List<OutlineTextObject> m_ObjectsBeforeResize = null;

    /// <summary>Drag threshold in VIEW pixels (converted via Context.ViewZoom).</summary>
    private const float   DRAG_THRESHOLD_VIEW = 4.0f;



    public string Name
    {
      get
      {
        return "Text";
      }
    }



    public Cursor Cursor
    {
      get
      {
        return Cursors.IBeam;
      }
    }



    /// <summary>
    /// Open box, live drag, or an armed press — a mid-press Ctrl+Z would
    /// otherwise replace the object list under our feet. Mere SELECTION is
    /// deliberately NOT pending (it must not block Space-pan, shortcuts or
    /// history).
    /// </summary>
    public bool HasPendingEdit
    {
      get
      {
        return ( m_EditPos.HasValue )
            || ( m_IsDraggingObject )
            || ( m_PressedObject != null )
            || ( m_ResizeObject != null );
      }
    }



    public void OnPointerDown( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_EditPos.HasValue )
      {
        if ( EditRegion( Context ).Contains( new Point( (int)ImagePos.X, (int)ImagePos.Y ) ) )
        {
          // Reposition the open box (unchanged behavior).
          m_IsDraggingText = true;
          m_DragOffset = new PointF( ImagePos.X - m_EditPos.Value.X, ImagePos.Y - m_EditPos.Value.Y );
          return;
        }
        // Click-away: land the box, then treat the click against the ground.
        CommitBox( Context );
        var clickedAfterCommit = HitTest( Context, ImagePos );
        if ( clickedAfterCommit != null )
        {
          Context.SetSelectedTextObject( clickedAfterCommit );
          return;
        }
        Context.SetSelectedTextObject( null );
        OpenNewBox( Context, ImagePos );
        return;
      }

      bool ctrlHeld = ( ( Control.ModifierKeys & Keys.Control ) == Keys.Control );

      // Right-border RESIZE handle of a selected object: drag sets its wrap
      // width and the text reflows. Checked before the body hit-test (the
      // border zone lies inside the bounds) and never with Ctrl (selection
      // gesture).
      if ( !ctrlHeld )
      {
        var resizeTarget = HitTestResizeHandle( Context.SelectedTextObjects, ImagePos, Context.ViewZoom );
        if ( resizeTarget != null )
        {
          m_ResizeObject = resizeTarget;
          m_ResizeOriginalWrap = resizeTarget.WrapWidth;
          m_ResizeStartBounds = resizeTarget.BoundsWithMargin();
          m_ObjectsBeforeResize = OutlineTextObject.CloneList( Context.TextObjects );
          return;
        }
      }

      var hit = HitTest( Context, ImagePos );
      if ( hit != null )
      {
        if ( ctrlHeld )
        {
          // Ctrl+click toggles multi-selection membership — no drag arm, no
          // new box (editing is F2, only with a single selection).
          Context.ToggleSelectedTextObject( hit );
          return;
        }
        // Plain click: an already-selected object keeps the WHOLE selection
        // (the group drags together); an unselected one becomes the single
        // selection. Editing is F2 — a second click no longer opens the box.
        bool partOfSelection = false;
        if ( Context.SelectedTextObjects != null )
        {
          foreach ( var sel in Context.SelectedTextObjects )
          {
            if ( sel == hit )
            {
              partOfSelection = true;
              break;
            }
          }
        }
        if ( !partOfSelection )
        {
          Context.SetSelectedTextObject( hit );
        }
        m_PressedObject = hit;
        m_PressPos = ImagePos;
        return;
      }

      if ( ctrlHeld )
      {
        // Ctrl+click on empty ground: no new box (it's a selection gesture).
        return;
      }
      Context.SetSelectedTextObject( null );
      OpenNewBox( Context, ImagePos );
    }



    public void OnPointerMove( OutlineToolContext Context, PointF ImagePos )
    {
      if ( ( m_IsDraggingText )
      &&   ( m_EditPos.HasValue ) )
      {
        var previous = EditRegion( Context );
        m_EditPos = new PointF( ImagePos.X - m_DragOffset.X, ImagePos.Y - m_DragOffset.Y );
        Context.InvalidateImageRegion( Rectangle.Union( previous, EditRegion( Context ) ) );
        return;
      }
      if ( m_ResizeObject != null )
      {
        // Live reflow: the border follows the pointer (width IS the wrap
        // width; only an unbreakable overflow word exceeds it).
        var oldBounds = m_ResizeObject.BoundsWithMargin();
        float newWrap = Math.Max( OutlineTextLayout.MIN_WRAP_WIDTH,
                                  ImagePos.X - m_ResizeObject.Position.X );
        if ( newWrap != m_ResizeObject.WrapWidth )
        {
          m_ResizeObject.WrapWidth = newWrap;
          m_ResizeObject.InvalidateMeasurement();
          Context.InvalidateImageRegion( Rectangle.Union( oldBounds, m_ResizeObject.BoundsWithMargin() ) );
        }
        return;
      }
      if ( m_PressedObject == null )
      {
        return;
      }
      if ( !m_IsDraggingObject )
      {
        // Threshold in VIEW pixels so precision is zoom-independent.
        float dx = ( ImagePos.X - m_PressPos.X ) * Context.ViewZoom;
        float dy = ( ImagePos.Y - m_PressPos.Y ) * Context.ViewZoom;
        if ( dx * dx + dy * dy <= DRAG_THRESHOLD_VIEW * DRAG_THRESHOLD_VIEW )
        {
          return;
        }
        m_ObjectsBeforeDrag = OutlineTextObject.CloneList( Context.TextObjects );
        // The whole selection travels as a group: capture members + origins.
        m_DraggedObjects = new List<OutlineTextObject>();
        m_DragOriginals = new List<PointF>();
        if ( Context.SelectedTextObjects != null )
        {
          foreach ( var sel in Context.SelectedTextObjects )
          {
            m_DraggedObjects.Add( sel );
            m_DragOriginals.Add( sel.Position );
          }
        }
        m_IsDraggingObject = true;
      }
      float moveX = ImagePos.X - m_PressPos.X;
      float moveY = ImagePos.Y - m_PressPos.Y;
      var invalid = Rectangle.Empty;
      for ( int i = 0; i < m_DraggedObjects.Count; ++i )
      {
        var obj = m_DraggedObjects[i];
        var oldBounds = obj.BoundsWithMargin();
        obj.Position = new PointF( m_DragOriginals[i].X + moveX, m_DragOriginals[i].Y + moveY );
        var bothBounds = Rectangle.Union( oldBounds, obj.BoundsWithMargin() );
        invalid = invalid.IsEmpty ? bothBounds : Rectangle.Union( invalid, bothBounds );
      }
      if ( !invalid.IsEmpty )
      {
        Context.InvalidateImageRegion( invalid );
      }
    }



    public void OnPointerUp( OutlineToolContext Context, PointF ImagePos )
    {
      if ( m_IsDraggingText )
      {
        m_IsDraggingText = false;
        return;
      }
      if ( m_ResizeObject != null )
      {
        var resized = m_ResizeObject;
        var startBounds = m_ResizeStartBounds;
        float originalWrap = m_ResizeOriginalWrap;
        var beforeResize = m_ObjectsBeforeResize;
        DisarmResize();
        if ( resized.WrapWidth != originalWrap )
        {
          CommitObjects( Context,
            Rectangle.Union( startBounds, resized.BoundsWithMargin() ),
            beforeResize, "Resize text" );
        }
        return;
      }
      if ( m_PressedObject == null )
      {
        return;
      }
      bool dragged = m_IsDraggingObject;
      bool anyMoved = false;
      var invalid = Rectangle.Empty;
      if ( ( dragged )
      &&   ( m_DraggedObjects != null ) )
      {
        for ( int i = 0; i < m_DraggedObjects.Count; ++i )
        {
          if ( m_DraggedObjects[i].Position != m_DragOriginals[i] )
          {
            anyMoved = true;
          }
          var obj = m_DraggedObjects[i];
          var bothBounds = Rectangle.Union(
            RectangleAt( obj, m_DragOriginals[i], Context ),
            obj.BoundsWithMargin() );
          invalid = invalid.IsEmpty ? bothBounds : Rectangle.Union( invalid, bothBounds );
        }
      }
      var beforeDrag = m_ObjectsBeforeDrag;
      DisarmPress();

      if ( ( dragged )
      &&   ( anyMoved ) )
      {
        CommitObjects( Context, invalid, beforeDrag, "Move text" );
      }
      // A clean release without a drag just leaves the selection standing —
      // editing is F2 (with exactly one object selected).
    }



    public void Cancel( OutlineToolContext Context )
    {
      // Stage 1: an in-flight drag aborts alone.
      if ( m_IsDraggingText )
      {
        m_IsDraggingText = false;
        return;
      }
      if ( m_ResizeObject != null )
      {
        RestoreResizedWidth( Context );
        DisarmResize();
        return;
      }
      if ( ( m_IsDraggingObject )
      ||   ( m_PressedObject != null ) )
      {
        RestoreDraggedPositions( Context );
        DisarmPress();
        return;
      }
      // Stage 2: an open box DISCARDS here — this path is reached via
      // Ctrl+Z-with-pending-edit ("undo the uncommitted typing") and forced
      // teardowns. The editor's ESCAPE key routes to CommitOpenBox instead
      // (exit edit mode, keep the edit; undo reverts it — user choice).
      if ( m_EditPos.HasValue )
      {
        var region = EditRegion( Context );
        CloseBox( Context );
        Context.InvalidateImageRegion( region );
        return;
      }
      // Stage 3: plain selection clears.
      Context.SetSelectedTextObject( null );
    }



    public bool OnKeyPress( OutlineToolContext Context, char PressedChar )
    {
      if ( !m_EditPos.HasValue )
      {
        return false;
      }
      var previous = EditRegion( Context );
      m_CaretIndex = Math.Max( 0, Math.Min( m_Text.Length, m_CaretIndex ) );
      if ( PressedChar == '\b' )
      {
        if ( m_CaretIndex > 0 )
        {
          m_Text.Remove( m_CaretIndex - 1, 1 );
          --m_CaretIndex;
        }
      }
      else if ( ( PressedChar == '\r' )
      ||        ( PressedChar == '\n' ) )
      {
        m_Text.Insert( m_CaretIndex, '\n' );
        ++m_CaretIndex;
      }
      else if ( !char.IsControl( PressedChar ) )
      {
        m_Text.Insert( m_CaretIndex, PressedChar );
        ++m_CaretIndex;
      }
      else
      {
        return false;
      }
      Context.InvalidateImageRegion( Rectangle.Union( previous, EditRegion( Context ) ) );
      return true;
    }



    /// <summary>
    /// Caret navigation / forward-delete inside the open edit box — arrows
    /// and Home/End/Delete never arrive as KeyPress characters, so the
    /// canvas (and the editor's Delete branch) forward them here. False =
    /// no box open or key not ours.
    /// </summary>
    public bool HandleNavigationKey( OutlineToolContext Context, Keys Key )
    {
      if ( !m_EditPos.HasValue )
      {
        return false;
      }
      string text = m_Text.ToString();
      int caret = Math.Max( 0, Math.Min( text.Length, m_CaretIndex ) );
      // VISUAL lines from the layout — with word-wrap on, Up/Down/Home/End
      // must follow what the user SEES, not the '\n' structure.
      var layout = BuildBoxLayout( Context );
      int caretLine;
      float caretX;
      OutlineTextLayout.CaretPosition( layout, caret, out caretLine, out caretX );

      switch ( Key )
      {
        case Keys.Left:
          caret = Math.Max( 0, caret - 1 );
          break;
        case Keys.Right:
          caret = Math.Min( text.Length, caret + 1 );
          break;
        case Keys.Home:
          caret = layout.Lines[caretLine].Start;
          break;
        case Keys.End:
          caret = layout.Lines[caretLine].Start + layout.Lines[caretLine].Length;
          break;
        case Keys.Up:
          if ( caretLine > 0 )
          {
            caret = OutlineTextLayout.IndexAt( layout, caretLine - 1, caretX );
          }
          break;
        case Keys.Down:
          if ( caretLine < layout.Lines.Count - 1 )
          {
            caret = OutlineTextLayout.IndexAt( layout, caretLine + 1, caretX );
          }
          break;
        case Keys.Delete:
          if ( caret < m_Text.Length )
          {
            var previous = EditRegion( Context );
            m_Text.Remove( caret, 1 );
            Context.InvalidateImageRegion( Rectangle.Union( previous, EditRegion( Context ) ) );
          }
          m_CaretIndex = caret;
          return true;
        default:
          return false;
      }

      if ( caret != m_CaretIndex )
      {
        m_CaretIndex = caret;
        // Only the caret moved — repaint the box so it shows at its new spot.
        Context.InvalidateImageRegion( EditRegion( Context ) );
      }
      return true;
    }



    public void OnDeactivate( OutlineToolContext Context )
    {
      // Switching tools keeps what was typed — commit the box into its
      // object. An armed press/drag/resize discards (the standard drag
      // contract); the canvas clears the selection itself right after.
      RestoreResizedWidth( Context );
      DisarmResize();
      RestoreDraggedPositions( Context );
      DisarmPress();
      m_IsDraggingText = false;
      if ( m_EditPos.HasValue )
      {
        CommitBox( Context );
      }
    }



    public float PointerGhostExtent( OutlineToolContext Context )
    {
      return 0;
    }



    // ---- Box lifecycle --------------------------------------------------

    private void OpenNewBox( OutlineToolContext Context, PointF ImagePos )
    {
      m_EditPos = ImagePos;
      m_Text.Clear();
      m_CaretIndex = 0;
      m_EditingObject = null;
      Context.InvalidateImageRegion( EditRegion( Context ) );
    }



    private void OpenBoxForObject( OutlineToolContext Context, OutlineTextObject Obj )
    {
      m_EditPos = Obj.Position;
      m_Text.Clear();
      m_Text.Append( Obj.Text ?? "" );
      m_CaretIndex = m_Text.Length;
      m_EditingObject = Obj;
      // Hide the object while its editor replaces it visually (the canvas'
      // render pass skips the editing object — no double draw).
      Context.SetEditingTextObject( Obj );
      Context.InvalidateImageRegion(
        Rectangle.Union( Obj.BoundsWithMargin(), EditRegion( Context ) ) );
    }



    /// <summary>
    /// Lands the open box: editing-new creates an object (non-empty text);
    /// editing-existing writes text/position/current style back into its
    /// object (emptied text deletes it; a no-change commit pushes nothing).
    /// This NEVER touches pixels — only the editor's Flatten action bakes.
    /// </summary>
    private void CommitBox( OutlineToolContext Context )
    {
      if ( !m_EditPos.HasValue )
      {
        return;
      }
      var editPos = m_EditPos.Value;
      string text = m_Text.ToString();
      var boxRegion = EditRegion( Context );
      var editedObject = m_EditingObject;
      // The width the box's lines broke at — FROZEN into the committed object
      // so it keeps these exact breaks until the next edit (captured before
      // CloseBox clears the box position it derives from).
      float frozenBreak = BoxBreakWidth( Context );
      CloseBox( Context );

      if ( editedObject != null )
      {
        var before = OutlineTextObject.CloneList( Context.TextObjects );
        var oldBounds = editedObject.BoundsWithMargin();

        if ( text.Trim().Length == 0 )
        {
          // Emptied out = delete the object.
          Context.TextObjects.Remove( editedObject );
          Context.SetSelectedTextObject( null );
          Context.InvalidateImageRegion( Rectangle.Union( oldBounds, boxRegion ) );
          CommitObjects( Context, Rectangle.Union( oldBounds, boxRegion ), before, "Delete text" );
          return;
        }

        bool changed = ( editedObject.Text != text )
                    || ( editedObject.Position != editPos )
                    || ( editedObject.FontFamily != Context.TextFontFamily )
                    || ( editedObject.FontSize != Context.TextFontSize )
                    || ( editedObject.Bold != Context.TextFontBold )
                    || ( editedObject.Italic != Context.TextFontItalic )
                    || ( editedObject.CharSpacing != Context.TextCharSpacing )
                    || ( editedObject.LineSpacing != Context.TextLineSpacing )
                    || ( editedObject.Color.ToArgb() != Context.PrimaryColor.ToArgb() );
        if ( !changed )
        {
          Context.InvalidateImageRegion( Rectangle.Union( oldBounds, boxRegion ) );
          return;
        }
        editedObject.Text        = text;
        editedObject.Position    = editPos;   // the box may have been dragged
        editedObject.FontFamily  = Context.TextFontFamily;
        editedObject.FontSize    = Context.TextFontSize;
        editedObject.Bold        = Context.TextFontBold;
        editedObject.Italic      = Context.TextFontItalic;
        editedObject.CharSpacing = Context.TextCharSpacing;
        editedObject.LineSpacing = Context.TextLineSpacing;
        editedObject.Color       = Context.PrimaryColor;
        // WrapWidth deliberately untouched — the box edits content/style;
        // explicit width belongs to the border-drag gesture. Freeze the width
        // the box's lines actually broke at (auto-wrapped objects only:
        // BoxBreakWidth returned the explicit width when one is set).
        if ( editedObject.WrapWidth <= 0f )
        {
          editedObject.AutoBreakWidth = frozenBreak;
        }
        editedObject.InvalidateMeasurement();
        var newBounds = editedObject.BoundsWithMargin();
        var union = Rectangle.Union( Rectangle.Union( oldBounds, newBounds ), boxRegion );
        Context.InvalidateImageRegion( union );
        CommitObjects( Context, union, before, "Edit text" );
        return;
      }

      // Editing-new: whitespace-only never creates an object.
      if ( text.Trim().Length == 0 )
      {
        Context.InvalidateImageRegion( boxRegion );
        return;
      }
      var beforeAdd = OutlineTextObject.CloneList( Context.TextObjects );
      var created = new OutlineTextObject()
      {
        Position      = editPos,
        Text          = text,
        FontFamily    = Context.TextFontFamily,
        FontSize      = Context.TextFontSize,
        Bold          = Context.TextFontBold,
        Italic        = Context.TextFontItalic,
        CharSpacing   = Context.TextCharSpacing,
        LineSpacing   = Context.TextLineSpacing,
        Color         = Context.PrimaryColor,
        // Freeze the width the box's lines broke at — the committed object
        // keeps these exact breaks (zero-jump WYSIWYG match) until re-edited.
        AutoBreakWidth = frozenBreak
      };
      Context.TextObjects.Add( created );    // appended = topmost
      var createdBounds = Rectangle.Union( created.BoundsWithMargin(), boxRegion );
      Context.InvalidateImageRegion( createdBounds );
      CommitObjects( Context, createdBounds, beforeAdd, "Text" );
    }



    private void CloseBox( OutlineToolContext Context )
    {
      m_EditPos = null;
      m_Text.Clear();
      m_CaretIndex = 0;
      m_IsDraggingText = false;
      if ( m_EditingObject != null )
      {
        m_EditingObject = null;
        Context.SetEditingTextObject( null );
      }
    }



    private void DisarmPress()
    {
      m_PressedObject = null;
      m_IsDraggingObject = false;
      m_ObjectsBeforeDrag = null;
      m_DraggedObjects = null;
      m_DragOriginals = null;
    }



    private void DisarmResize()
    {
      m_ResizeObject = null;
      m_ResizeOriginalWrap = 0f;
      m_ResizeStartBounds = Rectangle.Empty;
      m_ObjectsBeforeResize = null;
    }



    /// <summary>Puts a resized object's wrap width back (cancel/deactivate).</summary>
    private void RestoreResizedWidth( OutlineToolContext Context )
    {
      if ( ( m_ResizeObject == null )
      ||   ( m_ResizeObject.WrapWidth == m_ResizeOriginalWrap ) )
      {
        return;
      }
      var oldBounds = m_ResizeObject.BoundsWithMargin();
      m_ResizeObject.WrapWidth = m_ResizeOriginalWrap;
      m_ResizeObject.InvalidateMeasurement();
      Context.InvalidateImageRegion( Rectangle.Union( oldBounds, m_ResizeObject.BoundsWithMargin() ) );
    }



    /// <summary>Puts every group-dragged object back where it started (cancel/deactivate).</summary>
    private void RestoreDraggedPositions( OutlineToolContext Context )
    {
      if ( ( !m_IsDraggingObject )
      ||   ( m_DraggedObjects == null ) )
      {
        return;
      }
      var invalid = Rectangle.Empty;
      for ( int i = 0; i < m_DraggedObjects.Count; ++i )
      {
        var obj = m_DraggedObjects[i];
        var oldBounds = obj.BoundsWithMargin();
        obj.Position = m_DragOriginals[i];
        var bothBounds = Rectangle.Union( oldBounds, obj.BoundsWithMargin() );
        invalid = invalid.IsEmpty ? bothBounds : Rectangle.Union( invalid, bothBounds );
      }
      if ( !invalid.IsEmpty )
      {
        Context.InvalidateImageRegion( invalid );
      }
    }



    /// <summary>True while the WYSIWYG edit box is open.</summary>
    public bool HasOpenEditBox
    {
      get
      {
        return m_EditPos.HasValue;
      }
    }



    /// <summary>True while a right-border resize drag is in flight.</summary>
    public bool IsResizingTextObject
    {
      get
      {
        return m_ResizeObject != null;
      }
    }



    /// <summary>
    /// The selected object whose right-border resize zone contains ImagePos
    /// (topmost first); null = none. SHARED by the pointer-down gesture and
    /// the canvas' hover cursor, so the grab zone and the SizeWE feedback can
    /// never drift apart.
    /// </summary>
    public static OutlineTextObject HitTestResizeHandle( IReadOnlyList<OutlineTextObject> Selected,
                                                         PointF ImagePos, float ViewZoom )
    {
      if ( Selected == null )
      {
        return null;
      }
      float zone = Math.Max( 3f, 5f / Math.Max( 0.1f, ViewZoom ) );
      for ( int i = Selected.Count - 1; i >= 0; --i )
      {
        var bounds = Selected[i].BoundsWithMargin();
        if ( ( Math.Abs( ImagePos.X - bounds.Right ) <= zone )
        &&   ( ImagePos.Y >= bounds.Top )
        &&   ( ImagePos.Y <= bounds.Bottom ) )
        {
          return Selected[i];
        }
      }
      return null;
    }



    /// <summary>
    /// Escape's exit-edit-mode path (via the canvas): COMMITS the box and
    /// closes it — the user reverts with undo if unwanted. Cancel() keeps
    /// its discard semantics for Ctrl+Z-with-pending and forced teardowns.
    /// </summary>
    public void CommitOpenBox( OutlineToolContext Context )
    {
      CommitBox( Context );
    }



    /// <summary>
    /// F2 entry point (via the canvas): opens the editor box on the given
    /// object — same seeding as the old click-to-edit path.
    /// </summary>
    public void OpenEditorOnObject( OutlineToolContext Context, OutlineTextObject Obj )
    {
      if ( ( m_EditPos.HasValue )
      ||   ( Obj == null ) )
      {
        return;
      }
      OpenBoxForObject( Context, Obj );
    }



    // ---- Helpers ---------------------------------------------------------

    /// <summary>Topmost-first hit test (list order = z-order, last = topmost).</summary>
    private static OutlineTextObject HitTest( OutlineToolContext Context, PointF ImagePos )
    {
      if ( Context.TextObjects == null )
      {
        return null;
      }
      var point = new Point( (int)ImagePos.X, (int)ImagePos.Y );
      for ( int i = Context.TextObjects.Count - 1; i >= 0; --i )
      {
        if ( Context.TextObjects[i].BoundsWithMargin().Contains( point ) )
        {
          return Context.TextObjects[i];
        }
      }
      return null;
    }



    /// <summary>The object's bounds as if anchored at Pos (for move-undo regions).</summary>
    private static Rectangle RectangleAt( OutlineTextObject Obj, PointF Pos, OutlineToolContext Context )
    {
      var bounds = Obj.BoundsWithMargin();
      return new Rectangle(
        bounds.X + (int)( Pos.X - Obj.Position.X ),
        bounds.Y + (int)( Pos.Y - Obj.Position.Y ),
        bounds.Width, bounds.Height );
    }



    /// <summary>Object-commit through the canvas pipeline (region ∩ image).</summary>
    private static void CommitObjects( OutlineToolContext Context, Rectangle Region,
                                       List<OutlineTextObject> Before, string Description )
    {
      var clamped = Rectangle.Intersect( Region,
        new Rectangle( 0, 0, Context.Image.Width, Context.Image.Height ) );
      Context.CommitTextObjectsChange( clamped, Before, Description );
    }



    public void OnPaintPreview( OutlineToolContext Context, Graphics ViewGraphics,
                                Func<PointF, PointF> ImageToView, float ViewZoom, PointF PointerImagePos )
    {
      if ( !m_EditPos.HasValue )
      {
        return;
      }
      string text = m_Text.ToString();
      var originView = ImageToView( m_EditPos.Value );

      // ONE image-space layout drives everything — frame, glyphs and caret —
      // so the box is WYSIWYG (wrap + spacing) and committing it into an
      // object causes zero visual jump.
      var layout = BuildBoxLayout( Context );
      var sizeImage = OutlineTextLayout.Measure( layout, BoxWrapWidth(), Context.TextLineSpacing );

      var previousHint = ViewGraphics.TextRenderingHint;
      ViewGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

      // Dashed frame marks the live box (layout-exact size, scaled).
      using ( var framePen = new Pen( Color.FromArgb( 180, 255, 255, 255 ) ) )
      {
        framePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        ViewGraphics.DrawRectangle( framePen, originView.X - 2, originView.Y - 2,
                                    sizeImage.Width * ViewZoom + 4, sizeImage.Height * ViewZoom + 4 );
      }

      float viewFontSize = Math.Max( 1.0f, Context.TextFontSize * ViewZoom );
      using ( var font = GdiPlusOutlineRenderer.CreateFont( Context.TextFontFamily, viewFontSize,
                                                            Context.TextFontBold, Context.TextFontItalic ) )
      {
        if ( text.Length > 0 )
        {
          OutlineTextLayout.Draw( ViewGraphics, layout, text, font, originView,
                                  ViewZoom, Context.TextLineSpacing, Context.PrimaryColor );
        }

        // Steady caret at its BUFFER position — geometry straight from the
        // layout (spaces, wrap and spacing all exact for any font).
        int caret = Math.Max( 0, Math.Min( text.Length, m_CaretIndex ) );
        int caretLine;
        float caretXImage;
        OutlineTextLayout.CaretPosition( layout, caret, out caretLine, out caretXImage );
        float caretX = originView.X + caretXImage * ViewZoom;
        float caretY = originView.Y + caretLine * layout.LineAdvance( Context.TextLineSpacing ) * ViewZoom;
        float caretHeight = layout.LineHeight * ViewZoom;
        using ( var caretPen = new Pen( Context.PrimaryColor.A > 0
          ? Color.FromArgb( 255, Context.PrimaryColor ) : Color.White, Math.Max( 1.0f, ViewZoom ) ) )
        {
          ViewGraphics.DrawLine( caretPen, caretX, caretY + 2, caretX, caretY + caretHeight - 2 );
        }
      }
      ViewGraphics.TextRenderingHint = previousHint;
    }



    /// <summary>
    /// The FRAME width the open box measures with: an edited object keeps ITS
    /// explicit width (WYSIWYG); a fresh/auto box has none (the frame hugs the
    /// text — auto-wrap breaks lines but never fattens the box). See
    /// BoxBreakWidth for where lines actually wrap.
    /// </summary>
    private float BoxWrapWidth()
    {
      return ( m_EditingObject != null ) ? m_EditingObject.WrapWidth : 0f;
    }



    /// <summary>
    /// The width the box's LINES break at: an explicit object width wins;
    /// otherwise the distance from the box anchor to the canvas right edge, so
    /// typed text wraps within the picture instead of running off it. Matches
    /// the committed object's EffectiveBreakWidth exactly (zero-jump commit).
    /// </summary>
    private float BoxBreakWidth( OutlineToolContext Context )
    {
      if ( ( m_EditingObject != null )
      &&   ( m_EditingObject.WrapWidth > 0f ) )
      {
        return m_EditingObject.WrapWidth;
      }
      if ( ( Context.Image != null )
      &&   ( m_EditPos.HasValue ) )
      {
        float available = Context.Image.Width - m_EditPos.Value.X;
        return ( available > 0f ) ? available : 0f;
      }
      return 0f;
    }



    /// <summary>
    /// Image-space layout of the buffer with the CONTEXT's live style — the
    /// exact engine the committed object will render with, so the box is
    /// WYSIWYG including wrap and spacing. Buffers are short; laying out per
    /// paint/keystroke is cheap.
    /// </summary>
    private OutlineTextLayoutData BuildBoxLayout( OutlineToolContext Context )
    {
      using ( var font = GdiPlusOutlineRenderer.CreateFont( Context.TextFontFamily, Context.TextFontSize,
                                                            Context.TextFontBold, Context.TextFontItalic ) )
      {
        string text = ( m_Text.Length > 0 ) ? m_Text.ToString() : " ";
        // Lines break at the canvas edge (or the object's explicit width); the
        // frame still hugs (Measure uses BoxWrapWidth).
        return OutlineTextLayout.LayoutWithTemporaryGraphics( font, text, BoxBreakWidth( Context ), Context.TextCharSpacing );
      }
    }



    /// <summary>
    /// Image-space bounds of the current edit box (+AA margin). Empty when
    /// idle. Layout-exact — identical numbers to the committed object.
    /// </summary>
    private Rectangle EditRegion( OutlineToolContext Context )
    {
      if ( !m_EditPos.HasValue )
      {
        return Rectangle.Empty;
      }
      var size = OutlineTextLayout.Measure( BuildBoxLayout( Context ), BoxWrapWidth(), Context.TextLineSpacing );
      return Rectangle.FromLTRB(
        (int)Math.Floor( m_EditPos.Value.X ) - 4, (int)Math.Floor( m_EditPos.Value.Y ) - 4,
        (int)Math.Ceiling( m_EditPos.Value.X + size.Width ) + 4,
        (int)Math.Ceiling( m_EditPos.Value.Y + size.Height ) + 4 );
    }
  }
}
