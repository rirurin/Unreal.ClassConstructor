#pragma warning disable CS1591
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;

namespace Unreal.ClassConstructor;

// Defines a fake Unreal class conatining a customizable vtable, size and constructor.
public unsafe class DynamicClassConstants
{
    private static readonly string STR_STUB_VOID = "c20000"; // ret
    private static readonly string STR_STUB_RETURN = "33c0c3"; // xor eax, eax; ret
    public unsafe nint STUB_VOID;
    public unsafe nint STUB_RETURN;
    private Memory _memory;

    public unsafe void WriteToFixed(ref nint dest, string src)
    {
        var arr = Convert.FromHexString(src);
        var alloc = _memory.Allocate((nuint)arr.Length);
        _memory.ChangeProtection(alloc.Address, (int)alloc.Length, Reloaded.Memory.Enums.MemoryProtection.ReadWriteExecute);
        dest = (nint)alloc.Address;
        _memory.WriteRaw((nuint)dest, arr);
    }
    public DynamicClassConstants(Memory memory)
    {
        _memory = memory;
        // make stub functions with static addresses
        // fine to leak it - has to live for length of program
        WriteToFixed(ref STUB_VOID, STR_STUB_VOID);
        WriteToFixed(ref STUB_RETURN, STR_STUB_RETURN);
    }
}
