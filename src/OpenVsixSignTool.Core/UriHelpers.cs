using System;

namespace OpenVsixSignTool.Core
{
    public static class UriHelpers
    {
        private static readonly Uri _packageBaseUri = new Uri("package:///", UriKind.Absolute);
        private static readonly Uri _rootedPackageBaseUri = new Uri("package:", UriKind.Absolute);


        /// <summary>
        /// Converts a package URI to a path within the package zip file.
        /// </summary>
        /// <param name="partUri">The URI to convert.</param>
        /// <returns>A string to the path in a zip file.</returns>
        public static string ToPackagePath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(_packageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = _packageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }

        public static string ToQualifiedPath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(_rootedPackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = _rootedPackageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }

        public static Uri ToQualifiedUri(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(_rootedPackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped), UriKind.Absolute);
            return _rootedPackageBaseUri.MakeRelativeUri(pathUri);
        }
    }
}
