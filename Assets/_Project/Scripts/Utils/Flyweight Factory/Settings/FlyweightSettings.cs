using System.Linq;
using ExternPropertyAttributes;
using UnityEngine;
using UnityUtils;

namespace Flyweight
{
    [CreateAssetMenu(menuName = "Flyweight/Flyweight Settings")]
    public class FlyweightSettings : ScriptableObject
    {
        [Unity.Collections.ReadOnly] public string id;
        public FlyweightType type;
        [Required] public GameObject prefab;

        #region Unique ID Generation
        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            string newID;
            do
            {
                newID = Helpers.GenerateID();
            } while (!IsUnique(newID));

            id = newID;
        }

        private bool IsUnique(string id)
        {
            FlyweightSettings[] instances = Resources.FindObjectsOfTypeAll<FlyweightSettings>();
            return !instances.Any(instance => instance.id == id);
        }
        #endregion

        public virtual Flyweight Create()
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false);
            go.name = prefab.name;

            Flyweight flyweight = go.AddComponent<Flyweight>();
            flyweight.settings = this;

            return flyweight;
        }

        public virtual void OnGet(Flyweight f) => f.gameObject.SetActive(true);
        public virtual void OnRelease(Flyweight f) => f.gameObject.SetActive(false);
        public virtual void OnDestroyPoolObject(Flyweight f) => Destroy(f.gameObject);
    }

    public enum FlyweightType
    {
        Fire, Ice
    }
}