﻿using Sigil.Impl;
using System;

namespace Sigil
{
    /// <summary>
    /// Represents a variable local to the delegate being created.
    /// 
    /// To create a Local, call DeclareLocal().
    /// </summary>
    public class Local : IOwned, IDisposable
    {
        /// <summary>
        /// The name of this local.
        /// 
        /// If one is omitted during creation a random one is created instead.
        /// 
        /// Names are purely for debugging aid, and will not appear in the generated delegate.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type stored in this local.
        /// </summary>
        public Type LocalType { get; private set; }

        internal TypeOnStack StackType { get; private set; }
        internal ushort Index { get; private set; }

        internal BufferedILGenerator.DeclareLocallDelegate LocalDel { get; private set; }

        internal LocalReusableDelegate Reusable { get; private set; }

        private object _Owner;
        object IOwned.Owner { get { return _Owner; } }

        internal Local(object owner, ushort index, Type localType, BufferedILGenerator.DeclareLocallDelegate local, string name, LocalReusableDelegate reusable)
        {
            _Owner = owner;
            LocalDel = local;
            Name = name;

            Index = index;
            LocalType = localType;
            StackType = TypeOnStack.Get(localType);

            Reusable = reusable;
        }

        /// <summary>
        /// Returns the type and name of this Local, in string form.
        /// </summary>
        public override string ToString()
        {
            return LocalType.FullName + " " + Name;
        }

        /// <summary>
        /// Frees this local.
        /// 
        /// While not strictly required, freeing a local allows it's index to be reused.
        /// 
        /// Locals are only eligible for reuse when the new local is exactly the same type.
        /// </summary>
        public void Dispose()
        {
            if (Reusable == null)
            {
                throw new InvalidOperationException(this + " double released");
            }

            Reusable(this);

            Reusable = null;
        }
    }
}
