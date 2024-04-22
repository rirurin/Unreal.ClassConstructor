using p3rpc.commonmodutils;
using Unreal.NativeTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unreal.ClassConstructor.Interfaces;

namespace Unreal.ClassConstructor
{
    public class ObjectSearch : ModuleBase<ClassConstructorContext>, IObjectSearch
    {
        private Thread _findObjectThread { get; init; }
        private BlockingCollection<FindObjectBase> _findObjects { get; init; } = new();
        public ObjectSearch(ClassConstructorContext context, Dictionary<string, ModuleBase<ClassConstructorContext>> modules) : base(context, modules) 
        {
            _findObjectThread = new Thread(new ThreadStart(ProcessObjectQueue));
            _findObjectThread.IsBackground = true;
            _findObjectThread.Start();
        }

        public override void Register()
        {

        }

        public abstract class FindObjectBase
        {
            protected ObjectSearch Context { get; init; }
            public FindObjectBase(ObjectSearch context) { Context = context; }
            public abstract void Execute();
        }
        public class FindObjectByName : FindObjectBase
        {
            public string ObjectName { get; set; }
            public string? TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; } // Action<UObject*>
            public FindObjectByName(ObjectSearch context, string objectName, string? typeName, Action<nint> foundCb)
                : base(context)
            {
                ObjectName = objectName;
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindObject(ObjectName, TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectFirstOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<nint> FoundObjectCallback { get; set; }
            public FindObjectFirstOfType(ObjectSearch context, string typeName, Action<nint> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindFirstOf(TypeName);
                if (foundObj != null) FoundObjectCallback((nint)foundObj);
            }
        }
        public class FindObjectAllOfType : FindObjectBase
        {
            public string TypeName { get; set; }
            public Action<ICollection<nint>> FoundObjectCallback { get; set; }
            public FindObjectAllOfType(ObjectSearch context, string typeName, Action<ICollection<nint>> foundCb)
                : base(context)
            {
                TypeName = typeName;
                FoundObjectCallback = foundCb;
            }
            public unsafe override void Execute()
            {
                var foundObj = Context.FindAllOf(TypeName);
                if (foundObj != null) FoundObjectCallback(foundObj);
            }
        }

        private unsafe void ForEachObject(Action<nint> objItem)
        {
            for (int i = 0; i < _context.g_objectArray->NumElements; i++)
            {
                var currObj = &_context.g_objectArray->Objects[i >> 0x10][i & 0xffff];
                if (currObj->Object == null || (currObj->Flags & EInternalObjectFlags.Unreachable) != 0) continue;
                objItem((nint)currObj);
            }
        }

        // Synchronous operations for finding objects. This will block the caller thread for a while
        public unsafe UObject* FindObject(string targetObj, string? objType = null)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || _context.DoesClassMatch(currObj->Object, objType))
                    {
                        ret = currObj->Object;
                        return;
                    }
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllObjectsNamed(string targetObj, string? objType = null)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesNameMatch(currObj->Object, targetObj))
                {
                    if (objType == null || _context.DoesClassMatch(currObj->Object, objType))
                        objects.Add((nint)currObj->Object);
                }
            });
            return objects;
        }
        public unsafe UObject* FindFirstOf(string objType)
        {
            UObject* ret = null;
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesClassMatch(currObj->Object, objType))
                {
                    ret = currObj->Object;
                    return;
                }
            });
            return ret;
        }
        public unsafe ICollection<nint> FindAllOf(string objType)
        {
            var objects = new List<nint>();
            ForEachObject(currAddr =>
            {
                var currObj = (FUObjectItem*)currAddr;
                if (_context.DoesClassMatch(currObj->Object, objType))
                    objects.Add((nint)currObj->Object);
            });
            return objects;
        }
        // Async object finding operations
        public unsafe void FindObjectAsync(string targetObj, string? objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectByName(this, targetObj, objType, foundCb));
        public unsafe void FindObjectAsync(string targetObj, Action<nint> foundCb) => FindObjectAsync(targetObj, null, foundCb);
        public unsafe void FindFirstOfAsync(string objType, Action<nint> foundCb) => _findObjects.Add(new FindObjectFirstOfType(this, objType, foundCb));
        public unsafe void FindAllOfAsync(string objType, Action<ICollection<nint>> foundCb) => _findObjects.Add(new FindObjectAllOfType(this, objType, foundCb));

        private void ProcessObjectQueue()
        {
            try
            {
                while (true)
                {
                    if (_findObjects.TryTake(out var currFindObj))
                        currFindObj.Execute();
                }
            }
            catch (OperationCanceledException) { } // Called during process termination
        }

        public unsafe UObject* GetEngineTransient() => FindObject("/Engine/Transient", "Package");
        public unsafe UClass* GetType(string type) => (UClass*)FindObject(type, "Class");
        public unsafe void GetTypeAsync(string type, Action<nint> foundCb) => FindObjectAsync(type, "Class", foundCb);
    }
}
