using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SimpleBookmarks.Editor
{
    [Serializable]
    internal class BookmarksContainer
    {
        internal const string DataPath = "Library/SimpleBookmarks.json";
        
        public List<Group> groups;

        private static BookmarksContainer _instance;

        internal static BookmarksContainer Instance
        {
            get
            {
                if (_instance != null) return _instance;

                if (File.Exists(DataPath))
                {
                    var jsonStr = File.ReadAllText(DataPath, new UTF8Encoding(false));
                    _instance = JsonUtility.FromJson<BookmarksContainer>(jsonStr);
                }
                if (_instance == null)
                {
                    _instance = new BookmarksContainer();
                    _instance.groups = new List<Group>()
                    {
                        new()
                        {
                            name = "Default",
                            note = "The Default Group",
                            items = new List<Item>()
                        }
                    };
                    _instance.Save();
                }

                return _instance;
            }
        }

        public void Save()
        {
            try
            {
                var jsonStr = JsonUtility.ToJson(this, true);
                File.WriteAllText(DataPath, jsonStr, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void AddGroup(Group newGroup)
        {
            if(newGroup != null) groups.Add(newGroup);
        }

        public void RemoveGroup(Group target)
        {
            groups.Remove(target);
        }

        public void RemoveItem(Item target)
        {
            foreach (var g in groups)
            {
                g.items.Remove(target);
            }
        }
    }

    [Serializable]
    internal class Group
    {
        public string name;
        public string note;
        public List<Item> items;
    }

    [Serializable]
    internal class Item
    {
        protected bool Equals(Item other)
        {
            return Equals(obj, other.obj);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Item)obj);
        }

        public override int GetHashCode()
        {
            return (obj != null ? obj.GetHashCode() : 0);
        }

        public UnityEngine.Object obj;
        public string note;
    }
}