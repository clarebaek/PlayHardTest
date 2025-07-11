﻿using System;
using UnityEngine;

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// As a note, this is made as MonoBehaviour because we need Coroutines.
/// </summary>
///

namespace Utility.Singleton
{
	public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance = null;
		private static object _syncobj = new object();
		private static bool appIsClosing = false;

		public static T Instance
		{
			get
			{
				bool force = false;

#if UNITY_EDITOR
				if (false == Application.isPlaying)
				{
					force = true;
				}
#endif
				if (false == force && appIsClosing)
					return null;
				try
				{
					lock (_syncobj)
					{
						if (_instance == null)
						{
							T[] objs = FindObjectsOfType<T>();

							if (objs.Length > 0)
								_instance = objs[0];

							if (objs.Length > 1)
								Debug.LogError("There is more than one " + typeof(T).Name + " in the scene.");

							if (_instance == null)
							{
								string goName = typeof(T).ToString();
								GameObject go = GameObject.Find(goName);
								if (go == null)
								{
#if UNITY_EDITOR
									Debug.Log($"MonoSingleton {goName} Create");
#endif
									go = new GameObject(goName);
#if UNITY_EDITOR
								}
								else
								{
									Debug.Log($"MonoSingleton {goName} Find");
#endif
								}

								if (false == go.TryGetComponent(out _instance))
								{
									_instance = go.AddComponent<T>();
								}
							}

							//if (_instance is IInstantiatable instantiatable)
							//{
							//	instantiatable.OnInstantiate();
							//}
						}
						return _instance;
					}
				}
				catch (System.Exception e)
				{
					Debug.LogError($"Singleton<{typeof(T).Name}>.Instance : create error : {e.Message}");
					throw;
				}
			}
		}

		/// <summary>
		/// When Unity quits, it destroys objects in a random order.
		/// In principle, a Singleton is only destroyed when application quits.
		/// If any script calls Instance after it have been destroyed,
		///   it will create a buggy ghost object that will stay on the Editor scene
		///   even after stopping playing the Application. Really bad!
		/// So, this was made to be sure we're not creating that buggy ghost object.
		/// </summary>
		protected virtual void OnApplicationQuit()
		{
			// release reference on exit
			appIsClosing = true;
		}
	}
}