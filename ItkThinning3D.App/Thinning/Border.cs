namespace ItkThinning3D.App.Thinning;

public static class Border
{
    // vol[z,y,x] が前景(1)で、dir方向の隣が背景(0)なら true
    // 境界外は 0 扱い
    public static bool IsBorderPoint(byte[] vol, int d, int h, int w, int z, int y, int x, ThinningDir dir)
    {
        int idx = Neighborhood.Idx(z, y, x, h, w);
        if (vol[idx] == 0) return false; // 背景は対象外

        var (dz, dy, dx) = ThinningDirections.Offset(dir);
        int zn = z + dz, yn = y + dy, xn = x + dx;

        if (!Neighborhood.InBounds(zn, yn, xn, d, h, w))
            return true; // 外は背景扱い → 表面

        return vol[Neighborhood.Idx(zn, yn, xn, h, w)] == 0;
    }
}
