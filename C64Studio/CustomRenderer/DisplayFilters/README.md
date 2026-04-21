# Display Filters

Post-processing passes that run on the scaled, composited map image before it's
blit to the screen. Used to approximate how C64 graphics render on real CRTs so
the user can judge their work against the intended display.

## How a pass runs

The MapEditor calls `FilterPipeline.Apply( TargetBuffer, ctx )` at the end of
`PictureEditor_PostPaint`. The pipeline walks its enabled filters in order,
ping-ponging between `TargetBuffer` and an internal scratch buffer so each
filter is exactly one full raster (no intermediate copies, no per-filter
allocation). When no filter is enabled the pipeline returns immediately and
touches nothing.

## Coordinate system

The `FilterContext` a filter receives describes the **scaled map region inside
the target buffer**, not the whole target and not the unscaled source:

- `TargetBuffer` / `SourceBuffer` — the two ping-pong images, same size and
  pixel format. Read from source, write to target; never write in place.
- `RenderOffsetX`, `RenderOffsetY` — target-pixel coordinate of the map's
  top-left.
- `MapPixelWidth`, `MapPixelHeight` — size of the map region in target pixels.
- `SourceWidth`, `SourceHeight` — size of the map in unscaled source pixels.

The ratio `SourceHeight / MapPixelHeight` is the number of source rows per
target row. Filters that want to stay scale-invariant (e.g. scanlines) use
this ratio to align their pattern to the scaled map rather than to raw
target pixels.

## Adding a new filter — 3 steps

1. **Create the class**. Inherit `DisplayFilterBase`. Implement `Name`,
   `Apply( FilterContext )`, and override `SaveParameters` / `LoadParameters`
   / `Clone` if you have tunable state.

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

3. **Add to the projitems**. Open `C64Models/C64Models.projitems` and add a
   `<Compile Include="..." />` entry for the new file, same pattern as the
   existing DisplayFilters entries.

   Optional: add a parameter panel in `DlgDisplayFilters.RefreshParamPanel`
   by adding a `case MyFilter mf:` branch. Without this, your filter shows
   up in the UI but has no sliders to tune it.

## Performance rules

These are not polite suggestions — violating them makes the editor sluggish
at high zoom / large maps.

- Use **unsafe byte\*** from `FastImage.PinData()`. Never `GetPixel`/`SetPixel`
  in the raster loop.
- Remember to `UnpinData()` on both source and target when you're done.
- 32bpp pixel layout in memory is `[B][G][R][A]` (standard GDI DIB). Byte 0 is
  blue, byte 2 is red. Don't confuse these.
- Precompute any per-filter tables (LUTs, mask patterns) on parameter change,
  not per frame.
- Multiply by a 256-scaled integer and shift right by 8 instead of floats
  (`val = (val * factor) >> 8`). Faster and avoids float/int conversions in
  the inner loop.
- Clamp any channel that can exceed 255 after a boost before writing.

## Persistence

The pipeline saves via `FilterPipeline.SaveToBuffer()` and loads via
`LoadFromBuffer()`. Settings files from before the filter system existed
have no chunk — the pipeline just stays empty, which means the map renders
exactly as it did pre-filters. Settings files from a newer build might
reference a filter type this build doesn't know about — the loader drops
those silently rather than refusing the whole file.
