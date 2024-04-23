#pragma warning disable CS1591
using Unreal.NativeTypes.Interfaces;
namespace Unreal.ClassConstructor.Interfaces;
public interface IObjectSearch
{
    public unsafe UObject* FindObject(string targetObj, string? objType = null);
    public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null);
    public unsafe UObject* FindFirstOf(string objType);
    public unsafe ICollection<nint> FindAllOf(string objType);
    public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb);
    public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb);
    public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb);
    public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb);
    public unsafe UObject* GetEngineTransient();
    public unsafe UClass* GetType(string type);
    public unsafe void GetTypeAsync(string type, Action<nint> foundCb);
}
