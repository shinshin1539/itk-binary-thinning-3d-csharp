using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace ItkThinning3D.App.Thinning;

public static class BinaryThinning3D
{
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
        var foreground = new List<int>(Math.Max(1024, VolumeIO.CountOnes(vol)));
        for (int i = 0; i < vol.Length; i++)
            if (vol[i] != 0) foreground.Add(i);
        var eulerLut = ItkLee94.CreateEulerLut();
        var candidates = new List<int>(1024);
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


                    candidates.Add(idx);
                    if (stats != null) stats.CandidatesAdded++;
                }


                swCollect.Stop();
                if (stats != null) stats.MsCollect += swCollect.ElapsedMilliseconds;

                // ---- sequential re-check ----
                var swSeq = Stopwatch.StartNew();

                bool noChange = true;

                foreach (int idx in candidates)
                {
                    if (vol[idx] == 0) continue;

                    int z = idx / hw;
                    int rem = idx - z * hw;
                    int y = rem / w;
                    int x = rem - y * w;

                    vol[idx] = 0;

                    bool interior = (x > 0 && x < w - 1 && y > 0 && y < h - 1 && z > 0 && z < d - 1);
                    if (interior) Neighborhood.Get27FastInterior(vol, d, h, w, z, y, x, n27);
                    else Neighborhood.Get27(vol, d, h, w, z, y, x, n27);
                    if (!ItkLee94.IsSimplePoint(n27))
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
