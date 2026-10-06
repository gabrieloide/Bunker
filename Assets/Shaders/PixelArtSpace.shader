Shader "Bunker/PixelArtSpace"
{
    Properties
    {
        [Header(Base Colors)]
        _SpaceColor ("Deep Space Color", Color) = (0.02, 0.02, 0.06, 1.0)
        _StarColorFar ("Far Stars Color", Color) = (0.55, 0.65, 0.85, 1.0)
        _StarColorMid ("Mid Stars Color", Color) = (0.85, 0.90, 1.0, 1.0)
        _StarColorNear ("Near Stars Color", Color) = (1.0, 1.0, 0.9, 1.0)

        [Header(Pixel Resolution)]
        _PixelResolution ("Pixel Grid Resolution (W, H)", Vector) = (320, 180, 0, 0)
        [Toggle] _PixelLockMovement ("Pixel-Locked Movement", Float) = 1.0

        [Header(Movement)]
        _MoveSpeed ("Base Movement Speed", Float) = 4.0
        _MoveDirection ("Movement Direction (X, Y)", Vector) = (-1.0, -0.3, 0, 0)

        [Header(Twinkle Effect)]
        _TwinkleFPS ("Twinkle Step Speed (FPS)", Float) = 4.0
        _TwinkleIntensity ("Twinkle Intensity", Range(0.0, 1.0)) = 0.6

        [Header(Star Densities)]
        _DensityFar ("Far Layer Density", Range(0.05, 0.8)) = 0.40
        _DensityMid ("Mid Layer Density", Range(0.05, 0.6)) = 0.22
        _DensityNear ("Near Layer Density", Range(0.02, 0.4)) = 0.10

        [Header(Dithered Nebula)]
        [Toggle] _EnableNebula ("Enable Dithered Nebula", Float) = 1.0
        _NebulaColor ("Nebula Color", Color) = (0.09, 0.04, 0.18, 1.0)
        _NebulaScale ("Nebula Scale", Float) = 0.02
        _NebulaSpeed ("Nebula Drift Speed", Float) = 0.5

        [Header(Shooting Stars)]
        [Toggle] _EnableShootingStars ("Enable Shooting Stars", Float) = 1.0
        _ShootingStarInterval ("Interval (Seconds)", Float) = 5.0
        _ShootingStarSpeed ("Shooting Star Speed", Float) = 120.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Background" 
            "IgnoreProjector" = "True"
        }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "PixelSpaceUnlit"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _SpaceColor;
            fixed4 _StarColorFar;
            fixed4 _StarColorMid;
            fixed4 _StarColorNear;

            float4 _PixelResolution;
            float _PixelLockMovement;

            float _MoveSpeed;
            float4 _MoveDirection;

            float _TwinkleFPS;
            float _TwinkleIntensity;

            float _DensityFar;
            float _DensityMid;
            float _DensityNear;

            float _EnableNebula;
            fixed4 _NebulaColor;
            float _NebulaScale;
            float _NebulaSpeed;

            float _EnableShootingStars;
            float _ShootingStarInterval;
            float _ShootingStarSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // --- Pseudo-random Hashes ---
            float Hash21(int2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Hash22(int2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // --- Bayer 4x4 Matrix for Dithering ---
            static const float Bayer4x4[16] = {
                 0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                 3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
            };

            // --- Star Pixel Shapes ---
            // 1: Dot (1x1)
            // 2: Hollow Cross (3x3 with empty center)
            // 3: Solid Cross (3x3 with filled center)
            // 4: Elongated Flare (5x5, 3 width, 5 height)
            bool CheckStarShape(int starType, int dx, int dy)
            {
                int adx = abs(dx);
                int ady = abs(dy);

                if (starType == 1)
                {
                    return (adx == 0 && ady == 0);
                }
                else if (starType == 2)
                {
                    return (adx + ady == 1);
                }
                else if (starType == 3)
                {
                    return (adx + ady <= 1);
                }
                else if (starType == 4)
                {
                    return ((adx == 0 && ady <= 2) || (ady == 0 && adx <= 1));
                }

                return false;
            }

            // --- Layer Evaluation ---
            // Returns (hit: 0 or 1, brightness: 0..1)
            float2 EvaluateStarLayer(int2 pixelCoord, int cellSize, float layerSpeed, float density, int forcedTypeMin, int forcedTypeMax, int layerSeed)
            {
                // Scroll in pixels
                float2 scroll = _MoveDirection.xy * (_Time.y * _MoveSpeed * layerSpeed);
                if (_PixelLockMovement > 0.5)
                {
                    scroll = floor(scroll);
                }

                int2 shiftedPixel = pixelCoord - int2(floor(scroll));

                // Determine Cell
                // Floor division safe for negative numbers
                int2 cell = int2(
                    shiftedPixel.x >= 0 ? shiftedPixel.x / cellSize : (shiftedPixel.x - cellSize + 1) / cellSize,
                    shiftedPixel.y >= 0 ? shiftedPixel.y / cellSize : (shiftedPixel.y - cellSize + 1) / cellSize
                );

                int2 localPos = shiftedPixel - cell * cellSize;

                // Probability of star in cell
                float starRnd = Hash21(cell + layerSeed * 17);
                if (starRnd > density)
                {
                    return float2(0.0, 0.0);
                }

                // Star center placed inside cell with margin [3, cellSize - 4] so it never crosses cell boundaries
                int margin = 3;
                int usableSize = max(1, cellSize - margin * 2);
                float2 centerOffset = Hash22(cell + layerSeed * 31);
                int2 center = int2(margin, margin) + int2(floor(centerOffset * (float)usableSize));

                int dx = localPos.x - center.x;
                int dy = localPos.y - center.y;

                // Star type selection
                float typeRnd = Hash21(cell + layerSeed * 73);
                int starType = forcedTypeMin + (int)floor(typeRnd * (float)(forcedTypeMax - forcedTypeMin + 1));
                starType = clamp(starType, forcedTypeMin, forcedTypeMax);

                if (!CheckStarShape(starType, dx, dy))
                {
                    return float2(0.0, 0.0);
                }

                // Stepped Retro Twinkle
                float steppedTime = floor(_Time.y * _TwinkleFPS);
                float twinkleRnd = frac(sin(steppedTime * 0.45 + starRnd * 89.1) * 43758.5453);
                
                // Discrete brightness levels (40%, 75%, 100%)
                float brightness = 1.0;
                if (twinkleRnd < 0.3)
                    brightness = 1.0 - _TwinkleIntensity * 0.6;
                else if (twinkleRnd < 0.65)
                    brightness = 1.0 - _TwinkleIntensity * 0.3;

                return float2(1.0, brightness);
            }

            // --- Simple Dithered Nebula ---
            float SimpleNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(int2(i));
                float b = Hash21(int2(i + float2(1.0, 0.0)));
                float c = Hash21(int2(i + float2(0.0, 1.0)));
                float d = Hash21(int2(i + float2(1.0, 1.0)));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Determine virtual pixel coordinate
                float2 res = _PixelResolution.xy;
                if (res.x <= 0.0 || res.y <= 0.0)
                {
                    res = _ScreenParams.xy / 2.0;
                }

                int2 pixel = int2(floor(i.uv * res));

                // 1. Base Space Background
                fixed4 col = _SpaceColor;

                // 2. Dithered Nebula (Optional)
                if (_EnableNebula > 0.5)
                {
                    float2 nebUv = (float2)pixel * _NebulaScale + _Time.y * _NebulaSpeed * float2(0.05, 0.02);
                    float n = SimpleNoise(nebUv);
                    n += 0.5 * SimpleNoise(nebUv * 2.0 + 3.1);
                    n *= 0.66; // Normalize to approx 0..1

                    int ditherIndex = (pixel.x % 4) + (pixel.y % 4) * 4;
                    float threshold = Bayer4x4[ditherIndex];

                    // Dithered blend between space and nebula color
                    if (n > threshold * 0.8 + 0.3)
                    {
                        col = lerp(col, _NebulaColor, 0.85);
                    }
                    else if (n > threshold * 0.5 + 0.45)
                    {
                        col = lerp(col, _NebulaColor, 0.45);
                    }
                }

                // 3. Layer 1: Far Stars (Cell size 16, Speed 0.25x, Types 1 & 2)
                float2 l1 = EvaluateStarLayer(pixel, 16, 0.25, _DensityFar, 1, 2, 101);
                if (l1.x > 0.5)
                {
                    col = lerp(col, _StarColorFar * l1.y, 0.9);
                }

                // 4. Layer 2: Mid Stars (Cell size 24, Speed 0.65x, Types 2 & 3)
                float2 l2 = EvaluateStarLayer(pixel, 24, 0.65, _DensityMid, 2, 3, 202);
                if (l2.x > 0.5)
                {
                    col = lerp(col, _StarColorMid * l2.y, 0.95);
                }

                // 5. Layer 3: Near Stars (Cell size 40, Speed 1.1x, Types 3 & 4)
                float2 l3 = EvaluateStarLayer(pixel, 40, 1.10, _DensityNear, 3, 4, 303);
                if (l3.x > 0.5)
                {
                    col = _StarColorNear * l3.y;
                }

                // 6. Occasional Shooting Star (Diagonal Streak)
                if (_EnableShootingStars > 0.5 && _ShootingStarInterval > 0.0)
                {
                    float cycleTime = fmod(_Time.y, _ShootingStarInterval);
                    if (cycleTime < 0.6) // Duration of shoot: 0.6 seconds
                    {
                        float cycleSeed = floor(_Time.y / _ShootingStarInterval);
                        float shootProgress = cycleTime / 0.6;

                        // Start position
                        float2 startPos = float2(
                            frac(sin(cycleSeed * 78.233) * 43758.5453) * res.x,
                            frac(sin(cycleSeed * 12.989) * 23421.6312) * res.y * 0.6 + res.y * 0.4
                        );

                        float2 dir = normalize(float2(-1.5, -1.0));
                        float2 headPos = startPos + dir * (shootProgress * _ShootingStarSpeed);

                        // Check distance to pixel along the shooting line
                        float2 toPixel = (float2)pixel - headPos;
                        float proj = dot(toPixel, -dir); // Distance behind head
                        float distPerp = abs(toPixel.x * dir.y - toPixel.y * dir.x); // Distance perpendicular to line

                        // Tail length is 10 pixels, thickness 1 pixel
                        if (proj >= 0.0 && proj <= 10.0 && distPerp < 0.75)
                        {
                            float fade = 1.0 - (proj / 10.0);
                            col = lerp(col, fixed4(1, 1, 1, 1), fade);
                        }
                    }
                }

                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
