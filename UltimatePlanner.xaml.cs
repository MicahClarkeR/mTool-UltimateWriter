using mToolkitFrameworkExtensions;
using mToolkitFrameworkExtensions.Windows;
using mToolkitPlatformComponentLibrary;
using mToolkitPlatformComponentLibrary.Pipelines;
using mToolkitPlatformComponentLibrary.Workspace.Files;
using mToolkitPlatformDesktopLauncher.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using static UltimatePlanner.Files.Style.StyleManager;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using UltimatePlanner.Files.Style;

namespace UltimatePlanner
{
    /// <summary>
    /// UserControl that serves as the main interface for the Ultimate Planner application.
    /// </summary>
    public partial class UltimatePlanner : UserControl, mToolOwned
    {
        // Use private fields instead of public ones, and use camelCase for naming
        private List<mWorkspaceFile> files = new List<mWorkspaceFile>();

        private readonly mTool Owner;
        public mWorkspaceFile? File;
        private StyleManager Styles;

        // Constructor
        public UltimatePlanner(mTool owner)
        {
            Owner = owner;
            InitializeComponent();
            LoadDocuments();

            if(files.Count > 0 )
                Initialise(Owner, files[0]);

            // Set up context menu
            Input.ContextMenu = new ContextMenu();

            // Create context menu items for bold, italic, and underline
            ContextMenuExt.CreateMenuItem(Input.ContextMenu, "Bold", (e, s) => ApplyPropertyToSelection(TextBlock.FontWeightProperty, FontWeights.Bold));
            ContextMenuExt.CreateMenuItem(Input.ContextMenu, "Italic", (e, s) => ApplyPropertyToSelection(TextBlock.FontStyleProperty, FontStyles.Italic));
            ContextMenuExt.CreateMenuItem(Input.ContextMenu, "Underline", (e, s) => ApplyPropertyToSelection(TextBlock.TextDecorationsProperty, TextDecorations.Underline));

            // Add separator and menu items for font size
            Input.ContextMenu.Items.Add(new Separator());
            MenuItem size = ContextMenuExt.CreateMenuItem(Input.ContextMenu, "Size");
            ContextMenuExt.CreateMenuItem(size, "Heading 1", (e, s) => ApplyPropertyToSelection(TextElement.FontSizeProperty, 24.0));
            ContextMenuExt.CreateMenuItem(size, "Heading 2", (e, s) => ApplyPropertyToSelection(TextElement.FontSizeProperty, 20.0));
            ContextMenuExt.CreateMenuItem(size, "Heading 3", (e, s) => ApplyPropertyToSelection(TextElement.FontSizeProperty, 18.0));
            ContextMenuExt.CreateMenuItem(size, "Paragraph", (e, s) => ApplyPropertyToSelection(TextElement.FontSizeProperty, 18.0));

            // Add separator and menu items for text alignment
            Input.ContextMenu.Items.Add(new Separator());
            MenuItem align = ContextMenuExt.CreateMenuItem(Input.ContextMenu, "Align Selection");
            ContextMenuExt.CreateMenuItem(align, "Left", (e, s) => ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Left));
            ContextMenuExt.CreateMenuItem(align, "Centre", (e, s) => ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            ContextMenuExt.CreateMenuItem(align, "Right", (e, s) => ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            ContextMenuExt.CreateMenuItem(align, "Justify", (e, s) => ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Justify));
        }

        // Method to load the list of documents
        private void LoadDocuments()
        {
            DocumentList.Items.Clear();

            // Use var to simplify code and make it more concise
            foreach (var file in Owner.CurrentWorkspace.GetFilesAt("Documents\\"))
            {
                var fileInfo = new FileInfo(file);

                // Use string.Equals instead of comparing strings with "=="
                if (string.Equals(fileInfo.Extension, ".rtf", StringComparison.OrdinalIgnoreCase))
                {
                    AddDocument(fileInfo.Name.Replace(".rtf", ""));
                }
            }
        }

