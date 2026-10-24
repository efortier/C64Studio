namespace RetroDevStudio.Formats.TextTemplate
{
  /// <summary>
  /// One compile error of a template: the 1-based line and column of the
  /// offending token (a TAB counts as one column; "never closed" errors
  /// point at the opener) and the message. Rendering never produces these -
  /// everything a template can get wrong is reported by Template.Compile.
  /// </summary>
  public class TemplateError
  {
    public int      Line = 0;
    public int      Column = 0;
    public string   Message = "";



    public TemplateError()
    {
    }



    public TemplateError( int Line, int Column, string Message )
    {
      this.Line    = Line;
      this.Column  = Column;
      this.Message = Message ?? "";
    }



    public override string ToString()
    {
      return "line " + Line + ", col " + Column + ": " + Message;
    }
  }
}
