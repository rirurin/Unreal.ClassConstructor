using p3rpc.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unreal.ClassConstructor.Interfaces;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;

namespace Unreal.ClassConstructor
{
    public class ClassFactory : ModuleBase<ClassConstructorContext>, IClassFactory
    {
        private DynamicClassConstants _dynConsts;

        private ObjectSearch __objectSearch;
        private ClassHooks __classHooks;
        public ConcurrentDictionary<string, StaticClassParams> _classNameToStaticClassParams { get; private init; } = new();
        public ClassFactory(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) 
        {
            _dynConsts = new(_context._memory);
        }
        public override void Register() 
        {
            __objectSearch = GetModule<ObjectSearch>();
            __classHooks = GetModule<ClassHooks>();
        }

        public nint StringToPtrUni(string str)
        {
            str += "\0";
            byte[] strBytes = Encoding.Unicode.GetBytes(str);
            nint alloc = _context._memoryMethods.FMemory_Malloc(strBytes.Length, sizeof(char));
            _context._memory.WriteRaw((nuint)alloc, strBytes);
            return alloc;
        }

        // Gaslights Unreal Engine into thinking that the game compiled with a class "name" by copying vtable information from
        // an existing class and retrieving appropriate constructors

        // AActor vtable - 199 methods
        // UObject vtable - 78 methods
        public unsafe DynamicClass? CreateClass(
            string name,
            string superClassName,
            int newMethodCount, // superClassMethodCount + new methods to define
                                // afaik there's no way to determine the size of a class's vtable, so this'll have to be determined by
                                // checking how the vtable looks in a disassembler
            int superClassMethodCount,
            int allocSize, // superClassAllocSize + extra alloc for our new class
            InternalConstructor? ctorUserDefined = null
        )
        {
            // We need to get static class param info from two classes - the super class and a subclass of the super class (sibling class)
            // GetPrivateStaticClassBody asks for an InternalConstructor, which we copy from super class and attach our own code onto.
            // AddReferantObjects also comes from the super class, but Super ctor and Within ctor use sibling class
            // Do this early, because this operation can fail (if you try to derive from a class with no derivates, it won't find a sibling)
            UClass* superClass = __objectSearch.GetType(superClassName);
            UObject* siblingClass = __objectSearch.FindFirstSubclassOf(superClass);

            if (superClass == null)
            {
                _context._utils.Log($"ERROR: Could not find an existing superclass with the name {name}", System.Drawing.Color.Red);
                return null;
            } 
            else if (siblingClass == null)
            {
                _context._utils.Log($"ERROR: Could not find an appropriate sibling class for {name} (checked superclass {superClassName})", System.Drawing.Color.Red);
                return null;
            }
            else _context._utils.Log($"Found sibling class @ 0x{(nint)siblingClass:X}");

            _classNameToStaticClassParams.TryGetValue(
                _context.GetObjectName((UObject*)siblingClass->ClassPrivate), out var siblingParams);
            _classNameToStaticClassParams.TryGetValue(
                _context.GetObjectName((UObject*)superClass), out var superParams);
            UClass* classType = __objectSearch.GetType("Class"); // typeof(UClass) required for UObject::DeferredRegister
            // Make a fake vtable. Copy entries from the superclass's vtable, then blank out any new entries.
            // The mod user will be responsible for creating reverse wrappers to replace a particular vtable entry, which
            // they'll store as a field in their mod (there's no way for us to predict the calling convention of these methods)
            nint* superVtable = (nint*)superClass->class_default_obj->_vtable;
            int totalMethodCount = superClassMethodCount + newMethodCount;
            nint* vtableMethods = (nint*)_context._memoryMethods.FMemory_Malloc(totalMethodCount * sizeof(nint), (uint)sizeof(nint));
            // for each UE class, a global variable stores a pointer to the class pointer (fine to leak this)
            UClass** dynClassPtr = (UClass**)_context._memoryMethods.FMemory_Malloc(sizeof(nint), (uint)sizeof(nint));
            NativeMemory.Copy(superVtable, vtableMethods, (nuint)(superClassMethodCount * sizeof(nint)));
            NativeMemory.Clear(vtableMethods + superClassMethodCount, (nuint)((totalMethodCount - superClassMethodCount) * sizeof(nint)));
            // Create some FStrings used to pass arguments for what UE module this is from, the config.ini used and the name itself
            // This currently assumes /Script/xrd777 (Persona 3 Reload) though there'll be an option to change this at some point
            nint packageNameTemp = StringToPtrUni("/Script/xrd777");
            nint nameTemp = StringToPtrUni(name);
            nint configTemp = StringToPtrUni("Engine");
            // we need to make our own internal constructor function so we can insert our custom vtable
            var dynClassCtor = _context._hooks.CreateWrapper<InternalConstructor>(superParams.InternalConstructor, out _);
            dynClassCtor += x => // vtable redirection
            {
                if (*(nint*)x != 0)
                {
                    UObject* xAlloc = *(UObject**)x;
                    _context._utils.Log($"Redirected vtable ({xAlloc->_vtable:X} -> {(nint)vtableMethods:X})");
                    xAlloc->_vtable = (nint)vtableMethods;
                }
            };
            // if user defines any custom initialization code, run that now
            if (ctorUserDefined != null) dynClassCtor += ctorUserDefined;
            var ctorCsharpWrapper = _context._hooks.CreateReverseWrapper(dynClassCtor);
            // fuck it
            // we ball
            __classHooks._staticClassBody.OriginalFunction(
                packageNameTemp, // packageName
                nameTemp, // name
                dynClassPtr, // returnClass
                _dynConsts.STUB_VOID, // we're not defining any blueprints methods
                (uint)allocSize, // size
                (uint)sizeof(nuint), // align
                (uint)( // flags
                    /*EClassFlags.CLASS_Intrinsic | EClassFlags.CLASS_Native | 
                    EClassFlags.CLASS_Constructed | EClassFlags.CLASS_RequiredAPI
                    */
                    EClassFlags.CLASS_Intrinsic
                    ), 
                (ulong)EClassCastFlags.CASTCLASS_None, // castFlags
                configTemp, // config
                ctorCsharpWrapper.NativeFunctionPtr, // invoke our custom constructor
                _dynConsts.STUB_RETURN, // vtable helper (always null)
                superParams.AddReferantObjects, // add referenced objects
                /*sibling*/superParams.SuperStaticClassFn, // [superClass]::StaticClass
                /*sibling*/superParams.BaseStaticClassFn,  // UObject::StaticClass
                0, 0 // not a dynamic class
            );
            __classHooks._deferredRegister.Invoke(*dynClassPtr, classType, packageNameTemp, nameTemp);
            // goodbye!!!! :3
            _context._memoryMethods.FMemory_Free(packageNameTemp);
            _context._memoryMethods.FMemory_Free(nameTemp);
            _context._memoryMethods.FMemory_Free(configTemp);
            // Return a dynamic class instance to neatly store data associated with our custom class
            // It's the responsibility of the mod creator to store it so it doesn't get GC'd
            return new DynamicClass(*dynClassPtr, vtableMethods, dynClassCtor, ctorCsharpWrapper);
        }

        public unsafe UObject* SpawnObject(string name, UObject* outer = null) => SpawnObject(__objectSearch.GetType(name), outer);

        public unsafe UObject* SpawnObject(DynamicClass target, UObject* outer = null) => SpawnObject(target._instance, outer, true);
        public unsafe UObject* SpawnObject(UClass* targetClass, UObject* outer = null, bool bMarkAsRootSet = false)
        {
            if (outer != null) outer = __objectSearch.GetEngineTransient();
            var constructObject = _context._memoryMethods.FMemory_Malloc<FStaticConstructObjectParameters>(8);
            NativeMemory.Clear(constructObject, (nuint)_context._memoryMethods.FMemory_GetAllocSize((nint)constructObject));
            constructObject->Class = targetClass;
            constructObject->Outer = outer;
            //if (bMarkAsRootSet) constructObject->SetFlags |= EObjectFlags.MarkAsRootSet;
            _context._utils.Log($"Calling StaticConstructObject_Internal, alloc size {_context._memoryMethods.FMemory_GetAllocSize((nint)constructObject)}");
            var newObj = __classHooks._staticConstructObject.OriginalFunction(constructObject);
            _context._memoryMethods.FMemory_Free(constructObject);
            return newObj;
        }
    }
}
