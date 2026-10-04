extern alias studio;

using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// The Types enums exist in both referenced assemblies — StudioSettings' API
// takes the C64Studio copies.
using Function = studio::RetroDevStudio.Types.Function;
using StudioState = studio::RetroDevStudio.Types.StudioState;
using AcceleratorKey = studio::RetroDevStudio.AcceleratorKey;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioSettings = studio::RetroDevStudio.StudioSettings;



namespace TestProject
{
  /// <summary>
  /// Undo / redo are Ctrl+Z / Ctrl+Y only: the retired Alt+Backspace /
  /// Shift+Alt+Backspace / Ctrl+Shift+Z keys are gone from the defaults AND
  /// stripped from existing settings on load, and Delete Line (which owned
  /// Ctrl+Y) moved to Ctrl+Shift+L so Ctrl+Y resolves to Redo everywhere.
  /// </summary>
  [TestClass]
  public class TestKeyBindings
  {
    private static StudioSettings CreateSettings()
    {
      var core = new StudioCore();
      core.Settings.Core = core;
      return core.Settings;
    }



    private static AcceleratorKey BindingOf( StudioSettings Settings, Function Func )
    {
      return Settings.Accelerators.Values.FirstOrDefault( acc => acc.Function == Func );
    }



    private static void Seed( StudioSettings Settings, Function Func, Keys Key, Keys Secondary = Keys.None )
    {
      Settings.Accelerators.Add( Key, new AcceleratorKey( Key, Secondary, Func ) );
    }



