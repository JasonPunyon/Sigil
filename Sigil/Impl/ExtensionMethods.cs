﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Sigil.Impl
{
    internal static class ExtensionMethods
    {
        public static bool IsPrefix(this OpCode op)
        {
            return
                op == OpCodes.Tailcall ||
                op == OpCodes.Readonly ||
                op == OpCodes.Volatile ||
                op == OpCodes.Unaligned;
        }

        private static Type Alias(Type t)
        {
            if (t.IsValueType && t.IsEnum)
            {
                return Alias(Enum.GetUnderlyingType(t));
            }

            if (t == typeof(bool) || t == typeof(sbyte) || t == typeof(byte) || t == typeof(short) || t == typeof(ushort) || t == typeof(uint))
            {
                // Nothing smaller than In32 exists in CLR land
                return typeof(int);
            }

            if (t == typeof(long) || t == typeof(ulong))
            {
                // long/ulong are interchangable on the stack
                return typeof(long);
            }

            if (t == typeof(IntPtr) || t == typeof(UIntPtr))
            {
                return typeof(NativeInt);
            }

            return t;
        }

        public static bool IsAssignableFrom(this Type type1, TypeOnStack type2)
        {
            return TypeOnStack.Get(type1).IsAssignableFrom(type2);
        }

        public static bool IsAssignableFrom(this TypeOnStack type1, TypeOnStack type2)
        {
            // Native int can be convereted to any pointer type
            if (type1.IsPointer && type2 == TypeOnStack.Get<NativeInt>()) return true;
            if (type2.IsPointer && type1 == TypeOnStack.Get<NativeInt>()) return true;

            if ((type1.IsPointer || type1.IsReference) && !(type2.IsPointer || type2.IsReference)) return false;
            if ((type2.IsPointer || type2.IsReference) && !(type1.IsPointer || type1.IsReference)) return false;

            var t1 = type1.Type;
            var t2 = type2.Type;

            t1 = Alias(t1);
            t2 = Alias(t2);

            // The null type can be assigned to any reference type
            if (t2 == typeof(NullType) && !t1.IsValueType) return true;

            return ReallyIsAssignableFrom(t1, t2);
        }

        private static List<Type> GetBases(Type t)
        {
            if (t.IsValueType) return new List<Type>();

            var ret = new List<Type>();
            t = t.BaseType;

            while (t != null)
            {
                ret.Add(t);
                t = t.BaseType;
            }

            return ret;
        }

        private static bool ReallyIsAssignableFrom(Type t1, Type t2)
        {
            if (t1 == t2) return true;

            // quick and dirty base case
            if (t1 == typeof(object) && !t2.IsValueType) return true;

            var t1Bases = GetBases(t1);
            var t2Bases = GetBases(t2);

            if (t2Bases.Any(t2b => TypeOnStack.Get(t1).IsAssignableFrom(TypeOnStack.Get(t2b)))) return true;

            if (t1.IsInterface)
            {
                var t2Interfaces = t2.GetInterfaces();

                return t2Interfaces.Any(t2i => TypeOnStack.Get(t1).IsAssignableFrom(TypeOnStack.Get(t2i)));
            }

            if (t1.IsGenericType && t2.IsGenericType)
            {
                var t1Def = t1.GetGenericTypeDefinition();
                var t2Def = t2.GetGenericTypeDefinition();

                if (t1Def != t2Def) return false;

                var t1Args = t1.GetGenericArguments();
                var t2Args = t2.GetGenericArguments();

                for (var i = 0; i < t1Args.Length; i++)
                {
                    if (!TypeOnStack.Get(t1Args[i]).IsAssignableFrom(TypeOnStack.Get(t2Args[i]))) return false;
                }

                return true;
            }

            return t1.IsAssignableFrom(t2);
        }
    }
}
