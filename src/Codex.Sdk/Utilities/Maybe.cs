using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codex.Sdk.Utilities
{
    /// <summary>
    /// Compact representation of <see cref="bool?"/>
    /// </summary>
    public readonly record struct Maybe(sbyte state)
    {
        private readonly sbyte state = state;

        public bool HasValue => state != 0;

        public bool? State => state == 0 ? null : (state > 0);

        public static readonly Maybe True = new Maybe(1);
        public static readonly Maybe False = new Maybe(-1);
        public static readonly Maybe Unset = new Maybe(0);

        public static implicit operator bool?(Maybe value) => value.State;
        public static implicit operator Maybe(bool value) => value ? True : False;
    }
}
