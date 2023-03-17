#ifndef SWF_BASE_CG_INCLUDED
#define SWF_BASE_CG_INCLUDED

//
// structs
//

struct swf_appdata_t {
	float4 vertex    : POSITION;
	uint uva         : TEXCOORD0;
};

struct swf_grab_appdata_t {
	float4 vertex    : POSITION;
	uint uva         : TEXCOORD0;
};

struct swf_mask_appdata_t {
	float4 vertex    : POSITION;
	uint uva         : TEXCOORD0;
};

struct swf_v2f_t {
	float4 vertex    : SV_POSITION;
	float2 uv        : TEXCOORD0;
	fixed a          : TEXCOORD1;
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
	// Unpack uva into u (12 bits), v (12 bits), and a (8 bits).
	OUT.uv.x      = (IN.uva & 0x00000FFF) / 4096.0;
	OUT.uv.y      = ((IN.uva & 0x00FFF000) >> 12) / 4096.0;
	OUT.a         = ((IN.uva & 0xFF000000) >> 24) / 255.0;
	return OUT;
}

inline swf_mask_v2f_t swf_mask_vert(swf_mask_appdata_t IN) {
	swf_mask_v2f_t OUT;
	OUT.vertex    = UnityObjectToClipPos(IN.vertex);
	// Unpack uva into u (12 bits), v (12 bits), and a (8 bits).
	OUT.uv.x      = (IN.uva & 0x00000FFF) / 4096.0;
	OUT.uv.y      = ((IN.uva & 0x00FFF000) >> 12) / 4096.0;
	return OUT;
}

//
// frag functions
//

inline fixed4 swf_frag(swf_v2f_t IN) : SV_Target {
	fixed4 c = tex2D(_MainTex, IN.uv);
	c = c * _Tint;
	c.a *= IN.a;
	c.rgb *= c.a;
	return c;
}

inline fixed4 swf_mask_frag(swf_mask_v2f_t IN) : SV_Target {
	fixed4 c = tex2D(_MainTex, IN.uv);
	clip(c.a - 0.01);
	return c;
}

#endif // SWF_BASE_CG_INCLUDED