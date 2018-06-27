// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils.Wpf
{
    public class EventCommandExtension : MarkupExtension
    {
        private BindableCommandDummy Dummy { get; } = new BindableCommandDummy();
        private BindingBase CommandBinding { get; }

        public object Arg { private get; set; }

        public BindingBase ArgBinding { private get; set; }

        public EventCommandExtension(BindingBase commandBinding)
        {
            CommandBinding = commandBinding;
        }

        /// <summary>When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension. </summary>
        /// <returns>The object value to set on the property where the extension is applied. </returns>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IProvideValueTarget target = serviceProvider as IProvideValueTarget ??
                serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (!(target?.TargetObject is FrameworkElement frameworkElement))
            {
                return this;
            }

            if (target.TargetProperty == null)
            {
                throw new InvalidOperationException("Target Property is null.");
            }

            var dataContextBinding = new Binding
            {
                Source = frameworkElement,
                Path = new PropertyPath(nameof(FrameworkElement.DataContext))
            };
            BindingOperations.SetBinding(Dummy, FrameworkElement.DataContextProperty, dataContextBinding);
            BindingOperations.SetBinding(Dummy, BindableCommandDummy.CommandProperty, CommandBinding);
            if (ArgBinding != null)
            {
                BindingOperations.SetBinding(Dummy, BindableCommandDummy.ArgProperty, ArgBinding);
            }
            else
            {
                Dummy.Arg = Arg;
            }
            Type handlerType = GetTargetHandlerType(target);
            return GetType().GetMethod(nameof(InvokeCommand), BindingFlags.NonPublic)?.CreateDelegate(handlerType, this);
        }

        private void InvokeCommand(object sender, EventArgs args) => Dummy.Command?.Execute(Arg ?? Dummy.Arg);

        private static Type GetTargetHandlerType(IProvideValueTarget target)
        {
            Type handlerType;
            switch (target.TargetProperty)
            {
                case EventInfo eventInfo:
                    handlerType = eventInfo.EventHandlerType;
                    break;
                case MethodInfo methodInfo:
                    handlerType = methodInfo.GetParameters().Select(p => p.ParameterType)
                        .FirstOrDefault(t => t.IsSubclassOf(typeof(MulticastDelegate)));
                    break;
                default:
                    handlerType = null;
                    break;
            }

            if (handlerType == null)
            {
                string typeName = target.TargetProperty.GetType().FullName;
                throw new InvalidOperationException(
                    $"Expected an event, but target {target.TargetProperty} was type {typeName}");
            }

            return handlerType;
        }

        private class BindableCommandDummy : FrameworkElement
        {
            public static readonly DependencyProperty CommandProperty =
                DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BindableCommandDummy));

            public static readonly DependencyProperty ArgProperty =
                DependencyProperty.Register(nameof(Arg), typeof(object), typeof(BindableCommandDummy));

            public ICommand Command
            {
                get => (ICommand)GetValue(CommandProperty);
                private set => SetValue(CommandProperty, value);
            }

            public object Arg
            {
                get => GetValue(ArgProperty);
                set => SetValue(ArgProperty, value);
            }
        }
    }
}
