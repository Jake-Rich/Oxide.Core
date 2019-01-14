﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace uMod.Libraries
{
    /// <summary>
    /// Indicates that the specified function is a library function with a name
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class LibraryFunction : Attribute
    {
        /// <summary>
        /// Gets the name for the library function
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a library function using the methods name
        /// </summary>
        public LibraryFunction()
        {
        }

        /// <summary>
        /// Creates a library function using the given name
        /// </summary>
        /// <param name="name"></param>
        public LibraryFunction(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Indicates that the specified function is a library property with a name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class LibraryProperty : Attribute
    {
        /// <summary>
        /// Gets the name for the library property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Creates a library property using the methods name
        /// </summary>
        public LibraryProperty()
        {
        }

        /// <summary>
        /// Creates a library property using the given name
        /// </summary>
        /// <param name="name"></param>
        public LibraryProperty(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a library containing a set of functions for script languages to use
    /// </summary>
    public abstract class Library
    {
        public static implicit operator bool(Library library) => library != null;

        public static bool operator !(Library library) => !library;

        // Functions stored in this library
        private IDictionary<string, MethodInfo> functions;

        // Properties stored in this library
        private IDictionary<string, PropertyInfo> properties;

        /// <summary>
        /// Stores the last exception
        /// </summary>
        public Exception LastException { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the Library class
        /// </summary>
        public Library()
        {
            functions = new Dictionary<string, MethodInfo>();
            properties = new Dictionary<string, PropertyInfo>();

            Type type = GetType();

            foreach (MethodInfo method in type.GetMethods())
            {
                LibraryFunction attribute;
                try
                {
                    attribute = method.GetCustomAttributes(typeof(LibraryFunction), true).SingleOrDefault() as LibraryFunction;
                    if (attribute == null)
                    {
                        continue;
                    }
                }
                catch (TypeLoadException)
                {
                    continue; // Ignore rare exceptions caused by type information being loaded for all methods
                }
                string name = attribute.Name ?? method.Name;
                if (functions.ContainsKey(name))
                {
                    Interface.uMod.LogError($"{type.FullName} library tried to register an already registered function: {name}");
                }
                else
                {
                    functions[name] = method;
                }
            }

            foreach (PropertyInfo property in type.GetProperties())
            {
                LibraryProperty attribute;
                try
                {
                    attribute = property.GetCustomAttributes(typeof(LibraryProperty), true).SingleOrDefault() as LibraryProperty;
                    if (attribute == null)
                    {
                        continue;
                    }
                }
                catch (TypeLoadException)
                {
                    continue; // Ignore rare exceptions caused by type information being loaded for all properties
                }
                string name = attribute.Name ?? property.Name;
                if (properties.ContainsKey(name))
                {
                    Interface.uMod.LogError($"{type.FullName} library tried to register an already registered property: {name}");
                }
                else
                {
                    properties[name] = property;
                }
            }
        }

        /// <summary>
        /// Called to perform any library-specific clean up
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// Gets all function names in this library
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFunctionNames() => functions.Keys;

        /// <summary>
        /// Gets all property names in this library
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetPropertyNames() => properties.Keys;

        /// <summary>
        /// Gets a function by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MethodInfo GetFunction(string name)
        {
            return functions.TryGetValue(name, out MethodInfo info) ? info : null;
        }

        /// <summary>
        /// Gets a property by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PropertyInfo GetProperty(string name)
        {
            return properties.TryGetValue(name, out PropertyInfo info) ? info : null;
        }
    }
}
