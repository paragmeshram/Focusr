﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromaticBox.CountDownr.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.ComponentModel;

    public class MaskedTextBox : TextBox
    {
        public static DependencyProperty MaskProperty;

        static MaskedTextBox()
        {
            MaskProperty = DependencyProperty.Register("Mask",
                typeof(string), typeof(MaskedTextBox),
                new FrameworkPropertyMetadata(MaskChanged));

            FrameworkPropertyMetadata metadata = new FrameworkPropertyMetadata();
            metadata.CoerceValueCallback = CoerceText;
            TextProperty.OverrideMetadata(typeof(MaskedTextBox), metadata);

            CommandManager.RegisterClassCommandBinding(typeof(MaskedTextBox),
                new CommandBinding(ApplicationCommands.Paste, null));
        }

        private static void MaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox textBox = (MaskedTextBox)d;
            d.CoerceValue(TextProperty);
        }

        private static object CoerceText(DependencyObject d, object value)
        {
            MaskedTextBox textBox = (MaskedTextBox)d;
            MaskedTextProvider maskProvider = new MaskedTextProvider(textBox.Mask);
            maskProvider.Set((string)value);
            return maskProvider.ToDisplayString();
        }

        public MaskedTextBox()
            : base()
        {
            CommandBinding commandBinding1 = new CommandBinding(
                ApplicationCommands.Paste, null, this.SuppressCommand);
            this.CommandBindings.Add(commandBinding1);
            CommandBinding commandBinding2 = new CommandBinding(
                ApplicationCommands.Cut, null, this.SuppressCommand);
            this.CommandBindings.Add(commandBinding2);
        }

        private void SuppressCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        public string Mask
        {
            get { return (string) this.GetValue(MaskProperty); }
            set { this.SetValue(MaskProperty, value); }
        }

        public bool MaskCompleted
        {
            get
            {
                MaskedTextProvider maskProvider = this.GetMaskProvider();
                return maskProvider.MaskCompleted;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            this.OnKeyDown(e);

            MaskedTextProvider maskProvider = this.GetMaskProvider();
            int pos = this.SelectionStart;

            // Deleting a character (Delete key).
            // Currently this does nothing if you try to delete
            // a format character (unliked MaskedTextBox, which
            // deletes the next input character).
            // Could use our private SkipToEditableCharacter
            // method to change this behavior.
            if (e.Key == Key.Delete && pos < (this.Text.Length))
            {
                if (maskProvider.RemoveAt(pos))
                {
                    this.RefreshText(maskProvider, pos);
                }
                e.Handled = true;
            }

            // Deleting a character (backspace).
            // Currently this steps over a format character
            // (unliked MaskedTextBox, which steps over and
            // deletes the next input character).
            // Could use our private SkipToEditableCharacter
            // method to change this behavior.
            else if (e.Key == Key.Back)
            {
                if (pos > 0)
                {
                    pos--;
                    if (maskProvider.RemoveAt(pos))
                    {
                        this.RefreshText(maskProvider, pos);
                    }
                }
                e.Handled = true;
            }
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            MaskedTextProvider maskProvider = this.GetMaskProvider();
            int pos = this.SelectionStart;


            // Adding a character.
            if (pos < this.Text.Length)
            {
                pos = this.SkipToEditableCharacter(pos);

                // Overwrite mode is on.
                if (Keyboard.IsKeyToggled(Key.Insert))
                {
                    if (maskProvider.Replace(e.Text, pos))
                    {
                        pos++;
                    }
                }
                // Insert mode is on.
                else
                {
                    if (maskProvider.InsertAt(e.Text, pos))
                    {
                        pos++;
                    }
                }

                // Find the new cursor position.
                pos = this.SkipToEditableCharacter(pos);
            }
            this.RefreshText(maskProvider, pos);
            e.Handled = true;


            base.OnPreviewTextInput(e);
        }


        private void RefreshText(MaskedTextProvider maskProvider, int pos)
        {
            // Refresh string.            
            this.Text = maskProvider.ToDisplayString();

            // Position cursor.
            this.SelectionStart = pos;
        }
        private MaskedTextProvider GetMaskProvider()
        {
            MaskedTextProvider maskProvider = new MaskedTextProvider(this.Mask);
            maskProvider.Set(this.Text);
            return maskProvider;
        }

        // Finds the next non-mask character.
        private int SkipToEditableCharacter(int startPos)
        {
            MaskedTextProvider maskProvider = this.GetMaskProvider();

            int newPos = maskProvider.FindEditPositionFrom(startPos, true);
            if (newPos == -1)
            {
                // Already at the end of the string.
                return startPos;
            }
            else
            {
                return newPos;
            }
        }
    }
}
