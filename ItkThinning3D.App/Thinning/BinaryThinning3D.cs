namespace ItkThinning3D.App.Thinning;

public static class BinaryThinning3D
{
    // まずは「動く箱」。中身はこれから埋める。
    public static byte[] Thin(byte[] input, int d, int h, int w)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Length != d * h * w) throw new ArgumentException("size mismatch");
        // TODO: ここに ITK 相当の thinning を実装
        return (byte[])input.Clone();
    }

    public static int Idx(int z, int y, int x, int h, int w) => (z * h + y) * w + x;
    public static bool InBounds(int z, int y, int x, int d, int h, int w)
        => (uint)z < (uint)d && (uint)y < (uint)h && (uint)x < (uint)w;
}
