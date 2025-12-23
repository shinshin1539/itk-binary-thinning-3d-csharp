# itk-binary-thinning-3d-csharp

ITK-compatible 3D binary thinning (Lee 1994) implemented in C#.

This repository provides a C# implementation equivalent to ITK's
`BinaryThinningImageFilter3D` (topology-preserving thinning / skeletonization).
The goal is bitwise-equivalent behavior to ITK on representative volumes.

## Repository layout
- `ItkThinning3D.Core/` : **Library (core implementation)**  
  - `ItkBinaryThinningImageFilter3D.cs` (single-file core)
- `ItkThinning3D.App/` : Sample CLI app (optional)
- `ItkThinning3D.Tests/` : Tests / verification
- `RefCpp/` : Reference ITK C++ header sources (for comparison)

## Build
Requires .NET SDK (e.g., .NET 10).

```bash
dotnet build -c Release
```
## Usage
Input volume is a flat byte[] with index order:
idx = (z * H + y) * W + x (X is the fastest axis)
Any non-zero voxel is treated as foreground.

Usage example
```bash
using ItkThinning3D; // adjust if your namespace differs

byte[] thin = BinaryThinning3D.Thin(vol, D, H, W);
```