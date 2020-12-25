using System;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{

    partial class BuiltInMacros
    {
        public BuiltInMacros(Context context) => Context = context;
        public Context Context { get; }
    }
}