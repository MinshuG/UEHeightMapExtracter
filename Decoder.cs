using CUE4Parse.UE4.Assets.Exports.Texture;
using HeightMapExtractor;
using ImageMagick;
using ImageMagick.Formats;

public static class Decoder {
    public static MagickImage? Decode(this UTexture2D texture) => Decode(texture, texture.GetFirstMip());
    public static MagickImage? Decode(this UTexture2D texture, FTexture2DMipMap? mip) {
        if (!texture.IsVirtual && mip != null)
        {
            DecodeTexture(mip, texture.Format, texture.isNormalMap, out var readSettings, out var bytes);
            return new MagickImage(bytes, readSettings);
        }

        return null;
    }

    public static void DecodeTexture(FTexture2DMipMap mip, EPixelFormat format, bool isNormalMap, out IPixelReadSettings<ushort> readSettings, out byte[] data)
    {
        data = mip.Data.Data;
        switch (format)
        {
            case EPixelFormat.PF_B8G8R8A8:
                readSettings = new MyPixelReadSettings
                {
                    StorageType = StorageType.Char,
                    Mapping = "BGRA",
                    ReadSettings = new MagickReadSettings()
                    {
                        Format = MagickFormat.Bgra,
                        Width = mip.SizeX,
                        Height = mip.SizeY,
                        ColorSpace = ColorSpace.sRGB 
                    }
                };
                break;
            default:
                throw new NotImplementedException();
        }
    }
}