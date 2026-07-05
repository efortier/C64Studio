using RetroDevStudio.Dialogs;



namespace RetroDevStudio.IdleQueue
{
  public class IdleRequest
  {
    public RequestData          DebugRequest = null;
    public string               OpenLastSolution = null;
    public FormSplashScreen     CloseSplashScreen = null;
    public bool                 AutoSaveSettings = false;
    public bool                 ShowStartPage = false;
    // One-time startup check: if none of our windows ever gained foreground
    // activation (launched while the user worked elsewhere — Windows then
    // never sends WM_ACTIVATEAPP(0)), broadcast APPLICATION_DEACTIVATED so
    // the freshly restored documents pause their animation timers.
    public bool                 ReconcileAppActivation = false;
  }
}
