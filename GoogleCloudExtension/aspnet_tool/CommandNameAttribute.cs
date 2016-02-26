// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;

namespace AspnetTool
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CommandNameAttribute : Attribute
    {
        public string Name { get; }

        public CommandNameAttribute(string name)
        {
            Name = name;
        }
    }
}
