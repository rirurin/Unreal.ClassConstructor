#pragma warning disable CS1591
using Reloaded.Hooks.Definitions;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;
using Unreal.NativeTypes.Interfaces;

namespace Unreal.ClassConstructor.Interfaces;
public unsafe class DynamicClass
{
    public UClass* _instance;
    public nint* _vtableNative;
    public InternalConstructor _ctorNative;
    public IReverseWrapper<InternalConstructor> _ctorCsharp;
    public DynamicClass(UClass* instance, nint* vtableNative,
        InternalConstructor ctorNative, IReverseWrapper<InternalConstructor> ctorCsharp)
    {
        _instance = instance;
        _vtableNative = vtableNative;
        _ctorNative = ctorNative;
        _ctorCsharp = ctorCsharp;
    }

    //_context._memoryMethods.FMemory_Free((nint)vtableMethods);
    // On dispose
}
