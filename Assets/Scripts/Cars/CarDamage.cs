using UnityEngine;
using CameraGame;

namespace Cars
{
    public struct VertexCollision
    {
        public Vector3[] VertexMesh;
    }

    public struct ObjectTags
    {
        public static string player = "Player";

        public static string grass = "Grass";
    }

    public class CarDamage : MonoBehaviour
    {
        public float maxMoveDelta = 1.0f; // maximum distance one vertices moves per explosion (in meters)
        public float maxCollisionStrength = 50.0f;
        public float YforceDamp = 0.1f; // 0.0 - 1.0
        public float demolutionRange = 0.5f;
        public float impactDirManipulator = 0.0f;

        public MeshFilter[] optionalMeshList;
        public AudioSource crashSound;

        private MeshFilter[] _Meshfilters;

        private float _sqrDemRange;

        private Vector3 _ColPointToMe;
        private float _colStrength;

        private TraumaInducer _TraumaInducer;

        // Car crash data
        private VertexCollision[] _OriginalVertexData;

        // Start is called before the first frame update
        private void Start()
        {
            if (optionalMeshList.Length > 0)
            {
                _Meshfilters = optionalMeshList;
            }
            else
            {
                _Meshfilters = GetComponentsInChildren<MeshFilter>();
            }

            _sqrDemRange = demolutionRange * demolutionRange;

            _TraumaInducer = FindObjectOfType(typeof(TraumaInducer)) as TraumaInducer;

            // Active car damage
            LoadMeshData();
        }

        // Update is called once per frame
        private void Update()
        {
            // Repair car when pressed R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                RepairCar();

                // Debug.LogAssertionFormat("Repair Car");
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            // Uncomment this for mesh collider with player
            // if (collision.gameObject.CompareTag("Player")) return;

            Vector3 colRelVel = collision.relativeVelocity;

            colRelVel.y *= YforceDamp;

            if (collision.contacts.Length > 0)
            {
                _ColPointToMe = transform.position - collision.contacts[0].point;

                _colStrength = colRelVel.magnitude * Vector3.Dot(collision.contacts[0].normal, _ColPointToMe.normalized);

                if (_ColPointToMe.magnitude > 1.0f && !crashSound.isPlaying)
                {
                    crashSound.Play();

                    crashSound.volume = _colStrength / 200f;
                    
                    /*
                    if (crashSound.isPlaying)
                    {
                        // Camera shake
                        StartCoroutine(_TraumaInducer.StartScreenCameraShake());
    
                        Debug.LogAssertion("Shake");
                    }
                    */

                    if (collision.gameObject.CompareTag(ObjectTags.grass))
                    {
                        // Camera shake
                        StartCoroutine(_TraumaInducer.StartScreenCameraShake());

                        // Debug.LogAssertion("Shake");
                    }

                    // Debug.Log("Crash!");

                    OnMeshForce(collision.contacts[0].point, Mathf.Clamp01(_colStrength / maxCollisionStrength));
                }
            }
        }

        // if called by SendMessage(), we only have 1 param
        public void OnMeshForce(Vector4 originPosAndForce)
        {
            OnMeshForce(originPosAndForce, originPosAndForce.w);
        }

        private void OnMeshForce(Vector3 originPos, float force)
        {
            // force should be between 0.0 and 1.0
            force = Mathf.Clamp01(force);

            for (int j = 0; j < _Meshfilters.Length; ++j)
            {
                Vector3[] vertex = _Meshfilters[j].mesh.vertices;

                for (int i = 0; i < vertex.Length; ++i)
                {
                    Vector3 scaledVert = Vector3.Scale(vertex[i], transform.localScale);
                    Vector3 vertWorldPos = _Meshfilters[j].transform.position + (_Meshfilters[j].transform.rotation * scaledVert);
                    Vector3 originToMeDir = vertWorldPos - originPos;
                    Vector3 flatVertToCenterDir = transform.position - vertWorldPos;

                    flatVertToCenterDir.y = 0.0f;

                    // 0.5 - 1 => 45 to 0  / current vertice is nearer to exploPos than center of bounds
                    if (originToMeDir.sqrMagnitude < _sqrDemRange) //dot > 0.8f )
                    {
                        float dist = Mathf.Clamp01(originToMeDir.sqrMagnitude / _sqrDemRange);

                        float moveDelta = force * (1.0f - dist) * maxMoveDelta;

                        Vector3 moveDir = Vector3.Slerp(originToMeDir, flatVertToCenterDir, impactDirManipulator).normalized * moveDelta;

                        vertex[i] += Quaternion.Inverse(transform.rotation) * moveDir;
                    }
                }

                _Meshfilters[j].mesh.vertices = vertex;
                _Meshfilters[j].mesh.RecalculateBounds();
            }
        }

        // Make car damage more realistic with mesh collider 
        private void LoadMeshData()
        {
            _OriginalVertexData = new VertexCollision[_Meshfilters.Length];

            for (int i = 0; i < _Meshfilters.Length; i++)
            {
                _OriginalVertexData[i].VertexMesh = _Meshfilters[i].mesh.vertices;
            }
        }

        private void RepairCar()
        {
            for (int i = 0; i < _Meshfilters.Length; i++)
            {
                _Meshfilters[i].mesh.vertices = _OriginalVertexData[i].VertexMesh;

                _Meshfilters[i].mesh.RecalculateNormals();

                _Meshfilters[i].mesh.RecalculateBounds();
            }
        }
    }
}
