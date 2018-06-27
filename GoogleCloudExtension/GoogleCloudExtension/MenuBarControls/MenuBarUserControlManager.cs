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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GoogleCloudExtension.MenuBarControls
{
    [Export(typeof(MenuBarUserControlManager))]
    public class MenuBarUserControlManager
    {
        /// <summary>
        /// The wpf TemplatePart Name of the control on the menu bar that houses the microsoft account login widget.
        /// </summary>
        public const string MenuBarFrameControlContainerPartName = "PART_MenuBarFrameControlContainer";

        /// <summary>
        /// Adds the <see cref="GcpMenuBarControl"/> to the menu bar frame control container.
        /// </summary>
        public void ShowGcpInfo()
        {
            Window mainWindow = Application.Current.MainWindow;
            object control = mainWindow?.Template?.FindName(MenuBarFrameControlContainerPartName, mainWindow);
            if (control is ItemsControl itemsControl)
            {
                if (!itemsControl.ItemsSource.OfType<GcpMenuBarControl>().Any())
                {
                    ((ListCollectionView)itemsControl.ItemsSource).AddNewItem(new GcpMenuBarControl());
                }
            }
            else
            {
                throw new InvalidOperationException("Couldn't find target items control!");
            }
        }
    }
}
