﻿using Sigil.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Sigil
{
    public partial class Emit<DelegateType>
    {
        public void CallVirtual(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (method.IsStatic)
            {
                throw new SigilException("Only non-static methods can be called using CallVirtual, found " + method, Stack);
            }

            var expectedParams = method.GetParameters().Select(s => TypeOnStack.Get(s.ParameterType)).ToList();

            expectedParams.Insert(0, TypeOnStack.Get(method.DeclaringType));

            var onStack = Stack.Top(expectedParams.Count);

            if (onStack == null)
            {
                throw new SigilException("CallVirtual to " + method + " expected parameters [" + string.Join(", ", expectedParams) + "] to be on the stack", Stack);
            }

            // Things come off the stack in "Reverse" order
            var onStackR = onStack.Reverse().ToList();

            for (var i = 0; i < expectedParams.Count; i++)
            {
                var shouldBe = expectedParams[i];
                var actuallyIs = onStackR[i];

                if (!shouldBe.IsAssignableFrom(actuallyIs))
                {
                    throw new SigilException("Parameter #" + (i + 1) + " to " + method + " should be " + shouldBe + ", but found " + actuallyIs, Stack);
                }
            }

            var resultType = method.ReturnType == typeof(void) ? null : TypeOnStack.Get(method.ReturnType);

            UpdateState(OpCodes.Callvirt, method, resultType, pop: expectedParams.Count);
        }
    }
}