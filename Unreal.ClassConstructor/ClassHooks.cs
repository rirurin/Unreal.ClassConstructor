using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal.ClassConstructor
{
    public class ClassHooks
    {
        public HookState _hookState;

        private IHook<ICommonMethods.StaticConstructObject_Internal> _staticConstructObject;
        private IHook<ICommonMethods.GetPrivateStaticClassBody> _staticClassBody;
        public ClassHooks(HookState state)
        {
            _hookState = state;
            //_hookState._sharedScans.CreateListener("StaticConstructObject_Internal", addr => _hookState.AfterSigScan(addr, _hookState.GetDirectAddress, addr => _staticConstructObject = _hookState.MakeHooker<ICommonMethods.StaticConstructObject_Internal>(StaticConstructObject_InternalImpl, addr)));
            //_hookState._sharedScans.CreateListener("GetPrivateStaticClassBody", addr => _hookState.AfterSigScan(addr, _hookState.GetDirectAddress, addr => _staticClassBody = _hookState.MakeHooker<ICommonMethods.GetPrivateStaticClassBody>(GetPrivateStaticClassBodyImpl, addr)));
        }
    }
}
