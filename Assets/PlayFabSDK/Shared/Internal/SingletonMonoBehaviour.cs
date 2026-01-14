using UnityEngine;

namespace PlayFab.Internal
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T instance { get; private set; }

        // 기존 SDK에서 사용하던 CreateInstance 유지
        public static void CreateInstance()
        {
            if (instance != null) return;

            instance = FindFirstObjectByType<T>();
            if (instance == null)
            {
                var go = new GameObject(typeof(T).Name);
                instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
