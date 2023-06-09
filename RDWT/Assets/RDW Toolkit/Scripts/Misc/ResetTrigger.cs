using UnityEngine;


public class ResetTrigger : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;
    [HideInInspector]
    public Collider bodyCollider;

    [Range(0f, 1f)]
    public float ResetTriggerBuffer = 0.5f;

    [HideInInspector]
    public float xLength, zLength;


    public void Initialize()
    {
        // Set Size of Collider
        var trimAmountOnEachSide = bodyCollider.transform.localScale.x + 2 * ResetTriggerBuffer;
        transform.localScale = new Vector3(1 - trimAmountOnEachSide / transform.parent.localScale.x, 2 / transform.parent.localScale.y, 1 - trimAmountOnEachSide / transform.parent.localScale.z);
        xLength = transform.parent.localScale.x - trimAmountOnEachSide;
        zLength = transform.parent.localScale.z - trimAmountOnEachSide;
    }


    private void OnTriggerEnter(Collider other)
    {
    }


    private void OnTriggerExit(Collider other)
    {
        if (other == bodyCollider)
        {
            redirectionManager.OnResetTrigger();
        }
    }
}