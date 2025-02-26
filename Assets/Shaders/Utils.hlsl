float SaturatedUnlerp(float a, float b, float x)
{
    return saturate((x - a) / (b - a));
}
