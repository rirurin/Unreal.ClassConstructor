#pragma warning disable CS1591
using Reloaded.Hooks.Definitions;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;

namespace Unreal.ClassConstructor
{
    // Extend a particular class (increase size, hook to ctor)
    public class ClassExtenderParams
    {
        public uint Size { get; init; }
        public InternalConstructor? CtorHook { get; set; }
        public IHook<InternalConstructor>? CtorHookReal { get; set; }
        public ClassExtenderParams
            (uint size, InternalConstructor? ctorHook)
        {
            Size = size;
            CtorHook = ctorHook;
            CtorHookReal = null;
        }
    }
}