using p3rpc.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using System.Collections.Concurrent;
using Unreal.ClassConstructor.Interfaces;

namespace Unreal.ClassConstructor
{
    public class ObjectListeners : ModuleBase<ClassConstructorContext>, IObjectListeners
    {
        public ConcurrentDictionary<string, List<Action<nint>>> _objectListeners { get; private init; } = new();
        public ObjectListeners(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) {}
        public override void Register() {}
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_context.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.TryAdd(typeName, new() { cb });
        }
    }
}
