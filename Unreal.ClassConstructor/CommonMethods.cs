using SharedScans.Interfaces;
using Unreal.NativeTypes.Interfaces;

namespace Unreal.ClassConstructor
{
    public class CommonMethods
    {
        private string FMemory_Free_SIG = "E8 ?? ?? ?? ?? 48 8B 4D ?? 4C 8B BC 24 ?? ?? ?? ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 4C 8B A4 24 ?? ?? ?? ??";
        protected string FUObjectArray_SIG = "48 8B 05 ?? ?? ?? ?? 48 8B 0C ?? 48 8D 04 ?? 48 85 C0 74 ?? 44 39 40 ?? 75 ?? F7 40 ?? 00 00 00 30 75 ?? 48 8B 00";
        protected string FGlobalNamePool_SIG = "4C 8D 05 ?? ?? ?? ?? EB ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 ?? ?? ?? ?? 01 48 8B 44 24 ?? 48 8B D3 48 C1 E8 20 8D 0C ?? 49 03 4C ?? ?? E8 ?? ?? ?? ?? 48 8B C3";
        protected string StaticConstructObject_Internal_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 54 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC B0 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 8B 39";
        protected string GetPrivateStaticClassBody_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 60 45 33 ED";
        public CommonMethods(ISharedScans scans)
        {
            scans.AddScan<ICommonMethods.FMemory_Free>(FMemory_Free_SIG);
            scans.AddScan("FUObjectArray", FUObjectArray_SIG);
            scans.AddScan("FGlobalNamePool", FGlobalNamePool_SIG);
            scans.AddScan("StaticConstructObject_Internal", StaticConstructObject_Internal_SIG);
            scans.AddScan("GetPrivateStaticClassBody", GetPrivateStaticClassBody_SIG);
        }
    }

    public interface ICommonMethods
    {
        public unsafe delegate void FMemory_Free(nint ptr);
        public unsafe delegate UObject* StaticConstructObject_Internal(FStaticConstructObjectParameters* pParams);
        public unsafe delegate void GetPrivateStaticClassBody(
            nint packageName,
            nint name,
            UClass** returnClass,
            nint registerNativeFunc,
            uint size,
            uint align,
            uint flags,
            ulong castFlags,
            nint config,
            nint inClassCtor,
            nint vtableHelperCtorCaller,
            nint addRefObjects,
            nint superFn,
            nint withinFn,
            byte isDynamic,
            nint dynamicFn);
    }
}
