using UnityEditor;
using UnityEngine;

namespace CWH.Player.Test.Editor
{
    // Mixamo 업로드용: 스킨드 메시(뼈대 있는 캐릭터)를 현재 포즈(T포즈) 그대로
    // 뼈대 없는 정적 메시로 구워서(bake) 새 오브젝트로 만들어준다.
    // 이렇게 만든 결과물을 GameObject > Export To FBX 하면 뼈 없는 FBX가 나온다.
    public static class BakeSkinnedMeshForExport
    {
        [MenuItem("Tools/Mixamo/Bake Skinned Meshes To Static (Selected)")]
        private static void BakeSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("씬에서 캐릭터 루트 오브젝트를 먼저 선택하세요.");
                return;
            }

            var skinnedRenderers = selected.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedRenderers.Length == 0)
            {
                Debug.LogError("선택한 오브젝트 하위에 SkinnedMeshRenderer가 없습니다.");
                return;
            }

            var exportRoot = new GameObject(selected.name + "_StaticForMixamo");

            foreach (var smr in skinnedRenderers)
            {
                var bakedMesh = new Mesh { name = smr.sharedMesh.name + "_Baked" };
                smr.BakeMesh(bakedMesh);

                var meshObject = new GameObject(smr.name);
                meshObject.transform.SetParent(exportRoot.transform, false);
                meshObject.transform.position = smr.transform.position;
                meshObject.transform.rotation = smr.transform.rotation;
                meshObject.transform.localScale = smr.transform.lossyScale;

                var filter = meshObject.AddComponent<MeshFilter>();
                filter.sharedMesh = bakedMesh;

                var renderer = meshObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = smr.sharedMaterials;
            }

            Selection.activeGameObject = exportRoot;
            Debug.Log($"'{exportRoot.name}' 생성 완료. 이 오브젝트를 선택한 채로 " +
                      "GameObject > Export To FBX 하면 뼈대 없는 정적 메시로 export됩니다.");
        }
    }
}
