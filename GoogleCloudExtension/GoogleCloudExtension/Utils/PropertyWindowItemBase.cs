// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.ComponentModel;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class is to be used as the base of all of the items displayed in the Property Window in
    /// Visual Studio. This class implements enough of <seealso cref="ICustomTypeDescriptor"/> to provide
    /// the custom name, custom component and properties to the Property Window.
    /// The Properties Window will display the class name in normal text and the component name in bold.
    /// </summary>
    public abstract class PropertyWindowItemBase : ICustomTypeDescriptor
    {
        private readonly string _className;
        private readonly string _componentName;
        private readonly TypeConverter _converter;

        /// <summary>
        /// Initializes the item base to provide the name of the item, passed in the <paramref name="componentName"/> parameter
        /// and the kind of properties being displayed, passed in the <paramref name="className"/> parameter.
        /// </summary>
        /// <param name="className">The kind of properties being displayed.</param>
        /// <param name="componentName">The component name, typically the name of the item.</param>
        protected PropertyWindowItemBase(string className, string componentName)
        {
            _className = className;
            _componentName = componentName;
            _converter = TypeDescriptor.GetConverter(this, noCustomTypeDesc: false);
        }

        #region ICustomTypeDescriptor

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetClassName() => _className;

        string ICustomTypeDescriptor.GetComponentName() => _componentName;

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return _converter;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return _converter.GetProperties(this);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return _converter.GetProperties(null, null, attributes);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion
    }
}
