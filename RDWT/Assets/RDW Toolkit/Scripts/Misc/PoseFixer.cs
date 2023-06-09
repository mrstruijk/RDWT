using UnityEngine;


public class PoseFixer : MonoBehaviour
{
    [SerializeField] private bool fixPosition = true, fixRotation = true;
    private Vector3 _fixedPosition;
    private Quaternion _fixedRotation;


    private void OnEnable()
    {
        _fixedPosition = transform.position;
        _fixedRotation = transform.rotation;
    }


    // Use this for initialization
    private void Start()
    {
    }


    // Update is called once per frame
    private void Update()
    {
        if (fixPosition)
        {
            transform.position = _fixedPosition;
        }

        if (fixRotation)
        {
            transform.rotation = _fixedRotation;
        }
    }
}