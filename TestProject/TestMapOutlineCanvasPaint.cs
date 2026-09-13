extern alias studio;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MapOutlineCanvas = studio::RetroDevStudio.Controls.MapOutlineCanvas;
using OutlineTextObject = studio::RetroDevStudio.Controls.OutlineTextObject;
using SelectionTool = studio::RetroDevStudio.Controls.SelectionTool;
using RectangleTool = studio::RetroDevStudio.Controls.RectangleTool;



namespace TestProject
{
  /// <summary>
  /// Paint-order contract of the outline canvas: persistent text/image
  /// objects float ABOVE the raster before and after every raster edit, so a
  /// tool preview that stands in for raster pixels (a selection move's
  /// vacated source + floating pixels, a shape being dragged out) must render
  /// below them too. Drawing such a preview over the objects hides them for
  /// the duration of the drag — "the pasted picture vanishes while I move the
  /// selection and comes back on release". Rendered through DrawToBitmap,
  /// which drives the control's real OnPaint.
  /// </summary>
  [TestClass]
  public class TestMapOutlineCanvasPaint
  {
    private const int CANVAS_SIZE = 64;
    // An opaque red image object sits here; nothing in the raster is red.
    private static readonly Rectangle OBJECT_RECT = new Rectangle( 24, 24, 8, 8 );
    private static readonly Point OBJECT_PROBE = new Point( 28, 28 );
    // A white block in the raster — proves a raster-level preview really is
    // drawn (and moved), so the object probes cannot pass vacuously.
    private static readonly Rectangle RASTER_BLOCK = new Rectangle( 10, 10, 3, 3 );



    /// <summary>WinForms is happiest on an STA thread; the test runner's is MTA.</summary>
    private static void RunOnStaThread( Action Body )
    {
      Exception failure = null;
      var thread = new Thread( () =>
      {
        try
        {
          Body();
        }
        catch ( Exception ex )
        {
          failure = ex;
        }
      } );
      thread.SetApartmentState( ApartmentState.STA );
      thread.Start();
      thread.Join();
      if ( failure != null )
      {
        throw new AssertFailedException( failure.Message, failure );
      }
    }



    private static byte[] SolidPng( int Width, int Height, Color Fill )
    {
      using ( var bitmap = new Bitmap( Width, Height, PixelFormat.Format32bppArgb ) )
      using ( var g = Graphics.FromImage( bitmap ) )
      using ( var stream = new System.IO.MemoryStream() )
      {
        g.Clear( Fill );
        bitmap.Save( stream, ImageFormat.Png );
        return stream.ToArray();
      }
    }



    /// <summary>A black canvas carrying one opaque red image object, zoom 1, no pan.</summary>
    private static MapOutlineCanvas MakeCanvasWithRedObject()
    {
      var canvas = new MapOutlineCanvas();
      canvas.Size = new Size( CANVAS_SIZE, CANVAS_SIZE );
      var image = new Bitmap( CANVAS_SIZE, CANVAS_SIZE, PixelFormat.Format32bppArgb );
      using ( var g = Graphics.FromImage( image ) )
      {
        g.Clear( Color.Black );
        using ( var white = new SolidBrush( Color.White ) )
        {
          g.FillRectangle( white, RASTER_BLOCK );
        }
      }
      canvas.Image = image;
      canvas.TextObjects = new List<OutlineTextObject>()
      {
        new OutlineTextObject()
        {
          Position     = new PointF( OBJECT_RECT.X, OBJECT_RECT.Y ),
          ImagePNGData = SolidPng( OBJECT_RECT.Width, OBJECT_RECT.Height, Color.Red )
        }
      };
      return canvas;
    }



    private static void InvokeMouse( MapOutlineCanvas Canvas, string Method, int X, int Y )
    {
      var handler = typeof( MapOutlineCanvas ).GetMethod( Method, BindingFlags.Instance | BindingFlags.NonPublic );
      Assert.IsNotNull( handler, Method + " not found" );
      handler.Invoke( Canvas, new object[] { new MouseEventArgs( MouseButtons.Left, 1, X, Y, 0 ) } );
    }



