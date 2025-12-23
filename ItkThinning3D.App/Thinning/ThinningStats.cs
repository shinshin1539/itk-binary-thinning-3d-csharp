namespace ItkThinning3D.App.Thinning;

public sealed class ThinningStats
{
    public int OuterLoops;
    public int BorderPasses;
    public long CandidateChecks;
    public long CandidatesAdded;
    public long DeletedAccepted;
    public long DeletedReverted;
    public long SequentialChecks;
    public long MsTotal;
    public long MsCollect;
    public long MsSequential;
}
