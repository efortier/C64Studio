extern alias studio;

using System;
using System.Reflection;
using System.Windows.Forms;
using Krypton.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;

namespace TestProject
{
  /// <summary>
  /// The name dropdowns on the map editor's tool bars open a list that is
  /// as wide as its longest entry, while the control itself keeps the size
  /// it was given in the designer. Long marker / entity / charset / map
  /// names used to be cut off with an ellipsis, which made two names that
  /// share a prefix impossible to tell apart.
  /// </summary>
  [TestClass]
  public class TestComboDropDownWidth
  {
    // ---------------------------------------------------------------- harness

    private const int   CONTROL_WIDTH = 150;

    /// <summary>The handler the designer hooks to every name combo's DropDown.</summary>
    private static void Fit( KryptonComboBox Combo )
    {
      var method = typeof( MapEditor ).GetMethod( "ComboFitDropDownWidth",
                                                  BindingFlags.Static | BindingFlags.NonPublic );
      Assert.IsNotNull( method, "MapEditor.ComboFitDropDownWidth exists" );
      method.Invoke( null, new object[] { Combo, EventArgs.Empty } );
    }

    /// <summary>Raise a combo's DropDown event the way opening the list does.</summary>
    private static void RaiseDropDown( KryptonComboBox Combo )
    {
      var onDropDown = typeof( KryptonComboBox ).GetMethod( "OnDropDown",
                                                            BindingFlags.Instance | BindingFlags.NonPublic );
      Assert.IsNotNull( onDropDown, "KryptonComboBox.OnDropDown exists" );
      onDropDown.Invoke( Combo, new object[] { EventArgs.Empty } );
    }

    private static KryptonComboBox NewCombo( params string[] Items )
    {
      var combo = new KryptonComboBox();
      combo.DropDownStyle = ComboBoxStyle.DropDownList;
      combo.Size          = new System.Drawing.Size( CONTROL_WIDTH, 22 );
      combo.DropDownWidth = CONTROL_WIDTH;
      combo.Items.AddRange( Items );
      return combo;
    }

    private static int TextWidth( KryptonComboBox Combo, string Text )
    {
      return TextRenderer.MeasureText( Text, Combo.Font ).Width;
    }

    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestALongNameWidensTheListButNotTheControl()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var combo = NewCombo( "None", "REMOVE_FOG", "PLAYFIELD_CHARSET_SWITCH_A" ) )
        {
          Fit( combo );

          Assert.AreEqual( CONTROL_WIDTH, combo.Width, "the control keeps its tool bar size" );
          Assert.IsTrue( combo.DropDownWidth >= TextWidth( combo, "PLAYFIELD_CHARSET_SWITCH_A" ),
                         "the list fits the longest name (list " + combo.DropDownWidth + ", name "
                         + TextWidth( combo, "PLAYFIELD_CHARSET_SWITCH_A" ) + ")" );
          Assert.IsTrue( combo.DropDownWidth > combo.Width, "the list is wider than the control" );
        }
      } );
    }



    [TestMethod]
    public void TestShortNamesLeaveTheListAtTheControlWidth()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var combo = NewCombo( "None", "Fog", "Tick" ) )
        {
          Fit( combo );

          Assert.AreEqual( combo.Width, combo.DropDownWidth,
                           "a list that fits must not be widened past the control" );
        }
      } );
    }



    [TestMethod]
    public void TestTheListNarrowsAgainWhenTheLongNameIsRemoved()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var combo = NewCombo( "None", "Fog", "PLAYFIELD_CHARSET_SWITCH_A" ) )
        {
          Fit( combo );
          int widened = combo.DropDownWidth;
          Assert.IsTrue( widened > combo.Width, "the long name widened the list" );

          combo.Items.Remove( "PLAYFIELD_CHARSET_SWITCH_A" );
          Fit( combo );

          Assert.AreEqual( combo.Width, combo.DropDownWidth,
                           "removing the long name narrows the list back to the control" );
        }
      } );
    }



    [TestMethod]
    public void TestAScrollingListAllowsForItsScrollBar()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var shortList = NewCombo( "PLAYFIELD_CHARSET_SWITCH_A", "Fog" ) )
        using ( var longList = NewCombo( "PLAYFIELD_CHARSET_SWITCH_A", "Fog" ) )
        {
          // same longest name in both, but only one list has to scroll
          Assert.AreEqual( shortList.MaxDropDownItems, longList.MaxDropDownItems );
          for ( int i = 0; i < longList.MaxDropDownItems; ++i )
          {
            longList.Items.Add( "Filler " + i );
          }

          Fit( shortList );
          Fit( longList );

          Assert.AreEqual( shortList.DropDownWidth + SystemInformation.VerticalScrollBarWidth,
                           longList.DropDownWidth,
                           "a scrolling list is wider by exactly the scroll bar" );
        }
      } );
    }



    [TestMethod]
    public void TestAnEmptyListStaysAtTheControlWidth()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var combo = NewCombo() )
        {
          Fit( combo );

          Assert.AreEqual( combo.Width, combo.DropDownWidth, "an empty list keeps the control width" );
        }
      } );
    }



    [TestMethod]
    public void TestEveryNameComboInTheMapEditorIsWiredToTheFitter()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string[] combos = { "comboMarkerTypes", "comboEntityTypes", "comboCharsets", "comboMapCharset",
                            "comboEntityPreviewCharset", "comboMaps", "comboSpriteAnimations" };

        using ( var editor = new MapEditor( StudioTestHost.CreateCore() ) )
        {
          foreach ( var name in combos )
          {
            var field = typeof( MapEditor ).GetField( name, BindingFlags.Instance | BindingFlags.NonPublic );
            Assert.IsNotNull( field, name + " is a field on MapEditor" );

            var combo = (KryptonComboBox)field.GetValue( editor );
            Assert.IsNotNull( combo, name + " was created by the designer" );

            combo.Items.Add( "A_VERY_LONG_NAME_THAT_WOULD_BE_CLIPPED_IN_THE_TOOLBAR" );
            combo.DropDownWidth = combo.Width;

            RaiseDropDown( combo );

            Assert.IsTrue( combo.DropDownWidth > combo.Width,
                           name + " widens its list when it opens (was " + combo.Width
                           + ", list " + combo.DropDownWidth + ")" );
          }
        }
      } );
    }
  }
}
