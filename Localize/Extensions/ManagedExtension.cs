using System.Windows.Markup;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Spinico.Localize")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2007/xaml/presentation", "Spinico.Localize")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2008/xaml/presentation", "Spinico.Localize")]

namespace Spinico.Localize
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows;

    /// <summary>
    /// Defines a base class for markup extensions which are managed by a central 
    /// <see cref="ExtensionManager"/>. This allows the associated markup targets to be
    /// updated via the manager.
    /// </summary>
    /// <remarks>
    /// The ManagedExtension holds a weak reference to the target object to allow it to update 
    /// the target. A weak reference is used to avoid a circular dependency which would prevent the
    /// target being garbage collected.  
    /// </remarks>
    public abstract class ManagedExtension : MarkupExtension
    {        
        /// <summary>
        /// List of weak reference to the target DependencyObjects 
        /// to allow them to be garbage collected
        /// </summary>
        private List<WeakReference> _targetObjects = new List<WeakReference>();

        /// <summary>
        /// The target property 
        /// </summary>
        private object _targetProperty;

        /// <summary>
        /// The parent object reference
        /// </summary>
        internal object Parent { get; private set; }

        /// <summary>
        /// The target object the extension is associated with
        /// </summary>
        /// <remarks>
        /// For normal elements there will be a single target
        /// For templates there may be zero or more targets
        /// </remarks>
        internal object Target
        {
            get
            {
                WeakReference reference = _targetObjects.Find(target => target.IsAlive);

                return reference != null ? reference.Target : null;
            }
        }

        /// <summary>
        /// Create a new instance of the markup extension
        /// </summary>
        /// <param name="manager"></param>
        public ManagedExtension(ExtensionManager manager)
        {
            manager.Register(this);
        }

        /// <summary>
        /// Return the value for this instance of the Markup Extension
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns>The value of the element</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            RegisterTarget(serviceProvider);
         
            if (_targetProperty == null)
            {
                return this;
            }

            return GetValue();
        }

        /// <summary>
        /// Called by <see cref="ProvideValue(IServiceProvider)"/> to register the target 
        /// and object using the extension.   
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <remarks>
        /// Changes made in .NET 4 to the IProvideValueTarget broke 
        /// the support to obtain the target instance for nested extension. 
        /// The following worked in .NET 3.5, but not in .NET 4+ :
        ///    return base.TargetPropertyType == typeof(Collection<Localize>);    
        /// To make it work in .NET 4+, the parent instance is obtained using reflection.
        /// </remarks>        
        protected virtual void RegisterTarget(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            // Before attempting to use the service, make sure that the service itself is not null 
            // when returned by the relevant service provider 
            var service = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

            if (service != null)
            {
                if (service.TargetProperty == null)
                {
                    var parent = GetParent(service);
                    this.Parent = parent;
                }

                SetTarget(service.TargetObject, service.TargetProperty);
            }
        }

        /// <summary>
        /// Set the target object and target property to the current extension instance
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="targetProperty"></param>
        private void SetTarget(object targetObject, object targetProperty)
        {
            // Check if the target is a SharedDp which indicates the target is a template
            // In this case we don't register the target and ProvideValue returns "this"
            // allowing the extension to be evaluated for each instance of the template
            if (targetObject != null && targetObject.GetType().FullName != "System.Windows.SharedDp")
            {                
                _targetProperty = targetProperty;
                _targetObjects.Add(new WeakReference(targetObject));
            }
        }

        /// <summary>
        /// Obtain the parent instance using reflection
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method only works with the C# 4.0 XamlParser.
        /// This method will only be called once when the xaml is initially parsed.
        /// </remarks>
        private object GetParent(IProvideValueTarget service)
        {
            var xamlContextField = service.GetType().GetField("_xamlContext", BindingFlags.Instance | BindingFlags.NonPublic);

            if (xamlContextField != null)
            {
                var xamlContext = xamlContextField.GetValue(service);

                return xamlContext.GetType().GetProperty("GrandParentInstance").GetGetMethod().Invoke(xamlContext, null);
            }

            return null;
        }

        /// <summary>
        /// Update the associated targets
        /// </summary>
        internal void UpdateTargets()
        {
            foreach(WeakReference reference in _targetObjects)
            {
                if (reference.IsAlive)
                {
                    this.UpdateTarget(reference.Target);
                }
            }
        }

        /// <summary>
        /// Is the given object the target for the extension?
        /// </summary>
        /// <param name="target">The target to check</param>
        /// <returns>True, if the object is one of the targets for this extension</returns>
        internal bool IsTarget(object target)
        {
            foreach(WeakReference reference in _targetObjects)
            {
                if (reference.IsAlive && reference.Target == target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is this extension have at least one associated target(s) still alive (i.e. not garbage collected)
        /// </summary>
        internal bool HasTarget
        {
            get
            {
                // For normal elements the _targetObjects.Count will always be 1.
                // For templates "Count" may be zero if this method is called
                // in the middle of window creation (i.e. after the template has been
                // instantiated but before the elements that use it have been).
                // In this case return true so that we don't unhook the extension
                // prematurely
                if (_targetObjects.Count == 0)
                {
                    return true;
                }

                // Otherwise, check whether there is at least one referenced target(s) 
                // that is still alive
                foreach(WeakReference reference in this._targetObjects)
                {
                    if (reference.IsAlive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if a target attached to this extension is in design mode
        /// </summary>
        internal bool IsInDesignMode
        {
            get
            {
                foreach (WeakReference reference in _targetObjects)
                {
                    var element = reference.Target as DependencyObject;

                    if (element != null && DesignerProperties.GetIsInDesignMode(element))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Called by <see cref="UpdateTargets"/> to update each target 
        /// referenced by the extension
        /// </summary>
        /// <param name="target">The target to update</param>
        protected virtual void UpdateTarget(object target)
        {
            if (_targetProperty is DependencyProperty)
            {
                var dependencyObject = target as DependencyObject;

                // Make sure the dependency object is updatable (not sealed)
                if (dependencyObject != null && !dependencyObject.IsSealed)
                {
                    dependencyObject.SetValue(_targetProperty as DependencyProperty, GetValue());
                }
            }
            else if (_targetProperty is PropertyInfo)
            {
                // After a 'SetterBase' is in use (sealed), it cannot be modified.  
                if (!(target is SetterBase))
                {
                    var propertyInfo = _targetProperty as PropertyInfo;

                    propertyInfo.SetValue(target, GetValue(), null);
                }               
            }
        }

        /// <summary>
        /// Get the value associated with the key from the resource manager
        /// </summary>
        /// <returns>The value from the resources if possible, otherwise a default value</returns>
        protected abstract object GetValue();

        /// <summary>
        /// Get the Target Property of the associated extension
        /// </summary>
        /// <remarks>
        /// Can either be a <see cref="DependencyProperty"/> 
        /// or a <see cref="PropertyInfo"/> instance
        /// </remarks>
        protected object TargetProperty
        {
            get { return _targetProperty; }
        }

        /// <summary>
        /// Get the target property type
        /// </summary>
        protected Type TargetPropertyType
        {
            get
            {
                Type result = null;
                
                if (_targetProperty is DependencyProperty)
                    result = (_targetProperty as DependencyProperty).PropertyType;
                else if (_targetProperty is PropertyInfo)
                    result = (_targetProperty as PropertyInfo).PropertyType;
                else if (_targetProperty != null)
                    result = _targetProperty.GetType();

                return result;
            }
        }
    }
}
