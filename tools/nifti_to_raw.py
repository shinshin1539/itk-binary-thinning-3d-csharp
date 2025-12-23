#!/usr/bin/env python3
import argparse
import numpy as np
import nibabel as nib

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("in_nii", help="input nifti (.nii or .nii.gz)")
    ap.add_argument("out_raw", help="output raw (uint8 0/1)")
    ap.add_argument("--threshold", type=float, default=None,
                    help="if set, binarize by arr > threshold; otherwise arr!=0")
    args = ap.parse_args()

    img = nib.load(args.in_nii)
    arr = np.asanyarray(img.dataobj)  # usually shape (X,Y,Z) in nibabel
    # Binarize
    if args.threshold is None:
        bin_xyz = (arr != 0)
    else:
        bin_xyz = (arr > args.threshold)

    # Convert to (Z,Y,X) so that flatten(C) matches idx=(z*h+y)*w+x
    bin_zyx = np.transpose(bin_xyz, (2, 1, 0)).astype(np.uint8)

    D, H, W = bin_zyx.shape
    bin_zyx.tofile(args.out_raw)

    ones = int(bin_zyx.sum())
    print(f"Wrote {args.out_raw}")
    print(f"D H W = {D} {H} {W}")
    print(f"ones = {ones}")

if __name__ == "__main__":
    main()
