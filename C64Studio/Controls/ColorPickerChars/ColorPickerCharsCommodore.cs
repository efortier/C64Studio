using RetroDevStudio;
using RetroDevStudio.Formats;
using RetroDevStudio.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RetroDevStudio.Controls
{
  public partial class ColorPickerCharsCommodore : ColorPickerCharsBase
  {
    public ColorPickerCharsCommodore() :
      base( null, null, 0, 1 )
    { 
    }



    public ColorPickerCharsCommodore( StudioCore Core, CharsetProject Charset, ushort CurrentChar, byte CustomColor ) :
      base( Core, Charset, CurrentChar, CustomColor )
    {
      _Charset = Charset;

      InitializeComponent();

      panelCharColors.DisplayPage.Create( 128, 8, GR.Drawing.PixelFormat.Format32bppRgb );
    }



    public override void Redraw()
    {
      if ( _Charset == null )
      {
        return;
      }
      
      int     itemsPerRow = 16;
      if ( Parent != null )
      {
        itemsPerRow = Math.Max( 1, Parent.ClientSize.Width / SwatchSize );
      }
      int     numRows = ( 16 + itemsPerRow - 1 ) / itemsPerRow;

      if ( ( panelCharColors.DisplayPage.Width != itemsPerRow * SwatchSize )
      ||   ( panelCharColors.DisplayPage.Height != numRows * SwatchSize ) )
      {
        panelCharColors.DisplayPage.Create( itemsPerRow * SwatchSize, numRows * SwatchSize, GR.Drawing.PixelFormat.Format32bppRgb );
        panelCharColors.Size = new System.Drawing.Size( itemsPerRow * SwatchSize, numRows * SwatchSize );
        this.Size = new System.Drawing.Size( panelCharColors.Width, panelCharColors.Height );
      }

      GR.Image.MemoryImage tempImage = new GR.Image.MemoryImage( 8, 8, GR.Drawing.PixelFormat.Format32bppRgb );

      for ( byte i = 0; i < 16; ++i )
      {
        Displayer.CharacterDisplayer.DisplayChar( _Charset, SelectedChar, tempImage, 0, 0, i );

        // Scale to DisplayPage
        for ( int y = 0; y < 8; ++y )
        {
          for ( int x = 0; x < 8; ++x )
          {
             uint pixel = tempImage.GetPixel( x, y );
             
             int    destX = ( i % itemsPerRow ) * SwatchSize;
             int    destY = ( i / itemsPerRow ) * SwatchSize;
             
             int destStartX = destX + ( x * SwatchSize ) / 8;
             int destXEnd = destX + ( ( x + 1 ) * SwatchSize ) / 8;
             int destStartY = destY + ( y * SwatchSize ) / 8;
             int destYEnd = destY + ( ( y + 1 ) * SwatchSize ) / 8;
  
             for ( int dy = destStartY; dy < destYEnd; ++dy )
             {
               for ( int dx = destStartX; dx < destXEnd; ++dx )
               {
                 panelCharColors.DisplayPage.SetPixel( dx, dy, pixel );
               }
             }
          }
        }
      }
      panelCharColors.Invalidate();
    }



    private void panelCharColors_PostPaint( GR.Image.FastImage TargetBuffer )
    {
      int     itemsPerRow = 16;
      if ( Parent != null )
      {
        itemsPerRow = Math.Max( 1, Parent.ClientSize.Width / SwatchSize );
      }
      
      int     x1 = ( SelectedColor % itemsPerRow ) * SwatchSize;
      int     y1 = ( SelectedColor / itemsPerRow ) * SwatchSize;
      int     x2 = x1 + SwatchSize;
      int     y2 = y1 + SwatchSize;

      if ( Core != null )
      {
        uint  selColor = Core.Settings.FGColor( ColorableElement.SELECTION_FRAME );

        TargetBuffer.Rectangle( x1, y1, x2 - x1, y2 - y1, selColor );
      }
    }



    private void panelCharColors_MouseDown( object sender, MouseEventArgs e )
    {
      HandleMouseOnColorChooser( e.X, e.Y, e.Button );
    }



    private void panelCharColors_MouseMove( object sender, MouseEventArgs e )
    {
      HandleMouseOnColorChooser( e.X, e.Y, e.Button );
    }



    private void HandleMouseOnColorChooser( int X, int Y, MouseButtons Buttons )
    {
      if ( ( X < 0 )
      ||   ( X >= panelCharColors.ClientSize.Width ) 
      ||   ( Y < 0 )
      ||   ( Y >= panelCharColors.ClientSize.Height ) )
      {
        return;
      }
      
      int     itemsPerRow = 16;
      if ( Parent != null )
      {
        itemsPerRow = Math.Max( 1, Parent.ClientSize.Width / SwatchSize );
      }

      if ( ( Buttons & MouseButtons.Left ) == MouseButtons.Left )
      {
        int colorIndex = ( X / SwatchSize ) + ( Y / SwatchSize ) * itemsPerRow;
        if ( colorIndex < 16 )
        {
          SelectedColor = (byte)colorIndex;
          Redraw();
          RaiseColorSelectedEvent();
        }
      }
    }



  }
}
