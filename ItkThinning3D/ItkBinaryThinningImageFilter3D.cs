// Single-file port of ITK BinaryThinningImageFilter3D (Lee et al. 1994 style).
// Volume is a flat byte[] in Z-Y-X order, values 0 or 1.
// Note: requires /unsafe for the fast interior neighborhood loader.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ItkThinning3D;
public static class BinaryThinning3D
{
    readonly struct Cand
    {
        public readonly int Idx;
        public readonly int Z, Y, X;
        public Cand(int idx, int z, int y, int x) { Idx = idx; Z = z; Y = y; X = x; }
    }
    private static bool IsBorderPointLiteIdx(byte[] vol, int d, int h, int w, int idx, int z, int y, int x, ThinningDir dir)
    {
        int hw = h * w;
        return dir switch
        {
            ThinningDir.Xm => (x == 0)     ? true : vol[idx - 1] == 0,
            ThinningDir.Xp => (x == w - 1) ? true : vol[idx + 1] == 0,
            ThinningDir.Ym => (y == 0)     ? true : vol[idx - w] == 0,
            ThinningDir.Yp => (y == h - 1) ? true : vol[idx + w] == 0,
            ThinningDir.Zm => (z == 0)     ? true : vol[idx - hw] == 0,
            ThinningDir.Zp => (z == d - 1) ? true : vol[idx + hw] == 0,
            _ => false
        };
    }
    private static void IdxToZYX(int idx, int hw, int w, out int z, out int y, out int x)
    {
        z = idx / hw;
        int rem = idx - z * hw;
        y = rem / w;
        x = rem - y * w;
    }
    // 既存APIは維持：Stats不要ならこれを呼べばOK
    public static byte[] Thin(byte[] input, int d, int h, int w)
        => ThinWithStats(input, d, h, w, stats: null);

    // 追加API：Statsが欲しい時はこちら
    public static byte[] ThinWithStats(byte[] input, int d, int h, int w, ThinningStats? stats)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Length != d * h * w) throw new ArgumentException("size mismatch");

        var swTotal = Stopwatch.StartNew();

        // binary normalize
        var vol = (byte[])input.Clone();
        for (int i = 0; i < vol.Length; i++) vol[i] = (byte)(vol[i] != 0 ? 1 : 0);
        // foreground index list
        var foreground = new List<int>(1024);
        for (int i = 0; i < vol.Length; i++)
            if (vol[i] != 0) foreground.Add(i);
        var eulerLut = ItkLee94.CreateEulerLut();
        var candidates = new List<Cand>();
        var n27 = new byte[27];
        var cubeScratch = new int[26];

        int hw = h * w;

        ThinningDir BorderDir(int borderType) => borderType switch
        {
            1 => ThinningDir.Ym,
            2 => ThinningDir.Yp,
            3 => ThinningDir.Xp,
            4 => ThinningDir.Xm,
            5 => ThinningDir.Zp,
            6 => ThinningDir.Zm,
            _ => throw new ArgumentOutOfRangeException(nameof(borderType)),
        };

        int unchangedBorders = 0;
        while (unchangedBorders < 6)
        {
            if (stats != null) stats.OuterLoops++;
            unchangedBorders = 0;

            for (int currentBorder = 1; currentBorder <= 6; currentBorder++)
            {
                if (stats != null) stats.BorderPasses++;

                candidates.Clear();
                if (candidates.Capacity < foreground.Count) candidates.Capacity = foreground.Count;
                var dir = BorderDir(currentBorder);

                // ---- collect candidates ----
                var swCollect = Stopwatch.StartNew();

                foreach (int idx in foreground)
                {
                    if (vol[idx] == 0) continue;

                    IdxToZYX(idx, hw, w, out int z, out int y, out int x);
                    if (!IsBorderPointLiteIdx(vol, d, h, w, idx, z, y, x, dir)) continue;
                    bool interior = (x > 0 && x < w - 1 && y > 0 && y < h - 1 && z > 0 && z < d - 1);
                    if (interior) Neighborhood.Get27FastInterior(vol, d, h, w, z, y, x, n27);
                    else Neighborhood.Get27(vol, d, h, w, z, y, x, n27);

                    if (stats != null) stats.CandidateChecks++;

                    int numberOfNeighbors = -1;
                    for (int i = 0; i < 27; i++) if (n27[i] == 1) numberOfNeighbors++;
                    if (numberOfNeighbors == 1) continue;

                    if (!ItkLee94.IsEulerInvariant(n27, eulerLut)) continue;
                    if (!ItkLee94.IsSimplePoint(n27, cubeScratch)) continue;


                    candidates.Add(new Cand(idx, z, y, x));
                    if (stats != null) stats.CandidatesAdded++;
                }


                swCollect.Stop();
                if (stats != null) stats.MsCollect += swCollect.ElapsedMilliseconds;

                // ---- sequential re-check ----
                var swSeq = Stopwatch.StartNew();

                bool noChange = true;

                foreach (var c in candidates)
                {
                    int idx = c.Idx;
                    if (vol[idx] == 0) continue;
                    if (stats != null) stats.SequentialChecks++; 

                    int z = c.Z, y = c.Y, x = c.X;

                    vol[idx] = 0;

                    bool interior = (x > 0 && x < w - 1 && y > 0 && y < h - 1 && z > 0 && z < d - 1);
                    if (interior) Neighborhood.Get27FastInterior(vol, d, h, w, z, y, x, n27);
                    else Neighborhood.Get27(vol, d, h, w, z, y, x, n27);
                    if (!ItkLee94.IsSimplePoint(n27, cubeScratch))
                    {
                        vol[idx] = 1;
                        if (stats != null) stats.DeletedReverted++;
                    }
                    else
                    {
                        noChange = false;
                        if (stats != null) stats.DeletedAccepted++;
                    }
                }

                swSeq.Stop();
                if (stats != null) stats.MsSequential += swSeq.ElapsedMilliseconds;

                if (noChange) unchangedBorders++;
            }
        }

        swTotal.Stop();
        if (stats != null) stats.MsTotal = swTotal.ElapsedMilliseconds;

        return vol;
    }
}

