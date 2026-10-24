namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>Settings of a compiled template.</summary>
  public class TemplateOptions
  {
    /// <summary>
    /// Every line break of the template's text (CRLF, lone CR, lone LF) and
    /// the "\n" escape of a string literal render as this, so the output's
    /// line endings never depend on how the template was typed.
    /// </summary>
    public string   NewLine = "\r\n";
    /// <summary>Deepest allowed nesting of blocks and parentheses.</summary>
    public int      MaxNesting = 64;
  }
}
