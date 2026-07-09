using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WintabDN;



namespace RetroDevStudio.Tablet
{
  /// <summary>
  /// Reusable WinTab pen-input provider. Opens a Wacom digitizing context
  /// against a host window's HWND and turns WinTab packets (delivered as
  /// window messages the host forwards from its WndProc) into a normalized
  /// <see cref="PenSample"/>.
  ///
  /// Design notes:
  /// - No dependency on any editor — attach to any control by handle.
  /// - Packets arrive on the UI thread (via the host's forwarded WndProc), so
  ///   <see cref="Current"/> needs no locking and events fire synchronously.
  /// - The whole feature no-ops when the Wacom driver is not installed: the
  ///   constructor probes for Wintab32.dll with LoadLibrary and never touches
  ///   any WinTab P/Invoke until that succeeds (the vendored WintabDN pops a
  ///   MessageBox on a DllNotFound exception, which we must never trigger).
  /// </summary>
  public sealed class WintabPenProvider : IDisposable
  {
    // WinTab message numbers are lcMsgBase-relative. The default context uses
    // WT_DEFBASE, so WT_PACKET = base+0 and WT_PROXIMITY = base+5.
    private const int   WT_DEFBASE        = 0x7FF0;
    private const int   PACKET_OFFSET     = 0;      // WT_PACKET    - WT_DEFBASE
    private const int   PROXIMITY_OFFSET  = 5;      // WT_PROXIMITY - WT_DEFBASE
    private const uint  TPS_PROXIMITY     = 0x0001; // set = cursor OUT of context
    private const uint  TPS_INVERT        = 0x0010; // set = eraser end presented

    [DllImport( "kernel32", CharSet = CharSet.Ansi, SetLastError = true )]
    private static extern IntPtr LoadLibrary( string lpFileName );

    private readonly bool     m_DriverPresent;
    private IntPtr            m_Hwnd = IntPtr.Zero;
    private CWintabContext    m_Context = null;
    private CWintabData       m_Data = null;
    private uint              m_HCtx = 0;
    private uint              m_MsgBase = WT_DEFBASE;
    private int               m_MaxPressure = 1023;
    private bool              m_Enabled = false;
    private PenSample         m_Current;

    /// <summary>Raised on each pen packet (pressure/eraser/buttons update).</summary>
    public event EventHandler<PenSample>  SampleChanged;

    /// <summary>Raised when the pen enters or leaves hover range.</summary>
    public event EventHandler             ProximityChanged;



    public WintabPenProvider()
    {
      // Probe for the Wacom driver WITHOUT calling into any WintabDN P/Invoke
      // (those show a MessageBox on DllNotFound). Absent driver → silent no-op.
      m_DriverPresent = ( LoadLibrary( "Wintab32.dll" ) != IntPtr.Zero );
    }



    /// <summary>True when the Wacom driver is installed and a tablet is attached.</summary>
    public bool IsTabletPresent
    {
      get
      {
        if ( !m_DriverPresent )
        {
          return false;
        }
        try
        {
          return CWintabInfo.GetNumberOfDevices() > 0;
        }
        catch
        {
          return false;
        }
      }
    }



    /// <summary>The latest pen state (a value snapshot; safe to read on the UI thread).</summary>
    public PenSample Current
    {
      get
      {
        return m_Current;
      }
    }



    /// <summary>Device max pressure (raw units); 0..1 normalization uses this.</summary>
    public int MaxPressure
    {
      get
      {
        return m_MaxPressure;
      }
    }



    /// <summary>
    /// Points the WinTab context at a host window. WinForms can recreate a
    /// control's handle, so the host re-attaches on HandleCreated — if a
    /// context is open it is reopened against the new HWND.
    /// </summary>
    public void Attach( IntPtr hwnd )
    {
      if ( m_Hwnd == hwnd )
      {
        return;
      }
      bool wasEnabled = m_Enabled;
      if ( wasEnabled )
      {
        CloseContext();
      }
      m_Hwnd = hwnd;
      if ( wasEnabled )
      {
        OpenContext();
      }
    }



    /// <summary>Opens (true) or closes (false) the WinTab context. Idempotent.</summary>
    public bool Enabled
    {
      get
      {
        return m_Enabled;
      }
      set
      {
        if ( value == m_Enabled )
        {
          return;
        }
        m_Enabled = value;
        if ( value )
        {
          OpenContext();
        }
        else
        {
          CloseContext();
        }
      }
    }



