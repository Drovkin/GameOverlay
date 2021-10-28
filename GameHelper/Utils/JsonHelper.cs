﻿// <copyright file="JsonHelper.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.Utils
{
    using System.IO;
    using GameHelper.Settings;
    using Newtonsoft.Json;

    /// <summary>
    /// Utility functions to help read/write to Json files.
    /// </summary>
    internal static class JsonHelper
    {
        public static void Save(this State state) {
            SafeToFile(Core.GHSettings, State.CoreSettingFile);
        }
        /// <summary>
        /// Creates new instance or load from the file if file exists.
        /// </summary>
        /// <typeparam name="T">Class name to (De)serialize.</typeparam>
        /// <param name="file">file to load from.</param>
        /// <returns>class objecting containing the data (if data exits).</returns>
        public static T CreateOrLoadJsonFile<T>(FileInfo file)
            where T : new()
        {
            file.Directory.Create();
            if (file.Exists)
            {
                var content = File.ReadAllText(file.FullName);
                return JsonConvert.DeserializeObject<T>(content);
            }
            else
            {
                T obj = new();
                JsonHelper.SafeToFile(obj, file);
                return obj;
            }
        }

        /// <summary>
        /// Save the class object into the file.
        /// </summary>
        /// <param name="classObject">class object to save in the file.</param>
        /// <param name="file">file to save in.</param>
        public static void SafeToFile(object classObject, FileInfo file)
        {
            var content = JsonConvert.SerializeObject(classObject, Formatting.Indented);
            File.WriteAllText(file.FullName, content);
        }
    }
}
