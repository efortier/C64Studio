# Display Filters

Post-processing passes that run on the scaled, composited map image before it's
blit to the screen. Used to approximate how C64 graphics render on real CRTs so
the user can judge their work against the intended display.

## Filter roster

Signal-path filters (operate like the video signal did):

- **PAL Composite** — chroma bandwidth limit + PAL delay-line vertical chroma
  averaging. Makes alternating-line dithers blend into solid colors, exactly
  like the TVs C64 art was designed for. The most C64-authentic filter here.
- **Horizontal Blur** — beam spot / composite luma softness. σ is in SOURCE
  pixels and scales with zoom.
- **Convergence Error** — sub-pixel R/B beam misalignment, fringes on edges.
- **Gamma / Brightness / Contrast**, **Color Temperature** — display response
  and whitepoint.

Tube-face filters:

- **Phosphor Persistence** — temporal afterglow; moving sprites drag decaying
  trails. The one TEMPORAL filter: it sets `ctx.NeedsContinuousRepaint`
  until the trail has fully faded and the MapEditor keeps invalidating
  meanwhile.
- **Scanlines** — one scanline per SOURCE row, Gaussian beam profile whose
  width grows with pixel luminance (bright areas bloom, gaps close), with
  brightness compensation and automatic fade-out below 2× zoom.
- **Bloom / Halation** — tight glow + wide faint glow around bright areas.
- **Phosphor Mask** — aperture grille / slot mask / dot triad geometries,
  mask scale in target pixels (the mask belongs to the tube, not the signal).
- **Barrel Distortion** — glass curvature, vignette, rounded faceplate
  corners. Typically LAST so everything else warps with the glass.

A physically-motivated chain order: color → persistence → blur → convergence →
scanlines → bloom → mask → barrel. The "CRT Rich" and "PAL TV" presets follow
it.

## How a pass runs

The MapEditor calls `FilterPipeline.Apply( TargetBuffer, ctx )` at the end of
`PictureEditor_PostPaint`. The pipeline walks its enabled filters in order,
ping-ponging between `TargetBuffer` and an internal scratch buffer so each
filter is exactly one full raster (no intermediate copies, no per-filter
allocation). When no filter is enabled the pipeline returns immediately and
touches nothing.

**Ping-pong contract:** the pipeline swaps buffers after EVERY enabled filter.
A filter that decides it has nothing to do must still copy source → target
(`DisplayFilterBase.CopyThrough`); returning without writing hands stale
scratch data downstream.

**Chrome clipping:** filters must confine their effect to the map rect on BOTH
axes — the map can have editor chrome on all four sides. The standard shape is
a full-row `MemoryCopy` first, then overwriting the map span from the source.

## Coordinate system

The `FilterContext` a filter receives describes the **scaled map region inside
the target buffer**, not the whole target and not the unscaled source:

- `TargetBuffer` / `SourceBuffer` — the two ping-pong images, same size and
  pixel format. Read from source, write to target; never write in place.
- `RenderOffsetX`, `RenderOffsetY` — target-pixel coordinate of the map's
  top-left.
- `MapPixelWidth`, `MapPixelHeight` — size of the map region in target pixels.
- `SourceWidth`, `SourceHeight` — size of the VISIBLE map slice in unscaled
  source pixels. Clamped to the same region the target rect describes, so
  `SourceHeight / MapPixelHeight` is exactly the source-rows-per-target-row
  ratio (and `MapPixelWidth / SourceWidth` is the zoom factor). Scanlines,
  blur radii and shift amounts specified "in source pixels" all derive from
  these.

## Temporal filters

The pipeline instance (and every filter on it) is GLOBAL — shared by all open
map editors. Two consequences:

- **Per-view state must be keyed by `FilterContext.StateKey`** (the host
  passes its document identity; PhosphorPersistenceFilter keeps one history
  per key in a `ConditionalWeakTable`, so closed editors get collected
  automatically). Never store per-view temporal state in a plain field.
- **Trail liveness is reported through `ctx.NeedsContinuousRepaint`** (an
  output flag the pipeline clears at the start of each run and temporal
  filters set during Apply). The MapEditor reads it after `Apply` and keeps a
  30ms invalidate timer running until it drops back to false — per view, not
  per filter instance.

