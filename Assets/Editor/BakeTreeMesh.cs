using UnityEngine;
using UnityEditor;

public class BakeTreeMesh : MonoBehaviour
{
  [MenuItem("Tools/Bake Selected Mesh Rotation")]
  public static void BakeMesh()
  {
    GameObject selected = Selection.activeGameObject;
    if (selected == null)
    {
      Debug.LogError("Hãy chọn một cây trên Scene trước!");
      return;
    }

    MeshFilter mf = selected.GetComponentInChildren<MeshFilter>();
    if (mf == null || mf.sharedMesh == null)
    {
      Debug.LogError("Không tìm thấy MeshFilter!");
      return;
    }

    // Tạo bản sao của Mesh và nướng góc xoay/tỉ lệ hiện tại vào Vertex
    Mesh newMesh = Instantiate(mf.sharedMesh);
    Vector3[] vertices = newMesh.vertices;
    Vector3[] normals = newMesh.normals;
    Quaternion rot = selected.transform.rotation;

    for (int i = 0; i < vertices.Length; i++)
    {
      vertices[i] = rot * vertices[i];
      if (normals.Length > 0) normals[i] = rot * normals[i];
    }

    newMesh.vertices = vertices;
    newMesh.normals = normals;
    newMesh.RecalculateBounds();

    // Lưu Mesh mới vào thư mục Assets
    string path = "Assets/" + selected.name + "_BakedMesh.asset";
    AssetDatabase.CreateAsset(newMesh, path);
    AssetDatabase.SaveAssets();

    Debug.Log("Đã tạo Mesh mới thành công tại: " + path);
  }
}