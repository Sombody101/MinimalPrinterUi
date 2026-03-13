using SimpleFluidd.Services.Converters;
using Newtonsoft.Json;

namespace SimpleFluidd.Models;

[JsonConverter(typeof(NewtPolyVecConverter))]
public readonly struct PolyVec(double x, double y)
{
    public readonly double X = x;
    public readonly double Y = y;
}