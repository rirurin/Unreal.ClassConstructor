using Unreal.NativeTypes.Interfaces;

namespace Unreal.ClassConstructor.Interfaces
{
    public interface IObjectUtilities
    {
        public string GetFName(FName name);
        public unsafe string GetObjectName(UObject* obj);
        public unsafe string GetFullName(UObject* obj);
        public unsafe string GetObjectType(UObject* obj);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type);
        public unsafe bool DoesNameMatch(UObject* tgtObj, string name);
        public unsafe bool DoesClassMatch(UObject* tgtObj, string name);
    }
}
