﻿namespace Magma
{
    using Facepunch.Utility;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text.RegularExpressions;
    using uLink;
    using UnityEngine;

    public class Util
    {
        private Dictionary<string, System.Type> typeCache = new Dictionary<string, System.Type>();
        private static Util util;

        public void ConsoleLog(string str, [Optional, DefaultParameterValue(false)] bool adminOnly)
        {
            foreach (Magma.Player player in Magma.Server.GetServer().Players)
            {
                if (player.PlayerClient != null)
                {
                    uLink.NetworkPlayer netPlayer = player.PlayerClient.netPlayer;
                    if (((player.PlayerClient.netPlayer != uLink.NetworkPlayer.unassigned) && player.PlayerClient.netPlayer.isClient) && player.PlayerClient.netPlayer.isConnected)
                    {
                        if (!adminOnly)
                        {
                            ConsoleNetworker.singleton.networkView.RPC<string>("CL_ConsoleMessage", player.PlayerClient.netPlayer, str);
                        }
                        else if (player.Admin)
                        {
                            ConsoleNetworker.singleton.networkView.RPC<string>("CL_ConsoleMessage", player.PlayerClient.netPlayer, str);
                        }
                    }
                }
            }
        }

        public object CreateArrayInstance(string name, int size)
        {
            System.Type type;
            if (!this.TryFindType(name.Replace('.', '+'), out type))
            {
                return null;
            }
            if (type.BaseType.Name == "ScriptableObject")
            {
                return ScriptableObject.CreateInstance(name);
            }
            return Array.CreateInstance(type, size);
        }

        public object CreateInstance(string name, params object[] args)
        {
            System.Type type;
            if (!this.TryFindType(name.Replace('.', '+'), out type))
            {
                return null;
            }
            if (type.BaseType.Name == "ScriptableObject")
            {
                return ScriptableObject.CreateInstance(name);
            }
            return Activator.CreateInstance(type, args);
        }

        public Quaternion CreateQuat(float x, float y, float z, float w)
        {
            return new Quaternion(x, y, z, w);
        }

        public Vector3 CreateVector(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        public void DestroyObject(GameObject go)
        {
            NetCull.Destroy(go);
        }

        public static string GetAbsoluteFilePath(string fileName)
        {
            return (GetMagmaFolder() + fileName);
        }

        public static string GetMagmaFolder()
        {
            return Magma.Data.PATH;
        }

        public static string GetRootFolder()
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
        }

        public static string GetServerFolder()
        {
            return (Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))) + @"\rust_server_Data\");
        }

        public object GetStaticField(string className, string field)
        {
            System.Type type;
            if (this.TryFindType(className.Replace('.', '+'), out type))
            {
                FieldInfo info = type.GetField(field, BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    return info.GetValue(null);
                }
            }
            return null;
        }

        public static Util GetUtil()
        {
            if (util == null)
            {
                util = new Util();
            }
            return util;
        }

        public float GetVectorsDistance(Vector3 v1, Vector3 v2)
        {
            return TransformHelpers.Dist2D(v1, v2);
        }

        public static Hashtable HashtableFromFile(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            StreamReader reader = new StreamReader(stream);
            BinaryFormatter formatter = new BinaryFormatter();
            return (Hashtable) formatter.Deserialize(reader.BaseStream);
        }

        public static void HashtableToFile(Hashtable ht, string path)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                StreamWriter writer = new StreamWriter(stream);
                formatter.Serialize(writer.BaseStream, ht);
            }
        }

        public Vector3 Infront(Magma.Player p, float length)
        {
            return (p.PlayerClient.controllable.transform.position + ((Vector3) (p.PlayerClient.controllable.transform.forward * length)));
        }

        public object InvokeStatic(string className, string method, ParamsList args)
        {
            System.Type type;
            if (!this.TryFindType(className.Replace('.', '+'), out type))
            {
                return null;
            }

            MethodInfo info = type.GetMethod(method, BindingFlags.Static);
            if (info == null)
            {
                return null;
            }
            if (info.ReturnType == typeof(void))
            {
                info.Invoke(null, args.ToArray());
                return true;
            }
            return info.Invoke(null, args.ToArray());
        }

        public bool IsNull(object obj)
        {
            return (obj == null);
        }

        public void Log(string str)
        {
            Console.WriteLine(str);
        }

        public Match Regex(string input, string match)
        {
            return new System.Text.RegularExpressions.Regex(input).Match(match);
        }

        public Quaternion RotateX(Quaternion q, float angle)
        {
            return (q *= Quaternion.Euler(angle, 0f, 0f));
        }

        public Quaternion RotateY(Quaternion q, float angle)
        {
            return (q *= Quaternion.Euler(0f, angle, 0f));
        }

        public Quaternion RotateZ(Quaternion q, float angle)
        {
            return (q *= Quaternion.Euler(0f, 0f, angle));
        }

        public static void say(uLink.NetworkPlayer player, string playername, string arg)
        {
            ConsoleNetworker.SendClientCommand(player, "chat.add " + playername + " " + arg);
        }

        public static void sayAll(string arg)
        {
            ConsoleNetworker.Broadcast("chat.add " + Facepunch.Utility.String.QuoteSafe(Magma.Server.GetServer().server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(arg));
        }

        public static void sayUser(uLink.NetworkPlayer player, string arg)
        {
            ConsoleNetworker.SendClientCommand(player, "chat.add " + Facepunch.Utility.String.QuoteSafe(Magma.Server.GetServer().server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(arg));
        }

        public static void sayUser(uLink.NetworkPlayer player, string customName, string arg)
        {
            ConsoleNetworker.SendClientCommand(player, "chat.add " + Facepunch.Utility.String.QuoteSafe(customName) + " " + Facepunch.Utility.String.QuoteSafe(arg));
        }

        public void SetStaticField(string className, string field, object val)
        {
            System.Type type;
            if (this.TryFindType(className.Replace('.', '+'), out type))
            {
                FieldInfo info = type.GetField(field, BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    info.SetValue(null, Convert.ChangeType(val, info.FieldType));
                }
            }
        }

        public bool TryFindType(string typeName, out System.Type t)
        {
            lock (this.typeCache)
            {
                if (!this.typeCache.TryGetValue(typeName, out t))
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = assembly.GetType(typeName);
                        if (t != null)
                        {
                            break;
                        }
                    }
                    this.typeCache[typeName] = t;
                }
            }
            return (t != null);
        }
    }
}

