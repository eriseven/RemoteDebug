using UnityEngine;
using System.Reflection;

/**
 * Example console commands for getting information about GameObjects
 */
public static class GameObjectCommands
{

    [RD.Command("object.list", "lists all the game objects in the scene")]
    public static void ListGameObjects()
    {
        Object[] objects = Object.FindObjectsOfType(typeof(GameObject));
        foreach (Object obj in objects)
        {
            RD.Console.Log(obj.name);
        }
    }

    [RD.Command("object.print", "lists properties of the object")]
    public static void PrintGameObject(string[] args)
    {
        if (args.Length < 1)
        {
            RD.Console.Log( "expected : object print <Object Name>" );
            return;
        }
        
        GameObject obj = GameObject.Find(args[0]);
        if (obj == null)
        {
            RD.Console.Log("GameObject not found : " + args[0]);
        }
        else
        {
            RD.Console.Log("Game Object : " + obj.name);
            foreach (Component component in obj.GetComponents(typeof(Component)))
            {
                RD.Console.Log("  Component : " + component.GetType());
                foreach (FieldInfo f in component.GetType().GetFields())
                {
                    RD.Console.Log("    " + f.Name + " : " + f.GetValue(component));
                }
            }
        }
    }

    [RD.Command("test.error", "logs an error")]
    public static void TestError()
    {
        Debug.LogError("Testing error log");
    }

    [RD.Command("test.warn", "logs a warn")]
    public static void TestWarning()
    {
        Debug.LogWarning("Testing warning log");
    }

    [RD.Command("test.log", "logs a log")]
    public static void TestLog()
    {
        Debug.Log("Testing log");
    }
}



/**
 * Example console route for getting information about GameObjects
 *
 */
public static class GameObjectRoutes
{
    [RD.Route("^/object/list.json$", @"(GET|HEAD)", true)]
    public static void ListGameObjects(RD.RequestContext context)
    {
        string json = "[";
        Object[] objects = Object.FindObjectsOfType(typeof(GameObject));
        foreach (Object obj in objects)
        {
            // FIXME object names need to be escaped.. use minijson or similar
            json += string.Format("\"{0}\", ", obj.name);
        }

        json = json.TrimEnd(new char[]{',', ' '}) + "]";
        
        context.Response.WriteString(json, "application/json");
    }
}