        // Method to add a new document to the list
        private mWorkspaceFile AddDocument(string name)
        {

            var newFile = Owner.CurrentWorkspace.Create($"Documents\\{name}.rtf");
            files.Add(newFile);
            DocumentList.Items.Add(name);
            return newFile;
        }

        // Event handler for the "New Document" button click
        private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            string name = "";
            if (VariableUserControl.CreateWindow(ref name, "Set document title: ") && !string.IsNullOrEmpty(name))
            {
                var newFile = AddDocument(name);
                DocumentList.SelectedIndex = files.Count - 1;
                SetSelected(files.Count - 1);
            }
        }

        // Event handler for the "Save Document" button click
        private void SaveDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        // Event handler for the document list selection changed
        private void DocumentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSelected(DocumentList.SelectedIndex);
        }

        // Method to select a document from the list
        private void SetSelected(int index)
        {
            if (index >= 0 && index < DocumentList.Items.Count)
            {
                Save();
                Initialise(Owner, files[index]);
            }
        }

        // Event handler for the "Delete Document" button click
        private void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentList.SelectedIndex >= 0)
            {
                Save();

                // Use ternary operator to simplify code
                int next = (DocumentList.SelectedIndex + 1 >= files.Count) ? DocumentList.SelectedIndex - 1 : DocumentList.SelectedIndex + 1;

                File?.Delete();

                if (next != -1)
                {
                    Initialise(Owner, files[next]);
                }
                else
                {
                    File = null;
                }

                files.RemoveAt(DocumentList.SelectedIndex);
                DocumentList.Items.Remove(DocumentList.Items[DocumentList.SelectedIndex]);

                if (next != -1)
                {
                    DocumentList.SelectedIndex = next;
                }
            }
        }

        public mTool GetOwner()
        {
            throw new NotImplementedException();
        }

        // Applies a property to the selected text in the input control
        private void ApplyPropertyToSelection(DependencyProperty property, object value)
        {
            Input.Selection.ApplyPropertyValue(property, value);
        }

        /// <summary>
        /// Initializes the workspace file and loads its contents into the input document.
        /// </summary>
        /// <param name="file">The workspace file to load.</param>
        public void Initialise(mTool owner, mWorkspaceFile? file = null)
        {
            Styles = new StyleManager(owner);

            // If a file is provided, set it as the current file and load its contents into the input document.
            if (file != null)
            {
                File = file;

                TextRange range = new TextRange(Input.Document.ContentStart, Input.Document.ContentEnd);
                using (FileStream stream = File.Stream.GetFileStream())
                {
                    // If the file has content, load it into the input document; otherwise, create a new empty document.
                    if (stream.Length > 0)
                        range.Load(stream, DataFormats.Rtf);
                    else
                        Input.Document = new FlowDocument();
                }

                ReloadStyles();
            }
        }

        /// <summary>
        /// Saves the contents of the input document to the current workspace file.
        /// </summary>
        public void Save()
        {
            if (File != null)
            {
                TextRange range = new TextRange(Input.Document.ContentStart, Input.Document.ContentEnd);
                using (FileStream stream = File.Stream.GetFileStream())
                {
                    // Save the contents of the input document to the file.
                    range.Save(stream, DataFormats.Rtf);
                }

                // Send a status bar message indicating that the file was saved.
                XElement message = new XElement("message",
                    new XElement("text", $"Document '{File.FileInfo.Name}' has been saved."),
                    new XElement("timing", 3000));
                mFrameworkApplication.Pipelines.SendMessage<XElement>("statusbar", new mPipeMessage(message));
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            // If the user presses Ctrl+S, save the document.
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Save();
            }
            else if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                string name = "";
                if (VariableUserControl.CreateWindow(ref name, "Select a style", Styles.GetStyles().ToList()))
                {
                    StyleCombo.SelectedIndex = StyleCombo.Items.IndexOf(name);
                }
            }
        }

        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            string[] formats = e.DataObject.GetFormats();
            // Check if the data being pasted is of type text
            if (formats.Contains("Text") && !formats.Contains("Bitmap") && !formats.Contains("DeviceIndependentBitmap"))
            {
                // Get the plain text from the clipboard
                string plainText = e.DataObject.GetData(DataFormats.Text) as string;

                // Cancel the default paste operation
                e.CancelCommand();

                // Insert the plain text at the current caret position
                Input.Selection.Text = plainText;
            }
            else if (formats.Contains("Bitmap") || formats.Contains("DeviceIndependentBitmap"))
            {
                // Get the image from the clipboard
                BitmapSource image = e.DataObject.GetData(DataFormats.Bitmap) as BitmapSource;

                // Cancel the default paste operation
                e.CancelCommand();

                // Create an Image control and set the source
                Image imageControl = new Image
                {
                    Source = image,
                    MaxWidth = Math.Min(Input.ActualWidth - 200, image.PixelWidth), // Limit image width to RichTextBox width
                };

                // Create a new InlineUIContainer to hold the Image control
                InlineUIContainer inlineUIContainer = new InlineUIContainer(imageControl, Input.Selection.Start.GetNextContextPosition(LogicalDirection.Backward));

                // Insert the image at the current caret position
                Input.Document.Blocks.Add(new Paragraph(inlineUIContainer));
            }
            else
            {
                // If it's not text or an image, cancel the paste operation
                e.CancelCommand();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string sVal = e.AddedItems[0].ToString();
                double iVal = double.Parse(sVal.Substring(sVal.Length - 4, 2));

                ApplyPropertyToSelection(TextElement.FontSizeProperty, iVal);
            }
        }

        private void FontBoldButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.FontWeightProperty, (FontBoldButton?.IsChecked ?? false) ? FontWeights.Bold : FontWeights.Normal);
        }

        private void FontItalicButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.FontStyleProperty, (FontItalicButton?.IsChecked ?? false) ? FontStyles.Italic : FontStyles.Normal);
        }

        private void FontUnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            TextDecoration[] decs = InputGetPropertyCollection(Input.Selection, TextDecorations.Underline);
            if (decs.Length == 0)
            {
                ApplyPropertyToSelection(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
            }
            else
            {
                ApplyPropertyToSelection(TextBlock.TextDecorationsProperty, null);
            }

        }

        private void Input_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (Input.Selection != null)
            {
                FontItalicButton.IsChecked = SelectionHasProperty(Input.Selection, TextBlock.FontStyleProperty, FontStyles.Italic);
                FontBoldButton.IsChecked = SelectionHasProperty(Input.Selection, TextBlock.FontWeightProperty, FontWeights.Bold);

                object selectedTextFontSize = GetInputProperty(Input.Selection, TextBlock.FontSizeProperty);

                if (selectedTextFontSize is double)
                {
                    if (!HasMultipleFontSizes())
                    {
                        int fontSize = (int.Parse(selectedTextFontSize.ToString()));
                        int index = FontSizeCombo.Items.Count;

                        foreach (ComboBoxItem val in FontSizeCombo.Items)
                        {
                            if (val.Content.Equals($"{fontSize}pt"))
                                FontSizeCombo.SelectedIndex = FontSizeCombo.Items.Count - index;

                            index--;
                        }
                    }
                }
            }
        }

        private bool HasMultipleFontSizes()
        {
            TextRange selectedText = Input.Selection;
            if (selectedText.IsEmpty)
            {
                return false;
            }

            TextPointer pointer = selectedText.Start;
            double initialFontSize = GetFontSizeAtPointer(pointer);

            while (pointer != null && selectedText.Contains(pointer))
            {
                double currentFontSize = GetFontSizeAtPointer(pointer);
                if (initialFontSize != currentFontSize)
                {
                    return true;
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }

            return false;
        }

        private double GetFontSizeAtPointer(TextPointer pointer)
        {
            Inline parentInline = pointer.Parent as Inline;
            return parentInline != null ? parentInline.FontSize : 0;
        }

        private static bool SelectionHasProperty(TextSelection text, DependencyProperty property, object value)
        {
            object o = GetInputProperty(text, property);

            if (o is TextDecorationCollection && value.GetType() == typeof(TextDecorationCollection))
                return HasTextDecorationCollection((TextDecorationCollection)o, (TextDecorationCollection)value);
            else if (o.GetType() == value.GetType())
                return o.Equals(value);

            return false;

            bool HasTextDecorationCollection(TextDecorationCollection a, TextDecorationCollection b)
            {
                foreach (TextDecoration dec in a)
                {
                    foreach (TextDecoration dec2 in b)
                        if (dec == dec2)
                            return true;
                }

                return false;
            }
        }

        private static object GetInputProperty(TextSelection text, DependencyProperty property)
        {
            return text.GetPropertyValue(property);
        }

        private static TextDecoration[] InputGetPropertyCollection(TextSelection text, TextDecorationCollection ecs)
        {
            List<TextDecoration> list = new List<TextDecoration>();
            object suspect = text.GetPropertyValue(TextBlock.TextDecorationsProperty);

            if (suspect is not TextDecorationCollection)
                return list.ToArray();

            TextDecorationCollection selectedDecs = (TextDecorationCollection)suspect;

            foreach (TextDecoration dec in ecs)
            {
                for (int i = 0; i < selectedDecs.Count; i++)
                    if (selectedDecs[i] == dec) list.Add(selectedDecs[i]);
            }

            return list.ToArray();
        }

        private void AddStyleButton_Click(object sender, RoutedEventArgs e)
        {
            string name = "";

            if (VariableUserControl.CreateWindow(ref name, "Set style name", Styles.GetStyles().ToList(), true))
            {
                double fontSize = (double)GetInputProperty(Input.Selection, TextElement.FontSizeProperty);
                int bold = ((FontWeight)GetInputProperty(Input.Selection, TextBlock.FontWeightProperty)).ToOpenTypeWeight();
                int italics = SelectionHasProperty(Input.Selection, TextBlock.FontStyleProperty, FontStyles.Italic) ? 1 : 0;
                int underlined = -1;
                int align = (int)GetInputProperty(Input.Selection, TextBlock.TextAlignmentProperty);

                Styles.CreateStyle(name, fontSize, bold, italics, underlined, align);
                ReloadStyles();
            }
        }

        private void AlignLeftButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Left);
        }

        private void AlignCentreButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Center);
        }

        private void AlignRightButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Right);
        }

        private void AlignJustifyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, TextAlignment.Justify);
        }

        private bool ChangingStyleCombo = false;
        private void ReloadStyles()
        {
            string selected = StyleCombo.SelectedItem?.ToString() ?? string.Empty;
            StyleCombo.Items.Clear();

            foreach (string style in Styles.GetStyles())
                StyleCombo.Items.Add(style);

            if (ChangingStyleCombo = StyleCombo.Items.Contains(selected))
                StyleCombo.SelectedIndex = StyleCombo.Items.IndexOf(selected);
            ChangingStyleCombo = false;
        }

        private void StyleCombo_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (ChangingStyleCombo)
            {
                e.Handled = true;
                return;
            }

            if (StyleCombo.SelectedValue == null)
                return;

            StyleElement? style = Styles.GetStyle(StyleCombo.SelectedValue.ToString());
            ApplyPropertyToSelection(TextElement.FontWeightProperty, FontWeight.FromOpenTypeWeight((int)style.Bold));
            ApplyPropertyToSelection(TextBlock.FontStyleProperty, (style.Italics == 1) ? FontStyles.Italic : FontStyles.Normal);
            ApplyPropertyToSelection(TextElement.FontSizeProperty, style.FontSize);
            ApplyPropertyToSelection(TextBlock.TextAlignmentProperty, (TextAlignment)style.Align);
        }
    }
}
