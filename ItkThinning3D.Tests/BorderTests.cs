using ItkThinning3D.App.Thinning;

namespace ItkThinning3D.Tests;

public class BorderTests
{
    [Fact]
    public void SingleVoxel_IsBorderInAllDirections()
    {
        int D=5,H=5,W=5;
        var vol = new byte[D*H*W];
        vol[Neighborhood.Idx(2,2,2,H,W)] = 1;

        foreach (ThinningDir dir in Enum.GetValues(typeof(ThinningDir)))
        {
            Assert.True(Border.IsBorderPoint(vol, D,H,W, 2,2,2, dir), $"dir={dir}");
        }
    }

    [Fact]
    public void TwoVoxelsAlongX_InternalFaceIsNotBorder()
    {
        int D=5,H=5,W=5;
        var vol = new byte[D*H*W];
        vol[Neighborhood.Idx(2,2,2,H,W)] = 1; // left
        vol[Neighborhood.Idx(2,2,3,H,W)] = 1; // right

        // 左ボクセルの +X 方向は前景があるので border ではない
        Assert.False(Border.IsBorderPoint(vol, D,H,W, 2,2,2, ThinningDir.Xp));
        // 右ボクセルの -X 方向も同様
        Assert.False(Border.IsBorderPoint(vol, D,H,W, 2,2,3, ThinningDir.Xm));

        // 端面は border
        Assert.True(Border.IsBorderPoint(vol, D,H,W, 2,2,2, ThinningDir.Xm));
        Assert.True(Border.IsBorderPoint(vol, D,H,W, 2,2,3, ThinningDir.Xp));
    }
}
