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
  ///
  /// The filter list is a <see cref="ListView"/> with CheckBoxes rather than
  /// a <see cref="CheckedListBox"/> because ListView distinguishes
  /// "click on checkbox" from "click on row text" — clicking text selects
  /// the row without toggling the enable flag, and the checkbox is a
  /// dedicated click target. CheckedListBox toggles on any click which was
  /// confusing when the only intent was to view a filter's parameters.
  ///
  /// Dialog geometry (location + size) is persisted in
  /// <see cref="StudioSettings.DisplayFiltersDialogBounds"/> so the dialog
  /// reopens where the user last left it.
  /// </summary>
  public class DlgDisplayFilters : Form
  {
    private readonly FilterPipeline   m_Pipeline;
    private readonly FilterPipeline   m_OriginalSnapshot;
    private readonly Action           m_OnPipelineChanged;
    private readonly StudioCore       m_Core;

    // Left pane
    private ListView                  m_ListFilters;
    private KryptonButton             m_BtnMoveUp;
    private KryptonButton             m_BtnMoveDown;
    private KryptonButton             m_BtnRemove;
    private KryptonComboBox           m_ComboAdd;
    private KryptonButton             m_BtnAdd;

    // Right pane
    private KryptonPanel              m_PanelParams;
    private KryptonLabel              m_LabelParamHeader;
    // One ToolTip component for all parameter rows — a fresh ToolTip per
    // reset button (the old pattern) leaks a component per row per panel
    // rebuild for the dialog's lifetime.
    private readonly ToolTip          m_ParamToolTips = new ToolTip();

    // Bottom bar
    private KryptonComboBox           m_ComboPreset;
    private KryptonButton             m_BtnPresetApply;
    private KryptonButton             m_BtnRevert;
    private KryptonButton             m_BtnClose;

    // True once RestoreGeometry has finished setting initial bounds. Before
    // this flag flips, OnMove/OnResize events (which fire during Show with
    // Location still at (0,0)) would otherwise clobber the previously
    // saved position with zeros.
    private bool                      m_GeometryInitialized = false;



    public DlgDisplayFilters( FilterPipeline pipeline,
                              Action onPipelineChanged,
                              StudioCore core )
    {
      m_Pipeline          = pipeline;
      m_OnPipelineChanged = onPipelineChanged;
      m_OriginalSnapshot  = pipeline.Clone();
      m_Core              = core;

      Text            = "CRT Display Filters";
      FormBorderStyle = FormBorderStyle.Sizable;
      MinimumSize     = new Size( 640, 420 );
      ShowInTaskbar   = false;

      BuildUI();
      PopulateAddCombo();
      PopulatePresetCombo();

      RestoreGeometry();

      // Apply themes BEFORE populating the listview. Win32's
      // SetWindowTheme (called by ApplyDarkScrollBarsTo) re-themes the
      // native ListView and clears subitem state — including Checked.
      // If we populate first and theme second, every previously-
      // enabled filter loses its checkbox tick. Theming an empty
      // listview is safe; subsequent Items.Add calls inherit the
      // already-applied theme.
      if ( core != null )
      {
        core.Theming.ApplyTheme( this );
      }
      RetroDevStudio.CustomRenderer.DarkTheme.ApplyDarkScrollBarsTo( m_ListFilters );

      RefreshList();
      RefreshParamPanel();
    }



    // ================================================================
    // UI construction
    // ================================================================

    private void BuildUI()
    {
      // --- left side: pipeline list + add/remove/reorder ---

      m_ListFilters = new ListView
      {
        Location      = new Point( 12, 12 ),
        Size          = new Size( 260, 300 ),
        View          = View.Details,
        CheckBoxes    = true,
        FullRowSelect = true,
        MultiSelect   = false,
        HideSelection = false,
        HeaderStyle   = ColumnHeaderStyle.None,
        GridLines     = false,
      };
      // Single column wide enough to cover the list — headers are hidden so
      // the user just sees the filter names.
      m_ListFilters.Columns.Add( "Filter", m_ListFilters.ClientSize.Width - 4 );
      m_ListFilters.ItemSelectionChanged += ( s, e ) => OnListSelectionChanged();
      m_ListFilters.ItemChecked          += OnListItemCheckedHandler;
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
        // Parameter-heavy filters (PAL Composite) exceed the panel height —
        // scroll rather than clip.
        AutoScroll = true,
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
      m_ComboPreset.Items.Add( "CRT Rich" );
      m_ComboPreset.Items.Add( "PAL TV" );
      m_ComboPreset.Items.Add( "PAL RF" );
      m_ComboPreset.SelectedIndex = 0;
    }



    // ================================================================
    // Geometry persistence
    // ================================================================

    private void RestoreGeometry()
    {
      try
      {
        var saved = ( m_Core != null ) && ( m_Core.Settings != null )
                    ? m_Core.Settings.DisplayFiltersDialogBounds
                    : Rectangle.Empty;
        if ( saved.IsEmpty )
        {
          StartPosition = FormStartPosition.CenterParent;
          Size          = new Size( 720, 460 );
          return;
        }
        // Guard against a monitor disappearing between sessions. Clamp the
        // origin to the current working area so the dialog never opens off
        // screen.
        StartPosition = FormStartPosition.Manual;
        var screen    = Screen.FromPoint( new Point( saved.X, saved.Y ) ).WorkingArea;
        int x = Math.Max( screen.Left, Math.Min( screen.Right  - 200, saved.X ) );
        int y = Math.Max( screen.Top,  Math.Min( screen.Bottom - 200, saved.Y ) );
        int w = Math.Max( MinimumSize.Width,  Math.Min( screen.Width,  saved.Width ) );
        int h = Math.Max( MinimumSize.Height, Math.Min( screen.Height, saved.Height ) );
        Location = new Point( x, y );
        Size     = new Size( w, h );
      }
      finally
      {
        // Flip the gate ONLY after any Location/Size assignments above have
        // settled and any OnMove/OnResize events they triggered have fired
        // (and been rejected by the guard below). From here on, user moves
        // and resizes will be persisted.
        m_GeometryInitialized = true;
      }
    }



    private void SaveGeometry()
    {
      if ( ( m_Core == null )
      ||   ( m_Core.Settings == null ) )
      {
        return;
      }
      // Saving the normal bounds rather than Current Bounds means
      // maximized/minimized states don't stomp the last "free" position.
      var bounds = ( WindowState == FormWindowState.Normal )
                   ? new Rectangle( Location, Size )
                   : RestoreBounds;
      if ( ( bounds.Width  > 0 )
      &&   ( bounds.Height > 0 ) )
      {
        m_Core.Settings.DisplayFiltersDialogBounds = bounds;
      }
    }



    protected override void OnMove( EventArgs e )
    {
      base.OnMove( e );
      // RestoreGeometry sets the gate after applying the initial bounds, so
      // the pre-show OnMove events that fire while Location is still (0,0)
      // don't overwrite the saved position with zeros.
      if ( m_GeometryInitialized )
      {
        SaveGeometry();
      }
    }



    protected override void OnResize( EventArgs e )
    {
      base.OnResize( e );
      if ( m_GeometryInitialized )
      {
        SaveGeometry();
      }
      // Keep the single column matching the list width on resize. This
      // runs always, even before geometry is initialized, so the initial
      // column width is correct too.
      if ( ( m_ListFilters != null )
      &&   ( m_ListFilters.Columns.Count > 0 ) )
      {
        m_ListFilters.Columns[0].Width = m_ListFilters.ClientSize.Width - 4;
      }
    }



    protected override void OnFormClosed( FormClosedEventArgs e )
    {
      if ( m_GeometryInitialized )
      {
        SaveGeometry();
      }
      m_ParamToolTips.Dispose();
      base.OnFormClosed( e );
    }



    // ================================================================
    // Pipeline list management
    // ================================================================

    private int SelectedListIndex
    {
      get
      {
        return ( m_ListFilters.SelectedIndices.Count > 0 )
               ? m_ListFilters.SelectedIndices[0] : -1;
      }
    }



    private void RefreshList()
    {
      // Detach the ItemChecked handler while we populate so the
      // programmatic Checked assignments don't ricochet through
      // OnListItemChecked, which would write item.Checked back into
      // m_Pipeline.Filters[idx].Enabled (a redundant write that also
      // calls NotifyChanged for every item — extra repaint storm on
      // every dialog open). Reattach in finally so user clicks after
      // populate still update the model normally.
      m_ListFilters.ItemChecked -= OnListItemCheckedHandler;
      try
      {
        // Force the handle NOW, inside the detach window. Items added to a
        // handle-less ListView are only buffered — WinForms replays them
        // (re-raising ItemChecked, with the first transition carrying
        // Checked == false) when the handle is created at first Show,
        // AFTER the finally below has reattached the handler. That replay
        // would write transient wrong Enabled values into the LIVE
        // pipeline and hijack the selection via the select-on-check logic.
        // Same trap and same fix as MapEditor.RefreshLayerList.
        if ( !m_ListFilters.IsHandleCreated )
        {
          _ = m_ListFilters.Handle;
        }

        int prevIndex = SelectedListIndex;
        m_ListFilters.BeginUpdate();
        m_ListFilters.Items.Clear();
        foreach ( var f in m_Pipeline.Filters )
        {
          var item = new ListViewItem( f.Name ) { Checked = f.Enabled };
          ApplyItemEnabledLook( item );
          m_ListFilters.Items.Add( item );
        }
        if ( ( prevIndex >= 0 )
        &&   ( prevIndex < m_ListFilters.Items.Count ) )
        {
          m_ListFilters.Items[prevIndex].Selected = true;
        }
        else if ( m_ListFilters.Items.Count > 0 )
        {
          m_ListFilters.Items[0].Selected = true;
        }
        m_ListFilters.EndUpdate();
      }
      finally
      {
        m_ListFilters.ItemChecked += OnListItemCheckedHandler;
      }
      UpdateButtonEnableStates();
    }



    private void UpdateButtonEnableStates()
    {
      int sel = SelectedListIndex;
      bool has = ( sel >= 0 );
      m_BtnRemove.Enabled   = has;
      m_BtnMoveUp.Enabled   = has && ( sel > 0 );
      m_BtnMoveDown.Enabled = has && ( sel < m_ListFilters.Items.Count - 1 );
      m_BtnAdd.Enabled      = ( m_ComboAdd.Items.Count > 0 );
    }



    private void OnListSelectionChanged()
    {
      RefreshParamPanel();
      UpdateButtonEnableStates();
    }



    private void OnListItemChecked( ListViewItem item )
    {
      int idx = item.Index;
      if ( ( idx < 0 )
      ||   ( idx >= m_Pipeline.Filters.Count ) )
      {
        return;
      }
      // No-op when the model already matches: ListView replays buffered
      // checked items (re-raising ItemChecked) when its handle is created
      // at first Show — same trap as the map editor's layer list.
      if ( m_Pipeline.Filters[idx].Enabled == item.Checked )
      {
        return;
      }
      m_Pipeline.Filters[idx].Enabled = item.Checked;
      ApplyItemEnabledLook( item );
      // Clicking a checkbox is an unambiguous "I mean THIS filter" — pull
      // the selection (and with it the parameter panel) onto the toggled
      // row, otherwise the sliders keep editing whichever row happened to
      // be selected before and the user tweaks the wrong filter.
      if ( !item.Selected )
      {
        item.Selected = true;
        item.Focused  = true;
      }
      NotifyChanged();
    }



    /// <summary>
    /// Disabled filters render greyed out so the list reads at a glance —
    /// the checkbox alone is easy to mis-associate with the wrong row.
    /// </summary>
    private void ApplyItemEnabledLook( ListViewItem item )
    {
      item.ForeColor = item.Checked
                       ? m_ListFilters.ForeColor
                       : Color.FromArgb( 128, 128, 128 );
    }

    private void OnListItemCheckedHandler( object sender, ItemCheckedEventArgs e )
    {
      OnListItemChecked( e.Item );
    }



    private void MoveSelected( int delta )
    {
      int idx = SelectedListIndex;
      int newIdx = idx + delta;
      // idx itself is bounds-checked too, not just newIdx — the list view
      // could in principle show stale rows relative to the pipeline.
      if ( ( idx < 0 )
      ||   ( idx >= m_Pipeline.Filters.Count )
      ||   ( newIdx < 0 )
      ||   ( newIdx >= m_Pipeline.Filters.Count ) )
      {
        return;
      }
      var tmp = m_Pipeline.Filters[idx];
      m_Pipeline.Filters[idx] = m_Pipeline.Filters[newIdx];
      m_Pipeline.Filters[newIdx] = tmp;
      RefreshList();
      m_ListFilters.Items[newIdx].Selected = true;
      NotifyChanged();
    }



    private void RemoveSelected()
    {
      int idx = SelectedListIndex;
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
      int newIdx = m_Pipeline.Filters.Count - 1;
      if ( ( newIdx >= 0 )
      &&   ( newIdx < m_ListFilters.Items.Count ) )
      {
        m_ListFilters.Items[newIdx].Selected = true;
      }
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
      // Rebuild lays controls out from y=8 in unscrolled coordinates; if
      // the panel is still scrolled from the previous (taller) filter's
      // panel, the fresh rows would land offset in virtual space.
      m_PanelParams.AutoScrollPosition = new Point( 0, 0 );
      m_PanelParams.SuspendLayout();
      try
      {
        // Dispose, not just Clear — Clear only detaches; the sliders,
        // labels and buttons (win32 handles) of every previously selected
        // filter would pile up until the dialog closes.
        m_ParamToolTips.RemoveAll();
        while ( m_PanelParams.Controls.Count > 0 )
        {
          var old = m_PanelParams.Controls[0];
          m_PanelParams.Controls.RemoveAt( 0 );
          old.Dispose();
        }
        int idx = SelectedListIndex;
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
        else if ( filter is GammaAdjustFilter gf )
        {
          BuildGammaPanel( gf );
        }
        else if ( filter is ColorTemperatureFilter ct )
        {
          BuildColorTempPanel( ct );
        }
        else if ( filter is BarrelDistortionFilter bd )
        {
          BuildBarrelPanel( bd );
        }
        else if ( filter is PALCompositeFilter pal )
        {
          BuildPALPanel( pal );
        }
        else if ( filter is BloomFilter bloom )
        {
          BuildBloomPanel( bloom );
        }
        else if ( filter is ConvergenceFilter conv )
        {
          BuildConvergencePanel( conv );
        }
        else if ( filter is PhosphorPersistenceFilter pp )
        {
          BuildPersistencePanel( pp );
        }
        else if ( filter is VICLumaNoiseFilter vn )
        {
          BuildVICNoisePanel( vn );
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
    /// Adds a labeled slider + value label + reset-to-default button. Caller
    /// provides the default value that the reset button restores. Returns
    /// the y for the next row so callers can stack them.
    /// </summary>
    private int AddSlider( string labelText,
                           int min, int max, int defaultValue,
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

      // Declare these in outer scope so the lambdas can reach them.
      KryptonTrackBar slider;
      KryptonLabel    value;

      // Row width budget: the params panel auto-scrolls vertically for
      // parameter-heavy filters, and the vertical scrollbar shaves ~17 px
      // off the client width — every row must fit in ~383 px or a
      // horizontal scrollbar cascades in and clips the reset column.
      value = new KryptonLabel
      {
        Location = new Point( 316, y ),
        Size     = new Size( 40, 20 ),
      };
      value.Values.Text = get().ToString();
      m_PanelParams.Controls.Add( value );

      slider = new KryptonTrackBar
      {
        Location      = new Point( 132, y - 2 ),
        Size          = new Size( 180, 30 ),
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

      var resetBtn = new KryptonButton
      {
        Location = new Point( 358, y - 1 ),
        Size     = new Size( 22, 22 ),
      };
      resetBtn.Values.Text = "↺";
      // KryptonButton picks up the global palette automatically, so no
      // per-control Theming call is needed for the reset glyph to render
      // in dark mode.
      m_ParamToolTips.SetToolTip( resetBtn, "Reset to " + defaultValue );
      resetBtn.Click += ( s, e ) =>
      {
        int clamped = Math.Max( min, Math.Min( max, defaultValue ) );
        slider.Value      = clamped;
        value.Values.Text = clamped.ToString();
        set( clamped );
        NotifyChanged();
      };
      m_PanelParams.Controls.Add( resetBtn );

      return y + 32;
    }



    /// <summary>
    /// Adds a labeled dropdown for enum-like int parameters. Same row
    /// layout as <see cref="AddSlider"/> so panels can mix the two.
    /// </summary>
    private int AddCombo( string labelText,
                          string[] options,
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

      var combo = new KryptonComboBox
      {
        Location      = new Point( 132, y - 2 ),
        Size          = new Size( 180, 22 ),
        DropDownStyle = ComboBoxStyle.DropDownList,
      };
      foreach ( var option in options )
      {
        combo.Items.Add( option );
      }
      int current = get();
      combo.SelectedIndex = ( ( current >= 0 ) && ( current < options.Length ) ) ? current : 0;
      combo.SelectedIndexChanged += ( s, e ) =>
      {
        set( combo.SelectedIndex );
        NotifyChanged();
      };
      m_PanelParams.Controls.Add( combo );

      return y + 32;
    }



    private void BuildScanlinePanel( ScanlineFilter f )
    {
      // Defaults mirror the ScanlineFilter field initializers so the reset
      // button returns to the filter's "fresh construction" state.
      int y = 8;
      y = AddSlider( "Intensity (%)",    0,  100, 50,  () => f.Intensity,          v => f.Intensity          = v, y );
      y = AddSlider( "Beam min (%)",     10, 160, 45,  () => f.BeamWidthMin,       v => f.BeamWidthMin       = v, y );
      y = AddSlider( "Beam max (%)",     10, 160, 115, () => f.BeamWidthMax,       v => f.BeamWidthMax       = v, y );
      y = AddSlider( "Keep bright (%)",  0,  100, 70,  () => f.PreserveBrightness, v => f.PreserveBrightness = v, y );
    }



    private void BuildPhosphorPanel( PhosphorMaskFilter f )
    {
      int y = 8;
      y = AddCombo( "Pattern",
                    new[] { "Aperture grille", "Slot mask", "Dot triad" },
                    () => f.Pattern, v => f.Pattern = v, y );
      y = AddSlider( "Mask scale (px)",   1, 4,   1,  () => f.MaskScale,    v => f.MaskScale    = v, y );
      y = AddSlider( "R boost (%)",       0, 50,  15, () => f.RBoost,       v => f.RBoost       = v, y );
      y = AddSlider( "G boost (%)",       0, 50,  15, () => f.GBoost,       v => f.GBoost       = v, y );
      y = AddSlider( "B boost (%)",       0, 50,  15, () => f.BBoost,       v => f.BBoost       = v, y );
      y = AddSlider( "Non-phase dim (%)", 0, 50,  20, () => f.Dim,          v => f.Dim          = v, y );
      y = AddSlider( "Keep bright (%)",   0, 100, 50, () => f.PreserveLuma, v => f.PreserveLuma = v, y );
    }



    private void BuildBlurPanel( HorizontalBlurFilter f )
    {
      int y = 8;
      y = AddSlider( "Strength (%)", 0, 100, 50, () => f.Strength, v => f.Strength = v, y );
      // Sigma is stored as σ × 10 in SOURCE pixels — 2..40 maps to σ
      // 0.2..4.0 C64 pixels; the kernel scales with zoom automatically.
      y = AddSlider( "Blur σ × 10",  2, 40,  8,  () => f.Sigma,    v => f.Sigma    = v, y );
    }



    private void BuildGammaPanel( GammaAdjustFilter f )
    {
      int y = 8;
      y = AddSlider( "Gamma × 100",    50,   300, 100, () => f.Gamma,      v => f.Gamma      = v, y );
      y = AddSlider( "Brightness (%)", -100, 100, 0,   () => f.Brightness, v => f.Brightness = v, y );
      y = AddSlider( "Contrast (%)",   -100, 100, 0,   () => f.Contrast,   v => f.Contrast   = v, y );
    }



    private void BuildColorTempPanel( ColorTemperatureFilter f )
    {
      int y = 8;
      y = AddSlider( "Temperature", -100, 100, 0, () => f.Temperature, v => f.Temperature = v, y );
      y = AddSlider( "Tint",        -100, 100, 0, () => f.Tint,        v => f.Tint        = v, y );
    }



    private void BuildBarrelPanel( BarrelDistortionFilter f )
    {
      int y = 8;
      y = AddSlider( "Curvature (%)",  0, 100, 25, () => f.Curvature,    v => f.Curvature    = v, y );
      y = AddSlider( "Vignette (%)",   0, 100, 20, () => f.Vignette,     v => f.Vignette     = v, y );
      y = AddSlider( "Corners (%)",    0, 100, 0,  () => f.CornerRadius, v => f.CornerRadius = v, y );
    }



    private void BuildPALPanel( PALCompositeFilter f )
    {
      int y = 8;
      y = AddCombo( "Standard",
                    new[] { "PAL", "NTSC" },
                    () => f.Standard, v => f.Standard = v, y );
      y = AddCombo( "Connection",
                    new[] { "Composite", "S-Video" },
                    () => f.Connection, v => f.Connection = v, y );
      y = AddSlider( "Luma BW ×0.1MHz",   20, 60,  50,  () => f.LumaBandwidth,   v => f.LumaBandwidth   = v, y );
      y = AddSlider( "Chroma BW ×0.1MHz", 5,  20,  13,  () => f.ChromaBandwidth, v => f.ChromaBandwidth = v, y );
      y = AddSlider( "Delay line (%)",    0,  100, 100, () => f.LineBlend,       v => f.LineBlend       = v, y );
      y = AddSlider( "VIC phase err (°)", 0,  15,  4,   () => f.PhaseError,      v => f.PhaseError      = v, y );
      y = AddSlider( "Edge rise ×0.1px",  0,  30,  9,   () => f.EdgeRise,        v => f.EdgeRise        = v, y );
      y = AddSlider( "Edge fall ×0.1px",  0,  30,  2,   () => f.EdgeFall,        v => f.EdgeFall        = v, y );
      y = AddSlider( "Saturation (%)",    50, 200, 100, () => f.Saturation,      v => f.Saturation      = v, y );
      y = AddCombo( "Gamma 2.8 sim",
                    new[] { "Off (palette handles it)", "On" },
                    () => f.GammaSim, v => f.GammaSim = v, y );
    }



    private void BuildVICNoisePanel( VICLumaNoiseFilter f )
    {
      int y = 8;
      y = AddSlider( "Strength (%)", 0, 100, 25, () => f.Strength,   v => f.Strength   = v, y );
      y = AddSlider( "Coarseness",   1, 2,   1,  () => f.Coarseness, v => f.Coarseness = v, y );
    }



    private void BuildBloomPanel( BloomFilter f )
    {
      int y = 8;
      y = AddSlider( "Threshold (%)",     0,  100, 55, () => f.Threshold,        v => f.Threshold        = v, y );
      y = AddSlider( "Bloom (%)",         0,  100, 40, () => f.BloomStrength,    v => f.BloomStrength    = v, y );
      y = AddSlider( "Bloom σ × 10",      5,  40,  15, () => f.BloomRadius,      v => f.BloomRadius      = v, y );
      y = AddSlider( "Halation (%)",      0,  100, 20, () => f.HalationStrength, v => f.HalationStrength = v, y );
      y = AddSlider( "Halation σ × 10",   20, 120, 50, () => f.HalationRadius,   v => f.HalationRadius   = v, y );
    }



    private void BuildConvergencePanel( ConvergenceFilter f )
    {
      int y = 8;
      y = AddSlider( "R shift X ×0.1px", -30, 30, -4, () => f.RShiftX, v => f.RShiftX = v, y );
      y = AddSlider( "R shift Y ×0.1px", -30, 30, 0,  () => f.RShiftY, v => f.RShiftY = v, y );
      y = AddSlider( "B shift X ×0.1px", -30, 30, 4,  () => f.BShiftX, v => f.BShiftX = v, y );
      y = AddSlider( "B shift Y ×0.1px", -30, 30, 0,  () => f.BShiftY, v => f.BShiftY = v, y );
    }



    private void BuildPersistencePanel( PhosphorPersistenceFilter f )
    {
      int y = 8;
      y = AddSlider( "Half-life (ms)", 20, 1000, 90, () => f.HalfLifeMs, v => f.HalfLifeMs = v, y );
      y = AddSlider( "Strength (%)",   0,  100,  70, () => f.Strength,   v => f.Strength   = v, y );
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
          // C64 soft: gentle beam blur, then subtle luminance-modulated
          // scanlines.
          m_Pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 40, Sigma = 7, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter       { Intensity = 35, BeamWidthMin = 50, BeamWidthMax = 120, PreserveBrightness = 70, Enabled = true } );
          break;

        case 2:
          // Sharp CRT: crisp image, pronounced scanlines, aperture grille.
          m_Pipeline.Filters.Add( new HorizontalBlurFilter { Strength = 25, Sigma = 5, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter       { Intensity = 60, BeamWidthMin = 40, BeamWidthMax = 105, PreserveBrightness = 75, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter   { Pattern = PhosphorMaskFilter.PATTERN_APERTURE_GRILLE, RBoost = 20, GBoost = 20, BBoost = 20, Dim = 25, PreserveLuma = 60, Enabled = true } );
          break;

        case 3:
          // CRT Rich: everything chained in physically-motivated order —
          // scene color first, temporal phosphor, then the signal-path
          // effects (blur + convergence), the beam (scanline + bloom),
          // the tube face (mask), and the glass curvature last so
          // everything else warps with it.
          m_Pipeline.Filters.Add( new ColorTemperatureFilter    { Temperature = 15, Tint = 0, Enabled = true } );
          m_Pipeline.Filters.Add( new GammaAdjustFilter         { Gamma = 110, Brightness = 0, Contrast = 10, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorPersistenceFilter { HalfLifeMs = 90, Strength = 60, Enabled = true } );
          m_Pipeline.Filters.Add( new HorizontalBlurFilter      { Strength = 70, Sigma = 9, Enabled = true } );
          m_Pipeline.Filters.Add( new ConvergenceFilter         { RShiftX = -4, RShiftY = 0, BShiftX = 4, BShiftY = 0, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter            { Intensity = 55, BeamWidthMin = 45, BeamWidthMax = 115, PreserveBrightness = 70, Enabled = true } );
          m_Pipeline.Filters.Add( new BloomFilter               { Threshold = 55, BloomStrength = 40, BloomRadius = 15, HalationStrength = 20, HalationRadius = 50, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter        { Pattern = PhosphorMaskFilter.PATTERN_SLOT_MASK, RBoost = 15, GBoost = 15, BBoost = 15, Dim = 15, PreserveLuma = 60, Enabled = true } );
          m_Pipeline.Filters.Add( new BarrelDistortionFilter    { Curvature = 20, Vignette = 25, CornerRadius = 25, Enabled = true } );
          break;

        case 4:
          // PAL TV: the "how it actually looked in the living room" chain.
          // Signal simulation first (it IS the signal), then the tube:
          // afterglow, scanlines, slot mask, curved glass.
          m_Pipeline.Filters.Add( new PALCompositeFilter        { Standard = PALCompositeFilter.STANDARD_PAL, Connection = PALCompositeFilter.CONNECTION_COMPOSITE, LumaBandwidth = 50, ChromaBandwidth = 13, LineBlend = 100, PhaseError = 4, EdgeRise = 9, EdgeFall = 2, Saturation = 105, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorPersistenceFilter { HalfLifeMs = 110, Strength = 65, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter            { Intensity = 45, BeamWidthMin = 50, BeamWidthMax = 120, PreserveBrightness = 70, Enabled = true } );
          m_Pipeline.Filters.Add( new BloomFilter               { Threshold = 60, BloomStrength = 30, BloomRadius = 15, HalationStrength = 25, HalationRadius = 55, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter        { Pattern = PhosphorMaskFilter.PATTERN_SLOT_MASK, RBoost = 12, GBoost = 12, BBoost = 12, Dim = 15, PreserveLuma = 70, Enabled = true } );
          m_Pipeline.Filters.Add( new BarrelDistortionFilter    { Curvature = 18, Vignette = 20, CornerRadius = 30, Enabled = true } );
          break;

        case 5:
          // PAL RF: the antenna-cable childhood memory — softer luma,
          // stronger edge asymmetry, VIC bus noise, more phase ripple.
          m_Pipeline.Filters.Add( new PALCompositeFilter        { Standard = PALCompositeFilter.STANDARD_PAL, Connection = PALCompositeFilter.CONNECTION_COMPOSITE, LumaBandwidth = 35, ChromaBandwidth = 11, LineBlend = 100, PhaseError = 6, EdgeRise = 13, EdgeFall = 3, Saturation = 105, Enabled = true } );
          m_Pipeline.Filters.Add( new VICLumaNoiseFilter        { Strength = 35, Coarseness = 2, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorPersistenceFilter { HalfLifeMs = 110, Strength = 65, Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter            { Intensity = 45, BeamWidthMin = 50, BeamWidthMax = 120, PreserveBrightness = 70, Enabled = true } );
          m_Pipeline.Filters.Add( new BloomFilter               { Threshold = 60, BloomStrength = 35, BloomRadius = 18, HalationStrength = 30, HalationRadius = 60, Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter        { Pattern = PhosphorMaskFilter.PATTERN_SLOT_MASK, RBoost = 12, GBoost = 12, BBoost = 12, Dim = 15, PreserveLuma = 70, Enabled = true } );
          m_Pipeline.Filters.Add( new BarrelDistortionFilter    { Curvature = 20, Vignette = 25, CornerRadius = 30, Enabled = true } );
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