public enum ThinningDir
{
    Xm = 0, // -X
    Xp = 1, // +X
    Ym = 2, // -Y
    Yp = 3, // +Y
    Zm = 4, // -Z
    Zp = 5, // +Z
}

public sealed class ThinningStats
{
    public int OuterLoops;
    public int BorderPasses;
    public long CandidateChecks;
    public long CandidatesAdded;
    public long DeletedAccepted;
    public long DeletedReverted;
    public long SequentialChecks;
    public long MsTotal;
    public long MsCollect;
    public long MsSequential;
}

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

public static class ItkLee94
{
    static readonly byte[] StartOctantLut = new byte[26]
    {
        1,1,2,1,1,2,3,3,4,1,1,2,1,2,3,3,4,5,5,6,5,5,6,7,7,8
    };
    // .hxx の fillEulerLUT をそのまま移植
    public static int[] CreateEulerLut()
    {
        var lut = new int[256]; // 未代入は 0 のまま

        lut[1] = 1;
        lut[3] = -1;
        lut[5] = -1;
        lut[7] = 1;
        lut[9] = -3;
        lut[11] = -1;
        lut[13] = -1;
        lut[15] = 1;
        lut[17] = -1;
        lut[19] = 1;
        lut[21] = 1;
        lut[23] = -1;
        lut[25] = 3;
        lut[27] = 1;
        lut[29] = 1;
        lut[31] = -1;
        lut[33] = -3;
        lut[35] = -1;
        lut[37] = 3;
        lut[39] = 1;
        lut[41] = 1;
        lut[43] = -1;
        lut[45] = 3;
        lut[47] = 1;
        lut[49] = -1;
        lut[51] = 1;

        lut[53] = 1;
        lut[55] = -1;
        lut[57] = 3;
        lut[59] = 1;
        lut[61] = 1;
        lut[63] = -1;
        lut[65] = -3;
        lut[67] = 3;
        lut[69] = -1;
        lut[71] = 1;
        lut[73] = 1;
        lut[75] = 3;
        lut[77] = -1;
        lut[79] = 1;
        lut[81] = -1;
        lut[83] = 1;
        lut[85] = 1;
        lut[87] = -1;
        lut[89] = 3;
        lut[91] = 1;
        lut[93] = 1;
        lut[95] = -1;
        lut[97] = 1;
        lut[99] = 3;
        lut[101] = 3;
        lut[103] = 1;

        lut[105] = 5;
        lut[107] = 3;
        lut[109] = 3;
        lut[111] = 1;
        lut[113] = -1;
        lut[115] = 1;
        lut[117] = 1;
        lut[119] = -1;
        lut[121] = 3;
        lut[123] = 1;
        lut[125] = 1;
        lut[127] = -1;
        lut[129] = -7;
        lut[131] = -1;
        lut[133] = -1;
        lut[135] = 1;
        lut[137] = -3;
        lut[139] = -1;
        lut[141] = -1;
        lut[143] = 1;
        lut[145] = -1;
        lut[147] = 1;
        lut[149] = 1;
        lut[151] = -1;
        lut[153] = 3;
        lut[155] = 1;

        lut[157] = 1;
        lut[159] = -1;
        lut[161] = -3;
        lut[163] = -1;
        lut[165] = 3;
        lut[167] = 1;
        lut[169] = 1;
        lut[171] = -1;
        lut[173] = 3;
        lut[175] = 1;
        lut[177] = -1;
        lut[179] = 1;
        lut[181] = 1;
        lut[183] = -1;
        lut[185] = 3;
        lut[187] = 1;
        lut[189] = 1;
        lut[191] = -1;
        lut[193] = -3;
        lut[195] = 3;
        lut[197] = -1;
        lut[199] = 1;
        lut[201] = 1;
        lut[203] = 3;
        lut[205] = -1;
        lut[207] = 1;

        lut[209] = -1;
        lut[211] = 1;
        lut[213] = 1;
        lut[215] = -1;
        lut[217] = 3;
        lut[219] = 1;
        lut[221] = 1;
        lut[223] = -1;
        lut[225] = 1;
        lut[227] = 3;
        lut[229] = 3;
        lut[231] = 1;
        lut[233] = 5;
        lut[235] = 3;
        lut[237] = 3;
        lut[239] = 1;
        lut[241] = -1;
        lut[243] = 1;
        lut[245] = 1;
        lut[247] = -1;
        lut[249] = 3;
        lut[251] = 1;
        lut[253] = 1;
        lut[255] = -1;

        return lut;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEulerInvariant(byte[] neighbors, int[] lut)
    {
        // LUTの範囲（あなたの CreateEulerLut では min=-7, max=+5）
        const int MAX_LUT = 5;
        const int MIN_LUT = -7;

        int E = 0;
        int n;

        // Octant SWU   (24,25,15,16,21,22,12)
        n = 1;
        if (neighbors[24] == 1) n |= 128;
        if (neighbors[25] == 1) n |= 64;
        if (neighbors[15] == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[21] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[12] == 1) n |= 2;
        E += lut[n];
        { int rem = 7; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SEU   (26,23,17,14,25,22,16)
        n = 1;
        if (neighbors[26] == 1) n |= 128;
        if (neighbors[23] == 1) n |= 64;
        if (neighbors[17] == 1) n |= 32;
        if (neighbors[14] == 1) n |= 16;
        if (neighbors[25] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[16] == 1) n |= 2;
        E += lut[n];
        { int rem = 6; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NWU   (18,21,9,12,19,22,10)
        n = 1;
        if (neighbors[18] == 1) n |= 128;
        if (neighbors[21] == 1) n |= 64;
        if (neighbors[9]  == 1) n |= 32;
        if (neighbors[12] == 1) n |= 16;
        if (neighbors[19] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[10] == 1) n |= 2;
        E += lut[n];
        { int rem = 5; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NEU   (20,23,19,22,11,14,10)
        n = 1;
        if (neighbors[20] == 1) n |= 128;
        if (neighbors[23] == 1) n |= 64;
        if (neighbors[19] == 1) n |= 32;
        if (neighbors[22] == 1) n |= 16;
        if (neighbors[11] == 1) n |= 8;
        if (neighbors[14] == 1) n |= 4;
        if (neighbors[10] == 1) n |= 2;
        E += lut[n];
        { int rem = 4; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SWB   (6,15,7,16,3,12,4)
        n = 1;
        if (neighbors[6]  == 1) n |= 128;
        if (neighbors[15] == 1) n |= 64;
        if (neighbors[7]  == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[3]  == 1) n |= 8;
        if (neighbors[12] == 1) n |= 4;
        if (neighbors[4]  == 1) n |= 2;
        E += lut[n];
        { int rem = 3; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SEB   (8,7,17,16,5,4,14)
        n = 1;
        if (neighbors[8]  == 1) n |= 128;
        if (neighbors[7]  == 1) n |= 64;
        if (neighbors[17] == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[5]  == 1) n |= 8;
        if (neighbors[4]  == 1) n |= 4;
        if (neighbors[14] == 1) n |= 2;
        E += lut[n];
        { int rem = 2; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NWB   (0,9,3,12,1,10,4)
        n = 1;
        if (neighbors[0]  == 1) n |= 128;
        if (neighbors[9]  == 1) n |= 64;
        if (neighbors[3]  == 1) n |= 32;
        if (neighbors[12] == 1) n |= 16;
        if (neighbors[1]  == 1) n |= 8;
        if (neighbors[10] == 1) n |= 4;
        if (neighbors[4]  == 1) n |= 2;
        E += lut[n];
        { int rem = 1; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NEB   (2,1,11,10,5,4,14)
        n = 1;
        if (neighbors[2]  == 1) n |= 128;
        if (neighbors[1]  == 1) n |= 64;
        if (neighbors[11] == 1) n |= 32;
        if (neighbors[10] == 1) n |= 16;
        if (neighbors[5]  == 1) n |= 8;
        if (neighbors[4]  == 1) n |= 4;
        if (neighbors[14] == 1) n |= 2;
        E += lut[n];

        return E == 0;
    }

    // .hxx の isSimplePoint をそのまま移植（switchで開始octant決定）
    public static bool IsSimplePoint(byte[] neighbors)
    {
        var cube = new int[26];

        for (int i = 0; i < 13; i++) cube[i] = neighbors[i];
        for (int i = 14; i < 27; i++) cube[i - 1] = neighbors[i]; // center(13)は除外

        int label = 2;

        for (int i = 0; i < 26; i++)
        {
            if (cube[i] != 1) continue;

            int startOctant = StartOctantLut[i];

            OctreeLabeling(startOctant, label, cube);

            label++;
            if (label - 2 >= 2) return false;
        }

        return true;
    }
    public static bool IsSimplePoint(byte[] neighbors, int[] cubeScratch)
    {
        // cubeScratch.Length == 26 が前提
        for (int i = 0; i < 13; i++) cubeScratch[i] = neighbors[i];
        for (int i = 14; i < 27; i++) cubeScratch[i - 1] = neighbors[i];

        int label = 2;

        for (int i = 0; i < 26; i++)
        {
            if (cubeScratch[i] != 1) continue;

            int startOctant = i switch
            {
                0 or 1 or 3 or 4 or 9 or 10 or 12 => 1,
                2 or 5 or 11 or 13 => 2,
                6 or 7 or 14 or 15 => 3,
                8 or 16 => 4,
                17 or 18 or 20 or 21 => 5,
                19 or 22 => 6,
                23 or 24 => 7,
                25 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };

            OctreeLabeling(startOctant, label, cubeScratch); // ★ int[] 版を呼ぶ

            label++;
            if (label - 2 >= 2) return false;
        }
        return true;
    }

    // .hxx の Octree_labeling をそのまま移植（条件も再帰呼び出しも一致）
    static void OctreeLabeling(int octant, int label, int[] cube)
    {
        // 再帰 → 明示スタック（同一処理）
        Span<int> stack = stackalloc int[256];
        int sp = 0;
        stack[sp++] = octant;

        while (sp > 0)
        {
            int o = stack[--sp];

            if (o == 1)
            {
                if (cube[0] == 1) cube[0] = label;
                if (cube[1] == 1) { cube[1] = label; stack[sp++] = 2; }
                if (cube[3] == 1) { cube[3] = label; stack[sp++] = 3; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 2; stack[sp++] = 3; stack[sp++] = 4;
                }
                if (cube[9] == 1) { cube[9] = label; stack[sp++] = 5; }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 2; stack[sp++] = 5; stack[sp++] = 6;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 3; stack[sp++] = 5; stack[sp++] = 7;
                }
            }
            else if (o == 2)
            {
                if (cube[1] == 1) { cube[1] = label; stack[sp++] = 1; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 4;
                }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 5; stack[sp++] = 6;
                }
                if (cube[2] == 1) cube[2] = label;
                if (cube[5] == 1) { cube[5] = label; stack[sp++] = 4; }
                if (cube[11] == 1) { cube[11] = label; stack[sp++] = 6; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 4; stack[sp++] = 6; stack[sp++] = 8;
                }
            }
            else if (o == 3)
            {
                if (cube[3] == 1) { cube[3] = label; stack[sp++] = 1; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 4;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 5; stack[sp++] = 7;
                }
                if (cube[6] == 1) cube[6] = label;
                if (cube[7] == 1) { cube[7] = label; stack[sp++] = 4; }
                if (cube[14] == 1) { cube[14] = label; stack[sp++] = 7; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 4; stack[sp++] = 7; stack[sp++] = 8;
                }
            }
            else if (o == 4)
            {
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 3;
                }
                if (cube[5] == 1) { cube[5] = label; stack[sp++] = 2; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 6; stack[sp++] = 8;
                }
                if (cube[7] == 1) { cube[7] = label; stack[sp++] = 3; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 7; stack[sp++] = 8;
                }
                if (cube[8] == 1) cube[8] = label;
                if (cube[16] == 1) { cube[16] = label; stack[sp++] = 8; }
            }
            else if (o == 5)
            {
                if (cube[9] == 1) { cube[9] = label; stack[sp++] = 1; }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 6;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 7;
                }
                if (cube[17] == 1) cube[17] = label;
                if (cube[18] == 1) { cube[18] = label; stack[sp++] = 6; }
                if (cube[20] == 1) { cube[20] = label; stack[sp++] = 7; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 6; stack[sp++] = 7; stack[sp++] = 8;
                }
            }
            else if (o == 6)
            {
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 5;
                }
                if (cube[11] == 1) { cube[11] = label; stack[sp++] = 2; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 4; stack[sp++] = 8;
                }
                if (cube[18] == 1) { cube[18] = label; stack[sp++] = 5; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 7; stack[sp++] = 8;
                }
                if (cube[19] == 1) cube[19] = label;
                if (cube[22] == 1) { cube[22] = label; stack[sp++] = 8; }
            }
            else if (o == 7)
            {
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 5;
                }
                if (cube[14] == 1) { cube[14] = label; stack[sp++] = 3; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 4; stack[sp++] = 8;
                }
                if (cube[20] == 1) { cube[20] = label; stack[sp++] = 5; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 6; stack[sp++] = 8;
                }
                if (cube[23] == 1) cube[23] = label;
                if (cube[24] == 1) { cube[24] = label; stack[sp++] = 8; }
            }
            else if (o == 8)
            {
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 4; stack[sp++] = 6;
                }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 4; stack[sp++] = 7;
                }
                if (cube[16] == 1) { cube[16] = label; stack[sp++] = 4; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 6; stack[sp++] = 7;
                }
                if (cube[22] == 1) { cube[22] = label; stack[sp++] = 6; }
                if (cube[24] == 1) { cube[24] = label; stack[sp++] = 7; }
                if (cube[25] == 1) cube[25] = label;
            }
        }
    }
}
