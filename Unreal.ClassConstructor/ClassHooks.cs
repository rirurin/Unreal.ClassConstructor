using p3rpc.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System.Runtime.InteropServices;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;

namespace Unreal.ClassConstructor
{
    public class ClassHooks : ModuleBase<ClassConstructorContext>
    {
        public IHook<IClassMethods.StaticConstructObject_Internal> _staticConstructObject { get; private set; }
        public IHook<IClassMethods.GetPrivateStaticClassBody> _staticClassBody { get; private set; }
        public UClass_DeferredRegister _deferredRegister { get; private set; }
        private string UClass_DeferredRegister_SIG = "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8B 93 ?? ?? ?? ?? 48 8D 4C 24 ??";
        private ObjectListeners __objectListeners;
        private ClassExtender __classExtender;
        private ClassFactory __classFactory;
        public unsafe ClassHooks(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) 
        {
            _context._sharedScans.CreateListener("StaticConstructObject_Internal", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticConstructObject = _context._utils.MakeHooker<IClassMethods.StaticConstructObject_Internal>(StaticConstructObject_InternalImpl, addr)));
            _context._sharedScans.CreateListener("GetPrivateStaticClassBody", addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _staticClassBody = _context._utils.MakeHooker<IClassMethods.GetPrivateStaticClassBody>(GetPrivateStaticClassBodyImpl, addr)));
            _context._sharedScans.AddScan<UClass_DeferredRegister>(UClass_DeferredRegister_SIG);
            _context._sharedScans.CreateListener<UClass_DeferredRegister>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _deferredRegister = _context._utils.MakeWrapper<UClass_DeferredRegister>(addr)));
        }
        public override void Register() 
        {
            // Resolve imports
            __objectListeners = GetModule<ObjectListeners>();
            __classExtender = GetModule<ClassExtender>();
            __classFactory = GetModule<ClassFactory>();
        }

        public unsafe delegate void UClass_DeferredRegister(UClass* self, UClass* type, nint packageName, nint name);

        private unsafe UObject* StaticConstructObject_InternalImpl(FStaticConstructObjectParameters* pParams)
        {
            var newObj = _staticConstructObject.OriginalFunction(pParams);
            if (__objectListeners._objectListeners.TryGetValue(_context.GetObjectType(newObj), out var listeners))
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
                _context._utils.Log($"test hook from ctor! {*(nint*)x:X} (vtable {**(nint**)x:X})");
            };
            ctorHookReal += ctorHook;
            retHook = _context._utils.MakeHooker(ctorHookReal, addr).Activate();
            return retHook;
        }
    }
}
