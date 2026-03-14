/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Ui.Functions;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für TrackNumericUpDown.xaml
    /// </summary>
    public sealed partial class TrackNumericUpDown : UserControl
    {
        public EventHandler<int> OnValueChanged;
        public TrackNumericUpDown()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(TrackNumericUpDown), new PropertyMetadata(OnValueChangedCallBack));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static void OnValueChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TrackNumericUpDown c = sender as TrackNumericUpDown;
            c?.OnValueChangedC(c.Value);
        }

        private void OnValueChangedC(string c)
        {
            NumValue = Convert.ToInt32(c);
        }


        /* Track UP/Down */
        private int _numValue = 1;
        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                this.Text.Text = "T" + NumValue.ToString();
                return;
            }
        }
        private void NumUp_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackFunctions.CurrentSong == null)
                return;
            if (NumValue + 1 > PlaybackFunctions.CurrentSong.TrackContainers.Count)
                return;
            NumValue++;
            OnValueChanged?.Invoke(this, NumValue);
        }

        private void NumDown_Click(object sender, RoutedEventArgs e)
        {
            if (NumValue - 1 < 0)
                return;
            NumValue--;
            OnValueChanged?.Invoke(this, NumValue);
        }


        private void TextChanged_KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (Text == null)
                    return;

                string str = Regex.Replace(Text.Text, "[^0-9]", "");
                if (!int.TryParse(str, out var val))
                    return;
                if (PlaybackFunctions.CurrentSong == null)
                    return;

                if (val < 0 || val > PlaybackFunctions.CurrentSong.TrackContainers.Count)
                {
                    NumValue = NumValue;
                    return;
                }
                NumValue = val;
                OnValueChanged?.Invoke(this, NumValue);
            }
        }
    }
}
