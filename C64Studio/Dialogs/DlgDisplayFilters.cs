using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;
using RetroDevStudio.CustomRenderer.DisplayFilters;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Modeless editor for the user's CRT-style display filter pipeline.
  /// Changes apply live via <see cref="m_OnPipelineChanged"/> (a callback
  /// into the owning MapEditor). A snapshot taken at dialog-open lets Revert
  /// roll back every change made in the current session.
  ///
  /// Built entirely in code — no Designer.cs file — because the right-hand
  /// parameter panel is rebuilt from scratch every time the selected filter
  /// changes, and driving that through the Forms designer would be more
  /// awkward than just constructing each filter's editor inline.
  /// </summary>
  public class DlgDisplayFilters : Form
  {
    private readonly FilterPipeline   m_Pipeline;
    private readonly FilterPipeline   m_OriginalSnapshot;
    private readonly Action           m_OnPipelineChanged;

    // Left pane
    private CheckedListBox            m_ListFilters;
    private KryptonButton             m_BtnMoveUp;
    private KryptonButton             m_BtnMoveDown;
    private KryptonButton             m_BtnRemove;
    private KryptonComboBox           m_ComboAdd;
    private KryptonButton             m_BtnAdd;

    // Right pane
    private KryptonPanel              m_PanelParams;
    private KryptonLabel              m_LabelParamHeader;

    // Bottom bar
    private KryptonComboBox           m_ComboPreset;
    private KryptonButton             m_BtnPresetApply;
    private KryptonButton             m_BtnRevert;
    private KryptonButton             m_BtnClose;

    // Suppression flag so we don't loop when programmatically rebuilding the
    // checked-list. Without this, ItemCheck fires during RefreshList and
    // wrecks the user's selection.
    private bool                      m_SuppressListEvents = false;



    public DlgDisplayFilters( FilterPipeline pipeline,
                              Action onPipelineChanged,
                              StudioCore core )
    {
      m_Pipeline          = pipeline;
      m_OnPipelineChanged = onPipelineChanged;
      m_OriginalSnapshot  = pipeline.Clone();

      Text            = "CRT Display Filters";
      StartPosition   = FormStartPosition.CenterParent;
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimumSize     = new Size( 640, 420 );
      Size            = new Size( 720, 460 );
      ShowInTaskbar   = false;

      BuildUI();
      PopulateAddCombo();
      PopulatePresetCombo();
      RefreshList();
      RefreshParamPanel();

      if ( core != null )
      {
        core.Theming.ApplyTheme( this );
      }
    }



    // ================================================================
    // UI construction
    // ================================================================

    private void BuildUI()
    {
      // --- left side: pipeline list + add/remove/reorder ---

      m_ListFilters = new CheckedListBox
      {
        Location        = new Point( 12, 12 ),
        Size            = new Size( 260, 300 ),
        IntegralHeight  = false,
        CheckOnClick    = true,
      };
      m_ListFilters.SelectedIndexChanged += OnListSelectionChanged;
      m_ListFilters.ItemCheck            += OnListItemCheck;
      Controls.Add( m_ListFilters );

      m_BtnMoveUp = new KryptonButton
      {
        Location = new Point( 12, 320 ),
        Size     = new Size( 60, 24 ),
      };
      m_BtnMoveUp.Values.Text = "▲ Up";
      m_BtnMoveUp.Click += ( s, e ) => MoveSelected( -1 );
      Controls.Add( m_BtnMoveUp );

      m_BtnMoveDown = new KryptonButton
      {
        Location = new Point( 76, 320 ),
        Size     = new Size( 70, 24 ),
      };
      m_BtnMoveDown.Values.Text = "▼ Down";
      m_BtnMoveDown.Click += ( s, e ) => MoveSelected( +1 );
      Controls.Add( m_BtnMoveDown );

      m_BtnRemove = new KryptonButton
      {
        Location = new Point( 150, 320 ),
        Size     = new Size( 70, 24 ),
      };
      m_BtnRemove.Values.Text = "Remove";
      m_BtnRemove.Click += ( s, e ) => RemoveSelected();
      Controls.Add( m_BtnRemove );

      m_ComboAdd = new KryptonComboBox
      {
        Location      = new Point( 12, 352 ),
        Size          = new Size( 180, 22 ),
        DropDownStyle = ComboBoxStyle.DropDownList,
      };
      Controls.Add( m_ComboAdd );

      m_BtnAdd = new KryptonButton
      {
        Location = new Point( 196, 351 ),
        Size     = new Size( 60, 24 ),
      };
      m_BtnAdd.Values.Text = "Add";
      m_BtnAdd.Click += ( s, e ) => AddFromCombo();
      Controls.Add( m_BtnAdd );

      // --- right side: parameter editor for the selected filter ---

      m_LabelParamHeader = new KryptonLabel
      {
        Location = new Point( 286, 12 ),
        Size     = new Size( 400, 20 ),
      };
      m_LabelParamHeader.Values.Text = "Select a filter to edit its parameters.";
      Controls.Add( m_LabelParamHeader );

      m_PanelParams = new KryptonPanel
      {
        Location = new Point( 286, 36 ),
        Size     = new Size( 400, 308 ),
      };
      Controls.Add( m_PanelParams );

      // --- bottom bar: preset / revert / close ---

      var bottomY = 388;

      var labelPreset = new KryptonLabel
      {
        Location = new Point( 12, bottomY + 3 ),
        Size     = new Size( 50, 20 ),
      };
      labelPreset.Values.Text = "Preset:";
      Controls.Add( labelPreset );

      m_ComboPreset = new KryptonComboBox
      {
        Location      = new Point( 62, bottomY ),
        Size          = new Size( 140, 22 ),
        DropDownStyle = ComboBoxStyle.DropDownList,
      };
      Controls.Add( m_ComboPreset );

      m_BtnPresetApply = new KryptonButton
      {
        Location = new Point( 206, bottomY - 1 ),
        Size     = new Size( 60, 24 ),
      };
      m_BtnPresetApply.Values.Text = "Apply";
      m_BtnPresetApply.Click += ( s, e ) => ApplyPreset();
      Controls.Add( m_BtnPresetApply );

      m_BtnRevert = new KryptonButton
      {
        Location = new Point( 490, bottomY - 1 ),
        Size     = new Size( 100, 24 ),
      };
      m_BtnRevert.Values.Text = "Revert changes";
      m_BtnRevert.Click += ( s, e ) => Revert();
      Controls.Add( m_BtnRevert );

      m_BtnClose = new KryptonButton
      {
        Location = new Point( 596, bottomY - 1 ),
        Size     = new Size( 80, 24 ),
      };
      m_BtnClose.Values.Text = "Close";
      m_BtnClose.Click += ( s, e ) => Close();
      Controls.Add( m_BtnClose );
    }



    private void PopulateAddCombo()
    {
      m_ComboAdd.Items.Clear();
      foreach ( var t in DisplayFilterRegistry.KnownFilters )
      {
        // Construct one to pull its user-visible Name; the instance is
        // discarded and we just keep the Type in Items.Tag-analog via
        // a parallel list.
        var probe = (IDisplayFilter)Activator.CreateInstance( t );
        m_ComboAdd.Items.Add( new AddComboItem( probe.Name, t ) );
      }
      if ( m_ComboAdd.Items.Count > 0 )
      {
        m_ComboAdd.SelectedIndex = 0;
      }
    }



    private sealed class AddComboItem
    {
      public string Label { get; }
      public Type   Type  { get; }
      public AddComboItem( string label, Type type ) { Label = label; Type = type; }
      public override string ToString() { return Label; }
    }



    private void PopulatePresetCombo()
    {
      m_ComboPreset.Items.Clear();
      m_ComboPreset.Items.Add( "Off (clear)" );
      m_ComboPreset.Items.Add( "C64 soft" );
      m_ComboPreset.Items.Add( "Sharp CRT" );
      m_ComboPreset.SelectedIndex = 0;
    }



    // ================================================================
    // Pipeline list management
    // ================================================================

    private void RefreshList()
    {
      m_SuppressListEvents = true;
      try
      {
        int prevIndex = m_ListFilters.SelectedIndex;
        m_ListFilters.Items.Clear();
        foreach ( var f in m_Pipeline.Filters )
        {
          int idx = m_ListFilters.Items.Add( f.Name );
          m_ListFilters.SetItemChecked( idx, f.Enabled );
        }
        if ( ( prevIndex >= 0 )
        &&   ( prevIndex < m_ListFilters.Items.Count ) )
        {
          m_ListFilters.SelectedIndex = prevIndex;
        }
        else if ( m_ListFilters.Items.Count > 0 )
        {
          m_ListFilters.SelectedIndex = 0;
        }
      }
      finally
      {
        m_SuppressListEvents = false;
      }
      UpdateButtonEnableStates();
    }



    private void UpdateButtonEnableStates()
    {
      int sel = m_ListFilters.SelectedIndex;
      bool has = ( sel >= 0 );
      m_BtnRemove.Enabled   = has;
      m_BtnMoveUp.Enabled   = has && ( sel > 0 );
      m_BtnMoveDown.Enabled = has && ( sel < m_ListFilters.Items.Count - 1 );
      m_BtnAdd.Enabled      = ( m_ComboAdd.Items.Count > 0 );
    }



    private void OnListSelectionChanged( object sender, EventArgs e )
    {
      if ( m_SuppressListEvents )
      {
        return;
      }
      RefreshParamPanel();
      UpdateButtonEnableStates();
    }



    private void OnListItemCheck( object sender, ItemCheckEventArgs e )
    {
      if ( m_SuppressListEvents )
      {
        return;
      }
      if ( ( e.Index < 0 )
      ||   ( e.Index >= m_Pipeline.Filters.Count ) )
      {
        return;
      }
      m_Pipeline.Filters[e.Index].Enabled = ( e.NewValue == CheckState.Checked );
      NotifyChanged();
    }



    private void MoveSelected( int delta )
    {
      int idx = m_ListFilters.SelectedIndex;
      int newIdx = idx + delta;
      if ( ( idx < 0 )
      ||   ( newIdx < 0 )
      ||   ( newIdx >= m_Pipeline.Filters.Count ) )
      {
        return;
      }
      var tmp = m_Pipeline.Filters[idx];
      m_Pipeline.Filters[idx] = m_Pipeline.Filters[newIdx];
      m_Pipeline.Filters[newIdx] = tmp;
      RefreshList();
      m_ListFilters.SelectedIndex = newIdx;
      NotifyChanged();
    }



    private void RemoveSelected()
    {
      int idx = m_ListFilters.SelectedIndex;
      if ( ( idx < 0 )
      ||   ( idx >= m_Pipeline.Filters.Count ) )
      {
        return;
      }
      m_Pipeline.Filters.RemoveAt( idx );
      RefreshList();
      RefreshParamPanel();
      NotifyChanged();
    }



    private void AddFromCombo()
    {
      var item = m_ComboAdd.SelectedItem as AddComboItem;
      if ( item == null )
      {
        return;
      }
      var filter = (IDisplayFilter)Activator.CreateInstance( item.Type );
      filter.Enabled = true;
      m_Pipeline.Filters.Add( filter );
      RefreshList();
      m_ListFilters.SelectedIndex = m_Pipeline.Filters.Count - 1;
      RefreshParamPanel();
      NotifyChanged();
    }



    // ================================================================
    // Params panel
    //
    // The panel is rebuilt every time the selected filter changes. It's
    // easier to do it this way than to implement per-filter control reuse,
    // and the filter list is short enough that the rebuild cost is nil.
    // ================================================================

    private void RefreshParamPanel()
    {
      m_PanelParams.SuspendLayout();
      try
      {
        m_PanelParams.Controls.Clear();
        int idx = m_ListFilters.SelectedIndex;
        if ( ( idx < 0 )
        ||   ( idx >= m_Pipeline.Filters.Count ) )
        {
          m_LabelParamHeader.Values.Text = "Select a filter to edit its parameters.";
          return;
        }
        var filter = m_Pipeline.Filters[idx];
        m_LabelParamHeader.Values.Text = "Parameters for: " + filter.Name;

        if ( filter is ScanlineFilter sf )
        {
          BuildScanlinePanel( sf );
        }
        else if ( filter is PhosphorMaskFilter pf )
        {
          BuildPhosphorPanel( pf );
        }
        else if ( filter is HorizontalBlurFilter hb )
        {
          BuildBlurPanel( hb );
        }
        else
        {
          var label = new KryptonLabel
          {
            Location = new Point( 8, 8 ),
            Size     = new Size( 380, 40 ),
          };
          label.Values.Text = "(no parameters)";
          m_PanelParams.Controls.Add( label );
        }
      }
      finally
      {
        m_PanelParams.ResumeLayout();
      }
    }



    /// <summary>
    /// Adds a labeled slider that edits an int getter/setter. The caller
    /// provides a getter + setter pair so we can live-update the filter's
    /// field without reflection. Returns the y for the next row so callers
    /// can stack them.
    /// </summary>
    private int AddSlider( string labelText,
                           int min, int max,
                           Func<int> get,
                           Action<int> set,
                           int y )
    {
      var label = new KryptonLabel
      {
        Location = new Point( 8, y ),
        Size     = new Size( 120, 20 ),
      };
      label.Values.Text = labelText;
      m_PanelParams.Controls.Add( label );

      var value = new KryptonLabel
      {
        Location  = new Point( 340, y ),
        Size      = new Size( 48, 20 ),
      };
      value.Values.Text = get().ToString();
      m_PanelParams.Controls.Add( value );

      var slider = new KryptonTrackBar
      {
        Location      = new Point( 132, y - 2 ),
        Size          = new Size( 200, 30 ),
        Minimum       = min,
        Maximum       = max,
        TickFrequency = Math.Max( 1, ( max - min ) / 10 ),
        Value         = Math.Max( min, Math.Min( max, get() ) ),
      };
      slider.ValueChanged += ( s, e ) =>
      {
        set( slider.Value );
        value.Values.Text = slider.Value.ToString();
        NotifyChanged();
      };
      m_PanelParams.Controls.Add( slider );

      return y + 32;
    }



    private void BuildScanlinePanel( ScanlineFilter f )
    {
      int y = 8;
      y = AddSlider( "Intensity (%)", 0, 100, () => f.Intensity, v => f.Intensity = v, y );
      y = AddSlider( "Period (px)",   2, 16,  () => f.Period,    v => f.Period    = v, y );
      y = AddSlider( "Row offset",    0, 15,  () => f.Offset,    v => f.Offset    = v, y );
    }



    private void BuildPhosphorPanel( PhosphorMaskFilter f )
    {
      int y = 8;
      y = AddSlider( "R boost (%)",   0, 50, () => f.RBoost, v => f.RBoost = v, y );
      y = AddSlider( "G boost (%)",   0, 50, () => f.GBoost, v => f.GBoost = v, y );
      y = AddSlider( "B boost (%)",   0, 50, () => f.BBoost, v => f.BBoost = v, y );
      y = AddSlider( "Non-phase dim (%)", 0, 50, () => f.Dim, v => f.Dim = v, y );
    }



    private void BuildBlurPanel( HorizontalBlurFilter f )
    {
      int y = 8;
      y = AddSlider( "Strength (%)", 0, 100, () => f.Strength, v => f.Strength = v, y );
    }



    // ================================================================
    // Presets & revert
    // ================================================================

    private void ApplyPreset()
    {
      m_Pipeline.Filters.Clear();
      switch ( m_ComboPreset.SelectedIndex )
      {
        case 0:
          // Off — leave empty.
          break;

        case 1:
          // C64 soft: VICE-style look. Horizontal beam blur first, then
          // subtle scanlines with a gradient so the bands have a fade
          // rather than hard edges.
          m_Pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 40, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter       { Intensity = 20, Period = 2, Offset = 0, Enabled = true } );
          break;

        case 2:
          // Sharp CRT: thinner-looking scanlines via a 4-pixel gradient
          // period, stronger intensity, with phosphor mask on top.
          m_Pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 25, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter       { Intensity = 40, Period = 4, Offset = 0, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter   { RBoost = 20, GBoost = 20, BBoost = 20, Dim = 25, Enabled = true } );
          break;
      }
      RefreshList();
      RefreshParamPanel();
      NotifyChanged();
    }



    private void Revert()
    {
      m_Pipeline.Filters.Clear();
      foreach ( var f in m_OriginalSnapshot.Filters )
      {
        m_Pipeline.Filters.Add( f.Clone() );
      }
      RefreshList();
      RefreshParamPanel();
      NotifyChanged();
    }



    private void NotifyChanged()
    {
      m_OnPipelineChanged?.Invoke();
    }
  }
}
