using Unreal.NativeTypes.Interfaces;

#pragma warning disable CS1591
namespace Unreal.ClassConstructor
{
    public class StaticClassParams
    {
        public string PackageName { get; init; }
        public string Name { get; init; }
        public unsafe UClass** Instance { get; init; }
        public nint RegisterNativeFunc { get; init; } // if we want to register functions to blueprints
        public uint Size { get; init; }
        public uint Alignment { get; init; }
        public uint Flags { get; init; }
        public ulong CastFlags { get; init; }
        public nint Config { get; init; }
        public nint InternalConstructor { get; init; }
        public nint VtableHelperCtorCaller { get; init; }
        public nint AddReferantObjects { get; init; }
        public nint SuperStaticClassFn { get; init; }
        public nint BaseStaticClassFn { get; init; } // UObject
        public byte bIsDynamic { get; init; }
        public nint DynamicFn { get; init; }

        public unsafe StaticClassParams(string packageName, string name, UClass** instance, nint registerNativeFunc, uint size, uint alignment, uint flags, ulong castFlags, nint config, nint internalConstructor, nint vtableHelperCtorCaller, nint addReferantObjects, nint superStaticClassFn, nint baseStaticClassFn, byte bIsDynamic, nint dynamicFn)
        {
            PackageName = packageName;
            Name = name;
            Instance = instance;
            RegisterNativeFunc = registerNativeFunc;
            Size = size;
            Alignment = alignment;
            Flags = flags;
            CastFlags = castFlags;
            Config = config;
            InternalConstructor = internalConstructor;
            VtableHelperCtorCaller = vtableHelperCtorCaller;
            AddReferantObjects = addReferantObjects;
            SuperStaticClassFn = superStaticClassFn;
            BaseStaticClassFn = baseStaticClassFn;
            this.bIsDynamic = bIsDynamic;
            DynamicFn = dynamicFn;
        }
    }
}
