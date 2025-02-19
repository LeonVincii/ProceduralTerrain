static const uint _kMaxColorCount = 10;

uniform int _ColorCount;
uniform float _Heights[_kMaxColorCount];
uniform float4 _HeightColors[_kMaxColorCount];

void TerrainColor_float(float4 input, out float4 output)
{
    for (int i = 0; i < _ColorCount; ++i)
    {
        if (input.a <= _Heights[i])
        {
            output = _HeightColors[i];
            return;
        }
    }
}
