﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk
{
    public abstract class LinkedItem : Item
    {
        protected List<(string, Uri)> parents = new List<(string, Uri)>();
        protected List<(string, Uri)> children = new List<(string, Uri)>();

        public IEnumerable<(string, Uri)> Parents
        {
            get => parents;
        }

        public IEnumerable<(string, Uri)> Children
        {
            get => children;
        }

        public void AddChild(string child, Uri link)
        {
            children.Add((child, link));
        }

        public void AddParent(string parent, Uri link)
        {
            parents.Add((parent, link));
        }

        public bool IsParentOf(LinkedItem item)
        {
            var result = from s in children where s.Item1 == item.ItemID select s;
            return result.Count() > 0;
        }

        public bool IsChildOf(LinkedItem item)
        {
            var result = from s in parents where s.Item1 == item.ItemID select s;
            return result.Count() > 0;
        }
    }
}