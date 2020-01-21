﻿using System.Reflection;
using System.Runtime.Loader;

namespace JitPad.Core
{
    internal class UnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        public UnloadableAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}