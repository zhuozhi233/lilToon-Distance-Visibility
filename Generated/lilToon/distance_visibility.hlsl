// The include is inserted after Unity and lilToon common helpers.
float DistanceVisibilityHash(float2 pixelPosition)
{
    // Stable screen-space interleaved gradient noise for a dithered fade.
    return frac(52.9829189 * frac(dot(floor(pixelPosition), float2(0.06711056, 0.00583715))));
}

float DistanceVisibilityFadeIn(float distanceToCamera, float boundary, float width)
{
    width = max(width, 0.0);
    return width < 0.00001 ? step(boundary, distanceToCamera) : saturate((distanceToCamera - boundary) / width);
}

float DistanceVisibilityFadeOut(float distanceToCamera, float boundary, float width)
{
    width = max(width, 0.0);
    return width < 0.00001 ? step(distanceToCamera, boundary) : saturate((boundary - distanceToCamera) / width);
}

void DistanceVisibilityClip(float3 absolutePositionWS, float3 meshCenterWS, float2 pixelPosition)
{
#if !defined(UNITY_PASS_META)
    float useMeshCenter = step(0.5, _DV_UseMeshCenter);
    float3 distancePositionWS = lerp(absolutePositionWS, meshCenterWS, useMeshCenter);
    float distanceToCamera = distance(distancePositionWS, _WorldSpaceCameraPos.xyz);
    float nearVisibility = DistanceVisibilityFadeIn(distanceToCamera, max(_DV_NearDistance, 0.0), _DV_NearFade);
    float farVisibility = DistanceVisibilityFadeOut(distanceToCamera, max(_DV_FarDistance, 0.0), _DV_FarFade);
    float isNearOnly = step(0.5, _DV_Mode) * (1.0 - step(1.5, _DV_Mode));
    float isFarOnly = step(1.5, _DV_Mode);
    float useLegacyMode = 1.0 - step(2.5, _DV_Version);
    float nearEnabled = lerp(step(0.5, _DV_NearEnabled), 1.0 - isNearOnly, useLegacyMode);
    float farEnabled = lerp(step(0.5, _DV_FarEnabled), 1.0 - isFarOnly, useLegacyMode);
    nearVisibility = lerp(1.0, nearVisibility, nearEnabled);
    farVisibility = lerp(1.0, farVisibility, farEnabled);
    float visibility = min(nearVisibility, farVisibility);
    visibility = lerp(1.0, visibility, step(0.5, _DV_Enabled));
    float ditherThreshold = DistanceVisibilityHash(pixelPosition) * (254.0 / 256.0) + (1.0 / 256.0);
    clip(visibility - ditherThreshold);
#endif
}
