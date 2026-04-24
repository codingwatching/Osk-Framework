using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public enum ShutdownType : byte
    {
        None = 0,
        Restart,
        Quit,
    }

    [DefaultExecutionOrder(-1000)]
    public partial class Main : MonoBehaviour
    { 
        public static readonly GameFrameworkLinkedList<GameFrameworkComponent> SGameFrameworkComponents = new();
        
        private static readonly List<IUpdateable> k_Updateables = new();
        private static readonly List<IFixedUpdateable> k_FixedUpdateables = new();
        private static readonly List<ILateUpdateable> k_LateUpdateables = new();
        
        private static bool k_IsPaused = false;
        public static void SetPause(bool pause) => k_IsPaused = pause;

        private void Update()
        {
            if (k_IsPaused) return;
            for (int i = 0; i < k_Updateables.Count; i++)
            {
                k_Updateables[i].OnUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (k_IsPaused) return;
            for (int i = 0; i < k_FixedUpdateables.Count; i++)
            {
                k_FixedUpdateables[i].OnFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (k_IsPaused) return;
            for (int i = 0; i < k_LateUpdateables.Count; i++)
            {
                k_LateUpdateables[i].OnLateUpdate();
            }
        }

        public static T GetModule<T>() where T : GameFrameworkComponent
        {
            return (T)GetModule(typeof(T));
        }

        private static GameFrameworkComponent GetModule(Type type)
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    return current.Value;
                }
                current = current.Next;
            }

            return null;
        } 

        public static GameFrameworkComponent GetModule(string typeName)
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                Type type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return current.Value;
                }
                current = current.Next;
            }

            return null;
        }
    
        internal static void Register(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                OSK.MyLogger.Log("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    OSK.MyLogger.Log($"Game Framework component type {type.FullName} is already exist.");
                    return;
                }

                current = current.Next;
            }

            SGameFrameworkComponents.AddLast(gameFrameworkComponent);

            // Add to update lists if applicable
            if (gameFrameworkComponent is IUpdateable u) k_Updateables.Add(u);
            if (gameFrameworkComponent is IFixedUpdateable f) k_FixedUpdateables.Add(f);
            if (gameFrameworkComponent is ILateUpdateable l) k_LateUpdateables.Add(l);
        }
        
        internal static void UnRegister(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                OSK.MyLogger.Log("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    SGameFrameworkComponents.Remove(current);
                    
                    // Remove from update lists if applicable
                    if (gameFrameworkComponent is IUpdateable u) k_Updateables.Remove(u);
                    if (gameFrameworkComponent is IFixedUpdateable f) k_FixedUpdateables.Remove(f);
                    if (gameFrameworkComponent is ILateUpdateable l) k_LateUpdateables.Remove(l);
                    return;
                }

                current = current.Next;
            }
        }
        
        public static void Shutdown(ShutdownType shutdownType)
        {
            OSK.MyLogger.Log($"Shutdown Game Framework ({shutdownType})...");
            SGameFrameworkComponents.Clear();
            k_Updateables.Clear();
            k_FixedUpdateables.Clear();
            k_LateUpdateables.Clear();

            if (shutdownType == ShutdownType.None)
            {
                return;
            }

            if (shutdownType == ShutdownType.Restart)
            {
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.buildIndex);
                return;
            }

            if (shutdownType == ShutdownType.Quit)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }
        }
    }
}