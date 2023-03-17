#ifndef SWF_BASE_CG_INCLUDED
#define SWF_BASE_CG_INCLUDED

//
// structs
//

struct swf_appdata_t {
	float4 vertex    : POSITION;
	float3 uv        : TEXCOORD0;
};

struct swf_grab_appdata_t {
	float4 vertex    : POSITION;
	float3 uv        : TEXCOORD0;
};

struct swf_mask_appdata_t {
	float4 vertex    : POSITION;
	float2 uv        : TEXCOORD0;
};

struct swf_v2f_t {
	float4 vertex    : SV_POSITION;
	float3 uv        : TEXCOORD0;
};

struct swf_mask_v2f_t {
	float4 vertex    : SV_POSITION;
	float2 uv        : TEXCOORD0;
};

//
// vert functions
//

inline swf_v2f_t swf_vert(swf_appdata_t IN) {
	swf_v2f_t OUT;
	OUT.vertex    = UnityObjectToClipPos(IN.vertex);
	OUT.uv        = IN.uv;
	return OUT;
}

inline swf_mask_v2f_t swf_mask_vert(swf_mask_appdata_t IN) {
	swf_mask_v2f_t OUT;
	OUT.vertex    = UnityObjectToClipPos(IN.vertex);
	OUT.uv        = IN.uv;
	return OUT;
}

//
// frag functions
//

inline fixed4 swf_frag(swf_v2f_t IN) : SV_Target {
	fixed4 c = tex2D(_MainTex, IN.uv);
	c = c * _Tint;
	c.a *= IN.uv.z;
	c.rgb *= c.a;
	return c;
}

inline fixed4 swf_mask_frag(swf_mask_v2f_t IN) : SV_Target {
	fixed4 c = tex2D(_MainTex, IN.uv);
	clip(c.a - 0.01);
	return c;
}

#endif // SWF_BASE_CG_INCLUDED