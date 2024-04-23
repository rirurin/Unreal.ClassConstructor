#pragma warning disable CS1591
namespace Unreal.ClassConstructor.Interfaces;
public interface IClassExtender
{
    public unsafe void AddUnrealClassExtender(string targetClass, uint newSize, InternalConstructor? ctorHook = null);
    public unsafe delegate void InternalConstructor(nint alloc);
}
