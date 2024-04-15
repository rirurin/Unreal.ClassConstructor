using Reloaded.Hooks.Definitions;
using SharedScans.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal.ClassConstructor
{
    public class HookState
    {
        public ISharedScans _sharedScans {get; private set; }
        public long _baseAddress { get; private set; }
        public IReloadedHooks _hooks { get; private set; }

        public HookState(ISharedScans sharedScans, long baseAddress, IReloadedHooks hooks)
        {
            _sharedScans = sharedScans;
            _baseAddress = baseAddress;
            _hooks = hooks;
        }
        public nuint TransformAddressForFUObjectArray(int offset) => GetGlobalAddress((nint)(_baseAddress + offset + 3)) - 0x10;
        public nuint GetDirectAddress(int offset) => (nuint)(_baseAddress + offset);
        public nuint GetIndirectAddressShort(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 1);
        public nuint GetIndirectAddressShort2(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 2);
        public nuint GetIndirectAddressLong(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 3);
        public nuint GetIndirectAddressLong4(int offset) => GetGlobalAddress((nint)_baseAddress + offset + 4);
        public static string PreserveMicrosoftRegisters() => $"push rcx\npush rdx\npush r8\npush r9";
        public static string RetrieveMicrosoftRegisters() => $"pop r9\npop r8\npop rdx\npop rcx";

        public void AfterSigScan(nint addr, Func<int, nuint> transformCb, Action<long> hookerCb)
        {
            var addrTransformed = transformCb((int)(addr - _baseAddress));
            hookerCb((long)addrTransformed);
        }

        public static unsafe nuint GetGlobalAddress(nint ptrAddress) => (nuint)((*(int*)ptrAddress) + ptrAddress + 4);
        public IHook<T> MakeHooker<T>(T delegateMethod, long address) => _hooks.CreateHook(delegateMethod, address).Activate();
        public T MakeWrapper<T>(long address) => _hooks.CreateWrapper<T>(address, out _);
    }
}
