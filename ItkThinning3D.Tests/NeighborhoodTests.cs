using ItkThinning3D.App.Thinning;

namespace ItkThinning3D.Tests;

public class NeighborhoodTests
{
    [Fact]
    public void CenterIndex_Is13()
    {
        Assert.Equal(13, Neighborhood.N27Index(0, 0, 0));
    }

    [Fact]
    public void DxIsFastest_OrderCheck()
    {
        // dz=-1, dy=-1 の面で dx が -1,0,1 と並ぶことを確認
        int a = Neighborhood.N27Index(-1, -1, -1);
        int b = Neighborhood.N27Index( 0, -1, -1);
        int c = Neighborhood.N27Index( 1, -1, -1);
        Assert.Equal(a + 1, b);
        Assert.Equal(b + 1, c);

        // dy が 1 増えると +3
        int d = Neighborhood.N27Index(-1, 0, -1);
        Assert.Equal(a + 3, d);

        // dz が 1 増えると +9
        int e = Neighborhood.N27Index(-1, -1, 0);
        Assert.Equal(a + 9, e);
    }

    [Fact]
    public void Get27_PicksCenterCorrectly()
    {
        int D = 5, H = 5, W = 5;
        var vol = new byte[D * H * W];

        // 中心(2,2,2)だけ1
        vol[Neighborhood.Idx(2, 2, 2, H, W)] = 1;

        var n27 = new byte[27];
        Neighborhood.Get27(vol, D, H, W, 2, 2, 2, n27);

        Assert.Equal(1, n27[13]); // center
        Assert.Equal(1, n27.Sum(x => x)); // 他は全部0
    }
}
