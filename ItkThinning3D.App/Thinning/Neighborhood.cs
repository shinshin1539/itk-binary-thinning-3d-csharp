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

    // byte[27] で返す（中心も含む）
    // 境界外は 0 扱い
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Get27(byte[] vol, int d, int h, int w, int z, int y, int x, byte[] n27)
    {
        if (vol is null) throw new ArgumentNullException(nameof(vol));
        if (n27 is null) throw new ArgumentNullException(nameof(n27));
        if (n27.Length != 27) throw new ArgumentException("n27 must be length 27");

        // 念のため：呼び出し側は常に有効座標のはず
        // （無効なら従来同様に例外で落ちるほうがデバッグしやすい）
        if ((uint)z >= (uint)d || (uint)y >= (uint)h || (uint)x >= (uint)w)
            throw new ArgumentOutOfRangeException("z/y/x out of bounds");

        // interior は既存の unsafe 高速版へ（呼び出し側で分岐していても、ここは保険＆高速）
        if ((uint)(x - 1) < (uint)(w - 2) && (uint)(y - 1) < (uint)(h - 2) && (uint)(z - 1) < (uint)(d - 2))
        {
            Get27FastInterior(vol, d, h, w, z, y, x, n27);
            return;
        }

        Get27BoundaryUnsafe(vol, d, h, w, z, y, x, n27);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void Get27BoundaryUnsafe(byte[] vol, int d, int h, int w, int z, int y, int x, byte[] n27)
    {
        int hw = h * w;
        int baseIdx = (z * h + y) * w + x;

        bool hasXm = x > 0;
        bool hasXp = x + 1 < w;

        fixed (byte* pv = vol)
        fixed (byte* pn = n27)
        {
            int k = 0;

            // dz = -1,0,1 の3平面
            for (int dz = -1; dz <= 1; dz++)
            {
                int zz = z + dz;
                if ((uint)zz >= (uint)d)
                {
                    // 9個ゼロ
                    pn[k++] = 0; pn[k++] = 0; pn[k++] = 0;
                    pn[k++] = 0; pn[k++] = 0; pn[k++] = 0;
                    pn[k++] = 0; pn[k++] = 0; pn[k++] = 0;
                    continue;
                }

                int zBase = baseIdx + dz * hw;

                // dy = -1,0,1 の3行
                for (int dy = -1; dy <= 1; dy++)
                {
                    int yy = y + dy;
                    if ((uint)yy >= (uint)h)
                    {
                        pn[k++] = 0; pn[k++] = 0; pn[k++] = 0;
                        continue;
                    }

                    int row = zBase + dy * w; // (zz,yy,x)

                    // dx = -1
                    pn[k++] = hasXm ? (byte)(pv[row - 1] != 0 ? 1 : 0) : (byte)0;
                    // dx = 0（中心列は必ずin-bounds）
                    pn[k++] = (byte)(pv[row] != 0 ? 1 : 0);
                    // dx = +1
                    pn[k++] = hasXp ? (byte)(pv[row + 1] != 0 ? 1 : 0) : (byte)0;
                }
            }
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
