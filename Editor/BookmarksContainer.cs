using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
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
    internal class Item : ISerializationCallbackReceiver
    {
        protected bool Equals(Item other)
        {
            return Equals(Obj, other.Obj);
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
            return Obj != null ? Obj.GetHashCode() : 0;
        }

        [NonSerialized]
        public UnityEngine.Object Obj;
        public string note;
        public string objectGuid;
        
        public void OnBeforeSerialize()
        {
            if(Obj == null) return;
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Obj, out objectGuid, out long _))
                Debug.LogError($"Could not get guid for object {Obj}");
        }

        public void OnAfterDeserialize()
        {
            var path = AssetDatabase.GUIDToAssetPath(objectGuid);
            if (string.IsNullOrEmpty(path))
                Debug.LogError($"{objectGuid} is not a valid Guid. Note:{note}");
            if (AssetDatabase.IsValidFolder(path)) 
                Obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            else 
                Obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}