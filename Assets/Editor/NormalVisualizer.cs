using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshFilter))]
public class NormalsVisualizer : Editor
{
    const string _kEditorPrefKey = "_normal_length";

    Mesh _mesh;

    MeshFilter _meshFilter;

    Vector3[] _verts;
    Vector3[] _normals;

    float _normalLength = 1f;

    bool _drawNormals;

    private void OnEnable()
    {
        _meshFilter = (MeshFilter)target;

        if (_meshFilter != null)
            _mesh = _meshFilter.sharedMesh;

        _normalLength = EditorPrefs.GetFloat(_kEditorPrefKey);
    }

    private void OnSceneGUI()
    {
        if (_mesh == null || !_drawNormals || _normalLength <= 0f)
            return;

        _verts = _mesh.vertices;
        _normals = _mesh.normals;

        int len = _mesh.vertexCount;

        Handles.matrix = _meshFilter.transform.localToWorldMatrix;
        Handles.color = Color.green;

        for (int i = 0; i < len; ++i)
            Handles.DrawLine(_verts[i], _verts[i] + _normals[i] * _normalLength);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();

        _normalLength = EditorGUILayout.FloatField("Normal length", _normalLength);
        _drawNormals = EditorGUILayout.Toggle("Draw normals", _drawNormals);

        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetFloat(_kEditorPrefKey, _normalLength);
    }
}
