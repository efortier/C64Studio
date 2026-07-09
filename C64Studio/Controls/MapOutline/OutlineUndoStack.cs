using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Self-contained undo/redo for one map's outline image — deliberately
  /// NOT an UndoManager/UndoTask: the outline's history must never mix
  /// with the map document's, and applying an outline entry must never
  /// SetModified (drawing does not dirty the .mapproject). The map editor
  /// keeps one instance per Map object for the whole session and routes
  /// Undo/Redo here while outline mode is active.
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
      public string       Description;
      public long         Bytes;
      public long         Sequence;
    }



    /// <summary>
    /// Result of applying one undo/redo step. Region is the in-place dirty
    /// rect; when ReplacementImage is non-null the operation changed the
    /// image (usually its size) as a whole — the caller takes ownership of
    /// the clone and must swap it in for its current bitmap.
    /// </summary>
    public class OutlineUndoResult
    {
      public Rectangle    Region;
      public Bitmap       ReplacementImage;
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
    /// CurrentImage, which must already hold the post-change state.
    /// </summary>
    public void PushChange( Bitmap CurrentImage, Rectangle Region, Bitmap BeforeCrop, string Description )
    {
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
        Region      = region,
        Before      = BeforeCrop,
        After       = CurrentImage.Clone( region, CurrentImage.PixelFormat ),
        Description = Description,
        Bytes       = (long)region.Width * region.Height * 4 * 2,
        Sequence    = s_NextSequence++
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
    /// </summary>
    public void PushImageReplace( Bitmap BeforeFullImage, Bitmap AfterFullImage, string Description )
    {
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
        Description    = Description,
        Bytes          = (long)BeforeFullImage.Width * BeforeFullImage.Height * 4
                       + (long)AfterFullImage.Width * AfterFullImage.Height * 4,
        Sequence       = s_NextSequence++
      };
      m_UndoEntries.Add( entry );
      s_TotalBytes += entry.Bytes;
      ClearRedo();
      EnforceGlobalBudget();
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
      if ( entry.IsImageReplace )
      {
        return new OutlineUndoResult()
        {
          Region = new Rectangle( 0, 0, entry.Before.Width, entry.Before.Height ),
          ReplacementImage = (Bitmap)entry.Before.Clone()
        };
      }
      BlitRegion( TargetImage, entry.Before, entry.Region );
      return new OutlineUndoResult()
      {
        Region = entry.Region
      };
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
      if ( entry.IsImageReplace )
      {
        return new OutlineUndoResult()
        {
          Region = new Rectangle( 0, 0, entry.After.Width, entry.After.Height ),
          ReplacementImage = (Bitmap)entry.After.Clone()
        };
      }
      BlitRegion( TargetImage, entry.After, entry.Region );
      return new OutlineUndoResult()
      {
        Region = entry.Region
      };
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
