using System.Runtime.CompilerServices;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Get27FastInterior(byte[] vol, int d, int h, int w, int z, int y, int x, byte[] n27)
    {
        // 前提：1 <= x <= w-2, 1 <= y <= h-2, 1 <= z <= d-2
        // vol と n27 は null でない / n27.Length == 27 は呼び出し側で保証されている前提（ホットパス最適化）

        int hw = h * w;
        int baseIdx = (z * h + y) * w + x;

        fixed (byte* pv = vol)
        fixed (byte* pn = n27)
        {
            int p = baseIdx - hw; // z-1
            pn[0]  = pv[p - w - 1]; pn[1]  = pv[p - w]; pn[2]  = pv[p - w + 1];
            pn[3]  = pv[p - 1];     pn[4]  = pv[p];     pn[5]  = pv[p + 1];
            pn[6]  = pv[p + w - 1]; pn[7]  = pv[p + w]; pn[8]  = pv[p + w + 1];

            p = baseIdx; // z
            pn[9]  = pv[p - w - 1]; pn[10] = pv[p - w]; pn[11] = pv[p - w + 1];
            pn[12] = pv[p - 1];     pn[13] = pv[p];     pn[14] = pv[p + 1];
            pn[15] = pv[p + w - 1]; pn[16] = pv[p + w]; pn[17] = pv[p + w + 1];

            p = baseIdx + hw; // z+1
            pn[18] = pv[p - w - 1]; pn[19] = pv[p - w]; pn[20] = pv[p - w + 1];
            pn[21] = pv[p - 1];     pn[22] = pv[p];     pn[23] = pv[p + 1];
            pn[24] = pv[p + w - 1]; pn[25] = pv[p + w]; pn[26] = pv[p + w + 1];
        }
    }
}
