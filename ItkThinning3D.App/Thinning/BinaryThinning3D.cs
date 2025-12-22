using System;
using System.Collections.Generic;

namespace ItkThinning3D.App.Thinning;

public static class BinaryThinning3D
{
    public static byte[] Thin(byte[] input, int d, int h, int w)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Length != d * h * w) throw new ArgumentException("size mismatch");

        // binary normalize
        var vol = (byte[])input.Clone();
        for (int i = 0; i < vol.Length; i++) vol[i] = (byte)(vol[i] != 0 ? 1 : 0);

        var eulerLut = ItkLee94.CreateEulerLut();
        var candidates = new List<int>(1024);
        var n27 = new byte[27];

        int hw = h * w;

        ThinningDir BorderDir(int borderType) => borderType switch
        {
            1 => ThinningDir.Ym, // N (0,-1,0)
            2 => ThinningDir.Yp, // S (0,+1,0)
            3 => ThinningDir.Xp, // E (+1,0,0)
            4 => ThinningDir.Xm, // W (-1,0,0)
            5 => ThinningDir.Zp, // U (0,0,+1)
            6 => ThinningDir.Zm, // B (0,0,-1)
            _ => throw new ArgumentOutOfRangeException(nameof(borderType)),
        };

        int unchangedBorders = 0;
        while (unchangedBorders < 6)
        {
            unchangedBorders = 0;

            for (int currentBorder = 1; currentBorder <= 6; currentBorder++)
            {
                candidates.Clear();
                var dir = BorderDir(currentBorder);

                // 1) parallel候補収集（ただしここでは消さない）
                for (int z = 0; z < d; z++)
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = Neighborhood.Idx(z, y, x, h, w);
                    if (vol[idx] == 0) continue;

                    // border点でなければスキップ
                    if (!Border.IsBorderPoint(vol, d, h, w, z, y, x, dir)) continue;

                    Neighborhood.Get27(vol, d, h, w, z, y, x, n27);

                    // 端点（1近傍）は消さない
                    int numberOfNeighbors = -1;
                    for (int i = 0; i < 27; i++)
                        if (n27[i] == 1) numberOfNeighbors++;
                    if (numberOfNeighbors == 1) continue;

                    // Euler invariant
                    if (!ItkLee94.IsEulerInvariant(n27, eulerLut)) continue;

                    // Simple point
                    if (!ItkLee94.IsSimplePoint(n27)) continue;

                    candidates.Add(idx);
                }

                // 2) sequential re-check（消してから再判定→ダメなら戻す）
                bool noChange = true;

                foreach (int idx in candidates)
                {
                    if (vol[idx] == 0) continue;

                    int z = idx / hw;
                    int rem = idx - z * hw;
                    int y = rem / w;
                    int x = rem - y * w;

                    vol[idx] = 0;

                    Neighborhood.Get27(vol, d, h, w, z, y, x, n27);
                    if (!ItkLee94.IsSimplePoint(n27))
                    {
                        vol[idx] = 1; // つながり壊すので戻す
                    }
                    else
                    {
                        noChange = false;
                    }
                }

                if (noChange) unchangedBorders++;
            }
        }

        return vol;
    }
}
