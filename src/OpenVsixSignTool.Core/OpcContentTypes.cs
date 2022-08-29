using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Represents the mode of a content type.
    /// </summary>
    public enum OpcContentTypeMode
    {
        /// <summary>
        /// This is a default content type.
        /// </summary>
        Default,

        /// <summary>
        /// This is an overriding content type.
        /// </summary>
        Override
    }

    /// <summary>
    /// Represents a content type defined in a package.
    /// </summary>
    [DebuggerDisplay("Extension = {Extension}; ContentType = {ContentType};")]
    public class OpcContentType
    {
        /// <summary>
        /// The extension, without a leading period, of the content type.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// The MIME type of the content.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The mode of the content type. This can override a previously defined content type.
        /// </summary>
        public OpcContentTypeMode Mode { get; }

        /// <summary>
        /// Creates a new instance of a content type.
        /// </summary>
        /// <param name="extension">The extension, without a leading peroid, of the content type.</param>
        /// <param name="contentType">The MIME type of the content.</param>
        /// <param name="mode">The mode within the content type.</param>
        public OpcContentType(string extension, string contentType, OpcContentTypeMode mode)
        {
            Extension = extension;
            ContentType = contentType;
            Mode = mode;
        }
    }

    /// <summary>
    /// Represents a collection of content types in a package.
    /// </summary>
    public class OpcContentTypes : IList<OpcContentType>
    {
        private static readonly XNamespace _opcContentTypeNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
        private readonly List<OpcContentType> _contentTypes = new List<OpcContentType>();

        internal OpcContentTypes(XDocument document, bool isReadOnly)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            IsReadOnly = isReadOnly;
            var defaults = document.Root?.Elements(_opcContentTypeNamespace + "Default");
            var overrides = document.Root?.Elements(_opcContentTypeNamespace + "Override");
            if (defaults != null)
            {
                foreach (var @default in defaults)
                {
                    ProcessElement(OpcContentTypeMode.Default, @default);
                }
            }
            if (overrides != null)
            {
                foreach (var @override in overrides)
                {
                    ProcessElementOverride(OpcContentTypeMode.Override, @override);
                }
            }
        }

        /// <summary>
        /// Creates an XML representation of the content types to be placed in the package.
        /// </summary>
        /// <returns>An XML document representing the content types.</returns>
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
                if (contentType.Mode == OpcContentTypeMode.Default)
                {
                    element.SetAttributeValue("Extension", contentType.Extension);
                    element.SetAttributeValue("ContentType", contentType.ContentType);
                    root.Add(element);
                }                
            }

            foreach(var contentType in _contentTypes)
            {
                var element = new XElement(TranslateToElementName(contentType.Mode));
                if (contentType.Mode == OpcContentTypeMode.Override)
                {
                    element.SetAttributeValue("PartName", contentType.Extension);
                    element.SetAttributeValue("ContentType", contentType.ContentType);
                    root.Add(element);
                }                
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
            var extension = element.Attribute("Extension")?.Value;
            var contentType = element.Attribute("ContentType")?.Value;
            if (extension != null && contentType != null)
            {
                _contentTypes.Add(new OpcContentType(extension, contentType, mode));
            }
        }

        private void ProcessElementOverride(OpcContentTypeMode mode, XElement element)
        {
            var partName = element.Attribute("PartName")?.Value;
            var contentType = element.Attribute("ContentType")?.Value;
            if (partName != null && contentType != null)
            {
                _contentTypes.Add(new OpcContentType(partName, contentType, mode));
            }
        }

        /// <summary>
        /// Gets or sets a content type item by index.
        /// </summary>
        /// <param name="index">The index in the collection.</param>
        /// <returns>A content type instance.</returns>
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


        /// <summary>
        /// Gets the number of content types.
        /// </summary>
        public int Count => _contentTypes.Count;

        /// <summary>
        /// True if the content type collection is read only. This will be true if the package was opened in a read
        /// only mode. Attempting to modify the content types will result in an exception.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Adds a content type to the collection.
        /// </summary>
        /// <param name="item">The content type instance to add.</param>
        public void Add(OpcContentType item)
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Add(item);
        }

        /// <summary>
        /// Removes all content types.
        /// </summary>
        public void Clear()
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Clear();
        }


        /// <inheritdoc />
        public bool Contains(OpcContentType item) => _contentTypes.Contains(item);

        /// <inheritdoc />
        public void CopyTo(OpcContentType[] array, int arrayIndex) => _contentTypes.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<OpcContentType> GetEnumerator() => _contentTypes.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(OpcContentType item) => _contentTypes.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, OpcContentType item)
        {
            AssertNotReadOnly();
            IsDirty = true;
            _contentTypes.Insert(index, item);
        }

        /// <inheritdoc />
        public bool Remove(OpcContentType item)
        {
            AssertNotReadOnly();
            return IsDirty = _contentTypes.Remove(item);
        }

        /// <inheritdoc />
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
