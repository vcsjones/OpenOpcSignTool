using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OpenVsixSignTool
{
    public enum OpcContentTypeMode
    {
        Default,
        Override
    }

    public class OpcContentType
    {
        public string Extension { get; }
        public string ContentType { get; }
        public OpcContentTypeMode Mode { get; }

        public OpcContentType(string extension, string contentType, OpcContentTypeMode mode)
        {
            Extension = extension;
            ContentType = contentType;
            Mode = mode;
        }
    }

    public class OpcContentTypes : IList<OpcContentType>
    {
        private static readonly XNamespace _opcContentTypeNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
        private readonly List<OpcContentType> _contentTypes = new List<OpcContentType>();

        internal OpcContentTypes(XDocument document, bool isReadOnly)
        {
            IsReadOnly = isReadOnly;
            var defaults = document.Root.Elements(_opcContentTypeNamespace + "Default");
            var overrides = document.Root.Elements(_opcContentTypeNamespace + "Override");
            foreach(var @default in defaults)
            {
                ProcessElement(OpcContentTypeMode.Default, @default);
            }
            foreach (var @override in overrides)
            {
                ProcessElement(OpcContentTypeMode.Override, @override);
            }
        }

        public XDocument ToXml()
        {
            XName TranslateToElementName(OpcContentTypeMode mode)
            {
                switch(mode)
                {
                    case OpcContentTypeMode.Default:
                        return _opcContentTypeNamespace + "Default";
                    case OpcContentTypeMode.Override:
                        return _opcContentTypeNamespace + "Override";
                    default:
                        throw new ArgumentException($"Specified {nameof(OpcContentTypeMode)} is invalid.", nameof(mode));
                }
            }

            var document = new XDocument();
            var root = new XElement(_opcContentTypeNamespace + "Types");
            foreach(var contentType in _contentTypes)
            {
                var element = new XElement(TranslateToElementName(contentType.Mode));
                element.SetAttributeValue("Extension", contentType.Extension);
                element.SetAttributeValue("ContentType", contentType.ContentType);
                root.Add(element);
            }
            document.Add(root);
            return document;
        }

        internal OpcContentTypes(bool isReadOnly)
        {
            IsReadOnly = isReadOnly;
        }

        private void ProcessElement(OpcContentTypeMode mode, XElement element)
        {
            _contentTypes.Add(new OpcContentType(element.Attribute("Extension").Value, element.Attribute("ContentType").Value, mode));
        }

        public OpcContentType this[int index]
        {
            get => _contentTypes[index];
            set
            {
                AssertNotReadOnly();
                IsDirty = true;
                _contentTypes[index] = value;
            }
        }

        public int Count => _contentTypes.Count;

        public bool IsReadOnly { get; }

        public void Add(OpcContentType item)
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Add(item);
        }

        public void Clear()
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Clear();
        }

        public bool Contains(OpcContentType item) => _contentTypes.Contains(item);

        public void CopyTo(OpcContentType[] array, int arrayIndex) => _contentTypes.CopyTo(array, arrayIndex);

        public IEnumerator<OpcContentType> GetEnumerator() => _contentTypes.GetEnumerator();

        public int IndexOf(OpcContentType item) => _contentTypes.IndexOf(item);

        public void Insert(int index, OpcContentType item)
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Insert(index, item);
        }

        public bool Remove(OpcContentType item)
        {
            AssertNotReadOnly();
            return IsDirty = _contentTypes.Remove(item);
        }

        public void RemoveAt(int index)
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal bool IsDirty { get; set; }

        private void AssertNotReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Cannot update content types in a read only package. Please open the package in write mode.");
            }
        }
    }
}
