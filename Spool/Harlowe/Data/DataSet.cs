using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{

    class DataSet : DataCollection
    {
        public static DataType Type { get; } = new DataType(typeof(DataSet));
        public override bool Serializable => true;
        public DataSet(IEnumerable<Data> values) : base(values.Distinct().OrderBy(x => x)) {}
        protected override bool SupportsIndexing => false;
        protected override Data Create(IEnumerable<Data> values) => new DataSet(values);
        public override bool Equals(Data other) => other is DataSet && base.Equals(other);
    }

}