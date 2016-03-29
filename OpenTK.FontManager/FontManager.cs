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
    public static class FontManager
    {
        /// <summary>
        /// The collection of font families.
        /// </summary>
        private static readonly PrivateFontCollection fontCollection = new PrivateFontCollection();

        /// <summary>
        /// The loaded fonts.
        /// </summary>
        private static readonly List<Font> fonts = new List<Font>();

        /// <summary>
        /// Gets or sets the font directory.
        /// </summary>
        /// <value>The font directory.</value>
        public static string Directory { get; set; } = Environment.CurrentDirectory + "\\Fonts\\";

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public static void Dispose()
        {
            fontCollection.Dispose();
        }

        /// <summary>
        /// Gets the font of the specified name and size.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="size">The size.</param>
        /// <returns>The matching <see cref="Font"/>.</returns>
        public static Font GetFont(string name, int size = 0)
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
        public static Font LoadFont(string name, int size)
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
                var fileFont = LoadFontFromFile(name, size);

                if (fileFont != null)
                    return fileFont;
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
        public static Font LoadFont(FontFamily family, int size)
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
                var fileFont = LoadFontFromFile(family.Name, size);

                if (fileFont != null)
                    return fileFont;
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
        public static Font LoadFontFromFile(string filename, int size)
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
    }
}