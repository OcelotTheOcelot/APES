#define PIXELATION_ABSOLUTE_PIXEL_SIZE 4
#define PIXELATION_REFERENCE_RESOLUTION 1080
#define PIXELATION_COLOR_VARIATION 128

#define PIXELATION_PIXEL_SIZE (PIXELATION_ABSOLUTE_PIXEL_SIZE * (_ScreenParams.y / PIXELATION_REFERENCE_RESOLUTION))

half3 gradeColor(half3 col, float power)
{
    return floor((col * power) + 0.5f) / power;
}

// <TODO> optimization needed!
// Original idea by Elliot Bentine.
#define PIXELATION_DITHER_PRECISION 0.99
inline float Dither(float4 posCS)
{
    // I know UNITY_MATRIX_MVP spams Warning, but I have no clue how to eal with it yet.
    float2 originPosCS = mul(UNITY_MATRIX_MVP, float4(0, 0, 0, 1)).xy;
    posCS.xy += originPosCS * half2(-0.5f, 0.5f) * _ScreenParams.xy;

    int pixelSize = PIXELATION_PIXEL_SIZE;
    int xfactor = step(
        fmod(
            abs(floor(posCS.x)),
            pixelSize
        ), PIXELATION_DITHER_PRECISION
    );
    int yfactor = step(
        fmod(
            abs(floor(posCS.y - pixelSize)),
            pixelSize
        ), PIXELATION_DITHER_PRECISION
    );

    return xfactor * yfactor;
}