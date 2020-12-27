using System.Collections.Generic;
using System.Linq;
using Util;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        // TODO: Consider improving performance here
        public Array history(Filter filter) => new Array(Context.History.Select(x => new String(x)).Where(x => filter(passage(x))));
        public Array history() => new Array(Context.History.Select(x => new String(x)));
        public DataMap passage(String name) => new DataMap(new Dictionary<Data, Data>{
            {new String("name"), name},
            {new String("source"), new String(Context.Story.GetPassage(name.Value))},
            {new String("tags"), new Array(Context.Story.GetTags(name.Value).Select(x => new String(x)))}
        });
        public DataMap passage() => passage(new String(Context.CurrentPassage));
        public Array passages(Filter filter) => new Array(
            Context.Story.PassageNames
                .OrderBy(x => x, new AlphanumComparator())
                .Select(x => passage(new String(x)))
                .Where(x => filter(x))
        );
        public Array passages() => passages(_ => true);
    }
}