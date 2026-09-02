using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Self-contained undo/redo for one map's outline image — deliberately
  /// NOT an UndoManager/UndoTask: the outline's history must never mix
  /// with the map document's. (Dirty-marking is the EDITOR's policy, applied
  /// in its commit/apply handlers — since 2026-07-10 outline edits DO mark
  /// the document modified; this stack stays UI-free either way.) The map
  /// editor keeps one instance per Map object for the whole session and
  /// routes Undo/Redo here while outline mode is active.
  ///
  /// An entry is a dirty-rect pixel snapshot pair (before/after crops).
  /// Memory is bounded GLOBALLY across all stacks: past the shared byte
  /// budget the globally-oldest entries are evicted — but every stack
  /// keeps at least MIN_KEPT_ENTRIES so a briefly-visited map can't wipe
  /// another map's recent history.
  ///
  /// Growth note (Phase 3): size-changing operations (extend canvas,
  /// paste-replace) snapshot the WHOLE image on both sides — same entry
  /// shape, Region = full bounds of the respective bitmap, plus the
  /// bitmap-swap handled by the editor callback.
  /// </summary>
  public class OutlineUndoStack
  {
    private class Entry
    {
      public Rectangle    Region;
      public Bitmap       Before;
      public Bitmap       After;
      // An image-replace entry describes a SIZE-CHANGING operation (extend
      // canvas, paste-replace, delete picture): Before/After hold the two
      // complete images and applying hands the editor a clone to swap in,
      // instead of blitting a region in place.
      public bool         IsImageReplace;
      // Text-object payload, attachable to ANY entry kind. BOTH null = the
      // entry doesn't touch objects; BOTH non-null (possibly empty) =
      // applying replaces the whole live list with the respective snapshot.
      // Deep copies, owned by the stack. Whole-list snapshots by design:
      // objects are tiny, and one apply path covers create/edit/move/delete/
      // restyle/flatten/crop/extend/paste-replace alike.
      public List<OutlineTextObject> ObjectsBefore;
      public List<OutlineTextObject> ObjectsAfter;
      // Selection-rect transition, for operations that move the committed
      // raster selection with the pixels (move/center). SelectionBefore
      // having a value is the MARKER that this entry carries a selection
      // change — a raster move always has a non-null source, and no other
      // op transitions the selection, so no separate flag is needed. Undo
      // restores SelectionBefore, redo SelectionAfter (either may be null =
      // "cleared", e.g. a patch dragged fully off-canvas).
      public Rectangle?   SelectionBefore;
      public Rectangle?   SelectionAfter;
      public string       Description;
      public long         Bytes;
      public long         Sequence;
    }



    /// <summary>
    /// Result of applying one undo/redo step. Region is the in-place dirty
    /// rect; when ReplacementImage is non-null the operation changed the
    /// image (usually its size) as a whole — the caller takes ownership of
    /// the clone and must swap it in for its current bitmap. When
    /// ObjectsChanged is set, Objects (a DEEP COPY — caller takes ownership,
    /// never aliases the stack's snapshot) replaces the live text-object
    /// list wholesale; ImageChanged tells the caller whether any pixels
    /// changed at all (an object-only step must not dirty the bitmap).
    /// </summary>
    public class OutlineUndoResult
    {
      public Rectangle    Region;
      public Bitmap       ReplacementImage;
      public bool         ImageChanged;
      public bool         ObjectsChanged;
      public List<OutlineTextObject> Objects;
      // When set, the caller must re-home the committed selection rect on
      // Selection (which may be null = clear it). Only move/center entries
      // set this; every other undo/redo leaves the selection untouched.
      public bool         SelectionChanged;
      public Rectangle?   Selection;
    }



    /// <summary>Shared budget for all outline undo memory (~96 MB).</summary>
    private const long    TOTAL_BYTE_BUDGET = 96L * 1024 * 1024;
    /// <summary>Entries each stack keeps even when over budget.</summary>
    private const int     MIN_KEPT_ENTRIES = 10;

    private static long   s_TotalBytes = 0;
    private static long   s_NextSequence = 0;
    private static readonly List<WeakReference<OutlineUndoStack>> s_AllStacks = new List<WeakReference<OutlineUndoStack>>();

    private readonly List<Entry>    m_UndoEntries = new List<Entry>();
    private readonly List<Entry>    m_RedoEntries = new List<Entry>();



    public OutlineUndoStack()
    {
      s_AllStacks.Add( new WeakReference<OutlineUndoStack>( this ) );
    }



    public bool CanUndo
    {
      get
      {
        return m_UndoEntries.Count > 0;
      }
    }



    public bool CanRedo
    {
      get
      {
        return m_RedoEntries.Count > 0;
      }
    }



    public string UndoInfo
    {
      get
      {
        if ( m_UndoEntries.Count == 0 )
        {
          return "Undo";
        }
        return "Undo " + m_UndoEntries[m_UndoEntries.Count - 1].Description;
      }
    }



    public string RedoInfo
    {
      get
      {
        if ( m_RedoEntries.Count == 0 )
        {
          return "Redo";
        }
        return "Redo " + m_RedoEntries[m_RedoEntries.Count - 1].Description;
      }
    }



    /// <summary>
    /// Records a finished mutation. BeforeCrop is the Region's pre-change
    /// pixels (ownership transfers here); the after-crop is captured from
    /// CurrentImage, which must already hold the post-change state. Optional
    /// object payload (both-or-neither, deep copies, ownership transfers) —
    /// e.g. Flatten pairs its pixel bake with objects→empty in ONE entry.
    /// </summary>
    public void PushChange( Bitmap CurrentImage, Rectangle Region, Bitmap BeforeCrop, string Description,
                            List<OutlineTextObject> ObjectsBefore = null,
                            List<OutlineTextObject> ObjectsAfter = null,
                            Rectangle? SelectionBefore = null,
                            Rectangle? SelectionAfter = null )
    {
      NormalizeObjectPayload( ref ObjectsBefore, ref ObjectsAfter );
      var region = Rectangle.Intersect( Region, new Rectangle( 0, 0, CurrentImage.Width, CurrentImage.Height ) );
      if ( ( region.IsEmpty )
      ||   ( BeforeCrop == null ) )
      {
        if ( BeforeCrop != null )
        {
          BeforeCrop.Dispose();
        }
        return;
      }

      var entry = new Entry()
      {
        Region          = region,
        Before          = BeforeCrop,
        After           = CurrentImage.Clone( region, CurrentImage.PixelFormat ),
        ObjectsBefore   = ObjectsBefore,
        ObjectsAfter    = ObjectsAfter,
        SelectionBefore = SelectionBefore,
        SelectionAfter  = SelectionAfter,
        Description     = Description,
        Bytes         = (long)region.Width * region.Height * 4 * 2
                      + ObjectPayloadBytes( ObjectsBefore ) + ObjectPayloadBytes( ObjectsAfter ),
        Sequence      = s_NextSequence++
      };
      m_UndoEntries.Add( entry );
      s_TotalBytes += entry.Bytes;
      ClearRedo();
      EnforceGlobalBudget();
    }



    /// <summary>
    /// Records a size-changing operation. Ownership of BOTH bitmaps
    /// transfers to the stack — hand over the pre-operation image itself
    /// (the editor is swapping it out anyway) and a clone of the result.
    /// Optional object payload: crop/extend/paste-replace/delete-picture
    /// restore image AND objects in one undo step.
    /// </summary>
    public void PushImageReplace( Bitmap BeforeFullImage, Bitmap AfterFullImage, string Description,
                                  List<OutlineTextObject> ObjectsBefore = null,
                                  List<OutlineTextObject> ObjectsAfter = null )
    {
      NormalizeObjectPayload( ref ObjectsBefore, ref ObjectsAfter );
      if ( ( BeforeFullImage == null )
      ||   ( AfterFullImage == null ) )
      {
        BeforeFullImage?.Dispose();
        AfterFullImage?.Dispose();
        return;
      }
      var entry = new Entry()
      {
        Region         = new Rectangle( 0, 0, AfterFullImage.Width, AfterFullImage.Height ),
        Before         = BeforeFullImage,
        After          = AfterFullImage,
        IsImageReplace = true,
        ObjectsBefore  = ObjectsBefore,
        ObjectsAfter   = ObjectsAfter,
        Description    = Description,
        Bytes          = (long)BeforeFullImage.Width * BeforeFullImage.Height * 4
                       + (long)AfterFullImage.Width * AfterFullImage.Height * 4
                       + ObjectPayloadBytes( ObjectsBefore ) + ObjectPayloadBytes( ObjectsAfter ),
        Sequence       = s_NextSequence++
      };
      m_UndoEntries.Add( entry );
      s_TotalBytes += entry.Bytes;
      ClearRedo();
      EnforceGlobalBudget();
    }



    /// <summary>
    /// Records an object-ONLY mutation (text add/edit/move/delete/restyle) —
    /// no pixels change. Region is the union of the affected bounds ∩ image,
    /// used for invalidation on apply; an empty region is allowed. Both lists
    /// must be deep copies; ownership transfers to the stack.
    /// </summary>
    public void PushObjectChange( Rectangle Region,
                                  List<OutlineTextObject> ObjectsBefore,
                                  List<OutlineTextObject> ObjectsAfter, string Description )
    {
      if ( ( ObjectsBefore == null )
      ||   ( ObjectsAfter == null ) )
      {
        return;
      }
      var entry = new Entry()
      {
        Region        = Region,
        ObjectsBefore = ObjectsBefore,
        ObjectsAfter  = ObjectsAfter,
        Description   = Description,
        Bytes         = ObjectPayloadBytes( ObjectsBefore ) + ObjectPayloadBytes( ObjectsAfter ),
        Sequence      = s_NextSequence++
      };
      m_UndoEntries.Add( entry );
      s_TotalBytes += entry.Bytes;
      ClearRedo();
      EnforceGlobalBudget();
    }



    /// <summary>Both-or-neither: a half payload is normalized to none.</summary>
    private static void NormalizeObjectPayload( ref List<OutlineTextObject> Before,
                                                ref List<OutlineTextObject> After )
    {
      if ( ( Before == null )
      !=   ( After == null ) )
      {
        Before = null;
        After = null;
      }
    }



    /// <summary>
    /// Honest (if approximate) heap cost of an object snapshot — proportional
    /// to text length so a pasted novel can't hide from the byte budget, and
    /// to the PNG payload of image objects likewise. (Clones SHARE the PNG
    /// buffer, so counting it per snapshot over-charges — deliberately, like
    /// the shared text strings: conservative accounting evicts early rather
    /// than letting real memory hide from the budget.)
    /// </summary>
    private static long ObjectPayloadBytes( List<OutlineTextObject> Objects )
    {
      if ( Objects == null )
      {
        return 0;
      }
      long bytes = 64;
      foreach ( var obj in Objects )
      {
        bytes += 96 + 2L * ( obj.Text != null ? obj.Text.Length : 0 )
               + ( obj.ImagePNGData != null ? obj.ImagePNGData.Length : 0 );
      }
      return bytes;
    }



    /// <summary>Applies the top undo entry; null when there is nothing to undo.</summary>
    public OutlineUndoResult Undo( Bitmap TargetImage )
    {
      if ( m_UndoEntries.Count == 0 )
      {
        return null;
      }
      var entry = m_UndoEntries[m_UndoEntries.Count - 1];
      m_UndoEntries.RemoveAt( m_UndoEntries.Count - 1 );
      m_RedoEntries.Add( entry );
      return ApplyEntry( entry, TargetImage, entry.Before, entry.ObjectsBefore, entry.SelectionBefore );
    }



    public OutlineUndoResult Redo( Bitmap TargetImage )
    {
      if ( m_RedoEntries.Count == 0 )
      {
        return null;
      }
      var entry = m_RedoEntries[m_RedoEntries.Count - 1];
      m_RedoEntries.RemoveAt( m_RedoEntries.Count - 1 );
      m_UndoEntries.Add( entry );
      return ApplyEntry( entry, TargetImage, entry.After, entry.ObjectsAfter, entry.SelectionAfter );
    }



    /// <summary>
    /// Shared apply for both directions: PixelSide/ObjectSide are the entry's
    /// respective snapshots for the travel direction. Object snapshots return
    /// as DEEP COPIES so the stack's stored state can never alias the live
    /// list the editor mutates next (redo correctness).
    /// </summary>
    private static OutlineUndoResult ApplyEntry( Entry Applied, Bitmap TargetImage,
                                                 Bitmap PixelSide, List<OutlineTextObject> ObjectSide,
                                                 Rectangle? SelectionSide )
    {
      var result = new OutlineUndoResult()
      {
        Region = Applied.Region
      };
      if ( Applied.IsImageReplace )
      {
        result.Region = new Rectangle( 0, 0, PixelSide.Width, PixelSide.Height );
        result.ReplacementImage = (Bitmap)PixelSide.Clone();
        result.ImageChanged = true;
      }
      else if ( PixelSide != null )
      {
        BlitRegion( TargetImage, PixelSide, Applied.Region );
        result.ImageChanged = true;
      }
      // else: object-only entry — no pixels touched.
      if ( Applied.ObjectsBefore != null )
      {
        result.ObjectsChanged = true;
        result.Objects = OutlineTextObject.CloneList( ObjectSide );
      }
      // A non-null SelectionBefore marks a move/center entry: re-home the
      // committed selection on the travel-direction's rect (may be null).
      if ( Applied.SelectionBefore.HasValue )
      {
        result.SelectionChanged = true;
        result.Selection = SelectionSide;
      }
      return result;
    }



    public void Clear()
    {
      foreach ( var entry in m_UndoEntries )
      {
        DisposeEntry( entry );
      }
      m_UndoEntries.Clear();
      ClearRedo();
    }



    /// <summary>
    /// The smallest canvas size that can replay every stored entry without
    /// clipping. Used when a map's canvas is re-created blank (blank canvases
    /// are never persisted) while its history is still alive — undoing the
    /// erase-all must restore the drawing at its full former size.
    /// </summary>
    public Size RequiredCanvasExtent()
    {
      int width = 0;
      int height = 0;
      foreach ( var list in new[] { m_UndoEntries, m_RedoEntries } )
      {
        foreach ( var entry in list )
        {
          if ( entry.IsImageReplace )
          {
            width  = Math.Max( width, Math.Max( entry.Before.Width, entry.After.Width ) );
            height = Math.Max( height, Math.Max( entry.Before.Height, entry.After.Height ) );
          }
          else
          {
            width  = Math.Max( width, entry.Region.Right );
            height = Math.Max( height, entry.Region.Bottom );
          }
        }
      }
      return new Size( width, height );
    }



    private void ClearRedo()
    {
      foreach ( var entry in m_RedoEntries )
      {
        DisposeEntry( entry );
      }
      m_RedoEntries.Clear();
    }



    private static void BlitRegion( Bitmap Target, Bitmap Crop, Rectangle Region )
    {
      using ( var g = Graphics.FromImage( Target ) )
      {
        // Pixel-exact overwrite — snapshots must not re-blend.
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImage( Crop, Region, new Rectangle( 0, 0, Region.Width, Region.Height ), GraphicsUnit.Pixel );
      }
    }



    private static void DisposeEntry( Entry Entry )
    {
      s_TotalBytes -= Entry.Bytes;
      if ( Entry.Before != null )
      {
        Entry.Before.Dispose();
        Entry.Before = null;
      }
      if ( Entry.After != null )
      {
        Entry.After.Dispose();
        Entry.After = null;
      }
      // Object snapshots need no disposal — just release them to the GC.
      Entry.ObjectsBefore = null;
      Entry.ObjectsAfter = null;
    }



    /// <summary>
    /// Evicts the globally-oldest undo entries (across every live stack)
    /// while over budget, honoring each stack's minimum. Redo entries are
    /// never evicted here — they die naturally on the next PushChange.
    /// </summary>
    private static void EnforceGlobalBudget()
    {
      while ( s_TotalBytes > TOTAL_BYTE_BUDGET )
      {
        OutlineUndoStack victim = null;
        long oldestSequence = long.MaxValue;
        for ( int i = s_AllStacks.Count - 1; i >= 0; --i )
        {
          OutlineUndoStack stack;
          if ( !s_AllStacks[i].TryGetTarget( out stack ) )
          {
            s_AllStacks.RemoveAt( i );
            continue;
          }
          if ( stack.m_UndoEntries.Count <= MIN_KEPT_ENTRIES )
          {
            continue;
          }
          if ( stack.m_UndoEntries[0].Sequence < oldestSequence )
          {
            oldestSequence = stack.m_UndoEntries[0].Sequence;
            victim = stack;
          }
        }
        if ( victim == null )
        {
          // Every stack is at its guaranteed minimum — the budget yields.
          return;
        }
        DisposeEntry( victim.m_UndoEntries[0] );
        victim.m_UndoEntries.RemoveAt( 0 );
      }
    }
  }
}
