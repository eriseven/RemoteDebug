using UnityEngine;

namespace RD
{
    public static class ObjectCommands
    {
        [Command("Object.SetGoActive", "Enable or disable GO. arg0: GO name - arg1: state")]
        public static void SetGOActive(string[] args)
        {
            if(args.Length < 2)
            {
                Console.Log("Invalid args. This command takes two arguments.");
                return;
            }

            GameObject go = GameObject.Find(args[0]);
            if(go == null)
            {
                Console.Log($"GameObject with name '{args[0]}' could not be find.");
                return;
            }

            if(!bool.TryParse(args[1], out bool goState))
            {
                Console.Log($"Boolean '{args[1]}' state is not valid.");
                return;
            }

            go.SetActive(goState);
            Console.Log($"GameObject '{args[0]}' active has been changed to {args[1]}");
            // TODO: Could make second arg not mandotory and toggle state if not passed.
        }

        [Command("Object.PrintGOChilds", "Prints a specified GO hierarchy.")]
        public static void PrintGOHierarchy(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Log("Invalid args. This command takes one arguments.");
                return;
            }

            GameObject go = GameObject.Find(args[0]);
            if(go == null)
            {
                Console.Log($"GameObject with name '{args[0]}' could not be find.");
                return;
            }

            PrintGOChilds(go, 0);
        }

        [Command("Object.SetGOBehaviourActive", "Enable or disable a Behaviour of specific GO. arg0 - GO name - arg1: Behaviour type - arg2: state")]
        public static void SetGOBehaviourActive(string[] args)
        {
            if(args.Length < 3)
            {
                Console.Log("Invalid args. This command takes three arguments.");
                return;
            }

            GameObject go = GameObject.Find(args[0]);
            if(go == null)
            {
                Console.Log($"GameObject with name '{args[0]}' could not be find.");
                return;
            }

            Behaviour behaviour = null;
            foreach(Behaviour b in go.GetComponents(typeof(Behaviour)))
            {
                if(b.GetType().ToString() == args[1])
                {
                    behaviour = b;
                    break;
                }
            }

            if(behaviour == null)
            {
                Console.Log($"Coponent of type {args[1]} could not be found at GO {args[0]}.");
                return;
            }

            if (!bool.TryParse(args[2], out bool behaviourState))
            {
                Console.Log($"Boolean '{args[2]}' state is not valid.");
                return;
            }

            behaviour.enabled = behaviourState;
        }

        // ==================================================================================================

        private static void PrintGOChilds(GameObject go, int indent)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < indent; ++i)
                sb.Append(" ");

            sb.Append("+");
            sb.Append(go.name);
            Console.Log(sb.ToString());

            for (int i = 0; i < go.transform.childCount; ++i)
                PrintGOChilds(go.transform.GetChild(i).gameObject, indent + 1);
        }
    }
}
