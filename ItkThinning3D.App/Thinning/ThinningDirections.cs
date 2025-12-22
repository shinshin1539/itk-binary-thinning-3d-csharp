namespace ItkThinning3D.App.Thinning;

public enum ThinningDir
{
    Xm = 0, // -X
    Xp = 1, // +X
    Ym = 2, // -Y
    Yp = 3, // +Y
    Zm = 4, // -Z
    Zp = 5, // +Z
}

public static class ThinningDirections
{
    public static (int dz, int dy, int dx) Offset(ThinningDir dir) => dir switch
    {
        ThinningDir.Xm => (0, 0, -1),
        ThinningDir.Xp => (0, 0,  1),
        ThinningDir.Ym => (0, -1, 0),
        ThinningDir.Yp => (0,  1, 0),
        ThinningDir.Zm => (-1, 0, 0),
        ThinningDir.Zp => ( 1, 0, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(dir)),
    };
}
