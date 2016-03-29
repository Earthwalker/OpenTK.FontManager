//-----------------------------------------------------------------------
// <copyright file="FontManager.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenTK.FontManager
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Linq;

    /// <summary>
    /// Manages the fonts.
    /// </summary>
    public class FontManager : IDisposable
    {
        /// <summary>
        /// The collection of font families.
        /// </summary>
        private readonly PrivateFontCollection fontCollection = new PrivateFontCollection();

        /// <summary>
        /// The loaded fonts.
        /// </summary>
        private readonly List<Font> fonts = new List<Font>();

        /// <summary>
        /// Gets or sets the font directory.
        /// </summary>
        /// <value>The font directory.</value>
        public static string Directory { get; set; } = Environment.CurrentDirectory + "\\Fonts\\";

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the font of the specified name and size.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="size">The size.</param>
        /// <returns>The matching <see cref="Font"/>.</returns>
        public Font GetFont(string name, int size = 0)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(size >= 0);

            var font = fonts.Find(f => string.Compare(f.Name, name, StringComparison.OrdinalIgnoreCase) == 0 && (size == 0 || (int)f.Size == size));

            // check if the font has been registered
            return font ?? LoadFont(name, size == 0 ? 12 : size);
        }

        /// <summary>
        /// Loads the <see cref="Font"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="size">The size.</param>
        /// <returns>The loaded <see cref="Font"/>.</returns>
        public Font LoadFont(string name, int size)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(size >= 0);

            // check if the font exists
            var font = fonts.Find(f => string.Compare(f.Name, name, StringComparison.OrdinalIgnoreCase) == 0 && (int)f.Size == size);

            if (font != null)
                return font;

            // try creating a system font
            font = new Font(name, size);

            // if that failed, try loading it from a file
            if (font.Name != name)
            {
                if (LoadFontFromFile(name, size) == null)
                    return font;
            }

            // add the new font to our collection
            fonts.Add(font);

            return font;
        }

        /// <summary>
        /// Loads the <see cref="Font"/>.
        /// </summary>
        /// <param name="family">The family.</param>
        /// <param name="size">The size.</param>
        /// <returns>The loaded <see cref="Font"/>.</returns>
        public Font LoadFont(FontFamily family, int size)
        {
            Contract.Requires(family != null);
            Contract.Requires(size >= 0);

            // check if the font exists
            var font = fonts.Find(f => string.Compare(f.Name, family.Name, StringComparison.OrdinalIgnoreCase) == 0 && (int)f.Size == size);

            if (font != null)
                return font;

            // try creating a system font
            font = new Font(family, size);

            // if that failed, try loading it from a file
            if (font.Name != family.Name)
            {
                if (LoadFontFromFile(family.Name, size) == null)
                    return font;
            }

            // add the new font to our collection
            fonts.Add(font);

            return font;
        }

        /// <summary>
        /// Loads the <see cref="Font"/> from file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="size">The size.</param>
        /// <returns>The loaded <see cref="Font"/>.</returns>
        public Font LoadFontFromFile(string filename, int size)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            Contract.Requires(size >= 0);

            // check if the font exists
            var font = fonts.Find(f => string.Compare(f.Name, filename, StringComparison.OrdinalIgnoreCase) == 0 && (int)f.Size == size);

            if (font != null)
                return font;

            // check if we've already loaded the font family
            var family = fontCollection.Families.FirstOrDefault(f => string.Compare(f.Name, filename, StringComparison.OrdinalIgnoreCase) == 0);

            if (family == null)
                fontCollection.AddFontFile(Directory + $@"\{filename}.ttf");

            // ensure it loaded successfully
            family = fontCollection.Families.FirstOrDefault(f => string.Compare(f.Name, filename, StringComparison.OrdinalIgnoreCase) == 0);

            if (family == null)
                return null;

            return LoadFont(family, size);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool managed)
        {
            if (managed)
                fontCollection.Dispose();
        }
    }
}