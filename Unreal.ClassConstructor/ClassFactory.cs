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

        // Custom class creation:
        // Class defintion collection classes should move into their own mod (Unreal.ClassConstructor)
        // so that only one instance of these structures exist at runtime
        // move Unreal engine specific native types into Unreal.ModUtils (decrease size of p3rpc.nativetypes)
        // p3rpc.commondmodutils will still be the main project to interface with these new packages, since
        // there's nothing here that requires that it be P3RE (add exe detection + unique scans per game)
        // UnrealSignatureBase
        // -> UnrealSignatureP3RE
        // -> UnrealSignatureSMTVV
        // -> UnrealSignatureP6
        // -> UnrealSignatureHiFiRUSH
        // -> UnrealSignatureFF7RE
        // DynClassCustomVtableEntry
        // DynClassMakeParams (name, vtable size + alloc size, custom vtable entries), DynClassSuperParams (name, vtable size)
        // CreateNewClass (class, superClass)
        // ReplaceVtableEntry (index, funcptr)

        // Gaslights Unreal Engine into thinking that the game compiled with a class "name" by copying vtable information from
        // an existing class and retrieving appropriate constructors
        public unsafe UClass* CreateClass(
            string name,
            string superClassName,
            int newMethodCount, // superClassMethodCount + new methods to define
                                // afaik there's no way to determine the size of a class's vtable, so this'll have to be determined by
                                // checking how the vtable looks in a disassembler
            int superClassMethodCount,
            int allocSize // superClassAllocSize + extra alloc for our new class
        )
        {
            _context._utils.Log($"{_dynConsts.STUB_VOID:X}, {_dynConsts.STUB_RETURN:X}");
            UClass* superClass = __objectSearch.GetType(superClassName);
            // make fake vtable (vtableNative field in DynamicObject)
            // we can point these vtable entries to other places later on (e.g to override AActor::Tick, we can redirect
            // to our own)
            nint* superVtable = (nint*)superClass->class_default_obj->_vtable;
            int totalMethodCount = superClassMethodCount + newMethodCount;
            nint* vtableMethods = (nint*)_context._memoryMethods.FMemory_Malloc(totalMethodCount * sizeof(nint), (uint)sizeof(nint));
            UClass** dynClassPtr = (UClass**)_context._memoryMethods.FMemory_Malloc(sizeof(nint), (uint)sizeof(nint));
            for (int i = 0; i < superClassMethodCount; i++)
                vtableMethods[i] = superVtable[i];
            for (int i = superClassMethodCount; i < totalMethodCount; i++)
                vtableMethods[i] = 0;
            // Set names. We're gonna pretend that we're compiled as a Persona 3 Reload class
            nint packageNameTemp = StringToPtrUni("/Script/xrd777");
            nint nameTemp = StringToPtrUni(name);
            nint configTemp = StringToPtrUni("Engine");
            // we're a subclass of AActor, so find a class that's also a subactor of AActor to get it's super StaticClass
            // withinFn is pretty much always UObject::StaticClass
            _classNameToStaticClassParams.TryGetValue("AppActor", out var appActorParams); // for superfn/withinfn
            _classNameToStaticClassParams.TryGetValue("Actor", out var actorParams); // for ctor
            UClass* classType = __objectSearch.GetType("Class");
            _context._logger.WriteLine($"CLASS: {(nint)classType:X}");
            // what the fuck
            __classHooks._staticClassBody.OriginalFunction(
                packageNameTemp, nameTemp, dynClassPtr,
                _dynConsts.STUB_VOID, (uint)allocSize, (uint)sizeof(nuint),
                (uint)(EClassFlags.CLASS_Intrinsic), (ulong)EClassCastFlags.CASTCLASS_None,
                configTemp, actorParams.InternalConstructor, _dynConsts.STUB_VOID,
                _dynConsts.STUB_VOID, appActorParams.SuperStaticClassFn,
                appActorParams.BaseStaticClassFn, 0, 0
            );
            __classHooks._deferredRegister.Invoke(*dynClassPtr, classType, packageNameTemp, nameTemp);
            // goodbye!!!! :3
            _context._memoryMethods.FMemory_Free(packageNameTemp);
            _context._memoryMethods.FMemory_Free(nameTemp);
            _context._memoryMethods.FMemory_Free(configTemp);
            _context._memoryMethods.FMemory_Free((nint)vtableMethods);
            return *dynClassPtr;
        }
    }
}
