using System;
using System.Collections.Generic;
using System.Linq;

namespace RivenMenu
{
    public class Tab
    {
        public readonly string Name;
        public readonly string Tooltip;
        private readonly Func<IEnumerable<ButtonInfo>> source;

        public Tab(string name, string tooltip = "", Func<IEnumerable<ButtonInfo>> source = null)
        {
            Name = name;
            Tooltip = tooltip;
            this.source = source;
        }

        public List<ButtonInfo> Items =>
            (source != null ? source() : Buttons.InCategory(Name)).ToList();
    }
}