    [TestMethod]
    public void TestDefaultsAreCtrlZAndCtrlYOnly()
    {
      var settings = CreateSettings();
      settings.SetDefaultKeyBinding();

      var undo = BindingOf( settings, Function.UNDO );
      var redo = BindingOf( settings, Function.REDO );
      Assert.AreEqual( Keys.Control | Keys.Z, undo.Key );
      Assert.AreEqual( Keys.None, undo.SecondaryKey );
      Assert.AreEqual( Keys.Control | Keys.Y, redo.Key );
      Assert.AreEqual( Keys.None, redo.SecondaryKey );

      // What the Edit menu displays (primary key of the function).
      Assert.AreEqual( Keys.Control | Keys.Z, settings.DetermineAcceleratorKeyForFunction( Function.UNDO, StudioState.NORMAL ) );
      Assert.AreEqual( Keys.Control | Keys.Y, settings.DetermineAcceleratorKeyForFunction( Function.REDO, StudioState.NORMAL ) );

      // What a key press dispatches to.
      Assert.AreEqual( Function.UNDO, settings.DetermineAccelerator( Keys.Control | Keys.Z, StudioState.NORMAL ).Function );
      Assert.AreEqual( Function.REDO, settings.DetermineAccelerator( Keys.Control | Keys.Y, StudioState.NORMAL ).Function );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Alt | Keys.Back, StudioState.NORMAL ) );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Shift | Keys.Alt | Keys.Back, StudioState.NORMAL ) );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Control | Keys.Shift | Keys.Z, StudioState.NORMAL ) );

      // Delete Line left Ctrl+Y.
      Assert.AreEqual( Keys.Control | Keys.Shift | Keys.L, BindingOf( settings, Function.DELETE_LINE ).Key );
      Assert.AreEqual( 1, settings.Accelerators.Values.Count( acc => acc.Key == ( Keys.Control | Keys.Y ) ) );
    }



    [TestMethod]
    public void TestSanitizeMigratesTheRetiredDefaults()
    {
      // An existing settings.dat written by the previous build.
      var settings = CreateSettings();
      Seed( settings, Function.UNDO, Keys.Alt | Keys.Back, Keys.Control | Keys.Z );
      Seed( settings, Function.REDO, Keys.Shift | Keys.Alt | Keys.Back, Keys.Control | Keys.Shift | Keys.Z );
      Seed( settings, Function.DELETE_LINE, Keys.Control | Keys.Y );

      settings.SanitizeSettings();

      var undo = BindingOf( settings, Function.UNDO );
      var redo = BindingOf( settings, Function.REDO );
      Assert.AreEqual( Keys.Control | Keys.Z, undo.Key );
      Assert.AreEqual( Keys.None, undo.SecondaryKey );
      Assert.AreEqual( Keys.Control | Keys.Y, redo.Key );
      Assert.AreEqual( Keys.None, redo.SecondaryKey );
      Assert.AreEqual( Keys.Control | Keys.Shift | Keys.L, BindingOf( settings, Function.DELETE_LINE ).Key );
      Assert.AreEqual( 1, settings.Accelerators.Values.Count( acc => acc.Function == Function.UNDO ) );
      Assert.AreEqual( 1, settings.Accelerators.Values.Count( acc => acc.Function == Function.REDO ) );

      // Ctrl+Y is Redo, not Delete Line, and the retired keys are dead.
      Assert.AreEqual( Function.REDO, settings.DetermineAccelerator( Keys.Control | Keys.Y, StudioState.NORMAL ).Function );
      Assert.AreEqual( Function.UNDO, settings.DetermineAccelerator( Keys.Control | Keys.Z, StudioState.NORMAL ).Function );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Alt | Keys.Back, StudioState.NORMAL ) );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Shift | Keys.Alt | Keys.Back, StudioState.NORMAL ) );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Control | Keys.Shift | Keys.Z, StudioState.NORMAL ) );

      // Idempotent on the next start.
      settings.SanitizeSettings();
      Assert.AreEqual( Keys.Control | Keys.Z, BindingOf( settings, Function.UNDO ).Key );
      Assert.AreEqual( Keys.Control | Keys.Y, BindingOf( settings, Function.REDO ).Key );
      Assert.AreEqual( 1, settings.Accelerators.Values.Count( acc => acc.Function == Function.REDO ) );
    }



    [TestMethod]
    public void TestSanitizeStripsARetiredSecondaryKey()
    {
      // Primary already Ctrl+Z, but the retired Alt+Backspace still rides
      // along as the secondary key — it has to go too.
      var settings = CreateSettings();
      Seed( settings, Function.UNDO, Keys.Control | Keys.Z, Keys.Alt | Keys.Back );
      Seed( settings, Function.REDO, Keys.Control | Keys.Y, Keys.Control | Keys.Shift | Keys.Z );

      settings.SanitizeSettings();

      Assert.AreEqual( Keys.Control | Keys.Z, BindingOf( settings, Function.UNDO ).Key );
      Assert.AreEqual( Keys.None, BindingOf( settings, Function.UNDO ).SecondaryKey );
      Assert.AreEqual( Keys.Control | Keys.Y, BindingOf( settings, Function.REDO ).Key );
      Assert.AreEqual( Keys.None, BindingOf( settings, Function.REDO ).SecondaryKey );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Alt | Keys.Back, StudioState.NORMAL ) );
      Assert.IsNull( settings.DetermineAccelerator( Keys.Control | Keys.Shift | Keys.Z, StudioState.NORMAL ) );
    }



    [TestMethod]
    public void TestSanitizeLeavesCustomBindingsAlone()
    {
      // Bindings the user remapped to something else entirely stay.
      var settings = CreateSettings();
      Seed( settings, Function.UNDO, Keys.F3 );
      Seed( settings, Function.REDO, Keys.F4, Keys.Control | Keys.R );
      Seed( settings, Function.DELETE_LINE, Keys.Control | Keys.D );

      settings.SanitizeSettings();

      Assert.AreEqual( Keys.F3, BindingOf( settings, Function.UNDO ).Key );
      Assert.AreEqual( Keys.None, BindingOf( settings, Function.UNDO ).SecondaryKey );
      Assert.AreEqual( Keys.F4, BindingOf( settings, Function.REDO ).Key );
      Assert.AreEqual( Keys.Control | Keys.R, BindingOf( settings, Function.REDO ).SecondaryKey );
      Assert.AreEqual( Keys.Control | Keys.D, BindingOf( settings, Function.DELETE_LINE ).Key );
    }



    [TestMethod]
    public void TestSanitizeAddsTheNewDefaultsWhenMissing()
    {
      var settings = CreateSettings();

      settings.SanitizeSettings();

      Assert.AreEqual( Keys.Control | Keys.Z, BindingOf( settings, Function.UNDO ).Key );
      Assert.AreEqual( Keys.Control | Keys.Y, BindingOf( settings, Function.REDO ).Key );
      Assert.AreEqual( Keys.Control | Keys.Shift | Keys.L, BindingOf( settings, Function.DELETE_LINE ).Key );
    }
  }
}
