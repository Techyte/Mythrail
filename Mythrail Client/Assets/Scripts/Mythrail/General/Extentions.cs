using System;

namespace Mythrail.General
{
    public static class Extentions
    {
        public static T[] Copy<T>(this T[] array)
        {
            T[] copied = new T[array.Length];
            Array.Copy(array, 0, copied, 0, array.Length);
            return copied;
        }
    }
}