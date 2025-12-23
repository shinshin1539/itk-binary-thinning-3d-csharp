using System;
using ItkThinning3D.App.Thinning;

static int Parse(string s) => int.Parse(s);

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  gen-line  D H W  z y x0 x1  out.raw");
    Console.WriteLine("  gen-box   D H W  z0 z1 y0 y1 x0 x1  out.raw");
    Console.WriteLine("  gen-cross D H W  cz cy cx armLen  out.raw");
    Console.WriteLine("  gen-loop  D H W  z  y0 y1 x0 x1   out.raw");
    Console.WriteLine("  gen-torus D H W  cz cy cx R r     out.raw");
    Console.WriteLine("  thin      D H W  in.raw  out.raw");
    Console.WriteLine("  diff      D H W  a.raw   b.raw");
    return;
}

var cmd = args[0];

if (cmd == "gen-line")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    int z = Parse(args[4]), y = Parse(args[5]), x0 = Parse(args[6]), x1 = Parse(args[7]);
    string outPath = args[8];

    var vol = VolumeGen.MakeLineX(D, H, W, z, y, x0, x1);
    VolumeIO.WriteRawU8(outPath, vol, D, H, W);
    Console.WriteLine($"Wrote {outPath} ones={VolumeIO.CountOnes(vol)}");
    return;
}

if (cmd == "gen-box")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    int z0 = Parse(args[4]), z1 = Parse(args[5]);
    int y0 = Parse(args[6]), y1 = Parse(args[7]);
    int x0 = Parse(args[8]), x1 = Parse(args[9]);
    string outPath = args[10];

    var vol = VolumeGen.MakeSolidBox(D, H, W, z0, z1, y0, y1, x0, x1);
    VolumeIO.WriteRawU8(outPath, vol, D, H, W);
    Console.WriteLine($"Wrote {outPath} ones={VolumeIO.CountOnes(vol)}");
    return;
}

if (cmd == "gen-cross")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    int cz = Parse(args[4]), cy = Parse(args[5]), cx = Parse(args[6]);
    int arm = Parse(args[7]);
    string outPath = args[8];

    var vol = VolumeGen.MakeCross3D(D, H, W, cz, cy, cx, arm);
    VolumeIO.WriteRawU8(outPath, vol, D, H, W);
    Console.WriteLine($"Wrote {outPath} ones={VolumeIO.CountOnes(vol)}");
    return;
}

if (cmd == "gen-loop")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    int z = Parse(args[4]);
    int y0 = Parse(args[5]), y1 = Parse(args[6]);
    int x0 = Parse(args[7]), x1 = Parse(args[8]);
    string outPath = args[9];

    var vol = VolumeGen.MakeSquareLoopXY(D, H, W, z, y0, y1, x0, x1);
    VolumeIO.WriteRawU8(outPath, vol, D, H, W);
    Console.WriteLine($"Wrote {outPath} ones={VolumeIO.CountOnes(vol)}");
    return;
}

if (cmd == "gen-torus")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    int cz = Parse(args[4]), cy = Parse(args[5]), cx = Parse(args[6]);
    double R = double.Parse(args[7]);
    double r = double.Parse(args[8]);
    string outPath = args[9];

    var vol = VolumeGen.MakeTorus(D, H, W, cz, cy, cx, R, r);
    VolumeIO.WriteRawU8(outPath, vol, D, H, W);
    Console.WriteLine($"Wrote {outPath} ones={VolumeIO.CountOnes(vol)}");
    return;
}

if (cmd == "thin")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    string inPath = args[4];
    string outPath = args[5];
    bool profile = (args.Length >= 7 && args[6] == "--profile");

    var vol = VolumeIO.ReadRawU8(inPath, D, H, W);

    ThinningStats? st = profile ? new ThinningStats() : null;
    var outVol = BinaryThinning3D.ThinWithStats(vol, D, H, W, st);

    VolumeIO.WriteRawU8(outPath, outVol, D, H, W);
    Console.WriteLine($"Thinned {inPath} -> {outPath} ones {VolumeIO.CountOnes(vol)} -> {VolumeIO.CountOnes(outVol)}");

    if (st != null)
    {
        Console.WriteLine("---- profile ----");
        Console.WriteLine($"total(ms)       : {st.MsTotal}");
        Console.WriteLine($"collect(ms)     : {st.MsCollect}");
        Console.WriteLine($"sequential(ms)  : {st.MsSequential}");
        Console.WriteLine($"outer loops     : {st.OuterLoops}");
        Console.WriteLine($"border passes   : {st.BorderPasses}");
        Console.WriteLine($"candidate checks: {st.CandidateChecks}");
        Console.WriteLine($"candidates added: {st.CandidatesAdded}");
        Console.WriteLine($"deleted accepted: {st.DeletedAccepted}");
        Console.WriteLine($"deleted reverted: {st.DeletedReverted}");
        Console.WriteLine($"sequential checks: {st.SequentialChecks}");
    }
    return;
}

if (cmd == "diff")
{
    int D = Parse(args[1]), H = Parse(args[2]), W = Parse(args[3]);
    string aPath = args[4];
    string bPath = args[5];

    var a = VolumeIO.ReadRawU8(aPath, D, H, W);
    var b = VolumeIO.ReadRawU8(bPath, D, H, W);

    int diff = VolumeIO.DiffCount(a, b);
    Console.WriteLine($"Diff voxels: {diff} / {a.Length}");
    return;
}

Console.WriteLine($"Unknown command: {cmd}");
