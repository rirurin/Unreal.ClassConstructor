using riri.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using System.Collections.Concurrent;
using Unreal.ClassConstructor.Interfaces;

namespace Unreal.ClassConstructor
{
    public class ObjectListeners_DEPRECATED : ModuleBase<ClassConstructorContext>, IObjectListeners
    {
        private ObjectMethods _objectMethods;
        public ObjectListeners_DEPRECATED(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) {}
        public override void Register() 
        {
            _objectMethods = GetModule<ObjectMethods>();
        }
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_context.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb) => _objectMethods.NotifyOnNewObject(typeName, cb);
    }

    internal partial class ObjectMethods : ModuleBase<ClassConstructorContext>, IObjectMethods
    {
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_context.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.TryAdd(typeName, new() { cb });
        }
        public unsafe void NotifyOnNewObject<TNotifyType>(Action<nint> cb) where TNotifyType : unmanaged
            => NotifyOnNewObject(typeof(TNotifyType).Name.Substring(1), cb);
    }
}
