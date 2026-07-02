using Microsoft.VisualStudio.TestTools.UnitTesting;



namespace TestProject
{
  /// <summary>
  /// Locks in the argument convention for GR.Path.RelativePathTo as used by
  /// every "store a project-relative file path" browse handler:
  ///   RelativePathTo( baseDir, true, file, false )
  /// walks FROM the project base directory TO the file, preserving
  /// subdirectories ("gfx\foo.ext"), which Path.Combine( baseDir, stored )
  /// resolves correctly on load. The SWAPPED order (file first) collapses any
  /// subdirectory into "..\foo.ext" — the latent bug fixed in the
  /// ExternalCharset browse handlers; this test keeps it from coming back.
  /// </summary>
  [TestClass]
  public class TestRelativePaths
  {
    [TestMethod]
    public void TestRelativePathToBaseDirToFileConvention()
    {
      // File in a subdirectory of the base path: subdir must be preserved.
      Assert.AreEqual( @"gfx\charset.charsetproject",
        GR.Path.RelativePathTo( @"C:\proj", true, @"C:\proj\gfx\charset.charsetproject", false ) );

      // Deeper nesting.
      Assert.AreEqual( @"gfx\sets\charset.charsetproject",
        GR.Path.RelativePathTo( @"C:\proj", true, @"C:\proj\gfx\sets\charset.charsetproject", false ) );

      // File directly in the base path.
      Assert.AreEqual( @"charset.charsetproject",
        GR.Path.RelativePathTo( @"C:\proj", true, @"C:\proj\charset.charsetproject", false ) );

      // Different drive: falls back to the absolute file path.
      Assert.AreEqual( @"D:\other\charset.charsetproject",
        GR.Path.RelativePathTo( @"C:\proj", true, @"D:\other\charset.charsetproject", false ) );
    }

    [TestMethod]
    public void TestSwappedArgumentOrderLosesSubdirectory()
    {
      // Documents WHY the swapped order is wrong: the subdirectory collapses
      // into "..", so resolving against the base path lands in the WRONG
      // place. If this assert ever fails, RelativePathTo's semantics changed
      // and every caller needs a re-audit.
      Assert.AreEqual( @"..\charset.charsetproject",
        GR.Path.RelativePathTo( @"C:\proj\gfx\charset.charsetproject", false, @"C:\proj", true ) );
    }
  }
}
