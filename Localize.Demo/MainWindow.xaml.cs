namespace Localize.Demo
{
    using Spinico.Localize;
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Windows;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            CultureManager.CultureChanged += new EventHandler(OnCultureChanged);            
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CultureManager.CultureChanged -= new EventHandler(OnCultureChanged);
        }

        private void OnCultureChanged(object sender, EventArgs e)
        {
            UpdateLanguageMenus();

        }

        private void UpdateLanguageMenus()
        {
            string lang = CultureManager.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            frenchMenuItem.IsChecked = (lang == "fr");
            englishMenuItem.IsChecked = (lang == "en");
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CultureManager.CurrentCulture = new CultureInfo("en");
        }

        private void frenchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CultureManager.CurrentCulture = new CultureInfo("fr");
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            CultureManager.CurrentCulture = Thread.CurrentThread.CurrentCulture;

            // Can localize text resource from code-behind
            var localize = new Localize();
            string title = localize.GetText("Localize.Demo.MainWindow", "mainWindowTitle");
        }
    }
}
