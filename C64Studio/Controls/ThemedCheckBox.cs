using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace RetroDevStudio.Controls
{
  // Owner-drawn CheckBox so the text color is honored even when the control is
  // DISABLED. A stock CheckBox ignores ForeColor while disabled and hard-codes
  // SystemColors.GrayText (via ControlPaint.DrawStringDisabled), which reads as
  // black-on-dark on the dark theme. Mirrors the existing ThemedButton: the glyph
  // is still drawn natively (CheckBoxRenderer) so it looks the same as a stock
  // checkbox; only the text is drawn by us, in a themeable color.
  public class ThemedCheckBox : CheckBox
  {
    public Color DisabledTextColor { get; set; }



    public ThemedCheckBox()
    {
      SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true );

      DisabledTextColor = SystemColors.GrayText;
    }



    protected override void OnPaint( PaintEventArgs e )
    {
      var colorText = Enabled ? ForeColor : DisabledTextColor;

      using ( var brushBG = new SolidBrush( BackColor ) )
      {
        e.Graphics.FillRectangle( brushBG, ClientRectangle );
      }

      var glyphSize  = CheckBoxRenderer.GetGlyphSize( e.Graphics, CheckBoxState.UncheckedNormal );
      var glyphPoint = new Point( 0, ( ClientRectangle.Height - glyphSize.Height ) / 2 );

      CheckBoxState state;
      if ( !Enabled )
      {
        state = Checked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled;
      }
      else if ( Checked )
      {
        state = CheckBoxState.CheckedNormal;
      }
      else
      {
        state = CheckBoxState.UncheckedNormal;
      }
      CheckBoxRenderer.DrawCheckBox( e.Graphics, glyphPoint, state );

      int textLeft = glyphSize.Width + 3;
      var textRect  = new Rectangle( textLeft, 0, ClientRectangle.Width - textLeft, ClientRectangle.Height );
      TextRenderer.DrawText( e.Graphics, Text, Font, textRect, colorText, BackColor,
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis );
    }
  }
}
