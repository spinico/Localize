namespace Spinico.Localize
{
    using System.Windows.Markup;

    /// <summary>
    /// Markup Extension used to dynamically set the Language property of an Markup element to the
    /// the current <see cref="CultureManager.Culture"/> property value.
    /// </summary>
    /// <remarks>
    /// The culture used for displaying data bound items is based on the Language property.  This
    /// extension allows you to dynamically change the language based on the current 
    /// <see cref="CultureManager.Culture"/>.
    /// </remarks>
    [MarkupExtensionReturnType(typeof(XmlLanguage))]
    public class Language : ManagedExtension
    {
        /// <summary>
        /// List of active extensions
        /// </summary>
        private static ExtensionManager _manager = new ExtensionManager(2);

        /// <summary>
        /// Creates an instance of the extension to set the language property for an
        /// element to the current <see cref="CultureManager.CurrentCulture"/> property value
        /// </summary>
        public Language() 
            : base(_manager) { }

        /// <summary>
        /// Return the <see cref="XmlLanguage"/> to use for the associated markup element 
        /// </summary>
        /// <returns>
        /// The <see cref="XmlLanguage"/> corresponding to the current 
        /// <see cref="CultureManager.Culture"/> property value
        /// </returns>
        protected override object GetValue()
        {
            return XmlLanguage.GetLanguage(CultureManager.CurrentCulture.IetfLanguageTag);
        }

        /// <summary>
        /// Use the extension manager to update all targets
        /// </summary>
        public static void UpdateAllTargets()
        {
            _manager.Update();
        }

    }
}
