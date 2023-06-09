using UnityEngine;


public class HeadFollower : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;


    // Use this for initialization
    private void Start()
    {
    }


    // Update is called once per frame
    private void Update()
    {
        transform.position = redirectionManager.currPos;

        if (redirectionManager.currDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(redirectionManager.currDir, Vector3.up);
        }
    }
}