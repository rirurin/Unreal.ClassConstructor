using p3rpc.nativetypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unreal.ClassConstructor.Interfaces;

namespace Unreal.ClassConstructor
{
    public class ObjectListeners : IObjectListeners
    {
        private Dictionary<string, List<Action<nint>>> _objectListeners = new();
        public ObjectUtilities _utils;
        public ObjectListeners(ObjectUtilities utils)
        {
            _utils = utils;
        }
        public unsafe void NotifyOnNewObject(UClass* type, Action<nint> cb) => NotifyOnNewObject(_utils.GetObjectName((UObject*)type), cb);
        public unsafe void NotifyOnNewObject(string typeName, Action<nint> cb)
        {
            if (_objectListeners.TryGetValue(typeName, out var listener)) listener.Add(cb);
            else _objectListeners.Add(typeName, new() { cb });
        }
    }
}
