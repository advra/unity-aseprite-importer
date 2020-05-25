using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AsepriteImporter
{
    public static class SerializationHelper
    {
        /// <summary>
        /// Serializes 'value' to a string, using BinaryFormatter
        /// </summary>
        public static string SerializeToString<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(stream, value);
                stream.Flush();
                return System.Convert.ToBase64String(stream.ToArray());
            }
        }
        /// <summary>
        /// Deserializes an object of type T from the string 'data'
        /// </summary>
        public static T DeserializeFromString<T>(string data)
        {
            byte[] bytes = System.Convert.FromBase64String(data);
            using (var stream = new MemoryStream(bytes))
                return (T)(new BinaryFormatter()).Deserialize(stream);
        }
    }
    /// <summary>
    /// A wrapper to serialize classes/structs that Unity can't
    /// </summary>
    public abstract class SerializedClass<T>
    {
        [SerializeField]
        private string serializedData = string.Empty;
        protected T _value;
        public SerializedClass() { }
        public SerializedClass(T value)
        {
            Value = value;
        }
        public bool IsNull { get { return this == null || Value == null; } }
        public T Value
        {
            get
            {
                if (_value == null)
                    _value = Deserialize();
                return _value;
            }
            set
            {
                if (_value == null || !_value.Equals(value))
                {
                    _value = value;
                    Serialize();
                }
            }
        }
        public string Data { get { return serializedData; } }
        public virtual void Serialize()
        {
            serializedData = _value == null ?
                string.Empty : SerializationHelper.SerializeToString<T>(_value);
        }
        protected virtual T Deserialize()
        {
            return string.IsNullOrEmpty(serializedData) ?
                default(T) : SerializationHelper.DeserializeFromString<T>(serializedData);
        }
    }
}
 