using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using GR.Image;
using static System.Windows.Forms.AxHost;
using System.Windows.Forms.VisualStyles;

namespace RetroDevStudio.Controls
{
  public class CSListView : System.Windows.Forms.ListView
  {
    public uint SelectedTextColor { get; set; }
    public uint SelectedTextBGColor { get; set; }

    /// <summary>
    /// Sub-item column where the SmallImageList icon (or DrawItemImage
    /// payload) is rendered. Defaults to 0 — the first user-content
    /// column — matching the original CSListView behavior. Set higher
    /// to put the image alongside a different column; e.g. listTileInfo
    /// on the Tiles tab uses 2 so the numeric Nr. column stays free
    /// for the tile index. Other consumers that don't touch this
    /// property are unaffected.
    /// </summary>
    public int ImageColumnIndex { get; set; } = 0;

    /// <summary>
    /// ListBox-style convenience: the first selected row index, or -1 if none.
    /// Setting it selects + focuses that single row (clearing any other
    /// selection), or clears the selection for -1 / out of range. Lets call
    /// sites written against a ListBox keep using <c>.SelectedIndex</c> after the
    /// control is swapped to a CSListView.
    /// </summary>
    public int SelectedIndex
    {
      get
      {
        return ( SelectedIndices.Count > 0 ) ? SelectedIndices[0] : -1;
      }
      set
      {
        SelectedIndices.Clear();
        if ( ( value >= 0 ) && ( value < Items.Count ) )
        {
          Items[value].Selected = true;
          Items[value].Focused  = true;
          Items[value].EnsureVisible();
        }
      }
    }

    public delegate void DrawItemImageHandler( Graphics G, int X, int Y, ListViewItem Item, ListViewItem.ListViewSubItem SubItem );


    public event DrawItemImageHandler     DrawItemImage;



    public CSListView()
    {
      OwnerDraw = true;
      SelectedTextColor = 0xffffffff;
      SelectedTextBGColor = 0xff0000ff;
    }



    protected override void OnDrawColumnHeader( DrawListViewColumnHeaderEventArgs e )
    {
      e.DrawDefault = true;
    }



    protected override void OnDrawItem( DrawListViewItemEventArgs e )
    {
    }



