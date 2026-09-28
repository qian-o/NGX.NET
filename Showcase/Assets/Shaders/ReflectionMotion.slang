// RR accepts reflection motion instead of specular hit distance. Motion is in
// input pixels, current -> previous, excluding jitter, just like primary motion.
bool projectReflectionMotion(float3 current, float3 previous, bool sky, out float2 motion)
{
    float4 a = mul(float4(current, sky ? 0 : 1), g.currentViewProjection);
    float4 b = mul(float4(previous, sky ? 0 : 1), g.previousViewProjection);
    motion = 0;
    if (a.w <= 0 || b.w <= 0) return false;
    motion = (b.xy / b.w - a.xy / a.w) * float2(0.5, -0.5) * g.size.xy;
    return all(isfinite(motion)) && all(abs(motion) <= 65504.0); // RG16_FLOAT
}

// Solve the reflection law on a convex sphere: the normal bisects directions
// to the camera and the reflected point (or sky direction). Newton steps in
// the normal's tangent plane account for curvature rather than treating the
// sphere as a translating flat mirror. The radius comes from CreateSphere.
bool sphereReflectionPoint(float3 center, float radius, float3 camera, float3 target,
                           bool sky, float3 normal, out float3 point)
{
    point = center + radius * normal;
    if (length(camera - center) <= radius || (!sky && length(target - center) <= radius)) return false;
    const uint maxIterations = 8;
    const float angularTolerance = 1e-5;
    for (uint iteration = 0; iteration < maxIterations; iteration++)
    {
        point = center + radius * normal;
        float3 toCamera = camera - point, toTarget = sky ? target : target - point;
        float inverseCameraDistance = rcp(length(toCamera));
        float inverseTargetDistance = sky ? 0 : rcp(length(toTarget));
        float3 v = toCamera * inverseCameraDistance, l = normalize(toTarget);
        float3 sum = v + l;
        float3 t = tangentAxis(normal), b = cross(normal, t);
        float2 error = float2(dot(sum, t), dot(sum, b));
        if (dot(error, error) <= angularTolerance * angularTolerance)
            return dot(normal, v) > 0 && dot(normal, l) > 0;

        float2 vt = float2(dot(v, t), dot(v, b)), lt = float2(dot(l, t), dot(l, b));
        float a = radius * inverseCameraDistance, d = radius * inverseTargetDistance;
        float diagonal = dot(normal, sum) + a + d;
        float xx = diagonal - a * vt.x * vt.x - d * lt.x * lt.x;
        float xy = -a * vt.x * vt.y - d * lt.x * lt.y;
        float yy = diagonal - a * vt.y * vt.y - d * lt.y * lt.y;
        float determinant = xx * yy - xy * xy;
        if (determinant <= 0) return false;
        float2 step = float2(yy * error.x - xy * error.y, xx * error.y - xy * error.x) / determinant;
        normal = normalize(normal + t * step.x + b * step.y);
    }
    return false; // No valid correspondence: retain primary surface motion.
}

#if HAS_RAY_QUERY
float2 reflectionMotion(uint2 pixel, Surface s, float3 p, float3 view, Hit sampledHit, bool sampled)
{
    uint triangle = (uint)surfaceGeometryImage[pixel].z;
    uint objectIndex = (uint)vertices[triangle * 3].position.w;
    ObjectData object = objects[objectIndex];
    float3 surfaceMotion = object.previousOffset.xyz - object.offset.xyz;
    float2 fallback;
    if (!projectReflectionMotion(p, p + surfaceMotion, false, fallback)) fallback = 0;

    if (object.offset.w > 0)
    {
        // A stable mirror ray supplies a correspondence, not radiance. Random
        // microfacet samples would make this motion guide fluctuate every frame.
        float3 direction = reflect(-view, s.normal);
        if (!aboveSurface(s, direction)) return fallback;
        float3 origin = rayOrigin(p, s);
        Hit hit = trace(origin, direction, g.scene.w * 4);
        bool sky = hit.triangle == 0xffffffff;
        if (!sky && hit.object == objectIndex) return fallback;
        float3 target = sky ? direction : origin + direction * hit.distance;
        float3 previousTarget = target;
        if (!sky) previousTarget += objects[hit.object].previousOffset.xyz - objects[hit.object].offset.xyz;

        float3 currentPoint, previousPoint;
        if (!sphereReflectionPoint(object.offset.xyz, object.offset.w, g.camera.xyz, target,
            sky, s.normal, currentPoint)) return fallback;
        float3 normal = normalize(currentPoint - object.offset.xyz);
        if (!sphereReflectionPoint(object.previousOffset.xyz, object.offset.w, g.previousCamera.xyz,
            previousTarget, sky, normal, previousPoint)) return fallback;
        // Solve both frames to cancel the small difference between the smooth
        // analytic sphere and its rasterized triangle mesh, including at rest.
        float2 motion;
        return projectReflectionMotion(currentPoint, previousPoint, false, motion) ? motion : fallback;
    }

    if (!sampled) return fallback;
    bool sky = sampledHit.triangle == 0xffffffff;
    float3 current = sky ? -view : p - view * sampledHit.distance;
    float3 previous = current;
    if (!sky)
    {
        float3 hitMotion = objects[sampledHit.object].previousOffset.xyz - objects[sampledHit.object].offset.xyz;
        previous += reflect(hitMotion, s.normal) + 2 * s.normal * dot(surfaceMotion, s.normal);
    }
    float2 motion;
    return projectReflectionMotion(current, previous, sky, motion) ? motion : fallback;
}
#endif
