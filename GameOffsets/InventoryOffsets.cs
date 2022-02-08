﻿using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct InventoryOffsets
    {
        [FieldOffset(0x260)] public int MoveItemHoverState;
        [FieldOffset(0x268)] public long HoverItem;
        [FieldOffset(0x270)] public int XFake;
        [FieldOffset(0x274)] public int YFake;
        [FieldOffset(0x278)] public int XReal;
        [FieldOffset(0x27C)] public int YReal;
        [FieldOffset(0x288)] public short CursorInInventory;
        [FieldOffset(0x410)] public long ItemCount;
        [FieldOffset(0x494)] public Vector2i InventorySize;
        [FieldOffset(0x494)] public int TotalBoxesInInventoryRow;
    }
}
