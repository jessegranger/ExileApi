﻿using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct RenderComponentOffsets
    {
        [FieldOffset(0x78)] public Vector3 Pos;
        [FieldOffset(0x84)] public Vector3 Bounds;
        [FieldOffset(0x98)] public NativeStringU Name;
        [FieldOffset(0xB8)] public Vector3 Rotation;
        [FieldOffset(0xE0)] public float Height;
    }
}