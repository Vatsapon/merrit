Shader "Minimap/FogOfWar2"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _TransparentColor("Transparent Color", Color) = (1,1,1,1)
        _Threshold("Threshhold", Float) = 0.1
        _MainTex("Albedo (RGB)", 2D) = "white" {}

        _StencilComp("Stencil Comparison", Float) = 8
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }
        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
            }

            LOD 200

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }

            ColorMask[_ColorMask]

            CGPROGRAM
            #pragma surface surf Lambert alpha

            sampler2D _MainTex;

            struct Input 
            {
                float2 uv_MainTex;
            };

            fixed4 _Color;
            fixed4 _TransparentColor;
            half _Threshold;

            void surf(Input IN, inout SurfaceOutput o)
            {
                // Read color from the texture
                half4 c = tex2D(_MainTex, IN.uv_MainTex);

                // Output colour will be the texture color * the vertex colour
                half4 output_col = c * _Color;

                //calculate the difference between the texture color and the transparent color
                //note: we use 'dot' instead of length(transparent_diff) as its faster, and
                //although it'll really give the length squared, its good enough for our purposes!
                half3 transparent_diff = c.xyz - _TransparentColor.xyz;
                half transparent_diff_squared = dot(transparent_diff,transparent_diff);

                //if colour is too close to the transparent one, discard it.
                //note: you could do cleverer things like fade out the alpha
                if (transparent_diff_squared < _Threshold)
                    discard;

                //output albedo and alpha just like a normal shader
                o.Albedo = output_col.rgb;
                o.Alpha = output_col.a;
            }

            ENDCG
    }
        FallBack "Diffuse"
}