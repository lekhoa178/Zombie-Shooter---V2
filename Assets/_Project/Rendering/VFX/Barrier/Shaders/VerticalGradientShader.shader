Shader "Custom/VerticalGradient"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _Height ("Gradient Height", Float) = 1.0
        _OriginOffset ("Origin Offset", Float) = 0.0
        [Toggle] _UseCircleMask ("Use Circle Mask", Float) = 1
        _CircleRadius ("Circle Radius", Range(0.1, 5)) = 1.75
        _CircleIntensity ("Circle Intensity", Range(0.0, 1.0)) = 0.9
        _CircleCenterX ("Circle Center X", Range(0.0, 1.0)) = 0.5
        _CircleCenterY ("Circle Center Y", Range(0.0, 1.0)) = 0.5
        _GradientPower ("Gradient Power", Range(0.1, 20.0)) = 2.0
        _ColorTex ("Color Gradient Texture", 2D) = "white" {}
        [Toggle] _UseColorMap ("Use Color Map", Float) = 0
        _CloudTex ("Cloud Noise Texture", 2D) = "white" {}
        [Toggle] _UseNoise ("Use Noise Effect", Float) = 0
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.5
        _NoiseScrollSpeedY ("Noise Scroll Speed Y", Range(-0.2, 0.2)) = 0.03
        _NoiseScrollSpeedX ("Noise Scroll Speed X", Range(-0.2, 0.2)) = 0.02
        _NoiseMin ("Noise Min Value", Range(-1.0, 1.0)) = 0.0
        _NoiseMax ("Noise Max Value", Range(-1.0, 2.0)) = 1.0
        _ScreenBlendIntensity ("Screen Blend Intensity", Range(0.1, 3.0)) = 1.5
        [Toggle] _UseAlphaGradient ("Use Alpha Gradient", Float) = 1
        _AlphaGradientStrength ("Alpha Gradient Strength", Range(0.0, 1.0)) = 1.0
        [Toggle] _UseDepthEffect ("Use Depth Effect", Float) = 0
        [Header(Intersection Effect)]
        [Toggle] _UseIntersection ("Use Intersection Effect", Float) = 0
        _IntersectionColor ("Intersection Color", Color) = (1,1,1,1)
        _IntersectionIntensity ("Intersection Intensity", Range(0.0, 10.0)) = 1.0
        _IntersectionThreshold ("Intersection Threshold", Range(0.0, 1.0)) = 0.1
        [Toggle] _UseDissolve ("Use Dissolve Effect", Float) = 0
        _BarrierDissolve ("Dissolve Threshold", Range(0.0, 1.0)) = 0.5
        [Toggle] _InvertDissolveY ("Invert Dissolve Y Direction", Float) = 0
        _DissolveScrollX ("Dissolve Scroll X", Range(-1.0, 1.0)) = 0.0
        _DissolveScrollY ("Dissolve Scroll Y", Range(-1.0, 1.0)) = 0.0
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeIntensity ("Edge Intensity", Range(0.0, 5.0)) = 1.0
        _EdgeWidth ("Edge Width", Range(0.001, 0.1)) = 0.01
        _DissolveTex ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveNoiseStrength ("Dissolve Noise Strength", Range(0.0, 1.0)) = 0.3
        _DissolveDirectionStrength ("Dissolve Direction Strength", Range(0.0, 5.0)) = 2.0
        [Header(Star Effect)]
        [Toggle] _UseStars ("Use Star Effect", Float) = 0
        _StarTex ("Star Texture", 2D) = "black" {}
        _StarNoiseTex ("Star Noise Texture", 2D) = "black" {}
        _StarScrollSpeed ("Star Scroll Speed", Range(-1.0, 1.0)) = -0.035
        _StarNoiseScrollSpeed ("Star Noise Scroll Speed", Range(-1.0, 1.0)) = 0.5
        _StarScroll ("Star Scroll Threshold", Range(0.0, 1.0)) = 0.5
        _StarMask ("Star Gradient Mask", Range(0.0, 1.0)) = 0.5
        _StarIntensity ("Star Intensity", Range(0.0, 5.0)) = 1.0
        _StarNoiseIntensity ("Star Noise Intensity", Range(0.0, 5.0)) = 1.0
        _StarCutoff ("Star Cutoff Threshold", Range(0.0, 1.0)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 originOS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            
            TEXTURE2D(_ColorTex);
            SAMPLER(sampler_ColorTex);
            
            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);
            
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);
            
            TEXTURE2D(_StarTex);
            SAMPLER(sampler_StarTex);
            
            TEXTURE2D(_StarNoiseTex);
            SAMPLER(sampler_StarNoiseTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopColor;
                float _Height;
                float _OriginOffset;
                float _UseCircleMask;
                float _CircleRadius;
                float _CircleIntensity;
                float _CircleCenterX;
                float _CircleCenterY;
                float _GradientPower;
                float _UseColorMap;
                float _UseNoise;
                float _NoiseIntensity;
                float _NoiseScrollSpeedY;
                float _NoiseScrollSpeedX;
                float _NoiseMin;
                float _NoiseMax;
                float _ScreenBlendIntensity;
                float _UseAlphaGradient;
                float _AlphaGradientStrength;
                float _UseDepthEffect;
                float _UseIntersection;
                float4 _IntersectionColor;
                float _IntersectionIntensity;
                float _IntersectionThreshold;
                float _UseDissolve;
                float _BarrierDissolve;
                float _InvertDissolveY;
                float _DissolveScrollX;
                float _DissolveScrollY;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeWidth;
                float _DissolveNoiseStrength;
                float _DissolveDirectionStrength;
                float _UseStars;
                float _StarScroll;
                float _StarMask;
                float _StarIntensity;
                float _StarNoiseIntensity;
                float _StarCutoff;
                float _StarScrollSpeed;
                float _StarNoiseScrollSpeed;
                
                // Texture tiling and offset properties
                float4 _ColorTex_ST;
                float4 _CloudTex_ST;
                float4 _DissolveTex_ST;
                float4 _StarTex_ST;
                float4 _StarNoiseTex_ST;
            CBUFFER_END
            
            // Remap function since Unity doesn't have built-in unityRemap
            float Remap(float value, float2 inputRange, float2 outputRange)
            {
                return outputRange.x + (value - inputRange.x) * (outputRange.y - outputRange.x) / (inputRange.y - inputRange.x);
            }
            
            // Circle mask generation function
            float genSmoothCircle(float2 uv, float2 centerUV, float radius)
            {
                float2 center = uv - centerUV;
                float dist = (distance(center, float2(0,0)))*2;
                float circle = smoothstep(0, radius, dist);
                return 1 - circle;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.originOS = IN.positionOS.xyz;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                // Calculate gradient based on y-position
                float gradient = IN.originOS.y + _OriginOffset;
                // gradient = pow(gradient, 5);
                gradient = clamp(Remap(gradient, float2(0, _Height), float2(0, 1)), 0, 1);
                float gradientINV = 1 - gradient;
                // gradientINV = pow(gradientINV, 5);
                // return float4(gradientINV.xxx, 1);

                // Generate circle mask if enabled
                float circleMask = 1.0;
                if (_UseCircleMask > 0.5) {
                    float2 centerUV = float2(_CircleCenterX, _CircleCenterY);
                    circleMask = genSmoothCircle(float2(IN.uv.x, IN.uv.y), centerUV, _CircleRadius) * _CircleIntensity;
                    circleMask = pow(circleMask, 0.6);
                } else {
                    // When circle mask is disabled, use 0 instead of 1 for calculations
                    circleMask = 0.0;
                }

                // Calculate cloud noise if enabled
                float cloudNoise = 1.0;
                if (_UseNoise > 0.5) {
                    // Apply texture tiling and offset
                    float2 cloudUV1 = IN.uv * _CloudTex_ST.xy + _CloudTex_ST.zw;
                    float2 cloudUV2 = IN.uv * _CloudTex_ST.xy + _CloudTex_ST.zw;
                    
                    float noise1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, float2(cloudUV1.x, cloudUV1.y - _Time.y * _NoiseScrollSpeedY) * 2).r;
                    float noise2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, float2(cloudUV2.x + _Time.y * _NoiseScrollSpeedX, cloudUV2.y + _Time.y * _NoiseScrollSpeedX)).r;
                    cloudNoise = noise1 * noise2;
                    
                    // Remap noise value using extended range
                    cloudNoise = Remap(cloudNoise, float2(0, 1), float2(_NoiseMin, _NoiseMax));
                    
                    // Apply intensity without clamping
                    cloudNoise = lerp(1.0, cloudNoise * gradientINV, _NoiseIntensity);
                }

                cloudNoise += 0.2;
                cloudNoise = clamp(cloudNoise, 0.0, 1.0);
                
                // Apply cloud noise to create base cloud effect before color mapping
                // Use (1 - circleMask) when circle mask is enabled, otherwise use 1.0
                float maskFactor = _UseCircleMask > 0.5 ? (1 - circleMask) : gradient;
                // return float4(maskFactor.xxx, 1);
                float cloudEffect = cloudNoise * maskFactor * _ScreenBlendIntensity;
                
                float3 mainColor;
                
                if (_UseColorMap > 0.5) {
                    // Apply color mapping to the result
                    // Sample gradient texture based on cloudEffect value (0-1)
                    
                    // Apply texture tiling and offset for color gradient
                    float2 colorMapUV = float2(saturate(cloudEffect), 0) * _ColorTex_ST.xy + _ColorTex_ST.zw;
                    float3 mappedColor = SAMPLE_TEXTURE2D(_ColorTex, sampler_ColorTex, colorMapUV).rgb;
                    
                    // Blend the cloud gradient with the mapped color
                    mainColor = mappedColor;
                } else {
                    // First create the base gradient and cloud effect that's common to both workflows
                    float3 baseColor = lerp(_BaseColor.rgb, _TopColor.rgb, clamp(pow(cloudEffect, _GradientPower), 0, 1));
                    
                    // Use the cloud gradient directly for standard mode
                    mainColor = baseColor;
                }

                // Apply fog
                float3 screenColor = MixFog(mainColor, IN.fogFactor);
                
                // Set alpha value with controllable strength
                float alpha = _UseAlphaGradient > 0.5 ? lerp(1.0, gradientINV, _AlphaGradientStrength) : 1.0;
                
                // Calculate intersection effect
                float intersectionEffect = 0;
                if (_UseIntersection > 0.5) {
                    // Get scene depth (in view space)
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w), _ZBufferParams);
                    
                    // Get depth of current fragment (in view space)
                    float fragmentDepth = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                    
                    // Calculate intersection based on depth difference
                    float depthDifference = sceneDepth - fragmentDepth;
                    
                    // Create smooth intersection effect based on threshold
                    intersectionEffect = 1.0 - saturate(depthDifference / _IntersectionThreshold);
                    
                    // Apply smoothing
                    intersectionEffect = smoothstep(0, 1, intersectionEffect);
                    
                    // Apply intensity
                    intersectionEffect *= _IntersectionIntensity;
                }
                
                // Sample depth
                float usableDepth = 0;
                if (_UseDepthEffect > 0.5) {
                    // Simplified depth calculation without LinearEyeDepth
                    float rawDepth = SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w);
                    
                    // Calculate simple depth-based effect
                    float depthFactor = lerp(0.0, 1.0, rawDepth);
                    usableDepth = (smoothstep(0.9, 1, depthFactor) * 1.5) * gradientINV;
                }
                
                // Initialize dissolve effect variables
                float edge = 0;
                float mask = 1.0;
                
                // Apply dissolve effect if enabled
                if (_UseDissolve > 0.5) {
                    // Apply texture tiling and offset for dissolve with scrolling
                    float2 dissolveUV = IN.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
                    
                    // Apply scrolling to dissolve texture
                    dissolveUV.x += _Time.y * _DissolveScrollX;
                    dissolveUV.y += _Time.y * _DissolveScrollY;
                    
                    // Sample dissolve noise texture
                    float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
                    
                    // Create enhanced bottom-to-top dissolve effect
                    // If invert is enabled, flip the gradient direction
                    float directionalGradient;
                    if (_InvertDissolveY > 0.5) {
                        directionalGradient = pow(1.0 - gradient, _DissolveDirectionStrength);
                    } else {
                        directionalGradient = pow(gradient, _DissolveDirectionStrength);
                    }
                    
                    // Blend the noise with the directional gradient
                    // The _DissolveNoiseStrength controls how much influence the noise has
                    float dissolveValue = lerp(directionalGradient, directionalGradient * dissolveNoise, _DissolveNoiseStrength);
                    
                    // Calculate dissolve effect with edge width that scales to zero when threshold is zero
                    float edgeWidth = _BarrierDissolve > 0.001 ? _EdgeWidth : 0.0;
                    float edgeStart = max(0.001, _BarrierDissolve);
                    float edgeEnd = edgeStart + edgeWidth;
                    
                    if (_BarrierDissolve <= 0.001) {
                        // When dissolve threshold is zero or very close to it, no dissolve effect
                        mask = 1.0;
                        edge = 0.0;
                    } else {
                        // Normal dissolve calculation
                        mask = smoothstep(edgeStart, edgeStart + (edgeWidth * 0.1), dissolveValue);
                        edge = smoothstep(edgeStart + (edgeWidth * 0.1), edgeEnd, dissolveValue);
                        edge = mask - edge;
                    }
                    
                    // Apply edge color
                    edge *= _EdgeColor.rgb * _EdgeIntensity;
                }
                
                // Calculate star effect if enabled
                float3 starMask = 0;
                if (_UseStars > 0.5) {
                    // Apply texture tiling and offset for star textures
                    float2 starUV = IN.uv * _StarTex_ST.xy + _StarTex_ST.zw;
                    float2 starNoiseUV = IN.uv * _StarNoiseTex_ST.xy + _StarNoiseTex_ST.zw;
                    
                    // Sample star textures with scrolling UVs
                    float3 startex = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, float2(starUV.x, starUV.y - _Time.y * _StarScrollSpeed) * 1.5).rgb;
                    float starnoise = SAMPLE_TEXTURE2D(_StarNoiseTex, sampler_StarNoiseTex, float2(starNoiseUV.x, starNoiseUV.y - _Time.y * _StarNoiseScrollSpeed)).r;
                    
                    // Extract star channels
                    float stars = startex.r;
                    float stars2 = startex.g;
                    
                    // Create star mask using noise and gradient
                    float starNoiseEffect = smoothstep(_StarScroll, _StarScroll + 0.2, starnoise) - smoothstep(_StarScroll + 0.2, _StarScroll + 0.4, starnoise);
                    float starGradient = smoothstep(_StarMask, _StarMask + 0.2, gradient) - smoothstep(_StarMask + 0.2, _StarMask + 0.4, gradient);
                    
                    // Multiply star intensity with noise
                    float starNoiseModulation = starnoise * _StarNoiseIntensity;
                    
                    // Combine masks with noise intensity
                    float starValue = stars * starNoiseEffect * starGradient * starNoiseModulation;
                    float star2Value = stars2 * starNoiseModulation;
                    
                    // Apply cutoff threshold
                    starValue = starValue > _StarCutoff ? starValue : 0;
                    star2Value = star2Value > _StarCutoff ? star2Value : 0;
                    
                    // Combine and apply intensity
                    starMask = (starValue + star2Value) * _StarIntensity;
                }
                
                // Calculate final color with all effects
                float3 finalColor = screenColor + edge + usableDepth;
                
                // Add intersection effect
                if (_UseIntersection > 0.5) {
                    finalColor = lerp(finalColor, _IntersectionColor.rgb, saturate(intersectionEffect));
                }
                
                // Add star effect if enabled
                if (_UseStars > 0.5) {
                    finalColor += starMask;
                }
                
                // Return with appropriate alpha and controllable gradient strength
                return float4(finalColor, (_UseDissolve > 0.5) ? 
                    ((edge + mask) * lerp(1.0, gradientINV, _AlphaGradientStrength)) : 
                    alpha);
            }
            ENDHLSL
        }
        
        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            float4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}