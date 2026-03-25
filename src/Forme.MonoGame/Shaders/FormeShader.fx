// Forme shader for the Slug font rendering algorithm.
// Copyright (c) 2026 Christopher Whitley (AristurtleDev)
//
// Derived from the Slug reference shaders by Eric Lengyel.
// Copyright (c) 2017 Eric Lengyel (Terathon Software)
//
// Originally dual-licensed under MIT OR Apache-2.0.
// This derivative work is distributed under the MIT License.
//
// See LICENSE and THIRD_PARTY_NOTICES for full license text.
//
// Band texture format: RG32F - each texel stores two float values encoding unsigned 16-bit
// integers. This avoids integer texture requirements while preserving full u16 precision.
//
// Per-vertex input uses 4 float4 attributes (POSITION0 + TEXCOORD0-2):
//   POSITION0: pos.xy = object-space position, pos.zw = outward corner normal
//   TEXCOORD0: tex.xy = em-space sample coords, tex.z = packed band tex location, tex.w = band count
//   TEXCOORD1: color.xyzw = normalized RGBA (R/255, G/255, B/255, A/255)
//   TEXCOORD2: dil = (lutU, lutV, invJxx, invJyy)
//
// This shader targets SM3 for OpenGL and SM4 for DirectX 11. No integer bitwise ops are used
// so that the shader compiles cleanly at SM3 where they are unavailable.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

float4x4 forme_matrix;     // MVP matrix (orthographic)

