namespace Spinico.Localize
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Markup;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Defines the handling method for the <see cref="Localize.GetResource"/> event
    /// </summary>
    /// <param name="resource">The resource file name</param>
    /// <param name="key">The resource key</param>
    /// <param name="culture">The culture to get the resource for</param>
    /// <returns>The resource</returns>
    public delegate object GetResourceHandler(string resource, string key, CultureInfo culture);

    /// <summary>
    /// A markup extension to allow resources object for WPF Windows 
    /// and controls to be retrieved from an embedded resource file (.resx)
    /// </summary>    
    [MarkupExtensionReturnType(typeof(object))]
    [ContentProperty("Children")]
    public class Localize : ManagedExtension, ILocalize
    {
        #region DefaultResource Attached Property

        /// <summary>
        /// The DefaultResource attached property
        /// </summary>
        public static readonly DependencyProperty DefaultResourceProperty =
            DependencyProperty.RegisterAttached("DefaultResource",
            typeof(string),
            typeof(Localize),
            new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.Inherits,
                    new PropertyChangedCallback(OnDefaultResourcePropertyChanged)));

        /// <summary>
        /// Get the DefaultResource attached property for the given target
        /// </summary>
        /// <param name="target">The Target object</param>
        /// <returns>The name of the default resource</returns>
        [AttachedPropertyBrowsableForType(typeof(FrameworkElement))]        
        public static string GetDefaultResource(DependencyObject target)
        {
            return (string)target.GetValue(DefaultResourceProperty);
        }

        /// <summary>
        /// Set the DefaultResource attached property for the given target
        /// </summary>
        /// <param name="target">The Target object</param>
        /// <param name="value">The name of the default resource</param>
        public static void SetDefaultResource(DependencyObject target, string value)
        {
            target.SetValue(DefaultResourceProperty, value);
        }

        /// <summary>
        /// Handle a change to the attached DefaultResource property
        /// </summary>
        /// <param name="element">The dependency object (a framework element)</param>
        /// <param name="args">The dependency property changed event arguments</param>
        /// <remarks>
        /// Update extension with the default resource        
        /// </remarks>
        private static void OnDefaultResourcePropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {            
            if (DesignerProperties.GetIsInDesignMode(element))
            {
                foreach (Localize extension in _manager.Extensions)
                {
                    // Force the resource manager to be reloaded when 
                    // the attached DefaultResource changes
                    extension._resourceManager = null;
                    extension._defaultResource = args.NewValue as string;

                    if (extension.IsTarget(element))
                    {
                        extension.UpdateTarget(element);
                    }
                }
            }
            else if (_manager.Extensions.Count > 0)
            {
                string resource = args.NewValue as string;

                // Update default resource for template and instances 
                // without dependency object target
                var extensions = _manager.Extensions.Where(x =>
                    (x as Localize)._defaultResource != resource &&
                    !(x.Target is DependencyObject));

                foreach (Localize extension in extensions)
                {
                    extension._resourceManager = null;
                    extension._defaultResource = resource;
                }
            }                    
        }

        #endregion DefaultResource Attached Property

        /// <summary>
        /// The explicitly set embedded resource name (if any)
        /// </summary>
        private string _resource;
        
        /// <summary>
        /// The key used to retrieve the resource
        /// </summary>
        /// <remarks>
        /// For string, this key is the default syntax form 
        /// </remarks>
        private string _key;

        /// <summary>
        /// The default resource (based on the attached property)
        /// </summary>
        private string _defaultResource;

        /// <summary>
        /// The resource manager
        /// </summary>
        /// <remarks>
        /// Holding a strong reference to the ResourceManager keeps 
        /// it in the cache while there are Localize instances 
        /// that are using it.
        /// </remarks>
        private ResourceManager _resourceManager;

        /// <summary>
        /// The children instances
        /// </summary>
        /// <remarks>
        /// This extension support nested instances
        /// </remarks>
        private Collection<Localize> _children = new Collection<Localize>();        

        /// <summary>
        /// The manager for Localize extensions
        /// </summary>
        private static ExtensionManager _manager = new ExtensionManager();

        /// <summary>
        /// Cached resource managers
        /// </summary>
        private static Dictionary<string, WeakReference> _resourceManagers = new Dictionary<string, WeakReference>();

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// This event allows a designer to intercept calls to get 
        /// resources and provide the values dynamically instead 
        /// </summary>
        public static event GetResourceHandler GetResource;

        /// <summary>
        /// The fully qualified name of the embedded resource file (without .resources) 
        /// </summary>
        public string Resource
        {
            get
            {
                // If the Resource property is not set explicitly 
                // then look up for the DefaultResource attached property
                string resource = _resource;
                
                if (string.IsNullOrEmpty(resource))
                {
                    object target = this.Root.Target;

                    if (_defaultResource == null)
                    {
                        _defaultResource = GetDefaultResource(target);
                    }
                    
                    resource = _defaultResource;
                }

                return resource;
            }

            set { _resource = value; }
        }

        /// <summary>
        /// Try to retrieve the default resource name for the given target object
        /// </summary>
        /// <param name="target"></param>
        /// <returns>The default resource name, null otherwise</returns>
        private string GetDefaultResource(object target)
        {
            string resource = null;

            if (target != null)
            {
                if (target is DependencyObject)
                {
                    resource = (target as DependencyObject).GetValue(DefaultResourceProperty) as string;
                }
            }

            return resource;
        }

        /// <summary>
        /// The root Localize object used by nested instances
        /// </summary>        
        private Localize _root;
        internal Localize Root
        {
            get
            {
                // Evaluate on first call only
                if (_root == null)
                {
                    _root = this;

                    while (_root.Parent != null)
                    {
                        if (_root.Parent is Localize)
                        {
                            _root = _root.Parent as Localize;
                        }
                    }
                }

                return _root;
            }
        }   

        /// <summary>
        /// The resource key (mapped to the extension constructor parameter)
        /// </summary>
        /// <remarks>
        /// For string, this key is the default syntax form
        /// </remarks>
        [ConstructorArgument("key")] 
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// Support for binding
        /// </summary>
        /// <remarks>
        /// When used with numeric quantity, this allow for scenarios where 
        /// the negative, plural or empty form needs to be applied depending 
        /// on the resolved value
        /// </remarks> 
        public Binding BindTo { get; set; }

        /// <summary>
        /// The empty resource key for quantity equal to zero
        /// </summary>
        /// <remarks>
        /// This property has no effect if <see cref="Localize.BindTo"/> 
        /// is not bound to a numeric quantity value
        /// </remarks>
        [DefaultValue(null)]
        public string EmptyKey { get; set; }

        /// <summary>
        /// The negative resource key for quantity less than zero
        /// </summary>
        /// <remarks>
        /// This property has no effect if <see cref="Localize.BindTo"/> 
        /// is not bound to a numeric quantity value
        /// </remarks>
        [DefaultValue(null)]
        public string NegativeKey { get; set; }

        /// <summary>
        /// The plural resource key for quantity of more than one
        /// </summary>
        /// <remarks>
        /// This property has no effect if <see cref="Localize.BindTo"/> 
        /// is not bound to a numeric quantity value
        /// </remarks>
        [DefaultValue(null)]
        public string PluralKey { get; set; }

        /// <summary>
        /// The resource key of the value to use when the bound value is null
        /// </summary>
        /// <remarks>
        /// This property has no effect if <see cref="Localize.BindTo"/> is not set
        /// </remarks>
        [DefaultValue(null)]
        public string NullKey { get; set; }

        /// <summary>
        /// The case format
        /// </summary>
        [DefaultValue(ECaseFormat.None)]
        public ECaseFormat CaseFormat { get; set; }

        /// <summary>
        /// The default value to use if the resource can't be found
        /// </summary>
        /// <remarks>
        /// This particularly useful for properties which require non-null
        /// values because it allows the page to be displayed even if
        /// the resource can't be loaded
        /// </remarks>
        [DefaultValue(null)]
        public object DefaultValue { get; set; }

        /// <summary>
        /// The children Localize instances (the Content property)
        /// </summary>
        /// <remarks>
        /// You can nest Localize extensions in this case the parent Localize element
        /// value is used as a format string to format the values from Localize child
        /// elements in a way similar to a <see cref="MultiBinding"/>. 
        /// </remarks>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<Localize> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Create a new instance of the markup extension
        /// </summary>
        public Localize() 
            : base(_manager) { }


        /// <summary>
        /// Create a new instance of the markup extension
        /// </summary>
        /// <param name="key">The key used to get the value from the resources</param>
        public Localize(string key) 
            : base(_manager)
        {
            _key = key;
        }

        #region ILocalize

        /// <summary>
        /// Return the localized string value given the resource name and a resource key
        /// </summary>
        /// <param name="resource">The resource name (including namespace) without the extension (.resx)</param>         
        /// <param name="key">The resource key</param>         
        /// <returns>The string value from the specified resource, otherwise a default string</returns>
        public string GetText(string resource, string key)
        {
            object value = GetValue(resource, key, typeof(string));

            return value == null ? "#" + key : value as string;
        }

        #endregion ILocalize        

        /// <summary>
        /// Return the value for this instance of the Markup Extension
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns>The value of the element</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Register the target and property so we can update them
            RegisterTarget(serviceProvider);

            if (string.IsNullOrEmpty(this.Key) && !this.IsBound)
                throw new ArgumentException("Resource key is missing and no binding defined.");

            // When the extension has no target (used in a template)
            // (Also occurs when used as a child element in .NET 4+)
            if (base.TargetProperty == null)
            {
                return this;
            }
  
            object value = GetValue();            
         
            if (this.IsBound)
            {
                MultiBinding wrapper = CreateMultiBindingWrapper(value);

                value = wrapper.ProvideValue(serviceProvider);
            }

            return value;
        }

        /// <summary>
        /// Use the Markup Manager to update all targets
        /// </summary>
        public static void UpdateAllTargets()
        {
            _manager.Update();
        }

        /// <summary>
        /// Update the given target when the culture changes
        /// </summary>
        /// <param name="target">The target to update</param>
        protected override void UpdateTarget(object target)
        {
            if (this.IsBound)
            {
                var element = target as FrameworkElement;

                if (element != null)
                {
                    var value = GetValue();

                    MultiBinding wrapper = CreateMultiBindingWrapper(value);

                    element.SetBinding(base.TargetProperty as DependencyProperty, wrapper);
                }
            }
            else
            {
                base.UpdateTarget(target);                
            }
        }

        /// <summary>
        /// Generate a wrapper on a hierarchy of binding nodes instance
        /// The wrapper is a <see cref="MultiBinding"/> instance with a converter
        /// that process the bindings resolved value using the given nodes
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A <see cref="MultiBinding"/> instance</returns>
        private MultiBinding CreateMultiBindingWrapper(object value)
        {
            BindingNode root = null;
            var bindings = new Collection<Binding>();

            ProcessBindings(value, ref root, ref bindings);

            var wrapper = new MultiBinding();

            foreach (var binding in bindings)
            {
                wrapper.Bindings.Add(binding);
            }
            
            wrapper.TargetNullValue = GetTargetNullValue(this);

            wrapper.Converter = new BindingNodeConverter(root);

            return wrapper;
        }

        /// <summary>
        /// Is this instance bound to something?
        /// </summary>
        private bool IsBound
        {
            get { return this.HasBinding || this.HasChildren; }
        }

        /// <summary>
        /// Is this instance binding property set?
        /// </summary>
        private bool HasBinding
        {
            get { return this.BindTo != null; }
        }

        /// <summary>
        /// Is this instance has one or more child instances?
        /// </summary>
        private bool HasChildren
        {
            get { return  _children.Count > 0; }
        }

        /// <summary>
        /// This method is called to process all bindings for a Localize 
        /// instance that is bound (i.e. has a binding defined or contain 
        /// one or more Localize children).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parent"></param>
        /// <param name="bindings"></param>
        /// <remarks>
        /// The method is called recursively for all child that has one or 
        /// more children Localize instance
        /// </remarks>
        private void ProcessBindings(object value, ref BindingNode parent, ref Collection<Binding> bindings)
        {
            BindingNode wrapper;
            BindingNode node = CreateBindingNode(this, value, bindings.Count);
            MultiBinding multiBinding = CreateMultiBinding(this, value);

            if (parent == null) // is root node?
            {
                parent = new BindingNode(multiBinding);
                wrapper = parent;
            }
            else
            {
                wrapper = new BindingNode(multiBinding); 
                parent.Add(wrapper);
            }            
                     
            bindings.Add(node.Binding); 
            multiBinding.Bindings.Add(node.Binding);
            
            wrapper.Add(node);   

            foreach (Localize child in _children)
            {
                // Children instances have no target: the root Localize 
                // instance is used to evaluate the nested child's value (if any)
                object childValue = GetValue(child, child.Key);

                if (child.HasChildren)
                {                    
                    child.ProcessBindings(childValue, ref wrapper, ref bindings);                    
                }
                else
                {
                    if (child.HasBinding)
                    {
                        // Recursive call
                        var childNode = CreateBindingNodeWrapper(child, childValue, bindings);

                        wrapper.Add(childNode);
                    }
                    else
                    {
                        var childNode = CreateBindingNode(child, childValue, bindings.Count);                       

                        bindings.Add(childNode.Binding);
                        multiBinding.Bindings.Add(childNode.Binding);

                        wrapper.Add(childNode);
                    }
                }
            }            
        }

        /// <summary>
        /// Create a <see cref="BindingNode"/> instance based on the given instance binding
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns>A <see cref="BindingNode"/> instance</returns>
        /// <remarks>
        /// A nested instance with no binding is not able to resolve its resource in design mode (always null)
        /// In this case, the value is forced to the instance's key (in design mode only)
        /// </remarks>
        private BindingNode CreateBindingNode(Localize instance, object value, int index)
        {
            Binding binding;

            if (instance.HasBinding)
            {
                binding = CloneBindingOf(instance);
            }
            else
            {
                value = base.IsInDesignMode ? "{#" + instance.Key + "}" : value;

                binding = new Binding()
                {
                    Source = (value is String) ? FormatCase(value as String, instance.CaseFormat) : value
                };
            }

            return new BindingNode(binding, index);
        }

        /// <summary>
        /// Create a wrapper on a child instance that has no children
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <param name="bindings"></param>
        /// <returns>The <see cref="BindingNode"/> wrapper instance</returns>
        private BindingNode CreateBindingNodeWrapper(Localize instance, object value, Collection<Binding> bindings)
        {
            Binding binding = CloneBindingOf(instance);
            MultiBinding multiBinding = CreateMultiBinding(instance, value);

            multiBinding.Bindings.Add(binding);

            var node = new BindingNode(multiBinding);

            node.Add(new BindingNode(binding, bindings.Count));

            // Must add after the new node is created to match index correctly
            bindings.Add(binding);

            return node;
        }


        /// <summary>
        /// Create a <see cref="MultiBinding"/> instance
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// The instance key is used as a design mode default value
        /// when set as the converter's parameter
        /// </remarks>
        private MultiBinding CreateMultiBinding(Localize instance, object value)
        {
            return new MultiBinding()
            {
                Mode = BindingMode.OneWay,
                Converter = new StringFormatConverter(instance.Key, FormatCase)
                {
                    Default  = value,
                    Negative = GetValue(instance, instance.NegativeKey),
                    Plural   = GetValue(instance, instance.PluralKey),
                    Empty    = GetValue(instance, instance.EmptyKey),
                    Null     = GetValue(instance, instance.NullKey),
                    Case     = instance.CaseFormat
                }
            };
        }

        /// <summary>
        /// Create a <see cref="Binding"/> replicat of the given instance's binding
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks>
        /// Bindings are immutable when active, they must be 
        /// recreated when culture changes
        /// </remarks>
        private Binding CloneBindingOf(Localize instance)
        {
            var binding = new Binding();

            var source = instance.BindTo;

            if (source.ElementName != null)
            {
                binding.ElementName = source.ElementName;
            }

            if (source.RelativeSource != null)
            {
                binding.RelativeSource = source.RelativeSource;
            }

            if (source.Source != null)
            {
                binding.Source = source.Source;
            }

            binding.StringFormat            = source.StringFormat;
            binding.TargetNullValue         = source.TargetNullValue;
            binding.AsyncState              = source.AsyncState;
            binding.BindingGroupName        = source.BindingGroupName;
            binding.BindsDirectlyToSource   = source.BindsDirectlyToSource;
            binding.Converter               = source.Converter;
            binding.ConverterCulture        = source.ConverterCulture;
            binding.ConverterParameter      = source.ConverterParameter;
            binding.FallbackValue           = source.FallbackValue;
            binding.IsAsync                 = source.IsAsync;
            binding.Mode                    = source.Mode;
            binding.NotifyOnSourceUpdated   = source.NotifyOnSourceUpdated;
            binding.NotifyOnTargetUpdated   = source.NotifyOnTargetUpdated;
            binding.NotifyOnValidationError = source.NotifyOnValidationError;
            binding.Path                    = source.Path;
            binding.XPath                   = source.XPath;
            binding.UpdateSourceTrigger     = source.UpdateSourceTrigger;
            binding.ValidatesOnDataErrors   = source.ValidatesOnDataErrors;
            binding.ValidatesOnExceptions   = source.ValidatesOnExceptions;

            foreach (ValidationRule rule in source.ValidationRules)
            {
                binding.ValidationRules.Add(rule);
            }
                                   
            return binding;
        }

        /// <summary>
        /// Get the target's null value from the current resource using the given key
        /// </summary>
        /// <param name="targetNullKey"></param>
        /// <returns></returns>
        private object GetTargetNullValue(Localize instance)
        {
            object value = GetValue(instance, instance.NullKey);

            return (value is String) ? FormatCase(value as String, instance.CaseFormat) : value;
        }

        /// <summary>
        /// Get the value associated with the current instance's key
        /// </summary>
        /// <returns>The value from the resources if possible, otherwise the default value</returns>        
        protected override object GetValue()
        {
            object value = GetValue(this.Resource, this.Key, base.TargetPropertyType);

            if (value == null)
            {                
                value = GetDefaultValue(this.Key, base.TargetPropertyType);
            }
            
            if (value is String)
            {
                value = FormatCase(value as String, this.CaseFormat);                                                              
            }

            return value;
        }

        /// <summary>
        /// Get the value associated to the given instance's key (if any)
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="key"></param>
        /// <returns>The value from the resources if possible, otherwise the default value</returns>
        /// <remarks>The instance may be nested, in this case the root parent is used to resolve the value</remarks>
        private object GetValue(Localize instance, string key)
        {
            if (key != null)
            {
                var root = instance.Root as Localize;

                string resource = instance.Resource ?? root.Resource;

                return root.GetValue(resource, key, root.TargetPropertyType);                
            }

            return null;
        }
        
        /// <summary>
        /// Get the localized value
        /// </summary>
        /// <param name="resource">The name of the embedded resx</param>
        /// <param name="key">The resource key</param>        
        /// <param name="targetType">The target property type</param>        
        /// <returns>The value from the resources if possible otherwise the default value</returns>
        /// <remarks>
        /// When an instance of the Localize extension is used a child, the key becomes optional 
        /// when a binding is used. In this scenario case, a default format string is used so the 
        /// value of the binding can be processed correctly.
        /// </remarks>
        private object GetValue(string resource, string key, Type targetType)
        {
            object value = null;

            // Key may be null when instance of Localize is used as a child instance
            if (key != null)
            {
                value = GetLocalizedResource(resource, key);

                if (value != null)
                {
                    try
                    {
                        value = ConvertTo(targetType, value);
                    }
                    catch { }
                }
            }

            return value;
        }

        /// <summary>
        /// Format the string value using the specified case format
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format">One of the supported <see cref="ECaseFormat"/></param>
        /// <returns>The case formated string</returns>
        private static string FormatCase(string value, ECaseFormat format)
        {
            if (value != null) 
            { 
                if (format == ECaseFormat.Lower)
                    return value.ToLower();

                if (format == ECaseFormat.Upper)
                    return value.ToUpper();

                if (format == ECaseFormat.Title)
                {
                    string[] words = value.Split(' ');
                    
                    for (int i = 0; i < words.Length; i++)
                    {
                        if (words[i].Length > 2 || i == 0)
                        {
                            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                        }
                        else if (words[i].Length > 1)
                        {
                            words[i] = char.ToLower(words[i][0]) + words[i].Substring(1);
                        }
                        else
                        {
                            words[i] = char.ToLower(words[i][0]) + string.Empty;
                        }
                    }

                    return String.Join(" ", words);
                }
            }

            return value;
        }

        /// <summary>
        /// Return the default value for the property
        /// </summary>
        /// <param name="key">The resource key</param>
        /// <param name="targetType">The target property type</param>
        /// <returns>A default value object</returns>
        private object GetDefaultValue(string key, Type targetType)
        {
            object value = this.DefaultValue;

            if (value == null)
            {
                if (targetType == typeof(String) || 
                    targetType == typeof(object))
                {
                    return string.IsNullOrEmpty(key) ? "{0}" : "#" + key;
                }
            }
            else
            {
                try
                {
                    value = ConvertTo(targetType, this.DefaultValue);
                }
                catch { }               
            }

            return value;
        }

        /// <summary>
        /// Return the localized resource for the current culture
        /// </summary>        
        /// <param name="resource">The resource name</param>
        /// <param name="key">The resource key</param>
        /// <returns>The resource instance for the current culture</returns>
        /// <remarks>Calls GetResource event first then if not handled, uses the resource manager</remarks>
        private object GetLocalizedResource(string resource, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            object instance = null;

            if (!string.IsNullOrEmpty(resource))
            {
                try
                {
                    if (GetResource != null)
                    {
                        instance = GetResource(resource, key, CultureManager.CurrentCulture);
                    }

                    if (instance == null)
                    {
                        if (_resourceManager == null)
                        {
                            _resourceManager = GetResourceManager(resource);
                        }

                        if (_resourceManager != null)
                        {
                            instance = _resourceManager.GetObject(key, CultureManager.CurrentCulture);
                        }
                    }
                }
                catch { }
            }

            return instance;
        }

        /// <summary>
        /// Convert a resource instance object to the value type required by the framework element
        /// </summary>        
        /// <param name="targetType">The target property type</param>
        /// <param name="instance">The resource value to convert</param> 
        /// <returns>The value for the required target type</returns>
        private static object ConvertTo(Type targetType, object instance)
        {
            object value = null;
            BitmapSource bitmapSource = null;

            // Convert icons and bitmaps to BitmapSource objects that WPF uses
            if (instance is Icon)
            {
                Icon icon = instance as Icon;

                // For icons we must create a new BitmapFrame from the icon data stream
                // The approach we use for bitmaps (below) doesn't work when setting the
                // Icon property of a window (although it will work for other Icons)
                using (MemoryStream iconStream = new MemoryStream())
                {
                    icon.Save(iconStream);
                    iconStream.Seek(0, SeekOrigin.Begin);
                    bitmapSource = BitmapFrame.Create(iconStream);
                }
            }
            else if (instance is Bitmap)
            {
                Bitmap bitmap = instance as Bitmap;
                IntPtr bitmapHandle = bitmap.GetHbitmap();
                bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero, Int32Rect.Empty,
                                                                     BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                DeleteObject(bitmapHandle);
            }

            if (bitmapSource != null)
            {
                // If the target property is expecting the Icon to be content then we
                // create an ImageControl and set its Source property to image
                if (targetType == typeof(object))
                {
                    System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image();
                    imageControl.Source = bitmapSource;
                    imageControl.Width = bitmapSource.Width;
                    imageControl.Height = bitmapSource.Height;
                    value = imageControl;
                }
                else
                {
                    value = bitmapSource;
                }
            }
            else
            {
                value = instance;

                // Allow for resources to either contain simple strings or typed data                
                if (targetType != null)
                {
                    if (instance is String && targetType != typeof(String) && targetType != typeof(object))
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(targetType);
                        value = tc.ConvertFromInvariantString(instance as string);                       
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Get the resource manager for this type of resource
        /// </summary>
        /// <param name="resource">The name of the embedded resources (resx)</param>
        /// <returns>The resource manager</returns>
        /// <remarks>It caches resource managers to improve performance</remarks>
        private static ResourceManager GetResourceManager(string resource)
        {
            WeakReference reference = null;
            ResourceManager manager = null;

            if (resource == null) return null;

            if (_resourceManagers.TryGetValue(resource, out reference))
            {
                manager = reference.Target as ResourceManager;

                // If the resource manager has been garbage collected 
                // then remove the cache entry 
                if (manager == null)
                {
                    _resourceManagers.Remove(resource);
                }
            }

            if (manager == null)
            {
                Assembly assembly = FindResourceAssembly(resource);

                if (assembly != null)
                {
                    manager = new ResourceManager(resource, assembly);
                }

                _resourceManagers.Add(resource, new WeakReference(manager));
            }

            return manager;
        }

        /// <summary>
        /// Find the assembly that contains the type of resource
        /// </summary>
        /// <param name="resource">The name of the embedded resource</param>
        /// <returns>The assembly if loaded (otherwise null)</returns>
        private static Assembly FindResourceAssembly(string resource)
        {
            // First check the entry assembly
            Assembly entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null &&
                HasEmbeddedResource(entryAssembly, resource))
            {
                return entryAssembly;
            }

            // Look in all the assemblies available otherwise
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (Assembly assembly in assemblies)
            {
                // Skip system assemblies and must contain resources                
                if (!IsSystemAssembly(assembly.FullName) &&
                    HasEmbeddedResource(assembly, resource))
                {
                    return assembly;
                }
            }

            return null;
        }        

        /// <summary>
        /// Check if assembly name match a system assembly's name
        /// </summary>
        /// <remarks>
        /// This will reduce searching
        /// </remarks>
        /// <param name="assemblyName"></param>
        /// <returns>true if match, false otherwise</returns>
        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("Microsoft.") ||
                   assemblyName.StartsWith("System.") ||
                   assemblyName.StartsWith("System,") ||
                   assemblyName.StartsWith("mscorlib,") ||
                   assemblyName.StartsWith("PresentationCore,") ||
                   assemblyName.StartsWith("PresentationFramework,") ||
                   assemblyName.StartsWith("UIAutomationProvider,") ||
                   assemblyName.StartsWith("WindowsBase,");
        }

        /// <summary>
        /// Check if the assembly contains an embedded resources of the given name
        /// </summary>
        /// <param name="assembly">The assembly to check</param>
        /// <param name="resource">The resource to look for</param>
        /// <returns>true if the assembly contains the resource</returns>
        private static bool HasEmbeddedResource(Assembly assembly, string resource)
        {
            // Guard against dynamic assembly
            if (IsDynamic(assembly))
            {
                return false;
            }

            try
            {
                string[] names = assembly.GetManifestResourceNames();
                
                resource = resource.ToLower() + ".resources";

                foreach (string name in names)
                {
                    if (name.ToLower() == resource)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // GetManifestResourceNames may throw an exception
                // Then ignore these assemblies
                Debug.Print("Exception in HasEmbeddedResource");
            }

            return false;
        }   
  
        /// <summary>
        /// Check for dynamic assemblies
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns>true if assembly is dynamic, false otherwise</returns>
        private static bool IsDynamic(Assembly assembly)
        {            
            // .NET 3.5
            return (assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder);

            // .NET 4+
            //return assembly.IsDynamic;
        }

    }
}
