using ItkThinning3D.App.Thinning;

namespace ItkThinning3D.Tests;

public class SmokeTests
{
    [Fact]
    public void AllZero_StaysZero()
    {
        int d=8,h=8,w=8;
        var vol = new byte[d*h*w]; // all 0
        var outVol = BinaryThinning3D.Thin(vol, d,h,w);
        Assert.Equal(vol, outVol);
    }
}
