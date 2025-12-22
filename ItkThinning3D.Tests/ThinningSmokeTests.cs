using ItkThinning3D.App.Thinning;

namespace ItkThinning3D.Tests;

public class ThinningSmokeTests
{
    [Fact]
    public void AllZero_StaysZero()
    {
        int d=10,h=10,w=10;
        var vol = new byte[d*h*w];
        var outVol = BinaryThinning3D.Thin(vol,d,h,w);
        Assert.Equal(vol, outVol);
    }

    [Fact]
    public void SingleVoxel_Stays()
    {
        int d=7,h=7,w=7;
        var vol = new byte[d*h*w];
        vol[Neighborhood.Idx(3,3,3,h,w)] = 1;

        var outVol = BinaryThinning3D.Thin(vol,d,h,w);

        Assert.Equal(1, outVol.Sum(v => v));
        Assert.Equal(1, outVol[Neighborhood.Idx(3,3,3,h,w)]);
    }

    [Fact]
    public void OneVoxelThickLine_ShouldNotChange()
    {
        int d=7,h=7,w=7;
        var vol = new byte[d*h*w];
        for (int x=1; x<=5; x++)
            vol[Neighborhood.Idx(3,3,x,h,w)] = 1;

        var outVol = BinaryThinning3D.Thin(vol,d,h,w);

        Assert.Equal(vol.Sum(v=>v), outVol.Sum(v=>v));
        Assert.Equal(vol, outVol);
    }
}
