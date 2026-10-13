# NGX.NET Showcase

A hardware path tracer for NGX.NET using DirectX 12 or Vulkan.
Requires a GPU with DXR 1.1 or Vulkan ray queries, and the .NET 10 SDK.

```shell
dotnet run --project Showcase -c Release
```

Select the graphics API at startup. Hold the right mouse button to look around;
use WASD to move, Q/E vertically, and Shift to move faster.

## Rendering controls

The scene starts paused. The settings panel provides DLSS SR/RR/FG controls,
a daylight timeline and animation pause. Drag the timeline from sunrise through
noon to sunset: the sun follows the YZ semicircle from -Z through +Y to +Z.
The 06:00 / 12:00 / 18:00 labels describe that arc, not a geographic solar model;
the default is noon. Sampling and lighting defaults are
fixed in code: two path samples per pixel per frame, five scattering events,
and top-of-atmosphere solar irradiance 8 in scene-linear RGB. A neutral solar
source passes through the Earth atmosphere: direct sunlight attenuates and
warms near the horizon, while directional sky scattering supplies the background,
indirect illumination and reflections. There is no fixed ambient sky color.
Global display exposure adapts
smoothly to the view, so moving from the sunlit courtyard into a corridor reveals
its indirect illumination. It meters scene-linear HDR after reconstruction and
before tone mapping; film, light intensities and NGX inputs remain unexposed.

The meter averages 64 samples in each of 64 screen cells before taking logarithms,
reducing the influence of individual zero-valued path samples. Sky, surfaces and
reflections all participate in the same rule, without depth classification or
a sky-visibility exposure limit. It trims approximately 10% of the darkest and
brightest cells and averages the remaining log luminance. Small bright openings
therefore influence exposure gradually instead of switching the entire scene
to another target. The meter targets 18% gray at noon with gain limited to -6
through +14 stops. Adaptation uses elapsed time, with a one-
second time constant toward darkness and one third of a second toward brightness.
Only the meter bounds extreme values; the linear image is not clipped. This is
a global viewing adaptation, not calibrated photographic EV100 or local exposure.
To preserve daylight contrast, the meter key follows the square root of current
outdoor illuminance relative to noon. For each stop of outdoor light lost,
automatic adaptation leaves half a stop visible on the display. Illuminance is
integrated from the atmosphere, including skylight and the finite solar disk;
there is no clock-based tint or brightness curve. This is an explicit display
adaptation policy, separate from the physical radiance and RR inputs.

Display compression uses a middle-gray-anchored Reinhard-style curve applied
to peak RGB. Exposed peaks at or below 0.18 retain `color / (0.82 + max(color))`
exactly. Above that point, a logarithmic shoulder first maps the peak to
`q = 0.18 + log(1 + 4*(peak - 0.18))/4`, then uses `q/(0.82+q)` as its
display peak. This joins with continuous value and slope, lowers bright outdoor
walls and fabric without darkening existing low values, and still approaches
white without a hard cap. Scaling RGB by one factor preserves its channel ratios
rather than forcing bright colors toward white. This is a global display transform;
bright exteriors can still be brighter than shaded interiors, and large changes
in the viewed luminance distribution can still drive gradual exposure adaptation.

Scrubbing the daylight timeline resets the native film while preserving NGX temporal
history, so dragging the timeline does not repeatedly restart reconstruction or
frame generation. NGX history still resets when resources or reconstruction
settings change, and on resume. Light edits do not recreate resources or wait
for GPU idle. Exposure history remains continuous across
camera/lighting changes, resizing and reconstruction switches. Each frame slot
reuses its TLAS until scene transforms change. NGX camera samples and guides share their primary hit and
material; native subpixel samples retain their own intersections.

With SR and RR disabled, the native film progressively averages samples while
the camera and scene remain unchanged. Its sample count is visible in the panel.
Moving the camera, animating the scene, resizing or changing the sun direction
resets the film. Pause animation and hold the camera still to inspect convergence.
No blur, radiance clamp or local exposure is applied to this linear film.

