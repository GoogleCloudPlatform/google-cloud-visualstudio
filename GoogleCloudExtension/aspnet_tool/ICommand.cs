// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace AspnetTool
{
    public interface ICommand
    {
        int Execute();
    }

    public interface ICommandOptions
    {
        ICommand CreateCommand();
    }
}
