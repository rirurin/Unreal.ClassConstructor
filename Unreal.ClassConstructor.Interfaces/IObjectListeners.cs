using p3rpc.nativetypes.Interfaces;

namespace Unreal.ClassConstructor.Interfaces
{
    public interface IObjectListeners
    {
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb);
    }
}
