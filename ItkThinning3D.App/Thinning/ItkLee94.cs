using System;
using System.Runtime.CompilerServices;
namespace ItkThinning3D.App.Thinning;

public static class ItkLee94
{
    static readonly byte[] StartOctantLut = new byte[26]
    {
        1,1,2,1,1,2,3,3,4,1,1,2,1,2,3,3,4,5,5,6,5,5,6,7,7,8
    };
    // .hxx の fillEulerLUT をそのまま移植
    public static int[] CreateEulerLut()
    {
        var lut = new int[256]; // 未代入は 0 のまま

        lut[1] = 1;
        lut[3] = -1;
        lut[5] = -1;
        lut[7] = 1;
        lut[9] = -3;
        lut[11] = -1;
        lut[13] = -1;
        lut[15] = 1;
        lut[17] = -1;
        lut[19] = 1;
        lut[21] = 1;
        lut[23] = -1;
        lut[25] = 3;
        lut[27] = 1;
        lut[29] = 1;
        lut[31] = -1;
        lut[33] = -3;
        lut[35] = -1;
        lut[37] = 3;
        lut[39] = 1;
        lut[41] = 1;
        lut[43] = -1;
        lut[45] = 3;
        lut[47] = 1;
        lut[49] = -1;
        lut[51] = 1;

        lut[53] = 1;
        lut[55] = -1;
        lut[57] = 3;
        lut[59] = 1;
        lut[61] = 1;
        lut[63] = -1;
        lut[65] = -3;
        lut[67] = 3;
        lut[69] = -1;
        lut[71] = 1;
        lut[73] = 1;
        lut[75] = 3;
        lut[77] = -1;
        lut[79] = 1;
        lut[81] = -1;
        lut[83] = 1;
        lut[85] = 1;
        lut[87] = -1;
        lut[89] = 3;
        lut[91] = 1;
        lut[93] = 1;
        lut[95] = -1;
        lut[97] = 1;
        lut[99] = 3;
        lut[101] = 3;
        lut[103] = 1;

        lut[105] = 5;
        lut[107] = 3;
        lut[109] = 3;
        lut[111] = 1;
        lut[113] = -1;
        lut[115] = 1;
        lut[117] = 1;
        lut[119] = -1;
        lut[121] = 3;
        lut[123] = 1;
        lut[125] = 1;
        lut[127] = -1;
        lut[129] = -7;
        lut[131] = -1;
        lut[133] = -1;
        lut[135] = 1;
        lut[137] = -3;
        lut[139] = -1;
        lut[141] = -1;
        lut[143] = 1;
        lut[145] = -1;
        lut[147] = 1;
        lut[149] = 1;
        lut[151] = -1;
        lut[153] = 3;
        lut[155] = 1;

        lut[157] = 1;
        lut[159] = -1;
        lut[161] = -3;
        lut[163] = -1;
        lut[165] = 3;
        lut[167] = 1;
        lut[169] = 1;
        lut[171] = -1;
        lut[173] = 3;
        lut[175] = 1;
        lut[177] = -1;
        lut[179] = 1;
        lut[181] = 1;
        lut[183] = -1;
        lut[185] = 3;
        lut[187] = 1;
        lut[189] = 1;
        lut[191] = -1;
        lut[193] = -3;
        lut[195] = 3;
        lut[197] = -1;
        lut[199] = 1;
        lut[201] = 1;
        lut[203] = 3;
        lut[205] = -1;
        lut[207] = 1;

        lut[209] = -1;
        lut[211] = 1;
        lut[213] = 1;
        lut[215] = -1;
        lut[217] = 3;
        lut[219] = 1;
        lut[221] = 1;
        lut[223] = -1;
        lut[225] = 1;
        lut[227] = 3;
        lut[229] = 3;
        lut[231] = 1;
        lut[233] = 5;
        lut[235] = 3;
        lut[237] = 3;
        lut[239] = 1;
        lut[241] = -1;
        lut[243] = 1;
        lut[245] = 1;
        lut[247] = -1;
        lut[249] = 3;
        lut[251] = 1;
        lut[253] = 1;
        lut[255] = -1;

        return lut;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEulerInvariant(byte[] neighbors, int[] lut)
    {
        // LUTの範囲（あなたの CreateEulerLut では min=-7, max=+5）
        const int MAX_LUT = 5;
        const int MIN_LUT = -7;

        int E = 0;
        int n;

        // Octant SWU   (24,25,15,16,21,22,12)
        n = 1;
        if (neighbors[24] == 1) n |= 128;
        if (neighbors[25] == 1) n |= 64;
        if (neighbors[15] == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[21] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[12] == 1) n |= 2;
        E += lut[n];
        { int rem = 7; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SEU   (26,23,17,14,25,22,16)
        n = 1;
        if (neighbors[26] == 1) n |= 128;
        if (neighbors[23] == 1) n |= 64;
        if (neighbors[17] == 1) n |= 32;
        if (neighbors[14] == 1) n |= 16;
        if (neighbors[25] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[16] == 1) n |= 2;
        E += lut[n];
        { int rem = 6; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NWU   (18,21,9,12,19,22,10)
        n = 1;
        if (neighbors[18] == 1) n |= 128;
        if (neighbors[21] == 1) n |= 64;
        if (neighbors[9]  == 1) n |= 32;
        if (neighbors[12] == 1) n |= 16;
        if (neighbors[19] == 1) n |= 8;
        if (neighbors[22] == 1) n |= 4;
        if (neighbors[10] == 1) n |= 2;
        E += lut[n];
        { int rem = 5; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NEU   (20,23,19,22,11,14,10)
        n = 1;
        if (neighbors[20] == 1) n |= 128;
        if (neighbors[23] == 1) n |= 64;
        if (neighbors[19] == 1) n |= 32;
        if (neighbors[22] == 1) n |= 16;
        if (neighbors[11] == 1) n |= 8;
        if (neighbors[14] == 1) n |= 4;
        if (neighbors[10] == 1) n |= 2;
        E += lut[n];
        { int rem = 4; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SWB   (6,15,7,16,3,12,4)
        n = 1;
        if (neighbors[6]  == 1) n |= 128;
        if (neighbors[15] == 1) n |= 64;
        if (neighbors[7]  == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[3]  == 1) n |= 8;
        if (neighbors[12] == 1) n |= 4;
        if (neighbors[4]  == 1) n |= 2;
        E += lut[n];
        { int rem = 3; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant SEB   (8,7,17,16,5,4,14)
        n = 1;
        if (neighbors[8]  == 1) n |= 128;
        if (neighbors[7]  == 1) n |= 64;
        if (neighbors[17] == 1) n |= 32;
        if (neighbors[16] == 1) n |= 16;
        if (neighbors[5]  == 1) n |= 8;
        if (neighbors[4]  == 1) n |= 4;
        if (neighbors[14] == 1) n |= 2;
        E += lut[n];
        { int rem = 2; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NWB   (0,9,3,12,1,10,4)
        n = 1;
        if (neighbors[0]  == 1) n |= 128;
        if (neighbors[9]  == 1) n |= 64;
        if (neighbors[3]  == 1) n |= 32;
        if (neighbors[12] == 1) n |= 16;
        if (neighbors[1]  == 1) n |= 8;
        if (neighbors[10] == 1) n |= 4;
        if (neighbors[4]  == 1) n |= 2;
        E += lut[n];
        { int rem = 1; if (E < -MAX_LUT * rem || E > -MIN_LUT * rem) return false; }

        // Octant NEB   (2,1,11,10,5,4,14)
        n = 1;
        if (neighbors[2]  == 1) n |= 128;
        if (neighbors[1]  == 1) n |= 64;
        if (neighbors[11] == 1) n |= 32;
        if (neighbors[10] == 1) n |= 16;
        if (neighbors[5]  == 1) n |= 8;
        if (neighbors[4]  == 1) n |= 4;
        if (neighbors[14] == 1) n |= 2;
        E += lut[n];

        return E == 0;
    }

    // .hxx の isSimplePoint をそのまま移植（switchで開始octant決定）
    public static bool IsSimplePoint(byte[] neighbors)
    {
        var cube = new int[26];

        for (int i = 0; i < 13; i++) cube[i] = neighbors[i];
        for (int i = 14; i < 27; i++) cube[i - 1] = neighbors[i]; // center(13)は除外

        int label = 2;

        for (int i = 0; i < 26; i++)
        {
            if (cube[i] != 1) continue;

            int startOctant = StartOctantLut[i];

            OctreeLabeling(startOctant, label, cube);

            label++;
            if (label - 2 >= 2) return false;
        }

        return true;
    }
    public static bool IsSimplePoint(byte[] neighbors, int[] cubeScratch)
    {
        // cubeScratch.Length == 26 が前提
        for (int i = 0; i < 13; i++) cubeScratch[i] = neighbors[i];
        for (int i = 14; i < 27; i++) cubeScratch[i - 1] = neighbors[i];

        int label = 2;

        for (int i = 0; i < 26; i++)
        {
            if (cubeScratch[i] != 1) continue;

            int startOctant = i switch
            {
                0 or 1 or 3 or 4 or 9 or 10 or 12 => 1,
                2 or 5 or 11 or 13 => 2,
                6 or 7 or 14 or 15 => 3,
                8 or 16 => 4,
                17 or 18 or 20 or 21 => 5,
                19 or 22 => 6,
                23 or 24 => 7,
                25 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };

            OctreeLabeling(startOctant, label, cubeScratch); // ★ int[] 版を呼ぶ

            label++;
            if (label - 2 >= 2) return false;
        }
        return true;
    }

    // .hxx の Octree_labeling をそのまま移植（条件も再帰呼び出しも一致）
    static void OctreeLabeling(int octant, int label, int[] cube)
    {
        // 再帰 → 明示スタック（同一処理）
        Span<int> stack = stackalloc int[256];
        int sp = 0;
        stack[sp++] = octant;

        while (sp > 0)
        {
            int o = stack[--sp];

            if (o == 1)
            {
                if (cube[0] == 1) cube[0] = label;
                if (cube[1] == 1) { cube[1] = label; stack[sp++] = 2; }
                if (cube[3] == 1) { cube[3] = label; stack[sp++] = 3; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 2; stack[sp++] = 3; stack[sp++] = 4;
                }
                if (cube[9] == 1) { cube[9] = label; stack[sp++] = 5; }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 2; stack[sp++] = 5; stack[sp++] = 6;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 3; stack[sp++] = 5; stack[sp++] = 7;
                }
            }
            else if (o == 2)
            {
                if (cube[1] == 1) { cube[1] = label; stack[sp++] = 1; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 4;
                }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 5; stack[sp++] = 6;
                }
                if (cube[2] == 1) cube[2] = label;
                if (cube[5] == 1) { cube[5] = label; stack[sp++] = 4; }
                if (cube[11] == 1) { cube[11] = label; stack[sp++] = 6; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 4; stack[sp++] = 6; stack[sp++] = 8;
                }
            }
            else if (o == 3)
            {
                if (cube[3] == 1) { cube[3] = label; stack[sp++] = 1; }
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 4;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 5; stack[sp++] = 7;
                }
                if (cube[6] == 1) cube[6] = label;
                if (cube[7] == 1) { cube[7] = label; stack[sp++] = 4; }
                if (cube[14] == 1) { cube[14] = label; stack[sp++] = 7; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 4; stack[sp++] = 7; stack[sp++] = 8;
                }
            }
            else if (o == 4)
            {
                if (cube[4] == 1)
                {
                    cube[4] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 3;
                }
                if (cube[5] == 1) { cube[5] = label; stack[sp++] = 2; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 6; stack[sp++] = 8;
                }
                if (cube[7] == 1) { cube[7] = label; stack[sp++] = 3; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 7; stack[sp++] = 8;
                }
                if (cube[8] == 1) cube[8] = label;
                if (cube[16] == 1) { cube[16] = label; stack[sp++] = 8; }
            }
            else if (o == 5)
            {
                if (cube[9] == 1) { cube[9] = label; stack[sp++] = 1; }
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 6;
                }
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 7;
                }
                if (cube[17] == 1) cube[17] = label;
                if (cube[18] == 1) { cube[18] = label; stack[sp++] = 6; }
                if (cube[20] == 1) { cube[20] = label; stack[sp++] = 7; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 6; stack[sp++] = 7; stack[sp++] = 8;
                }
            }
            else if (o == 6)
            {
                if (cube[10] == 1)
                {
                    cube[10] = label;
                    stack[sp++] = 1; stack[sp++] = 2; stack[sp++] = 5;
                }
                if (cube[11] == 1) { cube[11] = label; stack[sp++] = 2; }
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 4; stack[sp++] = 8;
                }
                if (cube[18] == 1) { cube[18] = label; stack[sp++] = 5; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 7; stack[sp++] = 8;
                }
                if (cube[19] == 1) cube[19] = label;
                if (cube[22] == 1) { cube[22] = label; stack[sp++] = 8; }
            }
            else if (o == 7)
            {
                if (cube[12] == 1)
                {
                    cube[12] = label;
                    stack[sp++] = 1; stack[sp++] = 3; stack[sp++] = 5;
                }
                if (cube[14] == 1) { cube[14] = label; stack[sp++] = 3; }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 4; stack[sp++] = 8;
                }
                if (cube[20] == 1) { cube[20] = label; stack[sp++] = 5; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 6; stack[sp++] = 8;
                }
                if (cube[23] == 1) cube[23] = label;
                if (cube[24] == 1) { cube[24] = label; stack[sp++] = 8; }
            }
            else if (o == 8)
            {
                if (cube[13] == 1)
                {
                    cube[13] = label;
                    stack[sp++] = 2; stack[sp++] = 4; stack[sp++] = 6;
                }
                if (cube[15] == 1)
                {
                    cube[15] = label;
                    stack[sp++] = 3; stack[sp++] = 4; stack[sp++] = 7;
                }
                if (cube[16] == 1) { cube[16] = label; stack[sp++] = 4; }
                if (cube[21] == 1)
                {
                    cube[21] = label;
                    stack[sp++] = 5; stack[sp++] = 6; stack[sp++] = 7;
                }
                if (cube[22] == 1) { cube[22] = label; stack[sp++] = 6; }
                if (cube[24] == 1) { cube[24] = label; stack[sp++] = 7; }
                if (cube[25] == 1) cube[25] = label;
            }
        }
    }
}
