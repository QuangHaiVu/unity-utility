using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Random = System.Random;

namespace UnityModule.Utility
{
    public static partial class Util
    {
        #region Support

        /// <summary>
        /// return point for value in [minPointValue -> maxPointValue]
        /// </summary>
        /// <param name="min">value min compare</param>
        /// <param name="max">value max compare</param>
        /// <param name="value">value compare</param>
        /// <param name="minPointValue">min point for min value</param>
        /// <param name="maxPointValue">max point for max value</param>
        /// <returns></returns>
        public static float ClampTable(float value, float min, float max, float minPointValue, float maxPointValue)
        {
            if (value <= min)
            {
                return minPointValue;
            }

            if (value >= max)
            {
                return maxPointValue;
            }

            var delta = max - min;
            if (delta <= 0)
            {
                return 0;
            }

            return LinearRemap(value, min, max, minPointValue, maxPointValue);
        }

        public static float LinearRemap(this float value, float valueRangeMin, float valueRangeMax, float newRangeMin, float newRangeMax)
        {
            return (value - valueRangeMin) / (valueRangeMax - valueRangeMin) * (newRangeMax - newRangeMin) + newRangeMin;
        }

        #endregion

        #region Convert

        public static T ParseEnumType<T>(this string value) where T : struct
        {
            Enum.TryParse(value, out T type);
            return type;
        }

        #endregion

        #region Collection

        public static bool Add<T>(this ICollection<T> collection, T value)
        {
            if (collection.Exists(value))
            {
                return false;
            }

            collection.Add(value);
            return true;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
            {
                return true;
            }

            return !collection.Any();
        }

        public static bool Compare<T>(T x, T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public static bool Exists<T>(this IEnumerable<T> collection, T value)
        {
            var enumerable = collection as T[] ?? collection.ToArray();
            return enumerable.Any() && enumerable.Any(item => Compare(item, value));
        }

        public static (int, T) MaxIndex<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            if (collection == null) return (-1, default(T));
            var comparables = collection.ToList();
            var result = comparables.Max();
            return (comparables.IndexOf(result), result);
        }

        public static (int, T) MinIndex<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            if (collection == null) return (-1, default(T));
            var comparables = collection.ToList();
            var result = comparables.Min();
            return (comparables.IndexOf(result), result);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var r = new Random();
            return source.OrderBy(_ => r.Next()).ToList();
        }

        public static IDictionary<T1, T2> Shuffle<T1, T2>(this IDictionary<T1, T2> source)
        {
            var r = new Random();
            return source.OrderBy(x => r.Next()).ToDictionary(item => item.Key, item => item.Value);
        }

        public static IEnumerable<T> UniqueItem<T>(this IEnumerable<T> collection)
        {
            return collection.Select(x => x).Distinct().ToList();
        }

        public static bool Add<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }

        public static Dictionary<TKey, TValue> MakeDictionary<TKey, TValue>(this IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            return keys.Zip(values, (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
        }

        #endregion

        #region Transform

        /// <summary>
        /// Convert postion in world to position in canvas 'canvasRectTransform'
        /// </summary>
        /// <param name="canvasRectTransform"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector2 ToCanvasPosition(this Vector3 position, RectTransform canvasRectTransform)
        {
            if (Camera.main == null)
            {
                return Vector2.zero;
            }

            var viewport = Camera.main.WorldToViewportPoint(position);
            var sizeDelta = canvasRectTransform.sizeDelta;
            return new Vector3(viewport.x * sizeDelta.x - sizeDelta.x / 2, viewport.y * sizeDelta.y - sizeDelta.y / 2, viewport.z);
        }

        /// <summary>
        /// Convert position in canvas 'canvasRectTransform' to position in world
        /// </summary>
        /// <param name="position"></param>
        /// <param name="canvasRectTransform"></param>
        /// <returns></returns>
        public static Vector2 ToWorldPosition(this Vector3 position, RectTransform canvasRectTransform)
        {
            if (Camera.main == null)
            {
                return Vector2.zero;
            }

            var sizeDelta = canvasRectTransform.sizeDelta;
            return Camera.main.ViewportToWorldPoint(new Vector3((position.x + sizeDelta.x / 2) / sizeDelta.x, (position.y + sizeDelta.y / 2) / sizeDelta.y));
        }

        public static Vector2 ConvertRectTransform(this RectTransform target)
        {
            var rect = target.rect;
            var pivot = target.pivot;
            var fromPivotDerivedOffset = new Vector2(rect.width * pivot.x + rect.xMin, rect.height * pivot.y + rect.yMin);
            return RectTransformUtility.WorldToScreenPoint(null, target.position) + fromPivotDerivedOffset;
        }

        /// <summary>
        /// Converts the anchoredPosition of the first RectTransform to the second RectTransform,
        /// taking into consideration offset, anchors and pivot, and returns the new anchoredPosition
        /// return 'to.anchoredPosition + localPoint - pivotDerivedOffset'
        /// </summary>
        public static Vector2 SwitchToRectTransform(this RectTransform from, RectTransform to)
        {
            var screenP = from.ConvertRectTransform();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out var localPoint);
            var rect = to.rect;
            var pivot = to.pivot;
            var pivotDerivedOffset = new Vector2(rect.width * pivot.x + rect.xMin, rect.height * pivot.y + rect.yMin);
            return to.anchoredPosition + localPoint - pivotDerivedOffset;
        }

        /// <summary>
        /// Converts the anchoredPosition of the first RectTransform to the second RectTransform,
        /// taking into consideration offset, anchors and pivot, and returns the new anchoredPosition
        /// return 'localPoint - pivotDerivedOffset'
        /// </summary>
        public static Vector2 SwitchToRectTransform2(this RectTransform from, RectTransform to)
        {
            var screenP = from.ConvertRectTransform();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out var localPoint);
            var rect = to.rect;
            var pivot = to.pivot;
            var pivotDerivedOffset = new Vector2(rect.width * pivot.x + rect.xMin, rect.height * pivot.y + rect.yMin);
            return localPoint - pivotDerivedOffset;
        }

        #endregion

        #region Serialize

        public static bool ExistsFile(string path, string name)
        {
            return File.Exists($"{path}/{name}");
        }

        public static void LoadPersident<T>(string path, string name, T data)
        {
            if (!ExistsFile(path, name)) return;
            var bf = new BinaryFormatter();
            var file = File.Open($"{path}/{name}", FileMode.Open);
            JsonUtility.FromJsonOverwrite((string) bf.Deserialize(file), data);
            file.Close();
        }

        public static void SavePersident<T>(string path, string name, T data)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var bf = new BinaryFormatter();
            var file = File.Create($"{path}/{name}");
            var json = JsonUtility.ToJson(data);
            bf.Serialize(file, json);
            file.Close();
        }

        public static void SaveJsonPersident(string path, string name, string data)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var bf = new BinaryFormatter();
            var file = File.Create($"{path}/{name}");
            bf.Serialize(file, data);
            file.Close();
        }

        public static void LoadJsonPersident(string path, string name, ref string data)
        {
            if (!ExistsFile(path, name)) return;
            var bf = new BinaryFormatter();
            var file = File.Open($"{path}/{name}", FileMode.Open);
            data = (string) bf.Deserialize(file);
            file.Close();
        }

        public static void RemovePersident<T>(string path, string name)
        {
            if (ExistsFile(path, name))
            {
                File.Delete($"{path}/{name}");
            }
        }

        public static void CopyToClipboard(this string str)
        {
            var textEditor = new TextEditor {text = str};
            textEditor.SelectAll();
            textEditor.Copy();
        }

        #endregion

        #region Random

