extern alias studio;

using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using ExportSpriteAsGameBinary = studio::RetroDevStudio.Controls.ExportSpriteAsGameBinary;
using StudioByteBuffer = studio::GR.Memory.ByteBuffer;
using StudioDocumentInfo = studio::RetroDevStudio.DocumentInfo;
using StudioExportSpriteInfo = studio::RetroDevStudio.Formats.ExportSpriteInfo;
using StudioSpriteProject = studio::RetroDevStudio.Formats.SpriteProject;



namespace TestProject
{
  /// <summary>
  /// The real sprite game-binary export form and the SPRITE_TOTAL_FRAMES
  /// limit: more than SPRITE_TOTAL_FRAMES_MAX frames is refused before any
  /// file is written; at the limit the .asm sidecar carries the size
  /// constants. The form runs without a core, so its message boxes are
  /// skipped (a test cannot click them).
  /// </summary>
  [TestClass]
  public class TestSpriteGameBinaryExportForm
  {
    /// <summary>One animation per frame count; exports into Folder with the .asm sidecar on.</summary>
    private static SpriteProject BuildProject( string Folder, params int[] FrameCounts )
    {
      var project = new SpriteProject();
      foreach ( int frameCount in FrameCounts )
      {
        var overlay = new SpriteProject.Overlay();
        for ( int i = 0; i < frameCount; ++i )
        {
          var frame = new SpriteProject.OverlayFrame();
          frame.BankIndex[0] = 0;
          overlay.Frames.Add( frame );
        }
        project.Overlays.Add( overlay );
      }

      var settings = project.GameBinary;
      settings.AnimDirectory   = Folder;
      settings.AnimFilename    = "sprites.def";
      settings.AnimAsm         = true;
      settings.AnimAsmFilename = "sprite_headers.asm";
      settings.SprDirectory    = Folder;
      settings.SprFilename     = "sprites.spr";
      return project;
    }



    /// <summary>Runs the form's export on the project; returns HandleExport's result.</summary>
    private static bool RunExport( SpriteProject Project )
    {
      var studioProject = new StudioSpriteProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      using ( var form = new ExportSpriteAsGameBinary() )
      using ( var output = new TextBox() )
      {
        form.ApplyExportSettings( studioProject );
        return form.HandleExport( new StudioExportSpriteInfo() { Project = studioProject }, output, new StudioDocumentInfo() );
      }
    }



    private static string NewFolder()
    {
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioSpriteExport_" + Guid.NewGuid().ToString( "N" ) );
      Directory.CreateDirectory( folder );
      return folder;
    }



    private static int AsmConst( string Asm, string Name )
    {
      var match = System.Text.RegularExpressions.Regex.Match( Asm, @"\.const " + Name + @"\s+= (\d+)\s" );
      Assert.IsTrue( match.Success, Name + " is emitted" );
      return int.Parse( match.Groups[1].Value );
    }



    [TestMethod]
    public void TestTheFormRefusesMoreThanMaxFramesAndWritesNothing()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder, 64, 65 );
          Assert.AreEqual( SpriteProject.SPRITE_TOTAL_FRAMES_MAX + 1, project.TotalAnimationFrames );

          Assert.IsFalse( RunExport( project ) );
          Assert.AreEqual( 0, Directory.GetFiles( folder ).Length, string.Join( ", ", Directory.GetFiles( folder ) ) );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestTheFormWritesTheSizeConstantsAtTheLimit()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder, 64, 64 );
          Assert.AreEqual( SpriteProject.SPRITE_TOTAL_FRAMES_MAX, project.TotalAnimationFrames );

          Assert.IsTrue( RunExport( project ) );
          string asm = File.ReadAllText( Path.Combine( folder, "sprite_headers.asm" ) );
          Assert.AreEqual( 2, AsmConst( asm, "SPRITE_ANIM_DEF_COUNT" ) );
          Assert.AreEqual( 128, AsmConst( asm, "SPRITE_TOTAL_FRAMES" ) );
          Assert.IsTrue( File.Exists( Path.Combine( folder, "sprites.def" ) ) );
          Assert.IsTrue( File.Exists( Path.Combine( folder, "sprites.spr" ) ) );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }
  }
}
