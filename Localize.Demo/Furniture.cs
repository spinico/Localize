namespace Localize.Demo
{
    using System.ComponentModel;
    using System.Windows.Interop;

    public class Furniture : INotifyPropertyChanged
    {
        private int? _quantity = null;
        private InteropBitmap _picture;
        private string _name;
        private string _description;

        public int? Quantity {
            get { return _quantity; }
            set
            {
                _quantity = value;
                RaisePropertyChanged("Quantity");
            } 
        }

        public InteropBitmap Picture
        {
            get { return _picture; }
            set
            {
                _picture = value;
                RaisePropertyChanged("Picture");
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged("Description");
            }
        }

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
