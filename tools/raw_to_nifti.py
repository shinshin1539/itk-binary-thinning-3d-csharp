#!/usr/bin/env python3
import argparse
import numpy as np
import nibabel as nib

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("in_raw", help="input raw (uint8 0/1)")
    ap.add_argument("D", type=int)
    ap.add_argument("H", type=int)
    ap.add_argument("W", type=int)
    ap.add_argument("ref_nii", help="reference nifti to copy affine/header")
    ap.add_argument("out_nii", help="output nifti (.nii or .nii.gz)")
    args = ap.parse_args()

    D, H, W = args.D, args.H, args.W
    data = np.fromfile(args.in_raw, dtype=np.uint8)
    if data.size != D * H * W:
        raise ValueError(f"Size mismatch: got {data.size}, expected {D*H*W}")

    vol_zyx = data.reshape((D, H, W))  # z,y,x
    vol_xyz = np.transpose(vol_zyx, (2, 1, 0))  # back to x,y,z

    ref = nib.load(args.ref_nii)
    # Make uint8 NIfTI
    out = nib.Nifti1Image(vol_xyz.astype(np.uint8), affine=ref.affine, header=ref.header.copy())
    out.set_data_dtype(np.uint8)
    nib.save(out, args.out_nii)

    ones = int(vol_xyz.sum())
    print(f"Wrote {args.out_nii}, ones={ones}")

if __name__ == "__main__":
    main()
