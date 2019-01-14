﻿using System;
using System.Collections.Generic;

namespace uMod
{
    public class ExceptionHandler
    {
        private static readonly Dictionary<Type, Func<Exception, string>> Handlers = new Dictionary<Type, Func<Exception, string>>();

        public static void RegisterType(Type ex, Func<Exception, string> handler) => Handlers[ex] = handler;

        public static string FormatException(Exception ex)
        {
            return Handlers.TryGetValue(ex.GetType(), out Func<Exception, string> func) ? func(ex) : null;
        }
    }
}
