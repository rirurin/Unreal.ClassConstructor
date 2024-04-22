using p3rpc.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using Unreal.ClassConstructor.Interfaces;

namespace Unreal.ClassConstructor
{
    public class ObjectUtilities : ModuleBase<ClassConstructorContext>, IObjectUtilities
    {
        // Class to pass in calls from IObjectUtilities interface into utility functions in inter-mod communication.
        public ObjectUtilities(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) {}
        public override void Register() {}
        public string GetFName(FName name) => _context.GetFName(name);
        public unsafe string GetObjectName(UObject* obj) => _context.GetObjectName(obj);
        public unsafe string GetFullName(UObject* obj) => _context.GetFullName(obj);
        public unsafe string GetObjectType(UObject* obj) => _context.GetObjectType(obj);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type) => _context.IsObjectSubclassOf(obj, type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _context.DoesNameMatch(tgtObj, name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _context.DoesClassMatch(tgtObj, name);
    }
}