    protected override void OnDrawSubItem( DrawListViewSubItemEventArgs e )
    {
      var trimming = StringTrimming.None;
      if ( e.SubItem is CSListViewSubItem )
      {
        trimming = ( (CSListViewSubItem)e.SubItem ).Trimming;
      }
      int subItemIndex = e.Item.SubItems.IndexOf( e.SubItem );
      bool firstItem = ( subItemIndex == 0 );
      bool realFirstItem = firstItem;
      if ( CheckBoxes )
      {
        firstItem = ( subItemIndex == 1 );
      }
      // Whether THIS sub-item is the column the image renders into. Decoupled
      // from firstItem so consumers can place the image alongside any column
      // (see ImageColumnIndex). The first-column bounds quirk below stays
      // tied to firstItem because that's about column-0 sub-item bounds being
      // unreliable, which has nothing to do with where images go.
      bool isImageColumn = ( subItemIndex == ImageColumnIndex );

      // the file path
      var itemBounds = e.SubItem.Bounds;
      if ( firstItem )
      {
        itemBounds = new Rectangle( itemBounds.Left, itemBounds.Top, Columns[0].Width, itemBounds.Height );
      }
      var textBounds = new Rectangle( itemBounds.Left + 3, itemBounds.Top, itemBounds.Width - 6, itemBounds.Height );

      var checkBoxSize = CheckBoxRenderer.GetGlyphSize( e.Graphics, e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal );

      if ( e.Item.Selected )
      {
        var color = SelectedTextBGColor;

        e.Graphics.FillRectangle( new SolidBrush( GR.Color.Helper.FromARGB( color ) ), e.Bounds );

        if ( ( realFirstItem )
        &&   ( CheckBoxes ) )
        {
          CheckBoxRenderer.DrawCheckBox( e.Graphics,
                                         new Point( itemBounds.Location.X + ( Columns[0].Width - checkBoxSize.Width ) / 2,
                                                    itemBounds.Location.Y + ( itemBounds.Height - checkBoxSize.Height ) / 2 ),
                                         e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal );
        }
        if ( ( isImageColumn )
        &&   ( e.Item.ImageList != null )
        &&   ( e.Item.ImageIndex >= 0 )
        &&   ( e.Item.ImageIndex < e.Item.ImageList.Images.Count ) )
        {
          var image = e.Item.ImageList.Images[e.Item.ImageIndex];

          if ( DrawItemImage != null )
          {
            DrawItemImage( e.Graphics, itemBounds.Left + 3, itemBounds.Top, e.Item, e.SubItem );
          }
          else
          {
            e.Graphics.DrawImage( image, itemBounds.Left + 3, itemBounds.Top );
          }
          textBounds = new Rectangle( textBounds.Left + image.Width + 6, textBounds.Top, textBounds.Width - image.Width - 6, textBounds.Height );
        }

        e.Graphics.DrawString( e.SubItem.Text, e.SubItem.Font, new SolidBrush( GR.Color.Helper.FromARGB( SelectedTextColor ) ), textBounds,
          new StringFormat() { Trimming = trimming, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap } );
      }
      else
      {
        e.DrawBackground();

        if ( ( realFirstItem )
        &&   ( CheckBoxes ) )
        {
          CheckBoxRenderer.DrawCheckBox( e.Graphics,
                                         new Point( itemBounds.Location.X + ( Columns[0].Width - checkBoxSize.Width ) / 2,
                                                    itemBounds.Location.Y + ( itemBounds.Height - checkBoxSize.Height ) / 2 ),
                                         e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal );
        }

        if ( ( isImageColumn )
        &&   ( e.Item.ImageList != null )
        &&   ( e.Item.ImageIndex >= 0 )
        &&   ( e.Item.ImageIndex < e.Item.ImageList.Images.Count ) )
        {
          var image = e.Item.ImageList.Images[e.Item.ImageIndex];

          if ( DrawItemImage != null )
          {
            DrawItemImage( e.Graphics, itemBounds.Left + 3, itemBounds.Top, e.Item, e.SubItem );
          }
          else
          {
            e.Graphics.DrawImage( image, itemBounds.Left + 3, itemBounds.Top );
          }
          textBounds = new Rectangle( textBounds.Left + image.Width + 6, textBounds.Top, textBounds.Width - image.Width - 6, textBounds.Height );
        }

        // CSListViewSubItem can carry an OverrideForeColor — use it
        // when set so callers can tint individual cells without
        // having to flip UseItemStyleForSubItems off (which would
        // require re-themeing every other subitem on the row).
        Color textColor = ForeColor;
        if ( ( e.SubItem is CSListViewSubItem csSub )
        &&   ( csSub.OverrideForeColor.HasValue ) )
        {
          textColor = csSub.OverrideForeColor.Value;
        }
        e.Graphics.DrawString( e.SubItem.Text, e.SubItem.Font, new SolidBrush( textColor ), textBounds,
          new StringFormat() { Trimming = trimming, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap } );
      }

      if ( e.ItemState == ListViewItemStates.Focused )
      {
        e.DrawFocusRectangle( e.Bounds );
      }
    }


  }



  public class CSListViewSubItem : ListViewItem.ListViewSubItem
  {
    public StringTrimming Trimming { get; set; } = StringTrimming.None;

    /// <summary>
    /// Optional per-subitem text color override. When non-null,
    /// CSListView's painter uses this color for the unselected-text
    /// draw instead of the listview's <see cref="Control.ForeColor"/>.
    /// Lets callers tint individual cells (e.g. red text for an
    /// "unused" tile in the Tiles tab) without disabling
    /// <see cref="ListViewItem.UseItemStyleForSubItems"/> or
    /// re-implementing the painter. Selected-state still uses the
    /// listview's SelectedTextColor for legibility.
    /// </summary>
    public Color? OverrideForeColor { get; set; } = null;


    public CSListViewSubItem()
    {
    }



    public CSListViewSubItem( string Text, StringTrimming Trimming = StringTrimming.None )
    {
      this.Text = Text;
      this.Trimming = Trimming;
    }



  }



}