With SR or RR enabled, NGX receives only the current frame's sample average,
with its matching camera jitter, depth and guides. A stationary native film is
not fed into a changing jitter sequence. SR alone is not a path-tracing denoiser;
use native progressive mode when examining noise. Two independent paths share
each NGX primary sample and are averaged before reconstruction. This reduces
single-frame variance compared with one path, at increased path-tracing cost;
native film accumulates two subpixel paths per frame. Actual frame rate and image
quality still depend on the target GPU and reconstruction mode.

## PBRT migration

The old raster-primary renderer has been replaced from camera rays through film.
Graphics pipelines now draw only UI. Scene visibility, primary surfaces, motion,
light sampling and every scattering event all use the same ray-traced geometry.
There is no shadow-map/SSAO fallback, split diffuse/specular path buffer, analytic
ambient term, local exposure field or custom sphere reflection-motion solver.
D3D12/Vulkan retain resource management, BLAS/TLAS, NGX and presentation.

The source baseline is
[pbrt-v4 b4ce9687e6c695f5582997c61b0c66cf064bdb4a](https://github.com/mmp/pbrt-v4/tree/b4ce9687e6c695f5582997c61b0c66cf064bdb4a).
The implementation is a Slang port, with no pbrt C++/CUDA runtime dependency.

| Module | Implementation |
| --- | --- |
| `PbrtSampling.slang` | PBRT PCG32, hashing, permutation and nested Owen-scrambled radical inverses; independent pixel/dimension scrambles and progressive sample indices. |
| `SurfaceInteraction.slang` | Hardware intersection adapter, alpha masks, filtered material textures, geometric/shading frames, error-bounded ray origins and outward floating-point rounding. |
| `TextureFootprint.slang` / `Textures.slang` | Primary-ray UV derivatives, up to four anisotropic trilinear taps, shared material/guide footprints and linear-light texture lookup. |
| `PbrtBsdf.slang` | Diffuse, Dielectric, Conductor and CoatedDiffuse BxDFs; GGX visible-normal sampling, true dielectric/complex Fresnel, coating interface random walks and proportional layered PDFs. |
| `GltfBsdf.slang` | Opaque glTF parameter adapter: diffuse plus dielectric reflection, roughness-aware directional-albedo coupling, exact mixture PDFs and separate discrete mirror events. |
| `EnvironmentLighting.slang` / `SkySampling.slang` | Directional atmosphere importance sampling, attenuated finite solar disk, emissive triangle sampling, GPU light-selection probabilities and matching MIS. |
| `Atmosphere.slang` | Hillaire Earth atmosphere: transmittance, multiple scattering and sky radiance LUTs. |
| `AerialPerspective.slang` | Camera-volume in-scattering and transmittance, composed in scene-linear light before film and reconstruction. |
| `PathTracing.slang` | Camera-path `PathIntegrator::Li` and `SampleLd`: emitted-light MIS, next-event estimation, BSDF continuation, depth cutoff and throughput roulette. |
| `CameraFilm.slang` | Perspective camera, pixel box filter, full-precision progressive RGB film and current-frame NGX guides. |
| `AutoExposure.slang` | GPU display metering and one shared, temporally adapted exposure; ordered storage access on both graphics APIs. |

Metallic selects dielectric or conductor once per interaction, as in PBRT's
`MixMaterial`. The conductor retains PBRT's reflectance-to-complex-IOR mapping.
The nonmetal adapter treats glTF base color as external diffuse reflectance,
combining PBRT diffuse and IOR 1.5 GGX reflection. It does not reinterpret every
stone and fabric as a substrate inside a refractive coating: that mapping greatly
reduced the energy of repeated indoor reflections.

The diffuse term uses the [Kelemen and Szirmay-Kalos coupling](https://doi.org/10.2312/egs.20011003):
`baseColor/pi * (1-E(wo)) * (1-E(wi)) / (1-Eavg)`, where `E` is the
directional albedo of the actual rough GGX dielectric reflection. A generated
32 x 64 table integrates the production specular lobe at IOR 1.5; it resides in
the shared GPU texel buffer on both backends. The smooth limit uses exact Fresnel.
Using macro-normal Fresnel for rough walls and fabric used to remove most of
their diffuse color at grazing angles. The new coupling retains that color
while keeping the fixed-frame BSDF reciprocal and its white-furnace energy near
one within the table's approximation error. This is an explicit glTF parameter
adapter, not the exact glTF reference approximation or PBRT CoatedDiffuse.
Sampling uses the same aggregate BSDF and mixture PDF for either proposal.
Mirror probability stays separate from continuous diffuse density. Roughness
maps to alpha = roughness squared with PBRT remapping disabled. The baked
`GltfDirectionalAlbedo.bin` contains 2,048 row-major float32 values followed by
32 row averages (8,320 bytes, little endian). The shader metadata records its
grid and integration method; changing the specular model or IOR requires
reintegrating this asset.

The earlier CoatedDiffuse implementation remains available in the math module,
but is not applied to the scene's default glTF materials. Its 10-event layer
limit and one-sample f/PDF estimates retain the upstream defaults.

One correctness change from the pinned layered implementation is explicit:
the substrate-sampled connection evaluates its competing virtual-light PDF
in the internal direction (`PDF(wi, -w)`), matching the diffuse sample's
solid-angle measure. Using the forward exit PDF compares different measures
and the two MIS weights do not complement each other. Sampling throughput and
the extended path PDF are unchanged. This applies only to the retained layered
implementation, not the default opaque glTF adapter.

Transport uses RGB reflectance for the existing glTF/NGX pipeline, rather than
PBRT's wavelength-sampled spectra. This is a replacement of the Showcase's
surface renderer, not feature parity with every pbrt-v4 integrator: it does not
provide arbitrary participating media, transmissive glTF materials, spectral dispersion,
depth of field or a `.pbrt` scene parser. Primary surfaces derive UV footprints
from neighboring camera directions on the hit triangle's tangent plane, without
extra scene intersections. Up to four anisotropic trilinear taps filter color,
roughness, emission and normal maps over one output pixel; SR/RR therefore retain
detail recoverable at display resolution. Magnified textures retain level-zero
detail. Mip generation filters the full source extent in linear light with
premultiplied alpha, including odd sizes and one-pixel-wide mip tails. Secondary
transport and alpha-mask visibility still use level-zero texture lookup.
The display uses a global exposure and the gray-anchored tone mapper only
after reconstruction. Linear film remains separate from that display transform.

The nonuniform atmosphere is sampled using a two-dimensional luminance CDF
over equal-solid-angle sky cells, with 1% uniform support for bilinear sky
interpolation. The uniform-sky incomplete-PDF shortcut no longer applies.
Sky, solar disk and area-light powers determine GPU light-selection probabilities.
Sky and sun emission have separate MIS weights, including directions inside
the solar disk; camera and mirror escapes retain both emitters in full. Solar
radiance includes wavelength-dependent atmospheric attenuation and Earth shadow.
The finite disk remains separately sampled, retaining small-emitter efficiency.
Each bounce still uses one direct-light sample. Emissive triangle distributions use
emissive factor, triangle area and sidedness; textured emission is evaluated at
the sampled point. PDF and throughput measures stay distinct for PBRT's layered
BSDF. No hidden path-weight clipping or lighting compensation is added.
The first five scattering events are not terminated by throughput roulette, so
dim indoor paths reach the configured depth budget instead of becoming rare,
bright samples. Longer configured paths still use compensated roulette. The
five-event cutoff remains a finite-depth approximation, and moving low-sample
views can still show noise or reconstruction artifacts.

Ray origins include barycentric interpolation, triangle-extent intersection and
instance-translation error, followed by PBRT outward rounding. The additional
hardware error term follows [NVIDIA's ray-tracing error-bound analysis](https://developer.nvidia.com/blog/solving-self-intersection-artifacts-in-directx-raytracing/).
This is necessary because hardware traversal does not expose PBRT's software
triangle-intersection rejection bound. Behavior on target GPUs still needs
runtime validation.

After normal mapping, a shading normal whose mirror direction lies below the
geometric surface is rotated toward the geometric normal just enough to clear
the horizon. This prevents the shading frame from facing away from a grazing
camera ray; transport and RR guides use the same corrected frame. The correction
is view dependent, so global reciprocity is not claimed for these perturbed
surfaces. It does not change geometric visibility or ray offsets, nor guarantee
that every rough-lobe sample avoids geometric clipping. Primary normal maps use
the same footprint as the other material textures, averaging vectors before
normalization. This is a real-time extension to the pinned PBRT normal-map lookup,
not a microfacet variance model; indirectly viewed normal maps remain level zero.

## NGX contract

Color and film use RGBA32_FLOAT. Depth is an R32_FLOAT UAV containing reverse-Z
hardware depth, rather than a raster depth attachment. Motion is current-to-
previous input-pixel displacement from unjittered matrices. RR receives world
normals with packed roughness, stable material albedo descriptors, and the first
sampled glossy/impulse hit distance in world units; sky uses FP32_MAX. Reflection
motion vectors are not simultaneously provided. The albedo descriptors are NGX
material guides, not substitute BSDFs used by the integrator.

Specular albedo is directional hemispherical reflectance, as required by the
DLSS-RR integration guide, rather than normal-incidence F0. Dielectrics reuse
the integrated GGX table; rough conductors use NVIDIA's EnvBRDFApprox2 guide
fit and smooth conductors use exact Fresnel. These guide values do not alter
path throughput. Both primary sky and reflected sky use FP32_MAX hit distance.

The guide ray uses the same frame jitter as NGX camera samples. Native film uses
independent pixel samples and a separate center guide ray for presentation/FG.
The shared film is ordered by same-state storage barriers on both graphics APIs.

## Sky and atmosphere

The atmosphere is adapted from Sebastien Hillaire's
[A Scalable and Production Ready Sky and Atmosphere Rendering Technique](https://github.com/sebh/UnrealEngineSkyAtmosphere/tree/183ead5bdacc701b3b626347a680a2f3cd3d4fbd)
(EGSR 2020). The source is pinned to `183ead5bdacc701b3b626347a680a2f3cd3d4fbd`.
It uses the reference 6360/6460 km Earth radii, exponential Rayleigh/Mie density,
ozone absorption, Rayleigh/Cornette-Shanks phase functions and the geometric-series
multiple-scattering approximation. The ground is Lambertian with albedo 0.1.

Both APIs use shared RGBA32_FLOAT GPU tables:

| Table | Size | Updated when |
| --- | --- | --- |
| Transmittance | 256 x 64 | Initialization |
| Multiple scattering | 32 x 32 | Initialization |
| Ground skylight / daylight reference | 2 x 1 | Before sky updates |
| Sky radiance | 256 x 128 | Sun direction or observer height changes |
| Sky sampling distribution | 257 x 129 | Sky radiance changes |
| Aerial radiance / transmittance | Two 32 x 512 atlases (16 depth slices) | Sun, observer height, camera view or scene transforms change |

The visible sky uses Hillaire's quadratic elevation mapping on each side of the
true planetary horizon, retaining fine sunset structure and ground separation.
Its azimuth is periodic. The importance distribution separately resamples that
same sky into equal-solid-angle cells; its PDFs and uniform support remain
consistent. Background, direct lighting and mirror rays all evaluate the same
sky radiance. Multiple-scattering coordinates concentrate samples near zero
solar elevation and ground altitude to avoid interpolating across the sharp
sunset transition. The solar disk is excluded from the sky table and sampled
separately, with Earth occlusion evaluated for each solar direction.

The virtual Lambertian ground receives both direct sunlight and integrated
diffuse skylight. The latter is computed before the sky pass without a ground
term, avoiding a feedback loop. This local-ground extension prevents an
unilluminated black lower hemisphere when the sun reaches the horizon. The
same small table stores current and noon horizontal illuminance for exposure;
these are unit-irradiance values, scaled with the atmosphere's solar source.

For the finite Sponza scene, the environment also extends its own horizon haze
through the first twelve degrees below the virtual planet's edge, smoothly
joining a sky-lit fog background across the rest of the lower hemisphere.
The fog color is half the cosine-weighted sky-average radiance, derived from
the integrated skylight rather than a fixed tint or minimum brightness.
A 0.02-degree upper guard makes the two sides continuous. This adaptation fades out between
100 m and 1 km altitude; it does not modify the physical scattering solver or
add fog to local geometry. The shared sky LUT includes this adaptation, so its
background, reflected radiance and lighting distribution remain consistent.
It is an explicit presentation choice in place of distant terrain, not a claim
that the opaque Earth's silhouette is physically smooth.

glTF world units are treated as meters and converted once to kilometers.
Near-ground atmosphere queries use a two-meter altitude floor for Earth-centered
float32 precision. The sky table represents the observer altitude throughout
the local scene, while direct solar attenuation is queried at each surface.
The camera-volume LUT spans the camera far distance with squared depth spacing.
Each column uses one cumulative march with four substeps between depth slices,
and saves its prefixes into all 16 layers. Accumulated scattering cannot lose
previous contributions as depth increases, preventing independent-slice
resampling from introducing depth bands. A column takes at most 60 segments
instead of 240, reducing repeated visibility queries by 75% during LUT updates.
Primary surfaces receive `radiance * transmittance + inScattering` before native
film accumulation or NGX; sky misses already include their atmosphere integral.
Consequently, atmospheric haze is subtle over Sponza's tens-of-meters distances.
The local aerial pass queries the actual alpha-masked scene at each integration
point: one solar visibility ray and two deterministic sky visibility rays.
This stops roofs and walls from receiving unoccluded outdoor air illumination.
Sky visibility modulates the multiple-scattering source as a local approximation;
it does not solve volumetric room light transport. Extinction and transmittance
remain unchanged. The TLAS is updated before this pass, and animated geometry
invalidates its cached tables. Static atmosphere and sky passes do not trace
these additional scene rays.

This is a clear-sky model: no clouds, weather animation, moon, stars or geographic
calendar. The daylight timeline stops at the horizon at both ends; it does not
include pre-dawn or post-sunset twilight. Earth shadow and local camera-volume
occlusion are included; full volumetric multiple scattering within buildings
and atmospheric transport along every secondary local ray segment are not.
Surface lighting and shadows still use
the actual ray-traced scene. Exposure and tone mapping remain separate.

## License and sources

The adapted modules retain the authors' notices and the accompanying
[Apache-2.0 license](Assets/Shaders/Pbrt.LICENSE.txt), copied with the shader assets.
The source mappings above refer to PBRT's `samplers.h`, `util/lowdiscrepancy.h`,
`util/rng.h`, `util/hash.h`, `util/scattering.h`, `interaction.h`, `bxdfs.h`,
`bxdfs.cpp`, `materials.h`, `cpu/integrators.cpp`, `cameras.cpp` and `film.cpp`.

The Hillaire adaptation retains Epic Games' copyright and the full
[MIT license](Assets/Shaders/Atmosphere.LICENSE.txt). Source mappings are
`RenderSkyCommon.hlsl`, `RenderSkyRayMarching.hlsl` and `SkyAtmosphereCommon.cpp`.
