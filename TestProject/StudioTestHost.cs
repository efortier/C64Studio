extern alias studio;

using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using StudioCore = studio::RetroDevStudio.StudioCore;
using MainForm = studio::RetroDevStudio.MainForm;



namespace TestProject
{
  /// <summary>
  /// Hosts real C64Studio UI (documents, export forms) inside a test: an STA
  /// thread to run it on and a core with a stand-in MainForm.
  /// </summary>
  internal static class StudioTestHost
  {
    /// <summary>WinForms with drop targets needs an STA thread; failures are rethrown on the caller's.</summary>
    public static void RunOnSta( Action Body )
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
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture( failure ).Throw();
      }
    }



    /// <summary>
    /// A core with a stand-in MainForm: the real one shows a splash screen
    /// and loads (and later saves) the user's settings, so its constructor
    /// never runs. Only the base Form is constructed, then the fields the UI
    /// reaches are filled in - the core, the docking panel and the
    /// toolbar/menu items UpdateUndoSettings enables whenever a document
    /// marks itself modified or clean.
    /// </summary>
    public static StudioCore CreateCore()
    {
      var core = new StudioCore();
      core.Settings.Core = core;
      var mainForm = (MainForm)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject( typeof( MainForm ) );
      typeof( Form ).GetConstructor( Type.EmptyTypes ).Invoke( mainForm, null );
      GC.SuppressFinalize( mainForm );
      foreach ( var field in typeof( MainForm ).GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly ) )
      {
        if ( field.GetValue( mainForm ) != null )
        {
          continue;
        }
        if ( ( ( typeof( ToolStripItem ).IsAssignableFrom( field.FieldType ) ) || ( field.Name == "panelMain" ) )
        &&   ( field.FieldType.GetConstructor( Type.EmptyTypes ) != null ) )
        {
          field.SetValue( mainForm, Activator.CreateInstance( field.FieldType ) );
        }
      }
      typeof( MainForm ).GetField( "StudioCore" ).SetValue( mainForm, core );
      core.MainForm = mainForm;
      return core;
    }
  }
}
