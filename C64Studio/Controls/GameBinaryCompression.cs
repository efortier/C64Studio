using System;
using System.Globalization;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Shared helper for the game-binary exports (map + sprite): parses a C64 hex
  /// address and runs the bundled Krill compressor (dali.exe / ZX0) on an
  /// already-written file, producing a SECOND compressed file plus a size/ratio
  /// report. UI-agnostic — callers do their own control reading, path resolution
  /// and message boxes; this class only does the parse + the compressor run.
  /// </summary>
  public static class GameBinaryCompression
  {
    /// <summary>
    /// Parse a C64 hex address ($0000-$FFFF). Accepts an optional "$" or "0x"
    /// prefix. Returns false for empty/garbage/out-of-range input.
    /// </summary>
    public static bool TryParseHexAddress( string Text, out int Address )
    {
      Address = 0;
      string t = ( Text ?? "" ).Trim();
      if ( t.StartsWith( "$" ) )
      {
        t = t.Substring( 1 );
      }
      else if ( t.StartsWith( "0x" ) || t.StartsWith( "0X" ) )
      {
        t = t.Substring( 2 );
      }
      if ( t.Length == 0 )
      {
        return false;
      }
      if ( !int.TryParse( t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out Address ) )
      {
        return false;
      }
      return ( Address >= 0 ) && ( Address <= 0xFFFF );
    }



    /// <summary>
    /// Compress <paramref name="InputFilePath"/> to <paramref name="OutputFilePath"/>
    /// with the named compressor (currently only "ZX0" via Compressors/dali.exe).
    /// <paramref name="OverrideAddress"/> &lt; 0 = none, else 0..$FFFF passed to
    /// dali's --relocate-origin. On success returns true with <paramref name="Report"/>
    /// set (sizes, %, depack address); on failure returns false with
    /// <paramref name="Error"/> set (the caller decides how to surface it).
    /// </summary>
    public static bool RunCompressor(
      string Compressor, string InputFilePath, string OutputFilePath,
      int OverrideAddress, long UncompressedSize,
      out string Report, out string Error )
    {
      Report = null;
      Error  = null;

      string relocateArg = "";
      bool   relocated    = false;
      int    loadAddress  = -1;
      if ( OverrideAddress >= 0 )
      {
        // dali's --relocate-origin accepts a decimal value.
        relocateArg = " --relocate-origin " + OverrideAddress;
        loadAddress = OverrideAddress;
        relocated   = true;
      }

      string exeName;
      string args;
      switch ( Compressor )
      {
        case "ZX0":
        default:
          exeName = "dali.exe";
          args = "-o \"" + OutputFilePath + "\"" + relocateArg + " \"" + InputFilePath + "\"";
          break;
      }

      string toolPath = System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName( System.Windows.Forms.Application.ExecutablePath ),
        "Compressors",
        exeName );
      if ( !System.IO.File.Exists( toolPath ) )
      {
        Error = "Could not find the compressor:\r\n" + toolPath;
        return false;
      }

      try
      {
        var proc = new System.Diagnostics.Process();
        proc.StartInfo.FileName = toolPath;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        string stdOut = proc.StandardOutput.ReadToEnd();
        string stdErr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        int exitCode = proc.ExitCode;
        proc.Close();

        if ( ( exitCode != 0 )
        ||   ( !System.IO.File.Exists( OutputFilePath ) ) )
        {
          Error = "The compressor returned an error (exit code " + exitCode + ").\r\n\r\n"
                + ( string.IsNullOrEmpty( stdErr ) ? stdOut : stdErr );
          return false;
        }
      }
      catch ( Exception ex )
      {
        Error = "Could not run the compressor:\r\n" + toolPath + "\r\n\r\n" + ex.Message;
        return false;
      }

      long compressedSize = new System.IO.FileInfo( OutputFilePath ).Length;
      // Percentage of size REMOVED: 75% means the file is now 1/4 of the original.
      double percent = ( UncompressedSize > 0 )
                       ? ( 1.0 - ( compressedSize / (double)UncompressedSize ) ) * 100.0
                       : 0.0;

      // Depack load address: when not relocating, it's what dali reads from the
      // source — the first two bytes (little-endian) of the file.
      if ( !relocated )
      {
        try
        {
          byte[] head = new byte[2];
          using ( var fs = System.IO.File.OpenRead( InputFilePath ) )
          {
            if ( fs.Read( head, 0, 2 ) == 2 )
            {
              loadAddress = head[0] | ( head[1] << 8 );
            }
          }
        }
        catch ( Exception )
        {
          loadAddress = -1;
        }
      }
      string addrText = ( loadAddress >= 0 )
                        ? "  load $" + loadAddress.ToString( "X4" ) + ( relocated ? " (relocated)" : " (original)" )
                        : "";

      Report = "Compressed (" + Compressor + "): " + UncompressedSize + " -> " + compressedSize
             + " bytes  (" + percent.ToString( "0.0" ) + "% compression; "
             + ( 100.0 - percent ).ToString( "0.0" ) + "% of original)" + addrText + "  -> " + OutputFilePath;
      return true;
    }
  }
}
