using riri.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System.Runtime.InteropServices;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;
using System.Numerics;

namespace Unreal.ClassConstructor
{
    public class ClassHooks : ModuleBase<ClassConstructorContext>
    {
        public IHook<IClassMethods.StaticConstructObject_Internal> _staticConstructObject { get; private set; }
        public IHook<IClassMethods.GetPrivateStaticClassBody> _staticClassBody { get; private set; }
        public UClass_DeferredRegister _deferredRegister { get; private set; }
        private string UClass_DeferredRegister_SIG = "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8B 93 ?? ?? ?? ?? 48 8D 4C 24 ??";

        public UObjectProcessRegistrants _processRegistrants { get; private set; }
        private string UObjectProcessRegistrants_SIG = "48 8B C4 55 48 83 EC 70 48 89 58 ?? 48 8D 15 ?? ?? ?? ??";
        public delegate void UObjectProcessRegistrants();

        private string UWorld_SpawnActor_SIG = "40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC F8 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ??";
        public UWorld_SpawnActor _spawnActor;
        public unsafe delegate AActor* UWorld_SpawnActor(UWorld* self, UClass* type, FTransform* pUserTransform, FActorSpawnParameters* spawnParams);

        public FName_Ctor _fnameCtor;
        private string FNameCtor_SIG = "48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 48 89 54 24 ?? 33 C9 41 8B F8 4C 8B DA";
        public unsafe delegate FName* FName_Ctor(FName* self, nint name, EFindType findType);

        private string FUObjectHashTables_Get_SIG = "E8 ?? ?? ?? ?? 48 8B F8 33 C0 F0 0F B1 35 ?? ?? ?? ??";
        public FUObjectHashTables_Get _getObjectHashTables;
        public unsafe delegate FUObjectHashTables* FUObjectHashTables_Get();

        private string GEngine_SIG = "48 89 05 ?? ?? ?? ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8D 4D ??"; // in FEngineLoop::Init

        private ClassExtender __classExtender;
        private ClassFactory __classFactory;
        private ObjectMethods __objectMethods;

        public unsafe UEngine** GEngine;

