using p3rpc.nativetypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal.ClassConstructor
{
    public class ObjectUtilities
    {
        public ObjectImports _imports;
        public ObjectUtilities(ObjectImports imports)
        {
            _imports = imports;
        }
        public unsafe string GetFName(FName name) => _imports.g_namePool->GetString(name);
        public unsafe string GetObjectName(UObject* obj) => _imports.g_namePool->GetString(obj->NamePrivate);
        private unsafe string GetPathName(UObject* obj, UObject* end)
        {
            var path = GetObjectName(obj);
            if (obj->OuterPrivate != null)
            {
                var separator = obj == end ? ":" : ".";
                path = $"{GetPathName(obj->OuterPrivate, end)}{separator}{path}";
            }
            return path;
        }
        public unsafe string GetFullName(UObject* obj) // path name used throughout UE4SS
        {
            return obj->OuterPrivate != null ? GetPathName(obj, obj) : GetObjectName(obj);
        }
        public unsafe string GetObjectType(UObject* obj) => _imports.g_namePool->GetString(((UObject*)obj->ClassPrivate)->NamePrivate);
        public unsafe bool IsObjectSubclassOf(UObject* obj, UClass* type)
        {
            var currType = obj->ClassPrivate;
            while (currType != null)
            {
                if (_imports.g_namePool->GetString(((UObject*)currType)->NamePrivate).Equals("Object")) break; // UObject is base type
                if (((UObject*)currType)->NamePrivate.Equals(((UObject*)type)->NamePrivate))
                    return true;
                currType = (UClass*)currType->_super.super_struct;
            }
            return false;
        }
        private unsafe bool DoesNameMatch(UObject* tgtObj, string name) => _imports.g_namePool->GetString(tgtObj->NamePrivate).Equals(name);
        private unsafe bool DoesClassMatch(UObject* tgtObj, string name) => _imports.g_namePool->GetString(((UObject*)tgtObj->ClassPrivate)->NamePrivate).Equals(name);
    }
}
