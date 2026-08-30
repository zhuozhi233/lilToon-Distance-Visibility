// Generated bridge for lilToon Distance Visibility.
#define LIL_CUSTOM_PROPERTIES \
    float _DV_Enabled; \
    float _DV_Mode; \
    float _DV_NearEnabled; \
    float _DV_NearDistance; \
    float _DV_NearFade; \
    float _DV_FarEnabled; \
    float _DV_FarDistance; \
    float _DV_FarFade; \
    float _DV_UseMeshCenter; \
    float _DV_Version;

#define LIL_V2F_FORCE_POSITION_WS
#define BEFORE_UNPACK_V2F \
    DistanceVisibilityClip(lilToAbsolutePositionWS(input.positionWS), lilToAbsolutePositionWS(lilTransformOStoWS(float3(0.0, 0.0, 0.0))), input.positionCS.xy);
