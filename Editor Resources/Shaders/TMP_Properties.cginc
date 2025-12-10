// UI Editable properties
uniform fixed4		_FaceColor;					// RGBA : Color + Opacity
uniform float		_FaceDilate;				// v[ 0, 1]
uniform float		_OutlineSoftness;			// v[ 0, 1]

uniform fixed4		_OutlineColor;				// RGBA : Color + Opacity
uniform float		_OutlineWidth;				// v[ 0, 1]

uniform fixed4		_UnderlayColor;				// RGBA : Color + Opacity
uniform float		_UnderlayOffsetX;			// v[-1, 1]
uniform float		_UnderlayOffsetY;			// v[-1, 1]
uniform float		_UnderlayDilate;			// v[-1, 1]
uniform float		_UnderlaySoftness;			// v[ 0, 1]

// API Editable properties
uniform float		_WeightNormal;
uniform float		_WeightBold;

uniform float		_ScaleRatioA;
uniform float		_ScaleRatioC;

uniform float		_VertexOffsetX;
uniform float		_VertexOffsetY;

uniform float4		_ClipRect;	// bottom left(x,y) : top right(z,w)

uniform float		_MaskSoftnessX;
uniform float		_MaskSoftnessY;

// Font Atlas properties
uniform sampler2D	_MainTex;
uniform float		_TextureWidth;
uniform float		_TextureHeight;
uniform float 		_GradientScale;
uniform float		_ScaleX;
uniform float		_ScaleY;
uniform float		_PerspectiveFilter;
uniform float		_Sharpness;
