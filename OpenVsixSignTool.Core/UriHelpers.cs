using System;

namespace OpenVsixSignTool
{
    public static class UriHelpers
    {
        private static Uri PackageBaseUri = new Uri("package:///", UriKind.Absolute);
        private static Uri RootedPackageBaseUri = new Uri("package:", UriKind.Absolute);


        /// <summary>
        /// Converts a package URI to a path within the package zip file.
        /// </summary>
        /// <param name="partUri">The URI to convert.</param>
        /// <returns>A string to the path in a zip file.</returns>
        public static string ToPackagePath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(PackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = PackageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }


        public static string ToQualifiedPath(this Uri partUri)
        {
            var absolute = partUri.IsAbsoluteUri ? partUri : new Uri(RootedPackageBaseUri, partUri);
            var pathUri = new Uri(absolute.GetComponents(UriComponents.SchemeAndServer | UriComponents.PathAndQuery, UriFormat.Unescaped), UriKind.Absolute);
            var resolved = RootedPackageBaseUri.MakeRelativeUri(pathUri);
            return resolved.ToString();
        }
    }
}
