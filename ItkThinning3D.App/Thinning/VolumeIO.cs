using System;
using System.IO;

namespace ItkThinning3D.App.Thinning;

public static class VolumeIO
{
    public static byte[] ReadRawU8(string path, int d, int h, int w)
    {
        int n = checked(d * h * w);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != n)
            throw new InvalidDataException($"Size mismatch: file={bytes.Length} bytes, expected={n} bytes");
        return bytes;
    }

    public static void WriteRawU8(string path, byte[] vol, int d, int h, int w)
    {
        int n = checked(d * h * w);
        if (vol.Length != n) throw new ArgumentException("size mismatch", nameof(vol));
        File.WriteAllBytes(path, vol);
    }

    public static int CountOnes(byte[] vol)
    {
        int c = 0;
        for (int i = 0; i < vol.Length; i++) if (vol[i] != 0) c++;
        return c;
    }

    public static int DiffCount(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("length mismatch");
        int c = 0;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) c++;
        return c;
    }
}
