using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CWH.Villains
{
    public static class ConvenienceStoreNavMeshBootstrapper
    {
        private const string ConvenienceStoreScenePath = "Assets/IYC/00.Scene/ConvenienceStore.unity";
        private const string SurfaceName = "Convenience Store Runtime NavMesh Surface";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            EnsureBuiltForActiveScene();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static void EnsureBuiltForActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!IsConvenienceStoreScene(activeScene) || HasUsableNavMesh())
            {
                return;
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                GameObject surfaceObject = new GameObject(SurfaceName);
                surface = surfaceObject.AddComponent<NavMeshSurface>();
            }

            ConfigureSurface(surface, NavMeshCollectGeometry.PhysicsColliders);
            surface.BuildNavMesh();

            if (!HasUsableNavMesh())
            {
                ConfigureSurface(surface, NavMeshCollectGeometry.RenderMeshes);
                surface.BuildNavMesh();
            }

            Debug.Log($"Convenience Store runtime NavMesh built. vertices: {NavMesh.CalculateTriangulation().vertices.Length}");
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBuiltForActiveScene();
        }

        private static void ConfigureSurface(NavMeshSurface surface, NavMeshCollectGeometry geometry)
        {
            surface.agentTypeID = 0;
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = ~0;
            surface.useGeometry = geometry;
            surface.defaultArea = 0;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.08f;
            surface.minRegionArea = 0.35f;
        }

        private static bool HasUsableNavMesh()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            return triangulation.vertices != null && triangulation.vertices.Length > 0;
        }

        private static bool IsConvenienceStoreScene(Scene scene)
        {
            return scene.path == ConvenienceStoreScenePath
                   || scene.name.Contains("ConvenienceStore")
                   || scene.name.Contains("Convenience Store")
                   || Object.FindFirstObjectByType<VillainSpawnPoint>() != null
                   || GameObject.Find("automaticDoor_L_gp") != null
                   || GameObject.Find("automaticDoor_R_gp") != null;
        }
    }
}