/// <summary>
        /// divide the param into "numberDevide" so that it is not greater than "maxValueOf"
        /// </summary>
        /// <param name="param"></param>
        /// <param name="maxValueOf"></param>
        /// <param name="numberDevide"></param>
        /// <returns></returns>
        public static List<int> GenerateRandom(int param, int minValueOf, int maxValueOf, int numberDevide)
        {
            var result = new List<int>();
            if (param < numberDevide)
            {
                for (int i = 0; i < numberDevide; i++)
                {
                    result.Add(1);
                }

                return result;
            }

            for (var i = 0; i < numberDevide; i++)
            {
                result.Add(UnityEngine.Random.Range(0, param));
            }

            var t = result.Sum() / (float) param;

            bool check = false;
            for (var i = 0; i < result.Count; i++)
            {
                result[i] = (int) (result[i] / t);
                if (result[i] == 0)
                {
                    check = true;
                    result[i]++;
                }
                else if (result[i] > minValueOf)
                {
                    if (check)
                    {
                        result[i]--;
                    }

                    if (result[i] > maxValueOf)
                    {
                        result[i] = maxValueOf;
                    }
                }
            }

            var origin = new List<int>();
            foreach (var i in result)
            {
                origin.Add(i);
            }

            var distance = param - result.Sum();
            if (distance > 0)
            {
                bool checkDistance = true;
                while (checkDistance)
                {
                    for (var j = 0; j < result.Count; j++)
                    {
                        if (result[j] >= maxValueOf) continue;
                        result[j]++;
                        distance--;

                        if (distance == 0)
                        {
                            break;
                        }
                    }

                    if (distance == 0)
                    {
                        checkDistance = false;
                    }
                }
            }

            if (distance < 0)
            {
                bool checkDistance = true;
                while (checkDistance)
                {
                    for (var j = 0; j < result.Count; j++)
                    {
                        if (result[j] == minValueOf) continue;
                        result[j]--;
                        distance++;
                        if (distance == 0)
                        {
                            break;
                        }
                    }

                    if (distance == 0)
                    {
                        checkDistance = false;
                    }
                }
            }

            return result.Shuffle().ToList();
        }

        #endregion

        #region "Check Internet"
        public static string GetHtmlFromUri(string resource)
        {
            string html = string.Empty;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(resource);
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    bool isSuccess = (int)resp.StatusCode < 299 && (int)resp.StatusCode >= 200;
                    if (isSuccess)
                    {
                        using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                        {
                            //We are limiting the array to 80 so we don't have
                            //to parse the entire html document feel free to 
                            //adjust (probably stay under 300)
                            char[] cs = new char[80];
                            reader.Read(cs, 0, cs.Length);
                            foreach (char ch in cs)
                            {
                                html += ch;
                            }
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
            return html;
        }
        #endregion

        #region Format Time
        public static string ConvertTimeRemain(int timeValue)
        {
            TimeSpan time = TimeSpan.FromSeconds(timeValue);
            if (time.Hours > 0)
            {
                return string.Format("{0}h{1}m{2}s", time.Hours, time.Minutes, time.Seconds);
            }
            else if (time.Minutes > 0)
            {
                return string.Format("{0}m{1}s", time.Minutes, time.Seconds);
            }
            else
            {
                return string.Format("{0}s", time.Seconds);
            }
        }
        #endregion
    }
}