        public unsafe ClassHooks(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) 
        {
            _context._sharedScans.CreateListener("StaticConstructObject_Internal", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticConstructObject = _context._utils.MakeHooker<IClassMethods.StaticConstructObject_Internal>(StaticConstructObject_InternalImpl, addr)));
            _context._sharedScans.CreateListener("GetPrivateStaticClassBody", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticClassBody = _context._utils.MakeHooker<IClassMethods.GetPrivateStaticClassBody>(GetPrivateStaticClassBodyImpl, addr)));
            _context._sharedScans.AddScan<UClass_DeferredRegister>(UClass_DeferredRegister_SIG);
            _context._sharedScans.CreateListener<UClass_DeferredRegister>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _deferredRegister = _context._utils.MakeWrapper<UClass_DeferredRegister>(addr)));
            _context._sharedScans.AddScan<UObjectProcessRegistrants>(UObjectProcessRegistrants_SIG);
            _context._sharedScans.CreateListener<UObjectProcessRegistrants>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _processRegistrants = _context._utils.MakeWrapper<UObjectProcessRegistrants>(addr)));

            _context._utils.SigScan(UWorld_SpawnActor_SIG, "UWorld::SpawnActor", _context._utils.GetDirectAddress,
                addr => _spawnActor = _context._utils.MakeWrapper<UWorld_SpawnActor>(addr));

            _context._sharedScans.AddScan<FName_Ctor>(FNameCtor_SIG);
            _context._sharedScans.CreateListener<FName_Ctor>(addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetDirectAddress, addr => _fnameCtor = _context._utils.MakeWrapper<FName_Ctor>(addr)));

            _context._sharedScans.AddScan("GEngine", GEngine_SIG);
            _context._sharedScans.CreateListener("GEngine", addr => _context._utils.AfterSigScan(
                addr, _context._utils.GetIndirectAddressLong, addr => GEngine = (UEngine**)addr));
        }
        public override void Register() 
        {
            // Resolve imports
            __classExtender = GetModule<ClassExtender>();
            __classFactory = GetModule<ClassFactory>();
            //__objectListeners = GetModule<ObjectListeners>();
            //__objectUtilities = GetModule<ObjectUtilities>();
            __objectMethods = GetModule<ObjectMethods>();
        }

        public unsafe delegate void UClass_DeferredRegister(UClass* self, UClass* type, nint packageName, nint name);

        public unsafe UObject* StaticConstructObject_InternalImpl(FStaticConstructObjectParameters* pParams)
        {
            var newObj = _staticConstructObject.OriginalFunction(pParams);
            if (__objectMethods._objectListeners.TryGetValue(_context.GetObjectType(newObj), out var listeners))
                foreach (var listener in listeners) listener((nint)newObj);
            return newObj;
        }

        // Scuffed Unreal class manipulation

        
        private unsafe void GetPrivateStaticClassBodyImpl(
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
            nint vtableHelperCtorCaller, // xor eax,eax, ret
            nint addRefObjects, // ret
            nint superFn, // [superType]::StaticClass
            nint withinFn, // usually UObject::StaticClass
            byte isDynamic,
            nint dynamicFn)
        {
            var className = Marshal.PtrToStringUni(name);
            //_utils.Log($"Reading class {className}");
            // check if class has been extended, and do appropriate actions
            if (className != null && __classExtender._classNameToClassExtender.TryGetValue(className, out var classExtender))
            {
                // change size
                if (size <= classExtender.Size)
                {
                    _context._utils.Log($"NOTICE: Extended size of class \"{className}\" (from {size} to {classExtender.Size})", LogLevel.Debug);
                    size = classExtender.Size;
                }
                else _context._utils.Log($"ERROR: Class extender for \"{className}\" has defined size smaller than original class (from {size} to {classExtender.Size}). This has been rejected.", System.Drawing.Color.Red, LogLevel.Error);
                // hook ctor
                if (classExtender.CtorHook != null && inClassCtor != 0)
                {
                    var newHook = FollowThunkToGetAppropriateHook(inClassCtor, classExtender.CtorHook);
                    classExtender.CtorHookReal = newHook;
                }
            }
            // add class to static params map - collect info for dynamic class creation
            var packageNameStr = Marshal.PtrToStringUni(packageName);
            if (className != null && packageNameStr != null)
            {
                __classFactory._classNameToStaticClassParams.TryAdd(className, new StaticClassParams(
                    packageNameStr,
                    className,
                    returnClass,
                    registerNativeFunc,
                    size,
                    align,
                    flags,
                    castFlags,
                    config,
                    inClassCtor,
                    vtableHelperCtorCaller,
                    addRefObjects,
                    superFn,
                    withinFn,
                    isDynamic,
                    dynamicFn
                ));
            }
            _staticClassBody.OriginalFunction(packageName, name, returnClass, registerNativeFunc, size, align, flags, castFlags,
                config, inClassCtor, vtableHelperCtorCaller, addRefObjects, superFn, withinFn, isDynamic, dynamicFn);
        }
        public unsafe IHook<InternalConstructor> FollowThunkToGetAppropriateHook
            (nint addr, InternalConstructor ctorHook)
        {
            // build a new multicast delegate by injecting the native function, followed by custom code
            // this reference will live for program's lifetime so there's no need to store hook in the caller
            IHook<InternalConstructor>? retHook = null;
            InternalConstructor ctorHookReal = x =>
            {
                if (retHook == null)
                {
                    _context._utils.Log($"ERROR: retHook is null. Game will crash.", System.Drawing.Color.Red, LogLevel.Error);
                    return;
                }
                retHook.OriginalFunction(x);
            };
            ctorHookReal += ctorHook;
            retHook = _context._utils.MakeHooker(ctorHookReal, addr).Activate();
            return retHook;
        }
    }
}
