using System;



namespace RetroDevStudio.Tablet
{
  /// <summary>
  /// A normalized, UI-agnostic snapshot of the pen's current state. Position
  /// is deliberately NOT here — the host takes geometry from the mouse (exact
  /// cursor→image accuracy, automatic mouse/pen coexistence) and only reads
  /// pressure/eraser/proximity/buttons from the pen.
  /// </summary>
  public struct PenSample
  {
    /// <summary>Tip pressure, 0..1 (normalized by the device's max). 0 when unknown.</summary>
    public float Pressure;

    /// <summary>Pen within the tablet's hover range.</summary>
    public bool InProximity;

    /// <summary>The eraser end is presented (pen flipped) — WinTab "inverted" status.</summary>
    public bool IsEraser;

    /// <summary>Raw barrel-button bitmask (bit 0 = tip contact, higher bits = barrel buttons).</summary>
    public int Buttons;

    /// <summary>Tilt, reserved for later use (0 when unavailable).</summary>
    public float TiltX, TiltY;
  }
}
