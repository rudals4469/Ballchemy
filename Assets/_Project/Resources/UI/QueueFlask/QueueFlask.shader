Shader "Ballchemy/UI/QueueFlask"
{
 Properties
 {
  [PerRendererData] _MainTex ("Source Sprite", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _Mode ("0 Frame / 1 Glass", Float) = 0
  _Opacity ("Opacity", Range(0,1)) = 1
  _UVRect ("Source crop (x,y,width,height)", Vector) = (0.37109375,0.01171875,0.22265625,0.9765625)
  _StencilComp ("Stencil Comparison", Float) = 8
  _Stencil ("Stencil ID", Float) = 0
  _StencilOp ("Stencil Operation", Float) = 0
  _StencilWriteMask ("Stencil Write Mask", Float) = 255
  _StencilReadMask ("Stencil Read Mask", Float) = 255
  _ColorMask ("Color Mask", Float) = 15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off
  Lighting Off
  ZWrite Off
  ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; fixed4 color:COLOR; };
   sampler2D _MainTex;
   float4 _UVRect, _ClipRect;
   fixed4 _Color;
   float _Mode, _Opacity;
   v2f vert(appdata v)
   {
    v2f o;
    o.vertex=UnityObjectToClipPos(v.vertex);
    o.local=v.vertex;
    o.uv=_UVRect.xy+v.uv*_UVRect.zw;
    o.color=v.color*_Color;
    return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 source=tex2D(_MainTex,i.uv);
    // Work in gamma space so the key thresholds are stable in either project color space.
    float3 rgb=source.rgb;
    #ifndef UNITY_COLORSPACE_GAMMA
    rgb=LinearToGammaSpace(rgb);
    #endif
    float coverage;
    float3 ink;
    if (_Mode < 0.5)
    {
     // Brown outline/ticks survive; neutral white/gray checkerboard becomes transparent.
     coverage=smoothstep(0.035,0.14,max(rgb.r,max(rgb.g,rgb.b))-min(rgb.r,min(rgb.g,rgb.b)));
     ink=source.rgb;
    }
    else
    {
     // Only darker glass edge detail survives, not the light checkerboard or center.
     coverage=1-smoothstep(0.89,0.95,dot(rgb,float3(0.299,0.587,0.114)));
     ink=float3(1,1,1);
    }
    fixed4 result=fixed4(ink*i.color.rgb,coverage*source.a*_Opacity*i.color.a);
    #ifdef UNITY_UI_CLIP_RECT
    result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(result.a-0.001);
    #endif
    return result;
   }
   ENDCG
  }
 }
}
