using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivenMenu
{
    public class Tab
    {
        public readonly string Name;
        public readonly string Tooltip;
        private readonly Func<IEnumerable<ButtonInfo>> source;

        private List<ButtonInfo> cache;
        private float nextRefresh;

        public Tab(string name, string tooltip = "", Func<IEnumerable<ButtonInfo>> source = null)
        {
            Name = name;
            Tooltip = tooltip;
            this.source = source;
        }

        public List<ButtonInfo> Items
        {
            get
            {
                if (cache == null || Time.unscaledTime >= nextRefresh)
                {
                    cache = (source != null ? source() : Buttons.InCategory(Name)).ToList();
                    nextRefresh = Time.unscaledTime + 0.25f;
                }
                return cache;
            }
        }
    }
}