    private void OpenContext()
    {
      if ( ( !m_DriverPresent )
      ||   ( m_Hwnd == IntPtr.Zero )
      ||   ( m_HCtx != 0 ) )
      {
        return;
      }
      try
      {
        // SYSTEM context (CXO_SYSTEM), NOT a digitizing context. This is
        // essential: a digitizing context captures the pen as a pure data
        // stream — it does NOT move the system cursor or generate mouse
        // messages, so a mouse-position-driven drawing pipeline sees nothing
        // from the pen ("no cursor, no drawing"). A system context moves the
        // cursor with the pen AND generates mouse messages (position/contact)
        // while still delivering WT_PACKETs for pressure — so the pen draws
        // via the normal mouse path and we read pressure from the packets.
        var context = CWintabInfo.GetDefaultSystemContext( ECTXOptionValues.CXO_MESSAGES );
        if ( context == null )
        {
          return;
        }
        // Absolute mode for every data item: we read current pressure and the
        // current button bitmask, not relative deltas.
        context.PktMode = 0;

        m_MsgBase = context.MsgBase;
        if ( m_MsgBase == 0 )
        {
          m_MsgBase = WT_DEFBASE;
        }

        int maxP = CWintabInfo.GetMaxPressure();
        if ( maxP > 0 )
        {
          m_MaxPressure = maxP;
        }

        m_HCtx = context.Open( m_Hwnd, true );
        if ( m_HCtx == 0 )
        {
          return;
        }
        m_Context = context;
        m_Data = new CWintabData( m_Context );
        // A generous queue so a busy paint frame never drops the newest sample.
        m_Data.SetPacketQueueSize( 128 );
      }
      catch
      {
        CloseContext();
      }
    }



    private void CloseContext()
    {
      try
      {
        if ( ( m_Context != null )
        &&   ( m_HCtx != 0 ) )
        {
          m_Context.Close();
        }
      }
      catch
      {
      }
      m_Context = null;
      m_Data = null;
      m_HCtx = 0;
      // Reset the sample so a stale InProximity/IsEraser/Pressure from the last
      // hover can't leak into the next enable cycle (which would make a plain
      // mouse stroke be treated as a zero-pressure pen stroke, or erase if the
      // pen was flipped). Direct assignment — no ProximityChanged during teardown.
      m_Current = default( PenSample );
    }



    /// <summary>
    /// The host forwards its WndProc messages here. Returns true when the
    /// message was a WinTab packet/proximity event this provider consumed.
    /// </summary>
    public bool ProcessMessage( ref Message m )
    {
      if ( ( !m_Enabled )
      ||   ( m_HCtx == 0 )
      ||   ( m_Data == null ) )
      {
        return false;
      }

      int msg = m.Msg;

      if ( msg == m_MsgBase + PACKET_OFFSET )
      {
        // wParam = packet serial number, lParam = HCTX.
        uint serial = unchecked( (uint)m.WParam.ToInt64() );
        if ( serial != 0 )
        {
          try
          {
            WintabPacket pkt = m_Data.GetDataPacket( m_HCtx, serial );
            if ( pkt.pkContext != 0 )
            {
              UpdateFromPacket( pkt );
            }
          }
          catch
          {
          }
        }
        return true;
      }

      if ( msg == m_MsgBase + PROXIMITY_OFFSET )
      {
        // lParam low word: non-zero = entering context, zero = leaving.
        bool inProx = ( ( m.LParam.ToInt64() & 0xFFFF ) != 0 );
        SetProximity( inProx );
        return true;
      }

      return false;
    }



    private void UpdateFromPacket( WintabPacket pkt )
    {
      // Auto-calibrate the pressure range: if a packet ever exceeds the queried
      // max (e.g. GetMaxPressure() failed and left the small 1023 fallback in
      // place while an Intuos Pro streams up to 8191/65535), raise the divisor so
      // normalization can't saturate at "always full" for the rest of the session.
      if ( (int)pkt.pkNormalPressure > m_MaxPressure )
      {
        m_MaxPressure = (int)pkt.pkNormalPressure;
      }
      float pressure = ( m_MaxPressure > 0 )
        ? Math.Max( 0f, Math.Min( 1f, (float)pkt.pkNormalPressure / m_MaxPressure ) )
        : 0f;
      m_Current.Pressure = pressure;
      m_Current.IsEraser = ( ( pkt.pkStatus & TPS_INVERT ) != 0 );
      m_Current.Buttons  = (int)pkt.pkButtons;

      // A packet with TPS_PROXIMITY clear means the cursor is IN the context.
      SetProximity( ( pkt.pkStatus & TPS_PROXIMITY ) == 0 );

      SampleChanged?.Invoke( this, m_Current );
    }



    private void SetProximity( bool InProximity )
    {
      if ( InProximity == m_Current.InProximity )
      {
        return;
      }
      m_Current.InProximity = InProximity;
      if ( !InProximity )
      {
        // Leaving hover: pressure is meaningless — zero it so a stale value
        // can't be mistaken for a fresh press.
        m_Current.Pressure = 0f;
      }
      ProximityChanged?.Invoke( this, EventArgs.Empty );
    }



    public void Dispose()
    {
      Enabled = false;
    }
  }
}
