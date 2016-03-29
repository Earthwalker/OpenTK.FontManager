//-----------------------------------------------------------------------
// <copyright file="TextRenderer.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenTK.FontManager
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using System.Runtime.InteropServices;
    using OpenTK.Graphics;
    using OpenTK.Graphics.OpenGL;

    /// <summary>
    /// Uses System.Drawing for 2d text rendering.
    /// </summary>
    public class TextRenderer : IDisposable
    {
        /// <summary>
        /// Text bitmap.
        /// </summary>
        private readonly Bitmap bmp;

        /// <summary>
        /// Graphics surface for rendering fonts.
        /// </summary>
        private readonly Graphics gfx;

        /// <summary>
        /// The texture id.
        /// </summary>
        private readonly int texture;

        /// <summary>
        /// The dirty region.
        /// </summary>
        private Rectangle dirtyRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextRenderer"/> class.
        /// </summary>
        /// <param name="size">The size of the backing store.</param>
        /// <exception cref="InvalidOperationException">
        /// No GraphicsContext is current on the calling thread.
        /// </exception>
        public TextRenderer(Vector2 size)
        {
            Contract.Requires(size.X * size.Y > 0);

            if (GraphicsContext.CurrentContext == null)
                throw new InvalidOperationException("No GraphicsContext is current on the calling thread.");

            bmp = new Bitmap((int)size.X, (int)size.Y, System.Drawing.Imaging.PixelFormat.Format32bppArgb); // Format32bppArgb);
            gfx = Graphics.FromImage(bmp);
            gfx.SmoothingMode = SmoothingMode.None;
            gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

            texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, (int)size.X, (int)size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        }

        /// <summary>
        /// Gets the height of the backing store in physical pixels.
        /// </summary>
        /// <value>The height of the backing store in physical pixels.</value>
        public int Height
        {
            get
            {
                return bmp.Height;
            }
        }

        /// <summary>
        /// Gets the width of the backing store in physical pixels.
        /// </summary>
        /// <value>The width of the backing store in physical pixels.</value>
        public int Width
        {
            get
            {
                return bmp.Width;
            }
        }

        /// <summary>
        /// Clear the backing store to the specified color.
        /// </summary>
        /// <param name="color">A <see cref="System.Drawing.Color"/>.</param>
        public void Clear(Color color)
        {
            // This does nothing, if alpha byte is 0. gfx.Clear(color); This also does nothing, if
            // alpha byte is 0. Or blends, if alpha byte is > 0. gfx.FillRectangle (new SolidBrush
            // (color), new RectangleF(0, 0, bmp.Width, bmp.Height));
            if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                return;

            // Lock the bitmap's bits.
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Get the address of the first line.
            var ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            var bitmapData = new byte[Math.Abs(bmpData.Stride) * bmp.Height];

            // Copy the ARGB values into the array.
            Marshal.Copy(ptr, bitmapData, 0, bitmapData.Length);

            for (int i = 0; i < bitmapData.Length; i += 4)
            {
                bitmapData[i] = color.B;
                bitmapData[i + 1] = color.G;
                bitmapData[i + 2] = color.R;
                bitmapData[i + 3] = color.A;
            }

            // Copy the ARGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, ptr, bitmapData.Length);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            dirtyRegion = new Rectangle(0, 0, bmp.Width, bmp.Height);
        }

        /// <summary>
        /// Release all resources used by the PaintEventArgs.
        /// </summary>
        /// <remarks>IDisposable implementation.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Draws this instance.
        /// </summary>
        public void Draw()
        {
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);

            UploadBitmap();
            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex2(Width, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex2(Width, Height);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, Height);

            GL.End();
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);
        }

        /// <summary>
        /// Draw the specified string to the backing store.
        /// </summary>
        /// <param name="text">The <see cref="System.String"/> to draw.</param>
        /// <param name="font">The <see cref="System.Drawing.Font"/> that will be used.</param>
        /// <param name="color">The <see cref="Color"/> that will be used.</param>
        /// <param name="point">
        /// The location of the text on the backing store, in 2d pixel coordinates. The origin (0,
        /// 0) lies at the top-left corner of the backing store.
        /// </param>
        /// <param name="postprocessForeground">
        /// Determine whether to post-process the foreground color.
        /// </param>
        public void DrawString(string text, Font font, Color color, PointF point, bool postprocessForeground)
        {
            using (var brush = new SolidBrush(color))
                gfx.DrawString(text, font, brush, point);

            // Update the region, tat contains changes. Determines the
            var size = gfx.MeasureString(text, font);
            dirtyRegion = Rectangle.Round(RectangleF.Union(dirtyRegion, new RectangleF(point, size)));

            if (postprocessForeground)
                PostprocessForeground(point, size, color);
        }

        /// <summary>
        /// Internal (inheritable) dispose by parent.
        /// </summary>
        /// <param name="managed">
        /// Determine whether Dispose() has been called by the user (true) or the runtime from
        /// inside the finalizer (false).
        /// </param>
        /// <remarks>If disposing equals false, no references to other objects should be called.</remarks>
        private void Dispose(bool managed)
        {
            if (managed)
            {
                bmp.Dispose();
                gfx.Dispose();
                if (GraphicsContext.CurrentContext != null)
                    GL.DeleteTexture(texture);
            }
        }

        /// <summary>
        /// Post-processes the foreground.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="size">The size.</param>
        /// <param name="targetColor">Color of the target.</param>
        private void PostprocessForeground(PointF point, SizeF size, Color targetColor)
        {
            if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                return;

            // Lock the bitmap's bits.
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Get the address of the first line.
            var ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            var bitmapData = new byte[Math.Abs(bmpData.Stride) * bmp.Height];

            // Copy the ARGB values into the array.
            Marshal.Copy(ptr, bitmapData, 0, bitmapData.Length);

            int predefB = bitmapData[0];
            int predefG = bitmapData[1];
            int predefR = bitmapData[2];

            var index = 0;
            var endY = Math.Max(0, (int)point.Y) + Math.Min(bmp.Height, (int)(size.Height + 0.49F));
            var endX = Math.Max(0, (int)point.X) + Math.Min(bmp.Width, (int)(size.Width + 0.49F));

            for (int scanRow = Math.Max(0, (int)point.Y); scanRow <= endY; scanRow++)
            {
                for (int scanCol = Math.Max(0, (int)point.X); scanCol <= endX; scanCol++)
                {
                    index = (scanRow * bmp.Width * 4) + (scanCol * 4);
                    if (bitmapData[index] == predefB && bitmapData[index + 1] == predefG && bitmapData[index + 2] == predefR)
                        continue;

                    // Calculate the margin from the background RGB color components to the
                    // foreground RGB color components.
                    var deltaB = Math.Abs(bitmapData[index] - targetColor.B);
                    var deltaG = Math.Abs(bitmapData[index + 1] - targetColor.G);
                    var deltaR = Math.Abs(bitmapData[index + 2] - targetColor.R);

                    // Determine the highest RGB color component margin.
                    var deltaM = Math.Max(deltaB, Math.Max(deltaG, deltaR));

                    // Apply the entire target color RGB component to prevent color falsification
                    // and the respectively other RGB color component margins proportional to
                    // brighten up.
                    bitmapData[index] = (byte)Math.Min(255, targetColor.B + (deltaR / 3) + (deltaG / 3));
                    bitmapData[index + 1] = (byte)Math.Min(255, targetColor.G + (deltaR / 3) + (deltaB / 3));
                    bitmapData[index + 2] = (byte)Math.Min(255, targetColor.R + (deltaG / 3) + (deltaB / 3));

                    // Now we have exactly the target color or the target color proportional to
                    // brighten up and can apply the highest RGB color component margin to the alpha byte.
                    bitmapData[index + 3] = (byte)Math.Min(255, 255 - deltaM);
                }
            }

            // Copy the ARGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, ptr, bitmapData.Length);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);
        }

        /// <summary>
        /// Uploads the dirty regions of the backing store to the OpenGL texture.
        /// </summary>
        private void UploadBitmap()
        {
            if (dirtyRegion != RectangleF.Empty)
            {
                var data = bmp.LockBits(dirtyRegion, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

                GL.BindTexture(TextureTarget.Texture2D, texture);

                if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                {
                    GL.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        dirtyRegion.X,
                        dirtyRegion.Y,
                        dirtyRegion.Width,
                        dirtyRegion.Height,
                        PixelFormat.Bgr,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }
                else
                {
                    GL.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        dirtyRegion.X,
                        dirtyRegion.Y,
                        dirtyRegion.Width,
                        dirtyRegion.Height,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        data.Scan0);
                }

                bmp.UnlockBits(data);

                dirtyRegion = Rectangle.Empty;
            }
        }
    }
}