import sys
import numpy as np
import itk

def main():
    if len(sys.argv) != 6:
        print("Usage: python itk_thin_raw.py in.raw D H W out.raw")
        sys.exit(1)

    in_path = sys.argv[1]
    D = int(sys.argv[2]); H = int(sys.argv[3]); W = int(sys.argv[4])
    out_path = sys.argv[5]

    # RAW u8 -> (D,H,W) へ。C#の idx=(z*h+y)*w+x と整合（x最速）
    vol = np.fromfile(in_path, dtype=np.uint8)
    if vol.size != D*H*W:
        raise RuntimeError(f"size mismatch: got {vol.size}, expected {D*H*W}")
    vol = vol.reshape((D, H, W))

    # 0/1へ正規化
    vol = (vol != 0).astype(np.uint8)

    img = itk.GetImageFromArray(vol)  # forum等でもこの形で使われます :contentReference[oaicite:1]{index=1}
    # spacingは比較目的ならまず1でOK（このフィルタは形状のトポロジに主に依存）
    img.SetSpacing((1.0, 1.0, 1.0))

    # ITK: BinaryThinningImageFilter3D
    out_img = itk.BinaryThinningImageFilter3D.New(Input=img)
    out_img.Update()
    out_img = out_img.GetOutput()

    out_vol = itk.GetArrayFromImage(out_img).astype(np.uint8)
    out_vol.tofile(out_path)

    print(f"Wrote {out_path}, ones {int(vol.sum())} -> {int(out_vol.sum())}")

if __name__ == "__main__":
    main()
