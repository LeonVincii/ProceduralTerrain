#include "Utils.hlsl"

static const uint _kMaxColorCount = 10;

int _ColorCount;
float _Heights[_kMaxColorCount];
float4 _HeightColors[_kMaxColorCount];

void TerrainColor_float(float height, out float4 output)
{
    float height01 = saturate(Unlerp(_MinHeight, _MaxHeight, height));

    for (int i = 0; i < _ColorCount; ++i)
    {
        if (height01 <= _Heights[i])
        {
            output = _HeightColors[i];
            return;
        }
    }
}
