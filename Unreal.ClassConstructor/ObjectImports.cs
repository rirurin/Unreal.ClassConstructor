using p3rpc.nativetypes.Interfaces;
using SharedScans.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal.ClassConstructor
{
    public class ObjectImports
    {
        public unsafe FNamePool* g_namePool { get; private set; }
        public unsafe FUObjectArray* g_objectArray { get; private set; }

        public HookState _hookState;
        public ObjectImports(HookState hookState)
        {
            _hookState = hookState;
            unsafe
            {
                _hookState._sharedScans.CreateListener("FUObjectArray", addr => _hookState.AfterSigScan(addr, _hookState.TransformAddressForFUObjectArray, addr => g_objectArray = (FUObjectArray*)addr));
                _hookState._sharedScans.CreateListener("FGlobalNamePool", addr => _hookState.AfterSigScan(addr, _hookState.GetIndirectAddressLong, addr => g_namePool = (FNamePool*)addr));
            }
        }
    }
}
