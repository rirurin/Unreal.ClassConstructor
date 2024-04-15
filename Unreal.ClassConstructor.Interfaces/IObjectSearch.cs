using p3rpc.nativetypes.Interfaces;

namespace Unreal.ClassConstructor.Interfaces
{
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
    }
}
