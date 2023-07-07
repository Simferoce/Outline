using UnityEngine;

public class MeshCutter : MonoBehaviour
{
    [SerializeField] private MeshFilter mesh;
    [SerializeField] private GameObject plane;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            (Mesh A, Mesh B) = MeshExtension.Cut(mesh.sharedMesh, mesh.transform, plane.transform.up, plane.transform.position);
            mesh.mesh = A;
        }
    }

    //public void OnDrawGizmos()
    //{
    //    MeshExtension.Cut(A.sharedMesh, A.transform, plane.transform.up, plane.transform.position);
    //}
}