Size-keyed scratch caches (PAL/Bloom planes) are capacity-based (grow-only)
so two views of different sizes alternating paints don't reallocate per
frame. Temporal state is NOT serialized, NOT cloned, and resets itself when
the map rect size changes or after a long gap without paints.

## Adding a new filter — 2 steps

1. **Create the class**. Inherit `DisplayFilterBase`. Implement `Name`,
   `Apply( FilterContext )`, and override `SaveParameters` / `LoadParameters`
   / `Clone` if you have tunable state. The csproj globs the folder — no
   project-file edit needed.

   ```csharp
   public class MyFilter : DisplayFilterBase
   {
     static MyFilter()
     {
       DisplayFilterRegistry.Register( typeof( MyFilter ) );
     }

     public int SomeAmount { get; set; } = 50;

     public override string Name { get { return "My Filter"; } }

     public override void Apply( FilterContext ctx ) { /* ... */ }

     public override ByteBuffer SaveParameters()
     {
       var b = new ByteBuffer();
       b.AppendI32( SomeAmount );
       return b;
     }

     public override void LoadParameters( ByteBuffer buf )
     {
       if ( ( buf == null ) || ( buf.Length == 0 ) ) return;
       var r = buf.MemoryReader();
       SomeAmount = r.ReadInt32();
     }

     public override IDisplayFilter Clone()
     {
       return new MyFilter { Enabled = this.Enabled, SomeAmount = this.SomeAmount };
     }
   }
   ```

2. **Register**. The static constructor in step 1 already does this, but the
   registry needs to know the type exists at startup — add one line in
   `DisplayFilterRegistry.EnsureBuiltInsLoaded()`:

   ```csharp
   TouchType( typeof( MyFilter ) );
   ```

   Optional: add a parameter panel in `DlgDisplayFilters.RefreshParamPanel`
   by adding a `case MyFilter mf:` branch (sliders via `AddSlider`, enum-like
   dropdowns via `AddCombo`). Without this, your filter shows up in the UI
   but has no controls to tune it.

## Performance rules

These are not polite suggestions — violating them makes the editor sluggish
at high zoom / large maps, and the pipeline now runs at animation rate (30ms)
whenever map sprites are animating or a phosphor trail is decaying.

- Use **unsafe byte\*** from `FastImage.PinData()`. Never `GetPixel`/`SetPixel`
  in the raster loop.
- Remember to `UnpinData()` on both source and target when you're done.
- 32bpp pixel layout in memory is `[B][G][R][A]` (standard GDI DIB). Byte 0 is
  blue, byte 2 is red. Don't confuse these.
- Precompute any per-filter tables (LUTs, mask patterns, kernels) on parameter
  change, not per frame — cache keyed on the parameter values (see any of the
  existing filters for the pattern).
- Multiply by a 256-scaled integer and shift right by 8 instead of floats
  (`val = (val * factor) >> 8`). Faster and avoids float/int conversions in
  the inner loop.
- Clamp any channel that can exceed 255 after a boost before writing.
- For wide blurs use 2-pass sliding-window box filters (O(1) per pixel at any
  radius) instead of direct convolution — see `PALCompositeFilter` /
  `BloomFilter`.

## Persistence

The pipeline saves via `FilterPipeline.SaveToBuffer()` and loads via
`LoadFromBuffer()`. Settings files from before the filter system existed
have no chunk — the pipeline just stays empty, which means the map renders
exactly as it did pre-filters. Settings files from a newer build might
reference a filter type this build doesn't know about — the loader drops
those silently rather than refusing the whole file.

Per-filter parameter blobs are append-only: new fields go at the END with a
`r.Size - r.Position >= N` guard on load. When a field changes MEANING (see
ScanlineFilter's retired Period/Offset, HorizontalBlur's sigma unit change),
keep writing an in-range value in the legacy slot so older builds reading a
newer settings file stay sane, and append the replacement field after it.

The Map tab's "Filter enabled" master switch persists in
`StudioSettings.MapEditorDisplayFiltersActive` (SETTINGS_MAP_EDITOR chunk) —
it bypasses the pipeline without touching per-filter Enabled flags.
