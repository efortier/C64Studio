using System;
using System.Collections.Generic;
using System.IO;
using RetroDevStudio.Formats.TextTemplate;



namespace RetroDevStudio.Formats
{
  /// <summary>One active custom export, compiled and resolved, ready to render.</summary>
  public class CustomExportJob
  {
    public MapProject.CustomExport    Definition = null;
    public Template                   Template = null;
    /// <summary>The resolved output folder (set by ResolveDirectories).</summary>
    public string                     Directory = "";
    /// <summary>The resolved full output path (set by ResolveDirectories).</summary>
    public string                     Path = "";
  }



  /// <summary>
  /// Runs the custom text exports of a map project: compiles every active
  /// definition against MapExportSchema BEFORE anything is written (a broken
  /// template aborts the export like a bad file name pattern does), resolves
  /// their output paths once the map folder is known, and renders them
  /// against the export's context. Writing the files is the caller's job.
  /// </summary>
  public static class CustomExportRunner
  {
    /// <summary>
    /// Compiles the active definitions. Every problem (file name, template)
    /// goes to Errors as "custom export 'Name': ..."; definitions with a
    /// problem are left out of the result.
    /// </summary>
    public static List<CustomExportJob> Prepare( IEnumerable<MapProject.CustomExport> Definitions, out List<string> Errors )
    {
      Errors = new List<string>();
      var jobs = new List<CustomExportJob>();
      foreach ( var definition in Definitions )
      {
        if ( !definition.Active )
        {
          continue;
        }
        string name = DisplayName( definition );
        bool ok = true;
        string fileNameError = ValidateFileName( definition.FileName );
        if ( fileNameError != null )
        {
          Errors.Add( "custom export '" + name + "': " + fileNameError );
          ok = false;
        }
        List<TemplateError> templateErrors;
        var template = Template.Compile( definition.Content ?? "", MapExportSchema.Schema, out templateErrors );
        if ( template == null )
        {
          foreach ( var error in templateErrors )
          {
            Errors.Add( "custom export '" + name + "': " + error.ToString() );
          }
          ok = false;
        }
        if ( ok )
        {
          jobs.Add( new CustomExportJob() { Definition = definition, Template = template } );
        }
      }
      return jobs;
    }



    /// <summary>Null when FileName is usable, else what is wrong with it.</summary>
    public static string ValidateFileName( string FileName )
    {
      string name = ( FileName ?? "" ).Trim();
      if ( name.Length == 0 )
      {
        return "the file name is empty";
      }
      if ( ( name.IndexOf( '\\' ) >= 0 )
      ||   ( name.IndexOf( '/' ) >= 0 )
      ||   ( name.IndexOf( ':' ) >= 0 ) )
      {
        return "the file name '" + name + "' must not contain a folder - put the folder in the directory field";
      }
      if ( name.IndexOfAny( System.IO.Path.GetInvalidFileNameChars() ) >= 0 )
      {
        return "the file name '" + name + "' contains characters a file name cannot have";
      }
      return null;
    }



    /// <summary>
    /// The output folder of a definition: "" = the map export folder, a
    /// relative path = from the project file's folder (the map folder when
    /// the project was never saved), a rooted path as is.
    /// </summary>
    public static string ResolveDirectory( string Configured, string MapDirectory, string DocumentFolder )
    {
      string configured = ( Configured ?? "" ).Trim();
      if ( configured.Length == 0 )
      {
        return MapDirectory ?? "";
      }
      bool rooted = false;
      try
      {
        rooted = System.IO.Path.IsPathRooted( configured );
      }
      catch ( Exception )
      {
      }
      if ( rooted )
      {
        return configured;
      }
      string baseFolder = !string.IsNullOrEmpty( DocumentFolder ) ? DocumentFolder : ( MapDirectory ?? "" );
      if ( baseFolder.Length == 0 )
      {
        return configured;
      }
      return System.IO.Path.Combine( baseFolder, configured );
    }



    /// <summary>
    /// Resolves every job's folder and full path. Two jobs on one path, or a
    /// path one of ReservedPaths (the files the export itself writes) holds,
    /// are errors.
    /// </summary>
    public static void ResolveDirectories( List<CustomExportJob> Jobs, string MapDirectory, string DocumentFolder,
                                           IEnumerable<string> ReservedPaths, out List<string> Errors )
    {
      Errors = new List<string>();
      var reserved = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
      if ( ReservedPaths != null )
      {
        foreach ( var path in ReservedPaths )
        {
          string full = FullPath( path );
          if ( full != null )
          {
            reserved.Add( full );
          }
        }
      }
      var taken = new Dictionary<string, CustomExportJob>( StringComparer.OrdinalIgnoreCase );
      foreach ( var job in Jobs )
      {
        string name = DisplayName( job.Definition );
        job.Directory = ResolveDirectory( job.Definition.Directory, MapDirectory, DocumentFolder );
        string combined;
        try
        {
          combined = System.IO.Path.Combine( job.Directory, ( job.Definition.FileName ?? "" ).Trim() );
        }
        catch ( Exception )
        {
          combined = null;
        }
        string full = ( combined != null ) ? FullPath( combined ) : null;
        if ( full == null )
        {
          Errors.Add( "custom export '" + name + "': cannot resolve the output path '" + job.Directory + "' + '" + job.Definition.FileName + "'" );
          continue;
        }
        job.Path = full;
        if ( reserved.Contains( full ) )
        {
          Errors.Add( "custom export '" + name + "' would overwrite a file the export writes: " + full );
          continue;
        }
        CustomExportJob other;
        if ( taken.TryGetValue( full, out other ) )
        {
          Errors.Add( "custom exports '" + DisplayName( other.Definition ) + "' and '" + name + "' both write " + full );
          continue;
        }
        taken[full] = job;
      }
    }



    public static string Render( CustomExportJob Job, MapExportContext Context )
    {
      return Job.Template.Render( Context );
    }



    public static string DisplayName( MapProject.CustomExport Definition )
    {
      if ( ( Definition == null )
      ||   string.IsNullOrEmpty( Definition.Name ) )
      {
        return "(unnamed" + ( ( Definition != null ) ? ", ID " + Definition.ID : "" ) + ")";
      }
      return Definition.Name;
    }



    private static string FullPath( string Path )
    {
      try
      {
        return string.IsNullOrEmpty( Path ) ? null : System.IO.Path.GetFullPath( Path );
      }
      catch ( Exception )
      {
        return null;
      }
    }
  }
}
