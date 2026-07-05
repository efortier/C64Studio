using RetroDevStudio.Displayer;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RetroDevStudio.Formats;
using GR.Memory;
using RetroDevStudio.Types;
using RetroDevStudio.Converter;
using RetroDevStudio.Controls;
using RetroDevStudio.Dialogs;
using System.Drawing;
using GR.Generic;
using GR.Image;
using GR.Collections;
using System.Linq;

namespace RetroDevStudio.Documents
{
    public partial class SpriteEditor : BaseDocument
    {
        enum ToolMode
        {
            SINGLE_PIXEL,
            FILL
        }

        private int m_CurrentSprite = 0;

        private string m_ImportError = "";

        private bool m_IsSpriteProject = true;

        private Formats.SpriteProject m_SpriteProject = new RetroDevStudio.Formats.SpriteProject();

        // Overlay-tab state. m_CurrentOverlay tracks the user-selected overlay
        // for the new multi-overlay UI (Phase 2). The 8 slot-row arrays hold
        // the per-row controls created by BuildOverlaySlotRows() at construction
        // time — one set per fixed slot 0..7.
        private Formats.SpriteProject.Overlay m_CurrentOverlay = null;
        private System.Windows.Forms.CheckBox[] m_SlotEnabled = new System.Windows.Forms.CheckBox[8];
        private System.Windows.Forms.NumericUpDown[] m_SlotX = new System.Windows.Forms.NumericUpDown[8];
        private System.Windows.Forms.NumericUpDown[] m_SlotY = new System.Windows.Forms.NumericUpDown[8];
        private System.Windows.Forms.NumericUpDown[] m_SlotBank = new System.Windows.Forms.NumericUpDown[8];
        private System.Windows.Forms.ComboBox[] m_SlotCustomColor = new System.Windows.Forms.ComboBox[8];

        // Bank-to-bank in-process clipboard (Phase 3). Cleared on Copy with
        // the multi-selection's deep-cloned SpriteData. Paste writes them
        // sequentially starting at the first selected bank index, wrapping
        // would overwrite existing entries — guarded by the Sprites.Count.
        private List<Formats.SpriteProject.SpriteData> m_BankClipboard = new List<Formats.SpriteProject.SpriteData>();

        // Animation tab (Phase 4). m_CurrentFrame is the user-selected frame
        // in the current overlay's Frames list; the 8 NUDs hold the per-slot
        // bank index for that frame. Playback advances one frame per timer tick;
        // the timer interval is the overlay's single FrameDelay (1/50th sec) in ms.
        private Formats.SpriteProject.OverlayFrame m_CurrentFrame = null;
        private System.Windows.Forms.NumericUpDown[] m_FrameSlotBank = new System.Windows.Forms.NumericUpDown[8];
        private System.Windows.Forms.Timer m_OverlayAnimTimer = new System.Windows.Forms.Timer();
        private int m_OverlayAnimFramePos = 0;

        // Preview zoom factors. The picture box stretches its DisplayPage
        // to fill the client area, so a smaller page = larger apparent
        // sprites. Default 1x means the page is the box's client size and
        // sprites render 1:1; at 2x the page is half-size and stretches up.
        private int m_OverlayPreviewZoom = 1;
        private int m_AnimPreviewZoom = 1;

        // Sprite-test panel (Overlay tab): click-to-play concurrent overlay
        // animations. A SEPARATE timer + instance list, fully independent of the
        // picAnimPreview playback (m_OverlayAnim* / picAnimPreview) so the two
        // never interfere. The engine only READS m_CurrentOverlay and
        // m_AnimPreviewZoom; it never touches the animation-preview state.
        private System.Windows.Forms.Timer m_SpriteTestTimer = new System.Windows.Forms.Timer();
        private System.Collections.Generic.List<SpriteTestInstance> m_TestInstances = new System.Collections.Generic.List<SpriteTestInstance>();

        // One running animation in the test panel. Position is kept in panel/screen
        // pixels so changing the animation zoom mid-play re-maps cleanly at render.
        private sealed class SpriteTestInstance
        {
            public int ScreenX;
            public int ScreenY;
            public int FramePos;     // index into m_CurrentOverlay.Frames
            public int FrameTicks;   // accumulated ms toward the animation's frame delay
        }

        private bool m_ButtonReleased = false;

        private ToolMode m_Mode = ToolMode.SINGLE_PIXEL;

        public int m_SpriteWidth = 24;
        public int m_SpriteHeight = 21;

        private int m_SpriteEditorOrigWidth = -1;
        private int m_SpriteEditorOrigHeight = -1;

        private ColorSettingsBase _ColorSettingsDlg = null;

        private System.Drawing.Font m_DefaultOutputFont = null;
        private ExportSpriteFormBase m_ExportForm = null;
        private ImportSpriteFormBase m_ImportForm = null;



        public SpriteEditor(StudioCore Core)
        {
            this.Core = Core;
            DocumentInfo.Type = ProjectElement.ElementType.SPRITE_SET;
            DocumentInfo.UndoManager.MainForm = Core.MainForm;

            m_IsSaveable = true;
            InitializeComponent();
            SuspendLayout();

            GR.Image.DPIHandler.ResizeControlsForDPI(this);

            c64HiResMultiColorToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC;
            mega65_16x2116ColorsToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.MEGA65_16_X_21_16_COLORS;
            mega65_24x214ColorsToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC;
            mega65_64x214ColorsToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.MEGA65_64_X_21_HIRES_OR_MC;
            x16_8x8ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_8_16_COLORS;
            x16_16x8ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_8_16_COLORS;
            x16_32x8ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_8_16_COLORS;
            x16_64x8ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_8_16_COLORS;
            x16_8x16ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_16_16_COLORS;
            x16_16x16ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_16_16_COLORS;
            x16_32x16ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_16_16_COLORS;
            x16_64x16ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_16_16_COLORS;
            x16_8x32ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_32_16_COLORS;
            x16_16x32ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_32_16_COLORS;
            x16_32x32ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_32_16_COLORS;
            x16_64x32ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_32_16_COLORS;
            x16_8x64ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_64_16_COLORS;
            x16_16x64ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_64_16_COLORS;
            x16_32x64ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_64_16_COLORS;
            x16_64x64ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_64_16_COLORS;
            x16_8x8x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_8_256_COLORS;
            x16_16x8x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_8_256_COLORS;
            x16_32x8x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_8_256_COLORS;
            x16_64x8x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_8_256_COLORS;
            x16_8x16x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_16_256_COLORS;
            x16_16x16x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_16_256_COLORS;
            x16_32x16x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_16_256_COLORS;
            x16_64x16x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_16_256_COLORS;
            x16_8x32x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_32_256_COLORS;
            x16_16x32x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_32_256_COLORS;
            x16_32x32x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_32_256_COLORS;
            x16_64x32x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_32_256_COLORS;
            x16_8x64x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_8_64_256_COLORS;
            x16_16x64x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_16_64_256_COLORS;
            x16_32x64x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_32_64_256_COLORS;
            x16_64x64x256ToolStripMenuItem.Tag = SpriteProject.SpriteProjectMode.COMMANDER_X16_64_64_256_COLORS;

            m_DefaultOutputFont = editDataExport.Font;
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("as assembly", typeof(ExportSpriteAsAssembly)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("as BASIC DATA statements", typeof(ExportSpriteAsBASICData)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("to binary file", typeof(ExportSpriteAsBinaryFile)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("to image file", typeof(ExportSpriteAsImageFile)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("to image (clipboard)", typeof(ExportSpriteAsImage)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("to Mega65 S-BASIC Spritedef", typeof(ExportSpriteAsSBASICFCSpritedef)));
            comboExportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("as game binary", typeof(ExportSpriteAsGameBinary)));
            // Build the default export form WITHOUT going through the change
            // handler, so constructing the editor never marks the document
            // modified. The handler is reattached for genuine user selections.
            comboExportMethod.SelectedIndexChanged -= comboExportMethod_SelectedIndexChanged;
            comboExportMethod.SelectedIndex = 0;
            comboExportMethod.SelectedIndexChanged += comboExportMethod_SelectedIndexChanged;
            RebuildExportForm();

            comboImportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("from assembly", typeof(ImportSpriteFromASM)));
            comboImportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("from BASIC DATA statements", typeof(ImportSpriteFromBASICDATA)));
            comboImportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("from HEX", typeof(ImportSpriteFromHEX)));
            comboImportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("from sprite set/binary file", typeof(ImportSpriteFromBinaryFile)));
            comboImportMethod.Items.Add(new GR.Generic.Tupel<string, Type>("from image file", typeof(ImportSpriteFromImageFile)));
            comboImportMethod.SelectedIndex = 0;

            m_SpriteEditorOrigWidth = pictureEditor.ClientSize.Width;
            m_SpriteEditorOrigHeight = pictureEditor.ClientSize.Height;

            pictureEditor.DisplayPage.Create(m_SpriteWidth, m_SpriteHeight, GR.Drawing.PixelFormat.Format32bppRgb);
            panelSprites.PixelFormat = GR.Drawing.PixelFormat.Format32bppRgb;
            // panelSprites size is now Designer-set (right column on the
            // Overlay tab). The ClientSizeChanged handler will compute the
            // internal display size from the panel's actual ClientSize.

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                panelSprites.Items.Add(i.ToString(), m_SpriteProject.Sprites[i].Tile.Image);
            }
            ChangeColorSettingsDialog();

            pictureEditor.SetImageSize(m_SpriteWidth, m_SpriteHeight);
            panelSprites.SelectedIndex = 0;

            pictureEditor.PostPaint += new GR.Forms.FastPictureBox.PostPaintCallback(pictureEditor_PostPaint);

            comboExportRange.Items.Add("All");
            comboExportRange.Items.Add("Selection");
            comboExportRange.Items.Add("Range");
            comboExportRange.SelectedIndex = 0;

            btnChangeMode.Text = GR.EnumHelper.GetDescription(m_SpriteProject.Mode);

            panelSprites.KeyDown += new KeyEventHandler(HandleKeyDown);
            pictureEditor.PreviewKeyDown += new PreviewKeyDownEventHandler(pictureEditor_PreviewKeyDown);

            RebuildSpriteImage(m_CurrentSprite);

            labelCharNo.Text = "Sprite: " + m_CurrentSprite.ToString();
            pictureEditor.Image = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Image;

            panelSprites_SelectedIndexChanged(null, null);

            AdjustSpriteSizes();
            UpdateSpriteSelectionInfo();

