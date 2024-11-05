using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace FTEditor
{
    static class Utils
    {
        public static byte ToByte(this float value)
        {
            var ret = (byte) value;
            Assert.IsTrue((value - ret) is < 0.0000001f and > -0.0000001f, $"value: {value}");
            return ret;
        }

        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<TKey> keys)
        {
            foreach (var key in keys)
                dict.Remove(key);
        }

        public static bool ColorEquals(Color32[] a, Color32[] b)
        {
            var len = a.Length;
            for (var i = 0; i < len; i++)
            {
                if (a[i].r != b[i].r) return false;
                if (a[i].g != b[i].g) return false;
                if (a[i].b != b[i].b) return false;
                if (a[i].a != b[i].a) return false;
            }

            return true;
        }
    }
}