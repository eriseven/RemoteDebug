using System;
using System.Text.RegularExpressions;

namespace RD
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public delegate void CallbackSimple();
        public delegate void Callback(string[] args);

        public CommandAttribute(string cmd, string help, bool runOnMainThread = true)
        {
            Command = cmd;
            Help = help;
            RunOnMainThread = runOnMainThread;
        }

        public string Command;
        public string Help;
        public bool RunOnMainThread;
        public Callback Cbk;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        public delegate void Callback(RequestContext context);

        public RouteAttribute(string route, string methods = @"(GET|HEAD)", bool runOnMainThread = true)
        {
            Route = new Regex(route, RegexOptions.IgnoreCase);
            Methods = new Regex(methods);
            RunOnMainThread = runOnMainThread;
        }

        public Regex Route;
        public Regex Methods;
        public bool RunOnMainThread;
        public Callback Cbk;
    }
}