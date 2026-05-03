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
        int prevIndex = SelectedListIndex;
        m_ListFilters.BeginUpdate();
        m_ListFilters.Items.Clear();
        foreach ( var f in m_Pipeline.Filters )
        {
          var item = new ListViewItem( f.Name ) { Checked = f.Enabled };
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
      m_Pipeline.Filters[idx].Enabled = item.Checked;
      NotifyChanged();
    }

    private void OnListItemCheckedHandler( object sender, ItemCheckedEventArgs e )
    {
      OnListItemChecked( e.Item );
    }



    private void MoveSelected( int delta )
    {
      int idx = SelectedListIndex;
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
      m_PanelParams.SuspendLayout();
      try
      {
        m_PanelParams.Controls.Clear();
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

      value = new KryptonLabel
      {
        Location = new Point( 332, y ),
        Size     = new Size( 40, 20 ),
      };
      value.Values.Text = get().ToString();
      m_PanelParams.Controls.Add( value );

      slider = new KryptonTrackBar
      {
        Location      = new Point( 132, y - 2 ),
        Size          = new Size( 196, 30 ),
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
        Location = new Point( 374, y - 1 ),
        Size     = new Size( 22, 22 ),
      };
      resetBtn.Values.Text = "↺";
      // KryptonButton picks up the global palette automatically, so no
      // per-control Theming call is needed for the reset glyph to render
      // in dark mode.
      var tip = new ToolTip();
      tip.SetToolTip( resetBtn, "Reset to " + defaultValue );
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



    private void BuildScanlinePanel( ScanlineFilter f )
    {
      // Defaults mirror the ScanlineFilter field initializers so the reset
      // button returns to the filter's "fresh construction" state.
      int y = 8;
      y = AddSlider( "Intensity (%)", 0, 100, 25, () => f.Intensity, v => f.Intensity = v, y );
      y = AddSlider( "Period (px)",   2, 16,  2,  () => f.Period,    v => f.Period    = v, y );
      y = AddSlider( "Row offset",    0, 15,  0,  () => f.Offset,    v => f.Offset    = v, y );
    }



    private void BuildPhosphorPanel( PhosphorMaskFilter f )
    {
      int y = 8;
      y = AddSlider( "R boost (%)",       0, 50, 15, () => f.RBoost, v => f.RBoost = v, y );
      y = AddSlider( "G boost (%)",       0, 50, 15, () => f.GBoost, v => f.GBoost = v, y );
      y = AddSlider( "B boost (%)",       0, 50, 15, () => f.BBoost, v => f.BBoost = v, y );
      y = AddSlider( "Non-phase dim (%)", 0, 50, 20, () => f.Dim,    v => f.Dim    = v, y );
    }



    private void BuildBlurPanel( HorizontalBlurFilter f )
    {
      int y = 8;
      y = AddSlider( "Strength (%)", 0, 100, 50, () => f.Strength, v => f.Strength = v, y );
      // Taps slider steps by 1 but the filter clamps to odd on apply, so
      // even values just fall back to the next-lower odd count. That's
      // simpler than making the slider step-by-2 and easier to explain.
      y = AddSlider( "Taps (odd)",   3, 15,  7,  () => f.Taps,     v => f.Taps     = v, y );
      // Sigma slider is stored as σ × 10 — 5..30 maps to σ 0.5..3.0. The
      // *10 trick avoids a float slider; the filter divides by 10 on use.
      y = AddSlider( "Blur σ × 10",  5, 30,  12, () => f.Sigma,    v => f.Sigma    = v, y );
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
      y = AddSlider( "Curvature (%)", 0, 100, 25, () => f.Curvature, v => f.Curvature = v, y );
      y = AddSlider( "Vignette (%)",  0, 100, 20, () => f.Vignette,  v => f.Vignette  = v, y );
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

        case 3:
          // CRT Rich: everything chained in physically-motivated order —
          // scene-referred color first, then the beam effects (blur +
          // scanline + phosphor), then the glass curvature last so
          // everything else warps with it. Values are a starting point
          // drawn roughly from Retro-Crisis / Guest Advanced parameter
          // ranges; expect to tune per-map for taste.
          m_Pipeline.Filters.Add( new ColorTemperatureFilter { Temperature = 15, Tint = 0,              Enabled = true } );
          m_Pipeline.Filters.Add( new GammaAdjustFilter      { Gamma = 110, Brightness = 0, Contrast = 10, Enabled = true } );
          m_Pipeline.Filters.Add( new HorizontalBlurFilter   { Strength = 80, Taps = 11, Sigma = 20,    Enabled = true } );
          m_Pipeline.Filters.Add( new ScanlineFilter         { Intensity = 45, Period = 2, Offset = 0,  Enabled = true } );
          m_Pipeline.Filters.Add( new PhosphorMaskFilter     { RBoost = 15, GBoost = 15, BBoost = 15, Dim = 15, Enabled = true } );
          m_Pipeline.Filters.Add( new BarrelDistortionFilter { Curvature = 20, Vignette = 25,           Enabled = true } );
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
