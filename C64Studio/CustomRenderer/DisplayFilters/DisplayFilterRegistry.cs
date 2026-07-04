using System;
using System.Collections.Generic;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Tracks every <see cref="IDisplayFilter"/> implementation so the settings
  /// dialog can populate its "Add filter" dropdown and the settings loader
  /// can reconstruct a filter from its persisted type name.
  ///
  /// Each concrete filter registers itself in a static constructor, e.g.:
  /// <code>
  /// static ScanlineFilter()
  /// {
  ///   DisplayFilterRegistry.Register( typeof( ScanlineFilter ) );
  /// }
  /// </code>
  /// The static ctor fires once, at first reference from any thread. We
  /// touch every built-in filter type once in <see cref="EnsureBuiltInsLoaded"/>
  /// so users of the registry don't need to know which concrete classes
  /// exist — asking the registry for <see cref="KnownFilters"/> is enough.
  /// </summary>
  public static class DisplayFilterRegistry
  {
    private static readonly List<Type>  s_KnownFilters = new List<Type>();
    private static          bool        s_BuiltInsLoaded = false;



    /// <summary>
    /// Add a filter type to the registry. Idempotent — calling twice with
    /// the same type is fine.
    /// </summary>
    public static void Register( Type filterType )
    {
      if ( filterType == null )
      {
        return;
      }
      if ( !typeof( IDisplayFilter ).IsAssignableFrom( filterType ) )
      {
        throw new ArgumentException( "Type does not implement IDisplayFilter: " + filterType.FullName );
      }
      if ( !s_KnownFilters.Contains( filterType ) )
      {
        s_KnownFilters.Add( filterType );
      }
    }



    /// <summary>
    /// All registered filter types, in registration order. The UI uses this
    /// for its "Add filter" dropdown.
    /// </summary>
    public static IReadOnlyList<Type> KnownFilters
    {
      get
      {
        EnsureBuiltInsLoaded();
        return s_KnownFilters;
      }
    }



    /// <summary>
    /// Construct a filter by its persisted type name. Returns null if the
    /// type isn't registered — the settings loader treats that as "filter
    /// from a newer build, skip it" rather than an error.
    /// </summary>
    public static IDisplayFilter Create( string typeName )
    {
      if ( string.IsNullOrEmpty( typeName ) )
      {
        return null;
      }
      EnsureBuiltInsLoaded();
      foreach ( var t in s_KnownFilters )
      {
        if ( ( t.FullName == typeName ) || ( t.Name == typeName ) )
        {
          return (IDisplayFilter)Activator.CreateInstance( t );
        }
      }
      return null;
    }



    /// <summary>
    /// Touch each built-in filter type once so its static constructor runs
    /// and calls <see cref="Register"/>. Without this, a filter class that
    /// is never referenced elsewhere in the program won't load and the UI
    /// would be empty.
    /// </summary>
    private static void EnsureBuiltInsLoaded()
    {
      if ( s_BuiltInsLoaded )
      {
        return;
      }
      s_BuiltInsLoaded = true;
      // Simply referencing RuntimeTypeHandle forces the type (and its static
      // ctor) to load. We catch TypeLoadException so a stray build problem
      // with one filter doesn't take down the whole registry.
      TouchType( typeof( ScanlineFilter ) );
      TouchType( typeof( PhosphorMaskFilter ) );
      TouchType( typeof( HorizontalBlurFilter ) );
      TouchType( typeof( GammaAdjustFilter ) );
      TouchType( typeof( ColorTemperatureFilter ) );
      TouchType( typeof( BarrelDistortionFilter ) );
      TouchType( typeof( PALCompositeFilter ) );
      TouchType( typeof( BloomFilter ) );
      TouchType( typeof( ConvergenceFilter ) );
      TouchType( typeof( PhosphorPersistenceFilter ) );
      TouchType( typeof( VICLumaNoiseFilter ) );
    }



    private static void TouchType( Type t )
    {
      try
      {
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor( t.TypeHandle );
      }
      catch ( Exception )
      {
        // A misbehaving filter's static ctor shouldn't prevent the others
        // from registering. Swallow and continue.
      }
    }
  }
}