            BuildOverlaySlotRows();
            BuildFrameSlotControls();
            m_OverlayAnimTimer.Interval = 100;
            m_OverlayAnimTimer.Tick += overlayAnimTimer_Tick;
            m_SpriteTestTimer.Interval = 30;
            m_SpriteTestTimer.Tick += spriteTestTimer_Tick;
            RefreshOverlaysList();
            PopulateTestBackColorCombo();
            ResumeLayout();
            PopulateTestMagnificationFromProject();
            RebuildSpriteTest();
        }



        void pictureEditor_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            KeyEventArgs ke = new KeyEventArgs(e.KeyData);
            HandleKeyDown(sender, ke);
        }



        void DrawSpriteImage(GR.Image.IImage Target, int X, int Y, GR.Memory.ByteBuffer Data, Palette Palette, int Width, int Height, int CustomColor, SpriteMode Mode, int BackgroundColor, int MultiColor1, int MultiColor2, bool ExpandX, bool ExpandY, bool TransparentBackground, int PaletteOffset)
        {
            switch (Mode)
            {
                case SpriteMode.COMMODORE_24_X_21_MULTICOLOR:
                case SpriteMode.MEGA65_64_X_21_16_MULTICOLOR:
                    SpriteDisplayer.DisplayMultiColorSprite(Data, Palette, Width, Height, BackgroundColor, MultiColor1, MultiColor2, CustomColor, Target, X, Y, ExpandX, ExpandY, TransparentBackground);
                    break;
                case SpriteMode.COMMODORE_24_X_21_HIRES:
                case SpriteMode.MEGA65_64_X_21_16_HIRES:
                    SpriteDisplayer.DisplayHiResSprite(Data, Palette, Width, Height, BackgroundColor, CustomColor, Target, X, Y, ExpandX, ExpandY, TransparentBackground);
                    break;
                case SpriteMode.MEGA65_16_X_21_16_COLORS:
                    SpriteDisplayer.DisplayNCMSprite(Data, Palette, Width, Height, BackgroundColor, Target, X, Y, ExpandX, ExpandY, TransparentBackground);
                    break;
                case SpriteMode.COMMANDER_X16_8_8_16_COLORS:
                case SpriteMode.COMMANDER_X16_8_16_16_COLORS:
                case SpriteMode.COMMANDER_X16_8_32_16_COLORS:
                case SpriteMode.COMMANDER_X16_8_64_16_COLORS:
                case SpriteMode.COMMANDER_X16_16_8_16_COLORS:
                case SpriteMode.COMMANDER_X16_16_16_16_COLORS:
                case SpriteMode.COMMANDER_X16_16_32_16_COLORS:
                case SpriteMode.COMMANDER_X16_16_64_16_COLORS:
                case SpriteMode.COMMANDER_X16_32_8_16_COLORS:
                case SpriteMode.COMMANDER_X16_32_16_16_COLORS:
                case SpriteMode.COMMANDER_X16_32_32_16_COLORS:
                case SpriteMode.COMMANDER_X16_32_64_16_COLORS:
                case SpriteMode.COMMANDER_X16_64_8_16_COLORS:
                case SpriteMode.COMMANDER_X16_64_16_16_COLORS:
                case SpriteMode.COMMANDER_X16_64_32_16_COLORS:
                case SpriteMode.COMMANDER_X16_64_64_16_COLORS:
                    SpriteDisplayer.DisplayX1616ColorSprite(Data, Palette, PaletteOffset, Width, Height, BackgroundColor, Target, X, Y, TransparentBackground);
                    break;
                case SpriteMode.COMMANDER_X16_8_8_256_COLORS:
                case SpriteMode.COMMANDER_X16_8_16_256_COLORS:
                case SpriteMode.COMMANDER_X16_8_32_256_COLORS:
                case SpriteMode.COMMANDER_X16_8_64_256_COLORS:
                case SpriteMode.COMMANDER_X16_16_8_256_COLORS:
                case SpriteMode.COMMANDER_X16_16_16_256_COLORS:
                case SpriteMode.COMMANDER_X16_16_32_256_COLORS:
                case SpriteMode.COMMANDER_X16_16_64_256_COLORS:
                case SpriteMode.COMMANDER_X16_32_8_256_COLORS:
                case SpriteMode.COMMANDER_X16_32_16_256_COLORS:
                case SpriteMode.COMMANDER_X16_32_32_256_COLORS:
                case SpriteMode.COMMANDER_X16_32_64_256_COLORS:
                case SpriteMode.COMMANDER_X16_64_8_256_COLORS:
                case SpriteMode.COMMANDER_X16_64_16_256_COLORS:
                case SpriteMode.COMMANDER_X16_64_32_256_COLORS:
                case SpriteMode.COMMANDER_X16_64_64_256_COLORS:
                    SpriteDisplayer.DisplayX16256ColorSprite(Data, Palette, PaletteOffset, Width, Height, BackgroundColor, Target, X, Y, TransparentBackground);
                    break;
                default:
                    Debug.Log("DrawSpriteImage unsupported mode " + Mode);
                    break;
            }
        }



        void RebuildSpriteImage(int SpriteIndex)
        {
            var Data = m_SpriteProject.Sprites[SpriteIndex];

            DrawSpriteImage(Data.Tile.Image, 0, 0, Data.Tile.Data, Data.Tile.Colors.Palette, Data.Tile.Width, Data.Tile.Height,
              Data.Tile.CustomColor,
              Data.Mode,
              m_SpriteProject.Colors.BackgroundColor,
              m_SpriteProject.Colors.MultiColor1, m_SpriteProject.Colors.MultiColor2,
              false, false, false, Data.Tile.Colors.PaletteOffset);
        }



        private new bool Modified
        {
            get
            {
                return base.Modified;
            }
            set
            {
                if (value)
                {
                    SetModified();
                }
                else
                {
                    SetUnmodified();
                }
                saveSpriteProjectToolStripMenuItem.Enabled = Modified;
            }
        }



        void MirrorX()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int y = 0; y < m_SpriteHeight; ++y)
                {
                    for (int x = 0; x < m_SpriteWidth / 2; x += Lookup.PixelWidth(sprite.Tile.Mode))
                    {
                        var tempColor = sprite.Tile.GetPixel(x, y);
                        sprite.Tile.SetPixel(x, y, sprite.Tile.GetPixel(m_SpriteWidth - Lookup.PixelWidth(sprite.Tile.Mode) - x, y));
                        sprite.Tile.SetPixel(m_SpriteWidth - Lookup.PixelWidth(sprite.Tile.Mode) - x, y, tempColor);
                    }
                }
                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        void MirrorY()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int y = 0; y < m_SpriteHeight / 2; ++y)
                {
                    for (int x = 0; x < m_SpriteWidth; x += Lookup.PixelWidth(sprite.Tile.Mode))
                    {
                        var oldValue = sprite.Tile.GetPixel(x, y);
                        sprite.Tile.SetPixel(x, y, sprite.Tile.GetPixel(x, m_SpriteHeight - 1 - y));
                        sprite.Tile.SetPixel(x, m_SpriteHeight - 1 - y, oldValue);
                    }
                }
                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers == Keys.Control)
            && (e.KeyCode == Keys.C))
            {
                // copy
                CopySpriteToClipboard();
            }
            else if ((e.Modifiers == Keys.Control)
            && (e.KeyCode == Keys.V))
            {
                PasteFromClipboard();
                if (m_ImportError.Length > 0)
                {
                    Core.Notification.MessageBox("Error while converting", m_ImportError);
                }
            }
        }



        private void comboColor_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            Core.Theming.DrawSingleColorComboBox(combo, e, ConstantData.Palette);
        }



        /// <summary>
        /// Draw handler for the per-slot colour-override combos: item 0 is the
        /// "None" entry (plain themed text, no colour swatch), items 1..16 map
        /// to palette colours 0..15 via the theming helper's palette offset.
        /// </summary>
        private void slotColorOverride_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            if (e.Index <= 0)
            {
                Core.Theming.DrawThemedBackground(e, combo);
                if (e.Index == 0)
                {
                    e.Graphics.DrawString("None", combo.Font,
                        new System.Drawing.SolidBrush(combo.ForeColor), 3.0f, e.Bounds.Top + 1.0f);
                }
                return;
            }
            Core.Theming.DrawSingleColorComboBox(combo, e, ConstantData.Palette, -1);
        }



        private void comboMulticolor_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            Core.Theming.DrawMultiColorComboBox(combo, e, ConstantData.Palette);
        }



        private void pictureEditor_MouseDown(object sender, MouseEventArgs e)
        {
            pictureEditor.Focus();
            HandleMouseOnEditor(e.X, e.Y, e.Button);
        }



        private void HandleMouseOnEditor(int X, int Y, MouseButtons Buttons)
        {
            if (!pictureEditor.ClientRectangle.Contains(X, Y))
            {
                return;
            }

            int charX = (X * m_SpriteWidth) / pictureEditor.ClientRectangle.Width;
            int charY = (Y * m_SpriteHeight) / pictureEditor.ClientRectangle.Height;

            var affectedSprite = m_SpriteProject.Sprites[m_CurrentSprite];

            var newColor = new Tupel<ColorType, byte>(_ColorSettingsDlg.SelectedColor, _ColorSettingsDlg.SelectedCustomColor);

            if ((Core.Settings.BehaviourRightClickIsBGColorPaint)
            && ((Buttons & MouseButtons.Right) != 0))
            {
                Buttons = MouseButtons.Left;
                newColor.first = ColorType.BACKGROUND;
            }
            if (newColor.first == ColorType.BACKGROUND)
            {
                newColor.second = 0;
            }

            if (((Buttons & MouseButtons.Middle) != 0)
            || (((Buttons & MouseButtons.Left) != 0)
            && ((Control.ModifierKeys & Keys.Shift) != 0)))
            {
                Buttons &= ~MouseButtons.Left;

                if (m_ButtonReleased)
                {
                    // middle button toggles selected color
                    _ColorSettingsDlg.ToggleSelectedColor();
                    m_ButtonReleased = false;
                }
                return;
            }

            if ((Buttons & MouseButtons.Left) != 0)
            {
                Undo.UndoTask undo = null;
                if (m_ButtonReleased)
                {
                    undo = new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, m_CurrentSprite);
                    m_ButtonReleased = false;
                }

                bool modified = false;
                switch (m_Mode)
                {
                    case ToolMode.SINGLE_PIXEL:
                        modified = affectedSprite.Tile.SetPixel(charX, charY, newColor);
                        break;
                    case ToolMode.FILL:
                        modified = affectedSprite.Tile.Fill(charX, charY, newColor);
                        break;
                }

                if (modified)
                {
                    Modified = true;

                    if (undo != null)
                    {
                        DocumentInfo.UndoManager.AddUndoTask(undo);
                    }

                    SpriteChanged(m_CurrentSprite);
                }
            }
            else
            {
                m_ButtonReleased = true;
            }
            if ((Buttons & MouseButtons.Right) != 0)
            {
                var pickedColor = affectedSprite.Tile.GetPixel(charX, charY);

                _ColorSettingsDlg.SelectedColor = pickedColor.first;
                if (pickedColor.first == ColorType.CUSTOM_COLOR)
                {
                    _ColorSettingsDlg.SelectedCustomColor = pickedColor.second;
                }
            }
        }



        private void pictureEditor_MouseMove(object sender, MouseEventArgs e)
        {
            MouseButtons buttons = e.Button;
            if (!pictureEditor.Focused)
            {
                buttons = 0;
            }
            HandleMouseOnEditor(e.X, e.Y, buttons);
        }



        private void panelSprites_SelectedIndexChanged(object sender, EventArgs e)
        {
            int newChar = panelSprites.SelectedIndex;
            if (newChar != -1)
            {
                labelCharNo.Text = "Sprite: " + newChar.ToString();
                m_CurrentSprite = newChar;

                DoNotUpdateFromControls = true;

                _ColorSettingsDlg.PaletteOffset = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Colors.PaletteOffset;
                if ((!Lookup.HasCustomPalette(m_SpriteProject.Mode))
                && (!Lookup.HasCustomPalette(m_SpriteProject.Sprites[m_CurrentSprite].Tile.Mode)))
                {
                    _ColorSettingsDlg.CustomColor = m_SpriteProject.Sprites[m_CurrentSprite].Tile.CustomColor;
                    _ColorSettingsDlg.MultiColorEnabled = (m_SpriteProject.Sprites[m_CurrentSprite].Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR);
                }
                else
                {
                    _ColorSettingsDlg.ActivePalette = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Colors.ActivePalette;
                    m_SpriteProject.Colors.ActivePalette = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Colors.ActivePalette;
                }
                DoNotUpdateFromControls = false;

                pictureEditor.Image = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Image;
            }
            btnClearSprite.Enabled = (panelSprites.SelectedIndex != -1);
            btnDeleteSprite.Enabled = (panelSprites.SelectedIndex != -1);
        }



        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filename;

            if (OpenFile("Open Sprite Project or File", Types.Constants.FILEFILTER_SPRITE + Types.Constants.FILEFILTER_SPRITE_SPRITEPAD + Types.Constants.FILEFILTER_ALL, out filename))
            {
                ImportSprites(filename, true, true);
            }
        }



        public void Clear()
        {
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                m_SpriteProject.Sprites[i].Tile.CustomColor = 0;
                m_SpriteProject.Sprites[i].Mode = SpriteMode.COMMODORE_24_X_21_HIRES;
            }
            m_SpriteProject.Overlays.Clear();
            m_CurrentSprite = 0;
            RefreshOverlaysList();
            RebuildOverlayPreview();
        }



        private void CurrentSpriteModified()
        {
            // The bank sprite changed — rebuild the overlay preview so any
            // overlay slot that references this bank index re-renders.
            RebuildOverlayPreview();
        }



        public bool ImportSprites(string Filename, bool OnlyImportFromProject, bool AddUndo, int BytesToSkip = 0, bool ExpectPadding = false)
        {
            GR.Memory.ByteBuffer projectFile = GR.IO.File.ReadAllBytes(Filename);
            if (projectFile == null)
            {
                return false;
            }

            GR.IO.MemoryReader memIn = projectFile.MemoryReader();

            if (GR.Path.GetExtension(Filename).ToUpper() == ".SPD")
            {
                var spritePad = new SpritePadProject();

                if (!spritePad.ReadFromBuffer(projectFile))
                {
                    return false;
                }
                btnChangeMode.Text = GR.EnumHelper.GetDescription(SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC);

                m_SpriteProject.Colors.BackgroundColor = spritePad.BackgroundColor;
                m_SpriteProject.Colors.MultiColor1 = spritePad.MultiColor1;
                m_SpriteProject.Colors.MultiColor2 = spritePad.MultiColor2;
                for (int i = 0; i < spritePad.NumSprites; ++i)
                {
                    if (i < m_SpriteProject.Sprites.Count)
                    {
                        if (AddUndo)
                        {
                            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i), i == 0);
                        }

                        spritePad.Sprites[i].Data.CopyTo(m_SpriteProject.Sprites[i].Tile.Data, 0, 63);
                        m_SpriteProject.Sprites[i].Tile.CustomColor = (byte)spritePad.Sprites[i].Color;
                        m_SpriteProject.Sprites[i].Mode = spritePad.Sprites[i].Multicolor ? SpriteMode.COMMODORE_24_X_21_MULTICOLOR : SpriteMode.COMMODORE_24_X_21_HIRES;
                        m_SpriteProject.Sprites[i].Tile.Mode = Lookup.GraphicTileModeFromSpriteMode(m_SpriteProject.Sprites[i].Mode);
                    }
                }
                ChangeColorSettingsDialog();
                OnPaletteChanged();

                editSpriteFrom.Text = "0";
                editSpriteCount.Text = spritePad.NumSprites.ToString();

                if ((m_SpriteProject.ExportStartIndex != 0)
                || ((m_SpriteProject.ExportSpriteCount != 256)
                && (m_SpriteProject.ExportSpriteCount != 0)))
                {
                    comboExportRange.SelectedIndex = 2;
                }

                panelSprites.Invalidate();
                pictureEditor.Invalidate();
                Modified = false;

                saveSpriteProjectToolStripMenuItem.Enabled = true;
                closeCharsetProjectToolStripMenuItem.Enabled = true;
                EnableFileWatcher();
                return true;
            }
            else if (GR.Path.GetExtension(Filename).ToUpper() != ".SPRITEPROJECT")
            {
                bool allFillBytesZero = true;

                if ((BytesToSkip > 0)
                && (BytesToSkip < projectFile.Length))
                {
                    projectFile = projectFile.SubBuffer(BytesToSkip);
                    memIn = projectFile.MemoryReader();
                }

                int numBytesPerSprite = Lookup.NumBytesOfSingleSprite(m_SpriteProject.Mode);
                int numBytesPerSpritePadded = Lookup.NumPaddedBytesOfSingleSprite(m_SpriteProject.Mode);
                int numSprites = 0;
                if (ExpectPadding)
                {
                    numSprites = (int)(projectFile.Length - numBytesPerSprite) / numBytesPerSpritePadded + 1;
                }
                else
                {
                    numSprites = (int)(projectFile.Length) / numBytesPerSprite;
                }
                for (int i = 0; i < numSprites; ++i)
                {
                    GR.Memory.ByteBuffer tempBuffer = new GR.Memory.ByteBuffer();

                    if (ExpectPadding)
                    {
                        memIn.ReadBlock(tempBuffer, (uint)numBytesPerSpritePadded);
                    }
                    else
                    {
                        memIn.ReadBlock(tempBuffer, (uint)numBytesPerSprite);
                    }
                    if (i < m_SpriteProject.Sprites.Count)
                    {
                        if (AddUndo)
                        {
                            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i), i == 0);
                        }

                        tempBuffer.CopyTo(m_SpriteProject.Sprites[i].Tile.Data, 0, numBytesPerSprite);

                        if (tempBuffer.ByteAt(numBytesPerSprite) != 0)
                        {
                            allFillBytesZero = false;
                        }
                        if (m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC)
                        {
                            m_SpriteProject.Sprites[i].Tile.CustomColor = (byte)(tempBuffer.ByteAt(numBytesPerSprite) & 0xf);
                            m_SpriteProject.Sprites[i].Mode = ((tempBuffer.ByteAt(numBytesPerSprite) & 0x80) != 0) ? SpriteMode.COMMODORE_24_X_21_MULTICOLOR : SpriteMode.COMMODORE_24_X_21_HIRES;
                        }
                    }
                }
                if (allFillBytesZero)
                {
                    // sanity check, this means we have black on black, 
                    for (int i = 0; i < numSprites; ++i)
                    {
                        if (i < m_SpriteProject.Sprites.Count)
                        {
                            m_SpriteProject.Sprites[i].Tile.CustomColor = 1;
                        }
                    }
                }

                ChangeColorSettingsDialog();
                OnPaletteChanged();

                editSpriteFrom.Text = "0";
                editSpriteCount.Text = numSprites.ToString();

                panelSprites.Invalidate();
                pictureEditor.Invalidate();
                Modified = false;

                saveSpriteProjectToolStripMenuItem.Enabled = true;
                closeCharsetProjectToolStripMenuItem.Enabled = true;
                EnableFileWatcher();
                return true;
            }

            // sprite project
            if (OnlyImportFromProject)
            {
                // only import sprite data
                Formats.SpriteProject sprites = new RetroDevStudio.Formats.SpriteProject();

                if (!sprites.ReadFromBuffer(projectFile))
                {
                    return false;
                }

                panelSprites.Items.Clear();

                if (AddUndo)
                {
                    for (int spriteIndex = 0; spriteIndex < m_SpriteProject.TotalNumberOfSprites; ++spriteIndex)
                    {
                        DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), spriteIndex == 0);
                    }
                }

                m_SpriteProject.TotalNumberOfSprites = sprites.TotalNumberOfSprites;
                m_SpriteProject.Sprites.Clear();
                for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
                {
                    m_SpriteProject.Sprites.Add(new SpriteProject.SpriteData(sprites.Sprites[i]));

                    panelSprites.Items.Add(i.ToString(), m_SpriteProject.Sprites[i].Tile.Image);
                }
                ChangeColorSettingsDialog();
                OnPaletteChanged();
                panelSprites.Invalidate();
                pictureEditor.Invalidate();
                Modified = false;
                return true;
            }

            if (AddUndo)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject), true);
                for (int spriteIndex = 0; spriteIndex < m_SpriteProject.TotalNumberOfSprites; ++spriteIndex)
                {
                    DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex));
                }
            }

            m_IsSpriteProject = true;

            if (!m_SpriteProject.ReadFromBuffer(projectFile))
            {
                return false;
            }
            AdjustSpriteSizes();
            panelSprites.Items.Clear();

            btnChangeMode.Text = GR.EnumHelper.GetDescription(m_SpriteProject.Mode);

            ChangeColorSettingsDialog();

            editSpriteFrom.Text = m_SpriteProject.ExportStartIndex.ToString();
            editSpriteCount.Text = m_SpriteProject.ExportSpriteCount.ToString();
            if ((m_SpriteProject.ExportStartIndex != 0)
            || ((m_SpriteProject.ExportSpriteCount != 256)
            && (m_SpriteProject.ExportSpriteCount != 0)))
            {
                comboExportRange.SelectedIndex = 2;
            }

            // re-add item (update tags)
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                panelSprites.Items.Add(i.ToString(), m_SpriteProject.Sprites[i].Tile.Image);
            }
            pictureEditor.Image = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Image;
            panelSprites.Invalidate();
            pictureEditor.Invalidate();

            OnPaletteChanged();
            _ColorSettingsDlg.ActivePalette = m_SpriteProject.Sprites[m_CurrentSprite].Tile.Colors.ActivePalette;

            // Refresh the new Overlay tab from the freshly-loaded project. The
            // constructor's RefreshOverlaysList() ran against the empty default
            // project before ReadFromBuffer populated Overlays, so without this
            // call the listbox stays empty until the user clicks Add.
            RefreshOverlaysList();

            Modified = false;

            if (DocumentInfo.Element == null)
            {
                DocumentInfo.DocumentFilename = Filename;
            }

            saveSpriteProjectToolStripMenuItem.Enabled = true;
            closeCharsetProjectToolStripMenuItem.Enabled = true;
            EnableFileWatcher();
            return true;
        }



        public override bool LoadDocument()
        {
            if (string.IsNullOrEmpty(DocumentInfo.DocumentFilename))
            {
                return false;
            }
            try
            {
                ImportSprites(DocumentInfo.FullPath, false, false);
            }
            catch (System.IO.IOException ex)
            {
                Core.Notification.MessageBox("Could not load file", "Could not load sprite project file " + DocumentInfo.FullPath + ".\r\n" + ex.Message);
                return false;
            }
            // Reflect the loaded grid opacity in the slider. Guarded + clamped to the
            // slider's range (a programmatic Value set doesn't fire Scroll, but the
            // guard keeps it consistent with the rest of the load path).
            DoNotUpdateFromControls = true;
            int gridOp = m_SpriteProject.GridOpacity;
            if (gridOp < trackGridOpacity.Minimum) gridOp = trackGridOpacity.Minimum;
            if (gridOp > trackGridOpacity.Maximum) gridOp = trackGridOpacity.Maximum;
            trackGridOpacity.Value = gridOp;
            checkTestExpandX.Checked = m_SpriteProject.TestExpandX;
            checkTestExpandY.Checked = m_SpriteProject.TestExpandY;
            checkTestLoop.Checked = m_SpriteProject.TestLoop;
            PopulateTestBackColorCombo();
            DoNotUpdateFromControls = false;
            PopulateTestMagnificationFromProject();

            // Restore the saved export method. Detach the handler around the
            // programmatic index change, then rebuild the export sub-form
            // explicitly, so loading the project does not mark it modified.
            if (comboExportMethod.Items.Count > 0)
            {
                int savedExportMethod = m_SpriteProject.ExportMethodIndex;
                if (savedExportMethod < 0) savedExportMethod = 0;
                if (savedExportMethod >= comboExportMethod.Items.Count) savedExportMethod = 0;
                comboExportMethod.SelectedIndexChanged -= comboExportMethod_SelectedIndexChanged;
                comboExportMethod.SelectedIndex = savedExportMethod;
                comboExportMethod.SelectedIndexChanged += comboExportMethod_SelectedIndexChanged;
                RebuildExportForm();
            }

            RebuildSpriteTest();
            SetUnmodified();
            return true;
        }



        protected override bool QueryFilename(string PreviousFilename, out string Filename)
        {
            Filename = "";

            System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

            saveDlg.Title = "Save Sprite Project as";
            saveDlg.Filter = "Sprite Projects|*.spriteproject|All Files|*.*";
            saveDlg.FileName = GR.Path.GetFileName(PreviousFilename);
            ApplySaveDialogInitialDirectory(saveDlg, PreviousFilename);
            if (saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            Filename = saveDlg.FileName;
            return true;
        }



        protected override bool PerformSave(string FullPath)
        {
            GR.Memory.ByteBuffer dataToSave = SaveToBuffer();

            return SaveDocumentData(FullPath, dataToSave);
        }



        private void closeCharsetProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DocumentInfo.DocumentFilename == "")
            {
                return;
            }
            if (Modified)
            {
                var endButtons = MessageBoxButtons.YesNoCancel;
                if (Core.ShuttingDown)
                {
                    endButtons = MessageBoxButtons.YesNo;
                }
                DialogResult doSave = MessageBox.Show("There are unsaved changes in your sprite set. Save now?", "Save changes?", endButtons);
                if (doSave == DialogResult.Cancel)
                {
                    return;
                }
                if (doSave == DialogResult.Yes)
                {
                    Save(SaveMethod.SAVE);
                }
            }
            Clear();
            DocumentInfo.DocumentFilename = "";
            Modified = false;
            panelSprites.Invalidate();
            pictureEditor.Invalidate();

            closeCharsetProjectToolStripMenuItem.Enabled = false;
            saveSpriteProjectToolStripMenuItem.Enabled = false;
        }



        private void saveCharsetProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save(SaveMethod.SAVE);
        }



        public override GR.Memory.ByteBuffer SaveToBuffer()
        {
            GR.Memory.ByteBuffer projectFile = new GR.Memory.ByteBuffer();

            List<int> exportIndices = GetExportIndices();

            if (m_IsSpriteProject)
            {
                // Flush the live export form's UI into the project so its export
                // settings are persisted with the project.
                if (m_ExportForm != null)
                {
                    m_ExportForm.UpdateExportSettings(m_SpriteProject);
                }
                projectFile = m_SpriteProject.SaveToBuffer();
            }
            else
            {
                for (int i = 0; i < exportIndices.Count; ++i)
                {
                    projectFile.Append(m_SpriteProject.Sprites[exportIndices[i]].Tile.Data);
                    projectFile.AppendU8((byte)m_SpriteProject.Sprites[exportIndices[i]].Tile.CustomColor);
                }
            }
            return projectFile;
        }



        private List<int> GetExportIndices()
        {
            List<int> exportIndices = new List<int>();

            switch (comboExportRange.SelectedIndex)
            {
                case 0:
                    // all
                    for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
                    {
                        exportIndices.Add(i);
                    }
                    break;
                case 1:
                    // selection
                    exportIndices = panelSprites.SelectedIndices;
                    break;
                case 2:
                    // rage
                    {
                        int startIndex = GR.Convert.ToI32(editSpriteFrom.Text);
                        int numSprites = GR.Convert.ToI32(editSpriteCount.Text);

                        if (startIndex < 0)
                        {
                            startIndex = 0;
                        }
                        if (startIndex >= m_SpriteProject.TotalNumberOfSprites)
                        {
                            startIndex = m_SpriteProject.TotalNumberOfSprites - 1;
                        }
                        if (numSprites < 0)
                        {
                            numSprites = 1;
                        }
                        if (startIndex + numSprites > m_SpriteProject.TotalNumberOfSprites)
                        {
                            numSprites = m_SpriteProject.TotalNumberOfSprites - startIndex;
                        }
                        for (int i = 0; i < numSprites; ++i)
                        {
                            exportIndices.Add(startIndex + i);
                        }
                    }
                    break;
            }
            return exportIndices;
        }



        private void btnPasteFromClipboard_Click(DecentForms.ControlBase Sender)
        {
            PasteFromClipboard();
            if (m_ImportError.Length > 0)
            {
                Core.Notification.MessageBox("Error while converting", m_ImportError);
            }
        }



        private void PasteFromClipboard()
        {
            m_ImportError = "";
            IDataObject dataObj = Clipboard.GetDataObject();
            if (dataObj == null)
            {
                Core.MessageBox("No image on clipboard", "Cannot paste");
                return;
            }

            var clipList = new ClipboardImageList();

            if (clipList.GetFromClipboard())
            {
                int pastePos = panelSprites.SelectedIndex;
                if (pastePos == -1)
                {
                    pastePos = 0;
                }
                bool firstEntry = true;
                foreach (var entry in clipList.Entries)
                {
                    int indexGap = entry.Index;
                    pastePos += indexGap;

                    if (pastePos >= m_SpriteProject.Sprites.Count)
                    {
                        break;
                    }

                    DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, pastePos), firstEntry);
                    firstEntry = false;

                    var targetTile = m_SpriteProject.Sprites[pastePos].Tile;

                    if (((entry.Tile.Mode == GraphicTileMode.COMMODORE_HIRES)
                    || (entry.Tile.Mode == GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES))
                    && ((targetTile.Mode == GraphicTileMode.COMMODORE_HIRES)
                    || (targetTile.Mode == GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES)))
                    {
                        // can copy mode
                        targetTile.Mode = entry.Tile.Mode;

                        m_SpriteProject.Sprites[pastePos].Mode = Lookup.SpriteModeFromTileMode(targetTile.Mode);
                    }

                    if (Lookup.HaveCustomSpriteColor(m_SpriteProject.Mode))
                    {
                        targetTile.CustomColor = entry.Tile.CustomColor;
                    }
                    else
                    {
                        targetTile.Colors.ActivePalette = entry.Tile.Colors.ActivePalette;
                    }

                    int copyWidth = Math.Min(m_SpriteWidth, entry.Tile.Width);
                    int copyHeight = Math.Min(m_SpriteHeight, entry.Tile.Height);

                    for (int x = 0; x < copyWidth; ++x)
                    {
                        for (int y = 0; y < copyHeight; ++y)
                        {
                            targetTile.SetPixel(x, y, entry.Tile.MapPixelColor(x, y, targetTile));
                        }
                    }

                    RebuildSpriteImage(pastePos);
                    panelSprites.InvalidateItemRect(pastePos);

                    if (pastePos == m_CurrentSprite)
                    {
                        _ColorSettingsDlg.CustomColor = m_SpriteProject.Sprites[pastePos].Tile.CustomColor;
                        _ColorSettingsDlg.MultiColorEnabled = (m_SpriteProject.Sprites[pastePos].Tile.Mode == GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES);
                        _ColorSettingsDlg.ActivePalette = m_SpriteProject.Sprites[pastePos].Tile.Colors.ActivePalette;
                    }
                }
                pictureEditor.Invalidate();
                SetModified();
                return;
            }
            else if (!Clipboard.ContainsImage())
            {
                Core.MessageBox("No image on clipboard", "Cannot paste");
                return;
            }
            GR.Image.FastImage imgClip = null;
            foreach (string format in dataObj.GetFormats())
            {
                if (format == "DeviceIndependentBitmap")
                {
                    object dibData = dataObj.GetData(format);
                    imgClip = GR.Image.FastImage.CreateImageFromHDIB(dibData);
                    break;
                }
            }
            if (imgClip == null)
            {
                Core.Notification.MessageBox("No image on clipboard", "No image on clipboard");
                return;
            }
            var mcSettings = new ColorSettings(m_SpriteProject.Colors);

            bool pasteAsBlock = false;

            var importType = Lookup.GraphicImportTypeFromMode(m_SpriteProject.Mode);

            if (!Core.MainForm.ImportImage("", imgClip, importType, mcSettings,
                                             Lookup.SpriteWidth(m_SpriteProject.Mode), Lookup.SpriteHeight(m_SpriteProject.Mode),
                                             out GR.Image.IImage mappedImage, out mcSettings, out pasteAsBlock, out importType))
            {
                imgClip.Dispose();
                m_ImportError = "";
                return;
            }

            if (mcSettings.BackgroundColor != -1)
            {
                m_SpriteProject.Colors.BackgroundColor = mcSettings.BackgroundColor;
            }
            if (mcSettings.MultiColor1 != -1)
            {
                m_SpriteProject.Colors.MultiColor1 = mcSettings.MultiColor1;
            }
            if (mcSettings.MultiColor2 != -1)
            {
                m_SpriteProject.Colors.MultiColor2 = mcSettings.MultiColor2;
            }

            bool firstUndoStep = true;

            if (mcSettings.Palettes.Count > m_SpriteProject.Colors.Palettes.Count)
            {
                // a palette was imported!
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject), firstUndoStep);
                firstUndoStep = false;

                _ColorSettingsDlg.PalettesChanged();
                m_SpriteProject.Colors.Palettes.Add(mcSettings.Palettes[mcSettings.Palettes.Count - 1]);
            }

            int activePalette = mcSettings.ActivePalette;

            ChangeColorSettingsDialog();

            int spritesY = (mappedImage.Height + m_SpriteHeight - 1) / m_SpriteHeight;
            int spritesX = (mappedImage.Width + m_SpriteWidth - 1) / m_SpriteWidth;
            int spritesPerLine = panelSprites.ItemsPerLine;
            int currentTargetSprite = m_CurrentSprite;

            for (int j = 0; j < spritesY; ++j)
            {
                for (int i = 0; i < spritesX; ++i)
                {
                    if (pasteAsBlock)
                    {
                        int localX = (m_CurrentSprite % spritesPerLine) + i;
                        int localY = m_CurrentSprite / spritesPerLine + j;
                        if (localX >= spritesPerLine)
                        {
                            continue;
                        }
                        if (localY * spritesPerLine >= 256)
                        {
                            break;
                        }
                        currentTargetSprite = localX + localY * spritesPerLine;
                    }
                    if (currentTargetSprite >= m_SpriteProject.TotalNumberOfSprites)
                    {
                        // trying to paste too far
                        break;
                    }

                    int copyWidth = mappedImage.Width - i * m_SpriteWidth;
                    if (copyWidth > m_SpriteWidth)
                    {
                        copyWidth = m_SpriteWidth;
                    }
                    int copyHeight = mappedImage.Height - j * m_SpriteHeight;
                    if (copyHeight > m_SpriteHeight)
                    {
                        copyHeight = m_SpriteHeight;
                    }

                    DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, currentTargetSprite), firstUndoStep);
                    firstUndoStep = false;

                    GR.Image.FastImage imgSprite = mappedImage.GetImage(i * m_SpriteWidth, j * m_SpriteHeight, copyWidth, copyHeight) as GR.Image.FastImage;

                    m_ImportError = "";
                    m_SpriteProject.Sprites[currentTargetSprite].Tile.Colors.ActivePalette = activePalette;
                    m_SpriteProject.Sprites[currentTargetSprite].Tile.Colors.Palettes = m_SpriteProject.Colors.Palettes;
                    ImportSprite(imgSprite, currentTargetSprite);
                    imgSprite.Dispose();

                    if (!string.IsNullOrEmpty(m_ImportError))
                    {
                        Core.AddToOutput($"Error importing sprite {currentTargetSprite}: {m_ImportError}\r\n");
                    }


                    if (currentTargetSprite == m_CurrentSprite)
                    {
                        CurrentSpriteModified();
                        DoNotUpdateFromControls = true;

                        _ColorSettingsDlg.CustomColor = m_SpriteProject.Sprites[currentTargetSprite].Tile.CustomColor;
                        _ColorSettingsDlg.ActivePalette = m_SpriteProject.Sprites[currentTargetSprite].Tile.Colors.ActivePalette;
                        DoNotUpdateFromControls = false;
                    }
                    RebuildSpriteImage(currentTargetSprite);

                    panelSprites.InvalidateItemRect(currentTargetSprite);

                    if (!pasteAsBlock)
                    {
                        ++currentTargetSprite;
                    }
                }
            }

            // update all palettes for all sprites
            for (int i = 0; i < m_SpriteProject.Sprites.Count; ++i)
            {
                m_SpriteProject.Sprites[i].Tile.Colors.Palettes = m_SpriteProject.Colors.Palettes;
            }
            mappedImage.Dispose();
            imgClip.Dispose();
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void CopySpriteToClipboard()
        {
            // copy selected range/column (put custom data in clipboard)
            List<int> selectedImages = panelSprites.SelectedIndices;
            if (selectedImages.Count == 0)
            {
                return;
            }

            var clipList = new ClipboardImageList();
            clipList.Mode = Lookup.GraphicTileModeFromSpriteProjectMode(m_SpriteProject.Mode);
            clipList.Colors = m_SpriteProject.Colors;
            clipList.ColumnBased = panelSprites.IsSelectionColumnBased;

            foreach (int index in selectedImages)
            {
                var entry = new ClipboardImageList.Entry();
                var sprite = m_SpriteProject.Sprites[index];

                entry.Tile = sprite.Tile;
                entry.Index = index;

                clipList.Entries.Add(entry);
            }
            clipList.CopyToClipboard();
        }



        private void btnCopyToClipboard_Click(DecentForms.ControlBase Sender)
        {
            CopySpriteToClipboard();
        }



        public bool ImportSprite(GR.Image.FastImage Image, int SpriteIndex)
        {
            m_ImportError = "";
            if (Image.PixelFormat != GR.Drawing.PixelFormat.Format8bppIndexed)
            {
                // invalid format
                m_ImportError = "Invalid image format, must be 8 bit index";
                return false;
            }

            // Match image data
            GR.Memory.ByteBuffer Buffer = new GR.Memory.ByteBuffer(m_SpriteProject.Sprites[SpriteIndex].Tile.Data);

            int ChosenSpriteColor = -1;

            SpriteMode insertMode = SpriteMode.COMMODORE_24_X_21_HIRES;
            if (m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.MEGA65_64_X_21_HIRES_OR_MC)
            {
                insertMode = SpriteMode.MEGA65_64_X_21_16_HIRES;
            }

            if ((m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC)
            || (m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.MEGA65_64_X_21_HIRES_OR_MC))
            {
                // determine single/multi color
                bool[] usedColor = new bool[16];
                int numColors = 0;
                bool hasSinglePixel = false;
                bool usedBackgroundColor = false;

                for (int y = 0; y < Image.Height; ++y)
                {
                    for (int x = 0; x < Image.Width; ++x)
                    {
                        int colorIndex = (int)Image.GetPixelData(x, y);
                        if (colorIndex >= 16)
                        {
                            m_ImportError = $"Encountered color index >= 16 ({colorIndex}) at {x},{y}";
                            return false;
                        }
                        if ((x % 2) == 0)
                        {
                            if (colorIndex != (int)Image.GetPixelData(x + 1, y))
                            {
                                // not a double pixel, must be single color then
                                hasSinglePixel = true;
                            }
                        }

                        if (!usedColor[colorIndex])
                        {
                            if (colorIndex == m_SpriteProject.Colors.BackgroundColor)
                            {
                                usedBackgroundColor = true;
                            }
                            usedColor[colorIndex] = true;
                            numColors++;
                        }
                    }
                }
                if ((hasSinglePixel)
                && (numColors > 2))
                {
                    m_ImportError = "Has a single pixel, but more than two colors";
                    return false;
                }
                if ((hasSinglePixel)
                && (numColors == 2)
                && (!usedBackgroundColor))
                {
                    m_ImportError = "Looks like single color, but doesn't use the set background color";
                    return false;
                }
                if ((!hasSinglePixel)
                && (numColors > 4))
                {
                    m_ImportError = "Uses more than 4 colors";
                    return false;
                }
                if ((!hasSinglePixel)
                && (numColors == 4)
                && (!usedBackgroundColor))
                {
                    m_ImportError = "Uses 4 colors, but doesn't use the set background color";
                    return false;
                }
                if ((hasSinglePixel)
                || ((numColors == 2)
                && (usedBackgroundColor)))
                {
                    // eligible for single color
                    int usedFreeColor = -1;
                    for (int i = 0; i < 16; ++i)
                    {
                        if (usedColor[i])
                        {
                            if (i != m_SpriteProject.Colors.BackgroundColor)
                            {
                                if (usedFreeColor != -1)
                                {
                                    m_ImportError = "Uses more than one free color";
                                    return false;
                                }
                                usedFreeColor = i;
                            }
                        }
                    }

                    for (int y = 0; y < Image.Height; ++y)
                    {
                        for (int x = 0; x < Image.Width; ++x)
                        {
                            int ColorIndex = (int)Image.GetPixelData(x, y);

                            int BitPattern = 0;

                            if (ColorIndex != m_SpriteProject.Colors.BackgroundColor)
                            {
                                BitPattern = 1;
                            }

                            // noch nicht verwendete Farbe
                            if (BitPattern == 1)
                            {
                                ChosenSpriteColor = ColorIndex;
                            }
                            byte byteMask = (byte)(255 - (1 << ((7 - (x % 8)))));
                            Buffer.SetU8At(y * m_SpriteWidth / 8 + x / 8, (byte)((Buffer.ByteAt(y * m_SpriteWidth / 8 + x / 8) & byteMask) | (BitPattern << ((7 - (x % 8))))));
                        }
                    }
                }
                else
                {
                    // multi color
                    SetMulticolorMode(ref insertMode, true);
                    int usedMultiColors = 0;
                    int usedFreeColor = -1;
                    for (int i = 0; i < 16; ++i)
                    {
                        if (usedColor[i])
                        {
                            if ((i == m_SpriteProject.Colors.MultiColor1)
                            || (i == m_SpriteProject.Colors.MultiColor2)
                            || (i == m_SpriteProject.Colors.BackgroundColor))
                            {
                                ++usedMultiColors;
                            }
                            else
                            {
                                usedFreeColor = i;
                            }
                        }
                    }
                    if (numColors - usedMultiColors > 1)
                    {
                        // only one free color allowed
                        m_ImportError = "Uses more than one free color";
                        return false;
                    }
                    for (int y = 0; y < Image.Height; ++y)
                    {
                        for (int x = 0; x < Image.Width / 2; ++x)
                        {
                            int ColorIndex = (int)Image.GetPixelData(2 * x, y);

                            byte BitPattern = 0;

                            if (ColorIndex == m_SpriteProject.Colors.BackgroundColor)
                            {
                                BitPattern = 0x00;
                            }
                            else if (ColorIndex == m_SpriteProject.Colors.MultiColor1)
                            {
                                BitPattern = 0x01;
                            }
                            else if (ColorIndex == m_SpriteProject.Colors.MultiColor2)
                            {
                                BitPattern = 0x03;
                            }
                            else
                            {
                                // noch nicht verwendete Farbe
                                ChosenSpriteColor = usedFreeColor;
                                BitPattern = 0x02;
                            }
                            byte byteMask = (byte)(255 - (3 << ((3 - (x % 4)) * 2)));
                            Buffer.SetU8At(y * m_SpriteWidth / 8 + x / 4, (byte)((Buffer.ByteAt(y * m_SpriteWidth / 8 + x / 4) & byteMask) | (BitPattern << ((3 - (x % 4)) * 2))));
                        }
                    }
                }
                m_SpriteProject.Sprites[SpriteIndex].Mode = insertMode;
                m_SpriteProject.Sprites[SpriteIndex].Tile.Mode = Lookup.GraphicTileModeFromSpriteMode(insertMode);
                for (int i = 0; i < Buffer.Length; ++i)
                {
                    m_SpriteProject.Sprites[SpriteIndex].Tile.Data.SetU8At(i, Buffer.ByteAt(i));
                }
                if (SpriteIndex == m_CurrentSprite)
                {
                    ChangeColorSettingsDialog();
                }
            }
            else if (Lookup.GraphicImportTypeFromMode(m_SpriteProject.Mode) == GraphicType.SPRITES_16_COLORS)
            {
                ChosenSpriteColor = 0;
                insertMode = Lookup.SpriteModeFromSpriteProjectMode(m_SpriteProject.Mode);

                for (int y = 0; y < Image.Height; ++y)
                {
                    for (int x = 0; x < Image.Width; ++x)
                    {
                        byte colorIndex = (byte)Image.GetPixelData(x, y);
                        if (colorIndex >= 16)
                        {
                            m_ImportError = $"Encountered color index >= 16 ({colorIndex}) at {x},{y}";
                            return false;
                        }
                        m_SpriteProject.Sprites[SpriteIndex].Tile.SetPixel(x, y, new Tupel<ColorType, byte>(ColorType.CUSTOM_COLOR, colorIndex));
                    }
                }
            }
            else
            {
                // 256 colors
                ChosenSpriteColor = 0;
                insertMode = Lookup.SpriteModeFromSpriteProjectMode(m_SpriteProject.Mode);

                for (int y = 0; y < Image.Height; ++y)
                {
                    for (int x = 0; x < Image.Width; ++x)
                    {
                        byte colorIndex = (byte)Image.GetPixelData(x, y);
                        m_SpriteProject.Sprites[SpriteIndex].Tile.SetPixel(x, y, new Tupel<ColorType, byte>(ColorType.CUSTOM_COLOR, colorIndex));
                    }
                }
            }
            m_SpriteProject.Sprites[SpriteIndex].Tile.CustomColor = (byte)ChosenSpriteColor;
            m_SpriteProject.Sprites[SpriteIndex].Mode = insertMode;
            RebuildSpriteImage(SpriteIndex);

            return true;
        }



        private void btnShiftLeft_Click(DecentForms.ControlBase Sender)
        {
            ShiftLeft();
        }



        private void ShiftLeft()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int y = 0; y < m_SpriteHeight; ++y)
                {
                    var tempColor = sprite.Tile.GetPixel(0, y);
                    for (int x = 0; x < m_SpriteWidth - 1; ++x)
                    {
                        sprite.Tile.SetPixel(x, y, sprite.Tile.GetPixel(x + 1, y));
                    }
                    sprite.Tile.SetPixel(m_SpriteWidth - 1, y, tempColor);
                }
                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnShiftRight_Click(DecentForms.ControlBase Sender)
        {
            ShiftRight();
        }



        private void ShiftRight()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int y = 0; y < m_SpriteHeight; ++y)
                {
                    var tempColor = sprite.Tile.GetPixel(m_SpriteWidth - 1, y);
                    for (int x = 0; x < m_SpriteWidth - 1; ++x)
                    {

                        sprite.Tile.SetPixel(m_SpriteWidth - 1 - x, y, sprite.Tile.GetPixel(m_SpriteWidth - x - 2, y));
                    }
                    sprite.Tile.SetPixel(0, y, tempColor);
                }
                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnShiftUp_Click(DecentForms.ControlBase Sender)
        {
            ShiftUp();
        }



        private void ShiftUp()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int x = 0; x < m_SpriteWidth; x += Lookup.PixelWidth(sprite.Tile.Mode))
                {
                    var tempPixel = sprite.Tile.GetPixel(x, 0);
                    for (int y = 0; y < m_SpriteHeight - 1; ++y)
                    {
                        sprite.Tile.SetPixel(x, y, sprite.Tile.GetPixel(x, y + 1));
                    }
                    sprite.Tile.SetPixel(x, m_SpriteHeight - 1, tempPixel);
                }

                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnShiftDown_Click(DecentForms.ControlBase Sender)
        {
            ShiftDown();
        }



        private void ShiftDown()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int x = 0; x < m_SpriteWidth; x += Lookup.PixelWidth(sprite.Tile.Mode))
                {
                    var tempPixel = sprite.Tile.GetPixel(x, m_SpriteHeight - 1);
                    for (int y = 0; y < m_SpriteHeight - 1; ++y)
                    {
                        sprite.Tile.SetPixel(x, m_SpriteHeight - 1 - y, sprite.Tile.GetPixel(x, m_SpriteHeight - 1 - y - 1));
                    }
                    sprite.Tile.SetPixel(x, 0, tempPixel);
                }

                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnMirrorX_Click(DecentForms.ControlBase Sender)
        {
            MirrorX();
        }



        private void btnMirrorY_Click(DecentForms.ControlBase Sender)
        {
            MirrorY();
        }



        void pictureEditor_PostPaint(GR.Image.FastImage TargetBuffer)
        {
            if (m_SpriteProject.ShowGrid)
            {
                // Multicolour pixels are 2px wide, so the vertical grid has half as many
                // columns. Each grid pixel is white blended over the sprite by
                // GridOpacity — FastImage.SetPixel writes raw ARGB without alpha
                // compositing, so we blend by hand (see BlendGridPixel).
                int gridCols = Lookup.SpriteHasMulticolorEnabled(m_SpriteProject.Sprites[m_CurrentSprite].Mode)
                               ? (m_SpriteWidth / 2) : m_SpriteWidth;
                if (gridCols < 1) gridCols = 1;

                for (int i = 0; i < gridCols; ++i)
                {
                    int x = i * pictureEditor.ClientRectangle.Width / gridCols;
                    for (int j = 0; j < TargetBuffer.Height; ++j)
                    {
                        TargetBuffer.SetPixel(x, j, BlendGridPixel(TargetBuffer.GetPixel(x, j)));
                    }
                }
                for (int i = 0; i < m_SpriteHeight; ++i)
                {
                    int y = i * pictureEditor.ClientRectangle.Height / m_SpriteHeight;
                    for (int j = 0; j < TargetBuffer.Width; ++j)
                    {
                        TargetBuffer.SetPixel(j, y, BlendGridPixel(TargetBuffer.GetPixel(j, y)));
                    }
                }
            }
        }



        private void checkShowGrid_CheckedChanged(object sender, EventArgs e)
        {
            m_SpriteProject.ShowGrid = checkShowGrid.Checked;
            pictureEditor.Invalidate();
        }



        /// <summary>
        /// White grid pixel blended over the underlying pixel by the project's
        /// GridOpacity (alpha 0..255). 255 = solid white (legacy), 0 = invisible.
        /// FastImage.SetPixel writes raw ARGB without blending, so we composite here.
        /// </summary>
        private uint BlendGridPixel(uint Under)
        {
            int a = m_SpriteProject.GridOpacity;
            if (a >= 255) return 0xffffffff;
            if (a <= 0) return Under;
            int ia = 255 - a;
            uint r = (uint)((((Under >> 16) & 0xff) * ia + 255 * a) / 255);
            uint g = (uint)((((Under >> 8) & 0xff) * ia + 255 * a) / 255);
            uint b = (uint)(((Under & 0xff) * ia + 255 * a) / 255);
            return 0xff000000 | (r << 16) | (g << 8) | b;
        }



        private void trackGridOpacity_Scroll(object sender, EventArgs e)
        {
            // Scroll fires on user interaction only (not on programmatic Value sets),
            // and the guard is belt-and-suspenders against load-time updates.
            if (DoNotUpdateFromControls)
            {
                return;
            }
            m_SpriteProject.GridOpacity = trackGridOpacity.Value;
            pictureEditor.Invalidate();
            SetModified();
        }



        private void btnInvert_Click(DecentForms.ControlBase Sender)
        {
            Invert();
        }



        private void Invert()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                for (int i = 0; i < sprite.Tile.Data.Length; ++i)
                {
                    byte value = (byte)(~sprite.Tile.Data.ByteAt(i));
                    sprite.Tile.Data.SetU8At(i, value);
                }
                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnRotateLeft_Click(DecentForms.ControlBase Sender)
        {
            RotateLeft();
        }



        private void RotateLeft()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                int side = Math.Min(m_SpriteHeight, m_SpriteWidth);

                var resultTile = new GraphicTile(sprite.Tile);

                for (int i = 0; i < side; ++i)
                {
                    for (int j = 0; j < side; ++j)
                    {
                        int sourceX = i;
                        int sourceY = j;
                        int targetX = j;
                        int targetY = side - 1 - i;

                        var sourceColor = sprite.Tile.GetPixel(sourceX, sourceY);
                        resultTile.SetPixel(targetX, targetY, sourceColor);
                    }
                }
                // need to black out unrotated parts
                if (side < m_SpriteWidth)
                {
                    for (int i = side; i < m_SpriteWidth; ++i)
                    {
                        for (int j = 0; j < m_SpriteHeight; ++j)
                        {
                            resultTile.SetPixel(i, j, new Tupel<ColorType, byte>(ColorType.BACKGROUND, 0));
                        }
                    }
                }
                if (side < m_SpriteHeight)
                {
                    for (int i = 0; i < m_SpriteWidth; ++i)
                    {
                        for (int j = side; j < m_SpriteHeight; ++j)
                        {
                            resultTile.SetPixel(i, j, new Tupel<ColorType, byte>(ColorType.BACKGROUND, 0));
                        }
                    }
                }
                sprite.Tile.Data = resultTile.Data;
                SpriteChanged(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnRotateRight_Click(DecentForms.ControlBase Sender)
        {
            RotateRight();
        }



        private void RotateRight()
        {
            var selectedSprites = panelSprites.SelectedIndices;

            bool firstEntry = true;
            foreach (var spriteIndex in selectedSprites)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                int side = Math.Min(m_SpriteHeight, m_SpriteWidth);

                var resultTile = new GraphicTile(sprite.Tile);

                for (int i = 0; i < side; ++i)
                {
                    for (int j = 0; j < side; ++j)
                    {
                        int sourceX = i;
                        int sourceY = j;
                        int targetX = side - 1 - j;
                        int targetY = i;

                        var sourceColor = sprite.Tile.GetPixel(sourceX, sourceY);
                        resultTile.SetPixel(targetX, targetY, sourceColor);
                    }
                }
                // need to black out unrotated parts
                if (side < m_SpriteWidth)
                {
                    for (int i = side; i < m_SpriteWidth; ++i)
                    {
                        for (int j = 0; j < m_SpriteHeight; ++j)
                        {
                            resultTile.SetPixel(i, j, new Tupel<ColorType, byte>(ColorType.BACKGROUND, 0));
                        }
                    }
                }
                if (side < m_SpriteHeight)
                {
                    for (int i = 0; i < m_SpriteWidth; ++i)
                    {
                        for (int j = side; j < m_SpriteHeight; ++j)
                        {
                            resultTile.SetPixel(i, j, new Tupel<ColorType, byte>(ColorType.BACKGROUND, 0));
                        }
                    }
                }
                sprite.Tile.Data = resultTile.Data;
                SpriteChanged(spriteIndex);
            }
            pictureEditor.Invalidate();
            Modified = true;
        }



        private void btnDeleteSprite_Click(DecentForms.ControlBase Sender)
        {
            if (panelSprites.SelectedIndex == -1)
            {
                return;
            }

            for (int i = 0; i < m_SpriteProject.Sprites.Count; ++i)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i), i == 0);
            }

            List<int> selectedSprites = panelSprites.SelectedIndices;

            int firstSelectedIndex = 0;
            if (selectedSprites.Count > 0)
            {
                firstSelectedIndex = selectedSprites[0];
            }

            selectedSprites.Reverse();

            foreach (var index in selectedSprites)
            {
                int indexToRemove = index;

                m_SpriteProject.Sprites.RemoveAt(indexToRemove);
                panelSprites.Items.RemoveAt(indexToRemove);

                // add empty sprite in back
                m_SpriteProject.Sprites.Add(new SpriteProject.SpriteData(m_SpriteProject.Colors));
                panelSprites.Items.Add((m_SpriteProject.Sprites.Count - 1).ToString(), m_SpriteProject.Sprites[m_SpriteProject.Sprites.Count - 1].Tile.Image);
            }
            if (firstSelectedIndex < panelSprites.Items.Count)
            {
                panelSprites.SelectedIndex = firstSelectedIndex;
            }
            else
            {
                panelSprites.SelectedIndex = 0;
            }
            panelSprites_SelectedIndexChanged(null, null);

            SetModified();
        }



        private void editDataExport_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((System.Windows.Forms.Control.ModifierKeys == Keys.Control)
            && (e.KeyChar == 1))
            {
                editDataExport.SelectAll();
                e.Handled = true;
            }
        }



        private void comboExportRange_SelectedIndexChanged(object sender, EventArgs e)
        {
            labelCharactersFrom.Enabled = (comboExportRange.SelectedIndex == 2);
            editSpriteFrom.Enabled = (comboExportRange.SelectedIndex == 2);
            labelCharactersTo.Enabled = (comboExportRange.SelectedIndex == 2);
            editSpriteCount.Enabled = (comboExportRange.SelectedIndex == 2);
        }



        private void editSpriteFrom_TextChanged(object sender, EventArgs e)
        {
            int newStart = GR.Convert.ToI32(editSpriteFrom.Text);

            if (m_SpriteProject.ExportStartIndex != newStart)
            {
                m_SpriteProject.ExportStartIndex = newStart;
                SetModified();
            }
        }



        private void editSpriteCount_TextChanged(object sender, EventArgs e)
        {
            int newCount = GR.Convert.ToI32(editSpriteCount.Text);

            if (m_SpriteProject.ExportSpriteCount != newCount)
            {
                m_SpriteProject.ExportSpriteCount = newCount;
                SetModified();
            }
        }



        public void SpriteChanged(int SpriteIndex)
        {
            RebuildSpriteImage(SpriteIndex);
            panelSprites.Items[SpriteIndex].MemoryImage = m_SpriteProject.Sprites[SpriteIndex].Tile.Image;
            if (m_CurrentSprite == SpriteIndex)
            {
                pictureEditor.Image = m_SpriteProject.Sprites[SpriteIndex].Tile.Image;
                pictureEditor.Invalidate();
            }
            panelSprites.InvalidateItemRect(SpriteIndex);
            if (m_CurrentSprite == SpriteIndex)
            {
                // can only do those once a list of undo steps has been completely finished undoing
                DoNotUpdateFromControls = true;

                if ((m_SpriteProject.Sprites[SpriteIndex].Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR) != _ColorSettingsDlg.MultiColorEnabled)
                {
                    _ColorSettingsDlg.MultiColorEnabled = (m_SpriteProject.Sprites[SpriteIndex].Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR);
                }
                if (Lookup.HaveCustomSpriteColor(m_SpriteProject.Mode))
                {
                    if (m_SpriteProject.Sprites[SpriteIndex].Tile.CustomColor != _ColorSettingsDlg.CustomColor)
                    {
                        _ColorSettingsDlg.CustomColor = m_SpriteProject.Sprites[SpriteIndex].Tile.CustomColor;
                    }
                }
                if (m_SpriteProject.Sprites[SpriteIndex].Tile.Colors.ActivePalette != _ColorSettingsDlg.ActivePalette)
                {
                    _ColorSettingsDlg.ActivePalette = m_SpriteProject.Sprites[SpriteIndex].Tile.Colors.ActivePalette;
                }
                DoNotUpdateFromControls = false;
            }
            CurrentSpriteModified();
            SetModified();
        }



        public void ColorsChanged()
        {
            btnChangeMode.Text = GR.EnumHelper.GetDescription(m_SpriteProject.Mode);
            AdjustSpriteSizes();
            ChangeColorSettingsDialog();
            OnPaletteChanged();

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                int activePal = m_SpriteProject.Sprites[i].Tile.Colors.ActivePalette;
                m_SpriteProject.Sprites[i].Tile.Colors.Palettes = m_SpriteProject.Colors.Palettes;
                m_SpriteProject.Sprites[i].Tile.Colors.ActivePalette = activePal;

                RebuildSpriteImage(i);

                if (i == m_CurrentSprite)
                {
                    _ColorSettingsDlg.ActivePalette = activePal;
                }
            }
            pictureEditor.Invalidate();
            panelSprites.Invalidate();

            SetModified();
        }





        public void ReplaceSpriteColors(ColorType Color1, ColorType Color2)
        {
            foreach (var spriteIndex in panelSprites.SelectedIndices)
            {
                ReplaceSpriteColors(m_SpriteProject.Sprites[spriteIndex], Color1, Color2);
            }
        }



        private void ReplaceSpriteColors(SpriteProject.SpriteData Sprite, ColorType Color1, ColorType Color2)
        {
            if (Sprite == null)
            {
                Debug.Log("ReplaceSpriteColors invalid sprite passed");
                return;
            }
            for (int y = 0; y < m_SpriteHeight; ++y)
            {
                for (int x = 0; x < m_SpriteWidth; x += Lookup.PixelWidth(Sprite.Tile.Mode))
                {
                    ColorType color = Sprite.Tile.GetPixel(x, y).first;
                    if (color == Color1)
                    {
                        Sprite.Tile.SetPixel(x, y, new Tupel<ColorType, byte>(Color2, 0));
                    }
                    else if (color == Color2)
                    {
                        Sprite.Tile.SetPixel(x, y, new Tupel<ColorType, byte>(Color1, 0));
                    }
                }
            }
        }



        public void ImportFromData(ByteBuffer SpriteData)
        {
            if (SpriteData == null)
            {
                return;
            }
            int numBytesPerSprite = Lookup.NumBytesOfSingleSprite(m_SpriteProject.Mode);
            int numBytesPerSpritePadded = Lookup.NumPaddedBytesOfSingleSprite(m_SpriteProject.Mode);

            int numSprites = (int)SpriteData.Length / numBytesPerSprite;
            numSprites = Math.Min(numSprites, m_SpriteProject.TotalNumberOfSprites);
            for (int i = 0; i < numSprites; ++i)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i), i == 0);

                SpriteData.CopyTo(m_SpriteProject.Sprites[i].Tile.Data, i * numBytesPerSpritePadded, numBytesPerSprite);
                if (m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC)
                {
                    m_SpriteProject.Sprites[i].Tile.CustomColor = (byte)(SpriteData.ByteAt(i * numBytesPerSpritePadded + numBytesPerSprite) & 0xf);
                }
                else
                {
                    m_SpriteProject.Sprites[i].Tile.CustomColor = 1;
                }
                RebuildSpriteImage(i);
            }

            editSpriteFrom.Text = "0";
            editSpriteCount.Text = numSprites.ToString();

            panelSprites.Invalidate();
            pictureEditor.Invalidate();
            Modified = false;

            saveSpriteProjectToolStripMenuItem.Enabled = true;
            closeCharsetProjectToolStripMenuItem.Enabled = true;
        }



        public override bool ApplyFunction(Function Function)
        {
            if (!pictureEditor.Focused)
            {
                return false;
            }
            switch (Function)
            {
                case Function.GRAPHIC_ELEMENT_MIRROR_H:
                    MirrorX();
                    return true;
                case Function.GRAPHIC_ELEMENT_MIRROR_V:
                    MirrorY();
                    return true;
                case Function.GRAPHIC_ELEMENT_SHIFT_D:
                    ShiftDown();
                    return true;
                case Function.GRAPHIC_ELEMENT_SHIFT_U:
                    ShiftUp();
                    return true;
                case Function.GRAPHIC_ELEMENT_SHIFT_L:
                    ShiftLeft();
                    return true;
                case Function.GRAPHIC_ELEMENT_SHIFT_R:
                    ShiftRight();
                    return true;
                case Function.GRAPHIC_ELEMENT_ROTATE_L:
                    RotateLeft();
                    return true;
                case Function.GRAPHIC_ELEMENT_ROTATE_R:
                    RotateRight();
                    return true;
                case Function.GRAPHIC_ELEMENT_INVERT:
                    Invert();
                    return true;
                case Function.GRAPHIC_ELEMENT_PREVIOUS:
                    Previous();
                    return true;
                case Function.GRAPHIC_ELEMENT_NEXT:
                    Next();
                    return true;
                case Function.GRAPHIC_ELEMENT_CUSTOM_COLOR:
                    CustomColor();
                    return true;
                case Function.GRAPHIC_ELEMENT_MULTI_COLOR_1:
                    MultiColor1();
                    return true;
                case Function.GRAPHIC_ELEMENT_MULTI_COLOR_2:
                    MultiColor2();
                    return true;
                case Function.GRAPHIC_ELEMENT_BACKGROUND_COLOR:
                    BackgroundColor();
                    return true;
            }
            return base.ApplyFunction(Function);
        }



        private void MultiColor2()
        {
            _ColorSettingsDlg.SelectedColor = ColorType.MULTICOLOR_2;
        }



        private void BackgroundColor()
        {
            _ColorSettingsDlg.SelectedColor = ColorType.BACKGROUND;
        }



        private void MultiColor1()
        {
            _ColorSettingsDlg.SelectedColor = ColorType.MULTICOLOR_1;
        }



        private void CustomColor()
        {
            _ColorSettingsDlg.SelectedColor = ColorType.CUSTOM_COLOR;
        }



        private void Next()
        {
            panelSprites.SelectedIndex = (panelSprites.SelectedIndex + 1) % 256;
        }



        private void Previous()
        {
            panelSprites.SelectedIndex = (panelSprites.SelectedIndex + 256 - 1) % 256;
        }



        private ByteBuffer GatherExportData()
        {
            GR.Memory.ByteBuffer exportData = new GR.Memory.ByteBuffer();

            var exportIndices = GetExportIndices();
            for (int i = 0; i < exportIndices.Count; ++i)
            {
                exportData.Append(m_SpriteProject.Sprites[exportIndices[i]].Tile.Data);

                // C64 usually has 63 bytes, so 1 byte is padded with the color
                if (m_SpriteProject.Mode == SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC)
                {
                    byte color = (byte)m_SpriteProject.Sprites[exportIndices[i]].Tile.CustomColor;
                    if (m_SpriteProject.Sprites[exportIndices[i]].Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR)
                    {
                        color |= 0x80;
                    }
                    exportData.AppendU8(color);
                }
            }
            return exportData;
        }



        private void AdjustSpriteSizes()
        {
            m_SpriteWidth = Lookup.SpriteWidth(m_SpriteProject.Mode);
            m_SpriteHeight = Lookup.SpriteHeight(m_SpriteProject.Mode);

            // adjust aspect ratio of the editor
            AdjustSpriteEditorSizeRatio();

            panelSprites.ItemWidth = m_SpriteWidth;
            panelSprites.ItemHeight = m_SpriteHeight;
            int newWidth = (panelSprites.ClientSize.Width / 2) * 2;
            int newHeight = (panelSprites.ClientSize.Height / 2) * 2;
            if ((newWidth != panelSprites.ClientSize.Width)
            || (newHeight != panelSprites.ClientSize.Height))
            {
                panelSprites.ClientSize = new Size(newWidth, newHeight);
                panelSprites.SetDisplaySize(newWidth / 2, newHeight / 2);
            }
            else
            {
                panelSprites.SetDisplaySize(panelSprites.ClientSize.Width / 2, panelSprites.ClientSize.Height / 2);
            }
        }



        private void AdjustSpriteEditorSizeRatio()
        {
            int biggerSize = Math.Max(m_SpriteWidth, m_SpriteHeight);

            int gridCellWidth = m_SpriteEditorOrigWidth / biggerSize;
            int gridCellHeight = m_SpriteEditorOrigHeight / biggerSize;

            int gridCellSize = Math.Min(gridCellWidth, gridCellHeight);

            pictureEditor.ClientSize = new System.Drawing.Size(m_SpriteWidth * gridCellSize,
                                                                m_SpriteHeight * gridCellSize);

            pictureEditor.DisplayPage.Create(m_SpriteWidth, m_SpriteHeight, GR.Drawing.PixelFormat.Format32bppRgb);
        }



        private void OnPaletteChanged()
        {
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                RebuildSpriteImage(i);
                panelSprites.Items[i].MemoryImage = m_SpriteProject.Sprites[i].Tile.Image;
            }

            panelSprites.Invalidate();
            pictureEditor.Invalidate();
        }



        private void btnEditPalette_Click(object sender, EventArgs e)
        {
            var dlgPalette = new DlgPaletteEditor(Core, m_SpriteProject.Colors);
            if (dlgPalette.ShowDialog() == DialogResult.OK)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));
                m_SpriteProject.Colors.Palettes = dlgPalette.Colors.Palettes;

                SetModified();
                OnPaletteChanged();
            }
        }



        private void comboSpriteColor_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            if (Lookup.SpriteModeSupportsMulticolorFlag(m_SpriteProject.Mode))
            {
                Core?.Theming.DrawMultiColorComboBox(combo, e, m_SpriteProject.Colors.Palette);
            }
            else
            {
                Core?.Theming.DrawSingleColorComboBox(combo, e, m_SpriteProject.Colors.Palette);
            }
        }



        private void btnToolEdit_CheckedChanged(DecentForms.ControlBase Sender)
        {
            m_Mode = ToolMode.SINGLE_PIXEL;
        }



        private void btnToolFill_CheckedChanged(DecentForms.ControlBase Sender)
        {
            m_Mode = ToolMode.FILL;
        }



        public void ChangeColorSettingsDialog()
        {
            if (_ColorSettingsDlg != null)
            {
                panelColorSettings.Controls.Remove(_ColorSettingsDlg);
                _ColorSettingsDlg.Dispose();
                _ColorSettingsDlg = null;
            }

            switch (m_SpriteProject.Mode)
            {
                case SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC:
                case SpriteProject.SpriteProjectMode.MEGA65_64_X_21_HIRES_OR_MC:
                    _ColorSettingsDlg = new ColorSettingsMCSprites(Core,
                                                                    m_SpriteProject.Colors,
                                                                    m_SpriteProject.Sprites[m_CurrentSprite].Tile.CustomColor,
                                                                    m_SpriteProject.Sprites[m_CurrentSprite].Tile.Mode == GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES);
                    break;
                case SpriteProject.SpriteProjectMode.MEGA65_16_X_21_16_COLORS:
                    _ColorSettingsDlg = new ColorSettingsMega6516Colors(Core, m_SpriteProject.Colors, m_SpriteProject.Sprites[m_CurrentSprite].Tile.CustomColor);
                    break;
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_8_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_16_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_32_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_64_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_8_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_16_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_32_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_64_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_8_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_16_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_32_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_64_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_8_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_16_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_32_16_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_64_16_COLORS:
                    _ColorSettingsDlg = new ColorSettingsX16Sprites16(Core, m_SpriteProject.Colors, m_SpriteProject.Sprites[m_CurrentSprite].Tile.CustomColor);
                    break;
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_8_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_16_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_32_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_8_64_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_8_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_16_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_32_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_16_64_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_8_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_16_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_32_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_32_64_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_8_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_16_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_32_256_COLORS:
                case SpriteProject.SpriteProjectMode.COMMANDER_X16_64_64_256_COLORS:
                    _ColorSettingsDlg = new ColorSettingsX16Sprites256(Core, m_SpriteProject.Colors, m_SpriteProject.Sprites[m_CurrentSprite].Tile.CustomColor);
                    break;
                default:
                    Debug.Log("ChangeColorSettingsDialog unsupported mode " + m_SpriteProject.Mode);
                    break;
            }
            panelColorSettings.Controls.Add(_ColorSettingsDlg);
            _ColorSettingsDlg.SelectedColorChanged += _ColorSettingsDlg_SelectedColorChanged;
            _ColorSettingsDlg.ColorsModified += _ColorSettingsDlg_ColorsModified;
            _ColorSettingsDlg.ColorsExchanged += _ColorSettingsDlg_ColorsExchanged;
            _ColorSettingsDlg.PaletteModified += _ColorSettingsDlg_PaletteModified;
            _ColorSettingsDlg.PaletteSelected += _ColorSettingsDlg_PaletteSelected;
            _ColorSettingsDlg.MulticolorFlagChanged += _ColorSettingsDlg_MulticolorFlagChanged;

            _ColorSettingsDlg_SelectedColorChanged(_ColorSettingsDlg.SelectedColor);
        }



        private void _ColorSettingsDlg_PaletteSelected(ColorSettings Colors)
        {
            if (DoNotUpdateFromControls)
            {
                return;
            }
            DocumentInfo.UndoManager.StartUndoGroup();

            var selectedSprites = panelSprites.SelectedIndices;

            foreach (var i in selectedSprites)
            {
                if ((m_SpriteProject.Sprites[i].Tile.Colors.PaletteOffset != Colors.PaletteOffset)
                || (m_SpriteProject.Sprites[i].Tile.Colors.ActivePalette != Colors.ActivePalette))
                {
                    DocumentInfo.UndoManager.AddGroupedUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i));
                    Modified = true;
                    m_SpriteProject.Sprites[i].Tile.Colors.ActivePalette = Colors.ActivePalette;
                    m_SpriteProject.Sprites[i].Tile.Colors.PaletteOffset = Colors.PaletteOffset;
                    RebuildSpriteImage(i);
                    if (m_CurrentSprite == i)
                    {
                        pictureEditor.Invalidate();
                    }
                    panelSprites.InvalidateItemRect(i);
                }
            }
        }



        private void _ColorSettingsDlg_MulticolorFlagChanged()
        {
            if (DoNotUpdateFromControls)
            {
                return;
            }
            DocumentInfo.UndoManager.StartUndoGroup();

            var selectedSprites = panelSprites.SelectedIndices;

            foreach (var i in selectedSprites)
            {
                if (Lookup.SpriteModeSupportsMulticolorFlag(m_SpriteProject.Mode))
                {
                    if (Lookup.SpriteHasMulticolorEnabled(m_SpriteProject.Sprites[i].Mode) != _ColorSettingsDlg.MultiColorEnabled)
                    {
                        DocumentInfo.UndoManager.AddGroupedUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i));

                        SetMulticolorMode(ref m_SpriteProject.Sprites[i].Mode, _ColorSettingsDlg.MultiColorEnabled);
                        m_SpriteProject.Sprites[i].Tile.Mode = _ColorSettingsDlg.MultiColorEnabled ? GraphicTileMode.COMMODORE_MULTICOLOR_SPRITES : GraphicTileMode.COMMODORE_HIRES;

                        Modified = true;
                        RebuildSpriteImage(i);
                        if (m_CurrentSprite == i)
                        {
                            pictureEditor.Invalidate();
                        }
                        panelSprites.InvalidateItemRect(i);
                    }
                }
            }
        }



        private void SetMulticolorMode(ref SpriteMode Mode, bool MultiColorEnabled)
        {
            if ((MultiColorEnabled)
            && (Mode == SpriteMode.COMMODORE_24_X_21_HIRES))
            {
                Mode = SpriteMode.COMMODORE_24_X_21_MULTICOLOR;
            }
            if ((MultiColorEnabled)
            && (Mode == SpriteMode.MEGA65_64_X_21_16_HIRES))
            {
                Mode = SpriteMode.MEGA65_64_X_21_16_MULTICOLOR;
            }
            if ((!MultiColorEnabled)
            && (Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR))
            {
                Mode = SpriteMode.COMMODORE_24_X_21_HIRES;
            }
            if ((!MultiColorEnabled)
            && (Mode == SpriteMode.MEGA65_64_X_21_16_MULTICOLOR))
            {
                Mode = SpriteMode.MEGA65_64_X_21_16_HIRES;
            }
        }



        private void _ColorSettingsDlg_PaletteModified(ColorSettings Colors, int CustomColor, List<int> PaletteMapping)
        {
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));

            m_SpriteProject.Colors = new ColorSettings(Colors);

            // make sure all sprites still have valid palette indices!
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                var sprite = m_SpriteProject.Sprites[i];

                int origPalIndex = sprite.Tile.Colors.ActivePalette;
                sprite.Tile.Colors = new ColorSettings(Colors);
                sprite.Tile.Colors.ActivePalette = origPalIndex;

                int newPalIndex = PaletteMapping[origPalIndex];
                if (newPalIndex == -1)
                {
                    // a new palette, or a removed one, reset to slot 0
                    newPalIndex = 0;
                }

                if (sprite.Tile.Colors.ActivePalette != newPalIndex)
                {
                    DocumentInfo.UndoManager.AddGroupedUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i));
                    sprite.Tile.Colors.ActivePalette = newPalIndex;

                    RebuildSpriteImage(i);

                    if (m_CurrentSprite == i)
                    {
                        _ColorSettingsDlg.ActivePalette = sprite.Tile.Colors.ActivePalette;
                        pictureEditor.Invalidate();
                    }
                    panelSprites.InvalidateItemRect(i);
                }
            }

            OnPaletteChanged();

            SetModified();
        }



        private void _ColorSettingsDlg_ColorsExchanged(ColorType Color1, ColorType Color2)
        {
            bool firstEntry = true;
            foreach (int spriteIndex in panelSprites.SelectedIndices)
            {
                var sprite = m_SpriteProject.Sprites[spriteIndex];

                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstEntry);
                firstEntry = false;

                ReplaceSpriteColors(sprite, Color1, Color2);

                RebuildSpriteImage(spriteIndex);
                panelSprites.InvalidateItemRect(spriteIndex);

                if (spriteIndex == m_CurrentSprite)
                {
                    pictureEditor.Invalidate();
                }
            }
        }



        private void _ColorSettingsDlg_ColorsModified(ColorType Color, ColorSettings Colors, int CustomColor)
        {
            if (DoNotUpdateFromControls)
            {
                return;
            }

            switch (Color)
            {
                case ColorType.BACKGROUND:
                    if (m_SpriteProject.Colors.BackgroundColor != _ColorSettingsDlg.Colors.BackgroundColor)
                    {
                        DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));

                        m_SpriteProject.Colors.BackgroundColor = _ColorSettingsDlg.Colors.BackgroundColor;
                        Modified = true;
                        for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
                        {
                            RebuildSpriteImage(i);
                        }
                        pictureEditor.Invalidate();
                        panelSprites.Invalidate();
                    }
                    break;
                case ColorType.MULTICOLOR_1:
                    if (m_SpriteProject.Colors.MultiColor1 != _ColorSettingsDlg.Colors.MultiColor1)
                    {
                        DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));

                        m_SpriteProject.Colors.MultiColor1 = _ColorSettingsDlg.Colors.MultiColor1;
                        Modified = true;
                        for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
                        {
                            RebuildSpriteImage(i);
                        }
                        pictureEditor.Invalidate();
                        panelSprites.Invalidate();
                    }
                    break;
                case ColorType.MULTICOLOR_2:
                    if (m_SpriteProject.Colors.MultiColor2 != _ColorSettingsDlg.Colors.MultiColor2)
                    {
                        DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));

                        m_SpriteProject.Colors.MultiColor2 = _ColorSettingsDlg.Colors.MultiColor2;
                        Modified = true;
                        for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
                        {
                            RebuildSpriteImage(i);
                        }
                        pictureEditor.Invalidate();
                        panelSprites.Invalidate();
                    }
                    break;
                case ColorType.CUSTOM_COLOR:
                    if (Lookup.HaveCustomSpriteColor(m_SpriteProject.Mode))
                    {
                        DocumentInfo.UndoManager.StartUndoGroup();

                        var selectedSprites = panelSprites.SelectedIndices;

                        foreach (var i in selectedSprites)
                        {
                            if (m_SpriteProject.Sprites[i].Tile.CustomColor != _ColorSettingsDlg.CustomColor)
                            {
                                DocumentInfo.UndoManager.AddGroupedUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i));
                                m_SpriteProject.Sprites[i].Tile.CustomColor = _ColorSettingsDlg.CustomColor;
                                Modified = true;
                                RebuildSpriteImage(i);
                                if (i == m_CurrentSprite)
                                {
                                    pictureEditor.Invalidate();
                                }
                                panelSprites.InvalidateItemRect(i);
                            }
                        }
                    }
                    else
                    {
                        var selectedSprites = panelSprites.SelectedIndices;
                        foreach (var i in selectedSprites)
                        {
                            if (m_SpriteProject.Sprites[i].Tile.CustomColor != _ColorSettingsDlg.CustomColor)
                            {
                                m_SpriteProject.Sprites[i].Tile.CustomColor = _ColorSettingsDlg.CustomColor;
                            }
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }



        private void _ColorSettingsDlg_SelectedColorChanged(ColorType Color)
        {
        }



        private void btnClearSprite_Click(DecentForms.ControlBase Sender)
        {
            List<int> selectedImages = panelSprites.SelectedIndices;
            if (selectedImages.Count == 0)
            {
                return;
            }

            bool firstSprite = true;
            foreach (var spriteIndex in selectedImages)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, spriteIndex), firstSprite);
                firstSprite = false;

                for (int i = 0; i < m_SpriteProject.Sprites[spriteIndex].Tile.Width; ++i)
                {
                    for (int j = 0; j < m_SpriteProject.Sprites[spriteIndex].Tile.Height; ++j)
                    {
                        m_SpriteProject.Sprites[spriteIndex].Tile.SetPixel(i, j, new Tupel<ColorType, byte>(ColorType.BACKGROUND, 0));
                    }
                }
                SpriteChanged(spriteIndex);
            }
            SetModified();
        }



        private void comboExportMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            // This fires only on a genuine USER change: programmatic restores
            // detach this handler first (constructor / LoadDocument). So it is
            // always safe to remember the choice and mark the document modified.
            RebuildExportForm();
            m_SpriteProject.ExportMethodIndex = (comboExportMethod.SelectedIndex >= 0) ? comboExportMethod.SelectedIndex : 0;
            Modified = true;
        }



        // Dispose the current export sub-form and build the one for the selected
        // method. Does NOT touch the modified flag or the saved method index, so
        // it can be called freely during construction/load.
        private void RebuildExportForm()
        {
            if (m_ExportForm != null)
            {
                m_ExportForm.SettingsChanged -= ExportForm_SettingsChanged;
                m_ExportForm.Dispose();
                m_ExportForm = null;
            }

            editDataExport.Text = "";
            editDataExport.Font = m_DefaultOutputFont;

            var item = (GR.Generic.Tupel<string, Type>)comboExportMethod.SelectedItem;
            if ((item == null)
            || (item.second == null))
            {
                return;
            }
            m_ExportForm = (ExportSpriteFormBase)Activator.CreateInstance(item.second, new object[] { Core });
            m_ExportForm.Parent = panelExport;
            m_ExportForm.CreateControl();
            // The document was themed at load time, before this form existed, so
            // its freshly-created controls are still unthemed - recolor them now.
            Core.Theming.ApplyTheme(m_ExportForm);
            // A user edit on the form flushes the values + marks modified via this
            // subscription. Wire it BEFORE ApplyExportSettings; the form detaches
            // its own change handlers while populating, so loading the persisted
            // settings does not raise SettingsChanged (and does not dirty the doc).
            m_ExportForm.SettingsChanged += ExportForm_SettingsChanged;
            m_ExportForm.ApplyExportSettings(m_SpriteProject);
        }



        private void ExportForm_SettingsChanged(object sender, EventArgs e)
        {
            if (m_ExportForm != null)
            {
                m_ExportForm.UpdateExportSettings(m_SpriteProject);
            }
            Modified = true;
        }


        private void btnExport_Click(DecentForms.ControlBase Sender)
        {
            List<int> exportIndices = GetExportIndices();

            var exportInfo = new ExportSpriteInfo()
            {
                Project = m_SpriteProject,
                ExportData = GatherExportData(),
                ExportIndices = exportIndices
            };

            editDataExport.Text = "";
            editDataExport.Font = m_DefaultOutputFont;
            m_ExportForm.HandleExport(exportInfo, editDataExport, DocumentInfo);
        }



        private void comboImportMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_ImportForm != null)
            {
                m_ImportForm.Dispose();
                m_ImportForm = null;
            }

            var item = (GR.Generic.Tupel<string, Type>)comboImportMethod.SelectedItem;
            if ((item == null)
            || (item.second == null))
            {
                return;
            }
            m_ImportForm = (ImportSpriteFormBase)Activator.CreateInstance(item.second, new object[] { Core });
            m_ImportForm.Parent = panelImport;
            m_ImportForm.Size = panelImport.ClientSize;
            m_ImportForm.CreateControl();
            // Theme this freshly-created form (the document was themed before it existed).
            Core.Theming.ApplyTheme(m_ImportForm);
        }



        private void btnImport_Click(DecentForms.ControlBase Sender)
        {
            var undos = new List<Undo.UndoTask>();

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                undos.Add(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i));
            }
            undos.Add(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject));

            if (m_ImportForm.HandleImport(m_SpriteProject, this))
            {
                DocumentInfo.UndoManager.StartUndoGroup();
                foreach (var undo in undos)
                {
                    DocumentInfo.UndoManager.AddUndoTask(undo, false);
                }
                SetModified();
            }
        }



        private void btnChangeMode_Click(DecentForms.ControlBase Sender)
        {
            contextMenuChangeMode.Show(btnChangeMode, new Point(0, btnChangeMode.Height));
        }



        private void spriteModeChangedMenuItem_Click(object sender, EventArgs e)
        {
            var newMode = (SpriteProject.SpriteProjectMode)((ToolStripMenuItem)sender).Tag;

            ChangeMode(newMode);
        }



        private void ChangeMode(SpriteProject.SpriteProjectMode Mode)
        {
            if (DoNotUpdateFromControls)
            {
                return;
            }
            if (m_SpriteProject.Mode == Mode)
            {
                return;
            }

            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetValuesChange(this, m_SpriteProject), true);

            m_SpriteProject.Mode = Mode;
            btnChangeMode.Text = GR.EnumHelper.GetDescription(m_SpriteProject.Mode);

            AdjustSpriteSizes();

            m_SpriteProject.Colors.Palette = PaletteManager.PaletteFromMode(m_SpriteProject.Mode);
            ChangeColorSettingsDialog();

            //OnPaletteChanged();

            panelSprites.Items.Clear();

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, i), false);

                m_SpriteProject.Sprites[i].Mode = Lookup.SpriteModeFromSpriteProjectMode(m_SpriteProject.Mode);
                m_SpriteProject.Sprites[i].Tile.Mode = Lookup.GraphicTileModeFromSpriteProjectMode(m_SpriteProject.Mode);
                m_SpriteProject.Sprites[i].Tile.Data.Resize((uint)Lookup.NumBytesOfSingleSprite(m_SpriteProject.Mode));
                m_SpriteProject.Sprites[i].Tile.Width = m_SpriteWidth;
                m_SpriteProject.Sprites[i].Tile.Height = m_SpriteHeight;
                m_SpriteProject.Sprites[i].Tile.Image.Resize(m_SpriteWidth, m_SpriteHeight);
                m_SpriteProject.Sprites[i].Tile.Colors = new ColorSettings(m_SpriteProject.Colors);

                switch (m_SpriteProject.Mode)
                {
                    case SpriteProject.SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC:
                        if ((m_SpriteProject.Sprites[i].Mode != SpriteMode.COMMODORE_24_X_21_HIRES)
                        && (m_SpriteProject.Sprites[i].Mode != SpriteMode.COMMODORE_24_X_21_MULTICOLOR))
                        {
                            m_SpriteProject.Sprites[i].Mode = SpriteMode.COMMODORE_24_X_21_HIRES;
                        }
                        break;
                    case SpriteProject.SpriteProjectMode.MEGA65_64_X_21_HIRES_OR_MC:
                        m_SpriteProject.Sprites[i].Mode = SpriteMode.MEGA65_64_X_21_16_HIRES;
                        break;
                    default:
                        m_SpriteProject.Sprites[i].Mode = Lookup.SpriteModeFromSpriteProjectMode(m_SpriteProject.Mode);
                        break;
                }

                RebuildSpriteImage(i);

                panelSprites.Items.Add(i.ToString(), m_SpriteProject.Sprites[i].Tile.Image);
            }

            panelSprites.Invalidate();
            SetModified();
            pictureEditor.Invalidate();
        }



        private void btnHighlightDuplicates_Click(DecentForms.ControlBase Sender)
        {
            var duplicateGroups = new Map<ByteBuffer, int>();
            var itemGroup = new Map<int, int>();

            panelSprites.BeginUpdate();
            bool hasHighlight = false;
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                if (panelSprites.Items[i].Highlighted)
                {
                    hasHighlight = true;
                    panelSprites.Items[i].Highlighted = false;
                }
            }
            if (hasHighlight)
            {
                panelSprites.EndUpdate();
                return;
            }

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                panelSprites.Items[i].Highlighted = false;
            }
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites - 1; ++i)
            {
                for (int j = i + 1; j < m_SpriteProject.TotalNumberOfSprites; ++j)
                {
                    if (m_SpriteProject.Sprites[i].Tile.Data == m_SpriteProject.Sprites[j].Tile.Data)
                    {
                        int duplicateGroup = -1;
                        if (duplicateGroups.TryGetValue(m_SpriteProject.Sprites[i].Tile.Data, out duplicateGroup))
                        {
                            itemGroup.Add(i, duplicateGroup);
                            itemGroup.Add(j, duplicateGroup);
                        }
                        else
                        {
                            duplicateGroup = duplicateGroups.Count;
                            itemGroup.Add(i, duplicateGroup);
                            itemGroup.Add(j, duplicateGroup);
                            duplicateGroups.Add(m_SpriteProject.Sprites[i].Tile.Data, duplicateGroup);
                        }

                        panelSprites.Items[i].SetHighlightGroup(duplicateGroup);
                        panelSprites.Items[j].SetHighlightGroup(duplicateGroup);
                    }
                }
            }
            panelSprites.EndUpdate();
        }



        private void panelSprites_SelectionChanged(object sender, EventArgs e)
        {
            UpdateSpriteSelectionInfo();
        }



        private void UpdateSpriteSelectionInfo()
        {
            int numSelectedSprites = panelSprites.SelectedIndices.Count;

            labelSelectionInfo.Text = $"Selected {numSelectedSprites} sprites";
            btnBankCopy.Enabled = (numSelectedSprites > 0);
            btnBankPaste.Enabled = (numSelectedSprites > 0) && (m_BankClipboard.Count > 0);
        }



        private void btnBankCopy_Click(DecentForms.ControlBase Sender)
        {
            var selected = panelSprites.SelectedIndices;
            if ((selected == null) || (selected.Count == 0)) return;

            m_BankClipboard.Clear();
            foreach (var idx in selected)
            {
                if (idx < 0 || idx >= m_SpriteProject.Sprites.Count) continue;
                // Deep-clone via the SpriteData copy constructor — gives us our
                // own GraphicTile, so subsequent edits to the source bank don't
                // bleed into the clipboard buffer.
                m_BankClipboard.Add(new Formats.SpriteProject.SpriteData(m_SpriteProject.Sprites[idx]));
            }
            btnBankPaste.Enabled = (m_BankClipboard.Count > 0);
        }



        private void btnBankPaste_Click(DecentForms.ControlBase Sender)
        {
            if (m_BankClipboard.Count == 0) return;

            int firstSel = panelSprites.SelectedIndex;
            if (firstSel < 0) firstSel = 0;

            DocumentInfo.UndoManager.StartUndoGroup();
            for (int i = 0; i < m_BankClipboard.Count; ++i)
            {
                int target = firstSel + i;
                if (target >= m_SpriteProject.Sprites.Count) break;

                DocumentInfo.UndoManager.AddGroupedUndoTask(new Undo.UndoSpritesetSpriteChange(this, m_SpriteProject, target));

                // Replace the bank entry with a fresh deep-clone of the clipboard
                // entry so further pastes from the same buffer don't share state
                // with the bank.
                m_SpriteProject.Sprites[target] = new Formats.SpriteProject.SpriteData(m_BankClipboard[i]);
                // The panelSprites Items collection holds a reference to the
                // sprite's image; rebind so the grid renders the new content.
                panelSprites.Items[target].MemoryImage = m_SpriteProject.Sprites[target].Tile.Image;
                SpriteChanged(target);
            }
            RebuildOverlayPreview();
            SetModified();
        }



        // Non-null while the application is deactivated: the preview timers
        // that were RUNNING when focus left C64Studio (paused so an
        // unfocused app consumes no CPU) and must resume when focus
        // returns. This is intrinsic suspended-playback state, not a gate:
        // the overlay animation's play/pause is user intent that no other
        // data records, so it has to be captured across the pause. Both
        // tick handlers self-stop on dead state, so resuming after the
        // world changed (overlay deleted, test instances cleared) is safe.
        private List<System.Windows.Forms.Timer> m_TimersSuspendedByAppDeactivate = null;



        public override void OnApplicationEvent(ApplicationEvent Event)
        {
            switch (Event.EventType)
            {
                case ApplicationEvent.Type.DEFAULT_PALETTE_CHANGED:
                    {
                        bool prevModified = Modified;

                        if (!string.IsNullOrEmpty(Event.OriginalValue))
                        {
                            Core.Imaging.ApplyPalette((PaletteType)Enum.Parse(typeof(PaletteType), Event.OriginalValue, true),
                                                       Lookup.PaletteTypeFromSpriteMode(m_SpriteProject.Mode),
                                                       m_SpriteProject.Colors);
                        }
                        else
                        {
                            Core.Imaging.ApplyPalette(Lookup.PaletteTypeFromSpriteMode(m_SpriteProject.Mode),
                                                       Lookup.PaletteTypeFromSpriteMode(m_SpriteProject.Mode),
                                                       m_SpriteProject.Colors);

                        }
                        ColorsChanged();

                        Modified = prevModified;
                    }
                    break;
                case ApplicationEvent.Type.APPLICATION_DEACTIVATED:
                    // Zero CPU while another program is in front: park the
                    // running preview timers, remembering which ones so the
                    // user's play/pause state survives the round trip. The
                    // null check makes a duplicate deactivation harmless
                    // (never captures an already-paused set as "running").
                    if (m_TimersSuspendedByAppDeactivate == null)
                    {
                        m_TimersSuspendedByAppDeactivate = new List<System.Windows.Forms.Timer>();
                        if (m_OverlayAnimTimer.Enabled)
                        {
                            m_TimersSuspendedByAppDeactivate.Add(m_OverlayAnimTimer);
                            m_OverlayAnimTimer.Stop();
                        }
                        if (m_SpriteTestTimer.Enabled)
                        {
                            m_TimersSuspendedByAppDeactivate.Add(m_SpriteTestTimer);
                            m_SpriteTestTimer.Stop();
                        }
                    }
                    break;
                case ApplicationEvent.Type.APPLICATION_ACTIVATED:
                    if (m_TimersSuspendedByAppDeactivate != null)
                    {
                        foreach (var timer in m_TimersSuspendedByAppDeactivate)
                        {
                            timer.Start();
                        }
                        m_TimersSuspendedByAppDeactivate = null;
                    }
                    break;
            }
        }



        private void btnMoveSelectionToTarget_Click(DecentForms.ControlBase Sender)
        {
            int targetIndex = GR.Convert.ToI32(editMoveTargetIndex.Text);

            var selection = panelSprites.SelectedIndices;
            if (selection.Count == 0)
            {
                return;
            }
            if (targetIndex + selection.Count > m_SpriteProject.TotalNumberOfSprites)
            {
                Core.Notification.MessageBox("Can't move selection", "Not enough sprites for selection starting at the given index!");
                return;
            }

            int[] spriteMapNewToOld = new int[m_SpriteProject.TotalNumberOfSprites];
            int[] spriteMapOldToNew = new int[m_SpriteProject.TotalNumberOfSprites];
            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                spriteMapNewToOld[i] = -1;
                spriteMapOldToNew[i] = -1;
            }

            int insertIndex = targetIndex;
            foreach (var entry in selection)
            {
                spriteMapNewToOld[insertIndex] = entry;
                spriteMapOldToNew[entry] = insertIndex;
                ++insertIndex;
            }

            // now fill all other entries
            byte insertSpriteIndex = 0;
            int spritePos = 0;
            while (spritePos < 256)
            {
                // already inserted, skip
                if (spriteMapNewToOld[spritePos] != -1)
                {
                    ++spritePos;
                    continue;
                }
                while (selection.Contains(insertSpriteIndex))
                {
                    ++insertSpriteIndex;
                }
                spriteMapNewToOld[spritePos] = insertSpriteIndex;
                spriteMapOldToNew[insertSpriteIndex] = spritePos;
                ++spritePos;
                ++insertSpriteIndex;
            }

            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetShiftSprites(this, m_SpriteProject, spriteMapNewToOld, spriteMapOldToNew));

            ShiftSprites(spriteMapOldToNew, spriteMapNewToOld);
            Modified = true;
        }



        public void ShiftSprites(int[] OldToNew, int[] NewToOld)
        {
            // ..and sprites
            List<SpriteProject.SpriteData> origSpriteData = new List<SpriteProject.SpriteData>();
            List<GR.Forms.ImageListbox.ImageListItem> origListItems = new List<GR.Forms.ImageListbox.ImageListItem>();
            List<GR.Forms.ImageListbox.ImageListItem> origListItems2 = new List<GR.Forms.ImageListbox.ImageListItem>();

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                origSpriteData.Add(m_SpriteProject.Sprites[i]);
                origListItems.Add(panelSprites.Items[i]);
            }

            bool currentSpriteModified = NewToOld.Contains(m_CurrentSprite);

            for (int i = 0; i < m_SpriteProject.TotalNumberOfSprites; ++i)
            {
                m_SpriteProject.Sprites[i] = origSpriteData[NewToOld[i]];
                panelSprites.Items[i] = origListItems[NewToOld[i]];
            }
            // Patch overlay frame bank-index references so they still point at
            // the same bank entry after the shift remap.
            foreach (var overlay in m_SpriteProject.Overlays)
            {
                foreach (var frame in overlay.Frames)
                {
                    for (int s = 0; s < frame.BankIndex.Length; ++s)
                    {
                        int v = frame.BankIndex[s];
                        if (v >= 0 && v < OldToNew.Length)
                        {
                            frame.BankIndex[s] = OldToNew[v];
                        }
                    }
                }
            }
            panelSprites.Invalidate();

            if (currentSpriteModified)
            {
                SpriteChanged(m_CurrentSprite);
            }
            RebuildOverlayPreview();
            pictureEditor.Invalidate();
        }



        private void panelSprites_ClientSizeChanged(object sender, EventArgs e)
        {
            var size = panelSprites.ClientSize;

            int newWidth = (size.Width / 2) * 2;
            int newHeight = (size.Height / 2) * 2;

            // we use active client size, since we want to avoid distorted display (e.g. odd client size can't map *2 factors nicely)
            panelSprites.SetDisplaySize(newWidth / 2, newHeight / 2);
            panelSprites.SetActiveClientSize(newWidth, newHeight);
        }



        // -------- Overlay tab (Phase 2) --------

        /// <summary>
        /// Build the 8 fixed slot rows inside panelOverlaySlots at construction
        /// time. The row-building exception in CLAUDE.md applies — 8 identical
        /// slot rows are repetitive enough that authoring them in code (inside
        /// a Designer-authored container Panel) is cleaner than 56 explicit
        /// designer fields. All rows share the same X coordinates, sequential
        /// Y offsets, and identical widths — overlap-free by construction.
        /// </summary>
        private void BuildOverlaySlotRows()
        {
            const int rowHeight = 22;
            const int rowY0 = 4;

            panelOverlaySlots.Controls.Clear();

            // Compact one-row layout fitting in panelOverlaySlots' 280 px
            // width (down from 345 to make room for the bank panel on the
            // right of the Overlay tab). The X:/Y:/Bank: text labels are
            // dropped — tooltips on the NUDs carry the meaning.
            for (int i = 0; i < 8; ++i)
            {
                int y = rowY0 + i * rowHeight;

                var lbl = new System.Windows.Forms.Label();
                lbl.AutoSize = true;
                lbl.Location = new System.Drawing.Point(0, y + 4);
                lbl.Text = "Slot " + i + ":";
                panelOverlaySlots.Controls.Add(lbl);

                var chkEnabled = new System.Windows.Forms.CheckBox();
                chkEnabled.AutoSize = false;
                chkEnabled.Size = new System.Drawing.Size(18, 18);
                chkEnabled.Location = new System.Drawing.Point(42, y + 1);
                chkEnabled.Tag = i;
                chkEnabled.CheckedChanged += slotEnabled_CheckedChanged;
                toolTip1.SetToolTip(chkEnabled, "Enable this slot in the overlay");
                panelOverlaySlots.Controls.Add(chkEnabled);
                m_SlotEnabled[i] = chkEnabled;

                var nudX = new System.Windows.Forms.NumericUpDown();
                nudX.Location = new System.Drawing.Point(64, y + 1);
                nudX.Size = new System.Drawing.Size(45, 20);
                nudX.Minimum = -512;
                nudX.Maximum = 512;
                nudX.Tag = i;
                nudX.ValueChanged += slotXY_ValueChanged;
                toolTip1.SetToolTip(nudX, "Slot X offset (pixels)");
                panelOverlaySlots.Controls.Add(nudX);
                m_SlotX[i] = nudX;

                var nudY = new System.Windows.Forms.NumericUpDown();
                nudY.Location = new System.Drawing.Point(112, y + 1);
                nudY.Size = new System.Drawing.Size(45, 20);
                nudY.Minimum = -512;
                nudY.Maximum = 512;
                nudY.Tag = i;
                nudY.ValueChanged += slotXY_ValueChanged;
                toolTip1.SetToolTip(nudY, "Slot Y offset (pixels)");
                panelOverlaySlots.Controls.Add(nudY);
                m_SlotY[i] = nudY;

                var nudBank = new System.Windows.Forms.NumericUpDown();
                nudBank.Location = new System.Drawing.Point(160, y + 1);
                nudBank.Size = new System.Drawing.Size(45, 20);
                nudBank.Minimum = 0;
                nudBank.Maximum = 255;
                nudBank.Tag = i;
                nudBank.ValueChanged += slotBank_ValueChanged;
                toolTip1.SetToolTip(nudBank, "Bank sprite index for this slot (frame 0)");
                panelOverlaySlots.Controls.Add(nudBank);
                m_SlotBank[i] = nudBank;

                var cmbColor = new System.Windows.Forms.ComboBox();
                cmbColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
                cmbColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                cmbColor.FormattingEnabled = true;
                cmbColor.Location = new System.Drawing.Point(208, y + 1);
                cmbColor.Size = new System.Drawing.Size(65, 21);
                cmbColor.Tag = i;
                // Index 0 = "None" (each sprite keeps its own colour); indices
                // 1..16 = colour override 0..15 forced onto every sprite/frame
                // drawn through this slot.
                cmbColor.Items.Add("None");
                for (int c = 0; c < 16; ++c) cmbColor.Items.Add(c.ToString("d2"));
                cmbColor.SelectedIndex = 0;
                cmbColor.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.slotColorOverride_DrawItem);
                cmbColor.SelectedIndexChanged += slotCustomColor_SelectedIndexChanged;
                toolTip1.SetToolTip(cmbColor, "Slot color override (None = each sprite's own color)");
                panelOverlaySlots.Controls.Add(cmbColor);
                m_SlotCustomColor[i] = cmbColor;
            }
        }



        /// <summary>
        /// Repopulate listOverlays from m_SpriteProject.Overlays. Detaches
        /// SelectedIndexChanged during the bulk update so we don't trigger
        /// stale-index callbacks mid-rebuild.
        /// </summary>
        public void RefreshOverlaysList()
        {
            if (listOverlays == null) return;

            int prev = listOverlays.SelectedIndex;
            listOverlays.SelectedIndexChanged -= listOverlays_SelectedIndexChanged;
            try
            {
                listOverlays.BeginUpdate();
                listOverlays.Items.Clear();
                for (int i = 0; i < m_SpriteProject.Overlays.Count; ++i)
                {
                    var ov = m_SpriteProject.Overlays[i];
                    listOverlays.Items.Add(string.IsNullOrEmpty(ov.Name) ? "(unnamed)" : ov.Name);
                }
                listOverlays.EndUpdate();

                if (prev >= 0 && prev < listOverlays.Items.Count)
                    listOverlays.SelectedIndex = prev;
                else if (listOverlays.Items.Count > 0)
                    listOverlays.SelectedIndex = 0;
            }
            finally
            {
                listOverlays.SelectedIndexChanged += listOverlays_SelectedIndexChanged;
            }
            PopulateOverlayFieldsFromSelection();
            btnRemoveOverlay.Enabled = (listOverlays.SelectedIndex >= 0);
            btnCloneOverlay.Enabled = (listOverlays.SelectedIndex >= 0);
        }



        private void listOverlays_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateOverlayFieldsFromSelection();
            // Running test animations reference the previously-selected overlay's
            // frames/slots — clear them so they don't bleed across overlays.
            StopSpriteTest();
            btnRemoveOverlay.Enabled = (listOverlays.SelectedIndex >= 0);
            btnCloneOverlay.Enabled = (listOverlays.SelectedIndex >= 0);
        }



        /// <summary>
        /// Mirror the selected Overlay's data into the field pane (name +
        /// 8 slot rows). Detaches all the row-control change handlers for
        /// the duration so writing into the controls doesn't dirty the
        /// model. Project rule: detach/attach instead of a populating-flag.
        /// </summary>
        private void PopulateOverlayFieldsFromSelection()
        {
            int idx = (listOverlays != null) ? listOverlays.SelectedIndex : -1;
            m_CurrentOverlay = (idx >= 0 && idx < m_SpriteProject.Overlays.Count)
                              ? m_SpriteProject.Overlays[idx]
                              : null;

            DetachOverlayFieldHandlers();
            try
            {
                if (m_CurrentOverlay == null)
                {
                    editOverlayName.Text = "";
                    editFrameDelay.Value = 5;
                    checkLoop.Checked = true;
                    checkStartRandomFrame.Checked = false;
                    editAnimationID.Value = 0;
                    for (int i = 0; i < 8; ++i)
                    {
                        m_SlotEnabled[i].Checked = false;
                        m_SlotX[i].Value = 0;
                        m_SlotY[i].Value = 0;
                        m_SlotBank[i].Value = 0;
                        m_SlotCustomColor[i].SelectedIndex = 0;   // "None"
                    }
                }
                else
                {
                    editOverlayName.Text = m_CurrentOverlay.Name ?? "";
                    editFrameDelay.Value = ClampNudInt(m_CurrentOverlay.FrameDelay, 1, 255);
                    checkLoop.Checked = m_CurrentOverlay.Loop;
                    checkStartRandomFrame.Checked = m_CurrentOverlay.StartAtRandomFrame;
                    editAnimationID.Value = ClampNudInt(m_CurrentOverlay.AnimationID, 0, 255);
                    for (int i = 0; i < 8; ++i)
                    {
                        var slot = m_CurrentOverlay.Slots[i];
                        m_SlotEnabled[i].Checked = slot.Enabled;
                        m_SlotX[i].Value = ClampNudInt(slot.X, (int)m_SlotX[i].Minimum, (int)m_SlotX[i].Maximum);
                        m_SlotY[i].Value = ClampNudInt(slot.Y, (int)m_SlotY[i].Minimum, (int)m_SlotY[i].Maximum);

                        // Bank index from frame 0 (Phase 2 single-frame view). The
                        // Animation tab in Phase 4 will manage the full frame list.
                        int bankIdx = 0;
                        if (m_CurrentOverlay.Frames.Count > 0)
                        {
                            bankIdx = m_CurrentOverlay.Frames[0].BankIndex[i];
                        }
                        m_SlotBank[i].Value = ClampNudInt(bankIdx, 0, 255);

                        // Combo index 0 = "None" (override -1), 1..16 = colours 0..15.
                        int cc = slot.CustomColor;
                        m_SlotCustomColor[i].SelectedIndex = ((cc >= 0) && (cc <= 15)) ? cc + 1 : 0;
                    }
                }
            }
            finally
            {
                AttachOverlayFieldHandlers();
            }
            RebuildOverlayPreview();
            RefreshFramesList();
        }



        private static decimal ClampNudInt(int Value, int Min, int Max)
        {
            if (Value < Min) return (decimal)Min;
            if (Value > Max) return (decimal)Max;
            return (decimal)Value;
        }



        private void AttachOverlayFieldHandlers()
        {
            editOverlayName.TextChanged += editOverlayName_TextChanged;
            editFrameDelay.ValueChanged += editFrameDelay_ValueChanged;
            checkLoop.CheckedChanged += checkLoop_CheckedChanged;
            checkStartRandomFrame.CheckedChanged += checkStartRandomFrame_CheckedChanged;
            editAnimationID.ValueChanged += editAnimationID_ValueChanged;
            for (int i = 0; i < 8; ++i)
            {
                m_SlotEnabled[i].CheckedChanged += slotEnabled_CheckedChanged;
                m_SlotX[i].ValueChanged += slotXY_ValueChanged;
                m_SlotY[i].ValueChanged += slotXY_ValueChanged;
                m_SlotBank[i].ValueChanged += slotBank_ValueChanged;
                m_SlotCustomColor[i].SelectedIndexChanged += slotCustomColor_SelectedIndexChanged;
            }
        }



        private void DetachOverlayFieldHandlers()
        {
            editOverlayName.TextChanged -= editOverlayName_TextChanged;
            editFrameDelay.ValueChanged -= editFrameDelay_ValueChanged;
            checkLoop.CheckedChanged -= checkLoop_CheckedChanged;
            checkStartRandomFrame.CheckedChanged -= checkStartRandomFrame_CheckedChanged;
            editAnimationID.ValueChanged -= editAnimationID_ValueChanged;
            for (int i = 0; i < 8; ++i)
            {
                m_SlotEnabled[i].CheckedChanged -= slotEnabled_CheckedChanged;
                m_SlotX[i].ValueChanged -= slotXY_ValueChanged;
                m_SlotY[i].ValueChanged -= slotXY_ValueChanged;
                m_SlotBank[i].ValueChanged -= slotBank_ValueChanged;
                m_SlotCustomColor[i].SelectedIndexChanged -= slotCustomColor_SelectedIndexChanged;
            }
        }



        private void btnAddOverlay_Click(DecentForms.ControlBase Sender)
        {
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            var ov = new Formats.SpriteProject.Overlay();
            ov.Name = "Overlay " + (m_SpriteProject.Overlays.Count + 1);
            // Default: one frame so slot bank-index edits have somewhere to land.
            ov.Frames.Add(new Formats.SpriteProject.OverlayFrame());
            m_SpriteProject.Overlays.Add(ov);
            Modified = true;

            RefreshOverlaysList();
            listOverlays.SelectedIndex = m_SpriteProject.Overlays.Count - 1;
        }



        private void btnRemoveOverlay_Click(DecentForms.ControlBase Sender)
        {
            int idx = listOverlays.SelectedIndex;
            if (idx < 0 || idx >= m_SpriteProject.Overlays.Count) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            m_SpriteProject.Overlays.RemoveAt(idx);
            Modified = true;
            RefreshOverlaysList();
        }



        private void btnCloneOverlay_Click(DecentForms.ControlBase Sender)
        {
            int idx = listOverlays.SelectedIndex;
            if (idx < 0 || idx >= m_SpriteProject.Overlays.Count) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            var clone = m_SpriteProject.Overlays[idx].Clone();
            clone.Name = UniqueOverlayName(m_SpriteProject.Overlays[idx].Name + " copy");
            // Insert right after the source so the copy appears next to it.
            m_SpriteProject.Overlays.Insert(idx + 1, clone);
            Modified = true;

            RefreshOverlaysList();
            listOverlays.SelectedIndex = idx + 1;
        }



        /// <summary>
        /// Returns Candidate, or Candidate with a " 2"/" 3"/... suffix, whichever
        /// is the first not already used by an existing overlay - so a clone never
        /// shows an identical name in the list.
        /// </summary>
        private string UniqueOverlayName(string Candidate)
        {
            string name = Candidate;
            int suffix = 2;
            bool clash = true;
            while (clash)
            {
                clash = false;
                foreach (var o in m_SpriteProject.Overlays)
                {
                    if (o.Name == name) { clash = true; break; }
                }
                if (clash) { name = Candidate + " " + suffix; ++suffix; }
            }
            return name;
        }



        private void editOverlayName_TextChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            m_CurrentOverlay.Name = editOverlayName.Text;
            Modified = true;
            int idx = listOverlays.SelectedIndex;
            if (idx >= 0 && idx < listOverlays.Items.Count)
            {
                // Detach the listbox handler — rewriting Items[idx] fires
                // SelectedIndexChanged and would re-populate the field pane,
                // resetting the textbox cursor to position 0 mid-typing.
                listOverlays.SelectedIndexChanged -= listOverlays_SelectedIndexChanged;
                try
                {
                    listOverlays.Items[idx] = string.IsNullOrEmpty(m_CurrentOverlay.Name) ? "(unnamed)" : m_CurrentOverlay.Name;
                }
                finally
                {
                    listOverlays.SelectedIndexChanged += listOverlays_SelectedIndexChanged;
                }
            }
        }



        private void slotEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            var ctrl = (System.Windows.Forms.CheckBox)sender;
            int slotIdx = (int)ctrl.Tag;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            m_CurrentOverlay.Slots[slotIdx].Enabled = ctrl.Checked;
            Modified = true;
            RebuildOverlayPreview();
        }



        private void slotXY_ValueChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            var ctrl = (System.Windows.Forms.NumericUpDown)sender;
            int slotIdx = (int)ctrl.Tag;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            var slot = m_CurrentOverlay.Slots[slotIdx];
            slot.X = (int)m_SlotX[slotIdx].Value;
            slot.Y = (int)m_SlotY[slotIdx].Value;
            Modified = true;
            RebuildOverlayPreview();
        }



        private void slotBank_ValueChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            var ctrl = (System.Windows.Forms.NumericUpDown)sender;
            int slotIdx = (int)ctrl.Tag;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            // Phase 2 view: single-frame model — the bank-index edit goes into
            // frame 0. Phase 4's animation tab manages the full timeline.
            if (m_CurrentOverlay.Frames.Count == 0)
            {
                m_CurrentOverlay.Frames.Add(new Formats.SpriteProject.OverlayFrame());
            }
            m_CurrentOverlay.Frames[0].BankIndex[slotIdx] = (int)ctrl.Value;
            Modified = true;
            RebuildOverlayPreview();
        }



        private void slotCustomColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            var ctrl = (System.Windows.Forms.ComboBox)sender;
            int slotIdx = (int)ctrl.Tag;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            // Combo index 0 = "None" (-1), 1..16 = colour override 0..15.
            m_CurrentOverlay.Slots[slotIdx].CustomColor = ctrl.SelectedIndex - 1;
            Modified = true;
            // The override recolours every view of this animation.
            RebuildOverlayPreview();
            RebuildAnimPreview();
            RebuildSpriteTest();
        }



        /// <summary>
        /// Render the current overlay into picOverlayPreview by walking slots
        /// 0→7 (slot 0 at the bottom). Each enabled slot is drawn at its
        /// (X, Y) using the bank sprite named by frame[0]'s BankIndex[slot]
        /// in the slot's own CustomColor (with project-wide BG/MC1/MC2).
        /// Reuses the existing DrawSpriteImage helper.
        /// </summary>
        public void RebuildOverlayPreview()
        {
            if (picOverlayPreview == null) return;

            EnsurePreviewPageSized(picOverlayPreview, m_OverlayPreviewZoom);
            var page = picOverlayPreview.DisplayPage;

            // Background fill: project palette BG color.
            uint bgRGB = m_SpriteProject.Colors.Palette.ColorValues[m_SpriteProject.Colors.BackgroundColor];
            page.Box(0, 0, page.Width, page.Height, bgRGB);

            if (m_CurrentOverlay != null)
            {
                var frame = m_CurrentOverlay.Frames.Count > 0 ? m_CurrentOverlay.Frames[0] : null;
                for (int s = 0; s < 8; ++s)
                {
                    var slot = m_CurrentOverlay.Slots[s];
                    if (!slot.Enabled) continue;
                    int bankIdx = (frame != null) ? frame.BankIndex[s] : 0;
                    if (bankIdx < 0 || bankIdx >= m_SpriteProject.Sprites.Count) continue;

                    var bs = m_SpriteProject.Sprites[bankIdx];
                    DrawSpriteImage(page,
                                     slot.X,
                                     slot.Y,
                                     bs.Tile.Data,
                                     bs.Tile.Colors.Palette,
                                     bs.Tile.Width, bs.Tile.Height,
                                     // Slot colour override; "None" (-1) falls
                                     // back to the bank sprite's own colour.
                                     (slot.CustomColor >= 0) ? slot.CustomColor : bs.Tile.CustomColor,
                                     bs.Mode,
                                     m_SpriteProject.Colors.BackgroundColor,
                                     m_SpriteProject.Colors.MultiColor1,
                                     m_SpriteProject.Colors.MultiColor2,
                                     slot.ExpandX, slot.ExpandY,
                                     true,
                                     bs.Tile.Colors.PaletteOffset);
                }
            }

            picOverlayPreview.Invalidate();
        }



        // -------- Animation tab (Phase 4) --------

        /// <summary>
        /// Build the 8 fixed frame-slot NumericUpDown rows inside
        /// panelFrameSlots at construction time. Same pattern as
        /// BuildOverlaySlotRows: identical repetitive rows authored in code
        /// inside a Designer-authored container.
        /// </summary>
        private void BuildFrameSlotControls()
        {
            const int rowHeight = 22;
            const int rowY0 = 4;

            panelFrameSlots.Controls.Clear();

            for (int i = 0; i < 8; ++i)
            {
                int y = rowY0 + i * rowHeight;

                var lbl = new System.Windows.Forms.Label();
                lbl.AutoSize = true;
                lbl.Location = new System.Drawing.Point(0, y + 4);
                lbl.Text = "Slot " + i + " bank:";
                panelFrameSlots.Controls.Add(lbl);

                var nud = new System.Windows.Forms.NumericUpDown();
                nud.Location = new System.Drawing.Point(90, y + 1);
                nud.Size = new System.Drawing.Size(60, 20);
                nud.Minimum = 0;
                nud.Maximum = 255;
                nud.Tag = i;
                nud.ValueChanged += frameSlotBank_ValueChanged;
                panelFrameSlots.Controls.Add(nud);
                m_FrameSlotBank[i] = nud;
            }
        }



        /// <summary>
        /// Repopulate listAnimFrames from the current overlay's frames.
        /// Detaches SelectedIndexChanged during the bulk update so we don't
        /// trigger stale-index callbacks mid-rebuild.
        /// </summary>
        public void RefreshFramesList()
        {
            if (listAnimFrames == null) return;

            int prev = listAnimFrames.SelectedIndex;
            listAnimFrames.SelectedIndexChanged -= listAnimFrames_SelectedIndexChanged;
            try
            {
                listAnimFrames.BeginUpdate();
                listAnimFrames.Items.Clear();
                if (m_CurrentOverlay != null)
                {
                    for (int i = 0; i < m_CurrentOverlay.Frames.Count; ++i)
                    {
                        listAnimFrames.Items.Add("Frame " + i);
                    }
                }
                listAnimFrames.EndUpdate();

                if (prev >= 0 && prev < listAnimFrames.Items.Count)
                    listAnimFrames.SelectedIndex = prev;
                else if (listAnimFrames.Items.Count > 0)
                    listAnimFrames.SelectedIndex = 0;
            }
            finally
            {
                listAnimFrames.SelectedIndexChanged += listAnimFrames_SelectedIndexChanged;
            }
            PopulateFrameFieldsFromSelection();
            UpdateFrameButtonStates();
        }



        private void UpdateFrameButtonStates()
        {
            bool hasOverlay = (m_CurrentOverlay != null);
            bool hasFrame = hasOverlay && (listAnimFrames.SelectedIndex >= 0);
            btnAddFrame.Enabled = hasOverlay;
            btnRemoveFrame.Enabled = hasFrame;
            btnDuplicateFrame.Enabled = hasFrame;
            btnPlayAnim.Enabled = hasOverlay && (m_CurrentOverlay.Frames.Count > 0) && !m_OverlayAnimTimer.Enabled;
            btnPauseAnim.Enabled = m_OverlayAnimTimer.Enabled;
        }



        private void listAnimFrames_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateFrameFieldsFromSelection();
            UpdateFrameButtonStates();
        }



        /// <summary>
        /// Mirror the selected frame into the per-slot bank NUDs + delay box.
        /// Detach/attach handlers around the writes so they don't dirty the
        /// model.
        /// </summary>
        private void PopulateFrameFieldsFromSelection()
        {
            int idx = (listAnimFrames != null) ? listAnimFrames.SelectedIndex : -1;
            m_CurrentFrame = (m_CurrentOverlay != null && idx >= 0 && idx < m_CurrentOverlay.Frames.Count)
                            ? m_CurrentOverlay.Frames[idx]
                            : null;

            DetachFrameFieldHandlers();
            try
            {
                if (m_CurrentFrame == null)
                {
                    for (int i = 0; i < 8; ++i) m_FrameSlotBank[i].Value = 0;
                }
                else
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        int v = m_CurrentFrame.BankIndex[i];
                        m_FrameSlotBank[i].Value = ClampNudInt(v, 0, 255);
                    }
                }
            }
            finally
            {
                AttachFrameFieldHandlers();
            }
            RebuildAnimPreview();
        }



        private void AttachFrameFieldHandlers()
        {
            for (int i = 0; i < 8; ++i)
            {
                m_FrameSlotBank[i].ValueChanged += frameSlotBank_ValueChanged;
            }
        }



        private void DetachFrameFieldHandlers()
        {
            for (int i = 0; i < 8; ++i)
            {
                m_FrameSlotBank[i].ValueChanged -= frameSlotBank_ValueChanged;
            }
        }



        private void btnAddFrame_Click(DecentForms.ControlBase Sender)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            // Seed the new frame with the slot bank indices of the currently
            // selected frame (if any) so adding a new frame to a busy overlay
            // doesn't blank the slots — common ask is "duplicate as starting
            // point". If no frame selected, start at all-zero.
            var newFrame = new Formats.SpriteProject.OverlayFrame();
            if (m_CurrentFrame != null)
            {
                for (int i = 0; i < 8; ++i) newFrame.BankIndex[i] = m_CurrentFrame.BankIndex[i];
            }
            m_CurrentOverlay.Frames.Add(newFrame);
            Modified = true;
            RefreshFramesList();
            listAnimFrames.SelectedIndex = m_CurrentOverlay.Frames.Count - 1;
        }



        private void btnRemoveFrame_Click(DecentForms.ControlBase Sender)
        {
            if (m_CurrentOverlay == null) return;
            int idx = listAnimFrames.SelectedIndex;
            if (idx < 0 || idx >= m_CurrentOverlay.Frames.Count) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            m_CurrentOverlay.Frames.RemoveAt(idx);
            Modified = true;
            RefreshFramesList();
        }



        private void btnDuplicateFrame_Click(DecentForms.ControlBase Sender)
        {
            if (m_CurrentOverlay == null) return;
            int idx = listAnimFrames.SelectedIndex;
            if (idx < 0 || idx >= m_CurrentOverlay.Frames.Count) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            var src = m_CurrentOverlay.Frames[idx];
            var dup = new Formats.SpriteProject.OverlayFrame();
            for (int i = 0; i < 8; ++i) dup.BankIndex[i] = src.BankIndex[i];
            m_CurrentOverlay.Frames.Insert(idx + 1, dup);
            Modified = true;
            RefreshFramesList();
            listAnimFrames.SelectedIndex = idx + 1;
        }



        private void editFrameDelay_ValueChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            // Single per-animation frame delay, in 1/50th-second units.
            m_CurrentOverlay.FrameDelay = (int)editFrameDelay.Value;
            Modified = true;
            // If the preview animation is playing, apply the new timing at once.
            if (m_OverlayAnimTimer.Enabled)
            {
                m_OverlayAnimTimer.Interval = OverlayAnimIntervalMs();
            }
        }



        private void checkLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));
            m_CurrentOverlay.Loop = checkLoop.Checked;
            Modified = true;
        }



        private void checkStartRandomFrame_CheckedChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));
            m_CurrentOverlay.StartAtRandomFrame = checkStartRandomFrame.Checked;
            Modified = true;
        }



        private void editAnimationID_ValueChanged(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));
            m_CurrentOverlay.AnimationID = (int)editAnimationID.Value;
            Modified = true;
        }



        private void frameSlotBank_ValueChanged(object sender, EventArgs e)
        {
            if (m_CurrentFrame == null) return;
            var ctrl = (System.Windows.Forms.NumericUpDown)sender;
            int slotIdx = (int)ctrl.Tag;
            DocumentInfo.UndoManager.AddUndoTask(new Undo.UndoSpritesetOverlaysChange(this, m_SpriteProject));

            m_CurrentFrame.BankIndex[slotIdx] = (int)ctrl.Value;
            Modified = true;
            RebuildAnimPreview();
        }



        private void btnPlayAnim_Click(DecentForms.ControlBase Sender)
        {
            if (m_CurrentOverlay == null) return;
            if (m_CurrentOverlay.Frames.Count == 0) return;
            m_OverlayAnimFramePos = 0;
            m_OverlayAnimTimer.Interval = OverlayAnimIntervalMs();
            RebuildAnimPreviewForFrame(m_CurrentOverlay.Frames[0]);
            m_OverlayAnimTimer.Start();
            UpdateFrameButtonStates();
        }



        private void btnPauseAnim_Click(DecentForms.ControlBase Sender)
        {
            m_OverlayAnimTimer.Stop();
            // After pausing, repaint the preview as the user-selected static
            // frame so they can keep editing.
            RebuildAnimPreview();
            UpdateFrameButtonStates();
        }



        private void overlayAnimTimer_Tick(object sender, EventArgs e)
        {
            if (m_CurrentOverlay == null || m_CurrentOverlay.Frames.Count == 0)
            {
                m_OverlayAnimTimer.Stop();
                UpdateFrameButtonStates();
                return;
            }
            if (m_OverlayAnimFramePos >= m_CurrentOverlay.Frames.Count)
            {
                m_OverlayAnimFramePos = 0;
            }
            // One frame per tick: the timer interval IS the single per-animation
            // frame delay (1/50th-second units). Re-apply it each tick so editing
            // the delay while playing takes effect immediately.
            m_OverlayAnimTimer.Interval = OverlayAnimIntervalMs();
            m_OverlayAnimFramePos = (m_OverlayAnimFramePos + 1) % m_CurrentOverlay.Frames.Count;
            RebuildAnimPreviewForFrame(m_CurrentOverlay.Frames[m_OverlayAnimFramePos]);
        }



        /// <summary>
        /// Preview-timer interval in milliseconds for the current overlay's single
        /// per-animation frame delay (1/50th-second units -> ms).
        /// </summary>
        private int OverlayAnimIntervalMs()
        {
            int jiffies = (m_CurrentOverlay != null) ? m_CurrentOverlay.FrameDelay : 5;
            if (jiffies < 1) jiffies = 1;
            return jiffies * 20;   // one jiffy = 1/50th second = 20 ms
        }



        /// <summary>
        /// Render the current static frame (the one selected in the listbox)
        /// into picAnimPreview. Calls RebuildAnimPreviewForFrame internally.
        /// </summary>
        public void RebuildAnimPreview()
        {
            RebuildAnimPreviewForFrame(m_CurrentFrame);
        }



        private void RebuildAnimPreviewForFrame(Formats.SpriteProject.OverlayFrame Frame)
        {
            if (picAnimPreview == null) return;

            EnsurePreviewPageSized(picAnimPreview, m_AnimPreviewZoom);
            var page = picAnimPreview.DisplayPage;

            uint bgRGB = m_SpriteProject.Colors.Palette.ColorValues[m_SpriteProject.Colors.BackgroundColor];
            page.Box(0, 0, page.Width, page.Height, bgRGB);

            if (m_CurrentOverlay != null && Frame != null)
            {
                for (int s = 0; s < 8; ++s)
                {
                    var slot = m_CurrentOverlay.Slots[s];
                    if (!slot.Enabled) continue;
                    int bankIdx = Frame.BankIndex[s];
                    if (bankIdx < 0 || bankIdx >= m_SpriteProject.Sprites.Count) continue;

                    var bs = m_SpriteProject.Sprites[bankIdx];
                    DrawSpriteImage(page,
                                     slot.X,
                                     slot.Y,
                                     bs.Tile.Data,
                                     bs.Tile.Colors.Palette,
                                     bs.Tile.Width, bs.Tile.Height,
                                     // Slot colour override: "None" (-1) shows each bank
                                     // sprite in ITS OWN foreground colour (the default);
                                     // 0..15 forces that colour onto every frame of the
                                     // animation — matching the test playfield and the
                                     // game-binary export. Drives both static preview and
                                     // playback.
                                     (slot.CustomColor >= 0) ? slot.CustomColor : bs.Tile.CustomColor,
                                     bs.Mode,
                                     m_SpriteProject.Colors.BackgroundColor,
                                     m_SpriteProject.Colors.MultiColor1,
                                     m_SpriteProject.Colors.MultiColor2,
                                     slot.ExpandX, slot.ExpandY,
                                     true,
                                     bs.Tile.Colors.PaletteOffset);
                }
            }

            picAnimPreview.Invalidate();
        }



        // ===========================================================================
        //  Sprite-test panel: click-to-play concurrent overlay animations.
        //  Self-contained — own timer (m_SpriteTestTimer), own instance list
        //  (m_TestInstances) and own picture box (picSpriteTest). It never touches
        //  the picAnimPreview playback, so the two can run at the same time.
        // ===========================================================================

        private void picSpriteTest_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (m_CurrentOverlay == null) return;
            if (m_CurrentOverlay.Frames.Count == 0) return;

            // Store the click in panel/screen pixels; it is mapped through the current
            // zoom at render time (so a zoom change mid-play re-maps cleanly).
            m_TestInstances.Add(new SpriteTestInstance { ScreenX = e.X, ScreenY = e.Y, FramePos = 0, FrameTicks = 0 });
            if (!m_SpriteTestTimer.Enabled)
            {
                m_SpriteTestTimer.Start();
            }
            RebuildSpriteTest();    // immediate paint so the click feels responsive
        }



        private void spriteTestTimer_Tick(object sender, EventArgs e)
        {
            if ((m_CurrentOverlay == null)
            || (m_CurrentOverlay.Frames.Count == 0)
            || (m_TestInstances.Count == 0))
            {
                m_SpriteTestTimer.Stop();
                m_TestInstances.Clear();
                RebuildSpriteTest();
                return;
            }

            int dt = m_SpriteTestTimer.Interval;
            int frameCount = m_CurrentOverlay.Frames.Count;
            bool loop = m_SpriteProject.TestLoop;
            // Single per-animation frame delay (1/50th sec -> ms).
            int frameDelayMs = m_CurrentOverlay.FrameDelay * 20;
            if (frameDelayMs < 20) frameDelayMs = 20;

            // Iterate backwards so finished instances can be removed in place.
            for (int i = m_TestInstances.Count - 1; i >= 0; --i)
            {
                var inst = m_TestInstances[i];
                inst.FrameTicks += dt;

                // Advance while the accumulated time covers the animation's frame
                // delay (read live each tick) — so editing the delay while it plays
                // (including a looping instance) applies immediately; a shortened
                // delay catches up by advancing multiple frames here.
                while ((inst.FramePos < frameCount)
                && (inst.FrameTicks >= frameDelayMs))
                {
                    inst.FrameTicks -= frameDelayMs;
                    inst.FramePos++;
                    if (inst.FramePos >= frameCount)
                    {
                        if (loop)
                        {
                            inst.FramePos = 0;    // wrap and keep playing
                        }
                        else
                        {
                            break;                // finished its single pass
                        }
                    }
                }

                if ((!loop)
                && (inst.FramePos >= frameCount))
                {
                    m_TestInstances.RemoveAt(i);
                }
            }

            RebuildSpriteTest();
            if (m_TestInstances.Count == 0)
            {
                m_SpriteTestTimer.Stop();
            }
        }



        /// <summary>
        /// Paint the test panel: fill the background, then composite every running
        /// instance's current frame. Each sprite uses ITS OWN colour and mode
        /// (multicolour-aware), exactly like RebuildAnimPreviewForFrame — but this
        /// loops over instances and draws into picSpriteTest, never picAnimPreview.
        /// </summary>
        private void RebuildSpriteTest()
        {
            if (picSpriteTest == null) return;

            // Magnification is per-axis and fractional: each C64 pixel becomes a
            // scaleX-by-scaleY rectangle. We render the scene 1:1 into a page sized
            // clientW/scaleX x clientH/scaleY; the FastPictureBox StretchBlt's it up
            // to the client independently per axis, giving exactly that scale. The
            // non-magnified mode is the uniform anim zoom (scaleX == scaleY == zoom).
            double scaleX, scaleY;
            if (m_SpriteProject.TestUseC64Magnification)
            {
                ComputeC64Scale(out scaleX, out scaleY);
            }
            else
            {
                int zoom = (m_AnimPreviewZoom < 1) ? 1 : m_AnimPreviewZoom;
                scaleX = zoom;
                scaleY = zoom;
            }

            int clientW = System.Math.Max(1, picSpriteTest.ClientRectangle.Width);
            int clientH = System.Math.Max(1, picSpriteTest.ClientRectangle.Height);
            // Round (not truncate) so the actual per-axis ratio stays as close as
            // possible to scaleX:scaleY despite the integer page - the aspect ratio
            // is the whole point here.
            int pageW = System.Math.Max(1, (int)(clientW / scaleX + 0.5));
            int pageH = System.Math.Max(1, (int)(clientH / scaleY + 0.5));
            EnsurePreviewPageSized(picSpriteTest, pageW, pageH);
            var page = picSpriteTest.DisplayPage;

            page.Box(0, 0, page.Width, page.Height, ResolveTestBackgroundRGB());

            if (m_CurrentOverlay != null)
            {
                int frameCount = m_CurrentOverlay.Frames.Count;
                var bb = OverlayBoundingBox(m_CurrentOverlay);

                foreach (var inst in m_TestInstances)
                {
                    if (frameCount == 0) break;
                    int fp = inst.FramePos;
                    if ((fp < 0) || (fp >= frameCount)) fp = 0;
                    var frame = m_CurrentOverlay.Frames[fp];

                    // Screen click -> page coords (the page maps to the client
                    // per-axis). Place the overlay's top-left (its visible
                    // bounding-box corner) at the click.
                    int originX = (int)(inst.ScreenX / scaleX) - bb.X;
                    int originY = (int)(inst.ScreenY / scaleY) - bb.Y;

                    for (int s = 0; s < 8; ++s)
                    {
                        var slot = m_CurrentOverlay.Slots[s];
                        if (!slot.Enabled) continue;
                        int bankIdx = frame.BankIndex[s];
                        if ((bankIdx < 0) || (bankIdx >= m_SpriteProject.Sprites.Count)) continue;

                        var bs = m_SpriteProject.Sprites[bankIdx];
                        // Test expand checkboxes OR with each slot's own expand flags.
                        bool expandX = slot.ExpandX || m_SpriteProject.TestExpandX;
                        bool expandY = slot.ExpandY || m_SpriteProject.TestExpandY;

                        DrawSpriteImage(page,
                                         originX + slot.X,
                                         originY + slot.Y,
                                         bs.Tile.Data,
                                         bs.Tile.Colors.Palette,
                                         bs.Tile.Width, bs.Tile.Height,
                                         // Slot colour override; "None" (-1) =
                                         // each sprite in its own colour.
                                         (slot.CustomColor >= 0) ? slot.CustomColor : bs.Tile.CustomColor,
                                         bs.Mode,
                                         m_SpriteProject.Colors.BackgroundColor,
                                         m_SpriteProject.Colors.MultiColor1,
                                         m_SpriteProject.Colors.MultiColor2,
                                         expandX, expandY,
                                         true,                   // transparent background -> sprites stack
                                         bs.Tile.Colors.PaletteOffset);
                    }
                }
            }

            picSpriteTest.Invalidate();
        }



        /// <summary>
        /// ARGB the test panel is filled with: TestBackgroundColorIndex -1 follows
        /// the project background colour, otherwise it is a palette index.
        /// </summary>
        private uint ResolveTestBackgroundRGB()
        {
            int idx = m_SpriteProject.TestBackgroundColorIndex;
            if (idx < 0)
            {
                idx = m_SpriteProject.Colors.BackgroundColor;
            }
            var pal = m_SpriteProject.Colors.Palette;
            if ((idx < 0) || (idx >= pal.ColorValues.Length))
            {
                idx = 0;
            }
            return pal.ColorValues[idx];
        }



        /// <summary>
        /// Bounding box (page pixels) of the overlay's enabled slots using frame 0's
        /// banks, accounting for expand — used to anchor a spawned animation's
        /// top-left to the clicked point. Rectangle.Empty when nothing is enabled.
        /// </summary>
        private System.Drawing.Rectangle OverlayBoundingBox(Formats.SpriteProject.Overlay Ov)
        {
            if ((Ov == null) || (Ov.Frames.Count == 0)) return System.Drawing.Rectangle.Empty;

            var frame = Ov.Frames[0];
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int s = 0; s < 8; ++s)
            {
                var slot = Ov.Slots[s];
                if (!slot.Enabled) continue;
                int bankIdx = frame.BankIndex[s];
                if ((bankIdx < 0) || (bankIdx >= m_SpriteProject.Sprites.Count)) continue;

                var bs = m_SpriteProject.Sprites[bankIdx];
                int w = bs.Tile.Width * ((slot.ExpandX || m_SpriteProject.TestExpandX) ? 2 : 1);
                int h = bs.Tile.Height * ((slot.ExpandY || m_SpriteProject.TestExpandY) ? 2 : 1);
                if (slot.X < minX) minX = slot.X;
                if (slot.Y < minY) minY = slot.Y;
                if (slot.X + w > maxX) maxX = slot.X + w;
                if (slot.Y + h > maxY) maxY = slot.Y + h;
            }
            if ((maxX <= minX) || (maxY <= minY)) return System.Drawing.Rectangle.Empty;
            return new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);
        }



        private void StopSpriteTest()
        {
            m_SpriteTestTimer.Stop();
            m_TestInstances.Clear();
            RebuildSpriteTest();
        }



        /// <summary>
        /// Fill the test background-colour dropdown: row 0 = "(Project background)",
        /// rows 1..N = the project palette colours (drawn as swatches by DrawItem).
        /// </summary>
        private void PopulateTestBackColorCombo()
        {
            if (comboTestBackColor == null) return;

            bool saved = DoNotUpdateFromControls;
            DoNotUpdateFromControls = true;
            comboTestBackColor.Items.Clear();
            comboTestBackColor.Items.Add("(Project background)");
            int count = m_SpriteProject.Colors.Palette.ColorValues.Length;
            for (int i = 0; i < count; ++i)
            {
                comboTestBackColor.Items.Add("Colour " + i);
            }
            int sel = m_SpriteProject.TestBackgroundColorIndex + 1;
            if (sel < 0) sel = 0;
            if (sel >= comboTestBackColor.Items.Count) sel = 0;
            comboTestBackColor.SelectedIndex = sel;
            DoNotUpdateFromControls = saved;
        }



        private void comboTestBackColor_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0)
            {
                e.DrawFocusRectangle();
                return;
            }

            var g = e.Graphics;
            int textLeft = e.Bounds.Left + 2;
            string label;
            if (e.Index == 0)
            {
                label = "(Project background)";
            }
            else
            {
                int palIdx = e.Index - 1;
                var pal = m_SpriteProject.Colors.Palette;
                if ((palIdx >= 0) && (palIdx < pal.ColorValues.Length))
                {
                    using (var br = new System.Drawing.SolidBrush(GR.Color.Helper.FromARGB(pal.ColorValues[palIdx] | 0xff000000u)))
                    {
                        g.FillRectangle(br, e.Bounds.Left + 2, e.Bounds.Top + 2, 18, e.Bounds.Height - 4);
                    }
                    g.DrawRectangle(System.Drawing.Pens.Gray, e.Bounds.Left + 2, e.Bounds.Top + 2, 18, e.Bounds.Height - 4);
                }
                textLeft = e.Bounds.Left + 24;
                label = "Colour " + palIdx;
            }

            using (var tb = new System.Drawing.SolidBrush(e.ForeColor))
            using (var sf = new System.Drawing.StringFormat() { LineAlignment = System.Drawing.StringAlignment.Center, FormatFlags = System.Drawing.StringFormatFlags.NoWrap })
            {
                g.DrawString(label, e.Font, tb, new System.Drawing.Rectangle(textLeft, e.Bounds.Top, e.Bounds.Right - textLeft - 2, e.Bounds.Height), sf);
            }
            e.DrawFocusRectangle();
        }



        private void comboTestBackColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            m_SpriteProject.TestBackgroundColorIndex = comboTestBackColor.SelectedIndex - 1;
            SetModified();
            RebuildSpriteTest();
        }



        private void checkTestExpandX_CheckedChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            m_SpriteProject.TestExpandX = checkTestExpandX.Checked;
            SetModified();
            RebuildSpriteTest();
        }



        private void checkTestExpandY_CheckedChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            m_SpriteProject.TestExpandY = checkTestExpandY.Checked;
            SetModified();
            RebuildSpriteTest();
        }



        private void checkTestLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            m_SpriteProject.TestLoop = checkTestLoop.Checked;
            SetModified();
        }



        private void checkTestC64Mag_CheckedChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            m_SpriteProject.TestUseC64Magnification = checkTestC64Mag.Checked;
            UpdateMagResultLabel();
            RebuildSpriteTest();
            SetModified();
        }



        private void editTestTargetW_TextChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            int v;
            m_SpriteProject.TestTargetWidth = (int.TryParse(editTestTargetW.Text, out v) && (v > 0)) ? v : 0;
            UpdateMagResultLabel();
            if (m_SpriteProject.TestUseC64Magnification) RebuildSpriteTest();
            SetModified();
        }



        private void editTestTargetH_TextChanged(object sender, EventArgs e)
        {
            if (DoNotUpdateFromControls) return;
            int v;
            m_SpriteProject.TestTargetHeight = (int.TryParse(editTestTargetH.Text, out v) && (v > 0)) ? v : 0;
            UpdateMagResultLabel();
            if (m_SpriteProject.TestUseC64Magnification) RebuildSpriteTest();
            SetModified();
        }



        /// <summary>
        /// Per-axis playfield magnification for "Use C64 magnification": maps the
        /// auto-detected monitor resolution onto the C64 SCREEN size the user is
        /// targeting (editTestTargetW/H, default 320x200). Each C64 pixel becomes a
        /// ScaleX-by-ScaleY rectangle on screen, where ScaleX = monitorW / c64W and
        /// ScaleY = monitorH / c64H. The two factors differ whenever the C64 screen
        /// and the monitor have different aspect ratios (e.g. 320:200 vs 16:9), which
        /// is exactly the aspect-ratio difference being reproduced. Fractional and
        /// independent per axis on purpose — NOT a single rounded zoom. The C64
        /// screen is configurable so PAL/NTSC and border sizes can be accounted for.
        /// </summary>
        private void ComputeC64Scale(out double ScaleX, out double ScaleY)
        {
            int c64W = m_SpriteProject.TestTargetWidth;
            int c64H = m_SpriteProject.TestTargetHeight;
            if (c64W <= 0) c64W = 320;
            if (c64H <= 0) c64H = 200;
            var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            ScaleX = screen.Width  / (double)c64W;
            ScaleY = screen.Height / (double)c64H;
            if (ScaleX < 0.01) ScaleX = 0.01;
            if (ScaleY < 0.01) ScaleY = 0.01;
        }



        private void UpdateMagResultLabel()
        {
            if (labelTestMagResult == null) return;
            double sx, sy;
            ComputeC64Scale(out sx, out sy);
            labelTestMagResult.Text = "= " + sx.ToString("0.0") + "x wide, " + sy.ToString("0.0") + "x tall";

            int c64W = m_SpriteProject.TestTargetWidth;
            int c64H = m_SpriteProject.TestTargetHeight;
            if (c64W <= 0) c64W = 320;
            if (c64H <= 0) c64H = 200;
            var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            if (toolTip1 != null)
            {
                toolTip1.SetToolTip(labelTestMagResult,
                    "Your screen " + screen.Width + "x" + screen.Height
                    + " mapped onto a C64 screen of " + c64W + "x" + c64H
                    + " -> each C64 pixel is " + sx.ToString("0.00") + " wide x "
                    + sy.ToString("0.00") + " tall monitor pixels.");
            }
        }



        /// <summary>
        /// Reflect the project's C64-magnification settings in the test controls.
        /// The target W/H are the C64 SCREEN size (0/unset falls back to 320x200);
        /// the monitor side is detected when the factor is computed. Guarded so
        /// populating doesn't dirty the document.
        /// </summary>
        private void PopulateTestMagnificationFromProject()
        {
            bool prev = DoNotUpdateFromControls;
            DoNotUpdateFromControls = true;
            try
            {
                checkTestC64Mag.Checked = m_SpriteProject.TestUseC64Magnification;
                int w = m_SpriteProject.TestTargetWidth;
                int h = m_SpriteProject.TestTargetHeight;
                if (w <= 0) w = 320;
                if (h <= 0) h = 200;
                editTestTargetW.Text = w.ToString();
                editTestTargetH.Text = h.ToString();
            }
            finally
            {
                DoNotUpdateFromControls = prev;
            }
            UpdateMagResultLabel();
        }



        private void btnTestStop_Click(DecentForms.ControlBase Sender)
        {
            StopSpriteTest();
        }



        /// <summary>
        /// Ensure the preview's DisplayPage is sized to (clientW/zoom,
        /// clientH/zoom). Smaller page → FastPictureBox stretches it up →
        /// sprites in the page appear larger on screen. Recreates the page
        /// only when its current size doesn't match the target so we don't
        /// thrash on every render call.
        /// </summary>
        private static void EnsurePreviewPageSized(GR.Forms.FastPictureBox Box, int Zoom)
        {
            if (Zoom < 1) Zoom = 1;
            EnsurePreviewPageSized(Box, Box.ClientRectangle.Width / Zoom, Box.ClientRectangle.Height / Zoom);
        }



        /// <summary>
        /// Size the preview's DisplayPage to an explicit (PageW, PageH). The page is
        /// rendered 1:1 and the FastPictureBox StretchBlt's it up to the client - so a
        /// non-square page yields a non-square (per-axis) magnification on screen.
        /// Recreated only when the size actually changes, to avoid render-time thrash.
        /// </summary>
        private static void EnsurePreviewPageSized(GR.Forms.FastPictureBox Box, int PageW, int PageH)
        {
            PageW = System.Math.Max(1, PageW);
            PageH = System.Math.Max(1, PageH);

            var page = Box.DisplayPage;
            if ((page == null) || (page.Width != PageW) || (page.Height != PageH))
            {
                Box.DisplayPage.Create(PageW, PageH, GR.Drawing.PixelFormat.Format32bppRgb);
            }
        }



        private void btnOverlayZoomIn_Click(DecentForms.ControlBase Sender)
        {
            if (m_OverlayPreviewZoom < 16) m_OverlayPreviewZoom *= 2;
            labelOverlayZoom.Text = "Zoom: " + m_OverlayPreviewZoom + "x";
            RebuildOverlayPreview();
        }



        private void btnOverlayZoomOut_Click(DecentForms.ControlBase Sender)
        {
            if (m_OverlayPreviewZoom > 1) m_OverlayPreviewZoom /= 2;
            labelOverlayZoom.Text = "Zoom: " + m_OverlayPreviewZoom + "x";
            RebuildOverlayPreview();
        }



        private void btnAnimZoomIn_Click(DecentForms.ControlBase Sender)
        {
            if (m_AnimPreviewZoom < 16) m_AnimPreviewZoom *= 2;
            labelAnimZoom.Text = "Zoom: " + m_AnimPreviewZoom + "x";
            RebuildAnimPreview();
        }



        private void btnAnimZoomOut_Click(DecentForms.ControlBase Sender)
        {
            if (m_AnimPreviewZoom > 1) m_AnimPreviewZoom /= 2;
            labelAnimZoom.Text = "Zoom: " + m_AnimPreviewZoom + "x";
            RebuildAnimPreview();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
