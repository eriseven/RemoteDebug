using UnityEngine;

namespace RD
{
    public static class DebugObjectManager
    {
        [Command("ObjectManagement.CacheGO", "Caches a GameObject")]
        public static void CacheGO(string[] args)
        {
            if(args.Length < 1)
            {
                Console.Log("Invalid args. This command takes one arguments.");
                return;
            }

            GameObject go = GameObject.Find(args[0]);
            if (go == null)
            {
                Console.Log($"GameObject with name '{args[0]}' could not be find.");
                return;
            }

            m_cachedGo = go;
            Console.Log($"GameObject '{m_cachedGo.name}' is now cached.");
        }

        [Command("ObjectManagement.CleanCache", "Removes the cached reference")]
        public static void CleanCache()
        {
            m_cachedGo = null;
            Console.Log("Cached GO cleaned...");
        }

        [Command("ObjectManagement.ListComponents", "Lists all components on cached GameObject")]
        public static void ListComponents()
        {
            if(m_cachedGo == null)
            {
                Console.Log("Cache a GameObject before listing components.");
                return;
            }

            try
            {
                Console.Log(" +" + m_cachedGo.name);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach(Component cmp in m_cachedGo.GetComponents(typeof(Component)))
                {
                    sb.Append("\t-");
                    sb.Append(cmp.GetType().ToString());
                    sb.Append("\n");
                }
                Console.Log(sb.ToString());
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [Command("ObjectManagement.SetActive", "Set cached GO active")]
        public static void SetActive(string[] args)
        {
            if(m_cachedGo == null)
            {
                Console.Log("Cache a GameObject before.");
                return;
            }

            if (args.Length < 1)
            {
                Console.Log("Invalid args. This command takes one arguments.");
                return;
            }

            if (!bool.TryParse(args[0], out bool goState))
            {
                Console.Log($"Boolean '{args[0]}' state is not valid.");
                return;
            }

            try
            {
                m_cachedGo.SetActive(goState);
                Console.Log($"Cached GO {m_cachedGo.name} state set to: {goState}.");
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static GameObject m_cachedGo = null;
    }
}
