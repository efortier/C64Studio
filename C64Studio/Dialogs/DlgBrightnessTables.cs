using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Modal editor for the user's Brightness Up / Down chains used by
  /// the Map editor's brightness-shift feature. Two tabs:
  ///
  ///   Linear — single chain of C64 colors. Up walks forward, Down
  ///            walks backward. End-of-chain is a no-op.
  ///   Hue    — list of independent chains, one per hue family
  ///            (greyscale, red, green, blue, etc.). Same Up/Down
  ///            semantics but each chain is independent so a Red
  ///            char advances within the red family, not into a
  ///            different hue.
  ///
  /// Built code-only (no Designer.cs) — the chain editor is an
  /// owner-draw ListBox of color swatches plus a couple of small
  /// dropdowns/buttons; coding it directly is simpler than dragging
  /// owner-draw widgets through the Forms designer.
  ///
  /// Changes are scoped to the dialog: clicking OK commits the edited
  /// chains back into <see cref="Settings.StudioSettings"/>; Cancel
  /// discards. Nothing is auto-applied — the user explicitly opts in.
  /// </summary>
  public class DlgBrightnessTables : Form
  {
    private readonly StudioCore     m_Core;

    // Working copies; flushed back to Core.Settings only on OK.
    private List<int>                                       m_LinearChain;
    private bool                                            m_LinearEnabled;
    private List<StudioSettings.BrightnessHueChain>         m_HueChains;

    // Currently-edited hue chain index (used by the Hue tab's right pane).
    private int                     m_HueSelectedChain = -1;

    private TabControl              m_Tabs;
    // Linear tab controls
    private CheckBox                m_LinearEnabledCheck;
    private ListBox                 m_LinearList;
    private KryptonButton           m_LinearMoveUp;
    private KryptonButton           m_LinearMoveDown;
    private KryptonButton           m_LinearRemove;
    private KryptonComboBox         m_LinearAddCombo;
    private KryptonButton           m_LinearAdd;

    // Hue tab controls
    private ListBox                 m_HueChainsList;
    private KryptonButton           m_HueNewChain;
    private KryptonButton           m_HueDeleteChain;
    private CheckBox                m_HueChainEnabledCheck;   // applies to currently-selected chain
    private ListBox                 m_HueStepsList;
    private KryptonButton           m_HueStepUp;
    private KryptonButton           m_HueStepDown;
    private KryptonButton           m_HueStepRemove;
    private KryptonComboBox         m_HueAddCombo;
    private KryptonButton           m_HueAddStep;

    private Button                  m_Ok;
    private Button                  m_Cancel;



    /// <summary>
    /// Create the dialog. The provided StudioCore is used to read the
    /// current chains (deep-copied into working state) and to access
    /// the C64 palette for swatch rendering. On OK, the working state
    /// is written back to <c>core.Settings.BrightnessLinearChain</c>
    /// and <c>core.Settings.BrightnessHueChains</c>.
    /// </summary>
    public DlgBrightnessTables( StudioCore core )
    {
      m_Core = core;
      Text = "Brightness Tables";
      StartPosition = FormStartPosition.CenterParent;
      MinimizeBox = false;
      MaximizeBox = false;
      FormBorderStyle = FormBorderStyle.Sizable;
      ClientSize = new Size( 640, 460 );
      MinimumSize = new Size( 560, 380 );

      // Deep-copy the chains so Cancel can discard cleanly without
      // having mutated the live settings mid-edit.
      m_LinearChain   = new List<int>( core?.Settings?.BrightnessLinearChain ?? new List<int>() );
      m_LinearEnabled = ( core?.Settings != null ) ? core.Settings.BrightnessLinearEnabled : true;
      m_HueChains = new List<StudioSettings.BrightnessHueChain>();
      if ( core?.Settings?.BrightnessHueChains != null )
      {
        foreach ( var chain in core.Settings.BrightnessHueChains )
        {
          if ( chain == null )
          {
            m_HueChains.Add( new StudioSettings.BrightnessHueChain() );
          }
          else
          {
            m_HueChains.Add( new StudioSettings.BrightnessHueChain(
              chain.Enabled, new List<int>( chain.Steps ?? new List<int>() ) ) );
          }
        }
      }

      BuildLayout();

      // Initial population.
      RefreshLinearList();
      RefreshHueChainsList();
      if ( m_HueChains.Count > 0 )
      {
        m_HueChainsList.SelectedIndex = 0;
      }
      RefreshHueStepsList();
      UpdateButtonStates();

      if ( m_Core?.Theming != null )
      {
        m_Core.Theming.ApplyTheme( this );
      }
    }



    private void BuildLayout()
    {
      m_Tabs = new TabControl
      {
        Dock = DockStyle.Top,
        Height = ClientSize.Height - 50,
      };
      m_Tabs.TabPages.Add( BuildLinearTab() );
      m_Tabs.TabPages.Add( BuildHueTab() );
      Controls.Add( m_Tabs );

      m_Ok = new Button
      {
        Text = "OK",
        DialogResult = DialogResult.OK,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        Size = new Size( 80, 26 ),
        Location = new Point( ClientSize.Width - 175, ClientSize.Height - 36 ),
      };
      m_Ok.Click += ( s, e ) => CommitToSettings();
      Controls.Add( m_Ok );

      m_Cancel = new Button
      {
        Text = "Cancel",
        DialogResult = DialogResult.Cancel,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        Size = new Size( 80, 26 ),
        Location = new Point( ClientSize.Width - 90, ClientSize.Height - 36 ),
      };
      Controls.Add( m_Cancel );

      AcceptButton = m_Ok;
      CancelButton = m_Cancel;

      Resize += ( s, e ) =>
      {
        // Re-anchor the tabs (Dock=Top auto-resizes width but not height
        // — we want it to fill except for the OK/Cancel strip).
        m_Tabs.Height = ClientSize.Height - 50;
      };
    }



    private TabPage BuildLinearTab()
    {
      var page = new TabPage( "Linear" );
      page.Padding = new Padding( 8 );

      var info = new Label
      {
        Text = "A single chain of colors. Pressing Brightness Up walks each char forward through this list; Down walks backward. Reaching the end leaves the char unchanged.",
        Dock = DockStyle.Top,
        Height = 36,
        AutoEllipsis = true,
      };
      page.Controls.Add( info );

      // Enabled checkbox — when unchecked, the Linear toolbar buttons
      // and [/] keyboard shortcuts on the Map tab grey out / become
      // no-ops. Doesn't disable the chain editor itself; you can keep
      // editing the chain even while it's silenced.
      m_LinearEnabledCheck = new CheckBox
      {
        Text = "Enabled (linear up/down active on the Map tab)",
        Location = new Point( 8, 44 ),
        AutoSize = true,
        Checked = m_LinearEnabled,
      };
      m_LinearEnabledCheck.CheckedChanged += ( s, e ) =>
      {
        m_LinearEnabled = m_LinearEnabledCheck.Checked;
      };
      page.Controls.Add( m_LinearEnabledCheck );

      m_LinearList = MakeSwatchListBox();
      m_LinearList.Location = new Point( 8, 70 );
      m_LinearList.Size = new Size( 240, page.ClientSize.Height - 90 );
      m_LinearList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
      m_LinearList.SelectedIndexChanged += ( s, e ) => UpdateButtonStates();
      page.Controls.Add( m_LinearList );

      int btnX = 260;
      int btnY = 70;   // align with the chain list top below the Enabled checkbox
      m_LinearMoveUp = MakeButton( "Move Up", btnX, btnY );
      m_LinearMoveUp.Click += ( s, e ) => MoveSelected( m_LinearList, m_LinearChain, -1, RefreshLinearList );
      page.Controls.Add( m_LinearMoveUp );
      btnY += 32;

      m_LinearMoveDown = MakeButton( "Move Down", btnX, btnY );
      m_LinearMoveDown.Click += ( s, e ) => MoveSelected( m_LinearList, m_LinearChain, +1, RefreshLinearList );
      page.Controls.Add( m_LinearMoveDown );
      btnY += 32;

      m_LinearRemove = MakeButton( "Remove", btnX, btnY );
      m_LinearRemove.Click += ( s, e ) => RemoveSelected( m_LinearList, m_LinearChain, RefreshLinearList );
      page.Controls.Add( m_LinearRemove );
      btnY += 44;

      var addLabel = new Label
      {
        Text = "Add color:",
        Location = new Point( btnX, btnY ),
        Size = new Size( 100, 18 ),
      };
      page.Controls.Add( addLabel );
      btnY += 18;

      m_LinearAddCombo = MakeColorCombo();
      m_LinearAddCombo.Location = new Point( btnX, btnY );
      m_LinearAddCombo.Size = new Size( 130, 24 );
      page.Controls.Add( m_LinearAddCombo );
      btnY += 30;

      m_LinearAdd = MakeButton( "Insert", btnX, btnY );
      m_LinearAdd.Click += ( s, e ) =>
      {
        int colorIdx = m_LinearAddCombo.SelectedIndex;
        if ( ( colorIdx < 0 ) || ( colorIdx >= 16 ) ) return;
        InsertColorAtSelection( m_LinearList, m_LinearChain, colorIdx, RefreshLinearList );
      };
      page.Controls.Add( m_LinearAdd );

      return page;
    }



    private TabPage BuildHueTab()
    {
      var page = new TabPage( "Hue" );
      page.Padding = new Padding( 8 );

      var info = new Label
      {
        Text = "One chain per hue family. Each chain is independent — Brightness Up on a Red char follows the chain that contains Red, regardless of other chains.",
        Dock = DockStyle.Top,
        Height = 36,
        AutoEllipsis = true,
      };
      page.Controls.Add( info );

      // ----- Left pane: list of chains + add/remove -----
      var leftLabel = new Label
      {
        Text = "Chains:",
        Location = new Point( 8, 50 ),
        Size = new Size( 100, 18 ),
      };
      page.Controls.Add( leftLabel );

      m_HueChainsList = MakeSwatchListBox();
      m_HueChainsList.Location = new Point( 8, 70 );
      // Initial size is a placeholder — page.ClientSize.Height isn't
      // reliable at this point because the TabPage hasn't been added
      // to the TabControl yet. Real height is set in the page.Resize
      // handler below, which fires once the page becomes visible.
      m_HueChainsList.Size = new Size( 220, 200 );
      m_HueChainsList.ItemHeight = 28;
      m_HueChainsList.DrawMode = DrawMode.OwnerDrawFixed;
      // The chains list draws each row as a strip of swatches summarising
      // the chain — we replace the default swatch-row drawer with one
      // that paints up to N color squares.
      m_HueChainsList.DrawItem -= LinearListDrawItem;
      m_HueChainsList.DrawItem += HueChainsListDrawItem;
      m_HueChainsList.SelectedIndexChanged += ( s, e ) =>
      {
        m_HueSelectedChain = m_HueChainsList.SelectedIndex;
        RefreshHueStepsList();
        UpdateButtonStates();
      };
      page.Controls.Add( m_HueChainsList );

      // New/Delete chain buttons live at the bottom of the LEFT pane.
      // Initial Y is a placeholder; page.Resize repositions them.
      m_HueNewChain = MakeButton( "New chain", 8, 200 );
      m_HueNewChain.Click += ( s, e ) =>
      {
        m_HueChains.Add( new StudioSettings.BrightnessHueChain( true, new List<int>() ) );
        RefreshHueChainsList();
        m_HueChainsList.SelectedIndex = m_HueChains.Count - 1;
        UpdateButtonStates();
      };
      page.Controls.Add( m_HueNewChain );

      m_HueDeleteChain = MakeButton( "Delete chain", 110, 200 );
      m_HueDeleteChain.Click += ( s, e ) =>
      {
        if ( ( m_HueSelectedChain < 0 ) || ( m_HueSelectedChain >= m_HueChains.Count ) ) return;
        m_HueChains.RemoveAt( m_HueSelectedChain );
        m_HueSelectedChain = -1;
        RefreshHueChainsList();
        RefreshHueStepsList();
        UpdateButtonStates();
      };
      page.Controls.Add( m_HueDeleteChain );

      // ----- Right pane: chain editor (same widget shape as Linear) -----
      // Per-chain Enabled checkbox sits at the top of the right pane.
      // Toggling it flips the active chain's Enabled flag — disabled
      // chains are skipped when computing Hue Up/Down at runtime.
      m_HueChainEnabledCheck = new CheckBox
      {
        Text = "Chain enabled",
        Location = new Point( 240, 48 ),
        AutoSize = true,
      };
      m_HueChainEnabledCheck.CheckedChanged += ( s, e ) =>
      {
        var c = ActiveHueChainObj();
        if ( c == null ) return;
        c.Enabled = m_HueChainEnabledCheck.Checked;
        // Repaint the chains list so the disabled chain's row visibly
        // dims (HueChainsListDrawItem reads Enabled).
        m_HueChainsList.Invalidate();
      };
      page.Controls.Add( m_HueChainEnabledCheck );

      var rightLabel = new Label
      {
        Text = "Steps in selected chain:",
        Location = new Point( 240, 70 ),
        Size = new Size( 200, 18 ),
      };
      page.Controls.Add( rightLabel );

      m_HueStepsList = MakeSwatchListBox();
      m_HueStepsList.Location = new Point( 240, 90 );
      m_HueStepsList.Size = new Size( 200, 200 );   // placeholder; resized in page.Resize
      m_HueStepsList.SelectedIndexChanged += ( s, e ) => UpdateButtonStates();
      page.Controls.Add( m_HueStepsList );

      int btnX = 460;
      int btnY = 90;   // align with the steps list top
      m_HueStepUp = MakeButton( "Move Up", btnX, btnY );
      m_HueStepUp.Click += ( s, e ) =>
      {
        var chain = ActiveHueChain();
        if ( chain == null ) return;
        MoveSelected( m_HueStepsList, chain, -1, RefreshHueStepsList );
        RefreshHueChainsList();   // chain summary on left needs refresh
      };
      page.Controls.Add( m_HueStepUp );
      btnY += 32;

      m_HueStepDown = MakeButton( "Move Down", btnX, btnY );
      m_HueStepDown.Click += ( s, e ) =>
      {
        var chain = ActiveHueChain();
        if ( chain == null ) return;
        MoveSelected( m_HueStepsList, chain, +1, RefreshHueStepsList );
        RefreshHueChainsList();
      };
      page.Controls.Add( m_HueStepDown );
      btnY += 32;

      m_HueStepRemove = MakeButton( "Remove", btnX, btnY );
      m_HueStepRemove.Click += ( s, e ) =>
      {
        var chain = ActiveHueChain();
        if ( chain == null ) return;
        RemoveSelected( m_HueStepsList, chain, RefreshHueStepsList );
        RefreshHueChainsList();
      };
      page.Controls.Add( m_HueStepRemove );
      btnY += 44;

      var addLabel2 = new Label
      {
        Text = "Add color:",
        Location = new Point( btnX, btnY ),
        Size = new Size( 100, 18 ),
      };
      page.Controls.Add( addLabel2 );
      btnY += 18;

      m_HueAddCombo = MakeColorCombo();
      m_HueAddCombo.Location = new Point( btnX, btnY );
      m_HueAddCombo.Size = new Size( 130, 24 );
      page.Controls.Add( m_HueAddCombo );
      btnY += 30;

      m_HueAddStep = MakeButton( "Insert", btnX, btnY );
      m_HueAddStep.Click += ( s, e ) =>
      {
        var chain = ActiveHueChain();
        if ( chain == null ) return;
        int colorIdx = m_HueAddCombo.SelectedIndex;
        if ( ( colorIdx < 0 ) || ( colorIdx >= 16 ) ) return;
        InsertColorAtSelection( m_HueStepsList, chain, colorIdx, RefreshHueStepsList );
        RefreshHueChainsList();
      };
      page.Controls.Add( m_HueAddStep );

      // Re-layout on page resize. The TabPage's ClientSize isn't valid
      // at construction time (it's not yet added to the TabControl), so
      // we have to defer placement of the bottom-anchored buttons and
      // the Top|Bottom-anchored lists. WinForms' Anchor system would
      // do this automatically IF we'd seeded correct initial sizes —
      // since we can't, this handler runs once on first layout (and on
      // every subsequent resize) and explicitly positions everything.
      EventHandler relayoutHue = ( s, e ) =>
      {
        const int BottomBtnHeight = 26;
        const int BottomMargin    = 8;
        // Name distinct from the right-pane btnY local in the
        // enclosing scope to avoid CS0136 on net4.8.
        int bottomBtnY = page.ClientSize.Height - BottomBtnHeight - BottomMargin;
        int listBottom = bottomBtnY - 6;   // 6px gap above the bottom buttons
        // Chain list (left pane) starts at y=70; the steps list (right
        // pane) starts at y=90 because the per-chain Enabled checkbox
        // sits at y=70 above it.
        int chainListH = System.Math.Max( 40, listBottom - 70 );
        int stepsListH = System.Math.Max( 40, listBottom - 90 );

        m_HueChainsList.Size = new Size( 220, chainListH );
        m_HueStepsList.Size  = new Size( 200, stepsListH );
        m_HueNewChain.Location    = new Point( 8, bottomBtnY );
        m_HueDeleteChain.Location = new Point( 110, bottomBtnY );
      };
      page.Resize += relayoutHue;
      // Run once at first paint — Resize doesn't necessarily fire on
      // first display if the page was already sized when added to the
      // TabControl, so prime it via HandleCreated.
      page.HandleCreated += ( s, e ) => relayoutHue( s, e );

      return page;
    }



    private StudioSettings.BrightnessHueChain ActiveHueChainObj()
    {
      if ( ( m_HueSelectedChain < 0 ) || ( m_HueSelectedChain >= m_HueChains.Count ) )
      {
        return null;
      }
      return m_HueChains[m_HueSelectedChain];
    }

    private List<int> ActiveHueChain()
    {
      var c = ActiveHueChainObj();
      return ( c != null ) ? c.Steps : null;
    }



    private ListBox MakeSwatchListBox()
    {
      var lb = new ListBox
      {
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 22,
        IntegralHeight = false,
      };
      lb.DrawItem += LinearListDrawItem;
      return lb;
    }



    private KryptonComboBox MakeColorCombo()
    {
      var combo = new KryptonComboBox
      {
        DropDownStyle = ComboBoxStyle.DropDownList,
        ItemHeight = 22,
      };
      // 16 entries — color indices 0..15. Display is the swatch + index.
      for ( int i = 0; i < 16; ++i )
      {
        combo.Items.Add( i );
      }
      combo.SelectedIndex = 0;
      // KryptonComboBox dispatches owner-draw via the INNER WinForms
      // ComboBox, not the Krypton wrapper. Hooking combo.DrawItem here
      // (the wrapper) silently does nothing — the dropdown items render
      // as plain numbers. Routing through combo.ComboBox.DrawItem
      // matches the WireOwnerDrawCombo helper used in MapEditor.cs.
      if ( combo.ComboBox != null )
      {
        combo.ComboBox.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ComboBox.DrawItem += ColorComboDrawItem;
      }
      return combo;
    }



    private KryptonButton MakeButton( string text, int x, int y )
    {
      return new KryptonButton
      {
        Text = text,
        Location = new Point( x, y ),
        Size = new Size( 95, 26 ),
      };
    }



    /// <summary>
    /// Owner-draw painter for any of the chain ListBoxes. Each item
    /// holds an int (the C64 color index 0..15); we paint a swatch
    /// followed by the color number.
    /// </summary>
    private void LinearListDrawItem( object sender, DrawItemEventArgs e )
    {
      ListBox lb = (ListBox)sender;
      e.DrawBackground();
      if ( ( e.Index < 0 ) || ( e.Index >= lb.Items.Count ) )
      {
        return;
      }
      int colorIdx = ToColorIndex( lb.Items[e.Index] );
      DrawColorRow( e.Graphics, e.Bounds, colorIdx, lb.ForeColor );
      e.DrawFocusRectangle();
    }



    /// <summary>
    /// Owner-draw painter for the Hue chains list — each row shows a
    /// summary of the chain's contents as a strip of swatches with a
    /// "{i+1}: " label prefix. Disabled chains render dimmed (50%
    /// alpha approximation by mixing the swatch fill with the row
    /// background) so the user can see at a glance which chains are
    /// active.
    /// </summary>
    private void HueChainsListDrawItem( object sender, DrawItemEventArgs e )
    {
      ListBox lb = (ListBox)sender;
      e.DrawBackground();
      if ( ( e.Index < 0 ) || ( e.Index >= m_HueChains.Count ) )
      {
        return;
      }
      var chain = m_HueChains[e.Index];
      bool enabled = ( chain != null ) && chain.Enabled;

      // Label colour matches the row's text color; for disabled
      // chains, prepend a "(off)" marker so the disabled state is
      // discoverable without relying purely on swatch-dimming.
      using ( var brush = new SolidBrush( lb.ForeColor ) )
      {
        string label = ( e.Index + 1 ).ToString() + ":";
        if ( !enabled ) label = "(off) " + label;
        e.Graphics.DrawString( label, lb.Font, brush,
                               e.Bounds.X + 4, e.Bounds.Y + 6 );
      }

      int LabelWidth = enabled ? 28 : 60;
      int swatchW = 18;
      int swatchH = e.Bounds.Height - 6;
      int x = e.Bounds.X + LabelWidth;
      int y = e.Bounds.Y + 3;
      int maxX = e.Bounds.Right - 4;
      if ( ( chain != null ) && ( chain.Steps != null ) )
      {
        var steps = chain.Steps;
        for ( int i = 0; i < steps.Count; ++i )
        {
          if ( x + swatchW > maxX ) break;
          DrawSwatch( e.Graphics, x, y, swatchW, swatchH, steps[i], enabled );
          x += swatchW + 2;
        }
      }
      e.DrawFocusRectangle();
    }



    /// <summary>
    /// Owner-draw painter for the "add color" combos. Each item is
    /// the C64 color index (0..15) shown as a swatch + numeric label.
    /// </summary>
    private void ColorComboDrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;
      e.DrawBackground();
      if ( ( e.Index < 0 ) || ( e.Index >= combo.Items.Count ) )
      {
        return;
      }
      int colorIdx = ToColorIndex( combo.Items[e.Index] );
      DrawColorRow( e.Graphics, e.Bounds, colorIdx, combo.ForeColor );
      e.DrawFocusRectangle();
    }



    private void DrawColorRow( Graphics g, Rectangle bounds, int colorIdx, Color textColor )
    {
      using ( var brush = new SolidBrush( textColor ) )
      {
        g.DrawString( colorIdx.ToString( "00" ), Font, brush,
                      bounds.X + 4, bounds.Y + 3 );
      }
      const int IndexColumnWidth = 26;
      const int RightMargin = 4;
      int sx = bounds.X + IndexColumnWidth;
      int sy = bounds.Y + 2;
      int sw = bounds.Width - IndexColumnWidth - RightMargin;
      int sh = bounds.Height - 4;
      if ( sw < 1 ) sw = 1;
      if ( sh < 1 ) sh = 1;
      DrawSwatch( g, sx, sy, sw, sh, colorIdx );
    }



    private void DrawSwatch( Graphics g, int x, int y, int w, int h, int colorIdx )
    {
      DrawSwatch( g, x, y, w, h, colorIdx, true );
    }

    /// <summary>
    /// Variant that dims the swatch by ~50% when <paramref name="enabled"/>
    /// is false — used for disabled hue chains so the user can see at a
    /// glance which chains are off without needing to read the (off)
    /// label prefix.
    /// </summary>
    private void DrawSwatch( Graphics g, int x, int y, int w, int h, int colorIdx, bool enabled )
    {
      Color fill = ResolveColor( colorIdx );
      if ( !enabled )
      {
        // Mix toward mid-grey for the disabled look. Cheap and works
        // against any background theme.
        fill = Color.FromArgb(
          ( fill.R + 96 ) / 2,
          ( fill.G + 96 ) / 2,
          ( fill.B + 96 ) / 2 );
      }
      using ( var b = new SolidBrush( fill ) )
      {
        g.FillRectangle( b, x, y, w, h );
      }
      using ( var p = new Pen( Color.Black ) )
      {
        g.DrawRectangle( p, x, y, w - 1, h - 1 );
      }
    }



    /// <summary>
    /// Resolve a C64 color index (0..15) to an ARGB Color via the first
    /// available palette source: a currently-loaded MapProject's charset
    /// palette would be ideal, but we don't have access to one here, so
    /// we use the global PaletteManager. Falls back to a hardcoded
    /// approximation if the palette can't be resolved (defensive — the
    /// dialog should still be usable on a fresh install).
    /// </summary>
    private static Color ResolveColor( int colorIdx )
    {
      if ( ( colorIdx < 0 ) || ( colorIdx >= 16 ) ) return Color.Magenta;
      try
      {
        // PaletteManager.PaletteFromMode returns a Palette; its
        // ColorValues hold uint ARGB. Use the default palette mode.
        var pal = RetroDevStudio.ConstantData.Palette;
        if ( ( pal != null ) && ( colorIdx < pal.NumColors ) )
        {
          uint argb = pal.ColorValues[colorIdx];
          return Color.FromArgb( unchecked( (int)argb ) );
        }
      }
      catch
      {
        // Fall through to the hardcoded fallback.
      }
      // Hardcoded Pepto ARGB fallback (same standard 16 colors).
      uint[] fallback = {
        0xff000000, 0xffffffff, 0xff883932, 0xff67b6bd,
        0xff8b3f96, 0xff55a049, 0xff40318d, 0xffbfce72,
        0xff8b5429, 0xff574200, 0xffbb776d, 0xff545454,
        0xff808080, 0xffacea88, 0xff7e70ca, 0xffababab,
      };
      return Color.FromArgb( unchecked( (int)fallback[colorIdx] ) );
    }



    private static int ToColorIndex( object item )
    {
      try { return Convert.ToInt32( item ); }
      catch { return 0; }
    }



    // ----------------------------------------------------------------
    // Chain manipulation primitives shared by Linear and Hue chain
    // editor buttons.
    // ----------------------------------------------------------------

    private static void MoveSelected( ListBox lb, List<int> chain, int delta, Action refresh )
    {
      int idx = lb.SelectedIndex;
      int newIdx = idx + delta;
      if ( ( idx < 0 ) || ( newIdx < 0 ) || ( newIdx >= chain.Count ) ) return;
      int v = chain[idx];
      chain.RemoveAt( idx );
      chain.Insert( newIdx, v );
      refresh();
      lb.SelectedIndex = newIdx;
    }

    private static void RemoveSelected( ListBox lb, List<int> chain, Action refresh )
    {
      int idx = lb.SelectedIndex;
      if ( ( idx < 0 ) || ( idx >= chain.Count ) ) return;
      chain.RemoveAt( idx );
      refresh();
      // Re-select something near the deleted slot for continuity.
      if ( chain.Count == 0 ) return;
      int newSel = idx;
      if ( newSel >= chain.Count ) newSel = chain.Count - 1;
      lb.SelectedIndex = newSel;
    }

    private static void InsertColorAtSelection( ListBox lb, List<int> chain, int colorIdx, Action refresh )
    {
      int idx = lb.SelectedIndex;
      // Insert AFTER the selected entry; or append if nothing selected.
      int insertAt = ( idx < 0 ) ? chain.Count : ( idx + 1 );
      chain.Insert( insertAt, colorIdx );
      refresh();
      lb.SelectedIndex = insertAt;
    }



    // ----------------------------------------------------------------
    // Refresh helpers — re-populate ListBox items from working chains.
    // ----------------------------------------------------------------

    private void RefreshLinearList()
    {
      int prevSel = m_LinearList.SelectedIndex;
      m_LinearList.BeginUpdate();
      m_LinearList.Items.Clear();
      foreach ( int c in m_LinearChain )
      {
        m_LinearList.Items.Add( c );
      }
      m_LinearList.EndUpdate();
      if ( prevSel >= m_LinearList.Items.Count ) prevSel = m_LinearList.Items.Count - 1;
      if ( prevSel >= 0 ) m_LinearList.SelectedIndex = prevSel;
      UpdateButtonStates();
    }

    private void RefreshHueChainsList()
    {
      int prevSel = m_HueChainsList.SelectedIndex;
      m_HueChainsList.BeginUpdate();
      m_HueChainsList.Items.Clear();
      for ( int i = 0; i < m_HueChains.Count; ++i )
      {
        m_HueChainsList.Items.Add( i );   // index, painter reads m_HueChains
      }
      m_HueChainsList.EndUpdate();
      if ( prevSel >= m_HueChainsList.Items.Count ) prevSel = m_HueChainsList.Items.Count - 1;
      if ( prevSel >= 0 )
      {
        m_HueChainsList.SelectedIndex = prevSel;
        m_HueSelectedChain = prevSel;
      }
      else
      {
        m_HueSelectedChain = -1;
      }
      UpdateButtonStates();
    }

    private void RefreshHueStepsList()
    {
      int prevSel = m_HueStepsList.SelectedIndex;
      m_HueStepsList.BeginUpdate();
      m_HueStepsList.Items.Clear();
      var chain = ActiveHueChain();
      if ( chain != null )
      {
        foreach ( int c in chain )
        {
          m_HueStepsList.Items.Add( c );
        }
      }
      m_HueStepsList.EndUpdate();
      if ( prevSel >= m_HueStepsList.Items.Count ) prevSel = m_HueStepsList.Items.Count - 1;
      if ( prevSel >= 0 ) m_HueStepsList.SelectedIndex = prevSel;

      // Sync the per-chain Enabled checkbox to the active chain. Wrap
      // in a hold so the CheckedChanged handler doesn't re-fire and
      // ricochet (it'd just write the same value back, but cleaner to
      // suppress).
      if ( m_HueChainEnabledCheck != null )
      {
        var c = ActiveHueChainObj();
        m_HueChainEnabledCheck.Enabled = ( c != null );
        if ( c != null )
        {
          if ( m_HueChainEnabledCheck.Checked != c.Enabled )
          {
            m_HueChainEnabledCheck.Checked = c.Enabled;
          }
        }
      }
      UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
      // Linear
      int linIdx = ( m_LinearList != null ) ? m_LinearList.SelectedIndex : -1;
      if ( m_LinearMoveUp   != null ) m_LinearMoveUp.Enabled   = ( linIdx > 0 );
      if ( m_LinearMoveDown != null ) m_LinearMoveDown.Enabled = ( linIdx >= 0 ) && ( linIdx < m_LinearChain.Count - 1 );
      if ( m_LinearRemove   != null ) m_LinearRemove.Enabled   = ( linIdx >= 0 );

      // Hue
      bool haveChain = ( m_HueSelectedChain >= 0 ) && ( m_HueSelectedChain < m_HueChains.Count );
      var activeChain = ActiveHueChain();
      int hueStepIdx = ( m_HueStepsList != null ) ? m_HueStepsList.SelectedIndex : -1;
      if ( m_HueDeleteChain != null ) m_HueDeleteChain.Enabled = haveChain;
      if ( m_HueStepUp      != null ) m_HueStepUp.Enabled      = haveChain && ( hueStepIdx > 0 );
      if ( m_HueStepDown    != null ) m_HueStepDown.Enabled    = haveChain && ( hueStepIdx >= 0 ) && ( activeChain != null ) && ( hueStepIdx < activeChain.Count - 1 );
      if ( m_HueStepRemove  != null ) m_HueStepRemove.Enabled  = haveChain && ( hueStepIdx >= 0 );
      if ( m_HueAddStep     != null ) m_HueAddStep.Enabled     = haveChain;
      if ( m_HueAddCombo    != null ) m_HueAddCombo.Enabled    = haveChain;
    }



    private void CommitToSettings()
    {
      if ( m_Core?.Settings == null ) return;
      // Replace the lists wholesale rather than mutating in place — the
      // computed Up/Down properties read from these lists, so a clean
      // swap guarantees the next call sees the new state. Deep-copy to
      // detach from the dialog's working state (so any post-OK keystroke
      // ricochet can't corrupt settings).
      m_Core.Settings.BrightnessLinearChain   = new List<int>( m_LinearChain );
      m_Core.Settings.BrightnessLinearEnabled = m_LinearEnabled;
      var hueOut = new List<StudioSettings.BrightnessHueChain>( m_HueChains.Count );
      foreach ( var ch in m_HueChains )
      {
        hueOut.Add( new StudioSettings.BrightnessHueChain(
          ch.Enabled,
          new List<int>( ch.Steps ?? new List<int>() ) ) );
      }
      m_Core.Settings.BrightnessHueChains = hueOut;
    }

  }
}
