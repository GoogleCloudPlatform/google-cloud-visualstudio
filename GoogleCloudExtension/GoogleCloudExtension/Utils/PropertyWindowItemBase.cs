using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
