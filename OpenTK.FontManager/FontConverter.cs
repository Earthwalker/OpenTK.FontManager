//-----------------------------------------------------------------------
// <copyright file="FontConverter.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace OpenTK.FontManager
{
    using System;
    using System.Drawing;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// FontConverter
    /// </summary>
    public class FontConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>Whether the type can be converted.</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Font);
        }

        /// <summary>
        /// Reads the json for the <see cref="Font"/> type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The <see cref="Font"/>.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            return FontManager.LoadFont(token["Name"].ToString(), (int)token["Size"]);
        }

        /// <summary>
        /// Writes the json for the <see cref="Font"/> type.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var font = (Font)value;

            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(font.Name);

            writer.WritePropertyName("Size");
            writer.WriteValue(font.Size);

            writer.WriteEndObject();
        }
    }
}
