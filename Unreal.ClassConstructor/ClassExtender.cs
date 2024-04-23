using p3rpc.commonmodutils;
using Reloaded.Hooks.Definitions;
using System.Collections.Concurrent;
using Unreal.ClassConstructor.Interfaces;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;

namespace Unreal.ClassConstructor
{
    public class ClassExtender : ModuleBase<ClassConstructorContext>, IClassExtender
    {
        public ConcurrentDictionary<string, ClassExtenderParams> _classNameToClassExtender { get; private init; } = new();
        public ClassExtender(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) { }
        public override void Register() { }

        // This should be done on during module ctor, which will run before any Unreal Engine code runs
        public unsafe void AddUnrealClassExtender
            (string targetClass, uint newSize,
            InternalConstructor? ctorHook = null) // called when UE runs InternalConstructor_[TARGET_CLASS_NAME]
            //Action<IHook<ClassExtenderParams.InternalConstructor>>? onMakeHook = null) // for the caller to store the created hook
            => _classNameToClassExtender.TryAdd(targetClass, new ClassExtenderParams(newSize, ctorHook));
    }
}