texture2D curveTexture;
sampler2D curveSampler = sampler_state
{
    Texture   = <curveTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

texture2D bandTexture;
sampler2D bandSampler = sampler_state
{
    Texture   = <bandTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

texture2D bandLUTTexture;
sampler2D bandLUTSampler = sampler_state
{
    Texture   = <bandLUTTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

float2 curveTexSize;   // (width, height) in texels
float2 bandTexSize;    // (width, height) in texels

struct VSInput
{
    float4 pos   : POSITION0;  // xy = position, zw = outward corner normal
    float4 tex   : TEXCOORD0;  // xy = em-space coords, z = packed band tex location, w = band count
    float4 color : TEXCOORD1;  // normalized RGBA color
    float4 dil   : TEXCOORD2;  // x=lutU, y=lutV, z=invJxx, w=invJyy
};

struct VSOutput
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;  // em-space sample coords (dilated)
    float4 glyphLoc : TEXCOORD1;  // xy=packed band tex loc + band count, zw=band LUT UV
    float4 color    : TEXCOORD2;  // RGBA color
};

VSOutput VS_Main(VSInput input)
{
    VSOutput output;

    float2 n            = input.pos.zw;
    float2 screenOffset = n * 0.5;

    output.position = mul(float4(input.pos.xy + screenOffset, 0, 1), forme_matrix);
    output.texcoord = input.tex.xy + screenOffset * float2(input.dil.z, input.dil.w);
    output.glyphLoc = float4(input.tex.z, input.tex.w, input.dil.x, input.dil.y);
    output.color    = input.color;

    return output;
}

float2 FetchBandTexel(float2 bTexSize, float absTexelIndex)
{
    float tx = fmod(absTexelIndex, bTexSize.x);
    float ty = floor(absTexelIndex / bTexSize.x);
    float2 uv = (float2(tx, ty) + 0.5) / bTexSize;
    return tex2Dlod(bandSampler, float4(uv, 0, 0)).rg;
}

float4 FetchCurveTexel(float2 cTexSize, float2 curveLoc)
{
    float2 uv = (curveLoc + 0.5) / cTexSize;
    return tex2Dlod(curveSampler, float4(uv, 0, 0));
}

// Calculate the root eligibility for a sample-relative quadratic Bezier curve from the
// signs of the y coordinates of the three control points. Returns float2 where x indicates
// whether the first root contributes and y indicates whether the second root contributes.
// This is the float-arithmetic equivalent of CalcRootCode from the Slug reference shader,
// required because SM3 does not support integer bitwise operations.
float2 CalcRootEligibility(float y1, float y2, float y3)
{
    float s0 = (y1 > 0.0) ? 1.0 : 0.0;
    float s1 = (y2 > 0.0) ? 1.0 : 0.0;
    float s2 = (y3 > 0.0) ? 1.0 : 0.0;
    float ns0 = 1.0 - s0, ns1 = 1.0 - s1, ns2 = 1.0 - s2;

    float root1 = saturate(
        s0 * ns1 * ns2 +
        ns0 * s1 * ns2 +
        s0 * s1 * ns2  +
        s0 * ns1 * s2);

    float root2 = saturate(
        ns0 * s1 * ns2 +
        ns0 * ns1 * s2 +
        s0 * ns1 * s2  +
        ns0 * s1 * s2);

    return float2(root1, root2);
}

// Solve for the x coordinates where the quadratic Bezier C(t) crosses y = 0.
// The quadratic polynomial in t is given by
//
//     a t^2 - 2b t + c,
//
// where a = p1.y - 2 p2.y + p3.y, b = p1.y - p2.y, and c = p1.y.
// If the polynomial is nearly linear, solve -2b t + c = 0 instead.
float2 SolveHorizPoly(float4 p12, float2 p3)
{
    float2 a = p12.xy - p12.zw * 2.0 + p3;
    float2 b = p12.xy - p12.zw;
    float ra = 1.0 / a.y;
    float rb = 0.5 / b.y;
    float d = sqrt(max(b.y * b.y - a.y * p12.y, 0.0));
    float t1 = (b.y - d) * ra;
    float t2 = (b.y + d) * ra;

    // NOTE:
    //      The Slug reference uses 1.0 / 65536.0 as the near-linear epsilon,
    //      but this is too tight for MojoShader transpiled GLSL.
    //      Curves with a.y near zero but above that threshold
    //      reach ra = 1.0 / a.y, producing extreme t values and horizontal
    //      banding artifacts.
    //      A wider epsilon of 0.0001 prevents the near-division-by-zero.
    //      If someone has a better suggestion, please try it.
    if (abs(a.y) < 0.0001) { t1 = p12.y * rb; t2 = t1; }

    return float2(
        (a.x * t1 - b.x * 2.0) * t1 + p12.x,
        (a.x * t2 - b.x * 2.0) * t2 + p12.x);
}

float FormeRender(float2 renderCoord, float4 bandTransform, float4 glyphTexInfo)
{
    float glyTexPackedXY = glyphTexInfo.x;
    float glyphBandTexX  = fmod(glyTexPackedXY, bandTexSize.x);
    float glyphBandTexY  = floor(glyTexPackedXY / bandTexSize.x);
    float bandCount      = glyphTexInfo.y;
    float bandMaxX       = bandCount - 1.0;
    float bandMaxY       = bandCount - 1.0;

    // The effective pixel dimensions of the em square are computed
    // independently for x and y directions with texcoord derivatives.
    float2 emsPerPixel = fwidth(renderCoord);
    float2 pixelsPerEm = 1.0 / max(emsPerPixel, float2(0.0001, 0.0001));

    // Determine what bands the current pixel lies in by applying a scale and offset
    // to the render coordinates. Band indexes are clamped to [0, bandMax.xy].
    float2 bandPos   = renderCoord * bandTransform.xy + bandTransform.zw;
    float bandIndexY = clamp(floor(bandPos.y), 0.0, bandMaxY);
    float bandIndexX = clamp(floor(bandPos.x), 0.0, bandMaxX);

    float glyphBaseTexel = glyphBandTexY * bandTexSize.x + glyphBandTexX;

    // Loop over all curves in the horizontal band.
    float xcov = 0.0;
    float xwgt = 0.0;

    float2 hBandHeader  = FetchBandTexel(bandTexSize, glyphBaseTexel + bandIndexY);
    float  hCurveCount  = hBandHeader.r;
    float  hCurveOffset = hBandHeader.g;

    [loop]
    for (float ci = 0.0; ci < hCurveCount; ci += 1.0)
    {
        float2 curveLoc = FetchBandTexel(bandTexSize, hCurveOffset + ci);
        float4 p12 = FetchCurveTexel(curveTexSize, curveLoc) - float4(renderCoord, renderCoord);
        float2 p3  = FetchCurveTexel(curveTexSize, float2(curveLoc.x + 1.0, curveLoc.y)).xy - renderCoord;

        // Curves are sorted in descending order by max x coordinate.
        if (max(max(p12.x, p12.z), p3.x) * pixelsPerEm.x < -0.5) break;

        float2 elig = CalcRootEligibility(p12.y, p12.w, p3.y);
        if (elig.x + elig.y > 0.0)
        {
            float2 r = SolveHorizPoly(p12, p3) * pixelsPerEm.x;

            if (elig.x > 0.5) { xcov += saturate(r.x + 0.5); xwgt = max(xwgt, saturate(1.0 - abs(r.x) * 2.0)); }
            if (elig.y > 0.5) { xcov -= saturate(r.y + 0.5); xwgt = max(xwgt, saturate(1.0 - abs(r.y) * 2.0)); }
        }
    }

    // Loop over all curves in the vertical band. Swap x and y to reuse the
    // horizontal solver and eligibility logic.
    float ycov = 0.0;
    float ywgt = 0.0;

    float2 vBandHeader  = FetchBandTexel(bandTexSize, glyphBaseTexel + bandCount + bandIndexX);
    float  vCurveCount  = vBandHeader.r;
    float  vCurveOffset = vBandHeader.g;

    [loop]
    for (float vi = 0.0; vi < vCurveCount; vi += 1.0)
    {
        float2 curveLoc = FetchBandTexel(bandTexSize, vCurveOffset + vi);
        float4 rawP12 = FetchCurveTexel(curveTexSize, curveLoc);
        float2 rawP3  = FetchCurveTexel(curveTexSize, float2(curveLoc.x + 1.0, curveLoc.y)).xy;

        float4 p12 = float4(rawP12.y - renderCoord.y, rawP12.x - renderCoord.x,
                             rawP12.w - renderCoord.y, rawP12.z - renderCoord.x);
        float2 p3  = float2(rawP3.y - renderCoord.y, rawP3.x - renderCoord.x);

        // Curves are sorted in descending order by max y coordinate.
        if (max(max(p12.x, p12.z), p3.x) * pixelsPerEm.y < -0.5) break;

        float2 elig = CalcRootEligibility(p12.y, p12.w, p3.y);
        if (elig.x + elig.y > 0.0)
        {
            float2 r = SolveHorizPoly(p12, p3) * pixelsPerEm.y;

            if (elig.x > 0.5) { ycov += saturate(r.x + 0.5); ywgt = max(ywgt, saturate(1.0 - abs(r.x) * 2.0)); }
            if (elig.y > 0.5) { ycov -= saturate(r.y + 0.5); ywgt = max(ywgt, saturate(1.0 - abs(r.y) * 2.0)); }
        }
    }

    float coverage = max(
        abs(xcov * xwgt + ycov * ywgt) / max(xwgt + ywgt, 0.0001),
        min(abs(xcov), abs(ycov)));

    return sqrt(saturate(coverage));
}

float4 PS_Main(VSOutput input) : COLOR0
{
    float4 bandTransform = tex2D(bandLUTSampler, input.glyphLoc.zw);
    float  coverage      = FormeRender(input.texcoord, bandTransform, input.glyphLoc);
    return float4(input.color.rgb * coverage, coverage * input.color.a);
}

technique FormeTechnique
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VS_Main();
        PixelShader  = compile PS_SHADERMODEL PS_Main();
    }
};
