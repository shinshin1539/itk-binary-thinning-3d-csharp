namespace ItkThinning3D.App.Thinning;

public static class Neighborhood
{
    // ITK互換: dx 最速, 次に dy, 最後に dz
    // dx,dy,dz は -1,0,1 のみを想定
    public static int N27Index(int dx, int dy, int dz)
    {
        if (dx is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(dx));
        if (dy is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(dy));
        if (dz is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(dz));
        return (dz + 1) * 9 + (dy + 1) * 3 + (dx + 1);
    }

    // 1D配列のボリューム添字
    public static int Idx(int z, int y, int x, int h, int w) => (z * h + y) * w + x;

    public static bool InBounds(int z, int y, int x, int d, int h, int w)
        => (uint)z < (uint)d && (uint)y < (uint)h && (uint)x < (uint)w;

    // 27近傍を 0/1 の byte[27] で返す（中心も含む）
    // 境界外は 0 扱い
    public static void Get27(byte[] vol, int d, int h, int w, int z, int y, int x, byte[] n27)
    {
        if (vol is null) throw new ArgumentNullException(nameof(vol));
        if (n27 is null) throw new ArgumentNullException(nameof(n27));
        if (n27.Length != 27) throw new ArgumentException("n27 must be length 27");

        for (int dz = -1; dz <= 1; dz++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int zi = z + dz, yi = y + dy, xi = x + dx;
            int k = N27Index(dx, dy, dz);

            byte v = 0;
            if (InBounds(zi, yi, xi, d, h, w))
            {
                v = vol[Idx(zi, yi, xi, h, w)];
                v = (byte)(v != 0 ? 1 : 0); // 念のため 0/1 に正規化
            }
            n27[k] = v;
        }
    }
    public static void Get27FastInterior(byte[] vol, int d, int h, int w, int z, int y, int x, byte[] n27)
    {
        // 前提：1 <= x <= w-2, 1 <= y <= h-2, 1 <= z <= d-2
        int hw = h * w;
        int baseIdx = (z * h + y) * w + x;

        // z-1 plane
        int p = baseIdx - hw;
        n27[0]  = vol[p - w - 1]; n27[1]  = vol[p - w]; n27[2]  = vol[p - w + 1];
        n27[3]  = vol[p - 1];     n27[4]  = vol[p];     n27[5]  = vol[p + 1];
        n27[6]  = vol[p + w - 1]; n27[7]  = vol[p + w]; n27[8]  = vol[p + w + 1];

        // z plane
        p = baseIdx;
        n27[9]  = vol[p - w - 1]; n27[10] = vol[p - w]; n27[11] = vol[p - w + 1];
        n27[12] = vol[p - 1];     n27[13] = vol[p];     n27[14] = vol[p + 1];
        n27[15] = vol[p + w - 1]; n27[16] = vol[p + w]; n27[17] = vol[p + w + 1];

        // z+1 plane
        p = baseIdx + hw;
        n27[18] = vol[p - w - 1]; n27[19] = vol[p - w]; n27[20] = vol[p - w + 1];
        n27[21] = vol[p - 1];     n27[22] = vol[p];     n27[23] = vol[p + 1];
        n27[24] = vol[p + w - 1]; n27[25] = vol[p + w]; n27[26] = vol[p + w + 1];
    }
}
