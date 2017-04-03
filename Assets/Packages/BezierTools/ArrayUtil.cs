using UnityEngine;
using System.Collections;

namespace BezierTools {

	public static class ArrayUtil {
		public static void Insert<T>(ref T[] list, T newone, int at) {
			var expanded = new T[list.Length + 1];
			System.Array.Copy(list, expanded, at);
			System.Array.Copy(list, at, expanded, at + 1, list.Length - at);
			expanded[at] = newone;
			list = expanded;
		}

        public static void Insert<T>(ref T[] list, T[] newList, int at)
        {
            var expanded = new T[list.Length + newList.Length];
            System.Array.Copy(list, expanded, at);
            System.Array.Copy(list, at, expanded, at + newList.Length, list.Length - at);
            for (int i = 0; i < newList.Length; i++)
            {
                expanded[at + i] = newList[i];
            }
            list = expanded;
        }

        public static T Remove<T>(ref T[] list, int at) {
			var oldone = list[at];
			System.Array.Copy(list, at + 1, list, at, list.Length - (at + 1));
			System.Array.Resize(ref list, list.Length - 1);
			return oldone;
		}

        public static void Remove<T>(ref T[] list, int start, int end)
        {
            //Debug.Log("Before: Length " + list.Length + " start " + start + " end " + end);
            if(list.Length > (end + 1))
            {
                System.Array.Copy(list, end + 1, list, start, list.Length - (end + 1));
            }
            System.Array.Resize(ref list, list.Length - (end - start + 1));
            //Debug.Log("After: Length " + list.Length + " start " + start + " end " + end);
        }
    }
}