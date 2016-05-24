namespace Spinico.Localize
{
    using System;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// The culture manager is used to update the application's culture dynamically
    /// </summary>
    public static class CultureManager
    {
        /// <summary>
        /// Current culture of the application
        /// </summary>
        private static CultureInfo _culture;

        /// <summary>
        /// Raised when the <see cref="CurrentCulture"/> is changed
        /// </summary>
        /// <remarks>
        /// Since this event is static if the client object does not detach from the event a reference
        /// will be maintained to the client object preventing it from being garbage collected 
        /// (a potential memory leak)
        /// </remarks>
        public static event EventHandler CultureChanged;

        /// <summary>
        /// Sets the Culture for the application and raises the <see cref="CultureChanged"/>
        /// event causing any XAML elements using the <see cref="Localize"/> to automatically
        /// update
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                if (_culture == null)
                {
                    _culture = Thread.CurrentThread.CurrentUICulture;
                }

                return _culture;
            }

            set
            {
                if (_culture != value)
                {
                    _culture = value;

                    Thread.CurrentThread.CurrentUICulture = _culture;

                    SetThreadCulture(_culture);

                    Language.UpdateAllTargets();
                    Localize.UpdateAllTargets();

                    if (CultureChanged != null)
                    {
                        CultureChanged(null, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Set the thread culture to the given culture
        /// </summary>
        /// <param name="value">The culture to set</param>
        /// <remarks>If the culture is neutral then creates a specific culture</remarks>
        private static void SetThreadCulture(CultureInfo value)
        {
            Thread.CurrentThread.CurrentCulture = value.IsNeutralCulture ? 
                CultureInfo.CreateSpecificCulture(value.Name) : value;
        }

    }
}
