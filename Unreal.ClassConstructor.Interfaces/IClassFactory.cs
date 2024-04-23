#pragma warning disable CS1591
using Unreal.NativeTypes.Interfaces;
using static Unreal.ClassConstructor.Interfaces.IClassExtender;

namespace Unreal.ClassConstructor.Interfaces;
public interface IClassFactory
{
    public unsafe DynamicClass? CreateClass(
        string name, string superClassName, int newMethodCount, int superClassMethodCount, 
        int allocSize, InternalConstructor? ctorUserDefined = null);
    public unsafe UObject* SpawnObject(string name, UObject* outer = null);
    public unsafe UObject* SpawnObject(UClass* targetClass, UObject* outer = null, bool bMarkAsRootSet = false);
    public unsafe UObject* SpawnObject(DynamicClass target, UObject* outer = null);
}
