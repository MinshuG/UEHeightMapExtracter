using ImageMagick;

#if Q8
using QuantumType = System.Byte;
#elif Q16
using QuantumType = System.UInt16;
#elif Q16HDRI
using QuantumType = System.Single;
#else
#endif

namespace HeightMapExtractor;

public class MyPixelReadSettings: IPixelReadSettings<ushort>
{
    public string? Mapping { get; set; }
    public StorageType StorageType { get; set; }
    public IMagickReadSettings<ushort> ReadSettings { get; set; }
}