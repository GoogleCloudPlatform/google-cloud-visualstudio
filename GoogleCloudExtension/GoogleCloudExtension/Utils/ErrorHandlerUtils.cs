﻿using Microsoft.VisualStudio;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    public static class ErrorHandlerUtils
    {
        public static void HandleExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (AggregateException ex)
            {
                Debug.WriteLine($"Uncaught aggregate exception: {ex.Message}");
                if (ErrorHandler.ContainsCriticalException(ex))
                {
                    throw;
                }
                UserPromptUtils.ExceptionPrompt(ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uncaught exception: {ex.Message}");
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }
                UserPromptUtils.ExceptionPrompt(ex);
            }
        }
    }
}
