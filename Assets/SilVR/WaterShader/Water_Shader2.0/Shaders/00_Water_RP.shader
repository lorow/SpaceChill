Shader "SilVR/Water Render Plane"
{
	Properties
	{
		_MainTex ("Render Plane (Render Texture)", 2D) = "white" {}
		_CamIn("Camera In (Render Texture)", 2D) = "magenta" {}
		_imgRes("Wave Divisor", float) = 480
		_damping("Damping", Range(0,1)) = .99
		_maxLine("ObjectWave height", Range(0,1)) = 1
		_smoothing("Smoothing Factor", Range(0,1)) = 0
		_Aspect("Aspect Ratio x:z is 1:", float) = 1
		_BordVal("Border Size", float) = 1.5
		_WaterMask("Water Mask Map", 2D) = "black" {}
		_Masking("Water Mask weight", Range(0,1)) = 0
		_Power("Power", float) = 1
		_Threshold("Threshold", Range(0,1)) = 0

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			//#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D_half _MainTex;
			float4 _MainTex_ST;

			sampler2D_half _CamIn;
			sampler2D_half _WaterMask;
			fixed _imgRes;
			fixed _maxLine;
			float _damping;
			float _smoothing;
			float _Aspect;
			float _Masking;
			float _BordVal;
			float _Power;
			float _Threshold;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				//Beginning of heightmap Generation
				//Beginning of heightmap Generation

				float RAD_TWO = .7071068;

				//calculate the change in uv coords based off resolution and set max line for the rendering.
				float q = 1 / _imgRes;
				fixed ml = _maxLine;
				float uvX = i.uv.x;
				float uvY = i.uv.y;

				//The material outputs one image onto one horizontal half of a rectangle of ratio 2:1,
				//but after sampling what the previous frame was through the rendertexture that a camera
				//pointing at a plane with the shader on it and rendering it to the other half of the rectangle.
				//this way, the first half stores the previous frame, and the second half stores the frame before that.
				//Having both pieces of information allows for the shader to generate a ripple pattern via the hugo-elias algorithm.

				//Calculate variations in uv coords.	
				//Test if we are rendering to original or buffer
				fixed isSecondHalf = step(.5, uvX);

				//generate original cooridinates for when rendering to buffer
				float2 origCoords = i.uv;
				origCoords += float2(-.5, 0);

				//generate buffer coordinates for when calculating ripple in original
				float2 buffCoords = i.uv;
				buffCoords += float2(.5, 0);

				//Generates coordinates used for moving the camera to the previous frame half
				float2 camCoord = float2(uvX*2, uvY);

				//generate the border size (particularly for the positive x-edge, as the previous 
				//frame will actually bleed over if we dont set a large enough border.
				float BorderX = ((_imgRes-_BordVal) / _imgRes)*.5;
				fixed NotOnxEdge = step(i.uv.x, BorderX);

				//generate coordinates to check for previous frame levels for algorithm,
				float2 cauv = i.uv + float2(0, q);
				float2 cbuv = i.uv + float2(0, -q);
				float2 ccuv = i.uv + float2(q*.5 * _Aspect, 0);
				float2 cduv = i.uv + float2(-q*.5 * _Aspect, 0);




				//float2 cauvCorner = i.uv + float2(q*.5 * _Aspect, q);
				//float2 cbuvCorner = i.uv + float2(q*.5 * _Aspect, -q);
				//float2 ccuvCorner = i.uv + float2(-q*.5 * _Aspect, q);
				//float2 cduvCorner = i.uv + float2(-q * .5 * _Aspect, -q);




				//generate coordinates to check for previous frame levels from appended image.
				float2 camuv = camCoord + float2(0, q);
				float2 cbmuv = camCoord + float2(0, -q);
				float2 ccmuv = camCoord + float2(q * _Aspect, 0);
				float2 cdmuv = camCoord + float2(-q * _Aspect, 0);



				//float2 camuvCorner = camCoord + float2(q * _Aspect, q);
				//float2 cbmuvCorner = camCoord + float2(q * _Aspect, -q);
				//float2 ccmuvCorner = camCoord + float2(-q * _Aspect, q);
				//float2 cdmuvCorner = camCoord + float2(-q * _Aspect, -q);




				float2 cmuv1 = camCoord + float2(0, q*2);
				float2 cmuv2 = camCoord + float2(0, -q*2);
				float2 cmuv3 = camCoord + float2(q * _Aspect*2, 0);
				float2 cmuv4 = camCoord + float2(-q * _Aspect*2, 0);




				//float2 cmuv1Corner = camCoord + float2(q * _Aspect * 2, q * 2);
				//float2 cmuv2Corner = camCoord + float2(q * _Aspect * 2, -q * 2);
				//float2 cmuv3Corner = camCoord + float2(-q * _Aspect * 2, q * 2);
				//float2 cmuv4Corner = camCoord + float2(-q * _Aspect * 2, -q * 2);



				//test booleans for image masks (white will equal actual water pool for now)
				float colM = step(.02, tex2D(_WaterMask, camCoord).x);
				float colMa = step(.02, tex2D(_WaterMask, cmuv1).x);
				float colMb = step(.02, tex2D(_WaterMask, cmuv2).x);
				float colMc = step(.02, tex2D(_WaterMask, cmuv3).x);
				float colMd = step(.02, tex2D(_WaterMask, cmuv4).x);



				//float colMaCorner = step(.02, tex2D(_WaterMask, cmuv1Corner).x);
				//float colMbCorner = step(.02, tex2D(_WaterMask, cmuv2Corner).x);
				//float colMcCorner = step(.02, tex2D(_WaterMask, cmuv3Corner).x);
				//float colMdCorner = step(.02, tex2D(_WaterMask, cmuv4Corner).x);




				colM = (1 - colM);
				colMa = (1 - colMa);
				colMb = (1 - colMb);
				colMc = (1 - colMc);
				colMd = (1 - colMd);




				//colMaCorner = (1 - colMaCorner);
				//colMbCorner = (1 - colMbCorner);
				//colMcCorner = (1 - colMcCorner);
				//colMdCorner = (1 - colMdCorner);



				//sample colors for the original image
				fixed4 colO = tex2D(_CamIn, camCoord);
				fixed4 colBuff = tex2D(_MainTex, i.uv);

				fixed4 cola = tex2D(_CamIn, camuv);
				fixed4 colb = tex2D(_CamIn, cbmuv);
				fixed4 colc = tex2D(_CamIn, ccmuv);
				fixed4 cold = tex2D(_CamIn, cdmuv);




				//fixed4 colaCorner = tex2D(_CamIn, camuv);
				//fixed4 colbCorner = tex2D(_CamIn, cbmuv);
				//fixed4 colcCorner = tex2D(_CamIn, ccmuv);
				//fixed4 coldCorner = tex2D(_CamIn, cdmuv);




				//generate booleans representing whether the image was appended
				fixed4 cull = step(colO.y - colO.x - colO.z, .999);

				fixed4 culla = step(cola.y - cola.x - cola.z, .999);
				fixed4 cullb = step(colb.y - colb.x - colb.z, .999);
				fixed4 cullc = step(colc.y - colc.x - colc.z, .999);
				fixed4 culld = step(cold.y - cold.x - cold.z, .999);

				//fixed4 cull = step(.01, colO.x);

				//fixed4 culla = step(.01, cola.x);
				//fixed4 cullb = step(.01, colb.x);
				//fixed4 cullc = step(.01, colc.x);
				//fixed4 culld = step(.01, cold.x);




				//fixed4 cullaCorner = step(.01, colaCorner.x);
				//fixed4 cullbCorner = step(.01, colbCorner.x);
				//fixed4 cullcCorner = step(.01, colcCorner.x);
				//fixed4 culldCorner = step(.01, coldCorner.x);




				//sample current pixel through the buffer half of the image
				fixed4 c = (tex2D(_MainTex, buffCoords).x)*(1 - cull) + (ml, ml, ml, ml)*cull;

				//sample final colors for image after overlay was appended to the image
				fixed4 ca = tex2D(_MainTex, cauv).x *  (1 - culla) + (ml, ml, ml, ml)*culla;
				fixed4 cb = tex2D(_MainTex, cbuv).x *  (1 - cullb) + (ml, ml, ml, ml)*cullb;
				fixed4 cc = tex2D(_MainTex, ccuv).x * (1 - cullc) + (ml, ml, ml, ml)*cullc;
				fixed4 cd = tex2D(_MainTex, cduv).x *  (1 - culld) + (ml, ml, ml, ml)*culld;




				//fixed4 caCorner = tex2D(_MainTex, cauvCorner).x *  (1 - cullaCorner) + (ml, ml, ml, ml)*cullaCorner;
				//fixed4 cbCorner = tex2D(_MainTex, cbuvCorner).x *  (1 - cullbCorner) + (ml, ml, ml, ml)*cullbCorner;
				//fixed4 ccCorner = tex2D(_MainTex, ccuvCorner).x * (1 - cullcCorner) + (ml, ml, ml, ml)*cullcCorner;
				//fixed4 cdCorner = tex2D(_MainTex, cduvCorner).x *  (1 - culldCorner) + (ml, ml, ml, ml)*culldCorner;




				////actual hugo elias algorithm with modifications. Modifications included are a
				////boolean to determine whether or not the pixel is on the right most edge, and then
				////an extra value activated if the boolean is true to simulate the wrap effect that
				////causes the waves to bounce on the edge of the pool on the other corners.				
				//c = ((ca + cb + cc * NotOnxEdge+colBuff*(1-NotOnxEdge) + cd) / 2 - c)*_damping;

				//float d = (ca + cb + cc + cd) / 4;
				//c = c * (1 - _smoothing) + d * _smoothing;

				//actual hugo elias algorithm with modifications. Modifications included are a
				//boolean to determine whether or not the pixel is on the right most edge, and then
				//an extra value activated if the boolean is true to simulate the wrap effect that
				//causes the waves to bounce on the edge of the pool on the other corners.

				ca = ca * colMa + colBuff * (1 - colMa);
				cb = cb * colMb + colBuff * (1 - colMb);
				cc = cc * colMc + colBuff * (1 - colMc);
				cd = cd * colMd + colBuff * (1 - colMd);



				//caCorner = caCorner * colMaCorner + colBuff * (1 - colMaCorner);
				//cbCorner = cbCorner * colMbCorner + colBuff * (1 - colMbCorner);
				//ccCorner = ccCorner * colMcCorner + colBuff * (1 - colMcCorner);
				//cdCorner = cdCorner * colMdCorner + colBuff * (1 - colMdCorner);




				//c = ((ca + cb + cc + cd) / 2 - c)*_damping;
				
				
				c = ((ca + cb + cc * NotOnxEdge + colBuff * (1 - NotOnxEdge) + cd) / 2 - c)*_damping;
				
				//c = ((ca + cb + ccCorner*RAD_TWO + cdCorner*RAD_TWO + (cc + caCorner*RAD_TWO + cbCorner*RAD_TWO) * NotOnxEdge + colBuff * (1+2*RAD_TWO) * (1 - NotOnxEdge) + cd) / (2+2*RAD_TWO) - c)*_damping;
				
				float d = (ca + cb + cc + cd) / 4;

				c = c * (1 - _smoothing) + d * _smoothing;

				//c *= pow(c, _Power - .99);

				//c = c * step(_Threshold, c);

				//generate the color of the pixel
				fixed4 col = (c, c, c, c);

				//determine whether or not rendering to the second half, as well as if we are calculating
				//on the edge that bleeds through.
				fixed4 finalCol = col * (1 - isSecondHalf) +  tex2D(_MainTex,origCoords)*isSecondHalf;
				return finalCol;

			}
			ENDCG
		}
	}
}



