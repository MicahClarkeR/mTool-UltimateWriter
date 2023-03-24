using mToolkitFrameworkExtensions;
using mToolkitFrameworkExtensions.Tool;
using mToolkitPlatformComponentLibrary;
using mToolkitPlatformComponentLibrary.Workspace.Files;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace UltimatePlanner.Files.Style
{
    public class StyleManager : mEasyToolOwned
    {
        private Dictionary<string, mWorkspaceFile> InternalStyles = new Dictionary<string, mWorkspaceFile>();

        public StyleManager(mTool tool) : base(tool)
        {
            mWorkspaceFile[] styles = mWorkspaceEtx.FindFiles(tool, "Styles", ".style");

            foreach (mWorkspaceFile file in styles)
            {
                InternalStyles.Add($"{tool.GUID}\\{file.FileInfo.Name.Replace(".style", "")}", file);
            }
        }

        public StyleElement? GetStyle(string name)
        {
            name = $"{Owner.GUID}\\{name}";

            if (InternalStyles.ContainsKey(name))
            {
                string content = InternalStyles[name].Stream.GetStringContents();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);
                return new StyleElement(XElement.Parse(doc.OuterXml));
            }

            return null;
        }

        public StyleElement CreateStyle(string name, double? fontSize, int? bold, int? italic, int? underline, int? align)
        {
            if (InternalStyles.ContainsKey(name))
            {
                InternalStyles[name].Delete();
                InternalStyles.Remove(name);
            }

            StyleElement style = new StyleElement(fontSize, bold, italic, underline, align);

            if(Owner != null)
            {
                mWorkspaceFile file = Owner.CurrentWorkspace.Create($"Styles\\{name}.style", style.ToString());

                name = $"{Owner.GUID}\\{name}";
                InternalStyles.Add(name, file);
            }

            return style;
        }

        public string[] GetStyles()
        {
            return InternalStyles.
                Select((e, s) =>
                (e.Key.StartsWith(Owner.GUID)) ? e.Value.FileInfo.Name.Replace(".style", "") : null).
                Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        public mTool GetOwner()
        {
            throw new System.NotImplementedException();
        }

        public class StyleElement
        {
            public readonly double FontSize = -1;
            public readonly int Bold = 1;
            public readonly int Italics = 1;
            public readonly int Underlined = 1;
            public readonly int Align = 1;

            private readonly XElement root;

            public StyleElement(XElement element)
            {
                root = element;
                FontSize = double.Parse(element.Element("FontSize").Value);
                Bold = int.Parse(element.Element("Bold").Value);
                Italics = int.Parse(element.Element("Italics").Value);
                Underlined = int.Parse(element.Element("Underlined").Value);
                Align = int.Parse(element.Element("Align").Value);
            }

            public StyleElement(double? fontSize, int? bold, int? italics, int? underlined, int? align)
            {
                FontSize = fontSize ?? -1;
                Bold = bold ?? 1;
                Italics = italics ?? 1;
                Underlined = underlined ?? 1;
                Align = align ?? 1;

                root = new XElement("Style",
                            new XElement("FontSize", FontSize),
                            new XElement("Bold", Bold),
                            new XElement("Italics", Italics),
                            new XElement("Underlined", Underlined),
                            new XElement("Align", Align));
            }

            public override string ToString()
            {
                return root.ToString();
            }
        }
    }
}
