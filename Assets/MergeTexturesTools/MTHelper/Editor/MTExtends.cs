using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class MTExtends
{
    //public static List<T> Clone<T>(this List<T> list) where T : new()
    //{
    //    List<T> items = new List<T>();

    //    foreach (var m in list)
    //    {
    //        var model = new T();
    //        var ps = model.GetType().GetProperties();
    //        var properties = m.GetType().GetProperties();

    //        foreach (var p in properties)
    //        {
    //            foreach (var pm in ps)
    //            {
    //                if (pm.Name == p.Name)
    //                {
    //                    pm.SetValue(model, p.GetValue(m));
    //                }
    //            }
    //        }

    //        items.Add(model);
    //    }

    //    return items;
    //}

    public class Horizontal : IDisposable
    {
        public Horizontal()
        {
            GUILayout.BeginHorizontal();
        }

        public void Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }
}
