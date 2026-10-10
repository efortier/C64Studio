using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// MapProject.FindFreeMapStringID: the String ID that Add, Duplicate and the
  /// Map Strings tab's "?" button hand out - the lowest one from 1 to 255 that
  /// no other string holds. 0 is never handed out.
  /// </summary>
  [TestClass]
  public class TestMapStringIDs
  {
    private static MapProject ProjectWithIDs( params int[] IDs )
    {
      var project = new MapProject();
      foreach ( int id in IDs )
      {
        project.MapStrings.Add( new MapProject.MapString() { Label = "S" + id, StringID = (byte)id } );
      }
      return project;
    }



    [TestMethod]
    public void TestFirstStringGetsIDOne()
    {
      Assert.AreEqual( 1, new MapProject().FindFreeMapStringID( null ) );
    }



    [TestMethod]
    public void TestZeroIsNeverHandedOutEvenWhenFree()
    {
      Assert.AreEqual( 4, ProjectWithIDs( 1, 2, 3 ).FindFreeMapStringID( null ) );
    }



    [TestMethod]
    public void TestGapsAreFilledFirst()
    {
      // 1..8 and 10 taken, plus a leftover string on 0
      Assert.AreEqual( 9, ProjectWithIDs( 10, 5, 1, 2, 3, 4, 6, 7, 8, 0 ).FindFreeMapStringID( null ) );
    }



    [TestMethod]
    public void TestDuplicateIDsCountOnce()
    {
      Assert.AreEqual( 3, ProjectWithIDs( 1, 1, 2 ).FindFreeMapStringID( null ) );
    }



    [TestMethod]
    public void TestExcludedStringKeepsItsOwnFreeID()
    {
      var project = ProjectWithIDs( 1, 2, 3 );
      // reassigning the string on 2: once it stops counting itself, 2 is the lowest free ID
      Assert.AreEqual( 2, project.FindFreeMapStringID( project.MapStrings[1] ) );
    }



    [TestMethod]
    public void TestExcludedStringOnZeroMovesToTheLowestFreeID()
    {
      var project = ProjectWithIDs( 1, 2, 0 );
      Assert.AreEqual( 3, project.FindFreeMapStringID( project.MapStrings[2] ) );
    }



    [TestMethod]
    public void TestNoFreeIDReturnsMinusOne()
    {
      var project = new MapProject();
      for ( int id = 1; id <= 255; ++id )
      {
        project.MapStrings.Add( new MapProject.MapString() { StringID = (byte)id } );
      }
      Assert.AreEqual( -1, project.FindFreeMapStringID( null ) );
      // excluding a string frees its slot again
      Assert.AreEqual( 200, project.FindFreeMapStringID( project.MapStrings[199] ) );
    }
  }
}
