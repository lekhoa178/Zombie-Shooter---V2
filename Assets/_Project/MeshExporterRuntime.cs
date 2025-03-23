using System.Collections;
using UnityEngine;
using System.IO;

public class MeshExporterRuntime : MonoBehaviour
{
    public Animator animator;
    public SkinnedMeshRenderer skinnedMesh;
    public string animationName;
    public int frame = 0;
    public int frameRate = 30;
    
    void Start()
    {
        for (int i = 0; i < 13; ++i)
        {
            StartCoroutine(ExportMeshAtFrame(animationName, i));
        }
    }
    
    public IEnumerator ExportMeshAtFrame(string animName, int frame)
    {
        yield return new WaitForSeconds(2f);
        
        float normalizedTime = frame / (frameRate * animator.GetCurrentAnimatorStateInfo(0).length);

        animator.Play(animName, 0, normalizedTime);
        animator.Update(0);

        Mesh bakedMesh = new Mesh();
        skinnedMesh.BakeMesh(bakedMesh);

        SaveMeshAsObj(bakedMesh, Path.Combine(Application.persistentDataPath, $"ExportedMesh_Frame{frame}.obj"));
    }

    void SaveMeshAsObj(Mesh mesh, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine($"# Exported mesh: {mesh.name}");

            foreach (Vector3 v in mesh.vertices)
                writer.WriteLine($"v {v.x} {v.y} {v.z}");

            foreach (Vector3 n in mesh.normals)
                writer.WriteLine($"vn {n.x} {n.y} {n.z}");

            foreach (Vector2 uv in mesh.uv)
                writer.WriteLine($"vt {uv.x} {uv.y}");

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                writer.WriteLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
            }
        }

        Debug.Log($"Mesh exported to {path}");
    }
}