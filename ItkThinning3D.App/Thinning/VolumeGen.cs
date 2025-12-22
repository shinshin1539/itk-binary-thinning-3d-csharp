using System;

namespace ItkThinning3D.App.Thinning;

public static class VolumeGen
{
    public static byte[] MakeLineX(int d, int h, int w, int z, int y, int x0, int x1)
    {
        var v = new byte[d * h * w];
        x0 = Math.Clamp(x0, 0, w - 1);
        x1 = Math.Clamp(x1, 0, w - 1);
        if (x0 > x1) (x0, x1) = (x1, x0);

        for (int x = x0; x <= x1; x++)
            v[Neighborhood.Idx(z, y, x, h, w)] = 1;

        return v;
    }

    public static byte[] MakeSolidBox(int d, int h, int w, int z0, int z1, int y0, int y1, int x0, int x1)
    {
        var v = new byte[d * h * w];
        z0 = Math.Clamp(z0, 0, d - 1); z1 = Math.Clamp(z1, 0, d - 1);
        y0 = Math.Clamp(y0, 0, h - 1); y1 = Math.Clamp(y1, 0, h - 1);
        x0 = Math.Clamp(x0, 0, w - 1); x1 = Math.Clamp(x1, 0, w - 1);
        if (z0 > z1) (z0, z1) = (z1, z0);
        if (y0 > y1) (y0, y1) = (y1, y0);
        if (x0 > x1) (x0, x1) = (x1, x0);

        for (int z = z0; z <= z1; z++)
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            v[Neighborhood.Idx(z, y, x, h, w)] = 1;

        return v;
    }
        public static byte[] MakeCross3D(int d, int h, int w, int cz, int cy, int cx, int armLen)
    {
        var v = new byte[d * h * w];

        void Set(int z, int y, int x)
        {
            if (!Neighborhood.InBounds(z, y, x, d, h, w)) return;
            v[Neighborhood.Idx(z, y, x, h, w)] = 1;
        }

        for (int t = -armLen; t <= armLen; t++)
        {
            Set(cz, cy, cx + t); // X
            Set(cz, cy + t, cx); // Y
            Set(cz + t, cy, cx); // Z
        }
        return v;
    }

    // XY平面の四角いループ（1voxel厚の枠）を z固定で作る
    public static byte[] MakeSquareLoopXY(int d, int h, int w, int z, int y0, int y1, int x0, int x1)
    {
        var v = new byte[d * h * w];
        y0 = Math.Clamp(y0, 0, h - 1);
        y1 = Math.Clamp(y1, 0, h - 1);
        x0 = Math.Clamp(x0, 0, w - 1);
        x1 = Math.Clamp(x1, 0, w - 1);
        if (y0 > y1) (y0, y1) = (y1, y0);
        if (x0 > x1) (x0, x1) = (x1, x0);

        void Set(int y, int x)
        {
            if (!Neighborhood.InBounds(z, y, x, d, h, w)) return;
            v[Neighborhood.Idx(z, y, x, h, w)] = 1;
        }

        for (int x = x0; x <= x1; x++) { Set(y0, x); Set(y1, x); }
        for (int y = y0; y <= y1; y++) { Set(y, x0); Set(y, x1); }

        return v;
    }

    // 3Dトーラス（本命のループ）。中心(cz,cy,cx)、大半径R、小半径r
    public static byte[] MakeTorus(int d, int h, int w, int cz, int cy, int cx, double R, double r)
    {
        var v = new byte[d * h * w];

        for (int z = 0; z < d; z++)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            double dz = z - cz;
            double dy = y - cy;
            double dx = x - cx;

            double rho = Math.Sqrt(dx * dx + dy * dy);   // XY半径
            double t = rho - R;

            // (sqrt(x^2+y^2)-R)^2 + z^2 <= r^2
            if (t * t + dz * dz <= r * r)
                v[Neighborhood.Idx(z, y, x, h, w)] = 1;
        }

        return v;
    }

}

