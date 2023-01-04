using System.ComponentModel;

namespace BardMusicPlayer.Ui.MidiEdit.Utils
{
    /// heritate this instead of INotifyPropertyChanged
    /// prevent from implementing it in each model
    public class HandleBinding : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public  void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }
        
    }
}
