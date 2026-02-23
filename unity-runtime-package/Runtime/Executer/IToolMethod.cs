using System;
using System.Collections.Generic;

namespace UniMcp.Runtime
{
    public interface IToolMethod
    {
        string Description { get; }
        MethodKey[] Keys { get; }
        void ExecuteMethod(StateTreeContext args);
        string Preview();
    }
}
