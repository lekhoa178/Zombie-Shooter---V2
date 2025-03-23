Shader "Unlit/EmptyPass"
{
   
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Transparent" }
        LOD 100

        Pass{
            Name "Empty"
             ZWrite On
            ColorMask 0
        }
    }
}
