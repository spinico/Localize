namespace Spinico.Localize
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines a class for managing <see cref="ManagedExtension"/> objects
    /// </summary>
    /// <remarks>
    /// This class provides a single point for updating active markup targets 
    /// that use the given MarkupExtension type managed by this class.   
    /// </remarks>
    public class ExtensionManager
    {
        /// <summary>
        /// The number of extensions registered since the last cleanup call
        /// </summary>
        private int _count;

        /// <summary>
        /// The registration count threshold at which to remove inactive extensions
        /// </summary>
        private int _threshold;

        /// <summary>
        /// The List of extensions
        /// </summary>
        private List<ManagedExtension> _extensions = new List<ManagedExtension>();

        /// <summary>
        /// The list of extensions property
        /// </summary>
        public List<ManagedExtension> Extensions
        {
            get { return _extensions; }
        }

        /// <summary>
        /// Create a new instance of the manager
        /// </summary>
        /// <param name="threshold">
        /// The limit quantity that trigger a removal of extensions 
        /// associated with garbage collected targets (default 50).
        /// </param>
        public ExtensionManager(int threshold = 50)
        {
            _threshold = threshold;
        }

        /// <summary>
        /// Update all active targets that use the markup extension
        /// </summary>
        /// <remarks>
        /// Creates a copy of the active targets to avoid exception if the list
        /// is changed while enumerating
        /// </remarks>
        public virtual void Update()
        {            
            var extensions = new List<ManagedExtension>(_extensions);

            foreach (var extension in extensions)
            {
                extension.UpdateTargets();
            }
        }

        /// <summary>
        /// Register a new extension and remove inactive extensions 
        /// which reference garbage collected target objects
        /// </summary>
        /// <param name="extension">The extension instance to register</param>
        internal void Register(ManagedExtension extension)
        {            
            if (_count > _threshold)
            {
                RemoveInactiveExtensions();
                _count = 0;
            }

            _extensions.Add(extension);
            _count++;
        }

        /// <summary>
        /// Remove extensions for targets which have been garbage collected.
        /// </summary>
        /// <remarks>
        /// This method is called periodically to release <see cref="ManagedExtension"/> 
        /// objects which are no longer required
        /// </remarks>
        private void RemoveInactiveExtensions()
        {
            int size = _extensions.Count;
            var extensions = new List<ManagedExtension>(size);
            
            foreach(var extension in _extensions)
            {
                if (extension.HasTarget)
                {
                    extensions.Add(extension);
                }
            }

            _extensions = extensions;
        }
    }
}
