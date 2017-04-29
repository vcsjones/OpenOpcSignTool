using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Represents a part inside of a package.
    /// </summary>
    public class OpcPart : IEquatable<OpcPart>
    {
        private readonly OpcPackageFileMode _mode;
        private readonly string _path;
        private readonly ZipArchiveEntry _entry;
        internal OpcRelationships _relationships;
        private readonly OpcPackage _package;

        public Uri Uri { get; }

        internal OpcPart(OpcPackage package, string path, ZipArchiveEntry entry, OpcPackageFileMode mode)
        {
            Uri = new Uri(OpcPackage.BasePackageUri, path);
            _package = package;
            _path = path;
            _entry = entry;
            _mode = mode;
        }

        public bool Equals(OpcPart other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return Uri.Equals(other.Uri);
        }

        public Stream Open() => _entry.Open();

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case OpcPart part: return Equals(part);
                default: return false;
            }
        }

        public OpcRelationships Relationships
        {
            get
            {
                if (_relationships == null)
                {
                    _relationships = ConstructRelationships();
                }
                return _relationships;
            }
        }

        public string ContentType
        {
            get
            {
                var extension = Path.GetExtension(_path)?.TrimStart('.');
                return _package.ContentTypes.FirstOrDefault(ct => string.Equals(ct.Extension, extension, StringComparison.OrdinalIgnoreCase))?.ContentType ?? OpcKnownMimeTypes.OctetString;
            }
        }

        private string GetRelationshipFilePath()
        {
            return Path.Combine(Path.GetDirectoryName(_path), "_rels/" + Path.GetFileName(_path) + ".rels").Replace('\\', '/');
        }

        private OpcRelationships ConstructRelationships()
        {
            var path = GetRelationshipFilePath();
            var entry = _package._archive.GetEntry(path);
            var readOnlyMode = _mode != OpcPackageFileMode.ReadWrite;
            var location = new Uri(OpcPackage.BasePackageUri, path);
            if (entry == null)
            {
                return new OpcRelationships(location, readOnlyMode);
            }
            else
            {
                using (var stream = entry.Open())
                {
                    return new OpcRelationships(location, XDocument.Load(stream, LoadOptions.PreserveWhitespace), readOnlyMode);
                }
            }
        }

        public override int GetHashCode() => Uri.GetHashCode();
    }
}