    /// <summary>One real OnPaint of the control into a bitmap (caller disposes).</summary>
    private static Bitmap Render( MapOutlineCanvas Canvas )
    {
      var shot = new Bitmap( CANVAS_SIZE, CANVAS_SIZE, PixelFormat.Format32bppArgb );
      Canvas.DrawToBitmap( shot, new Rectangle( 0, 0, CANVAS_SIZE, CANVAS_SIZE ) );
      return shot;
    }



    private static void AssertPixel( Bitmap Shot, Point At, Color Expected, string What )
    {
      var pixel = Shot.GetPixel( At.X, At.Y );
      Assert.AreEqual( Expected.R, pixel.R, What + " at " + At + ": R" );
      Assert.AreEqual( Expected.G, pixel.G, What + " at " + At + ": G" );
      Assert.AreEqual( Expected.B, pixel.B, What + " at " + At + ": B" );
    }



    [TestMethod]
    public void TestImageObjectRendersAboveRasterWhenIdle()
    {
      RunOnStaThread( () =>
      {
        using ( var canvas = MakeCanvasWithRedObject() )
        using ( var shot = Render( canvas ) )
        {
          AssertPixel( shot, OBJECT_PROBE, Color.Red, "idle: object" );
          AssertPixel( shot, new Point( 11, 11 ), Color.White, "idle: raster block" );
        }
      } );
    }



    [TestMethod]
    public void TestSelectionMovePreviewKeepsObjectVisible()
    {
      RunOnStaThread( () =>
      {
        using ( var canvas = MakeCanvasWithRedObject() )
        {
          var tool = new SelectionTool();
          canvas.ActiveTool = tool;
          // Select everything, then grab inside the selection and drag: the
          // move preview vacates the source and floats the lifted raster.
          canvas.SetSelectionRect( new Rectangle( 0, 0, CANVAS_SIZE, CANVAS_SIZE ) );
          InvokeMouse( canvas, "OnMouseDown", 10, 10 );
          InvokeMouse( canvas, "OnMouseMove", 15, 10 );
          Assert.IsTrue( tool.IsMovingSelection, "move should be in flight" );

          using ( var shot = Render( canvas ) )
          {
            // The preview really is drawn and really moved: the white block
            // now floats 5px to the right, and its source reads vacated.
            AssertPixel( shot, new Point( 16, 11 ), Color.White, "move preview: lifted block" );
            AssertPixel( shot, new Point( 11, 11 ), Color.Black, "move preview: vacated source" );
            // The object is not part of the raster move — it must stay
            // visible at its own position throughout the drag, exactly as it
            // will be the instant the move commits.
            AssertPixel( shot, OBJECT_PROBE, Color.Red, "move preview: object" );
          }
        }
      } );
    }



    [TestMethod]
    public void TestShapeDragPreviewKeepsObjectVisible()
    {
      RunOnStaThread( () =>
      {
        using ( var canvas = MakeCanvasWithRedObject() )
        {
          canvas.ActiveTool = new RectangleTool();
          // Default fill is opaque grey: dragging a rectangle across the
          // object previews a filled shape over its footprint.
          InvokeMouse( canvas, "OnMouseDown", 2, 2 );
          InvokeMouse( canvas, "OnMouseMove", 60, 60 );

          using ( var shot = Render( canvas ) )
          {
            // The preview really is drawn: opaque grey fill inside the drag
            // rect, away from the object.
            AssertPixel( shot, new Point( 40, 40 ), Color.FromArgb( 128, 128, 128 ), "shape preview: fill" );
            // The finished shape lands in the raster UNDER the object, so the
            // preview must not cover it either.
            AssertPixel( shot, OBJECT_PROBE, Color.Red, "shape preview: object" );
          }
        }
      } );
    }
  }
}
