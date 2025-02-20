static const uint _kMaxColorCount = 10;

float _MinHeight;
float _MaxHeight;

int _ColorCount;
float _Heights[_kMaxColorCount];
float4 _HeightColors[_kMaxColorCount];

float Unlerp(float a, float b, float x)
{
    return (x - a) / (b - a);
}

void TerrainColor_float(float height, out float4 output)
{
    float height01 = Unlerp(_MinHeight, _MaxHeight, height);

    for (int i = 0; i < _ColorCount; ++i)
    {
        if (height01 <= _Heights[i])
        {
            output = _HeightColors[i];
            return;
        }
    }
}
